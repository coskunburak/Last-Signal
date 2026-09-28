# R01 Asset Inventory

This document tracks the source assets, licenses, and project-owned derivative assets used in the PRE-S010 / R01 Combat Equipment Foundation.

## Crowbar Source

- **Name:** Crowbar — Game Ready Low Poly
- **Author:** e-d
- **URL:** https://sketchfab.com/3d-models/crowbar-game-ready-low-poly-ec466870e57b4198b57b854bf9d37beb
- **License:** CC BY (Attribution Required)
- **Geometry:** 1,184 triangles

## First Person Arms Source

- **Name:** First Person Arms
- **Author:** DJMaesen / bumstrum
- **URL:** https://sketchfab.com/3d-models/first-person-arms-e3c42c05b22944e5839deb8e003f0987
- **License:** CC BY (Attribution Required)
- **Geometry:** ~7,240 triangles, generic rig, no embedded animation clips

## Project-Owned Derivative Assets

These assets were assembled via the `R01Authoring` pipeline and constitute the actual runtime elements in the `LastSignal` project.

- `Crowbar.asset`: The `MeleeWeaponDefinition` configuring damage, physics query radii, and timing windows.
- `CrowbarMesh.asset`: Normalized and optimized geometry for the crowbar.
- `Crowbar_Viewmodel.prefab`: The fully assembled runtime viewmodel containing the Crowbar geometry, FP arms, Animator, AudioSource, and `MeleeWeaponPresenter`.
- `Crowbar.controller`: The runtime Unity Animator Controller driving the FP arms and crowbar.
- `Crowbar_Idle.anim`: Procedural idle animation clip.
- `Crowbar_Equip.anim`: Procedural equip transition animation clip.
- `Crowbar_Swing.anim`: Procedural swing animation clip mapping exactly to the `MeleeAttackState` timing windows.
- `M_Crowbar.mat`: Configured material for the crowbar.
- `M_Arms.mat`: Configured material for the FP arms.

All derivative assets are fully integrated into the project's combat architecture and instantiate deterministically when the player spawns.
