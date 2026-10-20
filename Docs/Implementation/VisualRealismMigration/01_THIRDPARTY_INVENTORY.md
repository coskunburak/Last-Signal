# 01 — COMPLETE THIRDPARTY ASSET INVENTORY (PHASE B)

**Date:** 2026-10-20  
**Scope:** `Assets/ThirdParty/` (10,337 non-meta files, 11,176 unique GUIDs, ~6.31 GB) + root-level third-party folders (`Assets/TreeChopping-main`, `Assets/Unity-Tree-Chopping-Simulation-main`, `Assets/Noto_Sans`) + `Packages/manifest.json` (`com.dgreenheck.openfracture`).  
**Status:** `CONFIRMED` (Local filesystem scan completed without loading binary payloads into context).

---

## 1. Summary Statistics

- **Total Distinct Local Asset Packages / Sources:** 53 (49 under `Assets/ThirdParty/`, 3 loose root folders under `Assets/`, 1 Git UPM package `OpenFracture`).
- **Layout Audit (`Tools/thirdparty_audit.py`):** Reported 10,337 files and 11,176 GUIDs under `Assets/ThirdParty/`, noting 15 newer top-level folders and 3 loose `.exr` sky files added after the October 11 `ThirdPartyLayoutManifest.json` snapshot. All existing GUIDs are intact.

---

## 2. Complete Inventory by Category

### Category 1–5: Nature, Trees, Forest Floor, Vegetation, Rocks & Terrain

| # | Local Path | Pack / Asset Name | Publisher / Source | Size (MB) | Models (FBX/OBJ/Blend) | Prefabs | Mats | Shaders | Textures | Active Scene Usage |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---|
| 1 | `Assets/ThirdParty/Environments/FantasyEnvironments` | Fantasy Landscape | Pxltiger (Asset Store) | 218.53 | 154 FBX | 154 | 33 | 0 | 35 | **ACTIVE** in `S013Cabin`, `FirstTownCore`, `FirstTownCore_VisualSlice`, `FirstTownHarvest_*`, `FirstTownTreePhysics_*` (via `Art/S013` & `Art/FirstTownForest` derivatives) |
| 2 | `Assets/ThirdParty/Environments/Silver_Cats` | Hand Painted Nature Kit LITE | Silver Cats (Asset Store) | 237.13 | 8 FBX | 8 | 3 | 0 | 29 | Used only in `S013AssetComparison.unity` |
| 3 | `Assets/ThirdParty/Environments/HolotnaMountainURP` | Mountain — Stylized Fantasy Environment (URP Extracted) | Holotna (itch.io / Fab) | 32.72 | 10 FBX | 15 | 14 | 4 | 25 | Used only in `S013AssetComparison.unity` |
| 4 | `Assets/ThirdParty/Environments/Mountain (Unity 2022.3 1.16f1)` | Mountain Source Archive & Terrain Assets | Holotna | 584.06 | 0 (9 Terrain `.asset`, 3 `.unitypackage`) | 0 | 1 | 0 | 182 | Unpacked archive reference only |
| 5 | `Assets/ThirdParty/tree_stump_01_4k.blend` | Tree Stump 01 (4K Photogrammetry) | Poly Haven (CC0) | 51.55 | 1 `.blend` | 0 | 0 | 0 | 3 (4K JPG/EXR) | Imported locally (Oct 18); not yet wired into production prefabs |
| 6 | `Assets/ThirdParty/asphalt_02_4k.blend` | Asphalt 02 (4K PBR Surface) | Poly Haven (CC0) | 84.44 | 1 `.blend` | 0 | 0 | 0 | 4 (4K JPG/PNG/EXR) | Used as source texture for `FirstTown` / `VisualSlice` road materials |

### Category 6–8: Architecture, Urban Props & Interior

