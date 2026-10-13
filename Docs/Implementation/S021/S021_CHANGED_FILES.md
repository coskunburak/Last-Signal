# S021 changed files and ownership

The checkout already contained unrelated uncommitted work. The following are S021 touch points; Git's full dirty list includes earlier combat feedback, zombie, vehicle and vendor changes that are outside this sprint.

| Area | Paths |
|---|---|
| Core combat | `Assets/LastSignal/Scripts/Runtime/Combat/{MeleeWeaponDefinition,MeleeAttackState,MeleeAttackResolver,PlayerCombatController,WeaponDefinition,WeaponRuntimeState,WeaponController,WeaponAudioPresenter,MeleeWeaponPresenter}.cs` |
| Damage zone | `Assets/LastSignal/Scripts/Runtime/AI/ZombieHitRegion.cs` |
| Ammo and UI | `Assets/LastSignal/Scripts/Runtime/Inventory/{InventoryContainer,PlayerInventory}.cs`, `Assets/LastSignal/Scripts/Runtime/{Session/AcceptanceHud,Shelter/ShelterStorageUI}.cs` |
| Persistence | `Assets/LastSignal/Scripts/Runtime/Persistence/{SaveGame,SaveSession,SaveValidation}.cs` |
| Editor authoring/build | `Assets/LastSignal/Scripts/Editor/S021CombatAuthoring.cs`, `Assets/LastSignal/Scripts/Editor/S013/S021Build.cs` |
| S021 data | `Assets/LastSignal/Data/Combat/S021/`, `Assets/LastSignal/Data/Items/Definitions/{ammo.9mm,weapon.pistol,weapon.blade,weapon.axe}.asset` |
| Integration assets | `Assets/LastSignal/Prefabs/Combat/S021/`, `Assets/LastSignal/Prefabs/Items/ammo.9mm.prefab`, `Assets/LastSignal/Art/CharacterIntegration/Diesel_WorldBody.prefab`, `Assets/LastSignal/Prefabs/Player/Player.prefab` |
| Existing data/scene bindings | `Assets/LastSignal/Data/Combat/WeaponDefinition_AssaultRifle.asset`, `Assets/LastSignal/Data/Combat/Crowbar/Crowbar.asset`, `Assets/LastSignal/Data/Items/Definitions/ItemCatalog.asset`, `Assets/LastSignal/Data/Loot/Profiles/Security.asset`, `Assets/LastSignal/Scenes/Production/S013Cabin.unity` |
| Tests and runner | `Assets/LastSignal/Scripts/Tests/{EditMode/S021CombatTests,PlayMode/S021CombatPlayTests}.cs`, `Tools/s021-verify.py` |
| Records | `Docs/Implementation/S021/*.md` |

Unity `.meta` files accompany new scripts and assets; their generated GUIDs must remain unchanged. `Assets/ThirdParty/Weapons` and `Assets/ThirdParty/Audio` were already present locally and were referenced, not rewritten by S021 authoring.
