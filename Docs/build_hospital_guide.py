from pathlib import Path
from collections import defaultdict, deque
import json, re, math

OUT = Path(__file__).parent
OUT.mkdir(exist_ok=True)
floors = []
def floor(name, room, x0,x1,z0,z1, note):
    floors.append(dict(name=name,room=room,b=(x0,x1,z0,z1),note=note))

# Shared, snapped boundaries traced from the supplied raster; not its inconsistent labels.
floor('F_MainCorridor','Corridor',-1.5,2.75,0,9.5,'The south end of the main spine; the entrance is at its south edge.')
floor('F_PostOpRecovery','Post-op',-13.25,-3.25,0,7.25,'Bottom-left room. The entrance apron is immediately to its east.')
floor('F_RecoveryRoom','Recovery',2.75,10.5,0,7,'Bottom-right, immediately east of the main spine.')
floor('F_WasteDisposal','Waste',10.5,16,0,7,'Immediately east of Recovery. Its south and north edges align with Recovery.')
floor('F_StaffRoom','Staff room',9.5,16,7,12.5,'Above Waste, to the right of the elevator lobby. This is the lower Staff Room, not the small top Staff utility room.')
floor('F_Storage','Storage',8.25,16,12.5,18,'Above Staff Room. The right edges align; Storage extends farther left, as in the drawing.')
floor('F_Elevator','Elevator',2.75,8.25,9,14.75,'Central-right shaft footprint. Its entrance faces south into the elevator lobby.')
floor('F_EntranceApron','Corridor',-3.25,-1.5,0,7.25,'Fills the strip between Post-op and the lower main corridor.')
floor('F_ElevatorLobby','Corridor',2.75,9.5,7,9,'Connects the main corridor, Recovery, elevator entrance and lower Staff Room. This replaces the old conflicting corridor concept with an explicitly bounded lobby.')
floor('F_ElevatorServiceStrip','Service shaft',8.25,9.5,9,12.5,'The narrow strip beside the elevator is enclosed service space, not a through-corridor.')
floor('F_WestLowerHall','Corridor',-6.75,-1.5,7.25,9.5,'Passage below the Office and above Post-op; joins the main spine on its east edge.')
floor('F_GeneralWard','General Ward',-15.75,-6.75,7.25,16.5,'Left of the Office and west lower hall; below the west cross-corridor.')
floor('F_NurseOffice','Nurse Office',-6.75,-0.75,9.5,16.5,'Immediately right of the General Ward. Its east side makes the main passage narrower here.')
floor('F_MainCorridor_Office','Corridor',-0.75,2.75,9.5,16.5,'Main passage alongside the Office. Do not extend the previous corridor cube through the Office.')
floor('F_MainCorridor_Middle','Corridor',-1.5,2.75,16.5,23.5,'Connects the Office segment to the wider upper corridor.')
floor('F_WestCrossCorridor','Corridor',-15.75,-1.5,16.5,19.25,'The long horizontal corridor between General Ward/Office and Anaesthesia Prep.')
floor('F_RightLanding','Corridor',2.75,8.25,14.75,17.25,'Above the elevator and below the stair footprint; leads to Storage.')
floor('F_StairFootprint','Stair enclosure',2.75,6,17.25,23.5,'Preserves the staircase location. This prototype encloses it; do not build an upper floor or traversable stairs.')
floor('F_PreOpApproach','Corridor',6,8.25,17.25,23.5,'Passage east of the enclosed stairs. Joins the right landing and the Pre-op entrance.')
floor('F_PreOp','Pre-op',8.25,16,18,24,'Above Storage and below Sterile Core.')
floor('F_AnaesthesiaPrep','Anaesthesia',-16.75,-6.75,19.25,26.5,'Above the west cross-corridor and below Operating Room 1.')
floor('F_WashroomAlcove','Washroom alcove',-6.75,-1.5,19.25,24.5,'The unlabeled washroom/service area drawn east of Anaesthesia. No fixtures are added.')
floor('F_OperatingApproach','Corridor',-6.75,-1.5,24.5,26.5,'Short east-west connector between Anaesthesia, the washroom alcove and the main spine.')
floor('F_OperatingDoorRecess','Corridor',-6.75,-3.25,26.5,27.5,'The northward recess at Operating Room 1\'s doorway.')
floor('F_OperatingApproachReturn','Corridor',-3.25,-1.5,26.5,27.5,'Completes the connector beside the Operating doorway recess.')
floor('F_MainCorridor_Upper','Corridor',-1.5,4.5,23.5,31.5,'Wider central passage west of Sterile Core.')
floor('F_SterileCore','Sterile Core',4.5,16,24,31.5,'Below ICU/Diagnostics and above Pre-op.')
floor('F_SterileCore_SouthNotch','Sterile Core',4.5,8.25,23.5,24,'Small extension at Sterile Core\'s southwest corner. This is part of the same room; do not wall its seam.')
floor('F_OperatingRoom1','Operating Room',-15.75,-6.75,26.5,35.75,'Main west portion of Operating Room 1.')
floor('F_OperatingRoom1_East','Operating Room',-6.75,-3.25,27.5,35.75,'East portion of the same Operating Room, above its doorway recess.')
floor('F_OperatingRoom1_North','Operating Room',-10,-4.75,35.75,36.75,'Raised outline on the north edge of Operating Room 1. All three slabs form one continuous room.')
floor('F_OperatingServiceShaft','Operating service shaft',-3.25,-1.5,27.5,35.75,'Simplified enclosed equipment/shaft strip between Operating Room and the main corridor.')
floor('F_MainCorridor_North','Corridor',-1.5,1.75,31.5,35.75,'Northward corridor leading toward WC and upper Staff utility room.')
floor('F_NorthLobby','Corridor',-3.25,1.75,35.75,37.75,'Wider top lobby directly below WC and the small Staff room.')
floor('F_NorthServiceAlcove','North service alcove',-4.75,-3.25,35.75,37.75,'Small enclosed service recess west of the north lobby. Treat its ambiguous interior symbols as a closed footprint in this first blockout.')
floor('F_WC','WC',-4.75,-0.25,37.75,42,'Small top-left WC; its door faces the north lobby.')
floor('F_StaffUtility','Staff utility',-0.25,4,37.75,42,'Small top Staff utility room, distinct from the large lower Staff Room.')
floor('F_ICUTreatment','ICU',1.75,9,31.5,37.75,'ICU lower portion, above Sterile Core and left of Diagnostics.')
floor('F_ICUTreatment_North','ICU',4,9,37.75,42,'ICU upper extension beside the Staff utility room. No wall across the seam.')
floor('F_DiagnosticsTests','Diagnostics',9,16,31.5,38.5,'Top-right room. Its north edge is lower than ICU\'s, matching the stepped silhouette.')

doors=[]
def door(name,axis,line,c,w,a,b,note):
    doors.append(dict(name=name,axis=axis,line=line,c=c,w=w,a=a,b=b,note=note,kind='door'))
