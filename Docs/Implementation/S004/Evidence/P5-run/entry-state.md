5cf747db643b32d0ab0d108a955f246a237c4ae8
 .../Zombie/Prefabs/LS_Zombie_Runtime.prefab        | 1020 ++++++++++++++++++--
 Assets/LastSignal/Enemies/Zombie/Shambler.asset    |    3 +
 .../Prefabs/Combat/Weapon_AssaultRifle.prefab      |    2 +-
 .../Scripts/Runtime/AI/ZombieAnimationPresenter.cs |   43 +-
 .../Scripts/Runtime/AI/ZombieController.cs         |   71 +-
 .../Scripts/Runtime/AI/ZombieDefinition.cs         |   12 +
 .../Scripts/Runtime/AI/ZombieRuntimeState.cs       |   20 +-
 .../Scripts/Runtime/Combat/IDamageable.cs          |    9 +
 .../Scripts/Runtime/Combat/WeaponFireResolver.cs   |   49 +-
 .../Scripts/Tests/EditMode/ZombieLogicTests.cs     |    3 +
 .../Scripts/Tests/PlayMode/ZombieMeleeTests.cs     |    2 +-
 .../PlayMode/ZombieProductionAcceptanceTests.cs    |    2 +-
 Docs/Implementation/S004/S004_ACCEPTANCE.md        |    5 +
 .../S004/S004_IMPLEMENTATION_BACKLOG.md            |   11 +
 Docs/Implementation/S004/S004_RISKS.md             |   12 +
 .../Implementation/S004/ZOMBIE_ANIMATION_MATRIX.md |    7 +
 Docs/Implementation/S004/ZOMBIE_ARCHITECTURE.md    |   11 +
 Docs/Implementation/S004/ZOMBIE_BEHAVIOR_SPEC.md   |   11 +
 ProjectSettings/DynamicsManager.asset              |   19 +-
 ProjectSettings/ProjectSettings.asset              |    2 +-
 ProjectSettings/TagManager.asset                   |   29 +-
 21 files changed, 1197 insertions(+), 146 deletions(-)
