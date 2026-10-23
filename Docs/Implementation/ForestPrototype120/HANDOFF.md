# ForestPrototype120 — implementation and manual handoff

## Existing architecture: verified 2026-10-21

- Unity **6000.5.0f1**, HDRP **17.5.0**; the live Editor reports `LastSignal_HDRP_High`. URP remains installed for existing compatibility; this implementation changes no packages, pipeline assets, quality levels, build scenes or global rendering settings.
- The initially open scene was `Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019103726-320e36cd.unity`. The user saved the prefab and scene work and authorized generation. The isolated forest scene has now been generated and saved; the original saved scene remains unchanged.
- `FirstTownCore` is the production settlement. Existing `ForestAuthoring`, `ForestSources`, `ForestLayout`, `ForestAuthoringState`, and `ForestPresentation` manage its 500 m forest stage, retained baseline terrain, protected hero subtree and production promotion. The new tool follows their ownership/checkpoint conventions and uses their folder helper, terrain path distance utility and imported S013 sources. It does not promote or modify the town.
- `FirstPersonMotor`, `PlayerStance`, `FirstPersonLook`, `PlayerInputReader` and the existing input actions supply traversal. The only existing source changed is an optional `TryJump(heightMeters)` entry point in `FirstPersonMotor`. No existing input binding invokes it; the prototype harness binds Space.
- Existing gathering uses `ResourceNodeView` and the S004/P01 tree physics/persistence authority. Prototype trees are decorative. No resource IDs, save references, reward logic or tree physics are attached or replaced.
- Navigation uses `Unity.AI.Navigation.NavMeshSurface`. The isolated prototype provides its own surface, marks it pending bake, and never alters an existing surface or bake.

## Imported asset mapping

| Package / real location | Selected content and native inspection | Preparation / limits |
|---|---|---|
| European Forests / `Assets/Realistic Tree` | HDRP Spruce 1/4/6/8/9, Birch 2. Spruce 1 has 15,096 / 7,548 / 3,773 / 1,886 triangles across four levels. Spruce 4 LOD0: 16,085; Spruce 8 LOD0: 17,138. Three mesh renderers per level; no trunk colliders. Source spruce heights approximately 4–6 m. | Cloned vendor HDRP Bark/Leaves graphs and textures, retained all four LODs, source-specific texture filenames, modest shader wind, capsule trunk collision. Fixed archetype height calibration to 11.5/15/18 m, then .87–1.1 scale variation. Birch 12 m. Visual anatomy and bark texel density need owner review. Offset/group variants are excluded. |
| Fir / `Assets/fir_tree_01_4k.blend (1)` | `fir_tree_01_a_LOD0`: **4,176,819** triangles, LOD1 **177,444**, LOD2 **70,661**. Blender grid contains additional b/c variants and geometry-node mesh. | Exactly one hero, composed only from a_LOD1/a_LOD2; LOD0 and grid objects excluded. Corrected individual object transform, project-owned HDRP/Lit bark/trunk/twig materials. Dead branches reuse the package bark texture. No claim of a newly decimated mesh. |
| Fern / `Assets/fern_02_4k.blend` | Four frond variants: 784 / 2,384 / 2,248 / 816 triangles. Imported material had no textures assigned. | Four separate prefabs, Blender orientation preserved, grid offsets removed. RGB + separate alpha packed; OpenGL normal and inverted roughness mask. No colliders/shadows. Crossfade to cull, not a geometry-reducing fern LOD. |
| Forest Ground / `Assets/forrest_ground_03_4k.blend` | Actual package spells **forrest**. Diffuse JPG, roughness JPG, OpenGL normal EXR, displacement PNG. | Owned diffuse/normal; linear mask R=0, G=1 (no fabricated AO), B=height, A=1−roughness. HDRP TerrainLit with litter, soil, grass/organic and rock layers. Height helps blend; it is not tessellated terrain displacement. |
| Existing S013 / `Assets/LastSignal/Art/S013/Production/Prefabs` | Bush (54 triangles), Rock (658), Grass (4), Pine bark submesh. | Shared atlas, flattened instanced terrain grass, small shrubs/rocks/branches. Existing Fantasy texture family is a realism limitation, not a new download. |