door('D_MainEntrance','H',0,0,1.8,'Corridor','Outside','South main entrance. Closed placeholder for the one-exit prototype.')
door('D_PostOp','H',7.25,-5.15,1.2,'Post-op','Corridor','North side of Post-op, opening into the lower west hall.')
door('D_Ward_PostOp','H',7.25,-12,1.2,'Post-op','General Ward','Door drawn at the northwest of Post-op into the Ward.')
door('D_Recovery','H',7,4.1,1.2,'Recovery','Corridor','Recovery opens north into the elevator lobby.')
door('D_Waste','H',7,11.8,1.2,'Waste','Staff room','Waste is reached from the Staff Room, as the drawn north doorway indicates.')
door('D_StaffRoom','V',9.5,8,1.2,'Staff room','Corridor','West side of Staff Room, facing the elevator lobby.')
door('D_Elevator','H',9,4.1,1.8,'Elevator','Corridor','South elevator opening. Closed placeholder; keypad and unlock behavior belong to later gameplay work.')
door('D_Storage','V',8.25,16,1.2,'Storage','Corridor','West side of Storage, facing the landing above the elevator.')
door('D_Ward_North','H',16.5,-9,1.2,'General Ward','Corridor','North Ward doorway off the west cross-corridor.')
door('D_Ward_East','V',-6.75,8.35,1.2,'General Ward','Corridor','Lower east Ward doorway into the lower west hall.')
door('D_Office_North','H',16.5,-5.75,1.2,'Nurse Office','Corridor','Office entrance on its north edge.')
door('D_Office_South','H',9.5,-2.05,1.2,'Nurse Office','Corridor','Second Office entrance into the lower west hall.')
door('D_PreOp','V',8.25,20.55,1.2,'Pre-op','Corridor','Pre-op door faces the passage east of the stair enclosure.')
door('D_Anaesthesia','V',-6.75,25.45,1.2,'Anaesthesia','Corridor','Upper east side of Anaesthesia, opening toward the Operating approach.')
door('D_Washroom','H',24.5,-5.7,1.2,'Washroom alcove','Corridor','Door on the north side of the unlabeled washroom.')
door('D_Operating','H',27.5,-5.4,1.5,'Operating Room','Corridor','Operating Room door faces south into its recessed approach.')
door('D_Sterile_West','V',4.5,25.3,1.2,'Sterile Core','Corridor','West side of Sterile Core, facing the wide main corridor.')
door('D_ICU_Sterile','H',31.5,5.6,1.2,'ICU','Sterile Core','Connection drawn between ICU and Sterile Core.')
door('D_ICU_West','V',1.75,36.65,1.2,'ICU','Corridor','Upper west ICU doorway into the north lobby.')
door('D_Diagnostics','V',9,33.25,1.2,'Diagnostics','ICU','Diagnostics opens west into ICU; no invented corridor through ICU.')
door('D_WC','H',37.75,-1.15,1.2,'WC','Corridor','WC door faces south into the north lobby.')
door('D_StaffUtility','H',37.75,0.65,1.2,'Staff utility','Corridor','Small Staff utility door faces south into the north lobby.')

windows=[]
def window(name,axis,line,c,w):
    windows.append(dict(name=name,axis=axis,line=line,c=c,w=w,kind='window'))
window('WIN_PostOp_South','H',0,-8.25,2.25)
window('WIN_PostOp_West','V',-13.25,3.5,2.5)
window('WIN_Recovery_South','H',0,6.4,2.5)
window('WIN_Waste_South','H',0,13.25,1.5)
window('WIN_Staff_East','V',16,9.6,1.5)
window('WIN_Storage_East','V',16,15.3,2)
window('WIN_PreOp_East','V',16,21.15,2)
window('WIN_Sterile_East','V',16,27.5,2)
window('WIN_Diagnostics_East','V',16,34.25,2)
window('WIN_Diagnostics_North','H',38.5,12,2.5)
window('WIN_ICU_North','H',42,6.4,2.5)
window('WIN_StaffUtility_North','H',42,1.8,1.5)
window('WIN_WC_North','H',42,-2.55,1.5)
window('WIN_Operating_NorthWest','H',35.75,-12.75,2.5)
window('WIN_Operating_NorthStep','H',36.75,-7.3,2.5)
window('WIN_Operating_West','V',-15.75,31.2,2.5)
window('WIN_Anaesthesia_West','V',-16.75,23,2.5)
window('WIN_Ward_West','V',-15.75,11.75,2.5)

def owner(x,z):
    hits=[f['room'] for f in floors if f['b'][0]<x<f['b'][1] and f['b'][2]<z<f['b'][3]]
    assert len(hits)<=1,(x,z,hits)
    return hits[0] if hits else 'Outside'
xs=sorted(set(v for f in floors for v in f['b'][:2]))
zs=sorted(set(v for f in floors for v in f['b'][2:]))
for i,f in enumerate(floors):
    a,b,c,d=f['b']
    assert a<b and c<d
    for g in floors[i+1:]:
        e,h,j,k=g['b']
        assert min(b,h)<=max(a,e) or min(d,k)<=max(c,j),(f['name'],g['name'])
raw=[]
for axis, lines, breaks in [('V',xs,zs),('H',zs,xs)]:
    for line in lines:
        for lo,hi in zip(breaks,breaks[1:]):
            mid=(lo+hi)/2
            a,b=(owner(line-.001,mid),owner(line+.001,mid)) if axis=='V' else (owner(mid,line-.001),owner(mid,line+.001))
            if a!=b: raw.append(dict(axis=axis,line=line,lo=lo,hi=hi,pair=tuple(sorted([a,b]))))
groups=defaultdict(list)
for r in raw: groups[(r['axis'],r['line'],r['pair'])].append((r['lo'],r['hi']))
spans=[]
for (axis,line,pair), intervals in groups.items():
    runs=[]
    for lo,hi in sorted(intervals):
        if runs and runs[-1][1]==lo:runs[-1][1]=hi
        else:runs.append([lo,hi])
    for lo,hi in runs:spans.append(dict(axis=axis,line=line,lo=lo,hi=hi,pair=pair,openings=[]))
spans.sort(key=lambda s:(0 if 'Outside' in s['pair'] else 1,s['line'] if s['axis']=='H' else s['lo'],s['axis'],s['line']))
for o in doors+windows:
    lo,hi=o['c']-o['w']/2,o['c']+o['w']/2
    matches=[s for s in spans if s['axis']==o['axis'] and s['line']==o['line'] and s['lo']+.099<lo and s['hi']-.099>hi]
    assert len(matches)==1,(o,matches)
    s=matches[0]
    if o['kind']=='door': assert set(s['pair'])==set([o['a'],o['b']]),(o,s)
    else: assert 'Outside' in s['pair'],(o,s)
    s['openings'].append(o)
    o['pair']=s['pair']
    o['span']=spans.index(s)+1

objects=[]
def obj(name,parent,pos,scale,info='',flags=None):
    d=dict(name=name,parent=parent,pos=pos,scale=scale,info=info,flags=flags or [])
    objects.append(d)
    return d
for f in floors:
    a,b,c,d=f['b']
    f['object']=obj(f['name'],'GEO_Floors',((a+b)/2,-.1,(c+d)/2),(b-a,.2,d-c),f['note'])
for i,s in enumerate(spans,1):
    s['objects']=[]
    def segment(suffix,lo,hi,y0,y1):
        if hi-lo<.00001:return
        length=hi-lo
        # Only terminal span ends extend .1 to close perpendicular cube corners.
        if abs(lo-s['lo'])<1e-7:lo-=.1
        if abs(hi-s['hi'])<1e-7:hi+=.1
        if s['axis']=='H':pos=((lo+hi)/2,(y0+y1)/2,s['line']);scale=(hi-lo,y1-y0,.2)
        else:pos=(s['line'],(y0+y1)/2,(lo+hi)/2);scale=(.2,y1-y0,hi-lo)
        o=obj(f'W{i:03}_{suffix}','GEO_Walls',pos,scale)
        s['objects'].append(o)
    cursor=s['lo']
    for k,o in enumerate(sorted(s['openings'],key=lambda t:t['c']),1):
        lo,hi=o['c']-o['w']/2,o['c']+o['w']/2
        assert lo>=cursor
        segment(f'Solid{k:02}',cursor,lo,0,3)
        segment(o['name']+'_Header',lo,hi,2.2,3)
        if o['kind']=='window':segment(o['name']+'_Sill',lo,hi,0,1)
        cursor=hi
    segment('SolidEnd',cursor,s['hi'],0,3)

# Physical placeholders. Open room doors stay disabled; two exits remain closed.
for d in doors:
    vertical=d['axis']=='V'; y=1.075
    pos=(d['line'],y,d['c']) if vertical else (d['c'],y,d['line'])
    scale=(.06,2.15,d['w']-.08) if vertical else (d['w']-.08,2.15,.06)
    closed=d['name'] in ('D_MainEntrance','D_Elevator')
    d['leaf']=obj(d['name']+'_Leaf','GEO_Doors',pos,scale,d['note'],[] if closed else ['Disable GameObject after creation'])
