import json,sys
from pathlib import Path
sys.path.insert(0,'Tools/AnimationPipeline')
sys.path.insert(0,'Tools/AnimationPipeline/QA/World')
from sheet_tools import frames,mask,annotate
from components import components
import numpy as np
specs=[]
def add(source,category,unit,name,rects,fps=10,loop=True,**kwargs):
 if not rects: raise ValueError((unit,name))
 specs.append(dict(source=source,category=category,unit=unit,name=name,rects=rects,fps=fps,loop=loop,**kwargs))
def tight(src,box):
 x,y,w,h=box; a=mask(src)[y:y+h,x:x+w]; ys,xs=np.nonzero(a)
 if len(xs)==0:raise ValueError((src,box))
 return list(map(int,[x+xs.min(),y+ys.min(),xs.max()-xs.min()+1,ys.max()-ys.min()+1]))
def row(src,lo,hi,minarea=300):
 return sorted([r[:4] for r in components(src) if r[4]>=minarea and lo<=r[1]+r[3]/2<hi],key=lambda r:r[0])
# In these GIFs the source transparency is already authored; no color key removal.
for file,unit in [('grenademan','GrenadeMan'),('grenademan2','GrenadeManGreen')]:
 src='Assets/Recursos/Sprites/Enemies/'+file+'.gif'
 shift=0 if file=='grenademan' else 3
 rows=[row(src,a+shift,b+shift,500) for a,b in [(0,60),(60,130),(130,195),(195,267),(267,330),(330,401)]]
 add(src,'Enemies',unit,'Idle',rows[0],8)
 add(src,'Enemies',unit,'AimForward',rows[1][:2],8,False)
 add(src,'Enemies',unit,'AimUp',rows[1][2:],8,False)
 add(src,'Enemies',unit,'Run',rows[2],12)
 add(src,'Enemies',unit,'ThrowGrenade',rows[3],12,False)
 add(src,'Enemies',unit,'Hurt',rows[4][:2],10,False)
 add(src,'Enemies',unit,'Jump',rows[4][2:3],8,False)
 add(src,'Enemies',unit,'Fall',rows[4][3:],8,False)
 add(src,'Enemies',unit,'Takeoff',rows[5][:2],10,False)
 add(src,'Enemies',unit,'Land',rows[5][2:],10,False)
 bullets=row(src,80+shift,120+shift,80)
 bullets=[r for r in bullets if r[2]<20]
 add(src,'Effects',unit+'Projectiles','GrenadeSpin',bullets,12)
 # Last grenade has its own complete authored box.
 throwproj=[r[:4] for r in components(src) if r[4]>=80 and r[0]>460]
 add(src,'Effects',unit+'Projectiles','ThrownGrenade',throwproj,10)
