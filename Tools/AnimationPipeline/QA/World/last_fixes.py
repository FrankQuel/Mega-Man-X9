import json
from pathlib import Path
p=Path('Tools/AnimationPipeline/world_specs.json');s=json.loads(p.read_text()); c=json.loads(Path('Tools/AnimationPipeline/QA/World/dragon_components.json').read_text())
for a in s:
 if a['unit']=='DragonEffects' and a['name'] in ['SmallFlameUp','SmallFlameDown']:
  lo,hi=(820,866) if a['name']=='SmallFlameUp' else (866,919)
  a['rects']=[r[:4] for r in sorted(c,key=lambda r:r[0]) if lo<=r[1]<hi and r[4]>30]
  a['isolateComponent']=True
 if a['unit']=='Rain':a['keyColors']=[[24,24,24]]
p.write_text(json.dumps(s,indent=2),encoding='utf8')
# Validate all rectangles are nonempty and stay inside their original source image.
from PIL import Image
for a in s:
 w,h=Image.open(a['source']).size
 for x,y,rw,rh in a['rects']:
  assert x>=0 and y>=0 and rw>0 and rh>0 and x+rw<=w and y+rh<=h,(a['unit'],a['name'],[x,y,rw,rh],w,h)
print(len(s),'entries',sum(len(a['rects']) for a in s),'frames; all rectangles valid')
# Final Meteor boundaries: frames cross nominal column intervals; use complete alpha components.
import sys
sys.path.insert(0,'Tools/AnimationPipeline/QA/World')
from components import components
src='Assets/Recursos/Sprites/Effects/meteor1.gif'; c=components(src)
for a in s:
 if a['unit']=='Meteor':
  a['rects']=[r[:4] for r in sorted(c,key=lambda r:r[0]) if r[4]>1000 and ((r[1]+r[3]/2<119)==(a['name']=='FlameUp'))]
  a['isolateComponent']=True
p.write_text(json.dumps(s,indent=2),encoding='utf8')