for w in windows:
    vertical=w['axis']=='V'
    pos=(w['line'],1.6,w['c']) if vertical else (w['c'],1.6,w['line'])
    scale=(.08,1.2,w['w']) if vertical else (w['w'],1.2,.08)
    w['pane']=obj(w['name']+'_CollisionPane','GEO_Windows',pos,scale,'Invisible collision pane: an opening that admits a view but does not let characters leave the building.',['Disable Mesh Renderer only; keep GameObject and Box Collider enabled'])
    scale=(.24,1.2,.06) if vertical else (.06,1.2,.24)
    w['mullion']=obj(w['name']+'_Mullion','GEO_Windows',pos,scale,'Plain central window divider. Sill and header were already built with the wall.')

def fmt(n):return f'{n:.4f}'.rstrip('0').rstrip('.') if n else '0'
def transform(pos,scale):
    return '```text\nPosition\n'+''.join(f'{a} = {fmt(v)}\n' for a,v in zip('XYZ',pos))+'\nRotation\nX = 0\nY = 0\nZ = 0\n\nScale\n'+''.join(f'{a} = {fmt(v)}\n' for a,v in zip('XYZ',scale))+'```\n'

text=[]
def add(s=''):text.append(s)
step=0
def heading(title):
    global step
    step+=1;add(f'\n## Step {step} — {title}\n')
def cube(o, action='Create', extra=''):
    heading(o['name'])
    if action=='Edit':add(f'Select the existing **{o["name"]}** in the Hierarchy. If building from an empty scene instead, create a Cube with this name. Do not create a second copy.\n')
    else:add('Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.\n')
    add(f'Parent:\n```text\nMainHospital\n└── {o["parent"]}\n```\n\nRename the Cube:\n```text\n{o["name"]}\n```\n\nIn **Inspector > Transform**, enter:\n')
    add(transform(o['pos'],o['scale']))
    if o['info']:add(o['info']+'\n')
    if extra:add(extra+'\n')
    for flag in o['flags']:add('**'+flag+'.**\n')
    add('Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.\n')

add('''# Haunted Hospital — complete beginner’s skeleton-building guide

**For Unity 6000.6.2f1 • one playable floor • metres • manual Cube construction**

Build this guide one numbered step at a time. It includes the entire sequence so you can work at your own pace; you do not need to ask for “next” after each step.

## What this guide creates

A plain architectural blockout: floor slabs, the hospital’s rooms and connected corridors, a central-right elevator enclosure, a closed staircase footprint, walls, real door and window openings, simple door placeholders, window collision panes, optional ceilings, and instructions for baking/testing an initial NavMesh. No furniture, clue objects, fixed key locations, item spawns, ghost/thief behavior, horror effects, materials or final lighting.

Your later game loop is **find four digits → enter the code → a key spawns in an accessible random location → take it to unlock the elevator → escape**. The image’s numbered circles and key icon are annotations, not objects to build. The elevator is the exit; no playable upper floor is needed.

## What was checked in your project

The saved scene `Assets/Scenes/Main/MainHospital.unity` contains the seven floor pieces and identity-transform containers from your message. `F_RightLowerCorridor` is absent. `ProjectSettings/ProjectVersion.txt` specifies **6000.6.2f1**. `Packages/manifest.json` includes **AI Navigation 2.0.14** and URP. The saved hospital scene contains no NavMesh Surface, Agent, Obstacle, Link or Modifier components. Its serialized NavMeshSettings are scene settings, not evidence of a baked surface. The current project’s default agent has radius 0.5 m and height 2 m.

This is a read of saved project files, not a verification of unsaved edits in the open Editor. The instructions below do not mean the hospital or its NavMesh has already been built or tested in Unity.

The accompanying coordinate schedule was checked for overlapping floor areas, doorway-to-room adjacency and 2D connectivity with a conservative 0.35 m clearance radius on a 0.05 m grid. Every accessible floor-piece centre and both sides of each open interior doorway were reachable from the entrance in that geometric check. This is not a substitute for the Unity bake and movement tests near the end of the guide.

## Important: which blueprint measurements this guide follows

You chose **“Match the drawing’s proportions.”** Accordingly, the coordinates below are a **manually traced, grid-snapped interpretation of the picture**, not a reconstruction of its conflicting printed metre dimensions. Room positions, the stepped outline, major connections and central-right elevator are preserved. Wall details hidden by symbols and small service pockets are simplified for a Cube blockout. Door/window placements are approximate readings of the symbols, not survey measurements.

The room footprint runs from **X = -16.75 to 16** and **Z = 0 to 42**: about **32.75 × 42 m before the outside wall thickness**. Wall faces extend 0.1 m beyond exterior boundary lines. This differs from the image’s printed 28 × 42 m because fitting both those numbers would squash the drawing horizontally. The traced main corridor varies in width; it does not universally retain the printed 3 m label. The 0.25 m construction grid is intentionally approximate. Exact numbers below make the pieces meet each other; they do not make the source image dimensionally exact.

These are **floor boundary / wall-centreline dimensions**, not clear internal room sizes. A 0.2 m wall centred on a boundary occupies 0.1 m on each side. Typical clear single openings are 1.2 m in this traced blockout, the Operating opening is 1.5 m, and the entrance/elevator openings are 1.8 m. Door height is 2.2 m; windows open from Y = 1 to 2.2 m. Heights are blockout choices; the top-view image cannot establish them. Walls are 3 m high.

The original seven coordinates used the printed sizes, so **do not combine them with these traced coordinates**. First save a separate scene, then replace their transforms in the seven edit steps. The original elevator left only 0.5 m between its south edge and Recovery’s north edge. Here a distinct 2 m-deep lobby serves the elevator, with 1.8 m clear depth between opposing 0.2 m walls. The narrow strip east of the shaft is enclosed service space, not another corridor.

## Contents

1. Save a separate scene and learn the controls.
2. Correct the existing seven floor pieces and finish all floors.
3. Check the full floor layout before building walls.
4. Build wall segments with door and window openings already left empty.
5. Add plain door placeholders and window collision panes/dividers.
6. Add ceilings after inspection, then bake and validate navigation.
7. Give the team the scene, navigation data and world conventions.

![Coordinate plan for this guide](Hospital_Skeleton_Coordinate_Plan.png)

The plan above is the coordinate interpretation used by this guide. Compare it with your supplied blueprint before investing time in walls. It is not a replacement source blueprint.
''')

