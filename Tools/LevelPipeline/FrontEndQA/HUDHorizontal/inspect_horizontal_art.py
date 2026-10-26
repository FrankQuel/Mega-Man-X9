from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
root=Path.cwd();out=root/'Tools/LevelPipeline/FrontEndQA/HUDHorizontal'
elements=Image.open(root/'SourceArt/UI/HUD/HUD Elements.png').convert('RGBA')
normal=Image.open(root/'Assets/Recursos/UI/HUD/Generated/HealthNormal.png').convert('RGBA')
life=normal.copy();life.paste((0,0,0,0),(8,8,16,16))
canvas=Image.new('RGBA',(1600,340),(18,29,38,255));draw=ImageDraw.Draw(canvas)
font=ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf',24)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
for unit,red,boss,position in [('X',False,False,(36,46)),('Zero',True,False,(36,170)),('Magma Dragoon',False,True,(1150,58))]:
    vertical=Image.open(root/('Assets/Recursos/UI/HUD/Generated/HealthFrameBoss.png' if boss else 'Assets/Recursos/UI/HUD/Generated/HealthFrameRed.png' if red else 'Assets/Recursos/UI/HUD/Generated/HealthFrameBlue.png')).convert('RGBA')
    frame=Image.new('RGBA',(140 if boss else 180,28))
    if boss:frame.paste(vertical.crop((0,36,17,49)),(0,8))
    else:frame.paste(elements.crop((239,209,273,237) if red else (68,177,99,205)),(0,0))
    bx=49 if boss else 89;left=2 if boss else 6
    for y in range(12):
        for x in range(88):frame.putpixel((bx+x,8+y),vertical.getpixel((left+y,x if x<4 else 87-x if x>=84 else 16)))
    frame.paste((0,0,0,0),(bx+4,11,bx+84,16))
    frame.save(out/(unit.replace(' ','')+'_native.png'))
    frame.alpha_composite(normal,(21 if boss else 35,2))
    if not boss:frame.alpha_composite(life,(62,2))
    fill=Image.new('RGBA',(80,5),elements.getpixel((68,38)));frame.alpha_composite(fill,(bx+4,11))
    x,y=position;canvas.alpha_composite(frame.resize((frame.width*3,84),Image.Resampling.NEAREST),position)
    draw.text((x,y-25),unit+'  ·  '+('JEFE' if boss else 'VIDAS COMPARTIDAS'),fill='white',font=small)
    if not boss:draw.text((x+62*3+36,y+2*3+36),'3',anchor='mm',fill='white',font=font)
    draw.text((x+(bx+44)*3,y+24*3),('48/48' if boss else '28/28')+' PV',anchor='mm',fill='white',font=small)
canvas.save(out/'hud_horizontal_art_qa.png')
