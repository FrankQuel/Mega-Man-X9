"""Author an articulated Dragon rig from the sheet's disassembled head/torso/legs/fists.
No source image is modified. Output stays in Tools until DragonRigBuilder.Build imports it.
"""
from pathlib import Path
import math,json,hashlib
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
ART=HERE/'dragon_rig_art';ART.mkdir(exist_ok=True)
QA=HERE/'QA/Dragon';QA.mkdir(parents=True,exist_ok=True)
SOURCE=ROOT/'SourceArt/Sprites/Bosses/Dragon/Dragon.png'
sheet=Image.open(SOURCE).convert('RGBA')
# Sheet-authored detached parts and their exact color-matched neutral-canvas placement.
# Detached-head and torso colors match the full reference pose 100%; remaining parts >94%.
source_parts=[('Head',[214,8,24,22],[21,0]),('TorsoAssembly',[198,34,57,23],[1,14]),('LegAssembly',[196,64,61,46],[0,35]),('FarForearm',[178,51,16,20],[4,29]),('NearForearm',[259,54,14,21],[41,28])]
canvas_size=(61,81)
raw={}
for name,(x,y,w,h),(ox,oy) in source_parts:
 im=sheet.crop((x,y,x+w,y+h)); c=Image.new('RGBA',canvas_size);c.alpha_composite(im,(ox,oy));raw[name]=np.array(c)
# Joint coordinates are pixels in the authored neutral canvas, y pointing down.
# Artwork is assigned once to a limb segment; no duplicated full arms or full legs.
joints={
'Hips':('Visual',(29,39),3),
'FarThigh':('Hips',(25,42),0),'FarShin':('FarThigh',(27,54),0),'FarFoot':('FarShin',(24,65),0),
'Torso':('Hips',(29,36),3),'Head':('Torso',(29,20),5),
'FarUpperArm':('Torso',(18,26),1),'FarForearm':('FarUpperArm',(12,32),2),
'NearThigh':('Hips',(32,42),6),'NearShin':('NearThigh',(36,55),6),'NearFoot':('NearShin',(43,67),6),
'NearUpperArm':('Torso',(36,26),7),'NearForearm':('NearUpperArm',(47,32),8)}
# Topological ordering is explicit, so Unity can build parents before children.
order=['Hips','FarThigh','FarShin','FarFoot','Torso','Head','FarUpperArm','FarForearm','NearThigh','NearShin','NearFoot','NearUpperArm','NearForearm']
pixels={n:np.zeros((81,61,4),dtype=np.uint8) for n in order}
pixels['Head']=raw['Head'];pixels['FarForearm']=raw['FarForearm'];pixels['NearForearm']=raw['NearForearm']
for y in range(81):
 for x in range(61):
  p=raw['TorsoAssembly'][y,x]
  if p[3]:
   target='FarUpperArm' if x<19 else 'NearUpperArm' if x>=34 else 'Torso'
   pixels[target][y,x]=p
  p=raw['LegAssembly'][y,x]
  if p[3]:
   if y<44 and 21<=x<=35:target='Hips'
   else:
    split=30 if y<54 else 32 if y<65 else 31
    side='Far' if x<split else 'Near'
    ankle=64 if side=='Far' else 66
    target=side+('Thigh' if y<54 else 'Shin' if y<ankle else 'Foot')
   pixels[target][y,x]=p
# Retain the painted anatomy at each hinge. A tiny joint patch copied from the same
# original artwork covers the one-pixel seam when the limb rotates, not extra members.
# These 3x3 patches stay inside opaque elbow/knee paint and are clipped to source alpha.
for child,parent,point in [('FarShin','FarThigh',(27,54)),('NearShin','NearThigh',(36,54)),('FarFoot','FarShin',(24,64)),('NearFoot','NearShin',(43,66))]:
 x,y=point
 for yy in range(y-1,y+2):
  for xx in range(x-1,x+2):
   if raw['LegAssembly'][yy,xx,3]:pixels[child][yy,xx]=raw['LegAssembly'][yy,xx]
