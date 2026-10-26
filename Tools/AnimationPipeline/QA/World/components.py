import sys,json
import numpy as np
from PIL import Image,ImageDraw
from collections import deque
sys.path.insert(0,'Tools/AnimationPipeline')
from sheet_tools import mask,annotate

def components(src,dilate=0,minarea=8):
 m=mask(src); orig=m.copy()
 for _ in range(dilate):
  pad=np.pad(m,1)
  m=np.logical_or.reduce([pad[dy:dy+m.shape[0],dx:dx+m.shape[1]] for dy in range(3) for dx in range(3)])
 seen=np.zeros(m.shape,bool); out=[]
 for y,x in zip(*np.nonzero(m)):
  if seen[y,x]:continue
  q=deque([(y,x)]); seen[y,x]=True; pts=[]
  while q:
   a,b=q.popleft()
   if orig[a,b]:pts.append((a,b))
   for dy,dx in [(-1,0),(1,0),(0,-1),(0,1),(-1,-1),(-1,1),(1,-1),(1,1)]:
    c,d=a+dy,b+dx
    if c>=0 and d>=0 and c<m.shape[0] and d<m.shape[1] and m[c,d] and not seen[c,d]:seen[c,d]=True;q.append((c,d))
  if len(pts)<minarea:continue
  ys,xs=zip(*pts);out.append([min(xs),min(ys),max(xs)-min(xs)+1,max(ys)-min(ys)+1,len(pts)])
 return sorted([list(map(int,r)) for r in out],key=lambda r:(r[1]//20,r[0]))
if __name__=='__main__':
 for name in ['grenademan','burnout','spikeball','bird','minilasercannon']:
  src='Assets/Recursos/Sprites/Enemies/'+name+'.gif'
  rects=components(src,0)
  print(name,json.dumps(rects))
  annotate(src,[r[:4] for r in rects],f'Tools/AnimationPipeline/QA/World/{name}_components.png',2)

