# Owner execution and visual acceptance

Status: **owner generation recorded on 2026-10-16 at approximately 14:29 Istanbul; saved stage exists; visual review pending**. The subsequent reopen-flow source fix has not been executed in Unity. No Unity tests, PlayMode, builds, compilation gates, navigation bakes or benchmarks were invoked by the assistant. The menu operations below are implemented C# entry points, not invented commands.

## Current review: Refinement 04

Corrected owner captures reveal an incorrectly tiled road atlas, oversized compound fences on the road, and weak practical lighting. Existing road/lighting assets are now revised. Follow `REFINEMENT_04.md`: import, save current stage edits, run **2b Apply road and fence refinement (existing stage)** once, then capture **04 Day, 04 Dusk, 03 Sunset** using **2 Visual comparison studio**, archived lighting off. This is a bounded edit to the existing stage, not regeneration. Native scene execution and after-image acceptance remain pending.

## Resume the existing result (only if closed)

Exit PlayMode/Prefab Mode and choose **Last Signal → First Town → Visual Slice → 0 Open saved stage (preserve unsaved scenes)**.

Callable method: `LastSignal.Editor.FirstTownVisualSlice.SliceAuthoring.OpenSavedStage()`.

This opens the saved `Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity` without regeneration or promotion. Any dirty loaded scenes are first saved as separate copies under `Assets/LastSignal/Art/FirstTownVisualSlice/Recovery/R<timestamp>-<unique-id>/`; their original files are not overwritten. Console and `Evidence/recovery-<timestamp>.txt` list the copies. A failed copy cancels the switch. These are scene snapshots, not backups of all referenced assets. If the stage is already loaded, it is selected without reloading or replacing its in-memory edits.

Menu 1 now also resumes an existing stage when invoked from production. Regeneration requires opening the stage and saving it first. **Do not regenerate for this recovery:** the production scene and current Terrain.asset bytes differ from their recorded generation hashes; the cause is unverified and the protection remains enabled. See `RECOVERY_2026-10-16.md`.

The original production scene deliberately remains unchanged until explicit acceptance and promotion. Opening it therefore shows the original environment; that alone does not mean the stage was lost.

## Initial creation (only when no saved stage exists)

Save existing Unity scene edits, exit PlayMode/Prefab Mode, open only `Assets/LastSignal/Scenes/Production/FirstTownCore.unity`, then choose:

**Last Signal → First Town → Visual Slice → 1 Create or regenerate staged slice**

Callable method: `LastSignal.Editor.FirstTownVisualSlice.SliceAuthoring.StageSlice()`.

This copies the current scene to `Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity`, creates the local environment and saves the stage. It does not bake navigation, run tests, enter PlayMode or replace production. Native script import/compilation must succeed in your Editor before menus become available; that has not been checked here.

Expected outputs after your successful operation:

- The staging scene remains open.
- `Assets/LastSignal/Art/FirstTownVisualSlice/Profile.asset`: actual baseline-grounded camera poses, scope, seed and budgets.
- `Assets/LastSignal/Art/FirstTownVisualSlice/Generations/G<timestamp>/`: TerrainData, architectural/road meshes, calibrated asphalt/sky/lighting and selected URP prop wrappers.
- `Docs/Implementation/FirstTownVisualSlice/Evidence/authoring-<timestamp>.json`: actual instance counts, skipped props and status; `assets-<timestamp>.tsv`: source paths.
- A stage checkpoint exists before modification. On exception the scene is restored; generated assets remain for diagnosis.

If it fails, send the first Console exception and `failed-<timestamp>.txt`. Do not regenerate the older full-town/forest passes to work around an error. Those own different data and would invalidate this local comparison.

## Consistent captures

Use **Last Signal → First Town → Visual Slice → 2 Visual comparison studio** (`SliceVisualReview.Open()`). Load the fixed pose profile. Pick a view and condition; press **Capture selected comparison + settings manifest**.

You can do this on production before staging if you want an untouched baseline first. Otherwise production remains untouched and can still be opened for baseline captures after the stage is saved. Use the same saved Profile asset on both scenes. Do not modify poses between the pair.

Captures go to `Docs/Implementation/FirstTownVisualSlice/Evidence/Captures/`. Every PNG is accompanied by a JSON settings manifest. Baseline/stage names are separate and timestamped; captures do not overwrite earlier evidence.