heading('Open and save a separate blockout scene')
add('''1. Open this existing project in Unity Hub using **6000.6.2f1**. Do not create a different project.
2. In Unity, make sure the **Play** triangle at the top is not active. Click it to exit Play mode if necessary. Edits made during Play mode may be lost when you stop.
3. In the bottom **Project** window, open **Assets > Scenes > Main**.
4. Double-click **MainHospital**. If asked about unrelated unsaved work, save that work before changing scenes.
5. Choose **File > Save As…**. Save beside it as **Hospital_Blockout.unity**.
6. Check the open scene’s name in the Hierarchy. Work in **Hospital_Blockout**, keeping **MainHospital.unity** as the original.
7. Press **Ctrl + S** regularly. This saves the scene; it does not build a game executable.

If starting with an empty scene instead, use this same saved scene name and create the seven floor objects when their steps say “select the existing object.” Keep the default Main Camera and Directional Light for now; neither needs repositioning to construct geometry.
''')
heading('Find the three windows you will use')
add('''- **Hierarchy:** the objects in this open scene, usually at the left. Select and rename objects here.
- **Scene:** the editor’s working view. Build here; the Game tab is the camera output and may currently look empty.
- **Inspector:** properties of the selected object, usually at the right. **Transform** contains Position, Rotation and Scale.
- **Project:** files and folders, usually at the bottom. A scene object in Hierarchy is different from an asset in Project.

If a panel is missing, use **Window > General > Hierarchy / Inspector / Scene / Project**. An Inspector padlock can pin the wrong object; unlock it if selecting a new object does not update its properties. Select one object at a time when entering coordinates. Unity has no Apply button for ordinary scene Transform edits: type the number and press Enter or Tab.
''')
heading('Set a top-down working view')
add('''1. Click the **Scene** tab.
2. Keep **2D mode off**. This is a 3D project viewed from above; 2D mode is not the same as Top view.
3. Find the little axis gizmo at the upper-right of the Scene view. Click its **positive Y** direction to look down from above.
4. Toggle the centre cube / perspective label if necessary until the view is orthographic (**Top / Iso**, not perspective).
5. Select **MainHospital** and press **F** while the pointer is over the Scene view to frame the selection.
6. Use the mouse wheel to zoom. Middle-mouse drag pans. Avoid dragging object handles while checking the layout.
7. North/up on the plan is **+Z**, east/right is **+X**, and height is **Y**. Check the axis gizmo if the view is rotated.

If the scene is too dark, use the Scene view lighting toggle to use the editor’s preview illumination. This does not add or bake game lights.
''')
heading('Prepare the parent objects')
add('''Your existing containers can be reused. Expand **MainHospital** using its small triangle. If one is missing, right-click the parent and choose **Create Empty**, then press **F2** to rename it.

```text
MainHospital
├── GEO_Floors
├── GEO_Walls
├── GEO_Ceilings
├── GEO_Doors
├── GEO_Windows       ← add this empty container
├── GameplayMarkers  ← leave empty
├── Props            ← leave empty
├── Lights
└── Navigation
```

Select **MainHospital**, then each container individually. Set **Position (0, 0, 0)**, **Rotation (0, 0, 0)**, **Scale (1, 1, 1)**. Do not scale a container to resize the hospital. Every coordinate in this guide relies on identity-transform parents.

Do not create duplicate containers. Do not move the existing floor cubes out of GEO_Floors. Do not recreate the deleted F_RightLowerCorridor. If your open scene differs from the saved state, use the object names and these final transforms to reconcile it before proceeding.
''')
heading('Learn the Cube procedure before entering the first floor')
add('''For every **new** Cube in this guide:

1. Right-click its specified parent **in the Hierarchy**.
2. Select **3D Object > Cube**. If it appears at the scene root, drag its Hierarchy row onto the named parent. Do this **before** entering the numbers.
3. Press **F2**, type the exact new name and press Enter.
4. In **Inspector > Transform**, expand the component if necessary.
5. Enter all three **Position** fields, all three **Rotation** fields and all three **Scale** fields.
6. Confirm the parent’s Transform is identity. The Inspector values are local to the parent; with identity parents they also match the intended world coordinates.
7. Leave the Cube’s **Box Collider** enabled and **Is Trigger** off. Do not add a Rigidbody.
8. Press **Ctrl + S**.

For an **existing** Cube, select it and replace its Transform; do not duplicate it. Use Inspector values, not dragging or visual snapping. A standard Cube is 1 m on every side, so its Scale is its resulting size in metres when its parents have unit scale.

All floors use Position Y = -0.1 and Scale Y = 0.2, placing their upper surface at Y = 0. The following arithmetic is used throughout:

```text
Position X = (west edge + east edge) / 2
Scale X    = east edge - west edge
Position Z = (south edge + north edge) / 2
Scale Z    = north edge - south edge
```

The ASCII diagrams are orientation aids, not scale drawings. The edge numbers are the actual checks.
''')
add('\n# Part 1 — Floors only\n')
for i,f in enumerate(floors):
    a,b,c,d=f['b'];room=f['room']
    n=owner((a+b)/2,d+.01);s=owner((a+b)/2,c-.01);w=owner(a-.01,(c+d)/2);e=owner(b+.01,(c+d)/2)
    sketch=f'```text\n                         +Z / NORTH\n                         {n}\n                            |\n{w} <-- [ THIS PIECE ] --> {e}\n                            |\n                         {s}\n                         -Z / SOUTH\n```'
    extra=f'**Edge check:** X = {fmt(a)} to {fmt(b)}; Z = {fmt(c)} to {fmt(d)}.\n\n{sketch}\n\nNeighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.'
    cube(f['object'],'Edit' if i<7 else 'Create',extra)

heading('Stop and check the entire floor plan')
add('''Do this before making the first wall. Do not skip this checkpoint merely because all coordinates have been typed.

1. Select MainHospital and press F in Top view. Compare against both the source image and the coordinate plan at the beginning of this guide.
2. Confirm all slabs are flat at Y = 0 on top. If one looks raised, check Position Y and Scale Y.
3. Confirm the entrance is south; Post-op is southwest; Recovery/Waste are southeast; Elevator is central-right; Storage is above Staff; Pre-op is above Storage; Sterile Core is above Pre-op; ICU and Diagnostics are at the top-right.
4. Confirm Ward is west of Office, the west cross-corridor is above both, Anaesthesia is above that corridor, and Operating Room is above Anaesthesia.
5. Look for floor gaps by zooming into each seam. The shared boundaries in this guide are exact. A line in the editor grid is not necessarily a physical crack.
6. No two floor rectangles in the schedule overlap in area. Pieces of one room deliberately meet edge-to-edge.
7. Check the continuous route: entrance → main spine → upper corridor → north lobby. Check the west cross-corridor, lower west hall, right landing and Pre-op approach too.
8. Save. This is the point to review the footprint with your team before continuing into walls.

**Specific junctions:** Recovery ends at Z = 7 and elevator lobby begins at Z = 7. Elevator lobby ends at Z = 9 and Elevator begins at Z = 9. Elevator ends at Z = 14.75 and right landing begins there. Storage starts at X = 8.25 and its landing ends there. Ward ends at Z = 16.5 and west cross-corridor starts there. Anaesthesia starts at Z = 19.25 where the cross-corridor ends.

If the traced interpretation needs changing, change the shared boundary schedule and all dependent walls together. Do not silently substitute one printed room size into the middle of this coordinate set.
''')
add('\n# Part 2 — Walls with real openings\n')
heading('Understand how these walls are built')
add('''A Cube cannot have a doorway cut out just by putting a door model on top of it. Build separate wall pieces around empty openings.

All full-height wall cubes run from Y = 0 to 3: **Position Y = 1.5, Scale Y = 3**. All walls are 0.2 m thick. North/south walls use thickness in Z; east/west walls use thickness in X. Rotation stays zero for both orientations.

At a door, leave Y = 0 to 2.2 empty and place only a header from Y = 2.2 to 3. At a window, keep a sill from Y = 0 to 1, an opening from Y = 1 to 2.2, and a header above it.

```text
DOOR, front view             WINDOW, front view
 +--------header-------+     +--------header-------+  Y=3
 |                     |     |                     |
 +----+-----------+----+     +----+-----------+----+  Y=2.2
 |wall| EMPTY GAP |wall|     |wall| EMPTY GAP |wall|
 |    |           |    |     +----+---sill----+----+  Y=1
 +----+-----------+----+     +---------------------+  Y=0
```

The following wall runs cover each room boundary once. **Do not add a second wall on the other room’s side.** There are no walls between slabs that are parts of the same room or corridor. Run endpoints extend 0.1 m into neighbouring corner walls to close corners; this small intentional wall overlap is different from overlapping floor slabs. Opening edges do not get extended, preserving their clear width.

Build every listed segment. A window header without its sill leaves the wrong opening. A full wall substituted for split segments closes a planned doorway. Use the wall IDs on the coordinate-plan image to find the run.
''')
for i,s in enumerate(spans,1):
    add(f'\n### Wall run W{i:03} — {s["pair"][0]} / {s["pair"][1]}\n')
    axis='X' if s['axis']=='H' else 'Z'
    fixed='Z' if s['axis']=='H' else 'X'
    add(f'Boundary: **{fixed} = {fmt(s["line"])}**, from **{axis} = {fmt(s["lo"])} to {fmt(s["hi"])}**.\n')
    if s['openings']:
        for o in sorted(s['openings'],key=lambda o:o['c']):add(f'- Leave **{o["name"]}** clear from {axis} = **{fmt(o["c"]-o["w"]/2)} to {fmt(o["c"]+o["w"]/2)}**; width **{fmt(o["w"])} m**. '+('Door opening starts at the floor.' if o['kind']=='door' else 'Window opening starts 1 m above the floor.'))
        add('')
    else:add('This run has no opening.\n')
    for o in s['objects']:cube(o)

