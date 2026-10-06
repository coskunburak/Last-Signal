#!/usr/bin/env python3
"""One-time, GUID-preserving Unity asset layout migration and integrity check.

Run --apply only with the Unity Editor closed. --check is safe at any time.
The generated manifest records every moved asset and its original GUID.
"""

import argparse
import json
import re
import time
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
MANIFEST = ROOT / "Docs/Architecture/AssetHierarchyManifest.json"
HEARTBEAT = ROOT / "Temp/LastSignalZombieValidation-heartbeat"
GUID = re.compile(r"^guid: ([0-9a-f]{32})$", re.MULTILINE)


def meta_guid(path):
    meta = Path(str(path) + ".meta")
    if not meta.is_file():
        raise RuntimeError(f"Missing Unity meta file: {meta}")
    match = GUID.search(meta.read_text(errors="replace"))
    if not match:
        raise RuntimeError(f"Missing GUID: {meta}")
    return match.group(1)


def folder(path):
    if path == ASSETS or path.is_dir():
        return
    folder(path.parent)
    path.mkdir()
    import uuid
    Path(str(path) + ".meta").write_text(
        "fileFormatVersion: 2\n"
        f"guid: {uuid.uuid4().hex}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData:\n"
        "  assetBundleName:\n"
        "  assetBundleVariant:\n"
    )


def rel(path):
    return path.relative_to(ROOT).as_posix()


