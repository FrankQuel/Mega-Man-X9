"""Magnify existing wall-contact frames without altering source art."""
import json
from pathlib import Path
from PIL import Image, ImageDraw
root=Path(__file__).resolve().parents[2]
data=json.loads((root/'Assets/Recursos/Editor/AnimationPipeline/animation_manifest.json').read_text(encoding='utf-8-sig'))
selections={'Blue':['WallJump','WallCling'],'BlueUltimate':['WallJump','WallCling'],'Red':['WallClimb','WallPushOff','JumpRise']}
for unit,names in selections.items():
    rows=[]
    for c in data['clips']:
        if c['unit']!=unit or c['name'] not in names:continue
        frames=c['frames'];strip=Image.open(root/c['texture']).convert('RGBA');cell=180
        row=Image.new('RGB',(max(800,len(frames)*cell),290),(28,31,33));d=ImageDraw.Draw(row)
        d.text((5,5),unit+'/'+c['name'],fill='white')
        for i,f in enumerate(frames):
            sprite=strip.crop((f['x'],strip.height-f['y']-f['height'],f['x']+f['width'],strip.height-f['y']))
            box=sprite.getbbox();sprite=sprite.crop(box);sprite=sprite.resize((sprite.width*4,sprite.height*4),Image.Resampling.NEAREST)
            row.paste(sprite,(i*cell+4,280-sprite.height),sprite);d.text((i*cell+5,25),str(i),fill='cyan')
        rows.append(row)
    out=Image.new('RGB',(max(r.width for r in rows),sum(r.height for r in rows)),(28,31,33));y=0
    for row in rows:out.paste(row,(0,y));y+=row.height
    out.save(root/f'Previews/RainyFoundry/WallFrames_{unit}.png')
