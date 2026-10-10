# Manual character acceptance — NOT_RUN

User execution only. Production scene: `Assets/LastSignal/Scenes/Production/S013Cabin.unity`. Actual player: `Assets/LastSignal/Prefabs/Player/Player.prefab`; body: `Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldBody.prefab`. Vehicle fixture for repeatable seat cases: `Assets/LastSignal/Scenes/Validation/VehiclePlayableAcceptance.unity` (use the production scene for the primary visual acceptance).

Open the production scene and enter a session through the existing flow. Focus Game view. F6 toggles inspection; F7 cycles Front → Back → Left → Right → Three-quarter. These keys are enabled in Editor/Development builds; macOS function-key settings may require Fn. Gameplay keeps FPS-relative movement/aim while observed externally. Existing gameplay bindings can be customized: use the active Controls configuration for Move, Sprint, Crouch, Fire, Aim, Reload, MeleeSlot, FirearmSlot, Interact and Inventory. There is no new Jump binding. Use the existing sprint/crouch slide mechanic; do not add a jump impulse to manufacture coverage.

Create your own uniquely named `Evidence/<timestamp>-manual/` folder, record Unity version, scene, build/editor, device, active control overrides, case ID, observed result and screenshot/video path. Every Observed entry below starts NOT_RUN; replace it only after personally checking. Capture actual frames at front/back/left/right/three-quarter, ADS, reload, crouch, slide, melee and seated steering.

| ID | User action | Expected / acceptance question | Observed |
|---|---|---|---|
| V01 | Inspect idle from all five angles | One Diesel body, nine intended skin parts, no alternative bodies/studio props, no missing/pink materials | NOT_RUN |
| V02 | Inspect feet, ground, clothing and shoulders | Appropriate player scale/ground alignment; no severe foot penetration, jacket collapse or broken clavicle | NOT_RUN |
| V03 | Compare FPS and world materials | Matte Diesel palette; FPS tint coherent, existing FPS texture/normal detail retained; Diesel is not textured-skin delivery | NOT_RUN |
| M01 | Idle, move slowly with supported analog input, normal movement, sprint | One coherent directional run family changes cadence with actual displacement; no backward planted-foot travel during forward movement. Low-speed/sprint are retimed run poses, not dedicated walk/sprint clips | NOT_RUN |
| M02 | Strafe, backward, diagonals, rotate while moving | Direction remains correct; retimed directional runs acceptable at low speed | NOT_RUN |
| M03 | Crouch idle/move; stand under low ceiling | Pose follows actual stance/clearance; no forced stand through obstruction | NOT_RUN |
| M04 | Start/complete slide, then crouch/stand | No stuck slide, feet/hips credible, return matches gameplay stance | NOT_RUN |
| M05 | Walk off a safe ledge and land | Fall/land observed from actual gravity; grounded recovery, no world-root motion | NOT_RUN |
| M06 | Jump action | BLOCKED: current gameplay has no jump action. Do not mark wired jump clip as playable coverage | NOT_RUN |
| C01 | Equip rifle; unequip/replace through existing equipment flow | Single cosmetic world weapon, no stale target or duplicate mesh; removal follows existing immediate unequip | NOT_RUN |
| C02 | Idle/ADS from front, both sides and back; look high/low, turn, crouch, sprint | Stock stays at right shoulder, never projects through the torso/back; right palm encloses pistol grip and left palm encloses vertical foregrip; fingers close without crossing the grip; no wrist twist/shoulder collapse; FPS aiming unaffected | NOT_RUN |
| C02b | Pause at settled idle and ADS, resume repeatedly | Torso, wrists and fingers do not accumulate offsets or snap to an open hand; both hands regain contact after transitions | NOT_RUN |
| C03 | Fire aimed/hip shots | One authoritative shot/ammo change; external recoil synchronized; no cosmetic duplicate damage | NOT_RUN |
| C04 | Tactical and empty reload, pause/resume; interrupt with slot switch/death, then re-equip | Existing gameplay ammo/timing preserved; support hand releases toward magazine and returns to foregrip; right palm keeps pistol grip; no retained magazine offset or stale finger pose; no claimed authored reload clip | NOT_RUN |
| C05 | Select crowbar, attack/recover, switch back to rifle | Crowbar scale/orientation correct, melee follows existing timing, no stale rifle hand lock | NOT_RUN |
| C06 | Interact/pick up, then equip/fire/reload | Accepted action only; combat interrupts cosmetic gesture without stuck arm pose; table-height pickup limitation visible | NOT_RUN |
| S01 | Drink/eat when needed, repeat after session load | Consume gesture once per successful use; no gesture for rejected/full-resource use | NOT_RUN |
| S02 | Take damage, begin/cancel/complete bandage | Hit response and Treating/lowering work; dedicated bandage gesture remains MISSING | NOT_RUN |
| F01 | F6 back to FPS while equipped with rifle and crowbar | FPS arms visible; no head/torso occlusion; no unexpected shoulder clipping | NOT_RUN |
| F02 | Inspect both weapons from all angles | No layer-29 rifle or layer-2 crowbar floating FPS arms; Diesel and world weapon visible; shadows sensible | NOT_RUN |
| F02b | During Play, use Scene view and frame the spawned Player/WorldBody; turn Gizmos off | Whole animated Diesel visible without F6, no floating FPS-only arms; returning to Game shows normal local FPS arms | NOT_RUN |
| F02c | Observe two player instances from each camera and an external observer | Each camera hides only its own body; sees the other full body and world weapon; only its own FPS arms render. This is local rendering acceptance, not network replication acceptance | NOT_RUN |
| F03 | Toggle F6 repeatedly, cycle F7, pause/resume | Original camera restored, no extra AudioListener, controls remain governed by gameplay pause | NOT_RUN |
| L01 | New session, return to menu, new session/load | One body/weapon/camera; no stale triggers, missing-reference errors or retained presentation objects | NOT_RUN |
| L02 | Die equipped/on foot, then start a new session | Death pose, hidden world weapon, no moving capsule from animation; fresh alive presentation | NOT_RUN |
| L03 | End session while inspecting/reloading/treating | Inspection destroyed with player, subscriptions cleaned, next session normal | NOT_RUN |
| V04 | Enter vehicle, steer left/right, pause/resume | Committed seated pose persists while paused; hips align with seat, hands reach actual wheel without extreme extension | NOT_RUN |
| V05 | F6/F7 while seated, return to cockpit, exit safely | Correct camera restored; body local offset restored on exit; no stuck seating or control ownership | NOT_RUN |
| V06 | Vehicle death / exit then weapon selection | Visually acceptable seated death; on-foot equipment and locomotion recover correctly | NOT_RUN |
| V07 | Optional existing development vehicle diagnostic (-veh001Inspect), keys 8/9, alongside F6 | Only one inspection camera owns view; disable current owner before switching; no duplicate arms, correct restoration | NOT_RUN |
| P01 | Observe representative movement/combat/vehicle use with Unity Profiler | Record allocations/frame cost if acceptance requires it; no per-frame weapon creation/material copies expected; no unmeasured performance PASS | NOT_RUN |

Entry/exit is the existing instantaneous vehicle transition; dedicated car-entry/car-exit clips are MISSING. Steering, shoulder deformation, finger contact, death in a seat and material appearance require human visual judgement regardless of automated test results. Record failures with angle, action, weapon and exact failing frame; tune owned profiles/offsets before changing source rigs.
