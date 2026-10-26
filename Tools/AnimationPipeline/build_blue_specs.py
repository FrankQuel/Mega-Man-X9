"""Verified blue-player clip specification builder; source sheets are read only."""
from pathlib import Path
import json, sys
import numpy as np
from PIL import Image,ImageDraw
sys.path.insert(0,str(Path(__file__).resolve().parent))
from sheet_tools import frames,mask,rgba
ROOT=Path(__file__).resolve().parents[2]
D=json.loads((ROOT/'Tools/AnimationPipeline/QA/Blue/candidate_rows.json').read_text())
M=D['Main']['rows'];U=D['Ultimate']['rows'];E=D['Extra']['rows']
def extract(label,band,gap=0):return frames(D[label]['source'],band,gap=gap,min_area=20)
def tight(label,band):
 x,y,w,h=band;m=mask(D[label]['source'])[y:y+h,x:x+w];ys,xs=np.nonzero(m)
 return [int(x+xs.min()),int(y+ys.min()),int(xs.max()-xs.min()+1),int(ys.max()-ys.min()+1)]
M[15]=extract('Main',D['Main']['bands'][15])
M[18]=extract('Main',(0,1104,590,59),gap=1)
M[1][6]=[316,121,82,47]
M[1][7]=[400,125,54,43]
M[4][5]=[365,298,50,44]
U[2][5]=[329,116,43,42]
U[4]=extract('Ultimate',(0,234,520,44),gap=1)
E[4][2]=[182,224,108,60]
M[21]=M[21][:4]+[[356,1310,46,59]]
U[15]=extract('Ultimate',D['Ultimate']['bands'][15])[:6]+[[363,815,59,31],[422,815,59,31]]
U[18]=[[19,964,35,57],[58,964,35,57],[97,964,35,55],[135,964,36,55],[173,964,35,53],[213,964,34,53],[247,964,45,46],[289,964,46,46],[333,965,45,46],[376,965,45,46],[421,965,43,46],[462,965,43,46]]
U[22]=extract('Ultimate',D['Ultimate']['bands'][22])
U[24]=U[24][:10]
U[25]=U[25][:-1]+[[463,1381,6,71]]
U[27]=extract('Ultimate',D['Ultimate']['bands'][27])
# These two frames straddle the originally projected y-band.
extra_recover=[tight('Extra',(80,315,40,62)),tight('Extra',(120,315,40,62))]+E[6][2:]
clips=[]
def add(label,name,rects,fps=12,loop=False,anchors=None,isolate=False,notes=None):
 c={'source':D[label]['source'],'category':'Player','unit':'BlueUltimate' if label=='Ultimate' else 'Blue','name':name,'rects':rects,'fps':fps,'loop':loop}
 if anchors is not None:c['anchors']=anchors
 if isolate:c['isolateComponent']=True
 if notes:c['notes']=notes
 clips.append(c)
def aimanchors(rects,offsets=None):
 return [[round(w/2,1),h] if offsets is None else [offsets[i],h] for i,(x,y,w,h) in enumerate(rects)]
def pair(label,name,rects,fps=12,loop=False,isolate=False):
 for n,rs in [(name,rects[::2]),(name+'Flash',rects[1::2])]:
  add(label,n,rs,fps,loop,isolate=isolate,notes='Alternating source frames are separated into the base and bright palette variants; no new frames are generated.')