| View | X/Z position | Aim X/Z | Purpose |
|---|---|---|---|
| 01 Safehouse exterior | -174 / -157 | -181 / -170 | Shelter frontage, refuge silhouette, adjacent woodland and yard |
| 02 Forest trail | -216 / -140 | -224 / -157 | Forest floor, trunk depth, trail readability |
| 03 Roadside approach | -168 / -135 | -116 / -135 | Road shoulders and uninterrupted bridge approach |
| 04 Gas station | 50 / -116 | 73 / -80 | Canopy, pumps, forecourt, approach and workshop roof |
| 05 Service frontage | 65 / -70 | 70 / -55 | Workshop roof, facade joinery, entrance and nearby props |
| 06 Aerial coverage | -63 / -103 at y270 | -63 / -104 | Endpoint/corridor limits, creek and road topology |

Perspective views use **75° vertical FOV**, matching the existing player prefab, and 1920×1080, HDR/ACES through the current volume, SMAA Medium, no overlay Canvas/viewmodel or editor Gizmos. Initial eye height is 1.65 m above the sampled baseline terrain (1.69 m for road/apron viewpoints to account for existing raised surfaces). Positions are fixed thereafter; aerial is orthographic. The authoritative position and height are the saved profile and capture JSON, not rounded numbers here.

Conditions: 08:20 day; 17:40 sunset; noon overcast; noon rain lighting + wet surfaces; 23:00 night; night with inspection flashlight. Sky/ambient calibration is deliberately changed by this implementation and logged, rather than concealed with unrecorded exposure adjustment.

**Capture limits:** These are explicit Editor environment renders, not runtime acceptance. They sample the existing lighting function and material response but do not simulate rain particles, roof exposure, gameplay or weather progression. The flashlight uses the existing S013 inspection specification (range12, outer50°, inner27°, intensity3); it is not a claim that a shipped flashlight item/input exists. Live environment reflections and interior volume behavior may also differ from a static capture. The fixture restores its temporary RenderSettings, lights, material swaps and shader globals.

Start with **views 01, 02, 04 and 05 in daylight**. Send those PNG/JSON pairs and authoring report first. If those reveal art or geometry defects, repair them before running the full condition matrix.

## Navigation and manual gameplay

When the first visual pass is coherent:

1. Keep the stage open. Choose **Last Signal → First Town → Visual Slice → 3 Bake STAGE navigation (user only)**. Exact method: `SliceAuthoring.BakeStageNavigation()`. It creates separate navigation data in the stage generation, temporarily excludes interactive door leaves, restores them, and does not run an agent route test. **Do not use the old First Town bake menu: it opens the production scene.**
2. Choose **Last Signal → First Town → Visual Slice → 3b Isolate save slot before manual play**. Exact method: `SliceAuthoring.IsolateReviewSave()`. This gives the stage a new scratch slot; world ID, content version and entity IDs remain unchanged. No save file is deleted or copied. Promotion restores the original slot.
3. Enter PlayMode yourself. Walk out of the safehouse, follow the north gate → south street → bridge → eastward junction → gas approach, inspect the service room, then return. Also use the western foot trail. Do not teleport to claim core route completion.
4. Drive the existing pickup along that route. Check bridge entry/exit, steering space, junction turns, braking, suspension contact, parking and seat-exit clearance. Report actual failures; authored dimensions are not measured vehicle-clearance evidence.
5. Check safehouse storage/rest, starter supplies, existing guaranteed fuse, station wrench/fuel/scrap, both service doors and encounter activation. Fight/pursue/retreat around the new vegetation and props. Verify no new collision blocks an existing interaction ray.
6. Save/load using the isolated review slot, verifying player/vehicle position, doors, loot depletion, enemies, time and weather. This is your manual persistence acceptance, not a completed result.
7. Review actual night, rain and roof transitions using the existing clock/weather controls. Keep HUD captures as evidence of time/weather, alongside clean presentation captures. No new runtime clock, rain simulation or flashlight gameplay system was introduced.

Performance remains **NOT_MEASURED**. After appearance and traversal are acceptable, measure representative route segments on the target hardware: CPU/GPU ms, frame-time p50/p95/p99, batches/draw calls/triangles, main thread/GC, texture and native/managed memory, shadows/overdraw, transitions and spikes. Include day, rain, night, driving and enemy pursuit. A Mac Editor capture does not establish Windows player acceptance.

