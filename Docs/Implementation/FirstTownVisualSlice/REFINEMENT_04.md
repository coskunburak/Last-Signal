# Refinement 04 — continuous asphalt, compound scale, practical light reach

2026-10-17. Based on the owner's corrected captures at 09:26–09:27 UTC. The Lighting03 display-transfer correction is visibly effective, but the scene is **not visually accepted**. No Unity authoring, compilation gate, runtime, tests, build, navigation bake or performance benchmark was run by the assistant.

## Implemented now: existing referenced assets

- Updated the existing generation's `Asphalt.mat`, retaining its GUID and all scene references. The former gas-station road texture is a **UV atlas for separate road meshes**, with large black gutters between islands. Repeating the complete atlas on the continuous road produced the black shard pattern visible in the supplied day capture. It now uses the project's existing `asphalt_02_diff_4k.jpg`; UV scale is 1.5 against the stage mesh's metres/6 coordinates (4 metres per tile).
- Copied the matching `asphalt_02_nor_gl_4k.exr` byte-for-byte into project-owned `SurfaceRefinement04/AsphaltNormal.exr`, with a fresh GUID and Normal Map/linear import settings. The vendor normal was imported as a color texture and is left unchanged. Removed the unrelated atlas metallic map; dry asphalt is dielectric, smoothness 0.18, normal strength 0.65. Existing wetness response remains in TownAtmosphere. Roughness/displacement images were not misassigned to incompatible URP slots.
- Updated the current stage Lighting.asset's named practical gains only. Gas forecourt: 1.18 → 4 (dusk intensity 4.72 → 16); Gas approach: 0.9 → 4 (dusk approximately 1.69 → 7.53); service shop interior: 1.05 → 1.7; safehouse gate: 1.1 → 2.5; safehouse yard: 0.9 → 2. Ambient, sun, sky, post exposure, clock/weather and the light/shadow budgets are unchanged. These are candidate calibrations, not visually verified results.
- Updated `SliceAssets.Asphalt` so any later deliberately reconciled generation uses the same compatible road textures. No generation was run.

## Prepared owner-operated scene correction

**Last Signal → First Town → Visual Slice → 2b Apply road and fence refinement (existing stage)**

Callable method: `LastSignal.Editor.FirstTownVisualSlice.SliceRefinement04.Apply()`.

This requires the existing saved stage in Edit Mode. It does not create another scene or regenerate terrain/content. It:

1. Checks the original compound transform, project-owned fence identity, mesh bounds and absence of gameplay components before modifying anything. Already proportioned fences and manually moved approach lights are retained.
2. Writes a pre-operation stage checkpoint under `Evidence/Refinement04/stage-before-<timestamp>.unity`.
3. Replaces only oversized compound fence runs with repeated project-owned fence modules at **2.4 m height**, keeping their original bottom height and gate opening. Uniform height/depth scaling preserves board proportions; module width is fitted to the measured run span.
4. Sets the north fence behind the road, from z=-140 to **z=-146**, and shortens the two adjoining side runs to meet it. The old north fence was exactly on the cross-street centerline; camera movement could not fix this layout defect. The 24 m gate opening in X remains.
5. Moves the original gas approach light and its decorative pole together from x=51 to **x=56.2**, outside the road's x=45.5…54.5 footprint, only if both remain at their original authored locations.
6. Keeps old fence objects/IDs in place but inactive, so oversized colliders disappear together with their renderers. Replacement prefab colliders form the correctly sized barrier. New objects live under `07 Refinement04 — human-scale compound fence`, within the existing generated root. No player, weapon, persistent entity, zombie, vehicle, interaction or vendor prefab is changed.
7. Saves the same stage, records old transforms/new dimensions and hashes in a timestamped authoring JSON. Repeating the command finds the marker and leaves edits intact. Immediate Undo reverses the scene operation; the checkpoint is retained for later selective recovery. The asset edits above have a separate before-files ZIP.

A new restore guard prevents stage regeneration from silently discarding the retained originals and scene refinement. Existing source/TerrainData/atmosphere guards and baseline hashes are unchanged.

**Status distinction:** material and lighting assets are already edited on disk and used by the stage after import. Fence replacement and pole relocation are implemented in the Editor command but **have not been executed**; the current stage file was preserved byte-for-byte by the assistant.

## Verified assets reused

- `Assets/ThirdParty/asphalt_02_4k.blend/textures/asphalt_02_diff_4k.jpg`
- `Assets/ThirdParty/asphalt_02_4k.blend/textures/asphalt_02_nor_gl_4k.exr` (source for the owned normal copy)
- `Assets/LastSignal/Art/FirstTown/Prefabs/Fence.prefab` (owned wrapper of `Assets/ThirdParty/S23 NEW ASSETS/Industrial Props/Prefabs/Fence.prefab`)
- `Assets/LastSignal/Art/FirstTown/Prefabs/LightPole.prefab` (existing scene instance relocated; no prefab edit)

Exact modified/created paths: `refinement-04-changed-files.txt`. Backups, source/asset diff, reference checks and protected file hashes: `Evidence/Refinement04/`.

## Minimal manual verification

1. Allow imports to finish, save any current stage edits, then run **2b Apply road and fence refinement (existing stage)** once. Keep the authoring JSON/Console result; if it throws, send that error rather than regenerate the stage.
2. In **2 Visual comparison studio**, with archived lighting off, capture **04 Gas station / Day**, **04 Gas station / Dusk**, **03 Roadside approach / Sunset**.
3. Check: continuous aggregate asphalt without black UV-island gaps; canopy/gas-approach warm light reaches the ground without a washed-out sky; approach pole is outside the traffic lane; fence reads at human scale and does not occupy the road. These are acceptance targets, not a claimed PASS.

The prior three PNG/JSON pairs remain valid before evidence:

- `Evidence/Captures/stage-lighting-current-view4-condition1-20261017-092640-012.*`
- `Evidence/Captures/stage-lighting-current-view4-condition7-20261017-092656-445.*`
- `Evidence/Captures/stage-lighting-current-view3-condition2-20261017-092710-947.*`

No after images exist yet. Do not repeat the old generation, full lighting matrix, tests or builds just for this visual check. Owner navigation review is needed later because the fence collision footprint changes. Module/renderer count and performance are not yet measured; reused fence materials already support instancing, but this does not prove acceptable performance.

## Remaining gaps / blockers

Ground-to-road blending, overly uniform tree silhouettes, sparse layered understory, station architectural richness and distant town-boundary walls remain below the art target. Day sky saturation and final dusk balance still require evaluation with the corrected surface. This pass does not claim to finish those requirements. Source scene and generated TerrainData hash differences remain unresolved; explicit approval and reconciliation are still required before promotion. No promotion performed.
