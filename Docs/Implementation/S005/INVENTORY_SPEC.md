# Inventory Specification

## Capacity Model
- **Type:** Fixed slots.
- **Constraints:** Number of slots is defined per inventory (default 24).
- **Rule:** A slot contains either 0 of an item, or an item and a quantity between 1 and `MaxStack`.

## Operations

### Add (`TryAdd`)
- **Behavior:** Attempts to add the requested quantity.
- **Algorithm:** 
  1. Fills existing non-full stacks of compatible items.
  2. Fills empty slots up to `MaxStack`.
- **Result:** Returns the exact quantity accepted.
- **Partial Add:** Allowed. If inventory is full, the remainder is safely rejected.

### Remove (`TryRemove`)
- **Behavior:** Removes a specific quantity from a given slot.
- **Result:** Fails entirely if the slot does not have sufficient quantity (All-or-nothing).

### Move (`TryMove`)
- **Behavior:** Moves a stack from a source slot to a destination slot.
- **Rules:**
  - If destination is empty, the stack is moved.
  - If destination holds the same item type, a `Merge` is attempted.
  - If destination holds a different item type, a swap occurs.

### Merge (`TryMerge`)
- **Behavior:** Combines two stacks of the same item.
- **Rules:** Transfers as much quantity as possible up to `MaxStack`. The remainder stays in the source slot.

### Split (`TrySplit`)
- **Behavior:** Moves a specified amount from a source stack to an empty destination slot.
- **Rules:** Fails if the destination is not empty, or if the source quantity is insufficient.

### Drop (`TryDrop`)
- **Behavior:** Removes a quantity from a slot and spawns a `WorldItem`.
- **Placement:** Raycasts forward from the `dropOrigin` (usually camera). Spawns at a safe distance or just in front of the hit point if obstructed.

## Edge Cases and Safety
- **Invalid Quantities:** 0 or negative quantities are explicitly rejected in all operations.
- **Zero Quantity Handling:** Slots automatically clear themselves when quantity reaches 0. World items destroy themselves when fully depleted.
- **Rapid Interaction:** Multiple raycast interaction requests resolve safely; `TryAdd` is deterministic, and `WorldItem` correctly updates its internal remainder.

## UI Representation
- The Inventory UI is strictly a presentation layer. It binds to `InventoryChanged` events.
- Opening, closing, or destroying the UI does not affect inventory data.

## Session Reset
- `SessionFlow` instantiates a new `PlayerInventory` component each time a Player prefab is spawned.
- The UI unbinds cleanly when `ReturnToMenu` is called.
- No static persistence is used for player inventory, ensuring a completely clean state between sessions.
