# FirstTownCore forest — owner-run production handoff

## Current resume point — navigation validation fix

The owner subsequently reported reference/clearance **PASS**, completed the navigation bake, then received `Disconnected forest marker Clearing — retreat radius 12 (-222, 2.28, -80)`. Read-only scene inspection confirmed a baked `Data/FirstTown/Navigation.asset`, PlayerSpawn inside the safehouse, and an enabled carving door leaf at its exit. Forest navigation validation omitted the temporary door-obstacle exclusion already used by FirstTown validation.

The forest check now temporarily disables only active, carving obstacles belonging to openable doors, waits for native navigation updates, and restores them on success, failure, reload or PlayMode transition. Other obstacles remain active; every forest marker still requires a complete path from PlayerSpawn with the same agent type and sampling radius. Failure now reports the sampled endpoints and last reachable path corner. This fixes a validation-condition mismatch; it does not establish that the reported route is connected until the owner reruns the check.

**Next owner action only:** after script compilation, run **Last Signal > First Town > Forest > 4 Validate baked forest navigation**. No Stage, Promote or rebake is needed for this validator-only change. The agent did not execute the navigation check. Runtime door traversal, visuals and performance remain unaccepted.

## Earlier implementation and owner history

**Overall PARTIAL — owner progress updated 2026-10-16.** The owner executed Stage and Promote. Production now contains the authored forest; the pre-forest scene backup is `Evidence/FirstTownCore-before-forest-20261016-082740434.unity`. Reference validation failed on Pine's shared shader; navigation validation stopped because no NavMesh had been baked. Visual/runtime acceptance remains NOT_RUN and performance NOT_MEASURED.

The recorded Metal compiler error was `invalid subscript 'positionOS'` in `ForestLit.shader`'s DepthOnly wrapper. The wrapper now uses `input.position`, matching the installed URP DepthOnlyPass. Other Lit passes correctly retain `positionOS`. Material validation now includes the material path, shader name and compiler diagnostics. These source fixes have not been compile- or acceptance-tested by the agent.

**Resume without regenerating:** let Unity import the shader fix, keep production FirstTownCore open and rerun **Forest > 2 Validate saved stage references and clearance** (it also accepts production). Resolve any remaining validation errors, then run **Last Signal > First Town > 2 Bake navigation (user only)**, followed by **Forest > 4 Validate baked forest navigation**. Stage and Promote do not need repeating for this shader fix.

The latest brief explicitly says: “Native Unity authoring gerekiyorsa güvenli Editor scriptini hazırla; ilgili çalıştırma işlemini kullanıcıya bırak.” Consequently the deliverable is executable staged authoring, not a claim that new forest objects already exist in the production scene.

## Staging workflow for future layout changes (already completed for this generation)

1. In Unity, save edits and open `Assets/LastSignal/Scenes/Production/FirstTownCore.unity`.
2. Run **Last Signal > First Town > Forest > 1 Create or regenerate staging scene**.
3. The editor opens `Assets/LastSignal/Scenes/Validation/FirstTownCore_ForestStage.unity`. Inspect the `FirstTown Forest — owned authoring` root. Its Layout asset exposes seed, budgets, minimum spacing, clearings, trail points/widths and artist exclusion bounds. Generated tree/rock/bush content is replaceable; **HeroTrees — artist protected** is retained.
4. Save the stage; run **2 Validate saved stage references and clearance**. Review source/terrain intersections and Console before promotion. No automatic test or bake is chained to either command.
5. When satisfied with authoring, run **3 Promote saved stage to FirstTownCore**. It refuses if production changed since staging, writes a source-scene backup under Evidence, saves the staged contents to the existing production scene path, and archives the staging scene under the owned Art folder. Source scene GUID and gameplay identities are retained. A later Stage begins from current production, preserving manual edits outside Generated.

If an existing stage is found, the tool refuses to replace it from production. Open that stage to regenerate. Regeneration replaces only Generated plus its owned terrain/detail content; artist HeroTrees and external scene objects are preserved. Each generation writes fresh owned assets; old generations remain referenced by archived stages and are not automatically deleted. Stage failures restore the saved staging checkpoint. Source-hash mismatch is a real merge/rebase stop: preserve both scenes and reconcile edits before retrying.

## Delivered system

- One existing 500m terrain, no new world/streaming authority. Owned TerrainData derivative, three gentle authored landforms, preserved road/building/river elevations, four blended Fantasy ground layers and three connected forest routes.
- Cluster density field plus seeded spacing, slope, height/moisture/species selection, road/structure/roof/vehicle/spawn/artist/clearing exclusions. Intended upper budgets: 340 canopy trees, 520 small bushes, 85 rocks, 12,000 terrain-instanced grass units and 16 pieces of existing bark-submesh debris. Actual placement counts are reported by the user-run generation, not asserted here.
- Approved S013 Fantasy Pine/Birch/Bush/Rock/Grass derivatives. Shared materials, bounded wind, explicit collider policy and supported URP dithered cull transitions. **No fake claim of reduced geometry LOD1/LOD2**: source meshes are already small and only one geometry tier is shipped. See performance plan for the resulting limits.
- One forest presentation component reads WorldClock/TownAtmosphere and sends global wind phase/wetness to owned URP shader derivatives. No independent clock/weather/save authority, no per-tree Update, no per-tree material clone. Existing rain/fog/lighting/audio ownership remains intact.
- Primitive tree/rock colliders; bushes/grass/small bark debris have no collider. Managed vegetation has no dynamic NavMeshObstacle. Existing terrain collider is rebound to the derivative; old NavMesh data is detached in staging because geometry changed. Existing bake tool remains the only bake operation.
- No additional live encounters or persisted IDs. Three clearings and forest route markers provide navigation/retreat review points without changing the existing thirteen-enemy save topology.
- Six disabled fixed review cameras are prepared by Stage. Screenshots and all visual acceptance belong to the owner.

Read `ENTRY_AUDIT.md`, `DESIGN_AND_ASSETS.md`, `MANUAL_QA.md` and `PERFORMANCE_PLAN.md`. `changed-files.txt` lists source changes; generated paths and real counts will be in `Evidence/stage-*.json` after your Stage command.

## Remaining limits

Visual quality and stable performance cannot be established from authored rules. Crossfade, custom terrain base-pass wetness, instanced grass wind, foliage near-camera silhouette and texture palette all require in-editor/player review. Native authoring has been executed by the owner, but reference/clearance validation has not yet passed.

Distinct leaf-litter textures, bespoke roots/stumps, reduced-geometry LODs/impostors, local volumetric shafts and new forest audio recordings were not added. Existing bark geometry/ground textures are used where appropriate; incompatible families and white placeholders were not introduced. Forest night/rain audio continues through the existing global ambience system, without a new zone audio framework.

Future 1km expansion needs explicit adjacent tile bounds/routes/exclusions and seam review; this implementation intentionally refuses an unexpected terrain size/position. It does not populate or claim support for unseen tiles automatically. Terrain detail inputs with existing non-512 resolution or a different scatter mode are rejected instead of being silently converted.

Current saves and gameplay schemas are not edited. After promotion, test a save whose player/vehicle was inside the regenerated forest: changed environmental geometry can invalidate a previous free position even though the save format remains compatible. Do not claim save placement safety until that review is complete.
