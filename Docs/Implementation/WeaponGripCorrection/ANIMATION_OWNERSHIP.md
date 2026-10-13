# Animation ownership
| Representation | Base owner | Final arm/weapon owner | Time source |
|---|---|---|---|
| Pistol FPS | Existing Generic Animator with owned idle clip | FirstPersonWeaponGrip; WeaponViewPresenter supplies root ADS/recoil | WeaponRuntimeState / accepted ShotFired |
| Machete/axe FPS | Existing Generic Animator with neutral clip | FirstPersonWeaponGrip profile trajectory; no crowbar Swing | MeleeAttackState.PresentationProgress |
| S021 world | Existing Diesel locomotion/action layers | CharacterWeaponRig single-hand final arm pass | Same combat state/definitions |
| Rifle FPS/world | Existing architecture | Existing rifle animation / stock and both-arm solve | Unchanged |
| Crowbar FPS/world | Existing architecture | Existing crowbar Animator / legacy hand attachment | Unchanged |
| Seated Diesel | Existing seating Animator/IK | Vehicle seating owner; weapon rig releases | Existing vehicle state |

No second Animator was added. Profile arm targets are independent of the hand; the resolved hand then owns the visual. There is no recursive hand-to-weapon-to-hand target feedback. Procedural rotations/positions are restored before Animator evaluation. No delayed callbacks were added. New owned clips have no animation events.

Magazine authority remains WeaponRuntimeState, including interrupted reloads. Cosmetic magazine/left-arm interpolation makes no ammunition transaction. Procedural grip work does not claim bespoke mocap equip/reload/slide-rack quality.
