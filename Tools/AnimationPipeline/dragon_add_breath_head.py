from pathlib import Path
from PIL import Image
import json

here = Path(__file__).resolve().parent
root = here.parents[1]
image = Image.open(root / 'SourceArt/Sprites/Bosses/Dragon/Dragon.png').convert('RGBA')
head = Image.new('RGBA', (36, 53))
head.alpha_composite(image.crop((456, 259, 478, 287)), (0, 0))
head.alpha_composite(image.crop((453, 290, 486, 327)), (3, 16))
art = Image.new('RGBA', (40, 57))
art.alpha_composite(head, (2, 2))
art.save(here / 'dragon_rig_art/OpenMouthHead.png')
path = here / 'dragon_rig.json'
data = json.loads(path.read_text())
data['openMouthArtPath'] = 'Tools/AnimationPipeline/dragon_rig_art/OpenMouthHead.png'
data['openMouthArtLocalPosition'] = [.04, .045, 0]
data['breathMouthLocalPosition'] = [-.03, .05, 0]
path.write_text(json.dumps(data, indent=2), encoding='utf8')
