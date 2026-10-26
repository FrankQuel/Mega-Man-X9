"""Produce a Spanish resource inventory from actual export and archive state."""
from pathlib import Path
from collections import Counter
import json
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
manifest=json.loads((ROOT/'Assets/Recursos/Editor/AnimationPipeline/animation_manifest.json').read_text(encoding='utf-8'))
inventory=json.loads((HERE/'original_inventory.json').read_text(encoding='utf-8'))
clips=manifest['clips']
animated=sum(len(c['frames'])>1 for c in clips)
poses=len(clips)-animated
lines=['# Auditoría de recursos y carpetas','',f'Se revisaron los {len(inventory)} archivos gráficos originales. Se exportaron {animated} clips de varios fotogramas y {poses} poses/estados/piezas estáticos ({len(clips)} entradas), con {sum(len(c["frames"]) for c in clips)} referencias a fotogramas.','', 'Los clips no equivalen a mecánicas de gameplay implementadas. Las poses de un fotograma se contabilizan como poses, no como movimiento.','', '| Recurso original | Resultado | Ubicación actual del original |','| --- | --- | --- |']
for row in inventory:
    source=row['originalPath'];related=[c for c in clips if c['source']==source]
    path=ROOT/source
    if not path.exists():path=ROOT/'SourceArt'/source.removeprefix('Assets/Recursos/')
    if related:
        a=sum(len(c['frames'])>1 for c in related);s=len(related)-a
        reason=f'{a} secuencias y {s} poses/piezas; sprites en '+', '.join(sorted(set(c['category']+'/'+c['unit'] for c in related)))
    elif 'paleta' in source.lower():reason='Referencia de color estática; archivada junto al original del personaje.'
    elif '/HUD/' in source:reason='Hoja estática de HUD. Carpeta correcta; separar widgets al implementar interfaz.'
    elif 'Character Select' in source:reason='Hoja de interfaz estática. Carpeta correcta; no se infiere secuencia temporal.'
    elif 'escenario' in source:reason='Escenario estático. Carpeta Environment correcta; no contiene cuadros de personaje.'
    else:reason='Ilustración o logotipo estático de menú. Carpeta MainMenu correcta.'
    lines.append('| '+source+' | '+reason+' | '+path.relative_to(ROOT).as_posix()+' |')
lines+=['','## Importación y organización','', '- Los originales archivados conservan su SHA-256; el organizador verifica que no falte ninguno.','- Las secuencias de puerta se ubican en Environment y las letras READY en UI.','- Las paletas y atlas originales usados se conservan en SourceArt; los archivos preparados para reproducir están en Assets.','- Los PNG generados usan filtro Point, sin mipmaps ni compresión. El escenario conserva su resolución de importación hasta 8192 px.','- Las carpetas Audio, VFX, Tilemaps y Scripts restantes siguen como estructura para futuras etapas; no se afirma que ya contengan esos sistemas.','', '## Límites','', '- FPS y algunos anclajes son estimados; requieren ajustar el ritmo con la lógica de juego.','- DragonParts son piezas estáticas. Todavía no existe rig ni animación articulada del jefe.','- Axl es una hoja ilustrada con variaciones anatómicas y de escala; sus clips requieren ajuste artístico.','- Las hojas fuente de HUD y selección de personaje aún requieren separar componentes para implementar pantallas funcionales.']
(ROOT/'AUDITORIA_RECURSOS.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Audit written:',len(inventory),'originals;',animated,'multi-frame clips;',poses,'single-frame entries')
