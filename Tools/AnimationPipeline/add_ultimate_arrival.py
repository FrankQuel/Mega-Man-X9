"""Preserve the original armor artwork while supplying its reverse materialization."""
import copy,json
from pathlib import Path
p=Path(__file__).resolve().parent/'blue_specs.json'
data=json.loads(p.read_text(encoding='utf-8'))
data=[c for c in data if not(c['unit']=='BlueUltimate' and c['name']=='TeleportArrival')]
entry=copy.deepcopy(next(c for c in data if c['unit']=='BlueUltimate' and c['name']=='TeleportDeparture'))
entry['name']='TeleportArrival';entry['rects'].reverse();entry['loop']=False
for field in ('anchors','framePivots','excludeRects'):
    if field in entry:entry[field].reverse()
entry['notes']='Materialization uses the original Ultimate armor departure frames in reverse order; no base-armor body is substituted.'
data.append(entry)
p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Ultimate arrival:',len(entry['rects']),'original frames')