def plan():
    files = {}
    dirs = {}

    def asset(source, destination):
        src, dst = ROOT / source, ROOT / destination
        if not src.is_file():
            raise RuntimeError(f"Missing source asset: {source}")
        if source in files or destination in files.values():
            raise RuntimeError(f"Duplicate asset move: {source} -> {destination}")
        files[source] = destination

    def directory(source, destination):
        if not (ROOT / source).is_dir():
            raise RuntimeError(f"Missing source directory: {source}")
        dirs[source] = destination

    def by_type(source, destinations):
        for path in sorted((ROOT / source).rglob("*")):
            if not path.is_file() or path.name.endswith(".meta") or path.name == ".DS_Store":
                continue
            extension = path.suffix.lower()
            if extension not in destinations:
                raise RuntimeError(f"Unclassified asset: {rel(path)}")
            asset(rel(path), f"{destinations[extension]}/{path.name}")

    # Studio-owned content: every prefab is under Prefabs (including Resources).
    directory("Assets/Resources", "Assets/LastSignal/Prefabs/Resources")
    directory("Assets/Game/Items/Definitions", "Assets/LastSignal/Data/Items/Definitions")
    directory("Assets/Game/Items/Prefabs", "Assets/LastSignal/Prefabs/Items")
    directory("Assets/LastSignal/Loot/Profiles", "Assets/LastSignal/Data/Loot/Profiles")
    directory("Assets/LastSignal/Loot/Materials", "Assets/LastSignal/Materials/Loot")
    directory("Assets/Settings", "Assets/LastSignal/Settings/Rendering")

    for path in sorted((ROOT / "Assets/LastSignal/Scenes").glob("*.unity")):
        section = "Production" if path.stem in {"IntegratedGraybox", "RelayExpedition"} else "Validation"
        asset(rel(path), f"Assets/LastSignal/Scenes/{section}/{path.name}")

    by_type("Assets/LastSignal/WorldCells", {
        ".prefab": "Assets/LastSignal/Prefabs/WorldCells",
        ".asset": "Assets/LastSignal/Data/WorldCells",
    })
    by_type("Assets/LastSignal/Shelter", {
        ".prefab": "Assets/LastSignal/Prefabs/Shelter",
        ".asset": "Assets/LastSignal/Data/Shelter",
        ".mat": "Assets/LastSignal/Materials/Shelter",
    })
    by_type("Assets/LastSignal/Combat/Crowbar", {
        ".prefab": "Assets/LastSignal/Prefabs/Combat/Crowbar",
        ".asset": "Assets/LastSignal/Data/Combat/Crowbar",
        ".mat": "Assets/LastSignal/Materials/Combat/Crowbar",
        ".controller": "Assets/LastSignal/Animations/Weapons/Crowbar",
        ".anim": "Assets/LastSignal/Animations/Weapons/Crowbar",
        ".obj": "Assets/LastSignal/Models/Weapons/Crowbar/Source",
        ".mtl": "Assets/LastSignal/Models/Weapons/Crowbar/Source",
        ".png": "Assets/LastSignal/Models/Weapons/Crowbar/Source",
    })
    by_type("Assets/LastSignal/Combat/Optics", {
        ".asset": "Assets/LastSignal/Models/Weapons/Optics",
        ".mat": "Assets/LastSignal/Materials/Combat/Optics",
    })
    by_type("Assets/LastSignal/Blood VFX/ProjectOwned", {
        ".prefab": "Assets/LastSignal/Prefabs/VFX/Blood",
        ".asset": "Assets/LastSignal/Data/VFX/Blood",
        ".mat": "Assets/LastSignal/Materials/VFX/Blood",
        ".shader": "Assets/LastSignal/Shaders/VFX/Blood",
    })
    by_type("Assets/LastSignal/Assets/Zombie/Enemies/Zombie", {
        ".prefab": "Assets/LastSignal/Prefabs/Enemies/Zombie",
        ".asset": "Assets/LastSignal/Data/Enemies/Zombie",
        ".mat": "Assets/LastSignal/Materials/Enemies/Zombie",
        ".png": "Assets/LastSignal/Art/Textures/Zombie",
        ".unity": "Assets/LastSignal/Scenes/Validation",
    })
    by_type("Assets/LastSignal/S012", {".asset": "Assets/LastSignal/Data/WorldCells"})
    by_type("Assets/LastSignal/Scripts/Runtime/Combat", {
        ".cs": "Assets/LastSignal/Scripts/Runtime/Combat",
        ".asset": "Assets/LastSignal/Data/Combat",
    })
    # The source script directory is already correct; omit its .cs entries.
    files = {src: dst for src, dst in files.items() if src != dst}
    files["Assets/LastSignal/Combat/Crowbar/CrowbarMesh.asset"] = "Assets/LastSignal/Models/Weapons/Crowbar/CrowbarMesh.asset"
    files["Assets/LastSignal/Assets/Zombie/Enemies/Zombie/Prefabs/SM_Zombie_TorsoWound_Quad.asset"] = "Assets/LastSignal/Models/Enemies/Zombie/SM_Zombie_TorsoWound_Quad.asset"
    files["Assets/LastSignal/Combat/Optics/MRPoly_Scope.asset"] = "Assets/LastSignal/Data/Combat/Optics/MRPoly_Scope.asset"

    asset("Assets/LastSignal/Prefabs/Player.prefab", "Assets/LastSignal/Prefabs/Player/Player.prefab")
    asset("Assets/LastSignal/Prefabs/Door.prefab", "Assets/LastSignal/Prefabs/Interactions/Door.prefab")
    asset("Assets/InputSystem_Actions.inputactions", "Assets/LastSignal/Settings/Input/InputSystem_Actions.inputactions")
    asset("Assets/Editor/BuildScript.cs", "Assets/LastSignal/Scripts/Editor/BuildScript.cs")
    asset("Assets/Readme.asset", "Assets/ThirdParty/UnityURPTemplate/Readme.asset")
    asset("Assets/Scenes/SampleScene.unity", "Assets/ThirdParty/UnityURPTemplate/Scenes/SampleScene.unity")
    asset("Assets/_Recovery/0.unity", "Assets/LastSignal/Scenes/Recovery/0.unity")

    # Keep imported packages intact. Only the project's authored Enemies assets
    # (planned above) are extracted before their containing vendor tree moves.
    vendors = {
        "Assets/LastSignal/Assets/Animation": "Assets/ThirdParty/Animation/AnimationLibraries",
        "Assets/LastSignal/Assets/Kevin Iglesias": "Assets/ThirdParty/Animation/KevinIglesias",
        "Assets/LastSignal/Assets/crouch and slide animation": "Assets/ThirdParty/Animation/CrouchAndSlide",
        "Assets/LastSignal/Assets/First Person ARPG Starter Pack": "Assets/ThirdParty/FirstPersonARPGStarterPack",
        "Assets/LastSignal/Assets/Loot": "Assets/ThirdParty/Loot",
        "Assets/LastSignal/Assets/MR POLY": "Assets/ThirdParty/Weapons/MRPoly",
        "Assets/LastSignal/Assets/tt-3d": "Assets/ThirdParty/Environment/TT3D",
        "Assets/LastSignal/Assets/Zombie": "Assets/ThirdParty/Zombies",
        "Assets/LastSignal/Assets/crowbar-game-ready-low-poly": "Assets/ThirdParty/Weapons/CrowbarLowPoly",
        "Assets/LastSignal/Assets/first-person-arms": "Assets/ThirdParty/Characters/FirstPersonArms",
        "Assets/LastSignal/Blood VFX/Vefects": "Assets/ThirdParty/VFX/Vefects",
    }
    for source, destination in vendors.items():
        directory(source, destination)
    asset("Assets/LastSignal/Assets/VAL.fbx", "Assets/ThirdParty/Characters/VAL.fbx")

    return files, dirs


