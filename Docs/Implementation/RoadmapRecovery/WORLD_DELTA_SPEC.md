# World delta contract

The authored baseline is the configured world, serialized door/encounter identities, point StableIds/profiles and item catalog. The save owns semantic deltas, not scene serialization. It stores seed plus each resolved outcome so load never invokes a fresh RNG pass. Geometry, prefabs, UI, colliders and component references remain authored assets.

An opportunity is explicitly Generated, Empty or Blocked; Unknown is not a valid checkpoint. Every Generated opportunity points to exactly one stable `loot:<pointId>` entity record. Two opportunities cannot own the same entity, and every Loot-origin record has exactly one opportunity. Unresolved and consumed can never be conflated.

A generated item is Present (known definition, positive quantity, transform) or Consumed (same identity/definition, quantity zero). Present piles can exceed MaxStack, preserving the existing partial-pickup contract. LootPopulationService owns a session-local consumption receipt dictionary. WorldItem reports committed quantity before inventory observers. Missing/inactive generated entities without a zero-quantity receipt fail capture; disappearance alone is not consumption. Receipts reset on teardown and restore directly from saved dispositions.

Full pickup produces a tombstone; partial pickup retains the same entity ID with exact remaining quantity. Hydration creates only Present records. Dropped items have separate stable entity IDs and Drop origin; they do not attach to a spawn opportunity. Consumed drops disappear, since no authored baseline can regenerate them. Saved drop IDs are assigned before activation and restored exactly once.

Restore starts with a clean session and an owned loot root, using saved outcomes instead of generation. Inventory and stash replacements validate every slot before mutation. Active-game repeated load is Busy; repeated menu loads replace the session and conserve all owners. Failed hydration destroys the new session's objects.

Door deltas restore authoritative open/closed angles directly without replaying interactions; moving doors return Busy on capture. Enemy pose/health are restored onto the authored encounter's new actor; zero health triggers the existing dead state. Player pose/health/stance, stash and magazine are independent authorities included in the same snapshot.

V1 rejects independently authored WorldItems in this composition rather than silently accepting unsupported baseline ownership. No unloaded-cell store, object destruction generalization or streaming readiness claim is made. Future cells must hand their deltas to this authority before unload and explicitly version their identity/topology contract.