# Main sheet: every visible pose and integral effect sequence.
add('Main','TeleportBeam',M[0][:2],10,False)
add('Main','TeleportOrb',M[0][2:],10,False)
add('Main','TeleportArrival',M[1],12,False)
add('Main','Idle',M[2],8,True)
add('Main','Jump',M[3],12,False)
add('Main','Shoot',M[4],16,False,aimanchors(M[4],[18,18,18,18,18,27,18,17]),notes='Body/foot horizontal alignment is estimated visually for the wide muzzle frames.')
add('Main','ChargeShot',M[5],14,False,aimanchors(M[5],[18,20,18,20,21,24,18,17]),notes='The expanding shot is integral to these source frames; horizontal body anchors are estimated visually.')
add('Main','RunStart',M[6][:2],12,False)
add('Main','Run',M[6][2:]+M[7],14,True)
add('Main','Dash',M[8],16,False)
pair('Main','JumpShoot',M[9]+M[10],12)
pair('Main','IdleAim',M[11][:4],8,True)
pair('Main','RunShoot',M[11][4:]+M[12]+M[13]+M[14],14,True)
pair('Main','DashShoot',M[15]+M[16],16)
add('Main','WallJump',M[17][:6],12,False)
pair('Main','WallJumpShoot',M[17][6:]+M[18],12)
add('Main','WallCling',M[19][:1],1,True)
add('Main','LadderClimb',M[19][1:9],10,True)
add('Main','LadderDismount',M[19][9:12],10,False)
add('Main','LadderShoot',M[19][12:],12,False,notes='The sheet supplies one bright aiming pose followed by two base-palette poses.')
add('Main','HitElectrical',M[20][:4],12,False)
add('Main','AlternateIdle',M[20][4:],8,True)
add('Main','AirHit',M[21],12,False,notes='The last rectangle includes the detached electrical sparks at its left; these belong to the same visible frame.')
add('Main','TeleportDeparture',M[22],12,False)
# Extra sheet: somersault, air movement and separate Nova Strike effects.
add('Extra','Somersault',E[0]+E[1]+E[2],14,False,notes='The first source pose touches the top edge of the sheet; no missing pixels are invented.')
add('Extra','NovaStrikeStartup',E[3][:4],14,False)
travel=[E[3][4],E[4][0],E[4][3]]
add('Extra','NovaStrikeTravel',travel,14,False,[[42,28],[45,28],[94,45]],notes='Travel frames contain the actor. Body alignment is approximate because the final frame includes a long trail.')
add('Extra','NovaStrikeTrail',E[4][1:3],14,False,notes='Standalone trail/effect frames, deliberately separate from the actor animation.')
add('Extra','AirDash',E[5][2:],14,False)
add('Extra','AirRecover',extra_recover,12,False,notes='Descriptive name: airborne recovery poses; the source does not document a gameplay trigger.')
add('Extra','CrouchIdle',E[7],8,True)
# Ultimate armor repeats the documented movement with additional hover and saber sequences.
add('Ultimate','Idle',U[0][-3:],8,True,notes='Only the final three grounded, arms-lowered breathing poses are used for Idle. The opening arm gesture is a separate IntroSalute action.')
add('Ultimate','IntroSalute',U[0],10,False,notes='Original complete arm-opening/lowering gesture, preserved as an explicit one-shot introduction instead of looping it as Idle.')
add('Ultimate','Jump',U[1],12,False)
add('Ultimate','Shoot',U[2],16,False,aimanchors(U[2],[16,16,16,16,17,23,16,16]),notes='Wide muzzle frame body anchors estimated visually.')
add('Ultimate','ChargeShot',U[3],14,False,aimanchors(U[3],[17,17,17,18,18,20,16,16]),notes='Expanding shot included in original frames. Body anchors estimated visually.')
add('Ultimate','RunStart',U[4][:2],12,False)
add('Ultimate','Run',U[4][2:]+U[5],14,True)
add('Ultimate','Dash',U[6],16,False)
add('Ultimate','Hover',U[7]+U[8],12,True,notes='Boot thrust belongs to the actor frames.')
pair('Ultimate','JumpShoot',U[9]+U[10],12)
pair('Ultimate','IdleAim',U[11][:4],8,True)
pair('Ultimate','RunShoot',U[11][4:]+U[12]+U[13]+U[14],14,True)
pair('Ultimate','DashShoot',U[15]+U[16],16,isolate=True)
pair('Ultimate','HoverShoot',U[17]+U[18]+U[19],12,True,isolate=True)
add('Ultimate','WallJump',U[20][:6],12,False)
pair('Ultimate','WallJumpShoot',U[20][6:]+U[21],12)
add('Ultimate','WallCling',U[22][:1],1,True)
add('Ultimate','LadderClimb',U[22][1:9],10,True)
add('Ultimate','LadderDismount',U[22][9:11],10,False)
add('Ultimate','LadderShoot',U[22][11:],12,False)
add('Ultimate','NovaStrikeStartup',U[23][:5],14,False,notes='The startup ends in the horizontal actor/booster pose.')
add('Ultimate','NovaStrikeBurst',U[23][5:],14,False,[[26,42],[26,43],[29,47],[79,45]],notes='All burst rectangles contain the actor or its gold silhouette. Horizontal body registration is approximate.')
add('Ultimate','HitElectrical',U[24][:4],12,False)
add('Ultimate','AlternateIdle',U[24][4:],8,True)
add('Ultimate','TeleportDeparture',U[25],12,False)
add('Ultimate','TeleportArrival',list(reversed(U[25])),12,False,notes='Materialization uses the original Ultimate armor departure frames in reverse order; no base-armor body is substituted.')
rs=U[26]+U[27]
add('Ultimate','DashChargeShot',rs,14,False,aimanchors(rs,[17,20,24,24,20,22,22,24,25,24,21,20,21,20]),notes='Low/dashing charged shot with the original growing effect; horizontal body registration is approximate.')
add('Ultimate','AirArmRaise',U[28][:9],12,False,notes='Descriptive name for an airborne arm-raise sequence; gameplay meaning is not encoded by the sheet.')
add('Ultimate','DefeatCollapse',U[28][9:],10,False)
rs=U[29]+U[30]
add('Ultimate','SaberGroundSlash',rs,16,False,aimanchors(rs,[18,17,19,19,67,66,67,66,41,24,23,23,23,23,20]),notes='Two consecutive rows form the complete grounded saber sweep. Body anchors are estimated visually to hold the actor while the large blade crosses the frame.')
add('Ultimate','SaberAirSlash',U[31],16,False,aimanchors(U[31],[18,17,20,31,24,20,16]),notes='Body anchors estimated visually for the wide aerial blade arcs.')
rs=U[32]+U[33]
add('Ultimate','SaberWallSlash',rs,16,False,aimanchors(rs,[13,13,65,65,67,48,24,16,16]),notes='Two rows form the wall/diagonal aerial slash. Body anchors estimated visually; the source has no gameplay labels.')
# All rectangles must be nonempty and inside the corresponding original sheet.
for c in clips:
 im=rgba(c['source'])
 for r in c['rects']:
  x,y,w,h=r
  assert min(x,y)>=0 and min(w,h)>0 and x+w<=im.width and y+h<=im.height,(c['name'],r)
  assert np.array(im.crop((x,y,x+w,y+h)))[:,:,3].any(),(c['name'],r)
 if 'anchors' in c:assert len(c['anchors'])==len(c['rects'])
