from pathlib import Path
from PIL import Image
import json
here=Path('Tools/AnimationPipeline');src=Image.open('SourceArt/Sprites/Bosses/Dragon/Dragon.png').convert('RGBA')
claw=src.crop((554,81,575,108));out=Image.new('RGBA',(claw.width+4,claw.height+4));out.alpha_composite(claw,(2,2));out.save(here/'dragon_rig_art/NearOpenClaw.png')
p=here/'dragon_rig.json';data=json.loads(p.read_text());data['openClawArtPath']='Tools/AnimationPipeline/dragon_rig_art/NearOpenClaw.png';data['openClawArtLocalPosition']=[.075,-.115,0];p.write_text(json.dumps(data,indent=2),encoding='utf8')
