# Ownership, anatomy and animation

FPS remains weapon-target-driven. Update restores prior procedural overrides; the existing Generic Animator supplies the underlying arm pose. WeaponViewPresenter supplies low/raised-ready, ADS, movement and bounded accepted-shot recoil. FirstPersonWeaponGrip solves the right arm once, then attaches the visual to the solved palm. The attached result is not fed back into the next target. Shoulder positions, forearm twist limits and arm lengths remain under the existing bounded solver.

PistolFingerPose stores 15 neutral-bind-relative local quaternion offsets, ordered thumb/index/middle/ring/little with three joints each. A second set contains the accepted-shot index pose. Authoring uses the actual neutral phalanx directions and measured pistol geometry to produce distinct thumb and finger arcs. Runtime applies these local offsets, not world-space bone rotations. Repeated application restores the previous snapshot; unequip/disable restores originals. FPS and Diesel have separate assets and calibrations. Rifle and melee retain their prior finger path.

The grip frame uses +Z along the open fingers and +Y into the handle. Model axes remain +Z barrel, +Y slide, +X right side. Contact is on the right side of the handle, not at its origin. Contacts in model metres:

| Rig | Palm contact | Calibration asset |
|---|---|---|
| VAL Generic FPS | (0.061, 0.056, -0.065) | PistolFingers_FPS.asset |
| Diesel Humanoid | (0.061, 0.068, -0.075) | PistolFingers_Diesel.asset |

The FPS hand has 15 functional finger joints. Arms, fingerless gloves and Gorka sleeve share 75-bone skins. The imported wrist hierarchy has approximately 100× scale; existing palm conversion accounts for that scale. No second skeleton, vendor FBX edit or skin-weight replacement was necessary. Diesel's larger hand receives separate offsets, including a different thumb metacarpal direction.

Low-ready and raised-ready remain profile-driven procedural poses. Low-ready heading is (5,-8,-3) degrees and raised-ready (1,-3,-2), with existing profile translations retained. The editor preview was used to inspect the inward barrel angle and lower-right composition; real gameplay FOV/aspect acceptance remains pending. ADS continues to align the actual sight reference. Existing shot/ammo/hit authority is unchanged.

PistolReloadPose samples the actual tactical/empty duration and commit fraction. The left hand reaches during the first 30% of the pre-commit interval; extraction peaks at 60%; insertion finishes at commit. Once ReloadCommitted is true, the magazine is seated and the offhand releases. Right-arm presentation adds a small shared lift/roll during assistance. Cancellation, switching, death and disable restore the magazine and hand overrides. This is a procedural approximation, not a cinematic reload clip. It never commits ammunition.

World-body uses the same authoritative reload sampling, accepted-shot ready/recoil state and separate Diesel finger calibration. The normal left arm stays locomotion-owned. StockContact and SecondaryGrip remain absent. Existing visibility, treatment/death and vehicle seat ownership are preserved.

Existing owned PistolGrip.controller/PistolGripIdle.anim remain the neutral base. No new animation clip or new mesh is claimed; the newly authored animation data is the two finger-pose assets plus procedural reload/ready composition.
