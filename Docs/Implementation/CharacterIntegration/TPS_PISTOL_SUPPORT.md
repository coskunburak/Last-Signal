# TPS pistol support integration

Implemented 2026-10-14. Tests and builds NOT_RUN by user request. Test sources were not changed.

CharacterWeaponRig owns the final TPS arm pose after Animator. The right hand attaches the visual first; the left hand then solves to a rigid root-child PistolSupport contact. No RigBuilder or second arm solver was added. FPS contacts and source weapon prefabs are unchanged.

Arberry Pistol, DXT, Glock17 and Custom9mm profiles now reference dedicated TPS_*_TwoHand prefabs. Each support contact is independent of the magazine and slide. The existing profile authoring command preserves the new world prefab, support enable flag and secondary finger calibration.

Ready/reloading supports the weapon, with the existing reload sample transitioning to ReloadReference. Sprint/slide reduce support. Equip/unequip fade it, and existing hide/death/switch/disable paths reset cached weights. Missing support configuration falls back to reload-only and warns. Left contact diagnostics now include supported pistols. Support is cosmetic and has no ammo, damage or firing authority.

## Calibration status

Unity confirmed the Arberry Avatar is valid and Humanoid during prefab authoring. The right knuckle span measured 0.07015558 m. Initial support contacts use 75% of that span across the primary palm normal, 20% back along its finger axis, with opposing palm normals and matching finger direction. These are anatomical starting placements, NOT visually approved per-weapon contact poses. WorldScale is compensated during authoring. Support finger closure remains the profile value (initial default 0.65).

Visual acceptance of the four variants, finger intersection, elbow reach, reload trajectory, ADS and movement transitions remains pending. No PlayMode or automated tests were run. Do not interpret compilation or serialized references as proof of final visual quality.

## User verification

Open S013Cabin, equip a pistol and use digit 6 for inspection, digit 7 for angles. Select runtime WorldBody; enable Draw selected pose contacts. Inspect Support hand weight, Palm error R/L, Unreachable distance R/L and Jacket penetration. Calibrate only the dedicated TPS prefab PistolSupport local transform, preserving PrimaryGrip. Socket +Z follows fingers and +Y points into contact. Profiles contain secondary finger flexion and World Support Finger Closure. Capture front/side/top views and reload video.

Existing WeaponGripCorrectionPlayTests and TriplePistolPlayTests still contain the old idle-world-support-zero contract. Those assertions require a separate authorized test update before the new two-handed contract can pass; sprint/slide release assertions remain relevant.

## Live calibration follow-up

Live DXT inspection confirmed the original support hint was below the shoulder, but torso clearance rotated the solved elbow above it: left shoulder y=1.39, elbow y=1.48, hint y=0.91 (actor space). The raw downward solution intersected the torso envelope by about 66 mm. Contact error alone concealed this failure.

The four Arberry TPS pistol profiles now use centered forward offsets: hip (-0.12,-0.14,0.54), raised (-0.12,-0.10,0.54), aim (-0.12,-0.035,0.54), in actor metres relative to the right shoulder. FPS fields and primary weapon sockets are unchanged. Regeneration preserves these calibrated offsets.

Pistol-only clearance now searches the elbow swivel circle with an anatomical height bound instead of escaping above the shoulder. Wrist and segment lengths remain unchanged; penetration diagnostics remain active. Other weapon types retain the existing clearance algorithm.

DXT was inspected live in the running scene, including a fresh Play session after script import. The front silhouette no longer has the crossed raised elbow. Sampled reach residuals were zero; palm errors approximately 0 / 0.04 mm. The padded jacket envelope still reported approximately 14 mm right / 4.5 mm left penetration near the proximal upper arm (not the wrist). This remains an unresolved clearance/visual calibration item; no production acceptance is claimed. Other variants inherited the centered stance but have not received equivalent live visual acceptance. No automated tests or build were run; test sources unchanged.

## 2026-10-15 — jitter correction and FPS integration (supersedes FPS exclusions above)

The later user request authorized implementation in both FPS and TPS, with automated tests excluded. The previous full-circle, five-degree clearance search ran AFTER the IK bend limiter. It could therefore bypass the temporal limit, change swivel branch and make the elbows jump. The replacement uses a bounded ±75-degree search around the authored anatomical hint, a smooth penalty for leaving that plane, local continuous refinement, and a final exponential/rate-limited swivel AFTER clearance. Wrist endpoints and both bone lengths remain unchanged. Arberry pistol elbow speed is 120 degrees/second. Non-pistol clearance is unchanged.

FPS now has its own `FpsTwoHandedPistol` flag and primary/support palm calibration, independent of TPS sockets. Both hands follow the weapon-owned sight frame. The support hand blends from its rest position into the grip, follows the existing reload reference, releases for sprint/slide/equip/unequip, and resets on visual changes/disable. Support fingers use secondary flexion rather than the old free-hand curl. Contact errors report the calibrated palm targets, including the support palm. The FPS Inspector draws these targets and shows support weight/reach.

Saved in Arberry Pistol, DXT, Glock17 and Custom9mm profiles:

- FPS primary palm offset `(-0.018, 0, 0)` in primary socket axes/metres. For these sockets local X points upward, so this lowers the hands 18 mm while preserving the gun's sight transform.
- FPS support offset `(0, 0.045, -0.014)` from the corrected primary palm, rotation `(0, 0, 180)`, closure 0.65.
- FPS shoulders `(±0.20, -0.30, -0.09)`, elbow hints `(±0.31, -0.48, 0.12)` in camera metres.
- FPS hip `(0.075, -0.145, 0.34)`, raised `(0.06, -0.12, 0.35)`.
- TPS elbow hints `(±0.29, -0.34, 0.16)`; centered grip positions from the preceding iteration are retained.

Both Arberry authoring paths preserve calibrated profiles. FPS regeneration skips its old automatic one-hand translation fit for an explicitly calibrated two-hand pistol. No shared source weapon prefab, Animator, Avatar, scene save or combat authority change is part of this correction.

### Live authoring observations

These are editor observations, not automated test passes. Evidence: `Evidence/20261015-pistol-live-authoring/observations.json` and adjacent PNGs.

- DXT idle: 120 distinct active frames, excluding the first 10, maximum elbow displacement between samples reduced from 11.221 mm to 0.270 mm. Frame rates were not locked, so this is not a benchmark or a guarantee over movement/reload.
- Final Glock17 ADS: contact errors 0.0064 / 0.0092 mm; reach residuals 0 / 0. The lowered cuffs no longer cover the rear of the slide as in the preceding capture.
- Final Custom9mm ready: contact errors 0.0153 / 0.0160 mm; FPS and TPS reach residuals 0 / 0.
- DXT TPS front/right/left inspected: elbows stay below the shoulders in the observed ready stance. The padded torso-envelope diagnostic still reports about 27 / 23 mm near the proximal upper arms. The envelope was NOT shrunk to conceal this. This remains an unresolved clearance diagnostic; no zero-intersection claim is made.
- Unity scripts compiled and the inspected console had no errors. Automated tests/builds were not run; test sources were not changed by this correction.

Movement, extreme pitch, reload trajectories, per-finger intersections and all state transitions have not received full acceptance. Legacy Pistol received serialized configuration validation but no separate final live visual capture. Existing tests expecting a zero-weight pistol support hand need an explicitly authorized contract update before the new behavior can satisfy those assertions. Do not call this a completed production acceptance gate.
