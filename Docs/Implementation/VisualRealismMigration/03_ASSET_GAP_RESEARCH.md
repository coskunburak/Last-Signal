# 03 — FREE COMMERCIAL ASSET GAP ANALYSIS & SOURCING MATRIX (PHASE C)

**Date:** 2026-10-20  
**Policy:** Complete local `Assets/ThirdParty` audit first; never request redundant downloads for assets already present locally (`asphalt_02_4k`, `tree_stump_01_4k`, `factory_brick_4k`, `wooden_axe_4k`, `kloppenheim_01`, `qwantani_*`, `MarpaStudio` Farm, `VGtiXN` Gas Station). Only recommend **free, commercially verifiable** assets (CC0 or Unity Standard EULA Free) with real, verified URLs. No automatic downloading or bulk importing without owner approval.

---

## 1. Local Coverage vs. Critical Gaps across the 9 Priority Categories

| Priority | Category | Currently Available in Local `Assets/ThirdParty` | Gap Severity | Why Local Inventory Is Insufficient |
|---:|---|---|---|---|
| **1** | **Production-Quality Conifer Trees** | `FantasyEnvironments` (`Pine_tree1` 1,554 tris, `Birch_tree1` 1,192 tris, 1 LOD tier) | **CRITICAL (P0)** | Existing trees are mid-poly painted meshes with no geometric LOD decimation, no impostor/billboard tier, and stylized needle cards. |
| **2** | **Realistic Forest Floor (3D Debris/Litter)** | `tree_stump_01_4k.blend` (Poly Haven CC0), `MarpaStudio/Mesh/Log.fbx`, Pine bark log derivative | **HIGH (P0)** | `tree_stump_01_4k` is locally present (needs LOD export), but we lack 3D deadwood branches, root plates, pine cones, and leaf/needle litter clumps. |
| **3** | **High-Quality Grasses & Ferns** | `FantasyEnvironments/Prefabs/Grass1.prefab` (4 tris), `Fern2.prefab` (painted) | **CRITICAL (P0)** | 4-triangle painted grass cards cause flat green carpet appearance (`ADR LS-ART-2026-001`). Need PBR alpha-clipped fern clumps and natural woodland grass. |
| **4** | **Natural Shrubs & Saplings** | `FantasyEnvironments/Prefabs/Bush1.prefab` (54 tris, painted) | **HIGH (P0)** | 54-tri bush has painted textures and blocky silhouette at eye level (1.65 m). Need conifer saplings and multi-tiered woodland shrubs. |
| **5** | **PBR Terrain Materials** | `asphalt_02_4k.blend` (CC0), `FantasyEnvironments` diffuse-only terrain layers (`grass`, `soil`, `road`, `rock`) | **CRITICAL (P0)** | Current forest terrain layers have **no normal maps, roughness maps, or height/displacement maps** (`FirstTownCoreForest/DESIGN_AND_ASSETS.md`). Need CC0 PBR conifer duff, leaf litter, damp mud, and gravel/rock layers. |
| **6** | **Realistic Rocks & Debris** | `FantasyEnvironments/Prefabs/Rock1–3.prefab` (658 tris, diffuse-only), `HolotnaMountainURP` (blocked license) | **HIGH (P1)** | Need photogrammetry/PBR mossy forest boulders and small stone scatter with normal/roughness maps and 2–3 LOD tiers. |
| **7** | **Tree Bark / Stump / Cut Surface Materials** | `tree_stump_01_4k.blend` (CC0), `Art/S013/Production/Materials/Wood.mat` (`TimberGrain_v2.png`) | **MEDIUM-HIGH (P0)** | S004 16-stage notch slicing currently maps interior wood to generic `Wood.mat`. Need a dedicated high-detail PBR fresh-cut sapwood/heartwood cross-section/split grain material + high-relief conifer bark PBR maps. |
| **8** | **Forest Wind & Foliage Shaders** | `ForestLit.shader`, `ForestWind.hlsl`, `ForestTerrain.shader` (Project-owned URP 17.5 derivatives) | **MEDIUM (P0 if HDRP)** | Current shaders support global wind (`_LSForestWindPhase`), static override (`_LS_FOREST_STATIC`), and wetness (`_LSForestWetness`), but lack subsurface scattering/transmission for backlit needles/ferns and require HDRP equivalents if migrating. |
| **9** | **Atmosphere / Fog / Sky Requirements** | `kloppenheim_01_puresky_4k.exr`, `qwantani_sunset_puresky_4k.exr`, `qwantani_night_puresky_4k.exr` (CC0), `FS003_Day_Sunless.png` | **LOW-MEDIUM (P1)** | Overcast, sunset, and night skies are already 4K CC0 EXRs. Only the clear-day sky (`FS003`) is a painted PNG; local height fog / volumetric shafts are missing in URP (`TownAtmosphere` uses `ExponentialSquared` distance fog). |