(ROOT/'Tools/AnimationPipeline/blue_specs.json').write_text(json.dumps(clips,indent=2)+'\n',encoding='utf-8')
# Contact sheets of final clips for inspection, not imported into Unity.
for c in clips:
 source=rgba(c['source']);size=108;cols=8;rows=(len(c['rects'])+cols-1)//cols
 canvas=Image.new('RGBA',(cols*size,rows*120+24),(50,50,50,255));d=ImageDraw.Draw(canvas);d.text((4,4),c['unit']+' '+c['name'],fill='white')
 for i,(x,y,w,h) in enumerate(c['rects']):
  crop=source.crop((x,y,x+w,y+h));zoom=min(2,96/max(w,h));crop=crop.resize((max(1,round(w*zoom)),max(1,round(h*zoom))),Image.Resampling.NEAREST)
  xx=(i%cols)*size+(size-crop.width)//2;yy=24+(i//cols)*120+16
  canvas.alpha_composite(crop,(xx,yy));d.text(((i%cols)*size+3,24+(i//cols)*120),str(i),fill='white')
 canvas.convert('RGB').save(ROOT/'Tools/AnimationPipeline/QA/Blue'/f"Clip_{c['unit']}_{c['name']}.png")
print('clips',len(clips),'frames',sum(len(c['rects']) for c in clips))
print('Blue',sum(c['unit']=='Blue' for c in clips),'BlueUltimate',sum(c['unit']=='BlueUltimate' for c in clips))