| # | Local Path | Pack / Asset Name | Publisher / Source | Size (MB) | Models | Prefabs | Mats | Shaders | Textures | Active Scene Usage |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---|
| 7 | `Assets/ThirdParty/VGtiXN/Countryside gas station` | Countryside Gas Station | VGtiXN (Asset Store) | 487.20 | 53 FBX | 59 | 31 | 0 | 135 | **ACTIVE** in `FirstTownCore` & `FirstTownCore_VisualSlice` (`Art/FirstTown/Prefabs/Canopy.prefab`, `ServiceShop.prefab`, etc.) |
| 8 | `Assets/ThirdParty/MarpaStudio` | Farm Environment (Built-In / URP / HDRP) | MarpaStudio / MarioParadiso (Asset Store) | 1046.93 | 37 FBX | 23 | 43 | 0 | 214 | Imported locally (Oct 15); contains Barn, Silo, WindMill, Shed, House, Fence, Well, WheelCart, Log, Tools with Built-In/URP/HDRP subfolders |
| 9 | `Assets/ThirdParty/Modular European House` | Modular European House | Asset Store | 203.88 | 22 FBX | 25 | 21 | 1 | 40 | Imported locally (Oct 15); includes `URP Assets` package & modular facade/roof meshes |
| 10 | `Assets/ThirdParty/Abandoned buildings` | Abandoned Buildings | Asset Store | 58.02 | 11 FBX | 12 | 9 | 0 | 13 | Imported locally (Oct 15); 2 ruined rural houses + stove/window/pole props |
| 11 | `Assets/ThirdParty/Free Wood Door Pack` | Free Wood Door Pack | Asset Store | 661.50 | 4 FBX | 105 | 111 | 0 | 64 | Imported locally (Oct 15); high-res 4K PBR doors + 2 audio clips |
| 12 | `Assets/ThirdParty/Shipping Container Environment` | Free Shipping Containers | Asset Store | 24.83 | 35 FBX | 35 | 2 | 0 | 4 | **ACTIVE** in `FirstTownCore` industrial yard (`Art/FirstTown/Prefabs/Container.prefab`) |
| 13 | `Assets/ThirdParty/S23 NEW ASSETS/Industrial building` | FREE Industrial Building / Garage | Asset Store | 9.56 | 22 FBX | 0 | 1 | 0 | 4 | **ACTIVE** in `FirstTownCore` (`Warehouse.prefab`, `CompoundFence.prefab`) |
| 14 | `Assets/ThirdParty/S23 NEW ASSETS/Industrial Props` | Industrial Props Kit | Asset Store | 5.35 | 11 FBX | 11 | 18 | 0 | 7 | **ACTIVE** in `FirstTownCore` (`Pallet.prefab`, cones, barriers) |
| 15 | `Assets/ThirdParty/S23 NEW ASSETS/factory_brick_4k.blend` | Factory Brick (4K PBR Surface) | Poly Haven (CC0) | 94.13 | 1 `.blend` | 0 | 0 | 0 | 4 (4K JPG/PNG/EXR) | Used for industrial masonry material derivatives |
| 16 | `Assets/ThirdParty/AssetsStore/Garage_props` | Garage Props Set | Asset Store | 272.94 | 13 FBX | 37 | 6 | 0 | 15 | Barrels, tanks, racks, benches, tables (imported Oct 15) |
| 17 | `Assets/ThirdParty/Factory Tools` | Workshop Tools — Free Low Poly Asset Pack | PolySquid / Asset Store | 1.24 | 22 FBX | 22 | 1 | 1 | 3 | Wrenches, saw, axe, tool racks (imported Oct 15) |
| 18 | `Assets/ThirdParty/Environments/BarnExtracted` & `barn-village-abandoned-house` | Abandoned Barn Village House Destroyed | Flawless Normals (Fab) | 35.23 | 1 FBX | 0 | 0 | 0 | 32 | Evaluated in `S013AssetComparison`; duplicate textures between ZIP folder and extracted folder |
| 19 | `Assets/ThirdParty/Environments/cupboard_pack.fbx` | Modular Wooden Storage & Cabinet Pack | 3dShowdown (Fab) | 8.27 | 1 FBX | 0 | 0 | 0 | 0 | Evaluated in `S013AssetComparison` (missing textures) |
| 20 | `Assets/ThirdParty/Environments/TT3D` | LowPoly Sci-Fi Starter Pack | TT3D | 1.09 | 3 FBX | 3 | 8 | 0 | 8 | Legacy graybox only |

