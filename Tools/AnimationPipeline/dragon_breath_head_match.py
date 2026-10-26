from PIL import Image
import numpy as np
im=Image.open('SourceArt/Sprites/Bosses/Dragon/Dragon.png').convert('RGBA');ref=np.array(im.crop((219,252,315,325)))
for label,(x,y,w,h) in [('HornHead',(456,259,22,28)),('OpenMouth',(453,290,33,37))]:
 a=np.array(im.crop((x,y,x+w,y+h))); opaque=a[:,:,3]>0;best=(-1,None)
 for oy in range(-10,74):
  for ox in range(-10,97):
   xs,ys=max(0,ox),max(0,oy);xe,ye=min(96,ox+w),min(73,oy+h)
   if xe<=xs or ye<=ys:continue
   p=a[ys-oy:ye-oy,xs-ox:xe-ox];t=ref[ys:ye,xs:xe];m=p[:,:,3]>0
   score=np.sum(np.all(p[:,:,:3]==t[:,:,:3],axis=2)&m&(t[:,:,3]>0))/opaque.sum()
   if score>best[0]:best=(score,(ox,oy))
 print(label,best)
