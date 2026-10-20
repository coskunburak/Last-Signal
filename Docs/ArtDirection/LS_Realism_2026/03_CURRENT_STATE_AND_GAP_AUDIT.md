# Repository baseline and visual gap audit

## Evidence scopes and truth labels

| Source | Known observation | What it does NOT establish |
|---|---|---|
| GitHub `main` as of 2026-10-19 HEAD `0442ebb066ad71d55fa5a7ae49999185268fe4f6` | docs and `Packages/manifest.json` report Unity URP 17.5.0 | latest local uncommitted project state |
| GitHub branch `s012-integrated-graybox-slice` ref `84aa7358be7b3f47517b7e083ff0734e762e210f` | branch exists | branch identical to active local working tree |
| Owner Scene View screenshot 2026-10-19 | repeated green forest surfaces, pine shapes, road seam, visible perimeter fence | PlayMode FPS visuals, final lighting, performance |
| `Docs/Implementation/FirstTownCoreForest/HANDOFF.md` | production-forest authored and prior staged promotion, post-bake validation needed | accepted visual quality or current local PASS |
| `FirstTownCoreForest/DESIGN_AND_ASSETS.md` | legacy ~1,554-triangle pine and ~1,192-triangle birch with 1-tier geometry, URP shader derivatives | real geometric distance LOD, new art suitability |
| `FirstTownAtmosphere/HANDOFF.md` | four panorama-based sky states, rain/particles, wet material variants, fog/post, day-night authority | approved final realism, volumetric cloud/fog tech, measured GPU speed |
| `FirstTownVisualSlice/REFINEMENT_05.md` | gas-station canopy stage pipeline, pending status | safe stage-to-production promotion or final POI acceptance |
| `Docs/Last_Signal_Town_S023_Asset_Research_2026-10-15.md` | town asset research and seamless town design | current unpaid vendor asset cache or license evidence |

## Visual quality gap by impact/effort

| Gap | P | Approach | Dependency | Reality check |
|---|---|---|---|---|
| Uniform green ground and missing forest litter | P0 | PBR soil/needles/leaf/gravel/moss blend + debris | compatible terrain/LOD shaders | foot camera before/after |
| Repeating fantasy-style canopy | P0 | realistic sourced trees, 3+ variants + ecological scatter | legally acquired meshes + actual geometry LOD | 2/10/50/100m views |
| Road/forest sharp seam | P0 | graded shoulder, masks, scatter edge, cracks/ditches | no road or vehicle collider regression | run/drive route |
| Flat terrain | P0 | authored macro/micro landform non-destructively | preserve road/structure/river elevations | NavMesh/save spawn |
| Lighting and distance separation | P1 | neutral-calibrated URP benchmark, fog/cloud/shafts selection | single WorldClock/TownAtmosphere owner | day/dusk/rain/night |
| Boxy world perimeter | P1 | readable logical edge + distant natural composition | route and map design | final approach view |
| Characters/gore/vehicles stylistic mismatch | P1 | parity audit after environment target | rig/gameplay unaffected | FPS/TPS checklist |
| Cuttable tree mesh compatibility | P0 | geometry contract for bark cut/fracture and stand/fall | S003 gathering/harvest local status | impact/physics/save QA |
| Unrealistic objects around gas station | P1 | architectural wear, lighting, grounded props | stage source-hash gate | safehouse/forecourt |
| 60 FPS target unmeasured | P0 | Windows player p95 and GPU profiling | target HW to be named | quantified pass/fail |

## Do not assume

No vendor download is included; third-party folders can be ignored by Git, and old manifests may not list currently downloaded packs. No art acceptance or test is marked PASS here. Source documents include historical proposed decisions and actual partial implementations; opening a Markdown file does not resolve their status.

## Mandatory local discovery before implementation

`git status --short --branch`, commit and branch, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`; scene paths/scene backups, local `Assets/ThirdParty` inventory (including 3 recently added environment packs), dependency and license files, current S003 tree-harvest scripts/prefabs and latest generated outputs, FirstTownCore and separate visual-stage status. Detect actual Unity menu/test runner scripts rather than guessing. Log all protected user-authored edits before beginning. No checkout/reset/clean/reimport-unrelated allowed.
