import sys,json
from pathlib import Path
sys.path.insert(0,'Tools/AnimationPipeline')
from sheet_tools import frames,annotate
sets={
'bird':[(0,0,169,41),(0,41,169,40),(0,81,169,43)],
'burnout':[(0,0,536,55),(0,55,536,55),(0,110,536,58)],
'spikeball':[(0,0,329,45),(0,45,329,48),(0,93,329,46)],
'minilasercannon':[(0,0,430,63),(0,63,430,62),(0,125,155,55),(155,125,135,55),(290,125,140,55),(0,180,155,60),(155,180,135,60),(0,240,155,55),(155,240,135,55),(0,295,155,59),(155,295,135,59)],
'grenademan':[(0,0,500,63),(0,63,280,69),(280,63,90,69),(0,132,500,68),(0,200,470,70),(470,200,30,70),(0,270,165,60),(165,270,210,60),(0,330,500,70)],
'gigadeath':[(0,0,95,56),(100,0,45,48),(150,0,128,48),(278,0,50,48),(0,57,70,40),(70,57,65,40),(135,48,145,42),(100,98,160,25),(50,123,130,30),(190,118,120,40),(20,155,150,30),(178,155,136,30),(90,187,145,30),(65,216,205,53),(115,269,97,71)],
}
allout={}
for name,bands in sets.items():
 src='Assets/Recursos/Sprites/Enemies/'+name+'.gif'
 data=[frames(src,b,gap=1,min_area=12) for b in bands]
 allout[name]=data
 annotate(src,[r for line in data for r in line],f'Tools/AnimationPipeline/QA/World/{name}_boxes.png',2)
print(json.dumps(allout))
Path('Tools/AnimationPipeline/QA/World/boxes.json').write_text(json.dumps(allout,indent=2))
