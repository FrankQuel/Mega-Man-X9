"""Contact sheet of original exported poses, for gameplay semantic review."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[2]
manifest = json.loads((root/'Assets/Recursos/Editor/AnimationPipeline/animation_manifest.json').read_text(encoding='utf-8-sig'))
names = {'Blue': ['Idle','AlternateIdle','WallJump','WallCling','LadderClimb'],
         'BlueUltimate': ['Idle','AlternateIdle','WallJump','WallCling'],
         'Red': ['Idle','WallClimb','WallPushOff','Enhanced_Ready','AirPoseExtended','AirPoseRaised','AirPoseLowered','GroundEnergyStrike','GroundHeavySlash'],
         'RedEffects': ['FireUppercutPose','IceRing','GroundIceSpikes','PinkFlameAura']}
rows=[]
for clip in manifest['clips']:
    if clip['name'] not in names.get(clip['unit'], []): continue
    strip=Image.open(root/clip['texture']).convert('RGBA')
    frames=clip['frames']; width=max(680,len(frames)*105)
    row=Image.new('RGB',(width,190),(24,29,33)); d=ImageDraw.Draw(row)
    d.text((6,4),clip['unit']+'/'+clip['name'],fill='white')
    for i,f in enumerate(frames):
        frame=strip.crop((f['x'],strip.height-f['y']-f['height'],f['x']+f['width'],strip.height-f['y']))
        frame.thumbnail((96,145),Image.Resampling.NEAREST)
        frame=frame.resize((frame.width*min(2,96//frame.width),frame.height*min(2,96//frame.width)),Image.Resampling.NEAREST)
        row.paste(frame,(i*105+4,180-frame.height),frame)
        d.text((i*105+6,22),str(i),fill='cyan')
    rows.append(row)
width=max(r.width for r in rows)
out=Image.new('RGB',(width,sum(r.height for r in rows)),(16,20,25)); y=0
for row in rows: out.paste(row,(0,y)); y+=row.height
out.save(root/'Previews/RainyFoundry/HeroPoseAudit.png')
print('Saved HeroPoseAudit.png',out.size)
