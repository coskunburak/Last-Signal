# First Weapon Decision: Assault Rifle vs Tactical Pistol

## Overview
This document outlines the decision to prioritize the implementation of the Assault Rifle over the Tactical Pistol for the Gate 0 combat vertical slice in the Last Signal project.

## Key Points

*   **Animation Readiness:** The `VAL.fbx` asset contains a complete FPS arms rig equipped with a rifle-type weapon. This rig includes all necessary bones (`wpn_body`, `mag`, `ammo`, `slide`, `Camera`, `handIK.L`, `handIK.R`) for a fully functional first-person perspective.
*   **Built-in Data:** `VAL` contains baked animation data covering essential states: idle, fire, reload, and equip animations.
*   **Pistol Limitations:** The MR POLY Tactical Pistol is a static, Generic-rigged mesh that currently contains zero animation clips.
*   **Combat Loop Validation:** Starting with the rifle allows us to validate the complete combat loop using real animations from day one, rather than blocking on animation creation.
*   **Future Integration:** The Tactical Pistol will follow as the second weapon (planned for Gate 11). It will share the proven architecture established by the rifle implementation.
*   **System Stress-Testing:** The rifle's automatic fire mode provides a more rigorous stress test for our fire cadence and cooldown systems compared to a semi-automatic-only weapon.
