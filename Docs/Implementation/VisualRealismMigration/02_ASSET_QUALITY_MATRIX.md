# 02 — ASSET QUALITY & PRODUCTION ACCEPTANCE MATRIX (PHASE B)

**Date:** 2026-10-20  
**Target Standard:** Grounded High-Fidelity Realism — Survival Horror Environment (Sons of the Forest visual benchmark)  
**Scoring Weights:** Visual Quality (30%) · Art Direction Consistency (20%) · Performance & LOD (15%) · Pipeline Compatibility (15%) · Gameplay/Physics Integration (10%) · Maintenance & Production Cost (10%).  
**License Gate:** Evaluated independently as a mandatory release gate (`VERIFIED_CC0`, `VERIFIED_MIT`, `STANDARD_EULA_RECEIPT_PENDING`, `LICENSE_PENDING_BLOCKED`). A high technical score never overrides a missing license.

---

## 1. Nature, Foliage, Trees & Terrain Matrix

| Asset Pack | Category | Visual Style | Visual (30) | Consistency (20) | Perf/LOD (15) | Pipeline (15) | Physics/Cut (10) | Maint (10) | Total (/100) | License Gate | Classification & Rationale |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|
| **FantasyEnvironments** (`Pine_tree1`, `Birch_tree1`, `Bush1`, `Rock1`, `Grass1`) | Trees / Foliage / Terrain | Painted / Mid-Poly Stylized Realism | 11 | 7 | 7 | 12 | 8 | 8 | **53** | `STANDARD_EULA_RECEIPT_PENDING` | **REPLACE (Hero/Forest Visuals) / LEGACY_FALLBACK**<br>Single-tier geometry (1,554 tris Pine, 1,192 tris Birch; cull-only LOD, no mesh decimation or billboard), diffuse-only painted bark/ground without micro-normal/roughness depth, repetitive conical silhouette. Proven compatible with S004 `OpenFracture` 16-level slicing (`8/10` physics), so keep as fallback/test fixture while replacing hero forest meshes. |
| **Silver_Cats** (`Hand_Painted_Nature_Kit_LITE`) | Trees / Foliage | Hand-Painted Stylized | 6 | 3 | 6 | 8 | 2 | 5 | **30** | `STANDARD_EULA_RECEIPT_PENDING` | **REPLACE / REJECT**<br>Explicitly hand-painted gradients; incompatible with grounded survival horror PBR realism. |
| **HolotnaMountainURP** & **Mountain (2022.3)** | Rocks / Trees / Terrain | Stylized Low/Mid-Poly | 10 | 6 | 8 | 10 | 3 | 5 | **42** | `LICENSE_PENDING_BLOCKED` | **BLOCKED / REPLACE**<br>Stylized foliage/rocks; itch.io/Fab channel license grant unverified locally (`S013_ASSET_REGISTRY.md`). Do not use in production. |
| **Poly Haven `tree_stump_01_4k.blend`** | Forest Floor / Stumps | Photogrammetry PBR (4K) | 28 | 19 | 8 | 13 | 7 | 8 | **83** | `VERIFIED_CC0` | **ADAPT**<br>Photorealistic mossy bark/roots (`diff`, `nor_gl`, `rough` 4K). Needs Blender/Unity FBX export with 3 LOD tiers (`LOD0` ~3k tris, `LOD1` ~1k, `LOD2` ~350) and 2K packed textures before scatter use. |
| **Poly Haven `asphalt_02_4k.blend`** | Terrain / Roads | Photogrammetry PBR (4K) | 27 | 19 | 12 | 14 | 9 | 9 | **90** | `VERIFIED_CC0` | **KEEP / ADAPT**<br>High-fidelity cracked asphalt PBR maps (`diff`, `disp`, `nor_gl`, `rough`). Adapt with OpenGL→DirectX/Unity normal check, packed mask map (`Metallic/AO/Detail/Smoothness`), and road-shoulder blend mask. |

---

## 2. Architecture, Urban Props & Industrial Matrix

