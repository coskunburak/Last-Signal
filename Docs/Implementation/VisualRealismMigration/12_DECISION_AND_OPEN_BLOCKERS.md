# 12 — PRODUCTION DECISION & OPEN BLOCKERS (GATE 1 DELIVERABLE)

**Date:** 2026-10-20  
**Current Gate:** Gate 0 (`READ-ONLY DISCOVERY` — `CONFIRMED`) & Gate 1 (`ASSET & PIPELINE DECISION` — `CONFIRMED`)  
**Next Gate:** Gate 2 (`SAFE HDRP IMPLEMENTATION` / `PIPELINE ADAPTER & HERO SLICE PREP`) — **AWAITING OWNER APPROVAL**

---

## 1. Official Production Recommendation

### Recommendation: **`PROCEED_HDRP_WITH_BLOCKERS`** (Executed via Option C Pipeline Adapter -> Controlled Option B HDRP Migration)

#### Concrete Technical Justification:
1. **Why HDRP Is Technically Viable & Beneficial Here:**
   - The project targets **Windows PC / Steam** with a **Sons of the Forest** grounded survival-horror visual benchmark.
   - Achieving believable dense conifer atmosphere—specifically **volumetric light shafts through tree trunks, local height fog in drainage depressions, subsurface light transmission through backlit pine needles/ferns, 8-layer height-blended terrain, and DBuffer road/moss/bark decals**—is native to **HDRP 17.5.0** (`Unity 6000.5.0f1`), whereas reproducing all five in URP 17.5.0 requires maintaining complex custom render passes or paid third-party plugins.
   - Our codebase dependency audit (`04_URP_DEPENDENCY_MAP.md`) proves that URP is **tightly contained**: only **6 Runtime C# scripts**, **6 Editor C# scripts**, **3 `.asmdef` files**, and **11 custom shaders** reference URP. All core simulation, save/load, AI, vehicle, audio, and S003/S004 physical tree chopping systems have **zero URP dependencies**.
   - Furthermore, the newly imported 1.05 GB rural architecture pack (`Assets/ThirdParty/MarpaStudio`) already ships with native `HDRP/` materials and prefabs alongside `URP/`.

2. **Why `WITH_BLOCKERS` Is Mandatory (Do Not Blindly Flip Pipeline Without Resolving These 4 Blockers):**
   - **Blocker B1 — Dirty Working Tree & Unverified External Backup (`BACKUP_NOT_VERIFIED`):** The repo currently has 42 modified tracked files and 52 untracked paths containing the entire **BLD P01 S003/S004 Physical Tree Chopping** implementation and **VisualSlice Refinement 05b**. Before touching `Packages/manifest.json` or `GraphicsSettings.asset`, we must create a local file-level checkpoint archive (E1) so zero uncommitted work can be lost.
   - **Blocker B2 — Custom Shader & Material Preparation (`11 Shaders + 259 Materials`):** Switching `GraphicsSettings.asset` to HDRP before preparing the C# `RenderPipelineAdapter` and HDRP equivalents for `ForestLit.shader`, `ForestTerrain.shader`, `TownSky.shader`, `ScopeLens.shader`, and `LS_Blood*.shader` would immediately turn the scene pink and break `TownAtmosphere` / `ScopeOpticPresenter` compilation.
   - **Blocker B3 — Source Forest Asset Gap (`FantasyEnvironments` Limitations):** Switching to HDRP alone will **not** fix the repetitive painted pine silhouettes or flat green ground seen in `FirstTownCore`. We must pair the pipeline upgrade with the verified free realistic conifer/foliage/terrain candidates (`European Forests — Realistic Trees`, `Flora-Form`, and Poly Haven CC0 `fir_tree_01`, `fern_02`, `forrest_ground_01/03/04`, plus local `tree_stump_01_4k` and `asphalt_02_4k`) inside the 120×120 m Hero Slice.
   - **Blocker B4 — Commercial License Receipts (`LICENSE_PENDING`):** While Poly Haven assets (`CC0`) and `OpenFracture` (`MIT`) are cleared, Unity Asset Store / Fab account receipts must be logged for `FantasyEnvironments`, `VGtiXN Countryside Gas Station`, `MarpaStudio`, `Free Wood Door Pack`, and `ThirdParty/Audio/Tree` before commercial Steam release.

