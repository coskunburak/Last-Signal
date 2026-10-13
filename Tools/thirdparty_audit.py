#!/usr/bin/env python3
"""Read-only integrity audit for the reorganized Unity vendor asset library."""

import hashlib
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
VENDOR = ROOT / "Assets/ThirdParty"
MANIFEST = ROOT / "Docs/Architecture/ThirdPartyLayoutManifest.json"
GUID = re.compile(r"^guid: ([0-9a-f]{32})$", re.MULTILINE)
STALE_PREFIXES = (
    "Assets/ThirdParty/S14 ASSETS/",
    "Assets/ThirdParty/S15 Sound Pack/",
    "Assets/ThirdParty/Vehicle Entegrations/",
    "Assets/ThirdParty/EnvironmentAsset/",
    "Assets/ThirdParty/FirstPersonARPGStarterPack/",
    "Assets/ThirdParty/UnityURPTemplate/",
    "Assets/ThirdParty/TT3D/",
    "Assets/ThirdParty/weapon/",
    "Assets/ThirdParty/VFX/Vefects/",
    "Assets/ThirdParty/ZOMBIE COMBAT, BALLISTIC FEEDBACK & GORE OVERHAUL/",
    "Assets/ThirdParty/Characters/VAL.fbx",
    "Assets/ThirdParty/Diesel.fbx",
)
INTENTIONALLY_MISSING_TEST_PATHS = {
    "Assets/ThirdParty/EnvironmentColor/a.png",
    "Assets/ThirdParty/sample.png",
}


def digest(values):
    return hashlib.sha256("\n".join(sorted(values)).encode()).hexdigest()


def main():
    data = json.loads(MANIFEST.read_text())
    failures = []
    paths = {entry.name for entry in VENDOR.iterdir() if entry.is_dir()}
    if paths != set(data["categories"]):
        failures.append(f"Unexpected top-level categories: {sorted(paths ^ set(data['categories']))}")
    for entry in VENDOR.iterdir():
        if entry.is_file() and entry.name not in {".DS_Store"} and not entry.name.endswith(".meta"):
            failures.append(f"Loose vendor asset: {entry.relative_to(ROOT)}")

    all_guids = {}
    file_guids = []
    for meta in VENDOR.rglob("*.meta"):
        asset = Path(str(meta)[:-5])
        if not asset.exists():
            failures.append(f"Orphan meta: {meta.relative_to(ROOT)}")
        match = GUID.search(meta.read_text(errors="replace"))
        if not match:
            failures.append(f"Missing GUID: {meta.relative_to(ROOT)}")
            continue
        guid = match.group(1)
        if guid in all_guids:
            failures.append(f"Duplicate GUID: {guid} ({all_guids[guid]}, {meta.relative_to(ROOT)})")
        all_guids[guid] = meta.relative_to(ROOT)
        if asset.is_file():
            file_guids.append(guid)
    file_count = 0
    for asset in VENDOR.rglob("*"):
        if asset.name == ".DS_Store" or asset.name.endswith(".meta"):
            continue
        if not Path(str(asset) + ".meta").is_file():
            failures.append(f"Missing meta: {asset.relative_to(ROOT)}")
        if asset.is_file():
            file_count += 1

    expected_files = data.get("verified_files", data["after_files"])
    expected_file_hash = data.get("verified_file_meta_guid_sha256", data["file_meta_guid_sha256"])
    expected_all_hash = data.get("verified_all_meta_guid_sha256", data["all_meta_guid_sha256"])
    if file_count != expected_files or file_count != len(file_guids):
        failures.append(f"Asset count changed: {file_count} files, {len(file_guids)} file GUIDs")
    if digest(file_guids) != expected_file_hash:
        failures.append("Vendor asset GUID inventory changed")
    if digest(all_guids) != expected_all_hash:
        failures.append("Vendor folder or asset GUID inventory changed")
    for old, new in data["moved"]:
        if not (ROOT / new).exists() or not Path(str(ROOT / new) + ".meta").is_file():
            failures.append(f"Relocated package missing: {new}")
        if (ROOT / old).exists():
            failures.append(f"Old package path still exists: {old}")
    for old, guid in data["retired_folder_guids"]:
        if (ROOT / old).exists() or guid in all_guids:
            failures.append(f"Retired wrapper remains: {old}")
    retired_guids = [guid.encode() for _, guid in data["retired_folder_guids"]]
    for path in (ROOT / "Assets").rglob("*"):
        if not path.is_file() or path.suffix not in {".asset", ".prefab", ".unity", ".mat", ".controller", ".anim"}:
            continue
        content = path.read_bytes()
        if any(guid in content for guid in retired_guids):
            failures.append(f"Serialized reference to retired folder GUID: {path.relative_to(ROOT)}")

    for base in (ROOT / "Assets/LastSignal/Scripts", ROOT / "ProjectSettings", ROOT / "Tools"):
        for path in base.rglob("*"):
            if not path.is_file() or path.suffix not in {".cs", ".py", ".asset", ".json"}:
                continue
            if path.name in {"reorganize_assets.py", "thirdparty_audit.py"}:
                # The first is a frozen historical plan; this file lists stale prefixes.
                continue
            content = path.read_text(errors="replace")
            for prefix in STALE_PREFIXES:
                if prefix in content:
                    failures.append(f"Stale asset path in {path.relative_to(ROOT)}: {prefix}")
            if path.suffix == ".cs":
                for literal in re.findall(r'"(Assets/ThirdParty/[^"\r\n]+)"', content):
                    if literal in INTENTIONALLY_MISSING_TEST_PATHS:
                        continue
                    if (literal.endswith("/") or Path(literal).suffix) and not (ROOT / literal).exists():
                        failures.append(f"Missing source path in {path.relative_to(ROOT)}: {literal}")

    print(f"ThirdParty: {file_count} assets, {len(all_guids)} unique GUIDs, {len(failures)} issues")
    for failure in failures[:50]:
        print(failure)
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
