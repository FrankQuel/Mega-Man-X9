"""Exact original pose keyframes and painted emission anchors; no rescaling or redraw."""
from pathlib import Path
import json
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sheet = Image.open(ROOT / 'SourceArt/Sprites/Bosses/Dragon/Dragon.png').convert('RGBA')
folder = HERE / 'dragon_rig_art/Poses'
folder.mkdir(parents=True, exist_ok=True)
# name, original rectangle, ground/torso anchor, mouth lip, front palm, striking foot.
definitions = [
 ('Guard',[292,26,81,79],[333,104],[319,51],[301,69],[307,98]),
 ('Windup',[110,151,95,68],[158,218],[143,174],[176,172],[191,210]),
 ('Slash',[211,150,94,73],[258,222],[241,173],[223,176],[292,214]),
 ('Sweep',[312,148,98,72],[354,219],[337,174],[325,176],[395,210]),
 ('JumpAttack',[527,126,65,104],[558,229],[552,163],[546,135],[574,214]),
 ('AttackRaised',[119,244,87,74],[166,317],[146,267],[175,266],[193,306]),
 ('CrouchAttack',[219,252,96,73],[270,324],[266,278],[230,287],[300,313]),
 ('Recover',[124,326,73,66],[160,391],[147,348],[166,347],[184,382]),
 ('SweepLow',[217,333,98,59],[265,391],[263,354],[228,360],[302,381]),
 ('KickHigh',[70,430,64,83],[104,512],[98,452],[81,450],[96,501]),
 ('KickForward',[141,438,81,74],[180,511],[176,457],[210,447],[150,495]),
 ('Turn',[256,434,73,84],[300,517],[282,457],[265,477],[300,507]),
 ('Lunge',[336,438,76,78],[379,515],[365,459],[347,461],[398,502]),
 ('Crouch',[127,694,94,68],[176,761],[156,729],[141,737],[144,752]),
 ('CrouchShift',[226,695,96,67],[275,761],[255,729],[239,739],[242,752]),
 ('Airborne',[351,684,75,79],[391,762],[381,708],[362,710],[405,753]),
]
poses=[]
for name, rect, origin, mouth, hand, foot in definitions:
    x,y,w,h=rect
    paint=sheet.crop((x,y,x+w,y+h))
    out=Image.new('RGBA',(w+4,h+4))
    out.alpha_composite(paint,(2,2))
    out.save(folder/(name+'.png'))
    point=lambda p:[round((p[0]-origin[0])/100,4),round((origin[1]-p[1])/100,4),0]
    poses.append(dict(name=name,artPath='Tools/AnimationPipeline/dragon_rig_art/Poses/'+name+'.png',
        localOffset=point([x+w/2,y+h/2]),mouth=point(mouth),hand=point(hand),foot=point(foot),
        sourceRect=rect,sourceOrigin=origin))
data_path=HERE/'dragon_rig.json'
data=json.loads(data_path.read_text())
data['authoredPoses']=poses
data_path.write_text(json.dumps(data,indent=2),encoding='utf8')
# Fixed-scale contact sheet with the actual annotated emission positions.
contact=Image.new('RGBA',(1000,1000),(22,29,45,255))
draw=ImageDraw.Draw(contact)
for i,(name,rect,origin,mouth,hand,foot) in enumerate(definitions):
    x,y,w,h=rect;ox=(i%4)*250+125;oy=(i//4)*250+230
    p=sheet.crop((x,y,x+w,y+h)).resize((w*2,h*2),Image.Resampling.NEAREST)
    contact.alpha_composite(p,(ox+int((x-origin[0])*2),oy+int((y-origin[1])*2)))
    draw.text(((i%4)*250+6,(i//4)*250+6),name,fill='white')
    for point,color in [(mouth,'#ffdd33'),(hand,'#00ffff'),(foot,'#ff88dd')]:
        px,py=ox+(point[0]-origin[0])*2,oy+(point[1]-origin[1])*2
        draw.ellipse((px-2,py-2,px+2,py+2),fill=color)
contact.save(HERE/'QA/Dragon/gameplay_original_poses_anchors.png')
print('Original gameplay poses:',len(poses))