## Detailed visual acceptance checklist

Use matched shots and inspect at full 1080p resolution. None is pre-marked PASS.

- [ ] Safehouse remains a recognizable, warmer refuge; the door, yard, parking and starter supplies read clearly at player height.
- [ ] Main road remains continuous through the bridge and two approach turns, with no road/shoulder tearing, floating ribbons, z-fighting or disconnected corners.
- [ ] Grass → soil → worn gravel → asphalt transitions feel deliberate. No broad mustard plane or equally spaced grass confetti dominates a hero frame.
- [ ] Road edge drainage and berms look shallow and plausible; no altered terrain buries loot, props, door thresholds, foundation or parking surface.
- [ ] Station canopy/pumps/sign remain recognizable from the approach. The office and service shop give the forecourt a coherent hierarchy.
- [ ] Workshop roof has credible pitch, ridge, overhang, seams, gable closure and corner joins. No black backfaces, open gaps, floating gutter or clipping awning.
- [ ] Entrance affordances match the functioning front and rear doors; trim/props do not narrow or block their swings and escape routes.
- [ ] Imported prop materials are neither pink nor unlit, excessively glossy, stretched or visually foreign. Wrecks retain believable length/height and terrain contact.
- [ ] Skipped props in the authoring report are reviewed. A skipped story element is incomplete, not silently counted as implemented.
- [ ] Lettering reads correctly from both intended board faces, stays within the board and is legible at the normal gameplay FOV. No oversized/reversed development text in hero frames.
- [ ] Canopy, understory and forest floor create near/middle/far depth; no wall of identical trees or repeated same-size bushes.
- [ ] Forest trail remains visible, with low cover below threat-reading height and open retreat space. No leaf mesh collider or tall shrub obstructs the combat corridor.
- [ ] Log/rock/grass grounding is believable; no floating meshes, buried crowns, stretched leaves or obvious LOD disappearance in the foreground.
- [ ] Daylight has readable shaded foliage and building faces without a blown-out/cyan sky. Sun/shadow direction and sky remain coherent at sunset.
- [ ] Overcast preserves shape; rain darkening is material-specific, not a universal mirror finish. Real rain stops/attenuates under safehouse and workshop roofs.
- [ ] Night preserves route landmarks and local warm/cool hierarchy without uniformly illuminating the whole forest.
- [ ] Inspection flashlight reveals texture and geometry defects without clipping white; actual gameplay flashlight availability is recorded separately.
- [ ] Unrelated districts remain unchanged; the aerial footprint matches the approved endpoints/corridor instead of a whole-town scatter pass.
- [ ] Gameplay, nav, vehicle, save/load and performance evidence is supplied separately from art captures.
- [ ] Owner explicitly accepts the exemplar before promotion or expansion.

## Regeneration, hand edits and promotion

Regenerate only with the **stage** open, using menu 1. Move artist-created objects out of Generated and into **Artist additions — preserved**. Renderer bounds in that root become placement exclusions. Re-running uses the same seed and baseline terrain; visible objects do not accumulate. Prior generation folders and checkpoint scenes are retained, so disk usage does increase intentionally.

Manually altered managed original renderers, re-enabled old vegetation, changed staged terrain/sky/calibration, or changed production scene/baseline terrain cause a refusal rather than silent loss. Keep the checkpoint and reconcile changes. Generated mesh/prefab edits inside the replaceable generation are not a persistent artist layer; archive them or move authored instances to Artist additions before regenerating.

After your visual and gameplay acceptance, choose **Last Signal → First Town → Visual Slice → 4 Promote ACCEPTED stage with backup** (`SliceAuthoring.Promote()`). It checks the source hash, asks for your art acceptance, backs up production into Evidence, restores the production save slot if review isolation was enabled, saves without replacing its `.meta`, and archives the stage. This operation has not been run.

To discard a candidate, simply reopen production; its scene and TerrainData were not changed. To recover a failed generation, use the saved stage checkpoint. To reverse a promotion, retain all generated assets, close the production scene, restore the backed-up `.unity` contents to the original path while keeping the original `.meta`, then reopen. Do not delete generation assets while a scene or archive still references them.
