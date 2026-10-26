"""Export a few small animated previews directly from the generated frame strips."""
from pathlib import Path
from PIL import Image,ImageDraw
import json
ROOT=Path(__file__).resolve().parents[2]
manifest=json.loads((ROOT/'Assets/Recursos/Editor/AnimationPipeline/animation_manifest.json').read_text(encoding='utf-8'))
selections=[('Blue','Run'),('BlueUltimate','ChargeShot'),('Red','GroundSlash1'),('SpikeBall','Walk'),('Axl','Run')]
out=ROOT/'Previews';out.mkdir(exist_ok=True)
for unit,name in selections:
    entry=next(c for c in manifest['clips'] if c['unit']==unit and c['name']==name)
    sheet=Image.open(ROOT/entry['texture']).convert('RGBA');frames=[]
    for f in entry['frames']:
        x,y,w,h=f['x'],sheet.height-f['y']-f['height'],f['width'],f['height']
        im=sheet.crop((x,y,x+w,y+h))
        scale=min(4,280//max(w,h));scale=max(1,scale)
        im=im.resize((w*scale,h*scale),Image.Resampling.NEAREST)
        canvas=Image.new('RGB',(max(360,im.width+30),max(260,im.height+60)), '#161d2d')
        canvas.paste(im,((canvas.width-im.width)//2,canvas.height-im.height-20),im)
        ImageDraw.Draw(canvas).text((12,12),unit+' / '+name,fill='white')
        frames.append(canvas)
    frames[0].save(out/f'{unit}_{name}.gif',save_all=True,append_images=frames[1:],duration=round(1000/entry['frameRate']),loop=0,disposal=2)
print('Animated previews:',len(selections))
