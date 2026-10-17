# Refinement 01 — existing staged service frontage

Status: **APPLIED_RENDER_MESHES; Unity import and visual acceptance pending.** This is a bounded repair of the saved candidate, not a regenerated environment or a claim that the full reference quality bar has been met.

## Actual evidence used

The saved `Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity` references the four render meshes edited below. Their GUIDs occur only in this scene, each in one MeshFilter; none is referenced by a MeshCollider, another scene, or a prefab. The stage remains byte-identical to its pre-pass file. Updating its existing referenced assets makes the refinement available in the same scene without overwriting an open scene's in-memory changes.

The owner's initial generation report remains `Evidence/authoring-20261016-112859-538.json`. No stage PNG/JSON captures were found in the existing project evidence. The conversation's JPG comparison contains the old baseline and aspirational references; it is not proof of the current candidate. No existing image needs to be repeated. Screenshot-based lighting, composition and fidelity acceptance therefore remain pending. The defects addressed here are measured directly from serialized scene/mesh data, not inferred from authoring descriptions.

## Changes already applied

All four assets are under `Assets/LastSignal/Art/FirstTownVisualSlice/Generations/G20261016-112859-538/`. Their existing `.meta` files and GUIDs are retained. Existing materials, renderers, transforms, wet-surface registration and scene object IDs remain in use.

| Asset | Saved candidate before | Applied refinement |
|---|---|---|
| `Mesh_012.asset` — Timber gable infill | Gable peak y=5.05 below roof ridge y=5.25; gap grows to approximately 0.313 m at x=±5. Roof consists of upward-facing top surfaces. | Gable infill now meets an 80 mm roof lining; front/rear inner faces and edge returns close the panels. Downward-facing roof lining and eave edges close the visible underside while retaining the original roof top and collision mesh. |
| `Mesh_013.asset` — Bargeboards and corner joinery | Front battens stop at the short gable; rear has no equivalent battens. Corner boards end at y=3.3. | Front/rear battens follow the corrected roof slope; corner boards reach the gable/eave transition. Horizontal trim and framed attic vents articulate the upper frontage. |
| `Mesh_014.asset` — Standing seams ridge cap and drainage | Box-shaped gutters and vertical bars; ridge is a rectangular bar. | Half-round gutters with inner/outer surfaces and end closures, connected swan-neck downpipes, wall collars and discharge bends; folded ridge cap follows the pitch; standing seams meet the roof; vent louvers provide modeled detail. |
| `Mesh_010.asset` — Service pedestrian paving | Nine flat strips stop at x=67.4, offset from the existing entrance at x=70. | Jointed, beveled paving retains the approach and adds an L-shaped landing to the existing entrance. Top height stays around y=0.032, matching the established flush path; no new step or collider was introduced. |

The four render meshes total 836 → 2,424 triangles (+1,588). This is an asset count, not a performance measurement. No new material, texture, renderer, light, behaviour, collider or scene object was introduced. Safehouse, forest, road route, doors, loot, vehicle, zombie systems and persistence are unchanged by this pass.

## Gap analysis and acceptance boundaries

| Area | Evidence / current conclusion | Remaining work |
|---|---|---|
| Terrain elevation, drainage, ground transitions | Saved terrain exists. Original baseline TerrainData still matches its recorded hash; generated TerrainData differs. File hashes do not establish the shape or appearance of that difference. | Review actual stage ground contact, swales and texture blending. No terrain overwrite or further height/splat edits made in this pass. |
| Forest density, silhouettes, ground cover | Owner report records 81 trees, 230 shrubs, 60 rocks and 5,788 grass instances. Installed approved assets are retained. These counts do not establish believable density or good silhouettes. | Player-height forest captures needed for spacing, near/mid/far depth, leaf shading, LOD transitions and ground richness. No speculative rescatters. |
| Roads and shoulders | Saved road meshes retain the approved endpoint/corridor. Main surfaces are at y≈0.021; apron y≈0.01; existing junction plates y≈0.04. These measured surfaces are not a road-quality pass. | Inspect seams, overlaps, shoulder blending and driving appearance in actual views; avoid changing height/collision without evidence. |
| Safehouse fidelity and storytelling | Existing safehouse, props, IDs and initial slice work retained. The old screenshot's reversed lettering is not proof that the staged labels remain wrong. | Evaluate the current safehouse at normal player FOV before selecting another art edit. |
| Gas station and service architecture | Actual gable gaps, roof underside omission and offset paving are confirmed by saved data and repaired above. Existing canopy, office, pumps and materials are retained. | Confirm imported shading, material scale, soffit/trim joins, vent and gutter detail, entrance grounding and the overall station composition in Unity. |
| Sky, lighting, shadows, fog, weather | Existing sky/lighting assets and weather director retained byte-for-byte. No stage captures establish exposure, fog depth or weather quality. | Matched daylight/sunset evidence first; overcast/rain/night later if those views are coherent. No unsupported exposure or fog tuning. |
| First-person coherence | Entry landing now reaches the established service-door approach; camera poses/FOV are unchanged. | Review 04 and 05 at the fixed 75° FOV. No aerial or offline geometry result substitutes for first-person visual acceptance. |

