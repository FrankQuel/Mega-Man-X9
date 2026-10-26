"""Download explicitly named HTTPS release assets, verify them, and optionally restore a Unity checkout."""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import zipfile

from sourcepack_common import MAX_ARCHIVE_BYTES, SHA_RE, destination, sha256_file, validate_manifest
from verify_sourcepack import verify_archive


def checked_url(value: str) -> str:
    parsed = urllib.parse.urlsplit(value)
    if parsed.scheme != "https" or not parsed.hostname or parsed.username is not None or parsed.password is not None:
        raise ValueError("Use an explicit HTTPS URL without embedded credentials")
    return value


def fetch(url: str, destination_path: Path, limit: int):
    request = urllib.request.Request(checked_url(url), headers={"User-Agent": "Unity-sourcepack-downloader/1"})
    with urllib.request.urlopen(request, timeout=60) as response:
        if urllib.parse.urlsplit(response.geturl()).scheme != "https":
            raise ValueError("Insecure download redirect")
        declared = response.headers.get("Content-Length")
        if declared and int(declared) > limit:
            raise ValueError("Download exceeds its allowed size")
        total = 0
        with destination_path.open("xb") as output:
            for chunk in iter(lambda: response.read(1024 * 1024), b""):
                total += len(chunk)
                if total > limit:
                    raise ValueError("Download exceeds its allowed size")
                output.write(chunk)


def extract_verified(archive: Path, manifest: Path, project_root: Path) -> int:
    verify_archive(archive, manifest)
    data = validate_manifest(json.loads(manifest.read_text(encoding="utf-8")))
    root = project_root.resolve()
    if not (root / "Assets").is_dir() or not (root / "ProjectSettings").is_dir():
        raise ValueError("Extract into a cloned Unity project containing Assets and ProjectSettings")
    existing = []
    for entry in data["files"]:
        path = destination(root, entry["path"])
        if path.exists():
            if not path.is_file() or path.stat().st_size != entry["bytes"] or sha256_file(path) != entry["sha256"]:
                raise ValueError(f"Existing file differs; nothing was overwritten: {entry['path']}")
            existing.append(entry["path"])
    present = set(existing); restored = 0
    with zipfile.ZipFile(archive) as package:
        for entry in data["files"]:
            if entry["path"] in present:
                continue
            path = destination(root, entry["path"])
            path.parent.mkdir(parents=True, exist_ok=True)
            created = False
            try:
                with path.open("xb") as output:
                    created = True
                    with package.open(entry["path"]) as source:
                        shutil.copyfileobj(source, output, 1024 * 1024)
                if sha256_file(path) != entry["sha256"]:
                    raise ValueError(f"Restored file checksum mismatch: {entry['path']}")
                restored += 1
            except Exception:
                if created and path.exists():
                    path.unlink()  # Only the new file created by this exact operation.
                raise
    verify_archive(archive, manifest, root)
    return restored


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--archive-url", required=True)
    parser.add_argument("--manifest-url", required=True)
    parser.add_argument("--archive-sha256", required=True)
    parser.add_argument("--project-root", type=Path, default=Path.cwd())
    parser.add_argument("--download-dir", type=Path, default=None)
    parser.add_argument("--extract", action="store_true")
    args = parser.parse_args()
    try:
        expected = args.archive_sha256.lower()
        if not SHA_RE.fullmatch(expected):
            raise ValueError("Supply the published 64-character archive SHA-256")
        checked_url(args.archive_url); checked_url(args.manifest_url)
        root = args.project_root.resolve()
        output = (args.download_dir or root / "Artifacts/Downloads").resolve()
        if output.is_relative_to(root / "Assets"):
            raise ValueError("Downloads must stay outside Assets")
        output.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(prefix="sourcepack-download-", dir=output) as temporary:
            staging = Path(temporary)
            manifest = staging / "manifest.json"; archive = staging / "resources.zip"
            fetch(args.manifest_url, manifest, 16 * 1024 * 1024)
            data = validate_manifest(json.loads(manifest.read_text(encoding="utf-8")))
            if data["archive"]["sha256"] != expected:
                raise ValueError("Manifest does not match the independently supplied archive SHA-256")
            fetch(args.archive_url, archive, MAX_ARCHIVE_BYTES)
            result = verify_archive(archive, manifest)
            final_archive = output / f"resources-{data['version']}.zip"
            final_manifest = output / f"resources-manifest-{data['version']}.json"
            for source, final in ((archive, final_archive), (manifest, final_manifest)):
                if final.exists():
                    if sha256_file(source) != sha256_file(final):
                        raise FileExistsError("A different downloaded artifact already exists; choose another download directory")
                else:
                    with final.open("xb") as target, source.open("rb") as origin:
                        shutil.copyfileobj(origin, target, 1024 * 1024)
            if args.extract:
                result["restored_files"] = extract_verified(final_archive, final_manifest, root)
            result["extracted"] = args.extract
            print(json.dumps(result, ensure_ascii=False, indent=2)); return 0
    except urllib.error.HTTPError as error:
        print(f"Download failed with HTTP {error.code}", file=sys.stderr); return 1
    except urllib.error.URLError:
        print("Download failed: HTTPS connection unavailable", file=sys.stderr); return 1
    except (OSError, ValueError, zipfile.BadZipFile, json.JSONDecodeError) as error:
        print(f"Source-pack restore failed: {error}", file=sys.stderr); return 1


if __name__ == "__main__":
    raise SystemExit(main())
