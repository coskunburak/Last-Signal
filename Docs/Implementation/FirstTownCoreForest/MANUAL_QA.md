# Manual execution and acceptance

All operations below are owner-run. The agent prepared them but did not execute them.

## Required order

1. **Native authoring:** open FirstTownCore, save changes, run `Last Signal > First Town > Forest > 1 Create or regenerate staging scene`. The source scene stays untouched. Review stage and generated report under `Docs/Implementation/FirstTownCoreForest/Evidence/`.
2. **Static validation:** save stage and run `2 Validate saved stage references and clearance`. If it reports a route/body conflict, adjust Layout trail points/exclusion bounds, save, and run Stage again from the existing stage scene. It never silently deletes HeroTrees or source objects.
3. **Determinism/re-generation review:** with unchanged source/layout/seed, run Stage twice from the staging scene. Compare tree/bush/rock/grass counts and `placementDigest` in the two stage reports. Check no duplicated Generated root and unchanged HeroTrees, gameplay IDs, source scene and source TerrainData. Move a deliberate manual tree into HeroTrees; regeneration must retain it and exclude its bounds.
4. **Compilation:** wait for editor import and inspect Console. For a dedicated batch compilation close the project and run the command below. Do not promote a stage with compiler/shader errors.
5. **Focused EditMode:** run the existing time/persistence suites via the runner below.
6. **Promotion / focused PlayMode:** return to the saved stage and use `3 Promote saved stage to FirstTownCore`. This is an explicit write to production with source-hash guard and backup. Run the focused PlayMode command below. Do not start a stage session against a valued FirstTown save without backing up that save using your normal save workflow.
7. **Navigation:** after promotion, use the existing menu `Last Signal > First Town > 2 Bake navigation (user only)`; the exact native method is `LastSignal.Editor.FirstTown.FirstTownAuthoring.BakeNavigation`. Alternatively, with Unity closed, run `python3 Tools/first-town.py bake`. Then, with production open, run `Last Signal > First Town > Forest > 4 Validate baked forest navigation`. This checks route/clearing connectivity using the actual zombie agent type. It is not live AI acceptance.
8. **Vehicle/player/live AI:** manually traverse the routes below and test doorways, exits, retreat and an existing zombie pursuit. Keep all existing encounter/save IDs; no additional enemies are required.
9. **Full EditMode / Full PlayMode:** run the full-suite commands after focused failures are resolved.
10. **Build:** use existing owner-run `python3 Tools/first-town.py build` after successful regressions; review its output/log. No build performed by the agent.
11. **Visual acceptance:** owner screenshots from fixed cameras and actual player/vehicle views across weather/time.
12. **Benchmark:** capture the metrics and conditions in PERFORMANCE_PLAN.md. No PASS until compared with baseline.

Terminal commands (run individually; save and close this Unity project first):

```sh
cd '/Users/burakcoskun/Last Signal'
python3 Tools/first-town-forest.py compile
python3 Tools/first-town-forest.py editmode
python3 Tools/first-town-forest.py playmode
python3 Tools/first-town.py bake
python3 Tools/first-town-forest.py full-editmode
python3 Tools/first-town-forest.py full-playmode
python3 Tools/first-town.py build
```

The new runner never calls Stage, Promote or bake automatically. Each invocation has a timestamped log/command/process report and tests.xml where relevant. Inspect XML counts/results; exit code alone is not PASS. Focused tests are existing WorldTimeTests, FirstTownPersistenceTests, FirstTownPlayTests, WorldTimeIntegrationTests and VehicleIntegrationTests; they are regression coverage, not a new forest visual test suite. If the active editor is open, use Test Runner with those filters.

## Route and gameplay checklist