# Shoulder pivots are at the inner painted joint, not the outside armor cap.
# Preserve a three-pixel socket patch under each rotating pad to avoid detached arms.
for x,y in [(19,26),(34,26)]:
 for yy in range(y-1,y+2):
  for xx in range(x-1,x+2):
   if raw['TorsoAssembly'][yy,xx,3]:pixels['Torso'][yy,xx]=raw['TorsoAssembly'][yy,xx]
# Sprite centers are separate Art children. Joint pivots can lie outside painted bounds.
parts=[]
for name in order:
 im=Image.fromarray(pixels[name]);box=im.getbbox()
 if box is None:raise ValueError('Empty anatomy '+name)
 im=im.crop(box);padded=Image.new('RGBA',(im.width+4,im.height+4));padded.alpha_composite(im,(2,2))
 padded.save(ART/(name+'.png'))
 parent,joint,sort=joints[name]
 px,py=(30,80) if parent=='Visual' else joints[parent][1]
 cx=(box[0]+box[2])/2;cy=(box[1]+box[3])/2
 parts.append(dict(name=name,parent=parent,joint=list(joint),localPosition=[(joint[0]-px)/100,(py-joint[1])/100,0],artLocalPosition=[(cx-joint[0])/100,(joint[1]-cy)/100,0],artPath='Tools/AnimationPipeline/dragon_rig_art/'+name+'.png',sortingOrder=sort,bounds=list(box)))
parents={n:joints[n][0] for n in order}
paths={}
for n in order:paths[n]=(paths[parents[n]]+'/' if parents[n]!='Visual' else 'Visual/')+n
rest={p['name']:p['localPosition'] for p in parts}
def mix(a,b,t):return a+(b-a)*t
def sample(points,t):
 if t<=points[0][0]:return points[0][1]
 for (t0,a),(t1,b) in zip(points,points[1:]):
  if t<=t1:return mix(a,b,(t-t0)/(t1-t0))
 return points[-1][1]
def leg_ik(pose,side,goalx,goaly,hip_delta=(0,0)):
 # Two-bone solver preserves both limb lengths. Angles are relative to painted bind pose.
 hp=np.array(joints[side+'Thigh'][1],float);kp=np.array(joints[side+'Shin'][1],float);ap=np.array(joints[side+'Foot'][1],float)
 u=np.array([kp[0]-hp[0],hp[1]-kp[1]]);l=np.array([ap[0]-kp[0],kp[1]-ap[1]])
 l1,l2=np.linalg.norm(u),np.linalg.norm(l)
 v=np.array([goalx-hp[0]-hip_delta[0],hp[1]-goaly-hip_delta[1]])
 d=np.linalg.norm(v);d=min(max(d,abs(l1-l2)+.1),l1+l2-.05);v=v/max(np.linalg.norm(v),.001)*d
 bend=-1 if side=='Far' else 1
 a2=bend*math.acos(max(-1,min(1,(d*d-l1*l1-l2*l2)/(2*l1*l2))))
 a1=math.atan2(v[1],v[0])-math.atan2(l2*math.sin(a2),l1+l2*math.cos(a2))
 base1=math.atan2(u[1],u[0]);base2=math.atan2(l[1],l[0])-base1
 thigh=math.degrees(a1-base1);shin=math.degrees(a2-base2)
 pose[side+'Thigh'][2]=thigh;pose[side+'Shin'][2]=shin;pose[side+'Foot'][2]=-thigh-shin

