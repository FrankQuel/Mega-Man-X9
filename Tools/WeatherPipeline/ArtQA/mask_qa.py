from pathlib import Path
from collections import deque
from PIL import Image,ImageDraw,ImageFont
import numpy as np
base=Path(r'C:\Users\hallo\Mega Man X9\Assets\Recursos\Sprites\Environment')
out=Path(r'C:\Users\hallo\Mega Man X9\Tools\WeatherPipeline\ArtQA')
E='edificios.png'; X='postes, pinchos y edificios extra.png'; S='pinchos.png'
entries=[('small',E,(46,161,358,262),[]),('medium',E,(432,62,483,361),[]),('twinTank',E,(933,79,475,344),[(1167,190)]),('tall',E,(1044,458,301,555),[]),('extraGarage',X,(43,281,245,255),[]),('extraTower',X,(527,123,194,413),[]),('lampShort',X,(66,812,101,179),[(111,852)]),('lampTall',X,(193,762,112,229),[(247,794)]),('spikesMainLong',S,(577,112,663,151),[]),('extrasFour',X,(52,628,162,64),[])]
def remove_bg(a,seeds=[]):
 eligible=(a.min(2)>=230)&((a.max(2).astype(int)-a.min(2))<=24)
 h,w=eligible.shape; mask=np.zeros((h,w),bool); q=deque()
 for x in range(w):
  for y in (0,h-1):
   if eligible[y,x] and not mask[y,x]:mask[y,x]=True;q.append((x,y))
 for y in range(h):
  for x in (0,w-1):
   if eligible[y,x] and not mask[y,x]:mask[y,x]=True;q.append((x,y))
 for x,y in seeds:
  if eligible[y,x] and not mask[y,x]:mask[y,x]=True;q.append((x,y))
 while q:
  x,y=q.popleft()
  for nx,ny in ((x-1,y),(x+1,y),(x,y-1),(x,y+1)):
   if 0<=nx<w and 0<=ny<h and eligible[ny,nx] and not mask[ny,nx]:mask[ny,nx]=True;q.append((nx,ny))
 rgba=np.dstack([a,np.where(mask,0,255).astype('uint8')]);return rgba,eligible,mask
# inspect remaining eligible connected components in factory; white chrome also appears, so report all labels on QA.
x,y,w,h=(376,551,652,462);a=np.array(Image.open(base/E))[y:y+h,x:x+w];rgba,eligible,mask=remove_bg(a)
rem=eligible&(~mask); seen=np.zeros_like(rem); comps=[]
for yy,xx in zip(*np.where(rem)):
 if seen[yy,xx]:continue
 q=deque([(xx,yy)]);seen[yy,xx]=True;pts=[]
 while q:
  xx2,yy2=q.popleft();pts.append((xx2,yy2))
  for nx,ny in ((xx2-1,yy2),(xx2+1,yy2),(xx2,yy2-1),(xx2,yy2+1)):
   if 0<=nx<w and 0<=ny<h and rem[ny,nx] and not seen[ny,nx]:seen[ny,nx]=True;q.append((nx,ny))
 if len(pts)>=15:
  pts=np.array(pts); comps.append((len(pts),tuple((pts.mean(0)+[x,y]).astype(int)),tuple((pts.min(0)+[x,y]).astype(int)),tuple((pts.max(0)+[x,y]).astype(int))))
comps.sort(reverse=True)
factory=Image.fromarray(rgba).convert('RGBA');bg=Image.new('RGBA',factory.size,(25,34,48,255));bg.alpha_composite(factory);d=ImageDraw.Draw(bg)
for i,(n,seed,p0,p1) in enumerate(comps):
 sx,sy=seed;d.rectangle((p0[0]-x,p0[1]-y,p1[0]-x,p1[1]-y),outline=(255,0,255),width=1);d.text((sx-x,sy-y),str(i),fill=(255,0,255));print('factory component',i,'pixels',n,'mean',seed,'bounds',p0,p1)
bg.save(out/'factory_remaining_components.png')
# tile native-sized selected assets on dark gray QA. Image crop/QA only, source unchanged.
canvas=Image.new('RGBA',(1600,1600),(24,33,47,255));draw=ImageDraw.Draw(canvas)
positions=[(15,25),(410,25),(930,25),(15,430),(360,430),(645,430),(875,430),(1015,430),(15,1060),(720,1060)]
for (label,name,(x,y,w,h),seeds),(px,py) in zip(entries,positions):
 a=np.array(Image.open(base/name))[y:y+h,x:x+w];rgba,_,_=remove_bg(a,[(sx-x,sy-y) for sx,sy in seeds]);im=Image.fromarray(rgba);canvas.alpha_composite(im,(px,py));draw.text((px,py-15),label,fill='white')
canvas.convert('RGB').save(out/'masked_preview.png')
