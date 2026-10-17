# First Town — manual execution and acceptance

Native scene/prefab authoring is **IMPLEMENTED**. All verification, bake, test, build and acceptance items below are **NOT_RUN**. Execute them yourself. An authored scene is not a PASS result.

## Quick path in the open Unity Editor

1. Save or close modified scenes; allow your normal script compilation to finish.
2. Open the existing `FirstTownCore` scene. **Do not rerun Create**: it intentionally refuses existing outputs.
3. Inspect/save the generated `Assets/LastSignal/Scenes/Production/FirstTownCore.unity`.
4. `Last Signal > First Town > 2 Bake navigation (user only)`.
5. `Last Signal > First Town > 3 Validate authored town (no play)`.
6. Inspect the generated JSON evidence. Only then run the focused tests and enter PlayMode for traversal/visual acceptance.

Create, bake and validate are intentionally separate. Create never enters PlayMode, bakes navigation, runs tests or builds. No source/old scene is overwritten. If native authoring fails, send the JSON report plus the Console exception and relevant Editor.log segment; do not delete existing source content or vendor assets to retry.

## Equivalent terminal commands

The runner defaults to `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`. Pass `--unity '/absolute/path/to/Unity'` if needed. For all commands except `static`, close the project's Unity Editor first; the runner refuses `Temp/UnityLockfile`.

```sh
cd '/Users/burakcoskun/Last Signal'
python3 Tools/first-town.py static
python3 Tools/first-town.py compile
python3 Tools/first-town.py bake
python3 Tools/first-town.py validate
python3 Tools/first-town.py editmode
python3 Tools/first-town.py playmode
python3 Tools/first-town.py regression
```

Run one command, inspect its result, then choose the next. The `create` action is only for a fresh checkout without FirstTown outputs; it has already been completed here. Each command creates a timestamped evidence directory and never implicitly runs another action. `static` checks selected file/meta references and source wiring only; it cannot establish C# compilation, a generated scene or gameplay quality.

`editmode` filters FirstTownPersistenceTests, SaveValidationTests and SaveFoundationTests. It covers empty resident-cell serialization, invalid destinations, duplicate enemies and multi-enemy snapshots alongside existing save foundations.

`playmode` filters FirstTownPlayTests. It checks multiple encounter death states through save/load after owner reordering, and physical shelter crossing without transform relocation or repeated expedition increments.

`regression` filters existing PersistenceIntegrationTests, ShelterIntegrationTests and VehicleIntegrationTests. These retain their existing fixture scenes, including legacy shelter traversal. They help identify compatibility regressions in the shared session/save contracts. Their existing test prerequisites still apply.

Raw native authoring method for a fresh checkout only, if the runner is unavailable (do not rerun over this delivered town):

```sh
'/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' \
  -batchmode -projectPath '/Users/burakcoskun/Last Signal' \
  -executeMethod LastSignal.Editor.FirstTown.FirstTownAuthoring.Create \
  -logFile '/private/tmp/lastsignal-first-town-author.log' -quit
```

Use `BakeNavigation` or `Validate` in place of `Create` for those separate operations.

## Acceptance matrix

| Area | Required evidence | Initial status |
|---|---|---|
| Source references | static result, no missing approved assets/metas | NOT_RUN |
| Compilation | Editor Console/compile command has no C# errors | NOT_RUN |
| Native generation | Native authoring report; scene, wrappers, district prefabs and data assets exist | IMPLEMENTED |
| Navigation | Bake result and native paths from home to entry/interior/retreat/bridge markers | NOT_RUN |
| Persistence | Focused tests; save/load doors, consumed loot, dead encounters, cargo and player location | NOT_RUN |
| Physical main loop | Walk and drive from safehouse through districts and back; no teleport prompt or displacement | NOT_RUN |
| Legacy compatibility | Existing persistence/shelter/vehicle regression results | NOT_RUN |
| Visual quality | Overview plus player-height views for every district, daylight/night/rain | NOT_RUN |
| Performance | Target-machine development-player captures, hardware/settings and frame-time distribution | NOT_RUN |
| Production acceptance | All above reviewed, unresolved visual/content issues closed | PARTIAL |

## Visual QA