## Evidence and exact changed files

- Four `Mesh_010/012/013/014.asset` files above: actual environment changes.
- `Tools/first-town-slice-refine.py`: deterministic offline mesh patch, strict input hashes, reference-scope preflight, recovery archive, idempotent apply and guarded restore. It does not invoke Unity.
- `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceAuthoring.cs`: refuse regeneration while the recorded in-place refinement is active, so the old generator recipe cannot silently erase it. Existing source/terrain guards remain.
- This report, `MANUAL_ACCEPTANCE.md`, `IMPLEMENTATION_REPORT.md`, and `changed-files.txt`: current status and focused review instructions.
- `Evidence/Refinement01/manifest.json`: exact before/after mesh hashes, counts, bounds and protected-file hashes.
- `Evidence/Refinement01/original-render-meshes.zip`: original bytes of only the four edited render meshes; not a replacement scene.
- `Evidence/Refinement01/offline-checks.json`: independent inspection of serialized vertex/index buffers, normals/tangents, gable cross-sections and entry paving coverage.
- `Evidence/Refinement01/promotion-safety.json`: read-only source divergence investigation.
- `refinement-01-changed-files.txt`: exact file list for this pass.

There are **no rendered before/after captures for this pass**. The archive and JSONs are geometry/file evidence only. No Unity authoring, capture, test, compilation gate, build, navigation verification, runtime check or performance benchmark was invoked by the assistant.

## Promotion safety

The source scene differs from the initial source hash. Comparing it with the saved pre-generation staging checkpoint identifies two additional Barrier prefab instances, scale changes on two existing Barrier instances and one Wreck instance, and the corresponding hierarchy references. Preserve these differences; do not copy the old scene over them. The checkpoint is a separate Unity save, not a byte-identical substitute for the initial source file.

The original forest TerrainData still matches recorded `C7FCEB5A…B2E387`. The generated terrain differs from its recorded hash. Its original generated bytes were not archived, so the exact terrain change cannot be reconstructed from the hash alone. Neither the original nor generated TerrainData was edited in this pass. Source reconciliation, generated-terrain investigation, visual/gameplay acceptance and explicit owner authorization remain prerequisites for promotion. None blocks this independent render-mesh repair.

## Minimal owner review

1. Keep/open the existing `FirstTownCore_VisualSlice`; let Unity import the changed assets. If automatic refresh is disabled, use **Assets → Refresh**. Do not generate another stage or promote. If the stage is already open, no scene reload is required because its references did not change.
2. Open **Last Signal → First Town → Visual Slice → 2 Visual comparison studio**. Use the existing `Assets/LastSignal/Art/FirstTownVisualSlice/Profile.asset`; keep poses/FOV unchanged.
3. Capture **04 Gas station / Day 08:20**, **05 Service frontage / Day 08:20**, and **05 Service frontage / Sunset 17:40** with **Capture selected comparison + settings manifest**. Inspect the gable-to-roof join, visible soffit, front/rear trim, gutter/downpipe connection and paving reaching the existing entrance. Check for missing/pink meshes, culling holes, shading artifacts, texture stretching, intersections and flickering seams.

Only these views are requested for this pass; no repeated generation, whole-world tests, navigation bake or recapture of untouched areas is needed to review this repair. Send those PNG/JSON pairs from `Evidence/Captures/`, or the first Unity import/Console error if they cannot be produced. Native verification remains the owner's responsibility.

## Reversal, only if needed

From the project root: `python3 Tools/first-town-slice-refine.py --restore`. This restores only the four archived render meshes and marks the refinement restored. It refuses if their bytes have changed since this pass, preserving later edits. It does not reload/save scenes or touch terrain. Do not execute it just to review the current result. The same tool with `--apply` applies the audited pass once; a repeat detects the applied bytes and writes nothing. Without flags it inspects state without writing.
