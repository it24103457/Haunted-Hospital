from pathlib import Path
import json
from collections import deque
import numpy as np

p=Path(__file__).parent
d=json.loads((p/'hospital_skeleton_coordinate_schedule.json').read_text())
cell=.05
xs=np.arange(-17.5,16.8,cell)+cell/2
zs=np.arange(-.5,42.6,cell)+cell/2
X,Z=np.meshgrid(xs,zs)
walk=np.zeros(X.shape,dtype=bool)
closed={'Elevator','Service shaft','Stair enclosure','Operating service shaft','North service alcove'}
for f in d['floors']:
    a,b,c,e=f['b']
    if f['room'] not in closed:walk|=(X>=a)&(X<=b)&(Z>=c)&(Z<=e)
for o in d['objects']:
    if o['parent']=='GEO_Floors' or any('Disable GameObject' in f for f in o['flags']):continue
    x,y,z=o['pos'];w,h,l=o['scale']
    if y-h/2>=1.8 or y+h/2<=0:continue
    walk&=~((X>=x-w/2)&(X<=x+w/2)&(Z>=z-l/2)&(Z<=z+l/2))
# Conservative 0.35 m circular clearance, 0.05 m beyond the proposed radius.
safe=walk.copy();r=7
pad=np.pad(walk,r)
for dz in range(-r,r+1):
    for dx in range(-r,r+1):
        if dx*dx+dz*dz<=r*r:
            safe &= pad[r+dz:r+dz+walk.shape[0],r+dx:r+dx+walk.shape[1]]
def ix(x,z):return (int(np.argmin(abs(zs-z))),int(np.argmin(abs(xs-x))))
start=ix(0,2)
assert safe[start]
seen=np.zeros_like(safe);seen[start]=True;q=deque([start])
while q:
    z,x=q.popleft()
    for zz,xx in ((z-1,x),(z+1,x),(z,x-1),(z,x+1)):
        if 0<=zz<safe.shape[0] and 0<=xx<safe.shape[1] and safe[zz,xx] and not seen[zz,xx]:
            seen[zz,xx]=True;q.append((zz,xx))
failed=[]
for f in d['floors']:
    if f['room'] in closed:continue
    a,b,c,e=f['b']
    # Thin notches are floor coverage, not standing destinations.
    if min(b-a,e-c)<1:continue
    if not seen[ix((a+b)/2,(c+e)/2)]:failed.append(f['name'])
assert not failed,failed
for door in d['doors']:
    if door['name'] in ('D_MainEntrance','D_Elevator'):continue
    for side in [-.55,.55]:
        x,z=(door['c'],door['line']+side) if door['axis']=='H' else (door['line']+side,door['c'])
        assert seen[ix(x,z)],(door['name'],side)
report={
    'method':'2D geometric clearance and connectivity; NOT a Unity NavMesh bake or play test',
    'grid_metres':cell,'checked_radius_metres':.35,
    'accessible_floor_centres':'pass','both_sides_of_open_interior_doors':'pass',
    'unreachable_clear_floor_cells':int((safe&~seen).sum()),
    'floor_overlap_check':'pass (generator)',
    'opening_boundary_and_room_adjacency_checks':'pass (generator)'
}
assert report['unreachable_clear_floor_cells']==0,report
(p/'Hospital_Skeleton_Coordinate_Checks.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
