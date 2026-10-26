"""Build transparent, aligned PNG strips and a reproducible Unity import manifest.

Originals are never overwritten. Run with the bundled Python described in README.
"""
import hashlib
import json
import math
from collections import Counter
from pathlib import Path
import re
import numpy as np
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent

def slug(text):
    return re.sub(r'[^A-Za-z0-9_]+','_',text).strip('_')

def source_path(source):
    p=ROOT/source
    if p.exists(): return p
    return ROOT/'SourceArt'/source.removeprefix('Assets/Recursos/')

def clear_background(im, colors=None, mode=None):
    a=np.array(im.convert('RGBA')).copy()
    if colors is None and np.all(a[:,:,3]>0):
        colors=[Counter(map(tuple,a[:,:,:3].reshape(-1,3))).most_common(1)[0][0]]
    for color in colors or []:
        a[np.all(a[:,:,:3]==color[:3],axis=2),3]=0
    return Image.fromarray(a)

def clear_edge_background(im, colors=None):
    from collections import deque
    a=np.array(im.convert('RGBA')).copy()
    candidate=np.zeros(a.shape[:2],bool)
    for color in colors or [[255,255,255]]:
        candidate|=np.all(a[:,:,:3]==color[:3],axis=2)
    h,w=candidate.shape;q=deque([(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]+[(0,y) for y in range(h)]+[(w-1,y) for y in range(h)])
    seen=np.zeros((h,w),bool)
    while q:
        x,y=q.popleft()
        if x<0 or y<0 or x>=w or y>=h or seen[y,x] or not candidate[y,x]:continue
        seen[y,x]=True;q.extend(((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
    a[seen,3]=0
    return Image.fromarray(a)

def largest_component(im):
    from collections import deque
    a=np.array(im.convert('RGBA')).copy();m=a[:,:,3]>0;h,w=m.shape
    seen=np.zeros((h,w),bool);largest=[]
    for sy,sx in zip(*np.nonzero(m)):
        if seen[sy,sx]:continue
        component=[];q=deque([(int(sx),int(sy))]);seen[sy,sx]=True
        while q:
            x,y=q.popleft();component.append((x,y))
            for dy in (-1,0,1):
                for dx in (-1,0,1):
                    xx,yy=x+dx,y+dy
                    if 0<=xx<w and 0<=yy<h and m[yy,xx] and not seen[yy,xx]:
                        seen[yy,xx]=True;q.append((xx,yy))
        if len(component)>len(largest):largest=component
    keep=np.zeros((h,w),bool)
    for x,y in largest:keep[y,x]=True
    a[~keep,3]=0
    return Image.fromarray(a)

def clear_neutral_border(im):
    """Remove only neutral background connected to crop edges (retain highlights)."""
    from collections import deque
    a=np.array(im.convert('RGBA')).copy()
    rgb=a[:,:,:3].astype(int)
    candidate=(rgb.max(2)-rgb.min(2)<22)&(rgb.min(2)>100)
    h,w=candidate.shape
    q=deque([(x,0) for x in range(w)]+[(x,h-1) for x in range(w)]+[(0,y) for y in range(h)]+[(w-1,y) for y in range(h)])
    seen=np.zeros((h,w),bool)
    while q:
        x,y=q.popleft()
        if x<0 or y<0 or x>=w or y>=h or seen[y,x] or not candidate[y,x]:continue
        seen[y,x]=True
        q.extend(((x-1,y),(x+1,y),(x,y-1),(x,y+1)))
    a[seen,3]=0
    return Image.fromarray(a)

def build():
    specs=[]
    for p in sorted(HERE.glob('*_specs.json')):
        data=json.loads(p.read_text(encoding='utf-8-sig'))
        specs.extend(data if isinstance(data,list) else data['clips'])
    result=[]; used=set(); previews=[]
    for spec in specs:
        cat,unit,name=map(slug,(spec['category'],spec['unit'],spec['name']))
        identity=f'{cat}/{unit}/{name}'
        if identity in used: raise ValueError(f'Duplicate clip: {identity}')
        used.add(identity)
        path=source_path(spec['source'])
        original=Image.open(path).convert('RGBA')
        sheet=clear_background(original,spec.get('keyColors')) if spec.get('backgroundMode') not in ('neutral','edge') else original
        rects=spec['rects']
        if not rects: raise ValueError(f'Empty clip {identity}')
        anchors=spec.get('anchors')
        if anchors is None and 'framePivots' in spec:
            anchors=[[r[2]*p[0],r[3]*(1-p[1])] for r,p in zip(rects,spec['framePivots'])]
        if anchors is None:
            anchors=[[w/2,h] for x,y,w,h in rects]
        if len(anchors)!=len(rects):raise ValueError(f'Anchor mismatch {identity}')
        left=math.ceil(max(a[0] for a in anchors))+2
        right=math.ceil(max(r[2]-a[0] for r,a in zip(rects,anchors)))+2
        top=math.ceil(max(a[1] for a in anchors))+2
        bottom=math.ceil(max(r[3]-a[1] for r,a in zip(rects,anchors)))+2
        cw,ch=left+right,top+bottom
        columns=min(len(rects),max(1,2048//cw))
        rows=math.ceil(len(rects)/columns)
        canvas=Image.new('RGBA',(cw*columns,ch*rows))
        frames=[]
        for i,(r,a) in enumerate(zip(rects,anchors)):
            x,y,w,h=map(int,r)
            if x<0 or y<0 or w<=0 or h<=0 or x+w>sheet.width or y+h>sheet.height:
                raise ValueError(f'Out of bounds: {identity} frame {i} {r} sheet {sheet.size}')
            frame=sheet.crop((x,y,x+w,y+h))
            if spec.get('backgroundMode')=='neutral': frame=clear_neutral_border(frame)
            if spec.get('backgroundMode')=='edge': frame=clear_edge_background(frame,spec.get('keyColors'))
            if 'excludeRects' in spec:
                for ex,ey,ew,eh in spec['excludeRects'][i]:
                    frame.paste((0,0,0,0),(ex,ey,ex+ew,ey+eh))
            if spec.get('isolateComponent'): frame=largest_component(frame)
            if frame.getbbox() is None: raise ValueError(f'Empty pixels {identity} frame {i}')
            ox,oy=(i%columns)*cw,(i//columns)*ch
            canvas.alpha_composite(frame,(round(ox+left-a[0]),round(oy+top-a[1])))
            frames.append({'name':f'{name}_{i:03d}','x':ox,'y':canvas.height-oy-ch,'width':cw,'height':ch,'pivotX':left/cw,'pivotY':bottom/ch})
        rel=(f'Assets/Recursos/UI/Animated/{unit}/Generated/{name}.png' if cat=='UI' else f'Assets/Recursos/Sprites/{cat}/{unit}/Generated/{name}.png')
        out=ROOT/rel;out.parent.mkdir(parents=True,exist_ok=True);canvas.save(out)
        entry={'category':cat,'unit':unit,'name':name,'texture':rel,'source':spec['source'],'frameRate':spec.get('fps',12),'loop':spec.get('loop',False),'frames':frames,'sourceRects':rects,'notes':spec.get('notes','')}
        result.append(entry)
        thumb=Image.new('RGBA',(min(canvas.width,880),ch),(22,29,45,255))
        thumb.alpha_composite(canvas.crop((0,0,thumb.width,ch)))
        previews.append((identity,thumb))
    manifest=ROOT/'Assets/Recursos/Editor/AnimationPipeline/animation_manifest.json'
    manifest.parent.mkdir(parents=True,exist_ok=True)
    manifest.write_text(json.dumps({'pixelsPerUnit':100,'clips':result},ensure_ascii=False,indent=2),encoding='utf-8')
    report={'clipCount':len(result),'frameCount':sum(len(e['frames']) for e in result),'units':dict(Counter(f"{e['category']}/{e['unit']}" for e in result)),'sourceCount':len(set(e['source'] for e in result))}
    (HERE/'generation_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    qa=HERE/'QA/Generated';qa.mkdir(parents=True,exist_ok=True)
    for start in range(0,len(previews),18):
        items=previews[start:start+18];height=sum(im.height+25 for _,im in items)
        contact=Image.new('RGB',(920,height),'#161d2d');draw=ImageDraw.Draw(contact);y=0
        for label,im in items:
            draw.text((8,y+4),label,fill='white');contact.paste(im.convert('RGB'),(8,y+23));y+=im.height+25
        contact.save(qa/f'page_{start//18+1:02d}.png')
    print(json.dumps(report,indent=2))

if __name__=='__main__': build()
