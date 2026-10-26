import json
import numpy as np
from pathlib import Path
from PIL import Image,ImageDraw
src='Assets/Recursos/UI/HUD/HUD Elements.png'
im=Image.open(src).convert('RGBA')
specs=[]
for name,x,x2 in [('NormalPulse',38,39),('CautionPulse',120,120),('DangerPulse',200,200),('VirusPulse',288,288)]:
 rects=[[x,339,24,24],[x2,413 if name=='NormalPulse' else 376,24,24]]
 exclusions=[]
 for rx,ry,w,h in rects:
  a=np.array(im.crop((rx,ry,rx+w,ry+h)))
  native=a[:,:,3]>0
  silhouette=np.concatenate([native[:12],native[:12][::-1]],axis=0)
  extras=native & ~silhouette
  boxes=[]
  for y in range(h):
   start=None
   for col,yes in enumerate(list(extras[y])+[False]):
    if yes and start is None:start=col
    elif not yes and start is not None:boxes.append([start,y,col-start,1]);start=None
  exclusions.append(boxes)
 specs.append(dict(source=src,category='UI',unit='StatusHUD',name=name,rects=rects,fps=4,loop=True,keyColors=[],excludeRects=exclusions,notes=('Dos estados de brillo originales; Normal usa la variante inferior rotulada fixed como estado luminoso corregido. ' if name=='NormalPulse' else 'Dos estados de brillo originales. ')+'Recorte solo del indicador circular, sin texto. Se retira únicamente la extensión rectangular de la etiqueta fuera de la silueta circular nativa; se conserva alfa y color originales del círculo.'))
Path('Tools/AnimationPipeline/hud_specs.json').write_text(json.dumps(specs,indent=2),encoding='utf8')
out=Image.new('RGBA',(440,140),(22,29,45,255));d=ImageDraw.Draw(out)
for i,s in enumerate(specs):
 d.text((i*110,2),s['name'],fill='white')
 for j,(r,ex) in enumerate(zip(s['rects'],s['excludeRects'])):
  x,y,w,h=r;a=im.crop((x,y,x+w,y+h))
  for rx,ry,rw,rh in ex:a.paste((0,0,0,0),(rx,ry,rx+rw,ry+rh))
  out.alpha_composite(a.resize((48,48),Image.Resampling.NEAREST),(i*110+j*50,30))
out.save('Tools/AnimationPipeline/QA/World/hud_pulses_verified.png')
print('Guardados 4 clips, 8 fotogramas')
