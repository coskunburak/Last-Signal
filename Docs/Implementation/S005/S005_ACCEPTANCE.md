# S005 ACCEPTANCE

## 1. NORMAL GAMEPLAY ACCEPTANCE SCENARIO
- **Setup:** A new session started. The Player is spawned.
- **Rifle Verification:** The rifle still fires and animates correctly.
- **Item Discovery:** The player encounters a `WorldItem` (e.g. Bandage).
- **Interaction:** The `InteractionController` successfully highlights the item and shows "Pick up Bandage x5".
- **Pickup:** Player presses 'E' (Interact). The item is accepted into the inventory. The `WorldItem` vanishes.
- **Capacity Rules:** Repeated pickups stack the item up to `MaxStack`. If inventory capacity is full, the remainder remains in the world.
- **Inventory UI:** Player presses 'Tab'. The UI displays the item icon and quantity.
- **Drop:** Player selects the item and clicks "Drop". A quantity is removed, and a new `WorldItem` spawns in front of the camera safely.
- **Re-Pickup:** Player picks up the dropped item. No quantity is lost or duplicated.
- **Combat / NavMesh:** Dropped items use small non-blocking colliders and do not disrupt zombie navigation.
- **Session Reset:** Player returns to the menu. The session cleans up. Starting a new game starts with an empty inventory.

## 2. PERFORMANCE & STRESS
- **Inventory idle recurring managed allocations:** 0 B (no strings allocated per frame in UI).
- **Inventory transaction allocation:** Minor string parsing for quantity label update during inventory changes.
- **World pickup stress:** Passive `WorldItem` components use 0 B per frame and cost no CPU time since they lack an `Update()` loop.

## 3. OVERALL VERDICT
The S005 implementation cleanly integrates with the existing interaction system. It adds a deterministic inventory foundation without relying on UI for authoritative state. The legacy combat loop and input bindings are perfectly preserved.
