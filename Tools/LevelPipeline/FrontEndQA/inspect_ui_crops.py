from PIL import Image,ImageDraw
from pathlib import Path
root=Path.cwd()
out=root/'Tools/LevelPipeline/FrontEndQA'
canvas=Image.new('RGB',(900,500),(22,28,38))
source=Image.open(root/'SourceArt/UI/HUD/HUD Elements.png').convert('RGBA')
for i,(name,box) in enumerate([('Blue X native life emblem',(68,177,99,205)),('Red Z native life emblem',(239,209,273,237))]):
    icon=source.crop(box);canvas.paste(icon.resize((icon.width*6,icon.height*6),Image.Resampling.NEAREST),(30+i*440,35),icon.resize((icon.width*6,icon.height*6),Image.Resampling.NEAREST));ImageDraw.Draw(canvas).text((30+i*440,220),name,fill='white')
source=Image.open(root/'Assets/Recursos/UI/Menus/Character Select/Character Select.png').convert('RGBA')
for i,box in enumerate([(20,70,180,205),(180,70,340,205)]):
    icon=source.crop(box);canvas.paste(icon.resize((240,203),Image.Resampling.NEAREST),(30+i*440,275))
canvas.save(out/'existing_ui_art_crops.png')