heading('Check the walls before adding placeholders')
add('''1. Switch from Top to a perspective view by dragging/orbiting the Scene view with Alt + left-mouse drag. Select a room and press F if necessary.
2. Confirm walls start on the floors, rise to Y = 3, and meet at corners. Wall tops are not ceilings.
3. Look through every intended doorway: there must be empty space from Y = 0 to 2.2. Headers above doors are expected.
4. Check there is no accidental wall across the main spine, the right landing, the corridor bends or the seams inside Operating/ICU/Sterile Core.
5. Windows must have a solid lower wall, a visible gap above it, and a header. The outside is currently empty; sky through a window is normal.
6. Confirm the elevator has one south opening into the lobby. Its west wall against the main corridor stays solid.
7. Confirm the stair footprint and enclosed service shafts are sealed. They are not playable rooms in this single-floor prototype.
8. Save before continuing.
''')
add('\n# Part 3 — Door placeholders and windows\n')
heading('Choose the initial door state')
add('''The architectural doorway and the door leaf are different things. The walls now contain the doorway. The next steps create a simple Cube leaf as a placeholder.

**Ordinary interior doors:** create and position each leaf, then uncheck the checkbox beside its name at the very top of the Inspector. This disables the whole GameObject, including its collider, leaving the doorway open for the first NavMesh. Disabling only Mesh Renderer would leave an invisible blocking collider and is wrong for an open door.

**Main entrance and elevator:** keep these two leaves active and their colliders enabled. They are closed prototype barriers. This prevents the entrance being an alternative escape and keeps the elevator locked for the initial world handoff. These are static placeholders; no keypad, animation, key requirement or automatic opening is implemented by the instructions.

Ordinary leaf bottoms are 0 m and tops 2.15 m, below the 2.2 m headers. Thin leaves sit on the wall centreline. Their unused width is 0.04 m clearance at each jamb. Frames are not added inside these clear openings.
''')
for d in doors:cube(d['leaf'])
heading('Understand the window collision setup')
add('''For each window, the wall already supplies its sill, header and side jambs. Add the two listed Cubes: an invisible collision pane and a visible centre divider.

For a **CollisionPane**, find the **Mesh Renderer** component in the Inspector and uncheck its component checkbox. **Do not** uncheck the object’s top-level checkbox. Leave its **Box Collider** enabled and **Is Trigger** off. You can now see through the window but a character cannot walk/jump through it. This is a collision-only placeholder, not finished glass.

For a **Mullion**, leave Mesh Renderer enabled. It is the small vertical bar in the middle. Its standard grey surface is sufficient. No transparent material, texture or lighting setup is needed.
''')
for w in windows:
    add(f'\n### {w["name"]} — on wall run W{w["span"]:03}\n')
    cube(w['pane']);cube(w['mullion'])

