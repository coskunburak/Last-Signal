# Animation matrix

All three reuse the existing project-owned `Assets/LastSignal/Animations/Weapons/Grip/PistolGrip.controller` for the neutral VAL arm layer (one Idle state). WeaponAnimationPresenter remains disabled on these prefabs; action coverage below is procedural, not missing Animator state names. No new retargeted AnimationClip was claimed or produced. DXT's nine vendor Generic clips are audited and preserved, but their weapon bone paths are not attached to the unrelated FPS arm skeleton.

| State | DXT / Glock17 / Custom9mm implementation |
|---|---|
| Idle / low-ready / raised-ready | Existing profile-driven view + arm IK; per-model profiles |
| ADS / return | Individual AimReference geometry; WeaponViewPresenter alignment |
| Equip | Profile lowering/recovery over authoritative equip time |
| Unequip | Procedural lowering; player FirearmSlot changes queue until authoritative holster, then swap/equip |
| Accepted fire / recoil / recovery | Existing authoritative shot event, view/root recoil + PistolMechanics |
| Trigger | Independently authored real trigger pivot, bounded rotation |
| Slide | Real geometry, individual travel .031/.028/.027 m |
| Hammer | DXT only; no invented hammer on other sources |
| Last-round slide lock | DEFERRED: round-pool combat has no authoritative chamber/slide-lock policy |
| Tactical / empty reload | PistolReloadPose samples actual timer/commit; actual magazine extracted/reinserted, left-hand assist; empty reload adds slide handling |
| Reload cancel | Combat cancels; restored magazine/rest transforms and old visual cleanup |
| Walk / sprint / crouch / slide | Existing bob, stance offsets, profile lowering and world locomotion/IK |
| Switch / disable / death / vehicle / teardown | Existing state and visibility ownership; new mechanics unsubscribe/reset |

All animation work is procedural or reused existing controller content. No animation commits ammo/damage. Reload audio uses the existing guarded clock/commit cues; shared pistol fire/equip/handling assets are retained; dry-fire fallback references the existing S015 DryFire cue clip. The S015 catalog remains unchanged. No three distinct gunshot recordings are claimed. Existing muzzle flash, bounded casing/tracer/impact pools and gameplay noise owner are preserved; gore is untouched.

Player-driven pistol cycling includes outgoing unequip. Direct Select APIs used by save restoration and explicit forced equipment changes remain immediate. Visual timing, finger motion and magazine/left-hand contact remain user acceptance items for every model.