# Round rolling enemy; bottom attack poses include authored dust.
src='Assets/Recursos/Sprites/Enemies/burnout.gif'
add(src,'Enemies','Burnout','Roll',row(src,0,55,1000),12)
add(src,'Enemies','Burnout','DeploySpikes',row(src,55,110,1000),12,False)
add(src,'Enemies','Burnout','SpinAttack',[tight(src,b) for b in [(16,112,80,50),(96,112,79,50),(175,112,79,50),(255,112,81,50),(336,112,93,50),(432,112,86,50)]],12)
src='Assets/Recursos/Sprites/Enemies/spikeball.gif'
add(src,'Enemies','SpikeBall','Roll',row(src,0,41,600),12)
add(src,'Enemies','SpikeBall','DeployLegs',row(src,41,91,600),10,False)
add(src,'Enemies','SpikeBall','Walk',row(src,91,139,600),12)
# Green, reversed green, blue sheet rows are separate forms, not one cycle.
src='Assets/Recursos/Sprites/Enemies/bird.gif'
add(src,'Enemies','BirdGreen','Idle',[[14,14,32,21]],8)
add(src,'Enemies','BirdGreen','Fire',[tight(src,b) for b in [(10,10,37,30),(48,10,49,30),(98,10,51,30)]],10,False)
add(src,'Enemies','BirdGreenReverse','Fire',[tight(src,b) for b in [(4,41,52,37),(57,41,51,37),(110,41,53,37)]],10,False)
add(src,'Enemies','BirdBlue','Idle',[[13,89,32,21]],8)
add(src,'Enemies','BirdBlue','Fire',[tight(src,b) for b in [(10,85,36,30),(49,85,50,30),(102,85,52,30)]],10,False)
# Flying mini laser cannon. Cell boundaries retain disconnected charging sparks.
src='Assets/Recursos/Sprites/Enemies/minilasercannon.gif'
add(src,'Enemies','MiniLaserCannon','Hover',[tight(src,b) for b in [(9,6,34,51),(44,6,31,51),(75,6,32,51),(108,6,32,51)]],10)
add(src,'Enemies','MiniLaserCannon','Charge',[tight(src,b) for b in [(142,0,71,57),(214,0,43,57),(258,0,63,57),(321,0,41,57),(364,0,60,57),(25,60,46,53),(75,60,59,53)]],12,False)
add(src,'Enemies','MiniLaserCannon','Release',[tight(src,b) for b in [(142,60,34,53),(180,60,53,53),(237,60,50,53),(287,57,56,56)]],12,False)
add(src,'Enemies','MiniLaserCannon','Recover',[tight(src,b) for b in [(346,65,32,49),(378,65,32,49),(320,142,35,47),(356,142,35,47)]],10,False)
add(src,'Enemies','MiniLaserCannon','LaserFire',[tight(src,b) for b in [(18,131,130,49),(18,179,130,50),(18,230,131,54),(18,287,130,58),(159,131,129,52),(159,185,129,54),(159,237,129,52),(159,288,129,56)]],12,False)
# Gigadeath is a composed turret. Isolated complete assemblies/parts are kept apart.
src='Assets/Recursos/Sprites/Enemies/gigadeath.gif'
add(src,'Enemies','GigaDeath','Idle',[[118,269,84,70]],8)
add(src,'Enemies','GigaDeathParts','TopArmor',[[2,6,84,47]],8)
add(src,'Enemies','GigaDeathParts','LowerArmor',[[7,62,60,35]],8)
add(src,'Enemies','GigaDeathParts','CannonRecoil',[[139,49,62,31],[207,50,63,31]],10,False)
add(src,'Enemies','GigaDeathParts','LegCycle',[[152,13,24,26],[179,13,26,26],[208,14,28,26],[239,13,27,26]],10)
add(src,'Enemies','GigaDeathParts','SmallFinCycle',[[74,58,16,14],[93,59,16,13],[112,60,16,12]],10)
add(src,'Enemies','GigaDeathParts','MissileLaunch',[[105,106,35,12],[153,106,26,12],[182,106,28,12],[213,106,31,12]],10,False)
add(src,'Effects','GigaDeathProjectiles','MuzzleFlash',[[61,128,12,11],[76,126,16,15]],12,False)
add(src,'Effects','GigaDeathProjectiles','SmallShot',[[106,128,9,13],[118,126,15,15],[136,129,24,11],[163,131,15,7]],12)
add(src,'Effects','GigaDeathProjectiles','MissileFlight',[[37,159,32,13],[72,159,32,14],[107,158,32,16],[142,155,28,21]],12)
add(src,'Effects','GigaDeathProjectiles','MissileTail',[[184,155,29,21],[216,155,29,21],[248,155,29,21],[280,155,29,21]],12)
add(src,'Effects','GigaDeathProjectiles','ImpactRing',[[94,191,18,16],[127,189,19,20],[159,187,21,24],[192,185,25,31],[224,185,32,31]],12,False)
# Door columns are opening frames in reading order; bottom half pieces are a separate section.
src='Assets/Recursos/Sprites/Effects/bossdoor.gif'
add(src,'Effects','BossDoor','Unlock',[tight(src,(x,0,36,72)) for x in [3,50,100,151,202]],10,False)
add(src,'Effects','BossDoor','Open',[tight(src,(x,74,38,70)) for x in [3,50,100,151,202]],10,False)
add(src,'Effects','BossDoor','RetractTop',[tight(src,b) for b in [(50,150,38,39),(100,150,38,39),(151,150,38,39)]],10,False)
add(src,'Effects','BossDoor','RetractBottom',[tight(src,b) for b in [(50,190,38,35),(100,190,38,35),(151,190,38,35)]],10,False)
# Explosion flash and two color variants of the smoke/debris cycle.
src='Assets/Recursos/Sprites/Effects/Explosion.gif'
add(src,'Effects','Explosion','Flash',[[4,5,65,64],[87,6,64,62]],20,False)
colorcells=[(165,10,57,61),(226,10,60,61),(288,10,61,61),(351,10,64,61),(420,10,67,61),(490,10,72,61),(567,10,71,61)]
colorcells += [(5,76,65,69),(72,76,64,69),(137,76,58,69),(196,76,53,69),(250,76,52,69),(303,76,48,69),(352,76,48,69),(401,76,52,69),(454,76,50,69)]
add(src,'Effects','Explosion','FireBlast',[tight(src,b) for b in colorcells if np.any(mask(src)[b[1]:b[1]+b[3],b[0]:b[0]+b[2]])],15,False)
smokecells=[(14,147,60,63),(77,147,60,63),(139,147,60,63),(202,147,65,63),(271,147,67,63),(342,147,72,63),(416,147,73,63)]
smokecells += [(12,214,59,65),(74,214,60,65),(137,214,61,65),(201,214,55,65),(259,214,47,65),(310,214,47,65),(360,214,47,65),(410,214,48,65),(462,214,48,65)]
add(src,'Effects','Explosion','SmokeBlast',[tight(src,b) for b in smokecells if np.any(mask(src)[b[1]:b[1]+b[3],b[0]:b[0]+b[2]])],15,False)
src='Assets/Recursos/Sprites/Effects/meteor1.gif'
add(src,'Effects','Meteor','FlameUp',[tight(src,(x,0,w,119)) for x,w in [(2,82),(85,80),(168,78),(247,77),(325,76),(403,77),(482,73),(555,74),(629,73)]],12)
add(src,'Effects','Meteor','FlameDown',[tight(src,(x,119,w,119)) for x,w in [(2,82),(85,80),(168,78),(247,77),(325,76),(403,77),(482,73),(555,74),(629,73)]],12)
src='Assets/Recursos/Sprites/Effects/Rain Effect.png'
add(src,'Effects','Rain','Fall',[[8,7,160,240],[177,7,160,240],[346,7,160,240],[8,255,160,240],[177,255,160,240]],14)
src='Assets/Recursos/Sprites/Effects/Ready Text - Stage Intro Screen.png'
add(src,'Effects','StageIntro','ReadyPulse',[[6,20,184,33],[6,54,184,33],[6,88,184,33],[6,54,184,33]],8)
# These rows visibly contain temporal rotation, sparkle or swelling frames.
src='Assets/Recursos/Sprites/Items/items.png'
for name,band,fps in [('BlueEnergyPulse',(60,0,270,34),12),('RedEnergyPulse',(60,37,270,39),12),('SmallPickupRotate',(120,79,155,30),12),('LargePickupRotate',(85,109,220,33),12),('CapsulePulse',(58,143,270,41),10),('SmallBlueOrbRotate',(115,191,160,27),12),('BlueOrbRotate',(20,219,350,35),12),('LargeBlueOrbRotate',(0,257,389,35),12),('PinkPickupRotate',(22,298,351,38),12),('EnergyTankRotate',(65,339,262,41),10),('LifeTankRotate',(65,384,262,41),10),('CorePulse',(115,430,153,35),10)]:
 add(src,'Items','Pickups',name,frames(src,band,gap=0,min_area=10),fps)