Third-party originals and their import settings remain unchanged. All texture-space/readability changes target owned copies. Scalar data maps are linear, color maps sRGB, normal copies use NormalMap import. GPU instance eligibility does not guarantee every HDRP draw batches; measure it.

## Implemented environment authoring

- **120 × 120 m playable Terrain**, 513 height samples, 512 splat resolution, 18 m data range. Gentle height trend, western ridge, eastern drainage and multi-scale microvariation; trail microrelief softened. Four normalized spatially blended layers.
- Curved 2.5 m trail, irregular approximately 21 m clearing, denser western woodland. Fixed seed **261021**, bounded rejection sampler with spatial hash minimum spacing **4.6 m**, correlated density, moisture, slope, clearing/trail buffers, hero/spawn reservations and artist exclusions.
- Default caps: **290 canopy trees**, **1 fir hero**, **1,500 ferns**, **220 shrubs**, **85 rocks**, **9,000 terrain details**. The successful generation reached these default caps; actual counts are recorded in Evidence/authoring.json.
- Non-playable visual terrain apron extends 24 m outside the tile with up to 150 far-LOD spruces. It has no collision. Outer-edge blockers retain the requested playable dimensions. Close boundary inspection may still expose the prototype limit.
- Leaf/needle wind reuses the imported vendor graph with reduced bend/speed; no new shader or per-tree Update loop. Fir and fern are static. Transmission is not claimed where no valid diffusion profile is assigned.
- **Morning:** 27° sun elevation, 65,000 lux, warm-neutral sunlight, physical sky, fixed EV12.8, ACES, 80 m shadows, 240 m fog mean free path, 96 m volumetric depth, restrained SSAO/contact shadows.
- **Overcast:** gradient ambient sky, 6,500 lux diffuse directional contribution, softer angular size, fixed EV11.5, 130 m fog mean free path. This is a controlled lighting preset, not simulated cloud/weather coverage.
- Camera: 75° vertical FOV, .06–280 m clips, TAA, dynamic resolution disabled. Scene-local profiles and light. Neither setup has been visually accepted.
- Explicit F9 recorder is inactive by default. After 180 warmup frames it records 30 seconds of frame times and available profiler counters; no performance task runs on load.

## Generated ownership and repeatability

Generated scene: `Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity`. Current generation: `Assets/LastSignal/Art/ForestPrototype120/Generations/G20261021101239485`. It contains 84 generated assets plus metadata, including 15 prefabs. See GENERATED_FILES.txt for exact paths.

Authoring inputs: `Assets/LastSignal/Art/ForestPrototype120/Settings.asset`. Edit this asset before regenerating. The scene uses the immutable settings snapshot stored in its generation folder, not live edits to the input asset.

Each successful authoring creates `Assets/LastSignal/Art/ForestPrototype120/Generations/G<UTC timestamp>/` containing material/texture/prefab derivatives, TerrainData, two VolumeProfiles, a settings snapshot, and meshes. The scene GUID and settings GUID stay stable. A rebuild replaces only the owned `Generated` subtree; roots outside it and the owner component remain. Their physical collider footprints are added as exclusions. Never hand-edit content inside Generated if it must survive regeneration.

Fresh immutable asset generations intentionally remain for rollback. They are not all live in the scene. Failed generations can remain for diagnosis; remove an unused generation in the Editor only after checking scene/prefab references. There is no production promotion or new scene loader.

## Exact manual execution order

**No automated test, visual acceptance run, NavMesh bake or benchmark has been executed by the agent.**

The scene is already generated. Save any new editor work and close Unity before shell commands to avoid the project lock. The `generate` command below is optional reproducibility testing; skip it to inspect the delivered generation first. Do not close unsaved work through a batch command.

