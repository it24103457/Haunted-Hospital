"""Check planned footprints against saved geometry; never edit Unity assets."""
import json, math, re
from pathlib import Path
HERE=Path(__file__).resolve().parent
plan=json.loads((HERE/'hospital_prop_placement_plan.json').read_text())
schedule=json.loads((HERE/'hospital_skeleton_coordinate_schedule.json').read_text(encoding='utf-8-sig'))
props=plan['props']
def box(p):
    w,h,d=plan['types'][p['type']]['size']
    if p['rotation'][1]%180: w,d=d,w
    x,y,z=p['position']
    return [x-w/2,x+w/2,z-d/2,z+d/2]
def overlap(a,b):
    return min(a[1],b[1])-max(a[0],b[0])>1e-5 and min(a[3],b[3])-max(a[2],b[2])>1e-5
errors=[]
for p in props:
    a=box(p); floors=[f['b'] for f in schedule['floors'] if f['room']==p['room']]
    for ix in range(11):
        for iz in range(11):
            x=a[0]+(a[1]-a[0])*ix/10; z=a[2]+(a[3]-a[2])*iz/10
            if not any(b[0]<=x<=b[1] and b[2]<=z<=b[3] for b in floors): errors.append('Outside room: '+p['id'])
    for door in schedule['doors']:
        half=door['w']/2+.25; c=door['c']; line=door['line']
        zone=[c-half,c+half,line-1.3,line+1.3] if door['axis']=='H' else [line-1.3,line+1.3,c-half,c+half]
        if overlap(a,zone): errors.append(f"Door clearance: {p['id']} / {door['name']}")
    for q in props:
        if p['id']<q['id'] and overlap(a,box(q)): errors.append(f"Footprint overlap: {p['id']} / {q['id']}")
scene=(HERE.parent/'Assets/Scenes/Main/MainHospital.unity').read_text(encoding='utf-8-sig')
names={}; transforms={}
for block in re.split(r'^--- !u!',scene,flags=re.M):
    head=block.splitlines()[0] if block else ''
    if head.startswith('1 &'):
        m=re.search(r'^  m_Name: (.*)$',block,re.M)
        if m: names[head.split('&')[1].split()[0]]=m.group(1).strip()
    if head.startswith('4 &'):
        go=re.search(r'm_GameObject: \{fileID: (\d+)\}',block)
        if go:
            def vec(k): return [float(x) for x in re.findall(r'[xyzw]: ([-\d.eE+]+)',re.search(k+r': (.*)',block).group(1))]
            transforms[go.group(1)]={'pos':vec('m_LocalPosition'),'scale':vec('m_LocalScale'),'rot':vec('m_LocalRotation')}
actual={names[k]:v for k,v in transforms.items() if k in names}
for f in schedule['floors']:
    b=f['b']; t=actual.get(f['name']); expect=[(b[0]+b[1])/2,-.1,(b[2]+b[3])/2]
    if not t or any(abs(a-b)>1e-4 for a,b in zip(t['pos'],expect)): errors.append('Floor position: '+f['name'])
    elif any(abs(a-b)>1e-4 for a,b in zip(t['scale'],[b[1]-b[0],.2,b[3]-b[2]])): errors.append('Floor size: '+f['name'])
for name,t in actual.items():
    if not name.startswith('W') or any(abs(v)>1e-5 for v in t['rot'][:3]): continue
    x,y,z=t['pos']; w,h,d=t['scale']; wall=[x-w/2,x+w/2,z-d/2,z+d/2]
    for p in props:
        ph=plan['types'][p['type']]['size'][1]; bottom=p['position'][1]-(ph/2 if p['type']=='KeyBox' else 0)
        if min(bottom+ph,y+h/2)>max(bottom,y-h/2)+1e-5 and overlap(box(p),wall): errors.append(f"Saved wall overlap: {p['id']} / {name}")
for pool,count,types in [('Supply',15,4),('Clue',10,3),('Key',4,1)]:
    rows=[p for p in props if p['pool']==pool]
    assert len(rows)==count and len({p['type'] for p in rows})==types
