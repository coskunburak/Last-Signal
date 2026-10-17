# FirstTown visual slice — canopy refinement 05

The owner captures made after Refinement04 show a continuous road surface and a human-height compound fence. They also show that the imported gas-station canopy reads as an open lattice from the road, its approach pole blocks the forecourt, and the station has little local light at dusk. These are observations from the saved staged-scene captures, not gameplay verification.

Baseline captures:

- `Evidence/Captures/stage-lighting-current-view4-condition1-20261017-094824-083.png` — station day.
- `Evidence/Captures/stage-lighting-current-view4-condition7-20261017-094831-484.png` — station dusk.
- `Evidence/Captures/stage-lighting-current-view3-condition2-20261017-094843-175.png` — roadside sunset.

`SliceRefinement05` adds a continuous metal roof sheet over the measured, existing vendor canopy roof collider; a deep fascia, drainage edge and standing seams give the roof a legible first-person profile. It retains the vendor canopy, its supports, its colliders and the existing rain-contact object. Two mounted warm lamps join the existing bounded practical-light system. The earlier approach pole moves 10 m toward the forecourt only if it still occupies the exact Refinement04 position; an artist-relocated pole is left alone. No terrain, road collision, gameplay component, source scene or vendor asset is edited.

The operation requires the saved Refinement04 marker, an unmodified measured canopy position and one recognizable vendor roof collider. It refuses unexpected geometry or pre-existing output assets. It saves a timestamped scene checkpoint and an authoring JSON before/after report under `Evidence/Refinement05/`, then writes to the same `FirstTownCore_VisualSlice` stage. Re-running after success leaves the stage untouched. The generated scene objects sit under `08 Refinement05 — weatherproof station canopy`; the four new owned assets sit under `Assets/LastSignal/Art/FirstTownVisualSlice/Refinement05/`.

Current status: code authored, **not yet executed in Unity**. Visual quality, navigation, runtime interaction and performance are **not verified**. The earlier source-scene and TerrainData hash differences still block promotion to the production scene; this independent stage refinement does not waive either guard.

Owner action after saving any personal scene edits: open `Assets/LastSignal/Scenes/Validation/FirstTownCore_VisualSlice.unity`, then choose **Last Signal → First Town → Visual Slice → 2c Repair station canopy and lighting (existing stage)**. Inspect the Console and `Evidence/Refinement05/authoring-*.json`. In Comparison Studio keep **Archived lighting comparison** off and capture **04 Day** and **04 Dusk** from the gas-station camera. Compare them with the two station baselines above, especially roof closure, pole silhouette, warm light pools and road-side visibility. Do not promote the stage yet.