| Asset Pack | Category | Visual Style | Visual (30) | Consistency (20) | Perf/LOD (15) | Pipeline (15) | Physics/Cut (10) | Maint (10) | Total (/100) | License Gate | Classification & Rationale |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|
| **VGtiXN Countryside Gas Station** | Architecture (Hero POI) | Realistic PBR | 23 | 16 | 10 | 11 | 8 | 7 | **75** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT**<br>Strong grounded rural architecture; already staged in `FirstTownCore` & `VisualSlice` (with Refinement 05b canopy weatherproofing). Needs PBR roughness/albedo normalization for URP/HDRP parity and receipt verification. |
| **MarpaStudio Farm Environment** | Architecture / Rural Props | Realistic PBR (Built-In, URP, HDRP included) | 24 | 17 | 11 | 15 | 8 | 8 | **83** | `STANDARD_EULA_RECEIPT_PENDING` | **KEEP / ADAPT**<br>Contains 37 realistic rural models (`BarnBase`, `Silo`, `Shed`, `OldShed`, `House`, `Fence`, `Gate`, `Well`, `WheelCart`, `Log`, `Shovel`, `Rake`) and **native Built-In, URP, and HDRP material/prefab folders**. Ideal for rural compound & forest-edge integration once receipt is logged. |
| **Free Wood Door Pack** | Interior / Doors | Realistic PBR (4K) | 25 | 17 | 10 | 12 | 8 | 7 | **79** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT**<br>High-detail PBR wood doors; downscale 4K textures to 2K max in project-owned derivatives and bind to existing `DoorInteractable` domain. |
| **Modular European House** | Architecture | Semi-Realistic PBR (URP included) | 20 | 13 | 11 | 13 | 7 | 7 | **71** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT (Town Center Only)**<br>Clean European facade; requires grunge/weathering/roughness adaptation to match abandoned rural tone. |
| **Abandoned Buildings** | Architecture (Ruins) | Mid-Detail PBR | 19 | 15 | 10 | 11 | 7 | 7 | **69** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT (Secondary Ruins)**<br>2 ruined wooden houses + stove/poles. Good silhouette for deep-forest/outskirt ruins; textures need normal/roughness tuning. |
| **Shipping Container Environment** | Industrial | Realistic PBR | 21 | 16 | 12 | 11 | 8 | 8 | **76** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT**<br>Active in `FirstTownCore` industrial yard; legacy 2016 materials already wrapped in URP owned materials; easily portable to HDRP Lit. |
| **S23 Industrial Building & Industrial Props** | Industrial / Urban Props | Realistic Low/Mid-Poly PBR | 20 | 15 | 13 | 12 | 8 | 8 | **76** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT**<br>Active in `FirstTownCore` (`Warehouse`, `CompoundFence`, `Pallet`, cones). Low triangle count; needs higher-detail PBR trim/normal maps when viewed close-up. |
| **Poly Haven `factory_brick_4k.blend`** | Architecture Surface | Photogrammetry PBR (4K) | 27 | 18 | 12 | 14 | 9 | 9 | **89** | `VERIFIED_CC0` | **KEEP / ADAPT**<br>High-quality CC0 masonry surface for industrial walls. |
| **AssetsStore Garage_props** | Interior / Industrial Props | Realistic PBR | 22 | 16 | 11 | 11 | 8 | 8 | **76** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT**<br>Good barrels, tanks, benches, racks; cap 4K textures to 1K/2K and create URP/HDRP Lit derivatives. |
| **Factory Tools** | Interior Props | Low-Poly Stylized PBR | 13 | 9 | 13 | 12 | 7 | 7 | **61** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT (Background Shelf Only) / REPLACE (Hero)**<br>Low-poly faceted silhouettes; acceptable only for distant shelf clutter, not close-up inspection. |
| **Flawless Normals Abandoned Barn** (`BarnExtracted`) | Architecture | Photogrammetry / Scan | 21 | 15 | 8 | 10 | 5 | 5 | **64** | `LICENSE_PENDING_BLOCKED` | **BLOCKED**<br>Open roof (invalid as `RainRoof` shelter) and Fab license terms unverified locally. |
| **cupboard_pack.fbx** | Interior | Untextured FBX | 8 | 5 | 10 | 8 | 5 | 4 | **40** | `LICENSE_PENDING_BLOCKED` | **BLOCKED / REPLACE**<br>Missing source textures and unverified Fab license. |
| **TT3D LowPoly Sci-Fi** | Graybox | Low-Poly Sci-Fi | 4 | 1 | 14 | 12 | 5 | 6 | **42** | `STANDARD_EULA_RECEIPT_PENDING` | **REPLACE / REFERENCE_ONLY**<br>Genre mismatch (Sci-Fi low-poly). |

---

## 3. Atmosphere, Sky, Weather & VFX Matrix

