# Persistence architecture — recovery gate A

Status: gameplay capture/hydration implemented and focused integration verified. Final regression, build and standalone gates are recorded in SAVE_ACCEPTANCE.md; architecture alone is not acceptance evidence.

| Existing authority | Static | Transient | Save-persistent requirement |
|---|---|---|---|
| ItemDefinition / StableItemId / ItemCatalog | ID, stack limit, prefab, category | lookup cache | definition IDs only |
| InventorySlot / InventoryContainer / PlayerInventory | default capacity | observers, UI selection/drop origin | ordered quantities and actual capacity |
| WorldItem | definition/prefab | collider, GameObject | entity ID, source, quantity, transform, consumed delta |
| DoorInteractable | hinge/leaf/open angle | busy/obstruction animation | authored stable ID and open/closed; first adapter rejects busy capture |
| LootPopulationService / LootSpawnPoint | point IDs/profiles | runtime root/prefab refs | seed and all resolved opportunities; explicit consumed record |
| SessionFlow | prefab/spawn/scene refs | player instance/input/subscriptions | world ID; monotonic session boundary |
| ShelterStorage / ShelterLoop | capacity/anchors | modal preparation | stash, expedition state/index; reopen modal closed |
| PlayerHealth | maximum | damage feedback/subscriptions | health; v1 living-player checkpoints only |
| WeaponRuntimeState | rifle definition | trigger/timing/animation | magazine separately from inventory reserve; no replayed reload |
| ZombieHealth / ZombieEncounter | prefab/spawn | perception/action timer/target | enemy identity, health/death policy |

One synchronous authority owns each save path on the main thread. The composition root must capture detached DTOs at a quiescent transaction boundary and hydrate only after full validation. No per-frame polling or independent component files. UI never owns state. `SaveFileStore` supplies the validated data/disk boundary; it does not pretend to capture or hydrate Unity objects.

Required item/catalog/world/version mismatch rejects the entire candidate; nothing is silently deleted or clamped. Schema and content versions are distinct. Future cells/time/weather/pressure/objectives require explicit schema extension. No migration chain exists before there is a real second schema.

Load order: menu-only request → validate file/schema/catalog and exact authored topology before creating a session → SessionFlow.BeginRestoreSession (paused, input disabled, no baseline loot generation) → one frame for the existing weapon Start → verify session generation → replace inventories, restore doors, rebuild owned loot root from saved outcomes, restore player/weapon/shelter/enemy → synchronize physics and check player capsule clearance → enable input. Failure tears down only the newly created session and returns to menu without writing the source. A disposed coroutine cleans up its own restoring generation. An interrupted older load cannot modify a newer session. Active-game loads return Busy. This controlled teardown policy avoids partially replacing an existing active world.

Pickup/drop now stage the existing inventory mutation, commit the world side, then publish the normal observer notification while the ownership barrier remains held. Save capture and recursive transfer return Busy/reject throughout publication. A subscriber exception propagates after authoritative quantities are committed; finally releases both guards. It does not undo or repeat the committed transfer. Global InventoryChanged semantics and the existing ammunition assertion remain unchanged. Shelter transfer and ammunition reserve commit retain their existing container IsBusy protection. InventorySnapshots rejects that state. A stable later capture succeeds.

Safe publication: validate/serialize → detach candidate → verify increasing generation → write unique same-directory temp and flush → reread/checksum/validate/exact compare → check session generation → atomic File.Replace with previous generation backup (File.Move only for first save). No delete-current fallback. Corrupt current blocks implicit overwrite; backup is explicitly readable. File-system power-loss guarantees and Windows behavior remain unverified until real interruption/platform tests.

## Composition and lifecycle

`SaveSession` owns persistence orchestration; `SessionFlow` continues to own session creation, pause/input and teardown. Explicit `Capture`, `Save` and coroutine `Load` APIs are the acceptance surface. No autosave, hotkey, frame polling or release save menu is added. PersistenceAcceptance.unity is a separate composition copied from the verified shelter scene, with one existing door prefab and serialized stable identities. The original acceptance scene is retained. The opt-in `-persistenceAcceptance <folder>` driver exists only in Editor/development builds.

Capture uses bounded, explicit scene-local discovery; the generated-loot receipt dictionary belongs to LootPopulationService and is cleared on session end. Generated identity is `loot:` plus authored LootSpawnPoint ID. Doors and the encounter use serialized PersistentEntityId. A new player drop gets an ID once when created; hydration assigns the saved ID before activation. There is no static world registry.

Health, pitch, crouch, full player pose, carried slots, stash slots, magazine, expedition state/index, door state, resolved loot, drops, enemy health/pose are persistent. Presentation timers, target tracking, input, reload progress and UI selections are transient. Restoring a reload cancels its pending animation/commit and preserves the captured committed magazine/reserve counts. Living checkpoints only; no death-bag policy is invented. Enemy death invokes the existing terminal AI path.

V1 is one configured world (`world.shelter.acceptance`, content `shelter-v1`), not a scene-name identity or a fabricated cell system. Exact topology/content compatibility rejects additions/removals until explicitly versioned. Independently authored WorldItems are rejected by this world's adapter; generated loot and drops are supported. Multi-cell ownership, migration, mass/equipment payloads and recovery UI are future contracts.
