# Architecture Freeze - P5
The following core architecture components are frozen for the duration of P5. They may only be modified to fix reproducible defects.

- **ZombieController**: Authoritative behavior state
- **ZombiePerception**: Visual sensing
- **ZombieNavigation**: NavMesh movement
- **ZombieAnimationPresenter**: Presentation
- **ZombieHealth**: Zombie health and death state
- **ZombieHitRegion**: Hit classification and owner routing
- **PlayerHealth**: Player health state
- **WeaponFireResolver**: Rifle hit resolution
- **SessionFlow / ZombieEncounter**: Lifecycle management

*No God MonoBehaviours are present in the current vertical slice.*