---

## 2. Verified Free & Commercially Usable Candidate Matrix

All candidates below are **free** and permit **commercial game distribution** under either **CC0 1.0 Universal (Public Domain)** or the **Standard Unity Asset Store EULA** (embedded in a game product; source redistribution in public Git remains prohibited via `.gitignore`).

### 2.1. Priority 1 & 4 — Conifer Trees, Saplings & Understory Shrubs

| Candidate Name | Verified URL | License | Pipeline Support | Formats / LOD / Tech Specs | Advantages vs. Current (`FantasyEnvironments`) | Risks & Mandatory Pre-Import Gate |
|---|---|---|---|---|---|---|
| **European Forests — Realistic Trees** (Asset Store) | https://assetstore.unity.com/packages/3d/vegetation/trees/european-forests-realistic-trees-229716 | FREE · Standard Unity Asset Store EULA | Built-in / URP / HDRP listed by vendor (verify in Unity 6000.5) | Multi-species realistic European forest trees & saplings with LOD groups | True 3D branch/needle layering and realistic proportions compared to 1,554-tri painted `Pine_tree1` | **Check before full import:** (1) Verify separate trunk (`submesh 0`) vs. foliage (`submesh 1`) for S004 `OpenFracture` compatibility; (2) check alpha overdraw in Forward+/HDRP; (3) save account license receipt. |
| **Poly Haven — Fir Tree 01** | https://polyhaven.com/a/fir_tree_01 | **CC0 1.0** (Public Domain) | Pipeline-neutral source (`.blend` / `.fbx` + 4K PBR maps) | High-detail photogrammetry/modeled fir with separate bark and needle textures | Zero license restrictions; clean PBR albedo/normal/roughness maps; authentic conifer silhouette | Source mesh is high-poly; **must** be decimated in Blender/Unity into `LOD0` (~4,500 tris), `LOD1` (~1,800 tris), `LOD2` (~600 tris) + cross-billboard `LOD3`, with watertight lower trunk for S004 slicing. |
| **Flora-Form** (Asset Store) | https://assetstore.unity.com/packages/3d/vegetation/flora-form-266407 | FREE · Standard Unity Asset Store EULA | URP listed (Materials convert to HDRP via Shader Graph/Lit) | Understory bushes, shrubs, and ground plants | Provides immediate mid-height shrub layer missing between 4-tri grass and canopy trees | Verify texture resolution and alpha clip edge quality at 1.65 m eye level before staging. |

### 2.2. Priority 2, 3 & 6 — Forest Floor, Ferns, Stumps, Deadwood & Rocks

| Candidate Name | Verified URL | License | Local Status | Formats / Tech Specs | Role in Hero Slice (120×120 m) | Risks & Preparation Required |
|---|---|---|---|---|---|---|
| **Poly Haven — Tree Stump 01** | https://polyhaven.com/a/tree_stump_01 | **CC0 1.0** | **ALREADY IN `Assets/ThirdParty/tree_stump_01_4k.blend`** | `.blend` + 4K `diff`, `nor_gl`, `rough` | Hero mossy stump & root plate scatter | Export `.fbx` with 3 LODs and downscale 4K EXR maps to 2K BC7/ASTC in `Assets/LastSignal/Art/Realism/`. |
| **Poly Haven — Tree Stump 02** | https://polyhaven.com/a/tree_stump_02 | **CC0 1.0** | Download candidate | `.blend`/`.fbx` + PBR maps | Decayed mossy stump variant to prevent visual repetition | Generate 3 LODs; mark explicitly as decorative (non-harvestable) so players do not confuse with S004 cut stumps. |
| **Poly Haven — Dead Tree Trunk** | https://polyhaven.com/a/dead_tree_trunk | **CC0 1.0** | Download candidate | `.blend`/`.fbx` + PBR maps | Fallen forest-floor logs and nurse logs | Distinguish visually from S003/S004 interactive `HarvestLog.prefab` pickups. |
| **Poly Haven — Fern 02** | https://polyhaven.com/a/fern_02 | **CC0 1.0** | Download candidate | `.blend`/`.fbx` + alpha-clipped frond PBR maps (albedo, normal, roughness, opacity, translucency) | Primary understory fern clusters (Layer 4 forest floor) | Pack opacity into Albedo Alpha, author 2 LOD tiers (`LOD0` ~180 tris, `LOD1` ~45 tris, cull at 35 m), enable GPU instancing + subsurface transmission. |
| **Poly Haven — Mossy Rock 01 / Forest Boulders** | https://polyhaven.com/a/mossy_rock_01 | **CC0 1.0** | Download candidate | `.blend`/`.fbx` + PBR maps | Replace diffuse-only `FantasyEnvironments/Rock1` in hero slice | Author simple convex/box collider and 3 geometric LODs; bind to static wetness shader variant. |

