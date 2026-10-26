from pathlib import Path
from PIL import Image,ImageDraw
import numpy as np
src=Image.open('SourceArt/Sprites/Bosses/Dragon/Dragon.png').convert('RGBA')
ref=np.array(src.crop((111,21,172,102)))
def match(rect):
 x,y,w,h=rect;a=np.array(src.crop((x,y,x+w,y+h))); opaque=a[:,:,3]>0
 best=(-1,None)
 for oy in range(-15,81):
  for ox in range(-20,62):
   xs,ys=max(0,ox),max(0,oy); xe,ye=min(61,ox+w),min(81,oy+h)
   if xe<=xs or ye<=ys:continue
   part=a[ys-oy:ye-oy,xs-ox:xe-ox];target=ref[ys:ye,xs:xe]
   m=part[:,:,3]>0
   same=np.all(part[:,:,:3]==target[:,:,:3],axis=2)&m&(target[:,:,3]>0)
   score=same.sum()/opaque.sum()
   if score>best[0]:best=(score,(ox,oy))
 return best
for name,rect in [('Head',(214,8,24,22)),('Torso',(198,34,57,23)),('Legs',(196,64,61,46)),('FistL',(178,51,16,20)),('FistR',(259,54,14,21))]:print(name,match(rect))