report={'props':len(props),'floor_count':40,'errors':sorted(set(errors)),'scope':'Normalized closed footprints, saved floor transforms, axis-aligned saved wall cubes and scheduled door clearance zones. Imported mesh fit, animation, player and NavMesh not tested.'}
(HERE/'Hospital_Prop_Placement_Checks.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
if not errors:
    from PIL import Image,ImageDraw,ImageFont
    im=Image.new('RGB',(1850,2380),'#fafbf9'); draw=ImageDraw.Draw(im)
    fontpath='C:/Windows/Fonts/arial.ttf'
    small=ImageFont.truetype(fontpath,21); label=ImageFont.truetype(fontpath,24); title=ImageFont.truetype(fontpath,38)
    def xy(x,z): return (round(110+(x+18)*45),round(220+(43-z)*45))
    def rect(b,fill,outline=None):
        draw.rectangle([xy(b[0],b[3]),xy(b[1],b[2])],fill=fill,outline=outline,width=2)
    draw.text((100,40),'Hospital furniture and random spawn candidates',fill='#263733',font=title)
    draw.text((100,105),'S: supplies   C: digit clues   K: key boxes   P: room furniture',fill='#263733',font=label)
    draw.text((100,145),'* Behind a planned barricade. Arrows show the front of each object.',fill='#263733',font=label)
    for f in schedule['floors']:
        rect(f['b'],'#eeeeeb' if f['room']=='Corridor' else '#e0e7e5','#a1adaa')
    for name,t in actual.items():
        if name.startswith('W') and abs(t['rot'][1])<1e-5:
            x,y,z=t['pos']; w,h,d=t['scale']
            if y-h/2<.5: rect([x-w/2,x+w/2,z-d/2,z+d/2],'#52605f')
    colors={'Supply':'#227aa6','Clue':'#946335','Key':'#aa3f79','':'#758782'}
    for p in props:
        col=colors[p['pool']];rect(box(p),col,'#263733')
        x,y,z=p['position'];yaw=math.radians(p['rotation'][1]); tip=xy(x+.65*math.sin(yaw),z+.65*math.cos(yaw))
        draw.line([xy(x,z),tip],fill=col,width=3);draw.ellipse((tip[0]-3,tip[1]-3,tip[0]+3,tip[1]+3),fill=col)
        q=xy(x,z); text=p['id']+('*' if p['gate'] else '');bbox=draw.textbbox((q[0]+8,q[1]-24),text,font=small)
        draw.rectangle(bbox,fill='white');draw.text((q[0]+8,q[1]-24),text,fill=col,font=small)
    labels=[('POST-OP',-8,4.8),('RECOVERY',6.2,4.3),('WASTE',12.7,3.4),('STAFF',12.1,9.7),('STORAGE',11.7,15.2),('ELEVATOR',5.5,12),('WARD',-11.4,14.5),('OFFICE',-4.4,14.4),('ANAESTHESIA',-11.7,24.8),('WASHROOM',-4.3,22.7),('PRE-OP',11.5,22.5),('OPERATING',-10.5,29),('STERILE CORE',10,27),('ICU',4.5,34.9),('DIAGNOSTICS',12.4,37.5),('W/C',-2.7,38.6),('UTILITY',1.8,38.8)]
    for text,x,z in labels: draw.text(xy(x,z),text,fill='#263733',font=small,anchor='mm')
    for x in range(-16,17,2): draw.text(xy(x,-.9),str(x),fill='#52605f',font=small,anchor='mt')
    for z in range(0,43,2): draw.text(xy(-17,z),str(z),fill='#52605f',font=small,anchor='rm')
    draw.text((100,2250),'World X runs left to right; World Z runs upward. Units: metres.',fill='#263733',font=label)
    draw.text((100,2300),'Planning footprints only; calibrate imported models and test player access before baking.',fill='#263733',font=small)
    im.save(HERE/'Hospital_Prop_Placement_Map.png')
