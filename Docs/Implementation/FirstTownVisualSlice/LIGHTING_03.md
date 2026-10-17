# Lighting 03 — owner capture correction, 2026-10-17

The five actual owner PNG/JSON pairs are retained under `Evidence/Captures/`. They confirm the correct staged scene and phase lighting profile. They do **not** meet visual acceptance: daylight road/foliage shadows are excessively dark, sunset loses facade detail, dusk loses almost the entire environment, and view 03 is obstructed by the safehouse fence. No new Unity render, compilation gate, test, PlayMode session, build, bake or benchmark was executed by the assistant.

## Implemented fixes

- **Capture transfer:** the old ARGBHalf target stored linear values, but RGB24/PNG export omitted display encoding. The installed URP source confirms it does not add a software sRGB conversion for this offscreen target. Readback now retains floating-point precision, applies `Color.gamma` once in Linear projects after the existing post processing, and only then quantizes to PNG. Gamma projects skip that conversion. The render target stays HDR: this URP version also inherits its format for intermediate buffers, so replacing it with an 8-bit destination would risk clipping before tonemapping. This is an exporter repair, not a claim that runtime lighting is now accepted.
- **Road camera:** saved pose 03 and its source default move from z=-140 to z=-135; eye height, FOV and eastward direction stay fixed. The safehouse front fence lies at z=-140. Use the comparison studio's profile pose; existing scene marker objects were not rewritten.
- **Practical fixture safety:** only the small child `Fixture` renderer surrounding a light origin stops casting shadows during phase-lighting presentation. Architecture and foliage retain their shadows. Original shadow flags and property blocks restore at capture cleanup/session unbind. This removes a self-occlusion risk; its contribution to the supplied darkness has not been measured.
- **Evidence:** new manifests identify `captureRevision: LIGHTING03_SRGB`, output encoding, render target format, project color space and each nearby lamp's actual position/intensity/range/shadow state.

## Changed implementation files

1. `Assets/LastSignal/Scripts/Editor/FirstTownVisualSlice/SliceVisualReview.cs`
2. `Assets/LastSignal/Scripts/Runtime/Atmosphere/TownAtmosphere.cs`
3. `Assets/LastSignal/Art/FirstTownVisualSlice/Profile.asset`
4. `Assets/LastSignal/Scripts/Runtime/VisualSlice/FirstTownSliceProfile.cs`

No new art assets. No asset GUID, stage/source scene, TerrainData, lighting profile, sky texture, exposure calibration, weather/time authority or gameplay change. Before-file backups and hashes: `Evidence/Lighting03/before-files.zip` and `before.json`. Current hashes and preservation comparisons: `implementation-evidence.json`.

## Existing before evidence

- Station Day: `Evidence/Captures/stage-lighting-current-view4-condition1-20261017-085728-448.png`
- Station Sunset: `Evidence/Captures/stage-lighting-current-view4-condition2-20261017-090052-656.png`
- Station Dusk: `Evidence/Captures/stage-lighting-current-view4-condition7-20261017-090059-295.png`
- Safehouse Dusk: `Evidence/Captures/stage-lighting-current-view1-condition7-20261017-090112-850.png`
- Road Sunset: `Evidence/Captures/stage-lighting-current-view3-condition2-20261017-090124-414.png`

Each has a matching JSON. Old PNGs have the transfer defect and must not serve as correctly encoded brightness baselines. Their geometry/composition evidence remains useful. No after capture exists yet.

## Minimal owner actions

1. Allow the Editor to import changes; keep the existing `FirstTownCore_VisualSlice` open. Do not regenerate or promote.
2. Open **Last Signal → First Town → Visual Slice → 2 Visual comparison studio**. Leave **Archived lighting comparison** off. Use **Capture selected comparison + settings manifest** for just three images: **04 Gas station / Day 08:20**, **04 Gas station / Dusk 18:30**, **03 Roadside approach / Sunset 17:40**.
3. Review road/foliage shadow detail, dusk canopy light pools and the cleared road framing. The new JSON must contain `LIGHTING03_SRGB` and `DISPLAY_SRGB_BYTES`. New timestamped files go into the same Captures directory. These recaptures are needed because the original exporter was defective; the other images need not be repeated now.

If the corrected station dusk remains unreadable, the per-lamp manifest will support a targeted position/intensity/shadow correction. Runtime Game View comparison remains owner-operated and is needed before accepting gameplay visibility.

## Remaining visual and promotion gaps

The supplied frames still show repetitive tree silhouettes, abrupt rectangular road/ground boundaries, sparse ground cover, a utility pole dominating the road sightline and insufficient architectural/prop richness at the gas station. Color export alone cannot solve those geometry/material/composition gaps. No new production-quality PASS is claimed. Forest trail and corrected safehouse lighting remain unreviewed after this fix. Source-scene and generated TerrainData hash differences still require reconciliation before any approved promotion; no guards or stored baselines were bypassed. The currently saved stage has owner changes since Lighting 02 and was preserved byte-for-byte in this pass.
