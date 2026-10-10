# Character architecture

Implementation description; verification is NOT_RUN.

```text
S013Cabin / SessionFlow -> existing Player.prefab
  Player (capsule/motor/input/look/combat/interaction/health; unchanged authorities)
    WorldBody -> Art/CharacterIntegration/Diesel_WorldBody.prefab
      Animator -> existing Animations/WorldBody.controller
      PlayerLocomotionPresenter (single world Animator parameter writer)
      CharacterWeaponRig (final shoulder/arm solve + cosmetic weapon)
      CharacterHandPose (cached anatomical wrist/finger calibration, owned by weapon rig)
      CharacterSeatPresentation (visual pelvis/steering; no seat authority)
      CharacterBodyVisibility (local shadows / external rendering)
      Armature -> Mixamo bones + nine skinned parts
      WorldWeapon (equipment-specific visual only)
    View -> existing FPS camera
      WeaponParent -> existing VAL rifle or crowbar viewmodel
    CharacterInspectionCamera -> lazily created diagnostic camera
    CharacterFirstPersonActions -> cosmetic action lowering/flinch
```

`FirstPersonMotor.HorizontalVelocity/VerticalVelocity` expose read-only movement data. No new movement controller or root displacement. Existing MoveX/MoveY/HorizontalSpeed/PlaybackRate/IsCrouching/IsSliding names are retained. Grounded, sprint, air, combat, health and seat state extend the same writer.

Combat remains `PlayerCombatController`, `WeaponController/WeaponRuntimeState` and `MeleeAttackState`. Shot events drive recoil; reload and melee sample their gameplay timers. Cosmetic clips never transfer ammo, deal damage or complete interactions. Accepted interactions/consumptions publish presentation notifications after gameplay succeeds. `PlayerSurvival.Initialize` explicitly binds the presenter because survival is added after player instantiation.

Vehicle ownership remains `VehicleActor` and `PlayerInputReader.InVehicle`. Pausing driving input does not imply exiting the seat. Presentation aligns hips to the existing seat-root origin and uses the steering-wheel transform. Save/load continues to serialize gameplay, not presentation transients. New-session construction rebinds the visuals; health reset clears stale death/action state.

FPS view is the gameplay ray/aim/camera authority even during inspection. Inspection is an editor/development hotkey feature, not a third-person control mode. It excludes layers 29 (rifle) and 2 (legacy crowbar); full-body visuals remain on existing layer 30. URP camera callbacks apply ShadowsOnly to an actor only during that actor's owner FPS/scope render pass, restoring normal full-body visibility for Scene and other-player cameras. FPS arms render only in their own primary camera through cached renderer forceRenderingOff state. Each actor therefore hides only its own body in its own first-person view; other actors remain visible.

For future remote actors `CharacterBodyVisibility.SetLocalOwner(false)` exposes the world body and disables local inspection entry. Presentation is per-actor and uses no Camera.main/global player singleton. A future remote state adapter still needs to supply the existing gameplay contracts; no networking or remote simulation was implemented.
