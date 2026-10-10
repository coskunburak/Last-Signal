# FPS / world-body synchronization

Implementation status: COMPLETE for shared-state presentation; runtime and visual verification: **NOT_RUN**.

The existing FPS rifle and crowbar Animators are retained. `WorldBody.controller` is extended in place. These are separate rigs driven by the same existing movement, combat, interaction, health and survival authorities; world clips do not execute gameplay effects.

The revised firearm solve runs in CharacterWeaponRig.LateUpdate after Animator evaluation. It owns only cosmetic torso, arm and finger rotations and the world weapon. CharacterHandPose calibrates each wrist from the actual palm geometry. Previous procedural rotations restore before the next Animator evaluation; the old generic finger layer stays at zero weight. Seated hand IK remains owned by CharacterSeatPresentation. FPS aim, recoil, animation and gameplay state remain independent authorities.

| Signal | Authority | World presentation | FPS presentation |
|---|---|---|---|
| Motion/stance | FirstPersonMotor / PlayerStance | Measured local velocity, crouch/slide | Existing view/weapon movement |
| Equip/ADS/reload | WeaponRuntimeState | Shoulder-stock placement, calibrated palm/arm solve and actual state timers | Existing VAL controller |
| Fire | Accepted WeaponController.ShotFired | Bounded cosmetic recoil | Existing shot effects/controller |
| Melee | MeleeAttackState | Upper-body clip sampled by PresentationProgress | Existing crowbar controller |
| Interaction/consume | Successful gameplay transaction | Upper-body gesture | WeaponParent lowering |
| Treatment | PlayerSurvival.ApplyingTreatment | Treating flag, hidden world weapon and released firearm pose | WeaponParent lowering; no bandage clip |
| Damage/death | PlayerHealth | Hit/death clip; dead weapon hidden | Existing gameplay + cosmetic flinch |
| Vehicle | Committed InVehicle state | Seated pelvis and wheel targets | Existing seat camera/input ownership |

`PlayerSurvival.Initialize` binds the presenter after the component is dynamically added by SessionFlow. Binding first detaches the previous subscription and is safe to repeat. Enable/disable unsubscribes/rebinds. Health reset rebinds the Animator and clears transient triggers. Death can animate on unscaled time; living paused presentation freezes. New sessions construct their own presentation; no global character singleton is introduced.

## Visibility and camera ownership

World meshes and cosmetic weapons use layer 30. After the 2026-10-10 Scene-view defect report, visibility is camera-specific: CharacterBodyVisibility subscribes to URP begin/endCameraRendering. Only the owning player's FPS camera and its child scope cameras see that actor as ShadowsOnly. Scene, inspection and other-player cameras see its full body with original shadow modes. Render completion restores the previous camera context (or normal full-body visibility); there is no permanent global ShadowsOnly flag. Renderer arrays are cached and refreshed on equipment changes, initialization and re-enable. Future remote ownership can use `SetLocalOwner(false)`; networking is outside this change.

The rifle viewmodel uses layer 29; legacy crowbar uses layer 2. Both external inspection camera paths include 30 and exclude **both 29 and 2**. Independently of layer masks, CharacterBodyVisibility uses forceRenderingOff to render each actor's FPS arms only in that actor's primary owner camera. This also prevents floating arms in Scene view and another player's camera, even when those cameras include all layers. Original forceRenderingOff values are preserved on recache/disable. Scope cameras see the world without any FPS arms. Existing FPS renderers remain on their authored layers. Owned FPS material copies preserve source textures/normal maps and use Diesel-compatible skin/jacket tints; identical topology or texture identity is not claimed.

In Editor or Development Build, F6 toggles the real player's inspection camera; F7 cycles Front, Back, Left, Right, Three-quarter. It changes rendering only: movement, look, interaction and aim continue using the existing FPS authority. Camera creation is lazy; no second AudioListener is created. A wall probe limits on-foot camera penetration. Seated exterior inspection skips that probe to avoid being trapped inside the car shell.

Inspection refuses entry if the gameplay camera is already disabled by another owner. It restores the captured enabled flag, body visibility and its own camera on toggle-off, disable, destruction, lost body or remote-ownership change. The existing opt-in vehicle diagnostic (`-veh001Inspect`, 8/9) now respects character inspection and restores Diesel visibility. Its public activation refuses while character inspection owns the view; character inspection likewise cannot take the disabled view from the vehicle diagnostic. Vehicle entry/exit continues to reposition the original gameplay camera and player through VehicleActor.

Known limits: external view does not change gameplay aim direction or turn into third-person controls; inspection is diagnostic. Runtime camera arbitration, reload synchronization, teardown and seated death require user execution of focused tests and manual acceptance.
