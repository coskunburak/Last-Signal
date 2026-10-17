# User visual acceptance

Overall status: **NOT_RUN**. Allowed row statuses are PASS / FAIL / NOT_RUN; record observations and evidence path before changing any row. No official-render match or production approval has been asserted.

## Repeatable material fixture

Open `Assets/LastSignal/Scenes/Validation/ArberryMaterialComparison.unity` manually. It contains an isolated comparison prefab, a wood floor plus real S013 metal/rock swatches, six fixed cameras, and four mutually exclusive light groups. It does not retune S013Cabin or any production lighting profile. Disable all but one camera and one light group. Render at 1920×1080 with unchanged project quality and no added post-process/exposure overrides. Record Unity/platform/quality before review. Camera transforms, FOV and background are serialized; no auto exposure is added.

For A/B, enable exactly one of Baseline and Candidate at identical local transforms. Baseline uses the preserved original external material remaps; Candidate uses reconstructed maps. Both use the same current Humanoid import and source geometry, so this compares materials, not before/after skeleton import. Baseline is disabled by default. Do not enable both simultaneously. No auto screenshot or render benchmark runs.

NeutralDaylight / Overcast / Shelter / NightFlashlight provide explicit serialized key/fill lighting. Camera_Front, Back, Side, ThreeQuarter, Face and JacketGloves reproduce framing. These are review presets, not certified calibration exposures.

## Acceptance matrix

| Capture / behavior | Status | Observe |
|---|---|---|
| Front/back/side | NOT_RUN | Single outfit, silhouette, 1.4 cm rest sole underflow, body clipping |
| Three-quarter full body | NOT_RUN | Scale against unchanged 1.8 m capsule; straps and silhouette |
| Face and hair | NOT_RUN | Iris/pupil, skin specular, hair alpha and back faces, normal Y convention |
| Jacket and glove close-up | NOT_RUN | Stitching, roughness, discarded polygon/hole, no magenta fallback |
| Daylight | NOT_RUN | Source colors and moderate environment detail |
| Overcast | NOT_RUN | Cloth/skin separation, AO not over-dark |
| Shelter | NOT_RUN | Warm lighting without plastic specular |
| Night/flashlight | NOT_RUN | No eye emission; normals, cutout shadows, readable surfaces |
| Rifle hip | NOT_RUN | Shoulder pocket, two palms, bounded wrists, torso clearance |
| Rifle ADS/recoil/reload/cancel | NOT_RUN | Sight alignment, magazine hand transitions, no stale offsets |
| DXT/Glock17/Custom hip+ADS+reload | NOT_RUN | Variant contact/trigger, own fingers, unchanged ammo state |
| Axe ready/anticipation/strike/recovery | NOT_RUN | Both handle contacts, no geometry penetration |
| Machete | NOT_RUN | Single hand and existing blade length |
| Crowbar | NOT_RUN | Transferred palm orientation/contact; cancellation recovery |
| Crouch and slide | NOT_RUN | No arm/torso penetration, stance recovery |
| Sprint/strafe/forward/back | NOT_RUN | Positive gait, foot slip, no upper-layer sticking |
| Seated left/right steering | NOT_RUN | Pelvis and wheel reach, pause, exit, seated death |
| Owner FPS/scope and external views | NOT_RUN | One body/shadow; own FPS arms only; sleeve/glove continuity |
| Death/new session/save-load | NOT_RUN | One body, correct equipment, no stale targets/subscriptions |
| Performance | NOT_RUN | Skinning/Animator/GPU time, draw calls, texture residency, steady allocations |

For motion use a fresh session in `Assets/LastSignal/Scenes/Production/S013Cabin.unity` after the manual compile gate passes. F6/F7 and Scene view remain available. Save reproducible pose, camera and lighting details for each failing capture; do not vary exposure between A/B shots. Review locally; the asset's no-AI restriction still applies to screenshots of protected character content.

No authored LOD is available. Budget acceptance needs measured macOS and Windows results; 120,917 triangles, 13 slots and ~157 MiB derived textures are starting costs, not approved production budgets.

## Actual FPS rig follow-up (2026-10-14)

All checks below are NOT_RUN. In S013Cabin inspect rifle, DXT, Glock17, Custom9mm, crowbar, machete and axe at normal and wide FOV. Inspect both hand surfaces against grips, trigger placement, thumb clearance, sleeves and cropped shoulder openings. Check hip/ADS, firing, empty/tactical reload and cancellation, sprint/slide/crouch, all melee phases, treatment, weapon switches, vehicle entry/exit and death. Confirm no A-pose flashes, frame-to-frame drift, ghost legacy hands, magazine leaks, duplicated world weapons or misaligned scope image. The new FPS and world use the same Arberry skin/glove/jacket materials; match appearance under comparable lighting. Record actual observations before changing any status to PASS.
