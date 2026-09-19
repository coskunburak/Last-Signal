# FPS Presentation Decision: Dedicated Arms vs Full-Body

## Overview
This document details the architectural decision to use dedicated first-person (FPS) arms for the player viewmodel instead of utilizing a full-body character mesh.

## Analysis and Key Points

*   **Dedicated FPS Assets:** The `VAL.fbx` asset contains purpose-built first-person arms meshes (the `Arms` object) paired with the `LVA4_Armature` skeleton.
*   **FPS-Specific Bone Structure:** `VAL` includes a `Camera` bone and a root bone specifically designed for FPS viewmodel attachment, ensuring correct alignment and perspective.
*   **Full-Body Limitations (tt-3d):** The tt-3d characters (`spaceship_captain`, `space_crew_man`, `alien_enemy`) are low-poly (~600-1100 triangles), Humanoid full-body models explicitly designed for third-person viewing.
*   **Issues with Full-Body in FPS:**
    *   Camera clipping through shoulder geometry during movement or animations.
    *   Poor hand deformation at close range.
    *   Forearm and wrist quality is insufficient for the close inspection required in an FPS view.
*   **Fidelity Requirements:** The `VAL` rig provides individual finger bones (index, middle, ring, pinky, thumb on both hands), which is critical for achieving high-quality weapon grips.
*   **Integrated Animation:** `VAL` features weapon-specific bones (`wpn_body`, `mag`, `slide`) integrated into the same skeleton. This allows the weapon and hands to animate together seamlessly.

## Decision
**Use VAL's dedicated FPS arms for the viewmodel.** 

The tt-3d characters will be reserved for future use cases such as shadow casting, third-person perspective (if applicable), and multiplayer world models.

**Asset Integrity Rule:** The original imported FBX files are NOT to be modified. Project-owned prefabs will reference the imported assets to maintain a non-destructive pipeline.