Use the disabled cameras under `99 Manual review cameras` to position the Scene view or temporarily preview individual shots. They are not active gameplay cameras. Capture overview, main street, safehouse approach and industrial approach, plus player-height views of clinic, school, motel, residential and farmland.

- Confirm every source wrapper is upright, metre-scaled and grounded; no stretched/white/magenta materials or black normal-map artifacts.
- Check front/back door skins, hinge alignment and collision match. Confirm the cabin's previous static door leaf is gone.
- Walk each S019 store and each modular interior. Check roof leaks, visible seams, false entrances, decorative house doors and rear escape routes.
- Read signs at walking speed. Confirm gas canopy/sign, civic tower, silo, school yard and motel have distinct silhouettes.
- Check road joins and bridge approaches for z-fighting, steps, floating supports, misplaced barriers and blocked sightlines.
- Confirm medical props have support; furniture/props do not overlap loot footprints or door sweeps. Review pantry/diner/house repetition and add only selected existing props where needed.
- Review dusk/night lighting and rain. Shared URP conversion is a baseline, not approved art. Water, school/motel architecture and outer boundaries remain polish tasks.

## Physical traversal and survival loop

1. New game at the safehouse. Verify current FPS/TPS character and weapon presentation remain intact. Open the cabin door physically; use storage, claim/install shelter modules and inspect crafting/bed controls.
2. Collect starting scrap/food/water/fuel. Leave through the compound north gate on foot. Expedition increments once; position remains continuous. Cross the south bridge and visit the checkpoint, clinic and town center.
3. Open both doors in representative interiors. Check obstruction recovery, narrow doorway collision, zombie navigation when doors open/close, and escape through the rear route.
4. Visit gas/service shop; collect fuel/wrench and the single existing relay fuse. Reach the industrial relay by road, repair it and return to the cabin radio. Confirm quest receipts and supply ownership.
5. Repeat with the existing pickup: safehouse parking → south bridge → main street → gas → east loop → industrial → residential → motel → safehouse. Check turns, bridge rails, clearance around parked wrecks and safe exits from both vehicle sides.
6. Explore school/gym, residential annexes, farmland and ranger trail. Check each loot type and encounter location against district purpose. Native paths do not prove an asset's decorative facade is enterable.
7. Return by either bridge. Enter the compound without an interaction prompt. Expedition returns to Shelter once; storage/crafting works after returning.
8. Consume loot, open doors and kill enemies in multiple districts. Save, return to menu, load. Check exact dead/alive identities, no regenerated loot, door state, inventory/storage/cargo, vehicle pose/fuel and relay receipts.
9. Save/load while occupying the vehicle and after exiting; repeat near a bridge and near the shelter boundary. Verify no ownership or overlap errors.
10. Repeat exit/return three times and test death/new game. Confirm one player, one vehicle, the authored encounter set and no orphaned Session Loot roots.

## Development build and performance capture

After acceptance of compilation/navigation and with macOS selected as the active build target:

```sh
cd '/Users/burakcoskun/Last Signal'
python3 Tools/first-town.py build
```

Or use `Last Signal > First Town > 4 Build development player (user only)`. Output is a fresh `Builds/FirstTown-<UTC timestamp>/LastSignal-FirstTown.app`. This builds only the town scene; it does not edit the source scene or EditorBuildSettings. No build was run at handoff.

Capture from the development player with Unity Profiler connected, not only the Editor Scene view. Disable Deep Profile for baseline captures. Record hardware, resolution, quality/URP settings, VSync/frame cap, weather/time, build revision and dirty-tree state.

- Warm up for 30 seconds. Capture 60 seconds each at safehouse, town-center street, industrial yard, north forest edge and while driving the complete loop.
- Record median/p95/p99 CPU/GPU frame time, main/render thread time, GC allocations/frame, memory, batches/SetPass calls, visible renderer/triangle counts and peak active enemies.
- Capture one combat interval and one save/load interval. Note any door-carving/nav spikes, synchronous loot initialization or asset upload stalls.
- Attach profiler data, Player.log and the native report counts. No FPS target or PASS threshold has been invented; select the project's target hardware/budget before judging acceptance.

Send the action's `result.json`, `unity.log`, any `tests.xml`, the native report and screenshots/profiler capture relevant to a failure. `PASS` from a single command applies only to that command, never automatically to art/gameplay/performance acceptance.
