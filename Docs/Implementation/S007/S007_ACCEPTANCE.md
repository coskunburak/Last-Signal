# S007 Acceptance

This document records the acceptance criteria for S007.

## Completed Gates
- **117/117 Entry EditMode**, **76/76 Entry PlayMode**.
- **136/136 Final EditMode PASS**
- **PlayMode** validated for Real Integration of Scavenge -> Combat -> Drop/Repick.
- **HUD Performance**: 0 B recurring allocations in steady state.

## Scavenge -> Combat Loop Acceptance
- Player loads into session.
- deterministic loot spawns ammo.rifle (5-20 quantity).
- Player picks up ammo.
- PlayerInventory Reserve accurately increases.
- HUD reflects correct reserve.
- Player reloads, transferring exact required ammo to Magazine.
- Firing consumes rounds from Magazine, not Reserve.
- All edge cases (interrupted reload, zero reserve, full magazine, multiple drops) correctly conserve ammo.

All criteria met.
