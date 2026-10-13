# Melee grip overhaul — 2026-10-13

Status: user-operated functional verification passed after the slide correction; visual acceptance pending.

## Current-local findings

- Unity version is **6000.5.0f1 (88b47c5e7076)**. Branch `s012-integrated-graybox-slice`, starting HEAD `c41dcf1a0e0942377556227e938ef847391623bb`.
- Extensive pre-existing dirty/untracked work includes three-pistol integration. The implementation was based on current files, not HEAD. Targeted before-images and full initial porcelain status were saved in `/private/tmp/lastsignal-melee-before` (temporary recovery aid, not committed evidence).
- Axe profile was `OneHandedHeavyMelee` (3). Both axe prefabs had no `LeftGrip`. FPS always solved the left arm toward `FpsOffhandPosition`; Diesel entered `ApplySingleHandPose`. Thus the current defect was explicit single-hand ownership, not just the older legacy right-hand attachment branch.
- Both melee weapons already used generic FPS bones, calibrated palms, bind-relative fingers and profile-driven attack timing. These were reused. The world action layer could still play the old crowbar animation beneath those procedural targets. It now suppresses that melee clip only for profile-owned melee; crowbar and firearm animation paths remain available.
- `weapon.blade` resolves to **Machete_Viewmodel**, not a short knife. It remains the approved source model and gameplay ID. No new knife model or attack mechanic was invented.
- The attachment directory contained only the request text. The referenced VEIN screenshot was unavailable. Composition follows the written upper-right head / diagonal shaft description; direct image matching is pending.

## Geometry and persistent contacts

Unity imported mesh inspection (asset inspection, not a verification gate) found unit mesh scale, axe bounds approximately centre `(0,-.07,.10)`, extents `(.02,.11,.32)`, and machete centre `(0,-.01,.16)`, extents `(.02,.03,.38)`. Axe shaft follows local +Z; its head is near +.34 Z; butt near -.21 Z. Cross sections around Z +.08 and -.12 have about .01 m X and .02 m Y half-widths. The shaft accommodates 20 cm axial hand spacing without rescaling or changing source geometry. Blender inspection crashed on startup; Unity imported geometry was used instead. No vendor file was edited.

| Contact | Model-local position, metres | Palm axes |
|---|---|---|
| Axe PrimaryGrip_R | `(.011,.002,.08)` | +Z fingers toward model -Y; +Y into shaft toward model -X |
| Axe SecondaryGrip_L | `(-.011,.002,-.12)` | +Z fingers toward model -Y; +Y into shaft toward model +X |
| Machete PrimaryGrip_R | `(.017,-.004,-.12)` | +Z fingers toward model -Y; +Y into handle toward model -X |

`BladeReference` reuses the former silhouette Transform identity. `WeaponVisualRoot` is the sockets' visual root. Existing impact references/gameplay origins remain untouched. These are palmar surface contacts; `CharacterHandPose` derives wrist targets using actual skeletal landmarks and skin bind rotations. Fingers use existing anatomical flex axes. Axe left finger and thumb angles/opposition are independently serialized; final finger/mesh penetration still requires visual calibration.

## Ownership and evaluation

1. Existing combat simulation owns windup, active window, recovery, stamina and hit resolution. No damage events or values were changed.
2. Procedural bones/fingers restore in Update before Animator evaluation. `PlayerLocomotionPresenter` (100) retains locomotion but disables legacy melee clip selection for profile-owned axe/blade.
3. FPS stance (120) moves the shared presentation frame; shoulders stay camera-relative. FPS grip (350) reads actual simulation progress, places the axe root once, converts **both** fixed contacts to wrists, then solves arms and fingers. Neither solved axe wrist moves the root afterward. This avoids a wrist/weapon feedback loop.
4. Diesel rig (300) adds bounded torso preparation/look pitch, places a shoulder-relative axe root, then fits the single axe root within 10 cm against both measured arm reaches and solves both wrists with existing elbow hints, clavicle assistance and forearm twist. The left hand never uses the firearm reload path for an axe.
5. Blade uses the retained one-hand solver: requested right wrist is solved, visual follows that resolved palm. FPS left arm uses independent relaxed targets; Diesel left arm remains locomotion-owned. Weapon switch/disable restores fingers, bones, bend histories and contact state.
6. Firearm enum values are unchanged; `TwoHandedHeavyMelee` is appended as value 5. Pistol finger assets, reload assistance, sight, recoil and rifle stock solver remain separate paths.

Axe authored ready values: FPS primary contact `(.14,-.17,.29)` and palm Euler `(20,-8,-28)` before the existing palm-frame correction; Diesel primary offset `(-.04,-.17,.32)` relative to the right shoulder. No pixel coordinates, camera rays or camera collision changes. Sprint rotation is reduced to keep the long handle controlled. Crouch, slide and equip lower the connected pose.

## Animation and lifecycle boundaries

No new `.anim` clips were generated. Existing `WoodenAxeGrip` / `MacheteGrip` controllers and neutral idle clips are reused. The heavy windup, acceleration, follow-through and recovery were procedurally recalibrated in the axe profile using the existing `AttackPose` / `SampleAttack` timeline. Knife/machete light strike remains its separate existing profile. Paused state freezes cosmetic interpolation; cancellation returns via existing recovery smoothing when time resumes.

Equip uses connected lowering. The combat controller immediately deactivates an outgoing weapon: unequip currently releases constraints instantly, **not a new animated holster**. Death, treatment and vehicle suppression retain existing authorities. No new stun state was added. Action-specific treatment/inventory/vehicle visual acceptance remains required.

## Diagnostics and limits

20 mm is the initial palm-contact and requested-wrist reach tolerance. FPS exposes both errors and `HasValidContact`; Diesel exposes both errors/residuals and `HasValidMeleeContact`. The existing solver never stretches bones. An unreachable axe target is kept as a visible diagnostic failure instead of reattaching the root to a clamped right wrist. FPS Inspector marks invalid two-hand poses; both editor inspectors show contacts and reach.

The first two user runs exposed world-left contact failures. A shared-root reach correction and slide-specific lowering fix followed. The final user run passed compile, 36/36 focused EditMode, 3/3 focused PlayMode, 31/31 character, 33/33 combat, 779/779 full EditMode, 430/430 full PlayMode and the development build. No rendered pose, performance measurement or production-quality visual PASS is claimed. Shoulder reach at extreme pitch, wrist roll, finger skin penetration, camera clipping, proportions and the actual VEIN composition need user-run tests and screenshots. The source machete remains longer than a short knife.
