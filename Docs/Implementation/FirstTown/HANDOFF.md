# First Town Core — implementation handoff

Date: 2026-10-15 (Europe/Istanbul). Overall: **PARTIAL**. Source implementation: **IMPLEMENTED**. Native scene/prefab generation: **IMPLEMENTED**. Compilation acceptance, navigation, Unity tests, PlayMode, builds, visual QA and performance: **NOT_RUN**.

Only native scene/prefab authoring was executed by the agent. Unity compiled/imported the authoring code as a prerequisite; no separate compilation acceptance, bake, test, PlayMode, benchmark, screenshot capture, visual QA or build was run. The native scene **exists and is open in Unity**, with navigation intentionally unbaked. This delivery must not be interpreted as an accepted production level. No new packages were searched for, downloaded or installed.

## Entry audit

- Branch: `s012-integrated-graybox-slice`; HEAD: `9191a5cd7bd245ee52e3bd9dab11466702ac2931`.
- Entry tree was dirty: player prefab, S013 scene, character/grip/combat presentation scripts and tests, package/settings changes; untracked Arberry and S023 work. Those pre-existing changes were not reverted or edited by this task.
- Unity version: 6000.5.0f1. Existing production source: `Assets/LastSignal/Scenes/Production/S013Cabin.unity`. Disk build settings still list S001Acceptance and CombatAcceptance; no change to those settings.
- Native authoring refused unsaved-scene loss, then opened the source and saved only a separate FirstTownCore scene. The Unity guidelines tool returned success without project-specific text.
- S023RegionPilot and its authoring exist but use CellPortal gates. They remain separate. FirstTown is authored from the current S013 source, retaining current player, inventory, survival, vehicle, time/weather, audio, shelter production and relay systems.
- Inspected only relevant source contracts, S013 kit, S019 rural store, selected prefab paths and the supplied third-party package inventory. Audit reference: `/Users/burakcoskun/Desktop/LastSignal_ThirdParty_Audit_m8ty795_/02_PACKAGES.csv`.
- Constraints found in code: one resident encounter in SaveSession; shelter anchor relocation; save schema requiring at least one streamed cell. These directly prevent the requested resident town loop and received scoped changes.

## 500 × 500 layout

Coordinates are metres; +Z is north, +X east; core X/Z span -250 to +250. District extents are planning envelopes, not runtime streaming cells.

| District | Center X,Z | Planning envelope X / Z | Implemented recipe |
|---|---:|---|---|
| Safehouse | -180,-170 | -216..-144 / -200..-140 | Current S013 cabin, functional door, fenced compound with north vehicle gate, storage/workbench/bed, parking and starting supplies |
| Main roads | 0,0 | Full core | 12 m main street; 8–10 m cross streets and east loop; separate service drives |
| Town center | 0,30 | -35..45 / 15..75 | Three S019 rural stores, diner, civic building/tower, sidewalks |
| Gas station | 65,-82 | 40..95 / -110..-45 | Countryside canopy/pumps/sign/office/cistern, playable service shop, fuel/scrap/wrench, existing unique quest fuse |
| Industrial yard | 168,-65 | 135..205 / -118..-20 | Warehouse, garage, container lanes, dispatch interior, tools/pallets/tank |
| Residential | 125,115 | 45..215 / 88..197 | Six European exterior silhouettes with six playable ground-floor annexes and garden cover |
| Clinic/pharmacy | -55,-42 | -72..-35 / -56..-22 | Medical interior, shelves/display, dressing and bandage loot |
| Police checkpoint | 0,-140 | -32..32 / -160..-120 | Staggered barriers, checkpoint office, ammo/rags; clear center passage |
| Motel/roadside | 80,-189 | 45..115 / -204..-158 | Four rooms, parking, sign, wreck and supplies |
| School | -45,139 | -70..-20 / 100..175 | Modular main building, classroom desks, separate gym and yard/play-frame silhouette |
| Farmland | 180,205 | 145..240 / 195..225 | Barn/silo/shed/hay and cultivated edge rows |
| Forest edge | -185,130 | Western/northern belt | Existing S013 pine/birch/bush/rock assets, ranger hut and trail |

A carved creek runs near X=-112, with physical 34 m bridges at Z=-140 and Z=80. The safehouse north gate joins the south cross street; the main axis links the checkpoint, center and northern ring. Gas, industrial, residential and motel connect through roads. The southern bridge is west of the checkpoint along the same cross-street axis.

The core remains entirely resident. Empty streamed-cell definitions and zero CellPortal components are deliberate. Population migration/materialization is disabled for this scene; authored encounters use existing ZombieEncounter with project-owned variants of `LS_Zombie_Runtime`, configured with the existing S022 Shambler/Runner/Lurker definitions. The named `LS_Zombie_Shambler/Runner/Lurker` prefabs are visual assets, not the gameplay actor. No new enemy, loot, road-runtime or building-runtime system was introduced.

## Source implementation

