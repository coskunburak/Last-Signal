# S006 architecture proposal (pending entry gate)

Reviewed LS-DOC-02/07/08/24, production S006/P01 and S005 architecture/spec/report/acceptance. S005 risks/backlog documents are absent. Historical sprint numbering and container/persistence/weight plans are superseded for this implementation by the supplied S006 brief. LS-DOC-08 themes justify kitchen, clinic, workshop and security profiles using six existing definitions.

Data flow: ItemDefinition → LootEntry → LootProfile; authored LootSpawnPoint + session seed → LootPopulationService → existing WorldItem → existing PlayerInventory.

LootProfile: immutable ScriptableObject, direct entries with positive integer weights and inclusive quantity range, integer empty probability in basis points. Reject whole invalid profile with diagnostic; no retries or hidden normalization. Zero/negative weights rejected. World quantities may exceed MaxStack because WorldItem/PlayerInventory explicitly support partial transfer.

Point identity: serialized GUID, duplicate IDs rejected, editor repair action explicit. Dedicated deterministic integer RNG per point from session seed + stable point ID, stable project-owned hash, never global Unity Random. Ordinal point sorting makes overlapping placement decisions stable. No profile recursion or runtime mutation.

Authority: session-owned component with explicit Begin/End calls through narrow SessionFlow integration. One-time discovery only inside the SessionFlow scene. Resolves every point once including empty/invalid/blocked outcomes. A second call is inert until teardown. Spawned items parented under a scene-owned root. Optional explicit test/development seed, generated normal session seed logged once.

Placement: authored center/orientation with conservative bounds, short support ray, world clearance and player capsule exclusion. Fail closed with no retries or teleportation. Editor gizmos and validation report include IDs, profiles, invalid placement, overlaps and profile counts. Authored prefab footprint must fit the configured bounds. No Update methods.

Compatibility defects to reproduce before fixes: existing SessionFlow does not clean dropped WorldItems; existing seed item prefabs have colliders but no visible renderer. Minimal fixes limited to session cleanup ownership and project-owned visual wrappers if confirmed. No inventory transaction or combat changes.

Verification: entry full suites first; deterministic/validation unit tests; population/placement/scene isolation/pickup/drop/session soak integration; 10,000-seed distribution report; 25/50/100 population measurements; full regression; development build; actual standalone interaction evidence. Future persistence can store stable point ID, item ID, remaining quantity and resolved state. Containers/save/ammo economy/respawn remain deferred.
