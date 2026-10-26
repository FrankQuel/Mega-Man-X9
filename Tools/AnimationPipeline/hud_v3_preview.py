"""Inspect native HUD extraction and quiet Ultimate idle without running Unity."""
from pathlib import Path
from PIL import Image, ImageDraw
import json

ROOT = Path(__file__).resolve().parents[2]
QA = ROOT / 'Tools/AnimationPipeline/QA'
QA.mkdir(parents=True, exist_ok=True)
source = Image.open(ROOT/'Assets/Recursos/UI/HUD/hud.png').convert('RGBA')
elements = Image.open(ROOT/'SourceArt/UI/HUD/HUD Elements.png').convert('RGBA')

def player(x):
    frame = Image.new('RGBA', (35, 52))
    out = 0
    for y in range(27, 81):
        if y in (37, 54): continue
        frame.paste(source.crop((x, y, x+35, y+1)), (0, out))
        out += 1
    frame.paste((0, 0, 0, 0), (9, 4, 14, 32))
    return frame

def boss():
    frame = Image.new('RGBA', (17, 49))
    out = 0
    for y in range(27, 65):
        if y in (37, 54): continue
        frame.paste(source.crop((11, y, 23, y+1)), (2, out))
        out += 1
    frame.paste(source.crop((117, 133, 134, 146)), (0, 36))
    frame.paste((0, 0, 0, 0), (5, 4, 10, 32))
    return frame

def indicator(x):
    frame = elements.crop((x, 339, x+24, 363))
    for x,y,w in ((23,16,1),(23,17,1),(22,18,2),(21,19,3),(20,20,4),(19,21,5),(18,22,6),(16,23,8)):
        frame.paste((0,0,0,0),(x,y,x+w,y+1))
    return frame

frames = [('Blue / Ultimate', player(5), False), ('Red', player(45), False), ('White / Dragon', boss(), True)]
native = Image.new('RGBA', (190, 70), (19, 27, 42, 255))
for i,(_,frame,_) in enumerate(frames): native.alpha_composite(frame, (10+i*62, 8))
native.resize((1140, 420), Image.Resampling.NEAREST).save(QA/'hud_v3_frames.png')
canvas = Image.new('RGBA', (1040, 1120), (18, 25, 39, 255))
draw = ImageDraw.Draw(canvas)
for row,(label,frame,is_boss) in enumerate(frames):
    for col,value in enumerate((28,18,7,0)):
        art = frame.copy()
        x = 5 if is_boss else 9
        art.paste((14,19,31,255), (x,4,x+5,32))
        if value:
            fill = elements.crop((68,38,73,39)).resize((5,value), Image.Resampling.NEAREST)
            art.alpha_composite(fill,(x,32-value))
        icon = indicator(200 if value<=7 else 120 if value<=14 else 38)
        size = 11 if is_boss else 12
        art.alpha_composite(icon.resize((size,size), Image.Resampling.NEAREST),(3 if is_boss else 20,37))
        art = art.resize((art.width*6, art.height*6), Image.Resampling.NEAREST)
        xx=col*260+(260-art.width)//2; yy=row*370+32
        canvas.alpha_composite(art,(xx,yy))
        draw.text((col*260+30,row*370+6),f'{label}   {value}/28',fill='white')
canvas.save(QA/'hud_v3_examples.png')
specs=json.loads((ROOT/'Tools/AnimationPipeline/blue_specs.json').read_text())
idle=next(s for s in specs if s['unit']=='BlueUltimate' and s['name']=='Idle')
sheet=Image.open(ROOT/'SourceArt/Sprites/Player/Robot futuristico azul/armadura definitiva del robot futuristico azul.png').convert('RGBA')
idle_canvas=Image.new('RGBA',(540,230),(18,25,39,255));d=ImageDraw.Draw(idle_canvas)
for i,(x,y,w,h) in enumerate(idle['rects']):
    frame=sheet.crop((x,y,x+w,y+h)).resize((w*4,h*4),Image.Resampling.NEAREST)
    idle_canvas.alpha_composite(frame,(i*180+(180-frame.width)//2,35))
    d.text((i*180+30,10),f'Idle {i+1} / brazos bajos',fill='white')
idle_canvas.save(QA/'hud_v3_ultimate_idle_fixed.png')
print('HUD frame extraction and three lowered-arm Idle poses previewed.')