def move_file(source, destination):
    src, dst = ROOT / source, ROOT / destination
    folder(dst.parent)
    if dst.exists() or Path(str(dst) + ".meta").exists():
        raise RuntimeError(f"Destination exists: {destination}")
    src.rename(dst)
    Path(str(src) + ".meta").rename(Path(str(dst) + ".meta"))


def remove_empty_old_folders():
    candidates = [
        "Assets/Game", "Assets/Resources", "Assets/Settings", "Assets/Scenes",
        "Assets/_Recovery", "Assets/Editor", "Assets/LastSignal/Assets",
        "Assets/LastSignal/Blood VFX", "Assets/LastSignal/Combat",
        "Assets/LastSignal/Loot", "Assets/LastSignal/Shelter",
        "Assets/LastSignal/WorldCells", "Assets/LastSignal/S012",
    ]
    for source in candidates:
        root = ROOT / source
        if not root.exists():
            continue
        for path in sorted((p for p in root.rglob("*") if p.is_dir()), key=lambda p: len(p.parts), reverse=True) + [root]:
            if not path.exists():
                continue
            ds_store = path / ".DS_Store"
            if ds_store.exists():
                ds_store.unlink()
            if not any(path.iterdir()):
                path.rmdir()
                meta = Path(str(path) + ".meta")
                if meta.exists():
                    meta.unlink()


def remove_empty_zombie_owned_tree():
    root = ROOT / "Assets/LastSignal/Assets/Zombie/Enemies"
    for path in sorted((p for p in root.rglob("*") if p.is_dir()), key=lambda p: len(p.parts), reverse=True) + [root]:
        if path.exists() and not any(path.iterdir()):
            path.rmdir()
            Path(str(path) + ".meta").unlink()


def replace_paths(files, dirs):
    # Unity serializes Inspector references by GUID. These replacements cover
    # authored string paths used by Editor builders, tests and build settings.
    replacements = list(files.items()) + [(src + "/", dst + "/") for src, dst in dirs.items()]
    replacements.sort(key=lambda pair: len(pair[0]), reverse=True)
    changed = []
    roots = [ROOT / x for x in ("Assets/LastSignal/Scripts", "Tools", "ProjectSettings", "Packages")]
    suffixes = {".cs", ".py", ".json", ".asset", ".inputactions", ".shader", ".hlsl", ".asmdef", ".txt"}
    for base in roots:
        for path in base.rglob("*"):
            if not path.is_file() or path.suffix not in suffixes or path.name.endswith(".meta"):
                continue
            if path.resolve() == Path(__file__).resolve():
                continue
            try:
                before = path.read_text()
            except (UnicodeError, OSError):
                continue
            after = before
            for old, new in replacements:
                after = after.replace(old, new)
            if after != before:
                path.write_text(after)
                changed.append(rel(path))
    return changed