- `FirstTownAuthoring`: creates a separate scene without saving the source; retains selected session/shelter/UI roots; relocates current safehouse; wires current player prefab and all doors/encounters; isolates world/content/save slot as `first-town-v1`.
- `FirstTownGeometry`: terrain, creek mesh, shared-material road meshes with repeating UVs, bridges, connected districts, two-door interiors assembled from the existing S013 kit, existing S019 store instances, loot counters, signs, deterministic foliage, review cameras and route markers.
- `FirstTownAssets`: selected vendor art becomes project-owned wrappers; imported vendor scripts, rigidbodies, lights and audio are removed from the new art instance; source files are untouched. Built-in materials get shared URP derivatives keyed by source GUID/local ID. Existing URP materials are reused. Textures are reused without reimport or new downloads.
- `SessionFlow`: optional additional resident encounters share existing begin/pause/end lifecycle. Existing primary encounter field remains supported.
- `SaveSession`: captures and restores all resident encounters by persistent ID, validates exact ownership/topology, preserves death/anatomy state and uses the actor's navigation agent type when restoring. Existing serialized enemy array is reused; no save-file schema bump.
- `SaveValidation`: zero streamed cells are legal for a resident-only save. Null arrays, unknown destinations, duplicate identities and invalid population ownership still fail validation.
- `ShelterLoop`: opt-in physical bounds update expedition state with 1 m boundary hysteresis, on foot or while driving. No player transform changes in this mode. Existing scenes retain anchor traversal because the new flag defaults false.
- Static environment mesh renderers are flagged for batching/occlusion participation in the delivered scene; door hierarchies remain movable. No occlusion bake or performance measurement was run.
- Added focused persistence and PlayMode regressions, plus an explicit user-only command runner. None executed.

The save slot and world/content IDs intentionally separate FirstTown from existing S013/S023 saves. Source scene door/loot identities are not rewritten. Generated town identities are deterministic; once a town save exists, do not casually rename IDs or duplicate district prefabs into the same scene. New instances need fresh persistent IDs and loot IDs, using the existing POI authoring approach.

## Native outputs delivered

| Output | State now |
|---|---|
| `Assets/LastSignal/Scenes/Production/FirstTownCore.unity` | IMPLEMENTED — native Create completed |
| `Assets/LastSignal/Art/FirstTown/{Prefabs,Materials}/` | IMPLEMENTED — native Create completed |
| `Assets/LastSignal/Prefabs/FirstTown/` district prefabs | IMPLEMENTED — native Create completed |
| `Assets/LastSignal/Data/FirstTown/` terrain/layer/creek/road meshes/loot profiles | IMPLEMENTED — native Create completed |
| `Assets/LastSignal/Data/FirstTown/Navigation.asset` | NOT_RUN — emitted only by your separate Bake |
| `Docs/Implementation/FirstTown/Evidence/` | IMPLEMENTED — native authoring reports present; QA evidence pending |

Creation refuses an existing town scene or output asset. It does not regenerate over manual edits. Failure cleanup removes only newly created output assets. After successful creation, edit the scene normally and save it before invoking Bake/Validate. Re-bake preserves the generated navigation asset GUID. The build method uses only FirstTownCore and a new timestamped macOS development output, without modifying EditorBuildSettings.

## Open issues / acceptance boundary

- **IMPLEMENTED — native delivery:** `FirstTownCore.unity`, project-owned wrappers/materials, district prefabs, terrain/road/loot data and runtime zombie variants exist. Authoring report: `Evidence/20261015-172216-241-authoring.json` (20:22 Europe/Istanbul). It reports 39 doors, 13 resident encounters, 39 loot points, 5,572 renderers, 91 shared materials and 182 shared meshes. These are authoring inventory counts, not performance or gameplay acceptance. The earlier failed authoring report is retained; its visual-versus-runtime prefab issue was corrected and temporary outputs were cleaned before the successful run.
- **PARTIAL — architecture:** European houses serve as exterior silhouettes; gameplay is in the attached authored annexes. Warehouse/garage/vendor-office doorways and vendor collision must be reviewed; dispatch/service interiors provide guaranteed authored loot destinations. School and motel use existing modules and limited interiors, not bespoke production architecture.
- **PARTIAL — environment polish:** water is a static opaque creek ribbon; no swimming/flow/shoreline VFX. Core perimeter fencing is a temporary visible boundary. The 1 km surrounding landscape, distant mountain ring, large-scale streaming and distant LOD treatment remain PLANNED.
- **NOT_RUN — native fit:** material conversion, wrapper scale/orientation, vendor mesh collision, porch/door gaps, foliage ground contact and vehicle turning clearances need scene review. Normal-map/specular/transparency interpretation may need selected material tuning.
- **NOT_RUN — gameplay:** save/load, loot support, door obstruction/carving, zombie traversal, vehicle occupancy and shelter production regressions must be exercised. Native open-door route validation is not proof of runtime door behavior.
- **NOT_RUN — performance:** no frame-time result is claimed. The 5,572-renderer authoring inventory is a material optimization concern; inspect whole-building/static batching, vendor facade granularity, occlusion and foliage LODs before production acceptance. Shared materials/meshes and bounded foliage alone do not establish a frame budget.
- **BLOCKED_EXTERNAL_ASSET:** none established. The remaining limits are authoring/QA/polish work; no new asset purchase is justified by the current evidence.

See `asset-usage.csv` for exact selected source paths, `changed-files.txt` for this task's changes and `MANUAL_QA.md` for commands and acceptance checks.
