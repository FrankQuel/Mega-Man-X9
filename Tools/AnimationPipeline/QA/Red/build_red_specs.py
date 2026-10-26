"""Manually reviewed animation bands; original art stays untouched."""
import sys,json
from pathlib import Path
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from sheet_tools import *
P='Assets/Recursos/Sprites/Player/Robot futuristico rojo/'
OUT=ROOT/'Tools/AnimationPipeline/red_specs.json'
data=json.loads((ROOT/'Tools/AnimationPipeline/QA/Red/rows.json').read_text())
MAIN=P+'robot futuristico rojo principal.gif'
E1=P+'robot futuristico rojo extra 1.gif'
E2=P+'robot futuristico rojo extra 2.gif'
E3=P+'robot futuristico rojo extra 3.gif'
SPECIAL=P+'ataques especiales del robot futuristico rojo.gif'
KEY={MAIN:[253,67,190],E1:[105,237,249],E2:[0,167,254],E3:[255,0,255],SPECIAL:[255,255,255]}
M=data['main']; A=data['extra1']; B=data['extra2']; C=data['extra3']; S=data['special']

def tight(source,band):
    x,y,w,h=band
    ys,xs=np.nonzero(mask(source)[y:y+h,x:x+w])
    return [int(x+xs.min()),int(y+ys.min()),int(xs.max()-xs.min()+1),int(ys.max()-ys.min()+1)]

# Blade arcs descend into the vertical space above the next recovery row.
M[10][4]=tight(MAIN,(343,676,103,76))
M[10][5]=tight(MAIN,(452,676,104,76))
M[11]=M[11][:6]+[tight(MAIN,(470,760,48,56)),tight(MAIN,(519,760,50,56))]
M[21]=frames(MAIN,(0,1494,622,74),gap=0)
# Sixteen run frames are separated by exactly one column, and two attack
# frames on each of the following rows have a one-column gap as well.
for row in (1,2,4,9):
    y0,y1=runs(mask(E3).any(axis=1),2)[row]
    C[row]=frames(E3,(0,y0,650,y1-y0),gap=0)
S[2]=frames(SPECIAL,(80,68,176,48),gap=0)

specs=[]
def anchor(source,rect):
    x,y,w,h=rect
    arr=np.array(rgba(source))[y:y+h,x:x+w,:3].astype(float)
    r,g,b=arr[:,:,0],arr[:,:,1],arr[:,:,2]
    armor=(r>130)&(r>g*1.5)&(r>b*1.6)&(g<170)&(b<150)
    ys,xs=np.nonzero(armor)
    if len(xs)>=12:
        # Median red armor excludes yellow hair and green/white saber effects.
        return [round(float(np.median(xs)),2),float(min(h,int(ys.max())+2))]
    return [w/2,h]

def add(source,name,rects,loop=False,fps=12,effect=False):
    if not rects:raise ValueError(name)
    item={'source':source,'category':'Effects' if effect else 'Player','unit':'RedEffects' if effect else 'Red','name':name,'rects':rects,'fps':fps,'loop':loop,'keyColors':[KEY[source]]}
    if source==SPECIAL:item['backgroundMode']='edge'
    if not effect:item['anchors']=[anchor(source,r) for r in rects]
    specs.append(item)

# Base sheet: locomotion, complete saber attacks, wall actions and damage.
add(MAIN,'TeleportArrivalEnergy',M[0],effect=True,fps=16)
add(MAIN,'TeleportIn',M[1][:4],fps=10)
add(MAIN,'Idle',M[1][4:],loop=True,fps=7)
add(MAIN,'Jump',M[2]+M[3])
add(MAIN,'JumpRise',M[2][3:],fps=10)
add(MAIN,'Fall',M[3][:4],loop=True,fps=10)
add(MAIN,'Land',M[3][4:],fps=14)
add(MAIN,'RunStart',M[4][:2],fps=14)
add(MAIN,'Run',M[4][2:]+M[5],loop=True,fps=16)
add(MAIN,'GroundSlash1',M[6]+M[7][:5],fps=16)
add(MAIN,'GroundSlash2',M[7][5:]+M[8]+M[9],fps=16)
add(MAIN,'GroundSlash3',M[10]+M[11],fps=16)
add(MAIN,'AirSlash1',M[12],fps=16)
add(MAIN,'AirSlash2',M[13],fps=16)
add(MAIN,'DashStart',M[14][:3],fps=16)
add(MAIN,'Dash',M[14][3:7],loop=True,fps=16)
add(MAIN,'DashEnd',M[14][7:],fps=16)
add(MAIN,'AirUppercutPose',M[15],fps=14)
add(MAIN,'AirSlashBackhand',M[16],fps=16)
add(MAIN,'WallClimb',M[17][:11],loop=True,fps=12)
add(MAIN,'WallPushOff',M[17][11:],fps=14)
add(MAIN,'WallSlash',M[18],fps=16)
add(MAIN,'Hurt',M[19],fps=12)
add(MAIN,'Electrocuted',M[20],loop=True,fps=14)
add(MAIN,'TurnAndTeleportOut',M[21],fps=12)
add(MAIN,'TurnAround',M[21][:9],fps=12)
add(MAIN,'TeleportOut',M[21][8:],fps=12)