def check():
    data = json.loads(MANIFEST.read_text())
    failures = []
    all_guids = {}
    for path in ASSETS.rglob("*"):
        if path.name == ".DS_Store":
            continue
        if path.name.endswith(".meta"):
            if not Path(str(path)[:-5]).exists():
                failures.append(f"Orphan meta: {rel(path)}")
        elif not Path(str(path) + ".meta").is_file():
            failures.append(f"Missing meta: {rel(path)}")
        if path.is_file() and path.suffix == ".prefab" and not path.is_relative_to(ASSETS / "ThirdParty") and not path.is_relative_to(ASSETS / "LastSignal/Prefabs"):
            failures.append(f"Studio prefab outside Prefabs: {rel(path)}")
        if path.is_file() and path.suffix == ".unity" and not path.is_relative_to(ASSETS / "ThirdParty") and not path.is_relative_to(ASSETS / "LastSignal/Scenes") and not path.name.startswith("InitTestScene"):
            failures.append(f"Studio scene outside Scenes: {rel(path)}")
    for meta in ASSETS.rglob("*.meta"):
        match = GUID.search(meta.read_text(errors="replace"))
        if not match:
            failures.append(f"Missing GUID in {rel(meta)}")
            continue
        guid = match.group(1)
        if guid in all_guids:
            failures.append(f"Duplicate GUID {guid}: {all_guids[guid]}, {rel(meta)}")
        all_guids[guid] = rel(meta)
    for item in data["assets"]:
        path = ROOT / item["to"]
        if not path.is_file() or meta_guid(path) != item["guid"]:
            failures.append(f"Moved asset missing or GUID changed: {item['to']}")
        if (ROOT / item["from"]).exists():
            failures.append(f"Old asset path still exists: {item['from']}")
    for item in data["directories"]:
        path = ROOT / item["to"]
        if not path.is_dir() or meta_guid(path) != item["guid"]:
            failures.append(f"Moved directory missing or GUID changed: {item['to']}")
    print(f"Moved assets: {len(data['assets'])}; moved folders: {len(data['directories'])}; unique GUIDs: {len(all_guids)}; failures: {len(failures)}")
    for failure in failures[:50]:
        print(failure)
    return 1 if failures else 0


def apply():
    if HEARTBEAT.exists() and time.time() - HEARTBEAT.stat().st_mtime < 30:
        raise RuntimeError("Close the Unity Editor before moving assets on disk.")
    if MANIFEST.exists():
        raise RuntimeError("Migration manifest already exists; run --check instead.")
    files, dirs = plan()
    # Verify the entire plan before changing any file.
    targets = list(files.values()) + list(dirs.values())
    if len(targets) != len(set(targets)):
        raise RuntimeError("Two assets target the same path")
    for source, destination in files.items():
        meta_guid(ROOT / source)
        if (ROOT / destination).exists():
            raise RuntimeError(f"Destination exists: {destination}")
    for source, destination in dirs.items():
        meta_guid(ROOT / source)
        if (ROOT / destination).exists():
            raise RuntimeError(f"Destination exists: {destination}")
    records = [{"from": source, "to": dest, "guid": meta_guid(ROOT / source)} for source, dest in files.items()]
    directory_records = [{"from": source, "to": dest, "guid": meta_guid(ROOT / source)} for source, dest in dirs.items()]
    # All granular moves first; vendor trees then contain only imported content.
    for source, destination in files.items():
        move_file(source, destination)
    remove_empty_zombie_owned_tree()
    for source, destination in dirs.items():
        src, dst = ROOT / source, ROOT / destination
        folder(dst.parent)
        src.rename(dst)
        Path(str(src) + ".meta").rename(Path(str(dst) + ".meta"))
    remove_empty_old_folders()
    changed_sources = replace_paths(files, dirs)
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST.write_text(json.dumps({"assets": records, "directories": directory_records, "updated_sources": changed_sources}, indent=2, ensure_ascii=False) + "\n")
    print(f"Moved {len(records)} individual assets and {len(directory_records)} folders; updated {len(changed_sources)} source files.")
    return check()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--apply", action="store_true")
    group.add_argument("--check", action="store_true")
    args = parser.parse_args()
    raise SystemExit(apply() if args.apply else check())