def pose_at(name,t,length):
 p={n:[0.,0.,0.] for n in order};q=t/length;w=2*math.pi*q
 if name in ('Idle','Breathe'):
  s=math.sin(w);p['Torso'][1]=.65*s;p['Head'][2]=1.5*s
  p['NearUpperArm'][2]=1.7*s;p['FarUpperArm'][2]=-1.7*s;p['Hips'][1]=.2*s
 elif name in ('Walk','Run'):
  run=name=='Run';stride=10 if run else 8;lift=7 if run else 5
  hipy=-3 if run else -2;p['Hips'][1]=hipy+.7*math.cos(2*w)
  p['Torso'][2]=(6 if run else 2)+1.3*math.sin(w);p['Head'][2]=-p['Torso'][2]*.6
  for side,phase in [('Far',w),('Near',w+math.pi)]:
   hp=joints[side+'Thigh'][1];ap=joints[side+'Foot'][1]
   gx=hp[0]+stride*math.cos(phase)
   gy=ap[1]-lift*max(0,math.sin(phase))
   leg_ik(p,side,gx,gy,(0,p['Hips'][1]))
  swing=24 if run else 15
  p['NearUpperArm'][2]=swing*math.cos(w);p['FarUpperArm'][2]=-swing*math.cos(w)
  p['NearForearm'][2]=-10-10*math.sin(w);p['FarForearm'][2]=8+8*math.sin(w)
 elif name=='IntroDrop':
  p['Torso'][2]=2*math.sin(w);p['Head'][2]=-2*math.sin(w)
  p['NearUpperArm'][2]=45;p['NearForearm'][2]=55;p['FarUpperArm'][2]=-20;p['FarForearm'][2]=-40
  p['NearThigh'][2]=30;p['NearShin'][2]=45;p['NearFoot'][2]=-55
  p['FarThigh'][2]=-15;p['FarShin'][2]=-25;p['FarFoot'][2]=30
 elif name=='Jump':
  h=sample([(0,0),(.12,-5),(.28,15),(.5,24),(.74,10),(.88,-4),(1,0)],q);p['Hips'][1]=h
  tuck=sample([(0,0),(.12,25),(.3,50),(.55,55),(.78,15),(.88,25),(1,0)],q)
  p['FarThigh'][2]=-tuck*.5;p['FarShin'][2]=-tuck*.55;p['FarFoot'][2]=tuck*.9
  p['NearThigh'][2]=tuck*.55;p['NearShin'][2]=tuck*.65;p['NearFoot'][2]=-tuck
  p['NearUpperArm'][2]=-tuck*.6;p['FarUpperArm'][2]=tuck*.3;p['Head'][2]=-tuck*.08
 elif name=='Crouch':
  k=sample([(0,0),(.55,1),(1,1)],q);p['Hips'][1]=-9*k;p['Torso'][2]=7*k;p['Head'][2]=-6*k
  for side in ('Far','Near'):
   ap=joints[side+'Foot'][1];leg_ik(p,side,ap[0],ap[1],(0,-9*k))
  p['NearUpperArm'][2]=-12*k;p['NearForearm'][2]=-16*k;p['FarForearm'][2]=12*k
 elif name=='ClawAttack':
  wind=sample([(0,0),(.28,1),(.42,0),(1,0)],q);strike=sample([(0,0),(.28,0),(.44,1),(.58,.7),(1,0)],q)
  p['Torso'][2]=-5*wind+9*strike;p['Head'][2]=4*wind-5*strike;p['Hips'][0]=-3*strike
  p['NearUpperArm'][2]=40*wind-78*strike;p['NearForearm'][2]=30*wind-18*strike
  p['FarUpperArm'][2]=-10*wind+22*strike;p['FarForearm'][2]=-8*strike
 elif name=='FireBreath':
  wind=sample([(0,0),(.2,1),(.4,0),(1,0)],q);fire=sample([(0,0),(.2,0),(.35,1),(.78,1),(1,0)],q)
  p['Torso'][2]=-5*wind+8*fire;p['Head'][2]=5*wind-13*fire;p['Hips'][0]=-2*fire
  p['NearUpperArm'][2]=16*fire;p['FarUpperArm'][2]=-10*fire;p['NearForearm'][2]=-7*fire;p['Hips'][1]=-6*fire
  for side in ('Far','Near'):
   ap=joints[side+'Foot'][1];leg_ik(p,side,ap[0],ap[1],(0,p['Hips'][1]))
 elif name=='CombatIdle':
  s=math.sin(w);p['Hips'][1]=-2;p['Torso'][2]=3+.8*s;p['Head'][2]=-3-1.2*s
  p['NearUpperArm'][2]=66+2*s;p['NearForearm'][2]=62-2*s
  p['FarUpperArm'][2]=-12-2*s;p['FarForearm'][2]=-36+2*s
  for side in ('Far','Near'):
   ap=joints[side+'Foot'][1];leg_ik(p,side,ap[0],ap[1],(0,-2))
 elif name=='Fireball':
  charge=sample([(0,0),(.22,1),(.43,1),(.52,0),(1,0)],q);push=sample([(0,0),(.42,0),(.52,1),(.66,1),(1,0)],q)
  p['Hips'][1]=-3*charge;p['Torso'][2]=-5*charge+12*push;p['Head'][2]=4*charge-8*push
  p['NearUpperArm'][2]=30*charge-92*push;p['NearForearm'][2]=45*charge-16*push
  p['FarUpperArm'][2]=35*charge+12*push;p['FarForearm'][2]=-55*charge-118*push
  for side in ('Far','Near'):
   ap=joints[side+'Foot'][1];leg_ik(p,side,ap[0],ap[1],(0,p['Hips'][1]))
 elif name=='RisingFlame':
  lift=sample([(0,0),(.18,-5),(.32,18),(.52,57),(.68,48),(.86,6),(1,0)],q);hit=sample([(0,0),(.18,0),(.32,1),(.62,1),(.85,.3),(1,0)],q)
  p['Hips'][1]=lift;p['Hips'][0]=-12*hit;p['Torso'][2]=-10*hit;p['Head'][2]=6*hit
  p['NearUpperArm'][2]=155*hit;p['NearForearm'][2]=20*hit;p['FarUpperArm'][2]=-28*hit;p['FarForearm'][2]=-25*hit
  p['NearThigh'][2]=42*hit;p['NearShin'][2]=55*hit;p['NearFoot'][2]=-70*hit
  p['FarThigh'][2]=-28*hit;p['FarShin'][2]=-30*hit;p['FarFoot'][2]=45*hit
 elif name=='FlamingKick':
  lift=sample([(0,0),(.14,-3),(.32,38),(.50,32),(.72,5),(.84,-4),(1,0)],q);kick=sample([(0,0),(.20,0),(.36,1),(.63,1),(.82,0),(1,0)],q)
  p['Hips'][1]=lift;p['Hips'][0]=-33*kick;p['Torso'][2]=-12*kick;p['Head'][2]=9*kick
  p['NearThigh'][2]=-102*kick;p['NearShin'][2]=-7*kick;p['NearFoot'][2]=62*kick
  p['FarThigh'][2]=28*kick;p['FarShin'][2]=45*kick;p['FarFoot'][2]=-50*kick
  p['NearUpperArm'][2]=65*kick;p['NearForearm'][2]=40*kick;p['FarUpperArm'][2]=-42*kick;p['FarForearm'][2]=-20*kick
 elif name=='GroundFireWave':
  crouch=sample([(0,0),(.22,1),(.70,1),(1,0)],q);strike=sample([(0,0),(.28,0),(.42,1),(.7,.9),(1,0)],q)
  p['Hips'][1]=-11*crouch;p['Torso'][2]=15*strike;p['Head'][2]=-12*strike
  p['NearUpperArm'][2]=-65*strike;p['NearForearm'][2]=-22*strike;p['FarUpperArm'][2]=45*strike;p['FarForearm'][2]=35*strike
  for side in ('Far','Near'):
   ap=joints[side+'Foot'][1];leg_ik(p,side,ap[0],ap[1],(0,p['Hips'][1]))
 elif name=='Landing':
  crouch=sample([(0,.55),(.18,1),(.5,.8),(1,0)],q);p['Hips'][1]=-9*crouch;p['Torso'][2]=7*crouch;p['Head'][2]=-5*crouch
  for side in ('Far','Near'):
   ap=joints[side+'Foot'][1];leg_ik(p,side,ap[0],ap[1],(0,p['Hips'][1]))
  p['NearUpperArm'][2]=28*crouch;p['NearForearm'][2]=20*crouch;p['FarUpperArm'][2]=-25*crouch
 elif name=='HurtFlash':
  hit=sample([(0,0),(.12,1),(.35,-.2),(.6,.12),(1,0)],q);p['Hips'][0]=4*hit;p['Torso'][2]=-10*hit;p['Head'][2]=8*hit
  p['NearUpperArm'][2]=24*hit;p['FarUpperArm'][2]=-18*hit;p['NearForearm'][2]=20*hit
 elif name=='Hurt':
  hit=sample([(0,0),(.16,1),(.36,-.3),(.6,.15),(1,0)],q)
  p['Hips'][0]=3*hit;p['Torso'][2]=-7*hit;p['Head'][2]=5*hit;p['NearUpperArm'][2]=13*hit;p['FarUpperArm'][2]=-8*hit
 elif name=='Death':
  k=sample([(0,0),(.15,.12),(.65,1),(1,1)],q);p['Hips'][1]=-28*k;p['Hips'][0]=-6*k;p['Hips'][2]=80*k
  p['Torso'][2]=12*k;p['Head'][2]=-15*k;p['NearUpperArm'][2]=-65*k;p['NearForearm'][2]=-35*k
  p['FarUpperArm'][2]=28*k;p['FarForearm'][2]=45*k;p['NearThigh'][2]=0;p['NearShin'][2]=0;p['NearFoot'][2]=-80*k
  p['FarThigh'][2]=0;p['FarShin'][2]=0;p['FarFoot'][2]=-80*k
 # Keep the corpse on the floor while the body collapses, without scaling any segment.
 if name=='Death':
  mats={};minimum=0.
  for n in order:
   parent=joints[n][0];base=rest[n];a=math.radians(p[n][2]);c,s=math.cos(a),math.sin(a)
   local=np.array([[c,-s,base[0]*100+p[n][0]],[s,c,base[1]*100+p[n][1]],[0,0,1.]])
   mats[n]=local if parent=='Visual' else mats[parent]@local
   part=next(v for v in parts if v['name']==n);b=part['bounds'];jx,jy=joints[n][1]
   for xx,yy in [(b[0],b[1]),(b[2],b[1]),(b[0],b[3]),(b[2],b[3])]:minimum=min(minimum,(mats[n]@np.array([xx-jx,jy-yy,1.]))[1])
  p['Hips'][1]-=minimum
 return p