add('\n# Part 4 — Ceilings and navigation\n')
heading('Inspect before adding ceilings')
add('''Ceilings hide the layout from a normal Top view, so leave them until floors, walls and openings pass the checks. You can bake an initial NavMesh without ceilings while making corrections. If you do add them now, use the following repeatable method; it covers irregular rooms without drawing one giant rectangular roof over exterior recesses.

1. Select **GEO_Ceilings**. Keep its Transform at position/rotation zero and scale one.
2. For each floor Cube listed in Part 1, select only that Cube and press **Ctrl + D**.
3. Drag the duplicate onto **GEO_Ceilings** in the Hierarchy, then rename its `F_` prefix to `C_` and remove Unity’s `(1)` suffix. Example: `F_RecoveryRoom` becomes `C_RecoveryRoom`.
4. Keep its Position X/Z, Rotation, and Scale X/Z exactly the same as the floor.
5. Set **Position Y = 3.1**, **Scale Y = 0.2**. The ceiling underside is now at Y = 3, meeting the wall tops.
6. Leave Box Collider enabled, Is Trigger off, and add no Rigidbody.
7. Repeat for every floor slab, including the multi-piece corridors/rooms and sealed stair/shaft footprints.
8. Check that the originals remain under GEO_Floors with Y = -0.1. Accidentally moving the original instead of a duplicate removes the walkable floor.
9. Use the Hierarchy’s **Scene visibility eye** on GEO_Ceilings to hide ceilings while editing. If its visibility controls are not shown, hover to the left of the object row. The eye affects editor visibility; it is not the object’s active checkbox. Restore visibility for the final visual check.
10. Save.

You should have the same number of ceiling slabs as floor slabs: **40**. Optional ceilings use the identical X/Z positions and sizes already given per floor, so no new floor-plan arithmetic is needed.
''')
heading('Verify the installed AI Navigation package')
add('''1. Choose **Window > Package Management > Package Manager**.
2. Set the package list to **In Project** and select **AI Navigation**.
3. This project already specifies **2.0.14**. Let Unity finish importing/resolving if it is still busy. There is no need to upgrade it for this guide.
4. If it is unexpectedly missing in a separate copy of the project, choose **Unity Registry**, search **AI Navigation**, then install the version offered as compatible with your Editor. Do not install the old GitHub NavMeshComponents package on top of this one.
5. Open **Window > General > Console**. Resolve red compile/import errors before relying on the Bake button.

The package’s local documentation confirms the NavMesh Surface, Modifier and Navigation window controls used below. This guide uses the current component-based bake, not a legacy Navigation window “Bake” tab.
''')
heading('Create a blockout agent type')
add('''1. Open **Window > AI > Navigation** and select the **Agents** tab.
2. Click **+** to add an agent type; name it **Hospital_Blockout**. Do not overwrite an existing teammate’s agent type.
3. Set **Radius = 0.3**, **Height = 1.8**, **Step Height = 0.2**, **Max Slope = 45**.
4. Set **Drop Height = 0** and **Jump Distance = 0** where the generated-link settings are displayed.
5. These are provisional human-sized navigation settings for the skeleton. Tell your teammates the chosen type and measurements. Their NavMeshAgent components must use this type to walk on this bake. A larger creature will need a suitable agent type, its own bake and clearance checks.

Why these settings: a 0.3 m radius means a 0.6 m diameter body fits comfortably within the 1.2 m doorways. The project’s existing default 0.5 m radius is wider. The bake radius, rather than only changing a runtime Agent radius, determines how far the navigation surface is kept from walls.
''')
heading('Add the NavMesh Surface to the correct parent')
add('''1. Select **MainHospital** in the Hierarchy — not the empty Navigation container and not GEO_Floors alone.
2. Click **Add Component** in the Inspector, search **NavMesh Surface**, and add it.
3. Set **Agent Type = Hospital_Blockout**.
4. Set **Default Area = Walkable**.
5. Set **Use Geometry = Physics Colliders**. This uses the enabled Box Colliders of floors, walls, panes and the two closed door placeholders.
6. Expand **Object Collection**. Set **Collect Objects = Current Object Hierarchy** and **Include Layers = Everything** for this geometry-only prototype.
7. Leave **Generate Links** off. There are no jumps, second-floor stairs or teleports in this blockout.
8. Expand **Advanced**. Enable **Override Voxel Size** and set **Voxel Size = 0.05**. Leave tile size at its default. Use **Minimum Region Area = 2** initially and leave **Build Height Mesh** off.

The Surface is on MainHospital so its current hierarchy includes both floors and blocking walls. Putting it on the empty Navigation child with “Current Object Hierarchy” would collect no hospital geometry. Putting it on GEO_Floors alone would omit walls and could generate paths through them.
''')
heading('Keep blocking geometry solid but not walkable on top')
add('''Do the following on each of **GEO_Walls**, **GEO_Doors**, **GEO_Windows**, and **GEO_Ceilings**:

1. Select the container.
2. Choose **Add Component > NavMesh Modifier**.
3. Set **Mode = Add or Modify Object**.
4. Enable **Apply To Children**.
5. Set **Affected Agents = All**.
6. Enable **Override Area**, then choose **Not Walkable**.

This keeps their collision geometry in the bake as blockers, while preventing walkable patches on their tops. **Do not choose Remove Object** for walls: that would make the bake ignore them. Do not mark GEO_Floors or MainHospital Not Walkable.

Next, add the same Modifier settings directly to these five floor Cubes, which are intentionally inaccessible in this version:

```text
F_Elevator
F_ElevatorServiceStrip
F_StairFootprint
F_OperatingServiceShaft
F_NorthServiceAlcove
```

The Washroom alcove is an accessible room and should stay Walkable. The elevator floor is excluded for this initial **closed-elevator** bake. When the team implements unlocking, they must plan the navigation update too; simply removing a closed door after baking does not generate the missing walkable surface.
''')
heading('Bake the first NavMesh')
add('''1. Confirm all ordinary interior door leaves are **inactive**, while D_MainEntrance_Leaf and D_Elevator_Leaf are **active**.
2. Confirm all floors, walls and window collision panes are active. Mesh Renderer can be off on the window panes, but their Box Colliders must be on.
3. Select **MainHospital** and click **Bake** on its NavMesh Surface component. Wait for the operation to finish.
4. Check the **NavMesh Data** field now references an asset instead of None.
5. Show the blue navigation overlay in the Scene view. If hidden, keep the Surface selected, open the Scene view **Overlays** menu, enable the **AI Navigation** overlay and enable its NavMesh display. Hide ceilings with the Scene visibility eye if they obscure it.
6. Press **Ctrl + S**. The generated NavMesh asset and its `.meta` file are part of the handoff, not just a temporary blue drawing.

The blue area should cover accessible floors and continue through all open doorways. It should stay inset from walls, have no walkable wall/ceiling tops, and exclude the locked elevator interior and closed stair/service enclosures. An isolated blue island is not proof a room can be reached from the entrance.
''')
heading('Inspect every route, not only the main corridor')
add('''Use Top view with ceilings hidden. Follow the connected blue surface across each route below. A narrow dark gap across a doorway can mean the room is disconnected.

| Start | Destination | Expected route |
|---|---|---|
| Inside main entrance | Post-op | Main spine → west lower hall → north Post-op doorway |
| Main entrance | Recovery | Main spine → elevator lobby → Recovery north doorway |
| Elevator lobby | Waste | Staff west doorway → Staff Room → Waste north doorway |
| Main spine | Storage | Right landing above Elevator → Storage west doorway |
| Main spine | Pre-op | Right landing → passage east of stairs → Pre-op west doorway |
| Main spine | General Ward | West cross-corridor → Ward north doorway, or lower west hall → Ward east doorway |
| Main spine | Nurse Office | West cross-corridor → Office north doorway, or lower west hall → south doorway |
| Main spine | Anaesthesia | Operating approach → Anaesthesia east doorway |
| Main spine | Operating Room | Operating approach → door recess → Operating south doorway |
| Operating approach | Washroom alcove | Washroom north doorway |
| Main spine | Sterile Core | Sterile west doorway |
| Main spine | ICU | North lobby → ICU west doorway, or Sterile Core → ICU south doorway |
| ICU | Diagnostics | Diagnostics west doorway |
| North lobby | WC / Staff utility | Their south doorways |
| Elevator lobby | Elevator cabin | **Blocked in this first bake** |
| Any corridor | Stair enclosure / service shafts | **Blocked** |

Also inspect the main spine where its width changes beside the Office and near ICU. Corridors that only meet at a point are not usable routes; this schedule gives shared edges, not point-only connections.
''')
heading('Run an actual path check with a temporary navigation probe')
add('''A blue overlay alone is not a complete movement test. Use the small disposable probe below to confirm paths in Play mode. It is test tooling, not ghost, thief or gameplay logic. If a teammate already has a working navigation tester, they can perform the equivalent route checks instead.

1. In Project, make **Assets > Tests > Manual** (right-click a folder > Create > Folder for each missing folder).
2. Inside Manual, choose **Create > Scripting > MonoBehaviour Script** (or **Create > C# Script**, depending on menu presentation). Name it **HospitalNavProbe**.
3. Double-click it. Replace its contents with the complete code below and save. Return to Unity and wait for compilation.
4. Create an empty GameObject under **Navigation** named **TEST_NavTarget**, with Position **(-8, 0, 3)**, Rotation zero and Scale one. This is only a test destination in Post-op, not an item spawn.
5. Create another empty under Navigation named **TEST_NavProbeRoot**, Position **(0, 0, 2)**, Rotation zero and Scale one. Add **NavMesh Agent** and set **Agent Type = Hospital_Blockout**, **Radius = 0.3**, **Height = 1.8**, **Base Offset = 0**, **Speed = 2**, **Stopping Distance = 0.1**.
6. Add **HospitalNavProbe** to TEST_NavProbeRoot. Drag TEST_NavTarget from Hierarchy to the script’s **Target** field.
7. Create a **Capsule** as a child of TEST_NavProbeRoot named **TEST_Visual**. Set its local Position **(0, 0.9, 0)**, Rotation zero, Scale **(0.6, 0.9, 0.6)**. Remove its Capsule Collider using the component menu > Remove Component. The capsule is only a visible body; the root’s feet stay at Y = 0.
8. Enter Play mode. The Console should report **PathComplete**, and the capsule should move through the hall and doorway into Post-op without crossing walls.
9. Stop Play mode. Move TEST_NavTarget to each coordinate in the next table; enter Play mode again for each test. Do not bake with the test objects present as geometry. The root has a NavMeshAgent and the visible child has no collider.
10. For the moving-agent team’s acceptance, repeat representative routes in reverse and from room to room, including ICU → Diagnostics and Staff → Waste. Check both path result and visible wall clearance.

```csharp
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HospitalNavProbe : MonoBehaviour
{
    public Transform target;

    void Start()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (target == null || !agent.isOnNavMesh)
        {
            Debug.LogError("Assign a target and place the probe on the matching baked NavMesh.", this);
            return;
        }

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        if (!NavMesh.SamplePosition(target.position, out NavMeshHit hit, 0.25f, filter))
        {
            Debug.LogWarning("Target is not on reachable-type navigation geometry within 0.25 m.", target);
            return;
        }

        NavMeshPath path = new NavMeshPath();
        bool found = agent.CalculatePath(hit.position, path);
        Debug.Log("Navigation result: " + path.status, this);
        if (found && path.status == NavMeshPathStatus.PathComplete)
            agent.SetPath(path);
        else
            Debug.LogWarning("No complete route: check the doorway, blockers and bake.", this);
    }
}
```

The 0.25 m sampling radius is deliberately small. A very large search radius can snap a target through a wall into a different room and produce a misleading success. Each target below is well inside its room, at floor height. Stop Play mode before editing test positions so changes persist.

| Target room | Position X | Position Y | Position Z | Expected |
|---|---:|---:|---:|---|
| Post-op | -8 | 0 | 3 | Complete |
| Recovery | 6 | 0 | 3 | Complete |
| Waste | 13 | 0 | 3 | Complete |
| Staff room | 12 | 0 | 10 | Complete |
| Storage | 12 | 0 | 15 | Complete |
| Pre-op | 12 | 0 | 21 | Complete |
| General Ward | -11 | 0 | 12 | Complete |
| Nurse Office | -4 | 0 | 12 | Complete |
| Anaesthesia | -11 | 0 | 22 | Complete |
| Washroom alcove | -4 | 0 | 22 | Complete |
| Operating Room | -10 | 0 | 31 | Complete |
| Sterile Core | 10 | 0 | 28 | Complete |
| ICU | 6 | 0 | 35 | Complete |
| Diagnostics | 12 | 0 | 35 | Complete |
| WC | -2.5 | 0 | 40 | Complete |
| Staff utility | 1.5 | 0 | 40 | Complete |
| Elevator interior | 5 | 0 | 12 | No sampled NavMesh / no complete route |
| Stair enclosure | 4 | 0 | 20 | No sampled NavMesh / no complete route |

These are starting test instructions; this guide has not executed the probe in your Editor. Record actual pass/fail results after constructing and baking the scene.
''')
heading('Fix common navigation failures')
add('''| Symptom | What to check, in order |
|---|---|
| No blue floor anywhere | Package imported; no Console errors; Surface on MainHospital; Current Object Hierarchy selected; geometry active; colliders enabled; selected agent type correct; click Bake. |
| Blue floor ignores walls | Surface must collect walls as well as floors. Use Physics Colliders. Walls must have Box Colliders and Is Trigger off. Modifier must be Add or Modify Object, not Remove Object. Re-bake. |
| Doorway disconnected | Interior leaf must be inactive, not merely invisible. Check no full wall crosses the opening, door clear width, agent type radius, header bottom at 2.2 and voxel size 0.05. Re-bake. |
| Some corridors missing | Check floor transforms and whether a Not Walkable Modifier was accidentally placed on MainHospital/GEO_Floors. Check clear width after subtracting wall thickness. |
| Agent says it is not on NavMesh | Match Hospital_Blockout on Surface and Agent. Put root at (0,0,2); visual capsule alone is raised. Ensure bake exists and Surface/MainHospital are active. |
| PathPartial / PathInvalid | Check the failed route from the route table, disabled door leaves, a shifted slab, a stray collider, or a Not Walkable modifier on the wrong object. |
| Blue on roofs or walls | Apply the Not Walkable Modifier to ceilings/walls with Apply To Children enabled; keep geometry included. Re-bake. |
| Agent clips wall corners | Check Box Collider sizes against meshes; do not shrink radius just to hide a broken doorway. Agree on the actual character size with the agent developer. |
| Invisible wall in a doorway | Look for an enabled leaf whose Mesh Renderer alone was switched off, or an extra old wall cube. |
| Window can be walked through | Its CollisionPane GameObject and Box Collider must remain enabled; only its Mesh Renderer should be off. |
| Door was opened but path remains blocked | Initial bake included the closed leaf. Re-bake for the test, or have the team implement the agreed runtime obstacle/bake strategy. |

Do not add NavMesh Links to conceal accidental floor gaps or door-bake failures. This is a continuous ground floor; normal doorways should have continuous navigation.
''')
heading('Remove the temporary probe and save a clean handoff')
add('''1. Stop Play mode.
2. Delete only **TEST_NavProbeRoot** (with its visual child) and **TEST_NavTarget** from the scene. The optional probe script can stay in Tests/Manual, or be removed if no longer needed.
3. Restore the intended initial door states: ordinary interior leaves inactive; entrance/elevator leaves active.
4. If any geometry or navigation settings changed during testing, select MainHospital and Bake again. Save the scene.
5. Restore ceiling visibility for a final perspective inspection, then hide it with the editor eye if that helps teammates inspect the layout.
6. The scene is ready for agent development only after the route tests pass, not just because every construction step has been entered.
''')
add('\n# Part 5 — Team handoff and later work\n')
heading('Give your teammates the world contract')
add('''Send the team the saved scene and these facts:

```text
Scene: Assets/Scenes/Main/Hospital_Blockout.unity
Unity: 6000.6.2f1
AI Navigation: 2.0.14
Units: 1 Unity unit = 1 metre
Walkable floor height: Y = 0
North: +Z; east: +X
Walls: 0.2 m thick; 3 m high
Door clear height: 2.2 m
Typical door clear width: 1.2 m
NavMesh type: Hospital_Blockout
Bake agent radius: 0.3 m; height: 1.8 m; step: 0.2 m
Initial state: interior door leaves inactive/open
Initial state: entrance and elevator active/closed
Excluded: elevator cabin, stair enclosure and sealed service shafts
No fixed clue, key, resource or enemy spawn positions have been created
```

Include **Hospital_Blockout.unity**, its `.meta`, the generated NavMesh data asset and its `.meta`, relevant project navigation settings (`ProjectSettings/NavMeshAreas.asset`), and any new assets/scripts actually used. Preserve existing version control conventions. Do not share Library/Temp as project source. Use the NavMesh Surface’s NavMesh Data field to locate the generated data asset instead of guessing its folder.

Do not move or scale MainHospital after integration without coordinating with teammates: their positions, paths and metadata will depend on the world coordinates.

| Agent from your document | What this skeleton provides | What comes later |
|---|---|---|
| Sound Hunter | Rooms, physical walls, open passages and validated NavMesh | Sound events, hearing/perception, states, search/chase behavior, spawn location |
| Resource Thief | Connected rooms and navigation routes | Item registry, utility/risk logic, agreed stash and spawn locations |
| Environment / Horror Director | Stable room names and future door locations | Event points, actual lights/audio, scare logic |
| Item Spawn Director | Accessible room areas and a basis for reachability checks | Candidate spawn points, CSP data, resource rules and randomized placements |

Your attachment describes design requirements, not executable instructions for this blockout. Examples in it such as a basement, pharmacy, fuse, ammo or batteries do not add rooms/items to your blueprint. Your current resource list is health, armour and sanity pills. A baked NavMesh also does not by itself implement or demonstrate the custom A*, risk-aware search, utility AI or CSP requested for the agents.

**Later elevator integration:** keep the cabin geometry. Agree whether AI ever enters it. If it should become navigable after unlocking, remove its Not Walkable exclusion and use an appropriate bake/runtime obstacle update strategy. For a dynamic door, a common later approach is to bake the doorway open, then use the closed door’s collider plus a carving obstacle; that is not the closed-door bake made in this guide. Never leave a permanently baked blocker and assume moving its mesh removes the baked obstruction.

**Later randomized item placement:** the key must not spawn in the locked elevator, sealed shafts or stair enclosure, nor behind the lock it opens. Candidate locations need complete-route validation, not only a nearby NavMesh sample. Do not treat the test target coordinates as a final item-placement design.

The two non-moving directors can already be developed with placeholder data. The hunter and thief developers can begin with this validated greybox; they do not need final textures, furniture or lighting.
''')
heading('Final completion checklist')
add('''- [ ] Working in Hospital_Blockout, with original MainHospital scene preserved.
- [ ] All seven original floor transforms reconciled; no old corridor or duplicate floors left over.
- [ ] Forty floor slabs use the specified shared boundaries and all have top Y = 0.
- [ ] Room positions and connections checked against the source blueprint and the traced-plan image.
- [ ] Every wall run built once; same-room slab seams remain open.
- [ ] All doorway and window openings exist physically, not just visually.
- [ ] Ordinary interior doors inactive/open for initial navigation.
- [ ] Entrance and elevator placeholders active/closed.
- [ ] Window panes collide while their Mesh Renderers are off.
- [ ] Stair footprint retained but sealed; no upper floor.
- [ ] Optional ceilings meet wall tops, without replacing/removing floors.
- [ ] Hospital_Blockout agent type and NavMesh Surface configured and baked.
- [ ] No walkable roofs, wall tops, locked cabin or service-shaft islands.
- [ ] Actual complete-path and movement tests passed for every accessible room.
- [ ] Temporary probe objects removed; clean scene and NavMesh data saved.
- [ ] Team informed of dimensions, coordinates, agent size and door state.
- [ ] No fixed digit/key/resource spawns or gameplay agents added in this pass.

## Reference notes

- Main architectural reference: the hospital blueprint you supplied in this conversation.
- Agent context: `haunted_hospital_four_is_agents.md`, supplied by you. Its examples are not additions to the floor plan.
- Unity’s [Scene view navigation](https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/editor-windows-views-reference/using-the-scene-view/scene-view-navigation) documents the editor viewing controls.
- Unity’s [AI Navigation package documentation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html) covers the component-based navigation workflow. Controls in this guide were also checked against the installed 2.0.14 package’s `Documentation~/NavMeshSurface.md`, `NavigationWindow.md` and `NavMeshModifier.md`.
- The temporary test uses Unity’s documented [NavMesh.SamplePosition](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMesh.SamplePosition.html) and [NavMeshAgent.CalculatePath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMeshAgent.CalculatePath.html) APIs. Test the supplied script in your Editor as instructed; it has not been compiled or run there during preparation of this document.

## Coordinate schedule — quick lookup after learning the steps

This table is for checking work, not a substitute for the individual steps above. Floor values are world/local values under identity parents. All floors have Y = -0.1, thickness 0.2 and zero rotation.

| Floor | X position | Z position | X size | Z size | West/east edges | South/north edges |
|---|---:|---:|---:|---:|---|---|
''')
for f in floors:
    o=f['object'];a,b,c,d=f['b'];add(f'| {f["name"]} | {fmt(o["pos"][0])} | {fmt(o["pos"][2])} | {fmt(o["scale"][0])} | {fmt(o["scale"][2])} | {fmt(a)} / {fmt(b)} | {fmt(c)} / {fmt(d)} |')
