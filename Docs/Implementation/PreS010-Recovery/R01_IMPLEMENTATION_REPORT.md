# R01 Implementation Report: Combat Equipment Foundation

## Overview
The R01 implementation introduces the foundational architecture for dedicated combat equipment, separating the player's permanent melee capability from standard droppable/consumable loot. This delivers the Crowbar as a permanent tool and establishes the shared Player Stamina authority used for both movement and combat.

## Architectural Changes

### 1. Shared Player Stamina
A central authoritative `PlayerStamina` component was introduced to govern stamina expenditure across all player actions.
- **Sprint Drain:** 18 stamina per second.
- **Melee Cost:** 25 stamina per swing.
- **Regeneration:** Recovers at 22 per second after a 1.25s delay.
- **Exhaustion:** Triggers upon reaching 0 stamina and persists until reaching a resumption threshold of 25 stamina.
- **Integration:** The `FirstPersonMotor` hooks into `PlayerStamina.Tick()`, strictly draining stamina only when actual movement displacement occurs (holding sprint while standing still does not drain).

### 2. Combat Slot System
The `PlayerCombatController` was expanded to support multiple discrete combat authorities (`CombatSlot.Firearm` and `CombatSlot.Melee`).
- Selecting a new slot disables the previously active weapon's GameObject but **does not destroy the instance**.
- The rifle instance retains its loaded ammunition and runtime state in the background while the crowbar is equipped.

### 3. Melee Simulation
The `MeleeAttackState` handles the state machine independently of Unity's Animator, passing through Ready → Windup → Active → Recovery → Ready.
- **Damage Authority:** Simulation-owned, preventing animation frame hitches from skipping hits.
- **Physics Query:** `MeleeAttackResolver` uses a capsule query with robust rejection logic (walls, rear targets, out-of-range, and dead safety). Multi-collider targets are correctly deduplicated so a single swing registers exactly one hit per enemy.

### 4. Input Routing
Input routing (`PlayerInputReader`) maps the `MeleeSlot` to `<Keyboard>/3` and `FirearmSlot` to `<Keyboard>/1` directly in the InputSystem layout. The pre-existing `neutralRequired` stale-input safety mechanism was strictly maintained.

### 5. Persistence
The existing persistence schemas were extended cleanly through an optional `CombatEquipmentSnapshot` within `SaveGame`.
- **Capture:** Records stamina, exhaustion, regeneration delay, the selected slot, and the melee weapon definition.
- **Hydration:** Safely restores the exact combat capability, preserving the unequipped firearm's magazine state.
- **Legacy Compatibility:** Missing combat snapshots in older saves cleanly default to equipping the firearm and granting full stamina, upholding strict backwards compatibility.

## Safety and Correctness
- **ItemCatalog Invariant:** Catalog items are verified to ensure physical integrity. Loot items retain their strict `WorldPrefab` requirements, while equipment-only `Tool` definitions (like the Crowbar) are explicitly permitted to omit droppable world prefabs, enforcing domain semantics rather than generically diluting validations.
- **Tests:** 10 R01-focused EditMode and PlayMode tests guarantee stamina math correctness, input switching, persistence backwards compatibility, and bounding box safety of the instantiated viewmodels. Full EditMode and PlayMode regression suites maintain 100% pass rates, ensuring zero regression to AI navigation, save validation, or movement logic.
