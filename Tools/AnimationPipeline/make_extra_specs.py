"""Manually reviewed custom boss poses and UI sequences."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
specs=[]
white='Assets/Recursos/Sprites/Bosses/Axl/Axl_OriginalSpriteSheet.png'
note='Hoja de poses ilustradas: proporciones variables; revisar continuidad artística antes de combate final. Rectángulos revisados para conservar cuerpos completos.'

def exact(name,rects,loop=False,fps=10,unit='Axl',category='Bosses',notes=note):
    specs.append(dict(source=white,category=category,unit=unit,name=name,rects=rects,fps=fps,loop=loop,backgroundMode='neutral',isolateComponent=(category=='Bosses'),notes=notes))

def row(name,y0,y1,spans,loop=False,fps=10,unit='Axl',category='Bosses',notes=note):
    exact(name,[[x,y0,r-x,y1-y0] for x,r in spans],loop,fps,unit,category,notes)

# Character rectangles follow complete connected silhouettes, rather than a fixed grid.
# Neighboring bboxes can overlap horizontally; isolateComponent removes neighbor fragments.
exact('Idle',[[23,134,67,84],[100,136,58,82],[176,136,49,82],[242,136,46,82]],True,6)
exact('ArmRaise',[[306,136,47,82],[366,130,35,88],[409,131,38,87],[462,131,34,87]])
exact('PointDiagonal',[[512,137,70,81]],True,1)
exact('MaterializeSilhouette',[[594,135,78,79]],False,1)
exact('Aim',[[680,136,71,82],[765,136,61,82],[839,134,71,84],[926,136,73,82]],True,6)
exact('Run',[[24,235,60,84],[99,235,54,85],[169,236,51,84],[234,235,61,86],[311,235,57,84],[373,235,67,84],[447,233,61,87],[521,233,56,87],[586,234,65,86],[666,235,75,85],[747,235,67,84],[832,236,70,84]],True,12)
exact('Crouch',[[915,248,74,72]],True,1)
exact('AirDash',[[34,333,63,91],[112,336,64,88],[194,339,66,88],[281,338,69,86],[365,339,79,85],[449,341,77,86],[539,342,85,85],[629,340,78,87],[718,339,50,88],[789,339,53,88]],True,12)
exact('DashStop',[[850,363,51,65],[924,364,54,64]])
exact('Shoot',[[24,440,74,82],[119,441,94,82],[217,442,95,81],[310,442,97,81],[400,442,107,81]],False,12)
exact('AirShoot',[[512,443,79,80]],False,10)
exact('ShootUp',[[638,417,61,106]],False,1)
exact('LowShoot',[[769,446,91,76],[872,453,76,69]],False,10)
exact('CrouchShoot',[[20,532,106,67],[127,533,83,67],[210,537,86,64],[294,537,94,64],[389,530,90,73],[481,532,97,72],[576,538,78,64],[671,532,97,69],[792,535,90,66],[898,543,108,57]],False,12)
exact('RunShoot',[[29,613,110,79],[139,613,118,79],[257,613,94,79]],True,10)
exact('ShootRecover',[[338,613,71,79],[416,615,64,77]],False,8)
exact('Charge',[[509,616,81,76],[611,617,87,79],[698,615,90,77],[782,607,165,85]],False,10)
exact('AirSpreadShot',[[18,707,122,64],[143,706,126,68],[266,704,108,70],[365,706,123,66],[489,705,123,67],[613,711,113,62],[730,705,121,67],[875,700,111,72]],False,10)
exact('DashAttackReady',[[23,783,53,76]],True,1)
exact('DashAttackStartup',[[93,784,112,75]],False,8)
exact('DashAttack',[[196,784,99,74],[291,784,101,73],[388,783,118,73],[495,784,116,72],[636,790,102,67],[763,788,102,70],[891,799,104,59]],False,12)
exact('AirCharge',[[27,871,99,84],[129,877,80,82],[225,874,84,86],[309,872,73,88],[389,877,83,84],[473,870,70,88],[550,875,90,83],[646,872,89,86]],False,10)
exact('DashAfterimage',[[723,883,83,74]],False,1)
exact('ShieldReady',[[821,882,41,67]],True,1)
exact('Shield',[[877,870,63,88]],True,1)
exact('ShieldRecover',[[951,883,44,69]],False,1)
exact('ChargedShot',[[19,970,78,82],[96,972,94,89],[190,975,93,86],[277,982,101,70],[377,983,121,69]],False,12)
exact('AirAimUp',[[19,1069,93,78],[119,1069,90,78],[215,1064,78,83],[298,1064,82,85],[394,1064,90,84],[482,1064,79,84],[564,1064,80,84]],False,10)
exact('JetRise',[[643,1062,82,86],[727,1060,67,90],[811,1064,64,92],[888,1064,56,92],[957,1064,52,92]],False,12)
exact('Hurt',[[22,1160,91,73],[140,1159,118,74],[260,1160,94,72],[364,1158,114,79]],False,10)
exact('HurtRecover',[[489,1158,82,78]],False,1)
exact('Knockdown',[[578,1160,71,65],[661,1159,83,68],[756,1176,65,52],[835,1181,84,47],[932,1188,77,40]],False,10)
exact('Hover',[[19,1251,73,93],[109,1251,61,93],[189,1248,82,90],[278,1246,82,108],[370,1251,61,103],[444,1251,45,103],[500,1245,40,108]],True,10)
exact('TeleportOut',[[553,1244,36,85],[598,1244,43,84],[649,1242,40,86],[699,1241,40,87],[755,1241,29,88],[796,1246,60,85],[869,1252,40,79],[919,1253,34,77],[964,1257,33,72]],False,10)
exact('Victory',[[24,1366,54,72],[93,1365,53,74],[161,1366,54,72],[230,1369,53,69],[298,1366,61,73],[383,1364,45,75],[451,1364,46,74]],False,8)
exact('TeleportIn',[[508,1365,50,70],[567,1348,52,87],[626,1349,58,86],[695,1349,56,86]],False,10)
exact('TeleportFinish',[[771,1348,58,86]],False,1)
exact('TeleportBeam',[[842,1349,52,86],[911,1349,14,86]],False,10)
exact('TeleportArrivalPose',[[940,1349,60,85]],True,1)
exact('AlternateIdle',[[23,1452,56,71],[97,1450,55,73],[167,1450,67,73]],True,6)
exact('TeleportArrivalBeam',[[47,19,12,94],[70,18,14,96]],False,10)
exact('TeleportArrivalEnergy',[[97,55,45,27],[152,37,61,61],[226,19,108,96],[358,17,102,100],[486,14,100,105],[607,14,102,105],[732,18,99,97],[862,16,121,108]],False,10)
row('HitBurst',1444,1536,[(240,314),(314,389),(389,462),(462,534),(534,610)],False,12,unit='AxlFX',category='Effects')
row('Sparkle',1444,1536,[(612,658),(663,709),(710,765),(767,825),(826,858)],True,10,unit='AxlFX',category='Effects')
row('Smoke',1444,1536,[(855,932),(932,1012)],False,8,unit='AxlFX',category='Effects')

# Supplied menu has three horizontal scanline variants for the Start selection.
specs.append(dict(source='Assets/Recursos/UI/Menus/MainMenu/Retro CRT Spanish Game Menu Sprite Sheet.png',category='UI',unit='MainMenu',name='StartSelectedCRT',rects=[[0,0,482,543],[483,0,482,543],[966,0,482,543]],fps=6,loop=True,keyColors=[],notes='Variantes completas del menú; se reproduce como una superficie UI.'))
specs.append(dict(source='Assets/Recursos/UI/Menus/MainMenu/Retro CRT Spanish Game Menu Sprite Sheet.png',category='UI',unit='MainMenu',name='OptionsSelected',rects=[[483,543,482,543]],fps=1,loop=True,keyColors=[],notes='Estado estático de selección.'))
specs.append(dict(source='Assets/Recursos/UI/Menus/MainMenu/Retro CRT Spanish Game Menu Sprite Sheet.png',category='UI',unit='MainMenu',name='ExitSelected',rects=[[966,543,482,543]],fps=1,loop=True,keyColors=[],notes='Estado estático de selección.'))
(OUT/'extra_specs.json').write_text(json.dumps(specs,ensure_ascii=False,indent=2),encoding='utf-8')
print('extra clips:',len(specs))

if __name__=='__main__':pass
