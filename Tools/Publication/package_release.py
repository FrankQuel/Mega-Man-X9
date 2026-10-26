"""Create a deterministic, versioned media ZIP without altering Unity assets or their .meta files."""
from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import stat
import subprocess
import sys
import tempfile
import zipfile

from sourcepack_common import MAX_ARCHIVE_BYTES, MAX_UNCOMPRESSED_BYTES, MEDIA_SUFFIXES, VERSION_RE, regular_files, safe_member, sha256_file, unity_guid
from verify_sourcepack import verify_archive


def selected_files(root: Path) -> dict[str, tuple[Path, str]]:
    selected = {}
    resources = root / "Assets/Recursos"
    if not resources.is_dir():
        raise ValueError("Assets/Recursos does not exist. Finish the Unity migration before packaging.")

    def add(path: Path, kind: str):
        relative = path.relative_to(root).as_posix()
        safe_member(relative)
        selected[relative] = (path, kind)

    for path in regular_files(resources):
        if path.suffix.lower() not in MEDIA_SUFFIXES:
            continue
        add(path, "media")
        meta = Path(str(path) + ".meta")
        if not meta.is_file():
            raise ValueError(f"Media has no Unity .meta: {path.relative_to(root)}")
        add(meta, "meta")
        parent = path.parent
        while parent != root / "Assets":
            folder_meta = Path(str(parent) + ".meta")
            if not folder_meta.is_file():
                raise ValueError(f"Unity folder has no .meta: {parent.relative_to(root)}")
            add(folder_meta, "meta")
            parent = parent.parent
    source = root / "SourceArt"
    if source.is_dir():
        for path in regular_files(source):
            lowered = path.name.lower()
            if lowered in {"agents.md", ".ds_store", "thumbs.db", "desktop.ini"} or "prompt" in lowered:
                continue
            if lowered.startswith(".env") or lowered.endswith((".pem", ".pfx", ".key")):
                raise ValueError("Private credential-like file found in SourceArt; review it before packaging")
            add(path, "source")
    if not any(kind == "media" for _, kind in selected.values()):
        raise ValueError("No media found beneath Assets/Recursos")
    return dict(sorted(selected.items()))


def code_reference(root: Path, requested: str | None) -> str | None:
    if requested is not None:
        if not re.fullmatch(r"[a-fA-F0-9]{40}|[a-fA-F0-9]{64}", requested):
            raise ValueError("--code-ref must be a full commit hash")
        return requested.lower()
    try:
        result = subprocess.run(["git", "-C", str(root), "rev-parse", "HEAD"], capture_output=True, text=True, timeout=5)
        value = result.stdout.strip()
        return value if result.returncode == 0 and re.fullmatch(r"[a-f0-9]{40}|[a-f0-9]{64}", value) else None
    except (OSError, subprocess.TimeoutExpired):
        return None


def unity_version(root: Path) -> str | None:
    path = root / "ProjectSettings/ProjectVersion.txt"
    if path.exists():
        match = re.search(r"^m_EditorVersion:\s*(.+)$", path.read_text(encoding="utf-8"), re.MULTILINE)
        return match.group(1).strip() if match else None
    return None