### 2.3. Priority 5 & 7 — PBR Terrain Layers, Bark & Cut Wood Cross-Section Materials

| Candidate Name | Verified URL | License | Local Status | Target Surface | Technical Integration |
|---|---|---|---|---|---|
| **Poly Haven — Forest Ground 01** | https://polyhaven.com/a/forrest_ground_01 | **CC0 1.0** | Candidate | Terrain Layer 1: Damp deciduous/mixed leaf litter & soil | Download 2K JPG/PNG (`diff`, `nor_dx`/`nor_gl`, `arm`), pack URP/HDRP MaskMap (`R=Metallic, G=AO, B=Detail, A=Smoothness`). |
| **Poly Haven — Forest Ground 03** | https://polyhaven.com/a/forrest_ground_03 | **CC0 1.0** | Candidate | Terrain Layer 2: Conifer pine-needle duff | Primary under-canopy terrain splat layer (replaces painted `layer_ground_grass`). |
| **Poly Haven — Forest Ground 04** | https://polyhaven.com/a/forest_ground_04 | **CC0 1.0** | Candidate | Terrain Layer 3: Stony/gravelly woodland trail & drainage | Replaces diffuse-only `layer_ground_soil` / `layer_ground_rock` on slopes and trails. |
| **Poly Haven — Asphalt 02** | https://polyhaven.com/a/asphalt_02 | **CC0 1.0** | **ALREADY IN `Assets/ThirdParty/asphalt_02_4k.blend`** | Road surface & cracked verge | Already local; retain 2K derivative with wetness smoothness response (`TownAtmosphere`). |
| **Poly Haven — Asphalt 01** | https://polyhaven.com/a/asphalt_01 | **CC0 1.0** | Candidate | Worn road shoulder / patched asphalt transition | Optional secondary road splat/decal blend. |
| **Poly Haven — Pine Bark & Tree Bark 03** | https://polyhaven.com/a/pine_bark<br>https://polyhaven.com/a/tree_bark_03 | **CC0 1.0** | Candidate | High-relief conifer bark for cuttable hero trees | Ensures 1m–2m eye-level axe-aiming close-ups pass `V03` visual inspection without blurry texels. |
| **Poly Haven — Bark Brown 02 / Split Wood** | https://polyhaven.com/a/wooden_planks | **CC0 1.0** | Candidate | S004 Submesh 1 (Fresh-cut interior wood wedge & stump cap) | Combine with `Art/S013/Production/Textures/TimberGrain_v2.png` + normal/roughness map so the 16-level V-notch interior looks like raw splintered sapwood rather than flat planks. |

---

## 3. Acquisition & Quarantine Protocol

1. **Zero Unauthorized Downloads:** None of the external candidates above have been downloaded or imported during Gate 0/1.
2. **Quarantine Staging Folder:** When approved by the user, new downloads go into `Assets/ThirdParty/RealismCandidates/<PackName>/` with a mandatory `PROVENANCE.json` recording:
   - Source URL, Publisher, License (`CC0-1.0` or `Unity-AS-EULA`), Acquisition Date, Archive SHA-256, and Unity Version tested.
3. **Project-Owned Normalization:** All converted materials, packed mask textures, LOD-reduced meshes, and prefabs are authored exclusively under `Assets/LastSignal/Art/Realism/` so vendor sources remain untouched.
