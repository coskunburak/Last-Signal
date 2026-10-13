# World-body synchronization
The previous one-hand CharacterWeaponRig branch is retained. It uses the same profile and actual weapon selection as FPS. New raised-ready weight is driven by accepted ShotFired, with the same hold/blend duration and bounded pause behavior. Hip/raised/ADS shoulder-relative targets remain distinct from camera-space FPS targets.

The free left arm remains locomotion-owned; only the explicit reload interval enables support weight. New per-joint flexion also applies to the Diesel right hand. Melee now uses the same SampleAttack cancellation/recovery rule as FPS, eliminating the prior world-body snap when the attack became Ready.

Existing final arm IK, reach clamping, reversible bone changes, vehicle ownership, body visibility and action/locomotion controllers are preserved. No new world animation controller or mesh was created. The six S021 production prefabs and three shared profiles persist the revised authoring; existing Player/WorldBody references resolve them without scene regeneration.

User must still inspect front, side, rear and three-quarter pistol/axe/machete poses, particularly clavicle/sleeve deformation, extreme pitch and reload return. Numerical contact is not visual acceptance.
