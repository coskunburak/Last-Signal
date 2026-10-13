# Pistol implementation handoff

Status, 2026-10-12: user-operated compile, all focused/full test gates and macOS development build PASS. Gate JSON and NUnit XML were inspected after the user supplied results. Gameplay visual acceptance is PENDING; benchmarks are NOT_RUN. Necessary editor asset imports and static authoring renders remain separate from gate evidence.

Implemented: distinct per-finger grip/trigger poses for FPS and Diesel, per-rig palm locations, lower/raised heading calibration, commit-driven reload magazine/offhand presentation, right-arm reload composition, selected-rig editor handles, persistent authoring, focused tests and visual QA matrix. Existing one-handed ownership, sight alignment, bounded recoil, restoration, visibility and gameplay rules remain in place.

Retained: existing low-poly pistol, VAL Generic arms/gloves/sleeves, Diesel Humanoid and neutral owned Animator controller. No new mesh, Blender export, skin weights, cinematic clip, vendor modification, commit or push. Surface-detail fidelity remains below the VEIN reference and is not certified.

Verified evidence:

| Gate | Result / passed tests | Evidence directory |
|---|---|---|
| compile | PASS | 20261012T081057Z-compile-740f5546 |
| pistol-edit | PASS, 24/24 | 20261012T081353Z-pistol-edit-becfc59c |
| pistol-play | PASS, 7/7 | 20261012T081521Z-pistol-play-862ec2b1 |
| combat-edit | PASS, 57/57 | 20261012T081721Z-combat-edit-dab3c2c7 |
| combat-play | PASS, 15/15 | 20261012T081747Z-combat-play-2fa2a578 |
| character-edit | PASS, 28/28 | 20261012T081911Z-character-edit-375fa6b6 |
| character-play | PASS, 31/31 | 20261012T081938Z-character-play-fb01d814 |
| regression-edit | PASS, 748/748 | 20261012T082138Z-regression-edit-0cb449cf |
| regression-play | PASS, 409/409 | 20261012T082210Z-regression-play-a1222e78 |
| development-build | PASS, StandaloneOSX Development | 20261012T083838Z-development-build-1aee80ea |

All directories are under `Evidence/`. No failed, skipped or inconclusive tests; no unexpected source changes. The two expected ProjectSettings changes were SENTIS_ANALYTICS_ENABLED toggles, as recorded by source-preservation.json. Focused runs overlap the full suites; do not add their counts as unique tests. Full regression totals are 748 EditMode and 409 PlayMode.

Development build succeeded with 0 errors and 401 warnings; it is not warning-free. Its summary is in `Builds/S021/20261012T083838Z-development-build-1aee80ea/build.txt`. Log samples include deprecated Unity object-discovery APIs. No claim is made that every warning has been resolved or that Windows release validation passed.

Next: use VISUAL_ACCEPTANCE.md to capture the actual S013Cabin pistol low-ready, forward-ready, ADS, fire, reload, third-person front/side and walking/sprinting. There is no source change or failed gate requiring another automated run at this point. Production-level visual quality remains unapproved until the user reviews those captures.