# Additional art provides full alternative attacks and poses.
add(E1,'DashSlash',A[0]+A[1],fps=16)
add(E1,'ProneCollapse',A[2],fps=10)
add(E1,'AirPoseExtended',A[3],loop=True,fps=12)
add(E1,'AirSlashLongA',A[4],fps=16)
add(E1,'AirSlashLongB',A[5],fps=16)
add(E1,'AirSlashLongC',A[6],fps=16)
add(E2,'AirPoseRaised',B[0],loop=True,fps=12)
add(E2,'AirPoseLowered',B[1],loop=True,fps=12)
add(E2,'PinkFlameAura',B[2],loop=True,fps=12,effect=True)
energy=[tight(E2,b) for b in [(15,320,53,60),(72,320,53,60),(128,320,53,60),(185,320,53,60),(241,320,53,60),(299,300,53,80),(369,300,59,80),(439,200,56,180),(501,190,56,190),(566,200,56,180)]]
add(E2,'GroundEnergyStrike',energy,fps=12)
add(E2,'GroundHeavySlash',B[4],fps=16)
add(E2,'LargeSaberWave',[tight(E2,(545,390,100,150))],effect=True)

# The third supplementary sheet supplies another complete effect style.
add(E3,'Enhanced_Idle',C[0],loop=True,fps=7)
add(E3,'Enhanced_RunStart',C[1][:2],fps=14)
add(E3,'Enhanced_Run',C[1][2:]+C[2],loop=True,fps=16)
add(E3,'Enhanced_GroundSlash1',C[3],fps=16)
add(E3,'Enhanced_GroundSlash2',C[4],fps=16)
add(E3,'Enhanced_RisingSlash',C[5],fps=16)
add(E3,'Enhanced_Ready',C[6],loop=True,fps=7)
add(E3,'Enhanced_Jump',C[7]+C[8],fps=12)
add(E3,'Enhanced_JumpRise',C[7],fps=12)
add(E3,'Enhanced_Fall',C[8][:4],loop=True,fps=10)
add(E3,'Enhanced_Land',C[8][4:],fps=14)
add(E3,'Enhanced_AirSlash1',C[9],fps=16)
add(E3,'Enhanced_AirSlash2',C[10],fps=16)
add(E3,'Enhanced_SpinSlash',C[11],fps=16)
add(E3,'Enhanced_DashStart',C[12][:3],fps=16)
add(E3,'Enhanced_Dash',C[12][3:],loop=True,fps=16)
add(E3,'Enhanced_DashEnd',C[13],fps=16)
add(E3,'Enhanced_GroundSlideSlash',C[14]+C[15],fps=16)
add(E3,'Enhanced_AirUppercutPose',C[16],fps=14)
add(E3,'Enhanced_WallSlash',C[17],fps=16)
add(E3,'Enhanced_WallSlashFollowThrough',C[18]+C[19],fps=16)
add(E3,'Enhanced_AirPoseLong',C[20],loop=True,fps=12)
add(E3,'Enhanced_AirSlashLong',C[21],fps=16)
add(E3,'Enhanced_Electrocuted',C[22],loop=True,fps=14)
add(E3,'Enhanced_WallSlashEnd',C[23]+C[24],fps=16)

# Standalone effects from the dedicated special-attacks sheet.
add(SPECIAL,'FireUppercutPose',S[0],effect=True)
add(SPECIAL,'IceRing',S[1],loop=True,fps=12,effect=True)
add(SPECIAL,'SaberWaveShrink',S[2],fps=16,effect=True)
add(SPECIAL,'IceProjectiles',S[3],fps=12,effect=True)
add(SPECIAL,'GroundIceSpikes',S[4],fps=12,effect=True)

for item in specs:
    source=rgba(item['source'])
    for rect in item['rects']:
        x,y,w,h=rect
        assert min(x,y)>=0 and w>0 and h>0 and x+w<=source.width and y+h<=source.height,(item['name'],rect)
assert len({(s['unit'],s['name']) for s in specs})==len(specs)
OUT.write_text(json.dumps(specs,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'{len(specs)} clips, {sum(len(s["rects"]) for s in specs)} frame references')