- Safehouse spawn and pickup: 18m spawn pad, both seat exits, vehicle rotation/reverse and safehouse gate. No trunk/collider within exits; no grass/rock hiding required interactions.
- Western vehicle road: (-180,-140) → (-204,-116) → (-212,-68) → (-194,-18) → (-211,31) → (-197,80). Drive both directions, reverse on bends, stop/exit/re-enter. Width7m is authored, drivability is NOT_RUN.
- Ranger foot loop: hut retreat (-197,136) through (-211,153), (-213,180), (-187,211), (-158,216), (-151,173), (-160,117), returning to (-190,80). Walk/run/crouch; no ankle snagging, floating trees, terrain holes or walls of foliage.
- North woodland/farm: school avenue (-75,170) → (-72,191) → (-47,214) → (0,224) → (65,219) → (120,210) → (120,200). Maintain residential/farm access and road sightlines. Cross the river via the existing north bridge, never via an implicit new ford.
- Clearings at (-222,-80), (-213,180), (-40,223): verify retreat to town and agent path completion. Existing zombies may pursue into these spaces; no new spawners or weather authority were added.
- Check bridges at z=-140/80, gas/industrial/residential/clinic/motel access, and all earlier loot/doors. Terrain modifications must not change the authored safehouse floor or bury road mesh edges.
- Load an old save inside the forest, a vehicle save, and a rain/sleep save. Check spawn obstruction, terrain height, body wetness, encounter identity, time continuity and existing restore safety. No silent schema migration is included.

## Fixed visual review

Stage creates six **disabled** cameras under Generated. Owner enables only the intended review camera or positions Scene view to it; do not leave an extra gameplay camera/listener active. No screenshots were taken by the agent.

| Camera | X/Z | Look toward X/Z | Main comparison |
|---|---|---|---|
| Town to Forest | -180,80 | -219,155 | Edge ramp, town silhouettes, artificial repetition |
| Forest Interior | -225,174 | -211,196 | Canopy, understory, depth and scale |
| Trail | -200,-105 | -212,-68 | Path transition and clearance |
| Forest Road | -212,-68 | -194,-18 | Vehicle road width, bend and slope |
| Rain Forest | -158,173 | -209,180 | Foliage/soil wetness, rain, haze, threat readability |
| Night Forest | -40,223 | -65,220 | Flashlight, silhouettes, darkness and clutter |

For each view record day, dawn, golden hour, rainy day and rainy night. Use the existing atmosphere QA time/weather sampling instructions in `Docs/Implementation/FirstTownAtmosphere/MANUAL_QA.md`; preserve normal production settings after review.

Inspect trunks seated into slopes, leaf-card quality close up, clipping edges, dither fade, shadow/wind alignment, dry/wet roughness, seams at terrain basemap distance, terrain-detail instancing and count, grass saturation, horizon silhouette, road/ground blend and missing shaders. Reject any implausible canopy intersections or obstructed threat corridor. Geometry/seed determinism alone does not establish art quality.

## Acceptance matrix

| Area | Implementation | Acceptance |
|---|---|---|
| Source-preserving staging and guarded promotion | IMPLEMENTED in source | NOT_RUN |
| Terrain relief / splat / preserved physical connections | IMPLEMENTED in authoring | NOT_RUN |
| Cluster/species/spacing/exclusion distribution | IMPLEMENTED in authoring | NOT_RUN |
| Prefab/material/collider normalization | IMPLEMENTED in authoring | NOT_RUN |
| Understory, grass and bark debris | IMPLEMENTED in authoring | NOT_RUN |
| Wind and wetness integration | IMPLEMENTED in runtime/shaders | NOT_RUN |
| Navigation and collision | Setup/checks prepared | NOT_RUN |
| Existing save/player/vehicle/zombie behavior | Authorities preserved in source | NOT_RUN |
| Reduced geometry LODs / impostors / bespoke litter | PARTIAL | NOT_RUN |
| Visual target | Owner screenshots required | NOT_RUN |
| Target-device performance | Owner comparison required | NOT_MEASURED |

On failure return the exact action, first relevant error/stack, its evidence folder, affected coordinates and screenshot/time/weather where applicable. Fix narrowly; rerun the focused check before broad regressions.
