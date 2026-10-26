from pathlib import Path
p=Path('Tools/AnimationPipeline/dragon_rig_author.py');s=p.read_text()
insert=''' elif name=='CombatIdle':
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
'''
s=s.replace(" elif name=='Hurt':",insert+" elif name=='Hurt':")
s=s.replace("('FireBreath',1.4,False)","('FireBreath',1.8,False)")
s=s.replace("('Death',1.2,False)]","('Death',1.2,False),('CombatIdle',1.8,True),('Fireball',1.35,False),('RisingFlame',1.3,False),('FlamingKick',1.35,False),('GroundFireWave',1.6,False),('HurtFlash',.6,False),('Landing',.45,False)]")
p.write_text(s)
