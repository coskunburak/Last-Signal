# Grip architecture
CharacterWeaponPoseProfile remains the sole weapon-pose asset: identity references, typed GripMode, right PrimaryHand, attack ownership, FPS/shoulder targets, hip/ADS/sprint/crouch data, phase motion, finger closure, blend/recoil and reach budgets. No name-based runtime mode dispatch or second profile registry.

Legacy=0 preserves serialized rifle/crowbar behaviour; TwoHandedRifle is explicitly supported by the existing stock/two-hand branch. Only S021 pistol/machete/axe opt into ProfileArmTargets. CharacterWeaponSockets retains every legacy field, exposes PrimaryGrip (PalmContact then RightGrip), optional SecondaryGrip, and mode-aware validation. Pistol requires primary/sight/muzzle; melee requires primary only. RightHand/LeftGrip/Stock GUID references in older assets remain intact.

Targets are authored independently → calibrated wrist → bounded arm solution → visual attached to solved palm. Clamp residual is retained separately from contact error, so a low contact error cannot disguise unreachable authoring. Neutral target acceptance: both contact and residual approximately <=.02 m; visual finger penetration independently fails acceptance.

Generic FPS references are serialized per prefab; CharacterHandPose accepts explicit bones as well as a Humanoid Animator. Hand/finger transforms restore before the next Animator update and on disable. Camera-space FPS shoulder positions also restore. No solver writes player scale, physics, damage, ammo or gameplay camera transforms.
