import sys,json
from pathlib import Path
from PIL import Image,ImageDraw
sys.path.insert(0,'Tools/AnimationPipeline')
from sheet_tools import rgba
specs=json.loads(Path('Tools/AnimationPipeline/world_specs.json').read_text())
for unit in sorted(set(s['unit'] for s in specs)):
 clips=[s for s in specs if s['unit']==unit]
 out=Image.new('RGB',(1100,len(clips)*150),(28,32,45));d=ImageDraw.Draw(out)
 for line,s in enumerate(clips):
  d.text((4,line*150+4),s['name'],fill='white')
  scale=min(2,1000/sum(r[2]+4 for r in s['rects']),116/max(r[3] for r in s['rects']))
  x=10
  for i,r in enumerate(s['rects']):
   a,b,w,h=r;im=rgba(s['source']).crop((a,b,a+w,b+h)); im=im.resize((max(1,int(w*scale)),max(1,int(h*scale))),Image.Resampling.NEAREST)
   out.paste(im,(int(x),line*150+27+int(116-h*scale)),im);d.text((int(x),line*150+17),str(i),fill='white');x+=im.width+8
 out.save(Path('Tools/AnimationPipeline/QA/World')/(unit+'_clips.png'))
