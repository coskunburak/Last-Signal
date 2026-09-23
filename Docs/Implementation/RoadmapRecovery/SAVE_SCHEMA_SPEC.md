# Save schema v1

Envelope: `format=last-signal-save-1`, SHA-256 `checksum`, escaped readable JSON `payload`. Checksum detects accidental corruption; it is not authentication or anti-cheat. UTF-8 limit 8 MiB.

Payload required sections: header, player, inventory, weapon, shelter, world. DTOs contain strings/numbers/bools/arrays and scalar transforms; no GameObject/Transform/MonoBehaviour/ScriptableObject reference. Missing sections fail. An explicit content version currently requires exact equality; unknown future schemas are rejected.

Header: schemaVersion=1, contentVersion, buildId, worldId, signed int seed, positive increasing long generation, UTC ISO round-trip timestamp. Wall timestamp is metadata only.

Player: stable ID, position XYZ and unit quaternion XYZW, health, pitch, crouch. Finite coordinates bounded to ±100000m; normalized quaternion tolerance .001. v1 checkpoints require a living player; death/recovery save policy is an OPEN future extension, not silently invented.

Inventory/stash: globally distinct container IDs, capacity 1–256, exactly capacity ordered slot records. Empty slot has no ID and quantity zero; occupied slot resolves to known definition with quantity 1..MaxStack. No serialized cached weight or reserve-ammo duplicate. Rifle stores definition identity and magazine 0..capacity. The shelter section also stores expedition index/state.

World: doors (stable ID/open), opportunities, item deltas, enemy health/transform. Combined entries capped at 10000. IDs across player, containers, doors, opportunities, world items and enemies must be nonblank, ≤128 chars and unique. The current validation context copies definition limits to prevent later caller mutation.

The schema is wired to real capture/hydration in the dedicated persistence scene; the actual saved route file is retained in standalone evidence. Limits are reversible technical bounds for the current small world, not release-scale certification.

Opportunity Unknown is an invalid checkpoint: all current-world points must resolve before saving. Empty and Blocked are explicit non-generated outcomes; Generated has exactly one Present/Consumed entity. Present world piles accept any positive int quantity, including quantities over a container MaxStack, because partial pickup already supports oversized piles. Container slots remain bounded by MaxStack. Consumed quantity must be zero.

Owner IDs are player.local, player.inventory and shelter.storage; the weapon definition contract is weapon.rifle. ID uniqueness is enforced across all persistent owners, opportunities and entities, independently of definition IDs which may repeat. Door/point/encounter topology must match the configured content. SHA-256 is integrity detection, not a security boundary. Build ID is metadata; content version controls compatibility.