```bash
cd '/Users/burakcoskun/Last Signal'
bash Tools/ForestPrototype120/manual-checks.sh sources
# Optional rebuild; the scene is already generated:
# bash Tools/ForestPrototype120/manual-checks.sh generate
bash Tools/ForestPrototype120/manual-checks.sh references
bash Tools/ForestPrototype120/manual-checks.sh edit
bash Tools/ForestPrototype120/manual-checks.sh play
bash Tools/ForestPrototype120/manual-checks.sh regression-edit
bash Tools/ForestPrototype120/manual-checks.sh regression-play
```

The script uses `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`, this project's absolute path, and writes logs/XML to `Docs/Implementation/ForestPrototype120/Evidence/`. It deliberately omits `-quit` for `-runTests`, allowing the Unity test runner to exit after writing results. No `-nographics` option is used.

Actual test filters:

- EditMode: `LastSignal.Tests.ForestPrototypeTests` — spacing, seed repeatability, exclusions, trail/clearing clearance, variation/weights, authored asset/LOD/collider contracts, data-map import settings.
- PlayMode: `LastSignal.Tests.ForestPrototypePlayTests` — spawn/traversal/crouch, jump/air-jump rejection, physical trunk blocking, six viewpoints/preset switching.
- Regression EditMode: `LastSignal.Tests.P01TreeStructureTests;LastSignal.Tests.P01TreePersistenceTests;LastSignal.Tests.FirstTownPersistenceTests`.
- Regression PlayMode: `LastSignal.Tests.MovementAcceptanceTests;LastSignal.Tests.P01TreePhysicsTests`.

Editor alternative: **Last Signal → Forest 120 → 1 Validate source assets**, then **2 Create or regenerate isolated forest**, then **3 Validate authored references**. Open **Window → General → Test Runner** and run the filters above in their respective tabs.

After focused tests, test idempotence manually: record the canopy count and placementDigest on the authoring owner; create an unrelated empty scene root named `ArtistPreservationProbe`; save; regenerate twice. Confirm one ForestPrototypeState and one Generated subtree, unchanged counts/digest, and the probe still present. Add a physical cube outside Generated, save/regenerate, and confirm a local vegetation exclusion instead of an overlapping trunk. Settings/assets are deliberately retained between rebuilds.

For navigation, open only the forest scene and invoke **Last Signal → Forest 120 → 4 Bake prototype navigation (explicit)**. Confirm a non-null NavMeshData and visually connected trail/clearing in the AI Navigation overlay. No zombie spawner or AI actor is added; zombie reachability with production agent settings is a later integration gate.

## Visual acceptance — Game view only

Open the generated scene, set Game view to **1920 × 1080**, and enter Play Mode. WASD/mouse, Shift sprint, existing Crouch action (inspect bindings if customized), Space jump, Escape cursor/gameplay toggle. On a Mac, use Fn+F1…F9 if the keyboard reserves function keys.

F1–F6 move the real gameplay camera to authored player-height markers. Marker positions may shift up to 3.4 m to avoid colliders; select `SpawnAndValidation/Six player-height viewpoints` to inspect final coordinates. All initial targets use terrain-relative eye height 1.74 m before the player settles to 1.62 m.

| Key | Initial X,Z / pitch,yaw | Inspect |
|---|---|---|
| F1 | 48.64,13 / 0°,28° | Curved trail, readable route, overlapping canopy; no straight prop rows. |
| F2 | 57,51 / 0°,36° | Shaded foreground toward sunlit clearing; visible shadow detail, restrained beams, stable exposure. |
| F3 | 24,82 / 0°,65° | Dense western woodland; distinct trunk layers and navigable gaps. |
| F4 | 53,60 / 55°,42° | Downward floor inspection; normals, leaf/needle scale, alpha edges, planted fern bases. |
| F5 | 57,59 / −12°,31° | Fir at 60,64; bark roughness, correct twig alpha, roots/contact; approach and retreat to inspect LOD transition. |
| F6 | same as F2 | Overcast material consistency, diffuse light, no flat white haze. |

