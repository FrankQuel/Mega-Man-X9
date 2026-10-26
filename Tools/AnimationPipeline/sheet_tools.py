"""Read-only sprite-sheet inspection helpers. Coordinates use top-left origin."""
from pathlib import Path
from collections import Counter
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]

def rgba(source):
    path=ROOT/source
    if not path.exists():path=ROOT/'SourceArt'/str(source).removeprefix('Assets/Recursos/')
    return Image.open(path).convert('RGBA')

def background(source):
    a = np.array(rgba(source))
    counts = Counter(map(tuple, a.reshape(-1, 4)))
    return counts.most_common(8)

def mask(source, key_colors=None):
    a = np.array(rgba(source))
    m = a[:, :, 3] > 0
    if key_colors is None and m.all():
        key_colors = [background(source)[0][0][:3]]
    for color in key_colors or []:
        m &= np.any(a[:, :, :3] != color[:3], axis=2)
    return m

def runs(values, gap=0):
    out = []
    start = None
    for i, value in enumerate(list(values) + [False]):
        if value and start is None:
            start = i
        elif not value and start is not None:
            if out and start - out[-1][1] <= gap:
                out[-1][1] = i
            else:
                out.append([start, i])
            start = None
    return out

def frames(source, band, key_colors=None, gap=2, min_area=30, min_width=3, min_height=4):
    """Return tight boxes left-to-right inside manually verified (x,y,w,h) band."""
    m = mask(source, key_colors)
    x,y,w,h = band
    a = m[y:y+h,x:x+w]
    boxes=[]
    for l,r in runs(a.any(axis=0),gap):
        ys,xs=np.nonzero(a[:,l:r])
        if len(ys)<min_area or r-l<min_width or ys.max()-ys.min()+1<min_height:
            continue
        boxes.append([int(x+l),int(y+ys.min()),int(r-l),int(ys.max()-ys.min()+1)])
    return boxes

def annotate(source, rects, output, scale=1):
    im=rgba(source).convert('RGB')
    d=ImageDraw.Draw(im)
    for i,(x,y,w,h) in enumerate(rects):
        d.rectangle((x,y,x+w-1,y+h-1),outline='#ff0088',width=1)
        d.text((x,y-10),str(i),fill='#ff0088')
    if scale!=1:
        im=im.resize((im.width*scale,im.height*scale),Image.Resampling.NEAREST)
    out=ROOT/output
    out.parent.mkdir(parents=True,exist_ok=True)
    im.save(out)
    return str(out)

def save_preview(source, output, max_width=1200, max_height=1800):
    im=rgba(source)
    scale=min(1,max_width/im.width,max_height/im.height)
    if scale<1:
        im=im.resize((round(im.width*scale),round(im.height*scale)))
    out=ROOT/output
    out.parent.mkdir(parents=True,exist_ok=True)
    im.save(out)
    return str(out)