### Category 9–12: Characters, Zombies, Weapons, Loot & Vehicles

| # | Local Path | Pack / Asset Name | Publisher / Source | Size (MB) | Models | Prefabs | Mats | Shaders | Textures | Active Scene Usage |
|---|---|---|---|---:|---:|---:|---:|---:|---:|---|
| 21 | `Assets/ThirdParty/Characters/Survival Char` | Arberry Survival Character | Asset Store / Fab | 599.05 | 1 FBX | 0 | 0 | 0 | 70 | **ACTIVE** primary player character body (`Art/CharacterIntegration/Arberry`) |
| 22 | `Assets/ThirdParty/Characters/Diesel` | Diesel Character (2 FBX copies w/ distinct GUIDs) | Mixamo / ThirdParty | 25.04 | 2 FBX | 0 | 0 | 0 | 0 | Legacy/fallback player body (`Art/CharacterIntegration`) |
| 23 | `Assets/ThirdParty/Characters/FirstPersonArms` | FirstPersonArms | ThirdParty | 16.30 | 1 FBX | 0 | 0 | 0 | 4 | Legacy FPS arms (`Prefabs/Combat/FPSArms.prefab`) |
| 24 | `Assets/ThirdParty/Characters/Fps Arms Pack [Updated] FBX` | FPS Arms Pack Updated | Asset Store | 88.90 | 9 FBX | 8 | 7 | 0 | 14 | Evaluated during S014/CharacterIntegration |
| 25 | `Assets/ThirdParty/Characters/VAL` | VAL Character | ThirdParty | 6.50 | 1 FBX | 0 | 0 | 0 | 0 | Unused character mesh |
| 26 | `Assets/ThirdParty/Animation/*` (5 subfolders) | `AnimationLibraries`, `CrouchAndSlide`, `KevinIglesias`, `ZOMBİE ATTACK ANIMATION`, `ZombieMotionClips` | Multiple | 214.59 | 136 FBX, 1 `.blend` | 17 | 5 | 1 | 9 | **ACTIVE** humanoid locomotion, melee, and zombie animation clips |
| 27 | `Assets/ThirdParty/Zombies/NewPunch` | Zombie / Shirtless Zombie | Studio New Punch | 92.08 | 2 FBX | 6 | 11 | 0 | 11 | **ACTIVE** primary zombie visual (`LS_Zombie_Shirtless_Visual.prefab`) |
| 28 | `Assets/ThirdParty/Zombies/ZombieMale_AAB` | ZombieMale_AAB | AAB | 106.47 | 2 FBX | 6 | 20 | 1 | 25 | **ACTIVE** S022 zombie archetype variant |
| 29 | `Assets/ThirdParty/Zombies/Zombie` | Realistic Zombie Pack | ThirdParty | 845.89 | 13 FBX, 1 OBJ | 1 | 8 | 1 | 15 | **ACTIVE** S022 zombie archetype variants |
| 30 | `Assets/ThirdParty/Zombies/SZombie` | SZombie | ThirdParty | 42.51 | 4 FBX, 1 `.blend` | 0 | 0 | 0 | 10 | Secondary zombie source |
| 31 | `Assets/ThirdParty/Zombies/ArtStore3D` | Fat Zombie (Low Poly) | ArtStore3D | 60.02 | 2 FBX | 2 | 2 | 0 | 8 | Secondary / low-poly zombie |
| 32 | `Assets/ThirdParty/Zombies/Zombie_Survival_AssetPack_LowPoly` | Zombie Survival LowPoly | ThirdParty | 1.00 | 1 FBX, 1 OBJ, 1 `.blend` | 0 | 0 | 0 | 1 | Unused low-poly |
| 33 | `Assets/ThirdParty/Weapons/wooden_axe_4k.blend` | Wooden Axe (4K PBR) | Poly Haven (CC0) | 33.14 | 1 `.blend` | 0 | 0 | 0 | 4 (4K) | **ACTIVE** primary player axe (`WoodenAxe` / `weapon.axe` / tree chopping) |
| 34 | `Assets/ThirdParty/Weapons/machete_4k.blend` | Machete (4K PBR) | Poly Haven (CC0) | 56.47 | 1 `.blend` | 0 | 0 | 0 | 4 (4K) | **ACTIVE** melee weapon (`Machete`) |
| 35 | `Assets/ThirdParty/Weapons/Gece Studio` | HK416 Rifle | Gece Studio | 209.29 | 1 FBX | 2 | 2 | 0 | 5 | **ACTIVE** rifle (`Art/CharacterIntegration`) |
| 36 | `Assets/ThirdParty/Weapons/PISTOLS` | Triple Pistols Source | ThirdParty | 552.72 | 3 FBX, 1 OBJ, 2 `.blend` | 0 | 0 | 0 | 84 | **ACTIVE** pistols (`Art/TriplePistols`) |
| 37 | `Assets/ThirdParty/Weapons/MRPoly` | Low-Poly Weapons | MRPoly | 0.56 | 3 FBX | 16 | 19 | 0 | 1 | Legacy world weapon placeholders |
| 38 | `Assets/ThirdParty/Weapons/CrowbarLowPoly` | Crowbar LowPoly | ThirdParty | 0.55 | 0 | 0 | 0 | 0 | 2 | Legacy crowbar |
| 39 | `Assets/ThirdParty/Loot/*` (3 subfolders) | `MedicinePack_JimmyPV` (9.53 MB), `Polygon-Lite Survival Collection` (4.44 MB), `stylooFoodAndKitchenAssetPackFreePack` (84.37 MB) | Multiple | 98.34 | 123 FBX | 69 | 11 | 0 | 10 | **ACTIVE** world loot & scavenging items (`S019Import`, `ItemCatalog`) |
| 40 | `Assets/ThirdParty/Vehicles/*` (3 subfolders) | `CharacterController` (0.21 MB), `Pack_Pickup` (0.85 MB), `Stylized Vehicles Pack Free` (1.92 MB) | MotionCore / Asset Store | 2.98 | 6 FBX | 6 | 11 | 0 | 5 | **ACTIVE** pickup vehicle mesh (`Pack_Pickup`) in `VehicleWorld` |