clipdefs=[('Idle',2.0,True),('Breathe',1.6,True),('Walk',1.0,True),('Run',.6,True),('Jump',1.,False),('Crouch',.5,False),('ClawAttack',.72,False),('FireBreath',1.8,False),('Hurt',.4,False),('Death',1.2,False),('CombatIdle',1.8,True),('Fireball',1.35,False),('RisingFlame',1.3,False),('FlamingKick',1.35,False),('GroundFireWave',1.6,False),('HurtFlash',.6,False),('Landing',.45,False),('IntroDrop',.8,True)]
clips=[]
for name,length,loop in clipdefs:
 count=max(13,round(length*30)+1);times=[round(i*length/(count-1),6) for i in range(count)]
 poses=[pose_at(name,t,length) for t in times]
 tracks=[]
 for n in order:
  tracks.append(dict(path=paths[n],times=times,x=[round(rest[n][0]+p[n][0]/100,6) for p in poses],y=[round(rest[n][1]+p[n][1]/100,6) for p in poses],angle=[round(p[n][2],5) for p in poses]))
 clips.append(dict(name=name,length=length,loop=loop,tracks=tracks))
definition=dict(pixelsPerUnit=100,source='SourceArt/Sprites/Bosses/Dragon/Dragon.png',sourceSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),artworkSources=[dict(name=n,rect=r,neutralOffset=o) for n,r,o in source_parts],parts=parts,clips=clips,mouthParent='Head',mouthLocalPosition=[-.075,.015,0],clawParent='NearForearm',clawLocalPosition=[-.01,-.14,0])
(HERE/'dragon_rig.json').write_text(json.dumps(definition,indent=2),encoding='utf8')
# Render our actual hierarchy, rotating each limb around its own joint, for visual QA.
# World canvas origin is arbitrary; all coordinates below are original source pixels.
def matrices(pose):
 result={}
 for n in order:
  parent=joints[n][0];base=rest[n];d=pose[n];a=math.radians(d[2]);c,s=math.cos(a),math.sin(a)
  local=np.array([[c,-s,base[0]*100+d[0]],[s,c,base[1]*100+d[1]],[0,0,1.]])
  result[n]=local if parent=='Visual' else result[parent]@local
 return result
