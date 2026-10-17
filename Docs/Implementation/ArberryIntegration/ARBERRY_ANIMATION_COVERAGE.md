# Animation and lifecycle coverage

The existing WorldBody.controller and its parameter/mask/layer contracts are unchanged. Existing clips retarget through Arberry's own Avatar. No clip was invented or downloaded. All runtime results below are **NOT_RUN**.

| Action | Implementation / limitation |
|---|---|
| Idle | Existing standing Humanoid idle |
| Forward/back/left/right/diagonal | Existing coherent HumanM eight-way locomotion tree |
| Walk/run/sprint | Existing positive measured-speed cadence; dedicated slow-walk/sprint gait quality remains limited by reused run family |
| Crouch idle/move/recovery | Existing crouch states driven by PlayerStance/motor |
| Slide/recovery | Existing Running Slide and authoritative stance state |
| Fall/landing | Existing Jump_Loop / Jump_Land retargeting |
| Jump | No gameplay jump introduced; upward-motion infrastructure only |
| Equipped move/ADS/recoil | Existing final cosmetic arm solve with own profiles |
| Rifle/pistol reload | Existing authoritative clock + hand/magazine motion; dedicated rifle reload clip MISSING |
| Axe/machete/crowbar attacks | Existing melee clock and layers, own profile target trajectories; no new damage events |
| Damage/death | Existing Hit/Death clips and PlayerHealth notifications |
| Seated/steering | Existing Driving_Loop, actual pelvis and steering-wheel IK |
| Entry/exit | Pose switching infrastructure; authored vehicle entry/exit clips MISSING |
| Interaction | Existing Interact clip on accepted interaction |
| Pickup | Existing PickUp_Table; dedicated floor pickup MISSING |
| Consume | Existing Consume clip; no new item prop system |
| Treatment | Existing weapon suppression/FPS lowering; authored bandage/hand gesture MISSING |
| Unequip/stow | Existing visibility/cancellation; authored full world stow clip MISSING |

Existing PlayerLocomotionPresenter remains the single parameter writer. FirstPersonMotor owns movement and gravity; Animator root motion cannot move the Player. Existing action ownership, death/reset, survival rebinding, seat/exit and pause cleanup are reused. New sessions use the existing SessionFlow's Player prefab, so no second spawning/ownership system was added. Save/load schema is unchanged; the active local presentation comes from the prefab, while weapon state and cosmetic pistol IDs retain existing persistence ownership. Actual session restoration and seated death remain user-operated acceptance items.