F7 restores Morning; F8 applies Overcast. Editor equivalents are **Last Signal → Forest 120 → Lighting → Morning / Overcast**. F1–F5 select Morning; F6 selects Overcast. Inspect each view stationary and while moving; look upward to catch missing/two-sided leaf faces and TAA shimmer. Inspect trunk/collider correspondence, route traversal in both directions and outer-edge dressing. Regeneration resets Morning.

## Optional performance procedure — only after visual review

Use 1920×1080, record the selected existing HDRP quality tier, laptop power mode, and whether profiling Editor or a standalone development build. The script does not change VSync, target frame rate or quality. Allow imports/shader compilation to finish. Keep only the forest scene loaded and close Scene view rendering if comparing steady-state Game view costs.

At F1, F2, F3, F5 and F6, press **F9** once and hold the camera still for warmup + 30 seconds. The recorder writes `frames.csv` and `summary.txt` under `Application.persistentDataPath/ForestPrototype120/<UTC timestamp>` and prints the exact directory. With the current company/product settings this is normally `~/Library/Application Support/DefaultCompany/Last Signal/ForestPrototype120/`; use the Console's reported path as authoritative. Save a separate repeated walk from F1 along the trail to the clearing.

Reports include average FPS, P50/P95/P99 frame time, main/render-thread markers, GPU timing if supported, draw calls, triangles, memory, hardware, resolution, quality, camera pose, generation and preset. Blank counters mean unavailable, not zero. FrameTimingManager GPU timing may be unsupported/disabled on Metal. Thread marker durations include waiting and should not be interpreted as active CPU work. The opt-in recorder itself adds small measurement overhead.

Use **Window → Analysis → Profiler** for CPU Timeline / Rendering / Memory and GPU Usage if supported. Inspect shadow, depth, GBuffer/forward, volumetric and post-processing passes using **Window → Analysis → Frame Debugger**. Save the profiler capture. At 60 FPS the working frame budget is 16.67 ms; report both averages and tail latency. Diagnose draw/overdraw/shadow/fog costs before changing density. Do not report the target achieved until measured on the actual M3 Pro setup.

## Known limitations and status

- Scene generation is complete. See STATUS.md and Evidence/authoring.json. Native authoring success is not a test pass or visual acceptance.
- Appearance is unverified: normalized spruce proportions, source graph wind, mixed ground texture families, fern distance culling, bark texel density, fog, sky brightness and alpha-edge stability all need the six-view review.
- One fir uses 177k/71k triangles. Ferns have a cull LOD rather than reduced mesh tiers. Canopy has three renderers per LOD. All are measurable performance concerns; 60 FPS is not claimed.
- A Blender background inspection attempt crashed before opening the source; no Blender modifications or decimation output were made. Existing imported LOD1/2 are used for the single hero instead.
- Navigation must be baked manually after each regeneration. No zombies, resource harvesting, inventory or save/load systems are introduced into this isolated exploration scene. Preservation in FirstTownCore is verified by file hashes, not by a gameplay regression run.
- Future harvesting should use the existing ResourceNodeView/tree physics authority. European trunks are separated from leaf meshes but include branches; neither they nor the fir have prepared cut rings, sealed cross-sections, stump/log pieces or persistent resource identities. These require an explicit later authoring pass. No physical felling feature is claimed.
- The live Console already contains an unrelated unsupported `.wmv` import error from `Unity-Tree-Chopping-Simulation-main/CombinedResultVid1.wmv`. It was not modified.

Return the focused XML/logs, the six Game-view screenshots, and any F9/Profiler reports. Tests and visual/performance acceptance remain NOT_RUN until owner results are supplied.

## Authoring corrections and preservation evidence

Birch falling-leaf systems were attached to their prefab root, and some LOD renderers were nested. Preparation now removes only the unwanted components, preserving parent hierarchies. Asset saving targets only owned generation assets. Failed generation folders were archived outside Assets. The protected-file before/after comparison covered 1,401 files with zero changes. No additional test or performance execution accompanied these corrections.
