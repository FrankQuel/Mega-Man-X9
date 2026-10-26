import json,sys
from pathlib import Path
sys.path.insert(0,'Tools/AnimationPipeline')
from sheet_tools import frames
p=Path('Tools/AnimationPipeline/world_specs.json'); specs=json.loads(p.read_text())
src='Assets/Recursos/Sprites/Bosses/Dragon/Dragon.png'
for s in specs:
 if s['unit']=='BossDoor':s['category']='Environment'
 if s['unit']=='StageIntro':s['category']='UI'
 if s['unit']=='MiniLaserCannon' and s['name']=='LaserFire':
  s['excludeRects']=[[[0,44,94,1]],[[105,0,20,1]],[],[],[],[[0,51,93,1]],[[94,0,31,1]],[]]
 if s['unit']=='DragonEffects':
  bands={'SlashArc':(180,770,360,53),'SmallFlameUp':(255,823,205,38),'SmallFlameDown':(170,861,365,58),'GroundFlameSmall':(150,1136,413,31),'GroundFlameMedium':(160,1167,399,35),'GroundFlameTall':(145,1202,422,58),'FlamePillar':(5,1260,688,85)}
  if s['name'] in bands:s['rects']=frames(src,bands[s['name']],gap=0,min_area=8)
  if s['name']=='Impact':s['rects']=[[136,1509,66,29],[205,1504,91,35],[298,1504,95,35],[395,1500,88,41],[484,1499,95,39]]
  if s['name']=='SmallFlameUp':s['loop']=False
# Replace broad hand boxes with exact connected component bounds. Isolating alpha avoids neighboring offcuts.
specs=[s for s in specs if s['unit'] not in ['Dragon','DragonParts']]
poses={
'Idle':[111,21,61,81],'Guard':[292,26,81,79],'Windup':[110,151,95,68],'Slash':[211,150,94,73],'Sweep':[312,148,98,72],'JumpAttack':[527,126,65,104],'AttackRaised':[119,244,87,74],'CrouchAttack':[219,252,96,73],'Recover':[124,326,73,66],'SweepLow':[217,333,98,59],'KickHigh':[70,430,64,83],'KickForward':[141,438,81,74],'Turn':[256,434,73,84],'Lunge':[336,438,76,78],'Hurt':[108,579,90,62],'Crouch':[127,694,94,68],'CrouchShift':[226,695,96,67],'Airborne':[351,684,75,79]}
for name,r in poses.items():specs.append(dict(source=src,category='Bosses',unit='Dragon',name=name,rects=[r],fps=8,loop=False,isolateComponent=True))
specs.append(dict(source=src,category='Bosses',unit='Dragon',name='CrouchIdle',rects=[poses['Crouch'],poses['CrouchShift']],fps=6,loop=True,isolateComponent=True))
parts=json.loads(Path('Tools/AnimationPipeline/QA/World/dragon_components.json').read_text())
index=0
for r in parts:
 if r[1]>=770 or r[4]<40 or r[:4] in poses.values():continue
 index+=1
 specs.append(dict(source=src,category='Bosses',unit='DragonParts',name=f'Part_{index:02d}',rects=[r[:4]],fps=8,loop=False,isolateComponent=True))
# Single sparks/flames intentionally are not reconstructed into new body frames.
p.write_text(json.dumps(specs,indent=2),encoding='utf8')
print('FINAL',len(specs),'clips',sum(len(s['rects']) for s in specs),'frames')
