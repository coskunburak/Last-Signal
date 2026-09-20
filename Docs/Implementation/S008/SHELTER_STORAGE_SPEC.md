# Shelter storage specification
ShelterLoop constructs one ShelterStorage per application gameplay session. Capacity is a serialized fixed-slot value (default 48; supported 1–256), while carried inventory remains 24. Production initial storage is empty: no free rounds, automatic deposits, healing, magazine fill or starter supplies. Tests may seed fixture quantities explicitly.

Storage reuses InventoryContainer and InventorySlot, with ItemDefinition and StableItemId identity. Positive quantities only; known catalog MaxStack limits apply; empty stacks are cleared. Stable slot traversal fills compatible partial stacks first, then empty slots. Add returns accepted quantity; exact Remove across stacks returns false without mutation if insufficient. UI receives copied InventorySlot structs and cannot mutate backing slots.

Closing UI, leaving, returning, pausing and player death do not erase stash contents. ReturnToMenu detaches UI/listeners and releases the stash reference. Session B starts empty. Storage is not a MonoBehaviour tied to a panel's destruction.

Preparation scroll grids size once at session bind to match capacity, and refresh on events. For future serialization: SlotIndex + StableItemId.Value + Quantity; resolve IDs through the catalog before hydration. No serialization is implemented in S008.
