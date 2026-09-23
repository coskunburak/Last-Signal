# P02-GAP / Canonical S008 Completion

Entry: 2026-09-22, main at 5b43cfbb8532d23ae5830e8956673b4d6b9f8fc3, Unity 6000.5.0f1. Existing dirty persistence, test corrections, materials and historical evidence are authoritative and preserved. No commit/reset/renumbering requested.

| Canonical identity | Actual implementation / evidence | Missing delta / action |
|---|---|---|
| S007 D061–D070, two cells | Historical S007 is ammunition/reload; current world is one loaded scene. SaveSession has single-cell staged restore and generation cancellation. | CellReady, streaming, stale cell callback, cross-cell navigation are missing. Not implemented in P02-GAP. |
| S008 D071–D080, world time | No existing world clock/weather/wetness/sleep. Historical S008 is shelter/preparation/expedition/return. | Implement and verify here; preserve Docs/Implementation/S008 unchanged. |
| S009 | Existing combat, visual AI and loot; no noise/pressure/population travel. | Consume absolute world seconds, boundary scheduler and processed timestamps. S007 remains a prerequisite gap. |
| S010 | Historical S008 already provides same-scene shelter, storage and atomic transfers. | Craft jobs, fuel, upgrades and claim rules remain future scope. Reuse storage and rest integration. |
| Persistence | Local SaveGame/SaveCodec/SaveValidation/SaveFileStore/SaveSession and PersistenceAcceptance scene; schema 1, checksum, atomic replacement, detached validation, menu-only staged load. | Add versioned time section in same transaction, deterministic legacy defaults; retain v1 fixture coverage. |
| Survival | PlayerHealth exists; stamina, bleeding, treatment and needs do not. | Add bounded rest healing using existing health with wetness modifier. Death/fuel boundaries tested as explicitly synthetic scheduling fixtures. |

## Implementation strategy

Pure C# WorldSimulation owns double absolute seconds, deterministic weather and bounded wetness. Normal real delta is converted once; controlled advancement splits at registered event boundaries and weather changes. Rest healing uses the integrated wetness curve. Unity WorldClock owns session lifecycle and authored settings, exposure, rest checks and presentation binding. Authored roof markers prevent arbitrary overhead colliders acting as roofs. Existing UI Text and IInteractable are reused. Scene is derived from local PersistenceAcceptance without replacing historical scenes. Full regression, fresh macOS Development build and standalone route are required before completion. No physics determinism or completed streaming claimed.