add('\n### Door opening lookup\n\nAll openings have bottom Y = 0 and top Y = 2.2. “Centre along run” means X for a horizontal wall and Z for a vertical wall.\n\n| Opening | Wall run | Direction | Fixed coordinate | Centre along run | Clear width | Connects |\n|---|---|---|---|---:|---:|---|')
for d in doors:add(f'| {d["name"]} | W{d["span"]:03} | {d["axis"]} | {"Z" if d["axis"]=="H" else "X"} = {fmt(d["line"])} | {fmt(d["c"])} | {fmt(d["w"])} | {d["a"]} / {d["b"]} |')

# Geometry validation: no floor overlaps, correctly attached openings, and room graph reachability.
graph=defaultdict(set)
for d in doors:
    if d['name'] not in ('D_MainEntrance','D_Elevator'):
        graph[d['a']].add(d['b']);graph[d['b']].add(d['a'])
seen={'Corridor'};q=deque(seen)
while q:
    for v in graph[q.popleft()]-seen:seen.add(v);q.append(v)
excluded={'Elevator','Service shaft','Stair enclosure','Operating service shaft','North service alcove'}
expected={f['room'] for f in floors}-excluded
assert expected<=seen, expected-seen
assert len(floors)==40
assert len({o['name'] for o in objects})==len(objects)
guide='\n'.join(text)
assert guide.count('```')%2==0
(OUT/'Hospital_Skeleton_Beginner_Guide.md').write_text(guide,encoding='utf-8')
(OUT/'hospital_skeleton_coordinate_schedule.json').write_text(json.dumps(dict(floors=[{k:v for k,v in f.items() if k!='object'} for f in floors],walls=spans,doors=doors,windows=windows,objects=objects),indent=2),encoding='utf-8')
print(json.dumps(dict(steps=step,floors=len(floors),wall_runs=len(spans),wall_cubes=sum(len(s['objects']) for s in spans),doors=len(doors),windows=len(windows),words=len(guide.split()),reachable_rooms=sorted(seen))))