---

## 2. Master Status Table Across All Workstreams

| Workstream | Current Status | Verification / Next Gate |
|---|---|---|
| **Gate 0: Read-Only Repository & Pipeline Audit** | `CONFIRMED` | Documented in `00_ENTRY_AUDIT.md` |
| **Gate 1: Complete ThirdParty Inventory (53 Packs)** | `CONFIRMED` | Documented in `01_THIRDPARTY_INVENTORY.md` & `02_ASSET_QUALITY_MATRIX.md` |
| **Gate 1: Free Realistic Asset Gap Research** | `CONFIRMED` | Documented in `03_ASSET_GAP_RESEARCH.md` |
| **Gate 1: URP Dependency Map & HDRP Feasibility** | `CONFIRMED` | Documented in `04_URP_DEPENDENCY_MAP.md` & `05_HDRP_MIGRATION_FEASIBILITY.md` |
| **Gate 1: Tree Chopping (S004 v2) Compatibility Contract** | `CONFIRMED` | Documented in `09_TREE_CHOPPING_COMPATIBILITY.md` |
| **Local Backup & Dirty-Tree Snapshot (Phase E1)** | `PROPOSED` | First action upon owner approval |
| **C# RenderPipelineAdapter & HDRP 17.5.0 Integration (Phase E2–E5)** | `PROPOSED` (`BLOCKED` awaiting `APPROVE HDRP MIGRATION`) | Gate 2 implementation |
| **120×120 m Realistic Forest Hero Slice (Phase F)** | `PROPOSED` | Gate 4 implementation (after Gate 2/3 verification) |
| **Windows PC 1080p 60 FPS Performance & Visual Signoff** | `NOT_MEASURED` / `VISUAL_NOT_APPROVED` | Gate 3 & Gate 4 owner execution |

---

## 3. Single Recommended Next Production Action

**Next Action:** **Execute Phase E1 (Local Dirty-Tree Checkpoint Snapshot) + Phase E2a (Pipeline-Independent C# `RenderPipelineAdapter` Refactoring)**, followed immediately by **Phase E2b–E5 (Controlled HDRP 17.5.0 Installation, Shader Derivatives & `TownAtmosphere` HDRP Backend)**.

- **Expected File Changes in Gate 2 (Once Approved):**
  1. `Docs/Implementation/VisualRealismMigration/Checkpoints/PreGate2_DirtyTree_Snapshot.tar.gz` + `SHA256SUMS.txt` (Protects all 94 dirty/untracked paths).
  2. `Assets/LastSignal/Scripts/Runtime/Rendering/RenderPipelineAdapter.cs` (New adapter isolating camera/volume/light/pipeline calls).
  3. Minimal adapter wiring in the 6 Runtime (`TownAtmosphere.cs`, `ScopeOpticPresenter.cs`, `CharacterInspectionCamera.cs`, `S013EnvironmentPresentation.cs`, `S013Performance.cs`, `S014PopulationCapture.cs`) and 6 Editor URP-dependent C# scripts + 3 `.asmdef` files.
  4. `Packages/manifest.json` (Add `"com.unity.render-pipelines.high-definition": "17.5.0"` while retaining URP for safe transition).
  5. `Assets/LastSignal/Settings/Rendering/HDRP/` (New project-owned `PC_HDRPAsset.asset` and `HDRenderPipelineGlobalSettings.asset`).
  6. `Assets/LastSignal/Art/Realism/Shaders/` (Project-owned HDRP derivatives of `ForestLit`, `ForestTerrain`, `TownSky`, `ScopeLens`, and `LS_Blood*`, leaving existing URP shaders untouched).
- **Approval Gate Required:**
  - Reply with **`APPROVE HDRP MIGRATION`** to authorize creating the E1 checkpoint and beginning Gate 2 (Phase E1–E5).
  - Or, if you prefer to build the 120×120 m Realistic Forest Hero Slice on **URP 17.5.0 first** before touching the render pipeline, reply with **`RETAIN URP FOR HERO SLICE FIRST`**.
