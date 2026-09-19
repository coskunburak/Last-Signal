# Technical Asset Inventory

## Characters

### `spaceship_captain` (tt-3d)
*   **Source:** `Assets/LastSignal/tt-3d/LowPolySci-FiStarterPack/Character/Models/spaceship_captain.fbx`
*   **Rig:** Humanoid (`animationType: 3`, `avatarSetup: 1`)
*   **Scale:** 1 (`useFileScale: 1`)
*   **Materials:** Standard import mode, externally mapped
*   **Triangle count:** ~600-1100 (marketplace spec, NOT_VERIFIED locally)
*   **Root motion suitability:** NOT_VERIFIED
*   **First-person suitability:** UNSUITABLE (full-body, shoulder/hand quality insufficient for FPS close-up)
*   **Full-body suitability:** READY
*   **Humanoid validation:** READY (avatar created from model)

### `space_crew_man` (tt-3d)
*   **Source:** `Assets/LastSignal/tt-3d/LowPolySci-FiStarterPack/Character/Models/space_crew_man.fbx`
*   **Structure:** Same as `spaceship_captain`
*   **First-person suitability:** UNSUITABLE
*   **Full-body suitability:** READY

### `alien_enemy` (tt-3d)
*   **Source:** `Assets/LastSignal/tt-3d/LowPolySci-FiStarterPack/Character/Models/alien_enemy.fbx`
*   **Structure:** Same as `spaceship_captain`
*   **First-person suitability:** UNSUITABLE
*   **Full-body suitability:** READY (potential enemy model)

## FPS Arms

### `VAL.fbx` (Sketchfab)
*   **Source:** `Assets/LastSignal/VAL.fbx`
*   **Rig:** Generic (`animationType: 2`, `avatarSetup: 0`)
*   **Scale:** 1 (internal bones use scale 100 via Blender export convention)
*   **Meshes:** Arms, VAL_Model, Gorka (clothing), Cloth gloves fingerless
*   **Skeleton:** `LVA4_Armature` with hierarchy: root → torso → clavicle → upperarm → forearm → hand → fingers (5 per hand)
*   **Camera bone:** Yes (`Camera` → `camera` → `camera_end`, under root)
*   **Weapon bones:** `wpn_body` → `mag` → `ammo`, `wpn_body` → `slide`
*   **IK targets:** `handIK.R`, `handIK.L`, `pole.R`, `pole.L`
*   **Animation:** `importAnimation: 1`, but `clipAnimations: []` (clips need definition)
*   **Human mapping:** `[]` (empty — not Humanoid mapped)
*   **First-person suitability:** READY (purpose-built FPS viewmodel)
*   **License:** LICENSE_REVIEW_REQUIRED (Sketchfab download, no license file in repo)

## Weapons

### Tactical Pistol (MR POLY)
*   **Source:** `Assets/LastSignal/MR POLY/Low Poly Weapons Set/Models/Tactical Pistol.fbx`
*   **Rig:** Generic (`animationType: 2`)
*   **Scale:** 1
*   **Materials:** Import mode 0 (None — uses separate .mat files)
*   **Prefab variants:** Blue, Brown-Gold, etc.
*   **Components found:** Body mesh, separate trigger, separate magazine (verify in Unity)
*   **Slide:** NOT_VERIFIED (need Unity inspection)
*   **Muzzle reference:** MISSING (needs project-owned socket)
*   **Casing ejection:** MISSING
*   **Right-hand grip:** MISSING (needs project-owned transform)
*   **Left-hand grip:** MISSING
*   **Sight alignment:** MISSING
*   **First-person suitability:** NEEDS_ADAPTATION (no rig, no animations, needs sockets)
*   **World-model suitability:** READY

### Assault Rifle (MR POLY)
*   **Source:** `Assets/LastSignal/MR POLY/Low Poly Weapons Set/Models/Assault Rifle.fbx`
*   **Import settings:** Same as pistol
*   **Prefab variants:** Available
*   **Components:** Body, trigger, magazine (verify separation in Unity)
*   **Bolt/charging handle:** NOT_VERIFIED
*   **Sockets/References:** All MISSING (needs project-owned transforms)
*   **First-person suitability:** NEEDS_ADAPTATION
*   **World-model suitability:** READY

### Pump Shotgun (MR POLY)
*   **Source:** `Assets/LastSignal/MR POLY/Low Poly Weapons Set/Models/Pump Shotgun.fbx`
*   **Import settings:** Same as pistol
*   **Pump component:** NOT_VERIFIED
*   **Sockets/References:** All MISSING
*   **First-person suitability:** NEEDS_ADAPTATION
*   **World-model suitability:** READY

## License Inventory

| Asset | Source | Author/Publisher | Local Path | License | Redistribution | Attribution | AI Usage | Modification Status |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Low Poly Weapons Set** | Unity Asset Store | MR POLY | `Assets/LastSignal/MR POLY/` | Unity Asset Store EULA (assumed) | Per EULA | Per EULA | No (marketplace flag) | Unmodified source, project-owned prefabs created separately |
| **Low-Poly Sci-Fi Starter Pack** | Unity Asset Store | tt-3d / wstylejapan | `Assets/LastSignal/tt-3d/` | Unity Asset Store EULA (assumed) | Per EULA | Per EULA | Not specified | Unmodified |
| **VAL Animation Asset** | Sketchfab | Unknown | `Assets/LastSignal/VAL.fbx` | LICENSE_REVIEW_REQUIRED | UNKNOWN | UNKNOWN | UNKNOWN | Unmodified source, clip definitions added to .meta only |
