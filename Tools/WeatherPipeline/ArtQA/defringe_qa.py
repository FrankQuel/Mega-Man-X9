exec(open(r'C:\Users\hallo\Mega Man X9\Tools\WeatherPipeline\ArtQA\mask_qa.py',encoding='utf-8-sig').read().split('# inspect remaining')[0])
# All known actual-background seeds are absolute source coordinates.
seed_map={
'small':[(211,260),(235,260),(258,260),(88,369)],
'medium':[(485,350),(865,350)],
 'twinTank':[(1167,190),(992,320),(1349,320)],
 'tall':[(1128,688),(1280,850)],
 'lampShort':[(108,842),(84,844)],'lampTall':[(237,787)]}
factory_seeds=[(804,755),(904,827),(408,828),(428,829),(452,829),(434,859),(415,884),(452,883),(432,912),(431,945),(414,965),(446,965),(430,983)]
entries.append(('factory',E,(376,551,652,462),factory_seeds))
positions=[(15,25),(410,25),(930,25),(15,430),(360,430),(645,430),(875,430),(1015,430),(15,1060),(720,1060),(720,1200)]
canvas=Image.new('RGBA',(1600,1690),(24,33,47,255));draw=ImageDraw.Draw(canvas)
for (label,name,(x,y,w,h),seeds),(px,py) in zip(entries,positions):
 seeds=seed_map.get(label,seeds)
 a=np.array(Image.open(base/name))[y:y+h,x:x+w];rgba,_,_=remove_bg(a,[(sx-x,sy-y) for sx,sy in seeds]); op=rgba[:,:,3]>0
 # Snapshot original alpha, 8-connected opaque edge adjacent to alpha0 or crop exterior.
 padded=np.pad(op,1,constant_values=False); all_neighbors=np.ones_like(op)
 for dy in range(3):
  for dx in range(3):
   if dx==1 and dy==1:continue
   all_neighbors &= padded[dy:dy+h,dx:dx+w]
 edge=op&(~all_neighbors)
 rgb=rgba[:,:,:3].astype(float); mn=rgb.min(2);chroma=rgb.max(2)-mn
 fix=edge&(mn>100)&(chroma<35)
 alpha=np.clip((255-mn[fix])/(255-32),.02,1)
 fixed=np.clip((rgb[fix]-255*(1-alpha[:,None]))/alpha[:,None],0,255)
 rgba[fix,:3]=np.rint(fixed).astype('uint8');rgba[fix,3]=np.rint(rgba[fix,3]*alpha).astype('uint8')
 Image.fromarray(rgba).save(out/(label+'_defringe_QA.png'))
 canvas.alpha_composite(Image.fromarray(rgba),(px,py));draw.text((px,py-15),label,fill='white')
 print(label,'edge pixels',int(edge.sum()),'defringed',int(fix.sum()))
canvas.convert('RGB').save(out/'masked_defringe_preview.png')