### Category 13–15: VFX, Audio, Rendering & Physics Utilities

| # | Local Path | Pack / Asset Name | Publisher / Source | Size (MB) | Files / Details | Active Scene Usage |
|---|---|---|---|---:|---|---|
| 41 | `Assets/ThirdParty/VFX/BloodDecals` & `Free Blood VFX` | Blood Decals & Vefects Blood VFX | Vefects / Asset Store | 80.60 | 7 FBX, 65 prefabs, 40 mats, 6 shaders, 75 textures | Reference / secondary; active blood uses custom `Assets/LastSignal/Shaders/VFX/Blood/LS_*.shader` |
| 42 | `Assets/ThirdParty/Audio/*` (7 subfolders) | `Essentials_Series_NOX_SOUND` (1094.06 MB, 1644 clips), `Horror_Music_Pack_Starter_Kit` (234.56 MB, 10 clips), `ZombieHorrorPackageFree` (13.58 MB, 44 clips), `FreeWeaponSFX_v1.1` (11.55 MB, 47 clips), `Tree` (5.37 MB, 5 clips), `kenney_impact-sounds` (1.05 MB, 130 clips), `VisceralBulletImpacts` (0.42 MB, 1 clip) | Multiple (NOX, Kenney, Freesound candidates, etc.) | 1360.59 | 1,881 audio files (`.wav`/`.ogg`/`.mp3`) | **ACTIVE** in `S015AudioCatalog` & `Audio/S004/ValidationPreview/` |
| 43 | `Assets/ThirdParty/Fantasy Skybox FREE` | Fantasy Skybox FREE | Render Knight (Asset Store) | 133.57 | 50 mats, 62 textures | **ACTIVE** (`FS003_Day_Sunless.png` used in `FirstTownAtmosphere`) |
| 44 | `Assets/ThirdParty/*.exr` (3 loose files) | `kloppenheim_01_puresky_4k.exr` (67.33 MB), `qwantani_sunset_puresky_4k.exr` (66.45 MB), `qwantani_night_puresky_4k.exr` (69.69 MB) | Poly Haven (CC0) | 203.47 | 3 4K HDRI `.exr` files | **ACTIVE** in `FirstTownAtmosphere` (`TownSky` material) |
| 45 | `Assets/ThirdParty/RainSnowCloudEffect` | Rain Snow Cloud Effect | Asset Store | 2.48 | 15 prefabs, 12 mats, 8 textures | **ACTIVE** (`RainStreak`, `Splash`, `Cloud` alpha textures used in `FirstTownAtmosphere`) |
| 46 | `Assets/ThirdParty/RainMaker` | Rain Maker — 2D and 3D Rain Particle System | Jeff Johnson / Digital Ruby (Asset Store) | 16.06 | 3 prefabs, 7 mats, 1 shader, 11 textures, 4 audio | Reference only (controller rejected to preserve `WorldClock` authority) |
| 47 | `Assets/ThirdParty/Frameworks/FirstPersonARPGStarterPack` | First Person ARPG Starter Pack | Asset Store | 7.54 | 3 FBX, 7 prefabs, 18 textures, 11 anims, 6 audio | Legacy reference only |
| 48 | `Assets/ThirdParty/Standard Assets` & `Samples` & `UI` & `Noto_Sans` | Unity Standard Assets (0.29 MB), URP Template (0.06 MB), Kenney Input Prompts 1.5 (17.29 MB, 3056 PNGs), Noto Sans Font (47.91 MB) | Unity / Kenney (CC0) / Google Fonts (OFL) | 65.55 | UI icons and fonts | **ACTIVE** UI prompts & typography |
| 49 | `Packages/manifest.json` (`com.dgreenheck.openfracture`) | OpenFracture v1.0.2 (`6d0ddff64173`) | Dan Greenheck (GitHub, MIT) | UPM | Runtime/Editor mesh slicing library | **ACTIVE** in Editor (`TreePhysicsAuthoring.cs`) for pre-slicing 128 notch mesh pairs |
| 50 | `Assets/TreeChopping-main` & `Assets/Unity-Tree-Chopping-Simulation-main` | Loose GitHub Tree Chopping Examples | GitHub (Unverified licenses) | 25.07 | C# sample scripts, `.exe`, `.wmv`, `.pdf` | **UNUSED** in runtime; `Unity-Tree-Chopping-Simulation-main` contains `.wmv` causing macOS import warning |

---

## 3. Duplicate & Hygiene Findings

1. **`BarnExtracted` vs `barn-village-abandoned-house`:** Both contain the same 16 textures (`22.79 MB` + `12.44 MB`), with `barn-village-abandoned-house` also storing the source `.zip`. Do not delete either without checking `S013AssetComparison.unity` GUID references.
2. **Diesel FBX Duplication:** `Assets/ThirdParty/Characters/Diesel/Diesel.fbx` and `S14 Source/Diesel.fbx` are byte-identical but have distinct `.meta` GUIDs recorded in `ThirdPartyLayout.md` — both must be preserved.
3. **Root-Level Example Pollution (`Assets/TreeChopping-main`, `Assets/Unity-Tree-Chopping-Simulation-main`):** Neither is referenced by `LastSignal.Runtime` or `LastSignal.Editor`. They sit outside `Assets/ThirdParty/` and are untracked by Git (`??`). However, per Section 1.1 rules, no files are deleted without explicit owner instruction.
