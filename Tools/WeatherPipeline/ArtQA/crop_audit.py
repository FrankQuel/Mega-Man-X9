from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
base=Path(r'C:\Users\hallo\Mega Man X9\Assets\Recursos\Sprites\Environment')
out=Path(r'C:\Users\hallo\Mega Man X9\Tools\WeatherPipeline\ArtQA')
regions={
'edificios.png': [('building_small',(0,0,420,440)),('building_mid',(420,0,930,440)),('building_twin_tank',(930,0,1448,440)),('building_comm_tower',(0,440,375,1086)),('building_factory',(375,440,1040,1086)),('building_tall',(1040,440,1448,1086))],
'pinchos.png':[('spikes_single_up',(30,90,210,280)),('spikes_three_up',(220,90,550,280)),('spikes_long_up',(560,90,1255,280)),('spikes_long_down',(560,300,1175,520)),('spikes_three_down',(220,300,555,520))],
'postes, pinchos y edificios extra.png':[('extra_garage',(25,230,300,550)),('extra_banner',(300,220,515,550)),('extra_tower',(515,100,730,550)),('extra_wide',(735,190,1055,550)),('extra_tall',(1055,0,1360,550)),('extra_pole',(1370,0,1510,550)),('lamp_short',(40,740,180,1010)),('lamp_tall',(180,720,320,1010)),('lamp_banner',(360,725,480,1010)),('spikes_four',(35,580,225,710)),('spikes_six',(225,580,445,710)),('spikes_down',(1140,560,1370,710))]
}
for name, entries in regions.items():
 im=Image.open(base/name); a=np.asarray(im); draw=ImageDraw.Draw(im)
 print(name,im.size)
 for label, (x0,y0,x1,y1) in entries:
  tile=a[y0:y1,x0:x1]; yy,xx=np.where(np.min(tile,axis=2)<220)
  x=x0+int(xx.min()); y=y0+int(yy.min()); r=x0+int(xx.max())+1; b=y0+int(yy.max())+1
  # Conservative 2px margin captures pale antialiasing and preserves native complete outline.
  x=max(0,x-2);y=max(0,y-2);r=min(im.width,r+2);b=min(im.height,b+2)
  rect=(x,y,r,b)
  print(label,'x y w h =',x,y,r-x,b-y,'LTRB',rect)
  Image.open(base/name).crop(rect).save(out/(label+'.png'))
  draw.rectangle((x,y,r-1,b-1),outline='magenta',width=2); draw.text((x,y),label,fill='magenta')
 im.save(out/(Path(name).stem+'_audit.png'))
