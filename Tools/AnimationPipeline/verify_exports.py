"""Validate frame bounds, pixels, archive integrity and expected artifact coverage."""
from pathlib import Path
from PIL import Image
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
data=json.loads((ROOT/'Assets/Recursos/Editor/AnimationPipeline/animation_manifest.json').read_text(encoding='utf-8'))
seen=set();total=0
for clip in data['clips']:
    identity=(clip['category'],clip['unit'],clip['name'])
    assert identity not in seen,identity
    seen.add(identity)
    path=ROOT/clip['texture'];im=Image.open(path).convert('RGBA')
    assert im.width<=8192 and im.height<=8192,(path,im.size)
    assert clip['frameRate']>0,identity
    for f in clip['frames']:
        x,y,w,h=f['x'],im.height-f['y']-f['height'],f['width'],f['height']
        assert x>=0 and y>=0 and x+w<=im.width and y+h<=im.height,(identity,f)
        assert 0<=f['pivotX']<=1 and 0<=f['pivotY']<=1,(identity,f)
        assert im.crop((x,y,x+w,y+h)).getchannel('A').getbbox(),(identity,f)
        total+=1
inventory=json.loads((HERE/'original_inventory.json').read_text(encoding='utf-8'))
for row in inventory:
    path=ROOT/row['originalPath']
    if not path.exists():path=ROOT/'SourceArt'/row['originalPath'].removeprefix('Assets/Recursos/')
    assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'],path
report={'success':True,'clipsAndPoses':len(seen),'multiFrameClips':sum(len(c['frames'])>1 for c in data['clips']),'singleFrameEntries':sum(len(c['frames'])==1 for c in data['clips']),'nonemptyFrames':total,'originalFilesUnchanged':len(inventory)}
(HERE/'python_validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
