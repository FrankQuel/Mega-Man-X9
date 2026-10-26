"""Inventory and reversible organization of original art after animation export."""
from pathlib import Path
from PIL import Image
import hashlib,json,zipfile,shutil
ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
SUFFIXES={'.gif','.png','.jpg','.jpeg'}

def inventory():
    rows=[]
    for p in sorted((ROOT/'Assets').rglob('*')):
        if p.suffix.lower() not in SUFFIXES or 'Generated' in p.parts:continue
        im=Image.open(p)
        rows.append(dict(originalPath=p.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),width=im.width,height=im.height,mode=im.mode,embeddedFrames=getattr(im,'n_frames',1)))
    target=HERE/'original_inventory.json'
    if not target.exists():target.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
    backup=ROOT/'Backups/before-animation-import-2026-10-24.zip'
    if not backup.exists():
        backup.parent.mkdir(exist_ok=True)
        with zipfile.ZipFile(backup,'w',zipfile.ZIP_DEFLATED) as z:
            for row in rows:
                p=ROOT/row['originalPath'];z.write(p,p.relative_to(ROOT).as_posix())
            for p in (ROOT/'ProjectSettings').glob('*'):
                if p.is_file():z.write(p,p.relative_to(ROOT).as_posix())
    print('Original art inventory:',len(rows),'Backup:',backup)

def organize():
    manifest=json.loads((ROOT/'Assets/Recursos/Editor/AnimationPipeline/animation_manifest.json').read_text(encoding='utf-8'))
    sources={c['source'] for c in manifest['clips']}
    sources|={p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets').rglob('*') if p.is_file() and ('paleta' in p.name.lower() or p.suffix.lower()=='.gif')}
    moves=[]
    for source in sorted(sources):
        src=ROOT/source;dst=ROOT/'SourceArt'/source.removeprefix('Assets/Recursos/')
        if not src.exists():continue
        dst.parent.mkdir(parents=True,exist_ok=True)
        if dst.exists():
            if dst.read_bytes()!=src.read_bytes():raise ValueError('Archive collision: '+str(dst))
            continue
        shutil.move(str(src),str(dst))
        meta=Path(str(src)+'.meta')
        if meta.exists():shutil.move(str(meta),str(dst)+'.meta')
        moves.append(dict(original=source,current=dst.relative_to(ROOT).as_posix(),reason='Original sheet or palette preserved outside Unity import; PNG clips are in Assets/Recursos/Sprites.'))
    record=HERE/'source_moves.json'
    previous=json.loads(record.read_text(encoding='utf-8')) if record.exists() else []
    record.write_text(json.dumps(previous+moves,ensure_ascii=False,indent=2),encoding='utf-8')
    # Archive now-empty original sheet directories, keeping intentionally empty project folders.
    parents=sorted({(ROOT/m['original']).parent for m in moves},key=lambda p:len(p.parts),reverse=True)
    for parent in parents:
        if parent.exists() and not any(parent.iterdir()):
            rel=parent.relative_to(ROOT/'Assets'/'Recursos')
            parent.rmdir()
            meta=Path(str(parent)+'.meta')
            archive=ROOT/'SourceArt'/rel
            if meta.exists():shutil.move(str(meta),str(archive)+'.meta')
    # Retire only pipeline-generated strips which are no longer in the manifest.
    active={c['texture'] for c in manifest['clips']}
    for p in (ROOT/'Assets').rglob('*.png'):
        if 'Generated' not in p.parts or p.relative_to(ROOT).as_posix() in active:continue
        dst=ROOT/'Backups/RetiredGenerated'/p.relative_to(ROOT)
        dst.parent.mkdir(parents=True,exist_ok=True)
        if dst.exists():continue
        shutil.move(str(p),str(dst))
        meta=Path(str(p)+'.meta')
        if meta.exists():shutil.move(str(meta),str(dst)+'.meta')
    for generated in list((ROOT/'Assets').rglob('Generated')):
        if not generated.is_dir():continue
        unit=generated.parent
        prefix=unit.relative_to(ROOT).as_posix()+'/'
        if any(path.startswith(prefix) for path in active):continue
        if any(p.is_file() and p.suffix!='.meta' for p in unit.rglob('*')):continue
        assert unit.resolve().is_relative_to((ROOT/'Assets').resolve()),unit
        dst=ROOT/'Backups/RetiredGeneratedDirectories'/unit.relative_to(ROOT)
        assert dst.resolve().is_relative_to((ROOT/'Backups').resolve()),dst
        if dst.exists():continue
        dst.parent.mkdir(parents=True,exist_ok=True)
        shutil.move(str(unit),str(dst))
        meta=Path(str(unit)+'.meta')
        if meta.exists():shutil.move(str(meta),str(dst)+'.meta')
    # Every original remains somewhere with exactly the original checksum.
    rows=json.loads((HERE/'original_inventory.json').read_text(encoding='utf-8'))
    for row in rows:
        p=ROOT/row['originalPath']
        if not p.exists():p=ROOT/'SourceArt'/row['originalPath'].removeprefix('Assets/Recursos/')
        if not p.exists() or hashlib.sha256(p.read_bytes()).hexdigest()!=row['sha256']:raise ValueError('Original changed or missing: '+row['originalPath'])
    print('Moved original sheets:',len(moves),'; original checksums verified:',len(rows))

if __name__=='__main__':
    import sys
    if '--organize' in sys.argv:organize()
    else:inventory()