| Asset Pack | Category | Visual Style | Visual (30) | Consistency (20) | Perf/LOD (15) | Pipeline (15) | Physics/Cut (10) | Maint (10) | Total (/100) | License Gate | Classification & Rationale |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|---|---|
| **Poly Haven 4K Skies** (`kloppenheim_01`, `qwantani_sunset`, `qwantani_night`) | Rendering / Sky | Real HDRI Panoramas (4K EXR) | 27 | 19 | 13 | 14 | 10 | 9 | **92** | `VERIFIED_CC0` | **KEEP**<br>Physically grounded EXR panoramas driving `FirstTownAtmosphere`; reusable in both URP (`TownSky.shader`) and HDRP (HDRI Sky / custom sky blend). |
| **Fantasy Skybox FREE** (`FS003_Day_Sunless`) | Rendering / Sky | Painted Panorama | 16 | 11 | 13 | 13 | 10 | 8 | **71** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT / REPLACE (in HDRP)**<br>Currently used for sunless day base in `TownSky.shader`; slightly soft/painted compared to 4K EXR skies. In HDRP Physical Sky or with a CC0 clear-day EXR, replace with photorealistic sky. |
| **RainSnowCloudEffect** | VFX / Weather | Alpha Particle Textures | 20 | 15 | 12 | 12 | 9 | 8 | **76** | `STANDARD_EULA_RECEIPT_PENDING` | **KEEP / ADAPT**<br>Provides streak/splash/cloud alpha maps used by project-owned `TownRain` bounded particle system. |
| **RainMaker** | VFX / Weather | Legacy Particle + Script | 14 | 10 | 8 | 7 | 3 | 5 | **47** | `STANDARD_EULA_RECEIPT_PENDING` | **REFERENCE_ONLY**<br>Runtime controller conflicts with `WorldClock` / `TownAtmosphere` single authority. Never instantiate in production scenes. |
| **BloodDecals & Free Blood VFX** | VFX | Stylized / Semi-Real | 18 | 13 | 11 | 9 | 8 | 7 | **66** | `STANDARD_EULA_RECEIPT_PENDING` | **ADAPT / REFERENCE_ONLY**<br>Active zombie gore uses project-owned `LS_LocalizedWound.shader`, `LS_Ballistic.shader`, and `LS_BloodSurface.shader`. |

---

## 4. Characters, Zombies, Weapons & Vehicles Matrix (Out of Phase-1 Remodel Scope — Parity Audit Only)

Per Section 3.1, characters, zombies, and weapons are audited for visual consistency without modifying their rigs, animations, or gameplay authorities in this phase:

| Asset Family | Active Asset(s) | Visual Parity Assessment vs. Grounded Realism | Action in Current Phase |
|---|---|---|---|
| **Player Body & Arms** | `Survival Char` (Arberry), `Diesel`, `FirstPersonArms` | **ACCEPTABLE (78/100):** Arberry uses 70 PBR textures (599 MB) and fits grounded survival realism well; material smoothness/subsurface response under HDRP/wetness will need a lightweight shader/material tuning pass later. | **KEEP** (Do not touch rig/animations; adapt materials only if pipeline changes). |
| **Zombies** | `NewPunch`, `ZombieMale_AAB`, `Zombies/Zombie` | **MODERATE GAP (72/100):** `NewPunch` and `Zombie` have realistic proportions and PBR skin/clothing maps; `ArtStore3D` and `Zombie_Survival_AssetPack_LowPoly` are too stylized (`REPLACE/DO_NOT_USE`). Entitlement receipt for `NewPunch` remains tracked as pending (`S004`). | **KEEP ACTIVE / DEFER SKIN SHADER POLISH** (Preserve `ZombieLocalizedWounds` and dismemberment/ragdoll contracts). |
| **Weapons** | `wooden_axe_4k.blend` (CC0), `machete_4k.blend` (CC0), `Gece Studio` HK416, `PISTOLS` | **HIGH PARITY (86/100):** Poly Haven 4K CC0 Wooden Axe & Machete and Gece Studio HK416 are high-fidelity PBR models. `MRPoly` and `CrowbarLowPoly` are low-poly (`REPLACE` for close-up). | **KEEP ACTIVE** (Preserve `WoodenAxe.asset` S021/S004 grip and `ScopeOpticPresenter` contracts). |
| **Vehicles** | `Pack_Pickup` (0.85 MB), `Stylized Vehicles Pack Free` (1.92 MB) | **VISUAL GAP (52/100):** `Pack_Pickup` has low-poly geometry and basic textures (though `TownAtmosphere` dynamically applies metallic wetness to its URP Lit materials). `Stylized Vehicles Pack Free` is too stylized. | **ADAPT (Short-Term Material Polish) / REPLACE MESH LATER** (Preserve `VehicleWorld` and `VehicleActor` physics/seat/cargo authorities untouched). |
| **Loot Items** | `MedicinePack_JimmyPV`, `stylooFoodAndKitchen`, `Polygon-Lite Survival` | **MIXED PARITY (58/100):** `Polygon-Lite` is faceted low-poly; acceptable only as small distant icons/pickups until PBR survival props replace them. | **KEEP FOR GAMEPLAY STABILITY** (Never break `StableItemId` or `ItemCatalog` GUIDs). |