W,H=260,210;S=2;origin=np.array([150,176])
def render(name,t,length):
 pose=pose_at(name,t,length);ms=matrices(pose);out=Image.new('RGBA',(W,H),(22,29,45,255));d=ImageDraw.Draw(out);d.line((0,origin[1]+1,W,origin[1]+1),fill='#607080')
 for part in sorted(parts,key=lambda p:p['sortingOrder']):
  n=part['name'];paint=Image.open(ROOT/part['artPath']);m=ms[n];center=part['artLocalPosition'];
  # Map image local pixels into rendered pixels. Inverse transform avoids interpolation blur.
  a=m[:2,:2];offset=m[:2,2]+a@np.array(center[:2])*100
  # source image y down -> world y up, screen y down
  f=np.diag([1,-1]);linear=f@a@f*S; trans=origin+f@offset*S-linear@np.array([paint.width/2,paint.height/2])
  inv=np.linalg.inv(linear);off=-inv@trans
  layer=paint.transform((W,H),Image.Transform.AFFINE,tuple([inv[0,0],inv[0,1],off[0],inv[1,0],inv[1,1],off[1]]),resample=Image.Resampling.NEAREST)
  out.alpha_composite(layer)
 d.text((7,7),f'{name} {t:.2f}s',fill='white');return out
