# S021 architecture decisions

| Requirement | Existing owner | S021 change | Authored coverage |
|---|---|---|---|
| Multiweapon input | `PlayerCombatController` | Owns two live firearm and three melee instances; repeated slot requests cycle, explicit ID selection supports restore | `S021CombatPlayTests` |
| Ammunition | `PlayerInventory` + `WeaponRuntimeState` | Distinct 9 mm item; guarded reserve-to-magazine transfer publishes after both owners change | `S021CombatTests` |
| Damage | `WeaponFireResolver`, `MeleeAttackResolver`, `ZombieHealth` | Reuses damage receipt; melee impact/noise now follows accepted receipt; optional region protection applies after zone multiplier | `S021CombatTests` |
| Condition | Per-firearm `WeaponRuntimeState` | Integer 0–1000, one point per accepted shot; damage factor .8–1; zero blocks fire and gives dry-fire cue | `S021CombatTests` |
| Third-person weapon | `CharacterWeaponRig` | Three new pose profiles and model wrappers appended to Diesel world body | `S021CombatPlayTests` structural checks |
| Persistence | `SaveSession`, `SaveValidation` | Additive combat extension v2 saves each firearm, selected IDs and condition; root rifle section retained | `S021CombatTests` |

Inactive viewmodels are retained, with state and subscriptions cancelled on disable. `UnequipWeapon` remains a terminal/legacy removal operation; normal switches use `SelectFirearm`, `SelectMeleeWeapon` and `SelectSlot`. `RestoreCondition` clamps incoming values, while save validation rejects out-of-range persisted values.

Protection is a per-zombie-region fraction (0–0.9), default zero. Damage order: base → anatomical multiplier → protection fraction → health/dismemberment receipt → visual/audio response. No armor inventory or attachment model was added. The existing hit-region metadata remains the wound presentation source.

Repair is deferred: current inventory stacks have no durable per-item instance ownership or condition slot. A repair command would need an atomic material and equipment transaction. The bounded firearm experiment remains persistent without creating a pseudo-repair path.
