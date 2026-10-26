"""Verify release archive bytes, every member SHA-256, Unity GUIDs, and optional restored files."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import stat
import sys
import zipfile

from sourcepack_common import destination, load_manifest, sha256_file, unity_guid


def verify_archive(archive: Path, manifest: Path, project_root: Path | None = None) -> dict:
    data = load_manifest(manifest)
    if archive.stat().st_size != data["archive"]["bytes"] or sha256_file(archive) != data["archive"]["sha256"]:
        raise ValueError("Archive size or SHA-256 does not match the manifest")
    expected = {item["path"]: item for item in data["files"]}
    with zipfile.ZipFile(archive) as package:
        names = package.namelist()
        if len(names) != len(set(names)) or set(names) != set(expected):
            raise ValueError("ZIP entries do not exactly match the manifest")
        for info in package.infolist():
            entry = expected[info.filename]
            if info.is_dir() or stat.S_IFMT(info.external_attr >> 16) == stat.S_IFLNK or info.file_size != entry["bytes"]:
                raise ValueError("Invalid ZIP member type or length")
            digest = hashlib.sha256()
            meta = bytearray() if info.filename.endswith(".meta") else None
            with package.open(info) as stream:
                for chunk in iter(lambda: stream.read(1024 * 1024), b""):
                    digest.update(chunk)
                    if meta is not None:
                        meta.extend(chunk)
            if digest.hexdigest() != entry["sha256"]:
                raise ValueError(f"Member SHA-256 mismatch: {info.filename}")
            if entry.get("unity_guid") and unity_guid(bytes(meta or b"")) != entry["unity_guid"]:
                raise ValueError(f"Unity GUID mismatch: {info.filename}")
    if project_root is not None:
        for entry in data["files"]:
            path = destination(project_root, entry["path"])
            if not path.is_file() or path.stat().st_size != entry["bytes"] or sha256_file(path) != entry["sha256"]:
                raise ValueError(f"Restored file missing or changed: {entry['path']}")
    return {"success": True, "version": data["version"], "files": len(expected), "archive_bytes": archive.stat().st_size,
            "uncompressed_bytes": data["uncompressed_bytes"], "project_checked": project_root is not None}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--project-root", type=Path, default=Path.cwd())
    parser.add_argument("--check-project", action="store_true")
    args = parser.parse_args()
    try:
        result = verify_archive(args.archive, args.manifest, args.project_root if args.check_project else None)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    except (OSError, ValueError, zipfile.BadZipFile, json.JSONDecodeError) as error:
        print(f"Verification failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