# Dragon full-body authored poses are kept separate from detached limbs.
src='Assets/Recursos/Sprites/Bosses/Dragon/Dragon.png'
poses={
'Idle':(105,15,80,95),'Guard':(288,18,90,95),'Windup':(104,143,104,79),'Slash':(207,143,99,87),'Sweep':(304,143,109,94),'JumpAttack':(524,120,76,119),'AttackRaised':(109,242,105,79),'CrouchAttack':(215,254,102,77),'Recover':(119,330,89,67),'SweepLow':(215,339,105,64),'KickHigh':(66,424,79,94),'KickForward':(140,429,91,89),'Turn':(250,426,88,96),'Lunge':(334,433,91,89),'Hurt':(105,573,108,75),'HeadlessBody':(214,565,109,85),'Crouch':(120,682,106,84),'CrouchShift':(224,682,107,84),'Airborne':(347,678,91,89)}
for name,b in poses.items():add(src,'Bosses','Dragon',name,[tight(src,b)],8,False)
# Detached limbs explicitly remain reusable parts rather than false body animations.
partcells={'Head':(210,5,38,28),'Torso':(196,35,71,25),'Legs':(194,58,73,56),'LeftFist':(173,48,25,28),'RightFist':(256,48,22,30),'DetachedArm':(540,15,56,44),'DetachedFoot':(551,81,30,35),'TorsoBack':(228,567,65,43),'LeftLowerBody':(329,583,44,55),'RightLowerBody':(379,580,42,56)}
for name,b in partcells.items():add(src,'Bosses','DragonParts',name,[tight(src,b)],8,False)
# Effect strips below the body include actual complete sequences; avoid palette swatches.
effectbands=[('SlashArc',(180,770,360,65),14,False),('SmallFlameUp',(255,834,205,34),12,False),('SmallFlameDown',(170,863,365,63),12,True),('FireballDown',(74,922,575,81),12,True),('FireballRight',(122,1004,472,71),12,True),('FireballDiagonal',(230,1078,242,61),12,True),('GroundFlameSmall',(150,1141,413,40),12,True),('GroundFlameMedium',(160,1180,399,35),12,True),('GroundFlameTall',(145,1210,422,61),12,True),('FlamePillar',(5,1269,688,84),12,False),('BlastWave',(180,1353,350,104),12,False),('Embers',(155,1460,384,40),14,False),('Impact',(132,1505,447,42),14,False),('ShockRing',(137,1547,449,34),14,False),('EnergyOrbs',(301,1598,145,74),12,False)]
for name,band,fps,loop in effectbands:
 rects=frames(src,band,gap=0,min_area=12,min_width=3,min_height=4)
 add(src,'Effects','DragonEffects',name,rects,fps,loop)
Path('Tools/AnimationPipeline/world_specs.json').write_text(json.dumps(specs,indent=2),encoding='utf8')
print('Clips',len(specs),'frames',sum(len(s['rects']) for s in specs))
for s in specs:print(s['unit'],s['name'],len(s['rects']))
