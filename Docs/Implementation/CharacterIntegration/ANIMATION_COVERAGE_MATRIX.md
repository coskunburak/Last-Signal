# Animation coverage

Every verification column is **NOT_RUN**. NEW/EXISTING means connected assets/code, not visual acceptance. PROC means explicitly procedural presentation; MISSING/BLOCKED are not hidden with unrelated clips.

Sources: H = `Assets/ThirdParty/Zombies/ZOMBİE ATTACK ANIMATION/Human Animations/Animations/Male`; C = `Assets/ThirdParty/Animation/CrouchAndSlide`; U = project-owned humanoid clips extracted from the locally imported UAL1/UAL2 libraries under `Assets/LastSignal/Art/CharacterIntegration/Clips`. Masks: UB = Character_UpperBody.mask; F = Character_Fingers.mask. B = Base Layer; A = Character Actions; G = Character Grip.

| Action | Actual source | State | Parameter/event | Gameplay source | Layer/mask | Rig dependency | Integration | Verification |
|---|---|---|---|---|---|---|---|---|
| Idle | H/Idles/HumanM@Idle01 | Standing Idle | HorizontalSpeed | Motor measured speed | B/none | Humanoid | EXISTING | NOT_RUN |
| Walk / low-speed forward | Retimed H/Movement/Run/HumanM@Run01_Forward | Standing Move | MoveX/Y, speed/rate | Motor velocity | B/none | Humanoid | PARTIAL dedicated walk: converted UAL clip disconnected after reversed foot-travel report | NOT_RUN |
| Run | H/Movement/Run/HumanM@Run01_* | Standing Move | MoveX/Y, speed/rate | Motor velocity | B/none | Humanoid | EXISTING | NOT_RUN |
| Sprint forward | Accelerated H/Movement/Run/HumanM@Run01_Forward | Standing Move | speed/rate, Sprinting | Motor.IsSprinting | B/none | Humanoid | Retimed run, separate UAL sprint pose disconnected after gait report | NOT_RUN |
| Strafe | H/Movement/Run left/right/diagonals | Standing Move | MoveX/Y | Motor local velocity | B/none | Humanoid | EXISTING; slowed run for walk | NOT_RUN |
| Backward | H/Movement/Run backward/diagonals | Standing Move | MoveY < 0 | Motor local velocity | B/none | Humanoid | EXISTING; slowed run for walk | NOT_RUN |
| Crouch idle/move | C/Idle Crouching, Walk Crouching * | Crouch Locomotion | IsCrouching, MoveX/Y | PlayerStance + velocity | B/none | Humanoid; mirrored directions | EXISTING | NOT_RUN |
| Crouch recovery | Existing stance transitions | Standing Idle/Move | IsCrouching false | Actual clearance/stance | B/none | None extra | EXISTING transition, no dedicated clip | NOT_RUN |
| Slide/recovery | C/Running Slide | Slide -> stance | IsSliding | Motor slide lifecycle | B/none | Humanoid | EXISTING | NOT_RUN |
| Jump | U/Jump_Start | Character Jump | !Grounded, VerticalVelocity > .1 | Motor vertical speed | B/none | Humanoid | BLOCKED gameplay action absent; state wired | NOT_RUN |
| Fall | U/Jump_Loop | Character Fall | !Grounded, vertical < .1 | Motor gravity | B/none | Humanoid | NEW | NOT_RUN |
| Land | U/Jump_Land | Character Land | Land edge trigger | Grounded edge; not crouch/slide | B/none | Humanoid | NEW | NOT_RUN |
| Weapon idle/grip | Final torso/arm solve + avatar-calibrated palms/finger closure | Locomotion then CharacterWeaponRig | profile/WeaponState | Active weapon definition | LateUpdate; G weight zero | RiflePose, StockContact, palm sockets | PROC; contact/deformation acceptance pending | NOT_RUN |
| Equip | Profile-based lowering/raising | No extra clip | StateTimer/Equipping | WeaponRuntimeState | Final pose | RiflePose | PROC | NOT_RUN |
| Unequip | Immediate visual removal/hide | No extra clip | selected/equipped state | Existing combat removal | visual lifecycle | Equipment profile | INTEGRATED; no delayed world stow clip | NOT_RUN |
| ADS | Shoulder-stock/profile interpolation | No extra clip | AimAmount | WeaponRuntimeState | Final pose | Stock/palm refs | PROC | NOT_RUN |
| Fire/recoil | Procedural weapon kick/recovery | No extra clip | ShotFired | Accepted WeaponController shot | Final pose | Profile recoil | PROC | NOT_RUN |
| Reload | Timed support-hand/magazine motion and partial finger release | No rifle reload clip | ReloadTime / StateTimer | Actual tactical/empty duration | Final pose | ReloadReference, magazine | PROC; authored rifle clip missing | NOT_RUN |
| Melee/recovery | H/Combat/1H/HumanM@Attack1H01_R | MeleeActive | MeleeActive, MeleeTime | MeleeAttackState timeline | A/UB | Crowbar visual/right hand | NEW | NOT_RUN |
| Pickup | U/PickUp_Table | Pickup | Pickup trigger | Accepted WorldItem interaction | A/UB | Humanoid | NEW; table-height reach, floor reach limited | NOT_RUN |
| Interaction | U/Interact | Interact | Interact trigger | InteractionPresented after success | A/UB | Humanoid | NEW | NOT_RUN |
| Consume/drink | U/Consume | Consume | Consume trigger | ItemConsumed Hydrate/Nourish | A/UB | Humanoid | NEW, no item prop spawning | NOT_RUN |
| Healing/bandage | No appropriate bandage clip | Treating signal only | Treating | ApplyingTreatment | FPS lowering, world weapon hidden/pose released | Shared survival binding | PARTIAL infrastructure; MISSING clip/hand gesture | NOT_RUN |
| Damage | H/Combat/HumanM@CombatDamage01 | Hit | Hit trigger | DamageAccepted | A/UB | Humanoid + FPS flinch | NEW | NOT_RUN |
| Death | H/Combat/HumanM@Death01 | Character Death | Dead | PlayerHealth.IsAlive | B/none | Humanoid; no ragdoll | NEW; seated death needs visual review | NOT_RUN |
| Vehicle entry/exit | No car-specific source | Seated/standing transitions | InVehicle | Committed vehicle ownership | B/none | Existing seat parent | PARTIAL; MISSING entry/exit clip | NOT_RUN |
| Seated idle | U/Driving_Loop (Sitting_Idle_Loop also extracted, unused) | Character Seated | Seated | InVehicle | B/none | Pelvis aligned to seat | NEW | NOT_RUN |
| Steering | Driving clip + actual wheel hand targets | Character Seated | Actual wheel transform | VehicleSteeringWheelPresenter | B IK/none | Wheel pivot/reach | PROC, visual tuning pending | NOT_RUN |

FPS continues its existing VAL draw/idle/shoot/tactical reload/empty reload/hide/sprint and crowbar controller. World cosmetic actions additionally lower/flinch the existing WeaponParent; they do not replace the FPS rigs. No 100% animation coverage claim is made.
