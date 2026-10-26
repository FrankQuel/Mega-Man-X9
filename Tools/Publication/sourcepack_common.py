"""Shared, standard-library-only validation for the downloadable Unity media pack."""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat

GIB = 1024 ** 3
MAX_ARCHIVE_BYTES = 2 * GIB - 1
MAX_UNCOMPRESSED_BYTES = 8 * GIB
MEDIA_SUFFIXES = frozenset({
    ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tga", ".tif", ".tiff", ".webp",
    ".psd", ".psb", ".exr", ".hdr", ".dds", ".wav", ".ogg", ".mp3", ".flac",
    ".aif", ".aiff", ".mp4", ".mov", ".webm", ".avi", ".mkv", ".ttf", ".otf",
    ".fbx", ".blend", ".obj", ".dll", ".so", ".dylib", ".bin", ".bytes",
    ".bundle", ".unitypackage", ".zip", ".7z", ".rar",
})
VERSION_RE = re.compile(r"[A-Za-z0-9][A-Za-z0-9._-]{0,79}\Z")
SHA_RE = re.compile(r"[a-f0-9]{64}\Z")
GUID_RE = re.compile(rb"^guid:\s*([a-fA-F0-9]{32})\s*$", re.MULTILINE)
RESERVED = {"CON", "PRN", "AUX", "NUL", *(f"COM{i}" for i in range(1, 10)), *(f"LPT{i}" for i in range(1, 10))}


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def unity_guid(data: bytes) -> str | None:
    match = GUID_RE.search(data)
    return match.group(1).decode("ascii").lower() if match else None


def is_reparse(path: Path) -> bool:
    info = path.lstat()
    return path.is_symlink() or bool(getattr(info, "st_file_attributes", 0) & 1024)


def safe_member(name: str) -> PurePosixPath:
    if not isinstance(name, str) or not name or "\\" in name or ":" in name or name.startswith("/"):
        raise ValueError("Unsafe archive path")
    parts = name.split("/")
    if any(not part or part in {".", ".."} or part.endswith((" ", ".")) or part.split(".")[0].upper() in RESERVED for part in parts):
        raise ValueError("Unsafe archive path")
    if not (name == "Assets/Recursos.meta" or name.startswith("Assets/Recursos/") or name.startswith("SourceArt/")):
        raise ValueError("Archive path is outside Assets/Recursos or SourceArt")
    return PurePosixPath(name)


def destination(root: Path, name: str) -> Path:
    relative = safe_member(name)
    root = root.resolve()
    target = root.joinpath(*relative.parts)
    if not target.resolve().is_relative_to(root):
        raise ValueError("Destination escapes the selected project")
    current = target
    while current != root:
        if current.exists() and is_reparse(current):
            raise ValueError("Destination contains a symbolic link or reparse point")
        current = current.parent
    return target


def regular_files(root: Path):
    if not root.is_dir() or is_reparse(root):
        raise ValueError(f"Missing or linked resource folder: {root.name}")
    for here, directories, names in os.walk(root, followlinks=False):
        for name in directories:
            if is_reparse(Path(here) / name):
                raise ValueError("Linked resource directory is not supported")
        for name in names:
            path = Path(here) / name
            if is_reparse(path) or not stat.S_ISREG(path.lstat().st_mode):
                raise ValueError("Linked or special resource file is not supported")
            yield path


def load_manifest(path: Path) -> dict:
    if path.stat().st_size > 16 * 1024 * 1024:
        raise ValueError("Manifest is too large")
    return validate_manifest(json.loads(path.read_text(encoding="utf-8")))


def validate_manifest(data: dict) -> dict:
    if not isinstance(data, dict) or data.get("schema_version") != 1 or data.get("format") != "unity-sourcepack":
        raise ValueError("Unsupported source-pack manifest")
    if not VERSION_RE.fullmatch(str(data.get("version", ""))):
        raise ValueError("Invalid source-pack version")
    archive = data.get("archive", {})
    if not SHA_RE.fullmatch(str(archive.get("sha256", ""))) or not isinstance(archive.get("bytes"), int) or not 0 < archive["bytes"] <= MAX_ARCHIVE_BYTES:
        raise ValueError("Invalid archive digest or size")
    files = data.get("files")
    if not isinstance(files, list) or not files or len(files) > 100000:
        raise ValueError("Invalid manifest file list")
    paths = set()
    total = 0
    for item in files:
        if not isinstance(item, dict):
            raise ValueError("Invalid manifest entry")
        name = item.get("path")
        safe_member(name)
        if name in paths or not isinstance(item.get("bytes"), int) or item["bytes"] < 0 or not SHA_RE.fullmatch(str(item.get("sha256", ""))):
            raise ValueError("Duplicate path or invalid manifest entry")
        paths.add(name)
        total += item["bytes"]
    if total > MAX_UNCOMPRESSED_BYTES or total != data.get("uncompressed_bytes") or len(files) != data.get("file_count"):
        raise ValueError("Manifest totals are invalid")
    for item in files:
        if item.get("kind") == "media" and item["path"].startswith("Assets/") and item["path"] + ".meta" not in paths:
            raise ValueError("Unity media is missing its .meta companion")
    return data
