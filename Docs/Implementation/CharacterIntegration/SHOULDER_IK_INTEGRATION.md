# Shoulder and weapon integration — revised after 12:00 feedback

Implementation changed; compilation, focused tests and visual acceptance for this revision are **NOT_RUN**. Earlier PASS results do not validate this revision. No package, second skeleton, vendor asset change or gameplay authority was added.

## Measured cause

Static inspection of the MRPoly rifle FBX geometry found that the old right-hand socket was near the vertical foregrip and the old support socket was near the barrel. The pistol grip is behind the magazine. Placing the weapon by its arbitrary mesh origin, then translating it backwards to satisfy arm reach, allowed its stock to penetrate the torso. Raw ±90-degree IK goal rotations also did not account for Diesel's actual wrist/palm axes, and the generic pistol finger layer did not produce a reliable closed grip.

The owned rifle now has these mesh-local contacts (metres, before cosmetic scale):

| Reference | Position | Meaning |
|---|---|---|
| StockContact | (0.060, 0.190, -0.374) | Rear stock surface; weapon placement anchor |
| RightGrip | (0.079, 0.092, -0.140) | Palm on the pistol-grip right surface |
| LeftGrip | (0.032, 0.105, 0.100) | Palm on the vertical foregrip left surface |
| ReloadReference | (0.035, 0.045, -0.008) | Support palm near the magazine |

Grip frame +Z points along extended fingers; +Y points from the palm into the grip. These are palm contacts, not wrist-bone targets. Existing ADS/muzzle/magazine references remain cosmetic; gameplay raycasts, firing and ammunition stay FPS/combat-owned.

## Evaluation and pose ownership

1. PlayerLocomotionPresenter writes the preserved locomotion/action controller parameters. The idle action layer is muted; the old Character Grip layer remains present but its weight stays zero.
2. CharacterWeaponRig.Update restores its last-frame local bone rotations and finger overrides. It follows the existing combat slot/definition and accepted shot event. Animator then evaluates the animation pose.
3. CharacterWeaponRig.LateUpdate (execution order 300) applies a 35-degree chest stance and bounded look pitch, counter-rotates the head toward gameplay aim, and places StockContact at a shoulder-relative pocket. Cosmetic rifle scale is 0.85. Hip pocket offset is (-0.02,-0.035,0.08); ADS is (-0.06,0.06,0.08), relative to the right upper-arm origin in player axes. The visual does not move the player/capsule or camera.
4. CharacterHandPose derives each wrist-to-palm transform from actual hand/index/middle/little landmarks. It converts palm contacts to wrist position/rotation with the imported skeleton scale accounted for.
5. A closed-form two-bone arm solve places elbows along anatomical hints with reach limits, then aligns calibrated wrists. The gun is never pulled backwards through the torso to satisfy a hand. Extreme unreachable poses clamp at arm reach and must be reported as contact failures in QA.
6. Fifteen finger joints per hand receive bind-relative flexion around geometry-derived axes, with thumb opposition and a separate trigger-index pose. Recoil adds a small trigger curl. Finger overrides restore before the next Animator evaluation and on disable, avoiding accumulation in pause or after switching actions.

The firearm solve occurs after all Animator layers, so imported muscle curves cannot overwrite the final wrist/finger pose. The existing Base Layer IK callback only clears on-foot Humanoid hand/hint weights; seated IK remains exclusively owned by CharacterSeatPresentation. There is no competing firearm Humanoid solver or new rig constraint component.

## Actions and lifecycle

Hip/ADS, bounded pitch, sprint/slide lowering, equip/unequip and recoil read existing authoritative state. Pose entry blends over 0.15 seconds. Reload samples the real tactical/empty timer; the support palm moves toward the magazine reference, opens partially and returns. This is procedural presentation, not an authored full-body reload clip.

Interaction, damage, treatment, death and seating release firearm pose ownership; the cosmetic rifle is hidden while a conflicting action owns the arms. Accepted equip/reload/fire interrupts cosmetic gestures through the existing presenter. Melee continues using the existing attack clip/timeline and hand-following world crowbar, with right-finger closure only. Vehicle hands retain their existing bounded Humanoid IK; no firearm torso/finger pass runs while seated.

Equipment replacement clears visual/profile/socket references and shot subscription; cached renderer visibility refreshes on replacement. Disable/teardown restores cached bone rotations and fingers, then removes the cosmetic weapon. No per-frame material allocation, prefab instantiation, hierarchy traversal or physics simulation is added by the pose solver.

## Authored verification, not executed

EditMode tests now check real stock/pistol/foregrip geometry, anatomical palm calibration and both hip/ADS arm reach with the shoulder stance. PlayMode checks sample actual camera-render boundaries after LateUpdate for wrist contact, stock contact, post-hit recovery and absence of rotation accumulation while paused. Existing equip/reload/melee/vehicle/lifecycle suites remain applicable. None was run by the agent.

## Visual acceptance required

Restart Play in S013Cabin, turn Gizmos off and inspect both sides, front and rear. Reject stock-through-back, wrist roll, open/spread fingers, finger penetration, excessive elbow flare or jacket collapse. Repeat hip/ADS, look up/down, crouch, sprint, reload, fire, melee switch, interaction, treatment, death, pause and vehicle entry/exit. Model proportions and extreme pitch still require human acceptance. Dedicated bandage, vehicle entry/exit and authored rifle reload clips remain absent; no complete-animation or production-ready PASS is claimed.