# Draw a new engineering plan from the coordinates; source blueprint is not modified.
from PIL import Image,ImageDraw,ImageFont
S=27;W=1320;H=1430;left=110;top=105
im=Image.new('RGB',(W,H),'#f6f7f8');dr=ImageDraw.Draw(im)
fontpath='C:/Windows/Fonts/arial.ttf'
def font(n):return ImageFont.truetype(fontpath,n)
def pt(x,z):return (left+(x+16.85)*S,top+(42.1-z)*S)
colors={'Corridor':'#fff1c9','Operating Room':'#c4e4f3','Anaesthesia':'#d8d2ee','General Ward':'#f4e2b9','Nurse Office':'#c5dfed','ICU':'#cbe7cf','Sterile Core':'#cbe7cf','Diagnostics':'#f2cfcd','Post-op':'#f2cfcd','Recovery':'#c4e4f3','Storage':'#c4e4f3','Staff room':'#f2cfcd','Waste':'#d8d2ee','Pre-op':'#f4e2b9'}
dr.text((65,25),'HOSPITAL SKELETON — COORDINATE PLAN',font=font(28),fill='#152f40')
dr.text((65,64),'Drawing-proportion interpretation • 1 unit = 1 m • north = +Z • openings shown in blue',font=font(17),fill='#435260')
for f in floors:
    a,b,c,d=f['b'];dr.rectangle([pt(a,d),pt(b,c)],fill=colors.get(f['room'],'#dedfe2'))
for x in range(-16,17,2):
    p=pt(x,0);dr.text((p[0]-8,top+42.1*S+12),str(x),font=font(12),fill='#687078')
for z in range(0,43,2):
    p=pt(-16.85,z);dr.text((65,p[1]-7),str(z),font=font(12),fill='#687078')
for i,s in enumerate(spans,1):
    points=[pt(s['lo'],s['line']),pt(s['hi'],s['line'])] if s['axis']=='H' else [pt(s['line'],s['lo']),pt(s['line'],s['hi'])]
    dr.line(points,fill='#283c48',width=5)
    m=((points[0][0]+points[1][0])/2,(points[0][1]+points[1][1])/2)
    # small identifiers support locating detailed wall schedules
    dr.text((m[0]+4,m[1]+3),f'{i:03}',font=font(10),fill='#5f6060')
for o in doors+windows:
    lo,hi=o['c']-o['w']/2,o['c']+o['w']/2
    p=[pt(lo,o['line']),pt(hi,o['line'])] if o['axis']=='H' else [pt(o['line'],lo),pt(o['line'],hi)]
    dr.line(p,fill='#fcf8ed' if o['kind']=='door' else '#5baace',width=7)
    if o['kind']=='door':
        dr.line(p,fill='#167cad',width=2)
labels={
'Post-op':(-8.5,3.7),'Recovery':(6.6,3.7),'Waste':(13.25,3.7),'Staff room':(12.7,10),
'Storage':(12.1,15.2),'Elevator':(5.5,12),'General Ward':(-11.2,12),'Nurse Office':(-3.7,13),
'Pre-op':(12,21),'Stair enclosure':(4.3,20.5),'Anaesthesia':(-11.6,22.8),'Washroom alcove':(-4.1,21.8),
'Operating Room':(-10,31.4),'Sterile Core':(10.7,27.7),'ICU':(6,35.5),'Diagnostics':(12.5,35),
'WC':(-2.5,40),'Staff utility':(1.9,40)}
for name,(x,z) in labels.items():
    label=name.replace(' ','\n') if name in ('Stair enclosure','Washroom alcove','Staff utility') else name
    if name=='Operating Room':label='Operating\nRoom 1'
    if name=='Nurse Office':label='Nurse Station /\nOffice'
    p=pt(x,z);dr.multiline_text(p,label,font=font(16),fill='#183342',anchor='mm',align='center')
dr.text(pt(.5,21),'MAIN',font=font(16),fill='#63532c',anchor='mm')
dr.text(pt(.5,20),'CORRIDOR',font=font(12),fill='#63532c',anchor='mm')
dr.text(pt(-8,17.9),'WEST CROSS-CORRIDOR',font=font(13),fill='#63532c',anchor='mm')
dr.text(pt(5.9,8),'ELEVATOR LOBBY',font=font(10),fill='#63532c',anchor='mm')
dr.text((1070,160),'N / +Z',font=font(20),fill='#183342');dr.line([(1100,230),(1100,190)],fill='#183342',width=3);dr.polygon([(1100,180),(1093,195),(1107,195)],fill='#183342')
dr.multiline_text((1040,300),'Wall labels:\n001 = W001\nin the guide\n\nBlue lines:\ndoor / window\nopenings\n\nGrey pockets:\nclosed service\nor stair space',font=font(15),fill='#435260',spacing=6)
dr.text((80,1300),'Floor footprint: 32.75 × 42 m; exterior wall faces extend 0.1 m beyond boundary lines.',font=font(18),fill='#253c4a')
dr.text((80,1333),'Coordinates are snapped approximations of the drawing, not the printed room-size labels.',font=font(17),fill='#435260')
dr.text((80,1364),'No fixed key, digit, resource or agent spawns. Elevator and entrance start closed.',font=font(17),fill='#435260')
im.save(OUT/'Hospital_Skeleton_Coordinate_Plan.png')
