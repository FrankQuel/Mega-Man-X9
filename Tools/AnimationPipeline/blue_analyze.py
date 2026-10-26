from pathlib import Path
import sys,json
sys.path.insert(0,str(Path('Tools/AnimationPipeline').resolve()))
from sheet_tools import frames,rgba
from PIL import Image,ImageDraw
base='Assets/Recursos/Sprites/Player/Robot futuristico azul/'
sets={
'Main':('robot futuristico azul principal.png',[(0,14,590,96),(0,113,590,59),(0,174,590,46),(0,229,590,57),(0,297,590,45),(0,347,590,78),(0,427,590,47),(0,483,590,47),(0,540,590,44),(0,592,590,57),(0,658,590,56),(0,717,590,46),(0,770,590,45),(0,820,590,46),(0,871,590,47),(0,927,590,43),(0,984,590,44),(0,1038,590,62),(0,1104,590,56),(0,1163,590,82),(0,1249,590,59),(0,1310,590,60),(0,1376,590,80)]),
'Ultimate':('armadura definitiva del robot futuristico azul.png',[(0,6,520,44),(0,56,520,53),(0,115,520,43),(0,164,520,70),(0,235,520,43),(0,284,520,46),(0,335,520,43),(0,381,520,58),(0,441,520,50),(0,498,520,53),(0,556,520,51),(0,610,520,44),(0,657,520,43),(0,704,520,45),(0,755,520,45),(0,807,520,39),(0,853,520,44),(0,905,520,56),(0,964,520,57),(0,1022,520,47),(0,1074,520,53),(0,1131,520,54),(0,1189,520,70),(0,1264,520,59),(0,1335,520,52),(0,1389,520,71),(0,1472,520,61),(0,1536,520,70),(0,1627,520,60),(0,1705,520,65),(0,1777,520,52),(0,1837,520,56),(0,1903,520,54),(0,1966,520,58)]),
'Extra':('robot futuristico azul extra.png',[(0,0,420,46),(0,50,420,42),(0,100,420,48),(0,154,420,56),(0,223,420,61),(0,295,420,45),(0,340,420,59),(0,423,420,30)])}
allrows={}
for label,(file,bands) in sets.items():
 source=base+file;im=rgba(source)
 rows=[]
 for i,b in enumerate(bands):
  rs=frames(source,b,gap=1,min_area=20)
  rows.append(rs)
  canvas=Image.new('RGBA',(im.width,b[3]+22),(60,60,60,255));canvas.alpha_composite(im.crop((0,b[1],im.width,b[1]+b[3])),(0,22));d=ImageDraw.Draw(canvas)
  for j,(x,y,w,h) in enumerate(rs):
   d.rectangle((x,y-b[1]+22,x+w-1,y-b[1]+h+21),outline='#ff0088');d.text((x,0),str(j),fill='white')
  canvas=canvas.resize((canvas.width*2,canvas.height*2),Image.Resampling.NEAREST)
  canvas.convert('RGB').save(Path('Tools/AnimationPipeline/QA/Blue')/f'{label}_row{i:02d}.png')
  print(label,i,b[1],len(rs),rs)
 allrows[label]={'source':source,'bands':bands,'rows':rows}
Path('Tools/AnimationPipeline/QA/Blue/candidate_rows.json').write_text(json.dumps(allrows,indent=2))
