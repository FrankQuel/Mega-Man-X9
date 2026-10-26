import sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from sheet_tools import *
from PIL import ImageDraw
P='Assets/Recursos/Sprites/Player/Robot futuristico rojo/'
sources={
'main':('robot futuristico rojo principal.gif',[(0,0,622,88),(0,95,622,58),(0,164,622,65),(0,230,622,80),(0,320,622,49),(0,384,622,49),(0,440,622,63),(0,514,622,46),(0,565,622,46),(0,620,622,47),(0,676,622,69),(0,748,622,68),(0,828,622,80),(0,912,622,69),(0,988,622,48),(0,1042,622,70),(0,1117,622,69),(0,1199,622,80),(0,1294,622,69),(0,1372,622,57),(0,1433,622,57),(0,1494,622,74)]),
'extra1':('robot futuristico rojo extra 1.gif',[(0,3,580,41),(0,55,580,41),(0,102,580,32),(0,155,480,70),(0,229,580,69),(0,302,580,68),(0,374,580,69)]),
'extra2':('robot futuristico rojo extra 2.gif',[(0,21,647,77),(0,109,647,78),(0,201,380,90),(0,195,647,185),(0,393,540,94)]),
'extra3':('robot futuristico rojo extra 3.gif',[(0,a,650,b-a) for a,b in runs(mask(P+'robot futuristico rojo extra 3.gif').any(axis=1),2)]),
'special':('ataques especiales del robot futuristico rojo.gif',[(0,6,80,109),(80,0,176,68),(80,68,176,48),(80,124,176,62),(48,191,208,48)])}
result={}
for key,(filename,bands) in sources.items():
    source=P+filename
    image=rgba(source).convert('RGB'); draw=ImageDraw.Draw(image)
    result[key]=[]
    for row,band in enumerate(bands):
        rects=frames(source,band,gap=2,min_area=35)
        result[key].append(rects)
        for col,(x,y,w,h) in enumerate(rects):
            draw.rectangle((x,y,x+w-1,y+h-1),outline='black',width=1)
            draw.text((x,y),f'{row}:{col}',fill='white',stroke_width=1,stroke_fill='black')
    image.save(ROOT/'Tools/AnimationPipeline/QA/Red'/f'{key}_annotated.png')
(ROOT/'Tools/AnimationPipeline/QA/Red/rows.json').write_text(json.dumps(result,indent=2))
print(json.dumps({key:[len(r) for r in rows] for key,rows in result.items()}))
