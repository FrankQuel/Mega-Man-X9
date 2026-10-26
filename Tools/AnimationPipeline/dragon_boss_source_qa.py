from pathlib import Path
import json,math
import numpy as np
from PIL import Image,ImageDraw
ROOT=Path.cwd();d=json.loads(Path('Tools/AnimationPipeline/dragon_rig.json').read_text());parts=d['parts'];rest={p['name']:p for p in parts};qa=Path('Tools/AnimationPipeline/QA/Dragon');qa.mkdir(exist_ok=True,parents=True)
W,H=450,410;origin=np.array([295.,365.]);scale=240.
def render(name,q):
 clip=next(c for c in d['clips'] if c['name']==name);t=q*clip['length'];mats={};out=Image.new('RGBA',(W,H),(22,29,45,255));draw=ImageDraw.Draw(out);draw.line((0,366,W,366),fill='#607080');draw.text((8,8),name+' / skeletal source QA',fill='white')
 for p,tr in zip(parts,clip['tracks']):
  a=math.radians(np.interp(t,tr['times'],tr['angle']));c,s=math.cos(a),math.sin(a);m=np.array([[c,-s,np.interp(t,tr['times'],tr['x'])],[s,c,np.interp(t,tr['times'],tr['y'])],[0,0,1.]])
  mats[p['name']]=m if p['parent']=='Visual' else mats[p['parent']]@m
 for p in sorted(parts,key=lambda p:p['sortingOrder']):
  path=p['artPath'];center=p['artLocalPosition'];bone=p['name']
  if bone=='Head' and name=='FireBreath' and .26<=q<.84:path=d['openMouthArtPath'];center=d['openMouthArtLocalPosition']
  if bone=='NearForearm' and name in ('ClawAttack','Fireball','RisingFlame') and .24<=q<.82:path=d['openClawArtPath'];center=d['openClawArtLocalPosition']
  im=Image.open(path).convert('RGBA');m=mats[bone];a=m[:2,:2];pos=m[:2,2]+a@np.array(center[:2]);f=np.diag([1.,-1.]);linear=f@a@f*scale/100;trans=origin+f@pos*scale-linear@np.array([im.width/2,im.height/2]);inv=np.linalg.inv(linear);offset=-inv@trans
  layer=im.transform((W,H),Image.Transform.AFFINE,(inv[0,0],inv[0,1],offset[0],inv[1,0],inv[1,1],offset[1]),Image.Resampling.NEAREST);out.alpha_composite(layer)
 return out
names=['CombatIdle','FireBreath','Fireball','RisingFlame','FlamingKick','GroundFireWave','HurtFlash','Landing'];out=Image.new('RGBA',(W*2,H*4),(22,29,45,255))
for i,n in enumerate(names):
 im=render(n,.55);im.save(qa/(n+'_boss_source.png'));out.alpha_composite(im,((i%2)*W,(i//2)*H))
out.convert('RGB').save(qa/'dragon_boss_attack_body_contact.png')
print('body QA saved')