contact=Image.new('RGBA',(W*5,H*len(clips)),(22,29,45,255))
for row,(name,length,loop) in enumerate(clipdefs):
 frames=[render(name,i*length/16,length) for i in range(17)]
 frames[0].save(QA/(name+'.gif'),save_all=True,append_images=frames[1:],duration=max(20,round(length/17*1000)),loop=0,disposal=2)
 for col,q in enumerate([0,.25,.5,.75,1]):contact.alpha_composite(render(name,q*length,length),(col*W,row*H))
contact.convert('RGB').save(QA/'dragon_rig_contact.png')
neutral=render('Idle',0,2);neutral.save(QA/'dragon_rig_neutral.png')
# Exact coverage of disassembled pixels, except intentional tiny hinge patches.
expected=np.zeros((81,61),bool)
for a in raw.values():expected|=a[:,:,3]>0
coverage=np.zeros((81,61),int)
for a in pixels.values():coverage+=(a[:,:,3]>0)
assert np.all(coverage[expected]>0),'Lost body pixels'
assert all(np.isfinite(t['angle']).all() for c in clips for t in c['tracks'])
report=dict(boneCount=len(parts),articulatedJointCount=12,clipCount=len(clips),transformTracks=sum(len(c['tracks']) for c in clips),missingSourcePixels=int(np.sum(expected&(coverage==0))),hingeOverlapPixels=int(np.sum(coverage>1)),allScaleCurvesAbsent=True,originalFileUnchanged=True,bindFacing='Left',notes='Transform animations are newly authored for this project from existing detached artwork; not claimed to reproduce original game timing.')
(HERE/'dragon_rig_authoring_report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps(report,indent=2))