def build_package(root: Path, output: Path, version: str, requested_ref: str | None = None, overwrite: bool = False) -> dict:
    root, output = root.resolve(), output.resolve()
    if not VERSION_RE.fullmatch(version):
        raise ValueError("Version may contain only letters, numbers, periods, underscores and hyphens")
    if output.is_relative_to(root / "Assets") or output.is_relative_to(root / "SourceArt"):
        raise ValueError("Packaging output must stay outside Assets and SourceArt")
    selected = selected_files(root)
    total = sum(path.stat().st_size for path, _ in selected.values())
    if total > MAX_UNCOMPRESSED_BYTES:
        raise ValueError("Selected resources exceed the uncompressed safety limit")
    names = {"archive": f"resources-{version}.zip", "manifest": f"resources-manifest-{version}.json",
             "guide": f"RESTORE-RESOURCES-{version}.md", "checksums": f"SHA256SUMS-{version}.txt"}
    output.mkdir(parents=True, exist_ok=True)
    if not overwrite and any((output / name).exists() for name in names.values()):
        raise FileExistsError("Artifacts for this version already exist; choose a new version or explicitly use --overwrite")
    reference = code_reference(root, requested_ref)
    with tempfile.TemporaryDirectory(prefix="sourcepack-", dir=output) as temporary:
        staging = Path(temporary)
        archive = staging / names["archive"]
        records = []
        with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as package:
            for relative, (path, kind) in selected.items():
                before = path.stat()
                info = zipfile.ZipInfo(relative, date_time=(1980, 1, 1, 0, 0, 0))
                info.create_system = 3; info.external_attr = (stat.S_IFREG | 0o644) << 16
                info.compress_type = zipfile.ZIP_DEFLATED
                digest = hashlib.sha256()
                with path.open("rb") as source, package.open(info, "w", force_zip64=True) as destination:
                    for chunk in iter(lambda: source.read(1024 * 1024), b""):
                        digest.update(chunk); destination.write(chunk)
                after = path.stat()
                if (before.st_size, before.st_mtime_ns) != (after.st_size, after.st_mtime_ns):
                    raise ValueError(f"Source changed during packaging: {relative}")
                record = {"path": relative, "bytes": before.st_size, "sha256": digest.hexdigest(), "kind": kind}
                if path.suffix.lower() == ".meta":
                    guid = unity_guid(path.read_bytes())
                    if kind == "meta" and guid is None:
                        raise ValueError(f"Unity metadata has no GUID: {relative}")
                    if guid is not None:
                        record["unity_guid"] = guid
                if kind == "media":
                    record["meta_path"] = relative + ".meta"
                records.append(record)
        if archive.stat().st_size > MAX_ARCHIVE_BYTES:
            raise ValueError("The ZIP must be smaller than 2 GiB; split the release resources before publishing")
        data = {"schema_version": 1, "format": "unity-sourcepack", "version": version,
                "created_utc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                "unity_editor": unity_version(root), "code_ref": reference,
                "source_state": "working_tree_without_commit" if reference is None else "commit_reference_with_working_tree_resources",
                "archive": {"file": names["archive"], "bytes": archive.stat().st_size, "sha256": sha256_file(archive)},
                "resource_roots": ["Assets/Recursos", "SourceArt"], "file_count": len(records), "uncompressed_bytes": total, "files": records}
        manifest = staging / names["manifest"]
        manifest.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        result = verify_archive(archive, manifest, root)
        guide = staging / names["guide"]
        guide.write_text(f"""# Restaurar recursos {version}

1. Descarga el código del tag o commit correspondiente ({reference or 'commit pendiente de fijar antes de publicar'}).
2. No abras Unity hasta restaurar y verificar estos recursos. Conserva la carpeta raíz Assets; Recursos es su contenedor interno.
3. Verifica el SHA-256 del ZIP: `{data['archive']['sha256']}`.
4. Desde la raíz del proyecto ejecuta:

```text
python Tools/Publication/verify_sourcepack.py --archive RUTA/{names['archive']} --manifest RUTA/{names['manifest']}
```

Para descargar y extraer, usa download_sourcepack.py con las URLs HTTPS reales publicadas del ZIP y manifiesto y el SHA-256 anterior. No hay una URL pública configurada en este archivo.

La extracción debe conservar las rutas Assets/Recursos y SourceArt, incluidos todos sus .meta. El descargador verifica todo antes de escribir y rechaza cambios en archivos ya existentes.

Después de restaurar:

```text
python Tools/Publication/verify_sourcepack.py --archive RUTA/{names['archive']} --manifest RUTA/{names['manifest']} --project-root . --check-project
```

Abre el proyecto con Unity {data['unity_editor'] or 'la versión indicada en ProjectSettings/ProjectVersion.txt'}. Unity regenerará Library y las cachés locales. Los recursos de Capcom y otros terceros conservan sus derechos y condiciones; este paquete no les otorga una licencia nueva.
""", encoding="utf-8")
        checksums = staging / names["checksums"]
        checksums.write_text("".join(f"{sha256_file(staging / names[key])}  {names[key]}\n" for key in ("archive", "manifest", "guide")), encoding="ascii")
        for name in names.values():
            os.replace(staging / name, output / name)
    return {**result, "artifacts": names, "archive_sha256": data["archive"]["sha256"], "code_ref": reference}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, default=Path.cwd())
    parser.add_argument("--output", type=Path, default=None)
    parser.add_argument("--version", required=True)
    parser.add_argument("--code-ref")
    parser.add_argument("--overwrite", action="store_true", help="Replace only the generated artifacts for this version")
    args = parser.parse_args()
    try:
        root = args.project_root.resolve()
        result = build_package(root, args.output or root / "Artifacts/Publication", args.version, args.code_ref, args.overwrite)
        print(json.dumps(result, ensure_ascii=False, indent=2)); return 0
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        print(f"Packaging failed: {error}", file=sys.stderr); return 1


if __name__ == "__main__":
    raise SystemExit(main())
