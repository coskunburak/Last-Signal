# P02-GAP-S007 — reconciliation before implementation

Baseline main / 92fd010; tracked tree clean at entry. Historical S007 is ammunition/reload, historical S008 is shelter. Neither is renamed. Canonical S007 is D061–D070, implemented here separately. Existing runtime source is authoritative.

| Card | Existing implementation / evidence | Missing delta | Required change |
|---|---|---|---|
| D061 | PersistentEntityId, loot point IDs and drop GUIDs; no spatial IDs | Deterministic regions and ownership | Integer coordinates and floor mapping; explicit content ownership |
| D062 | Resident WorldTimeAcceptance scene | Actual independent loading units | Measure instantiated authored prefab roots; retain resident service/shelter apron |
| D063 | SessionFlow staged restore only | Spatial lifecycle/readiness | Explicit state machine; restore, physics and navigation prerequisites |
| D064 | Shelter interaction gate | Streaming boundary | Solid enclosing geometry and readiness-gated interaction portals |
| D065 | WorldSnapshot, consumption receipts, door/enemy DTOs | Unloaded ownership | Reuse DTOs and LootPopulationService per exclusive content scope |
| D066 | SessionFlow.Generation / SaveSession staged load | Cell operation invalidation | Session + operation token checked before every asynchronous continuation |
| D067 | ZombieNavigation and baked resident NavMesh | Per-cell nav lifetime | Owned baked data; enclosed cells use explicit no-crossing fallback |
| D068 | OwnershipTransaction and synchronous Capture | Cell snapshot coordination | Reject unstable lifecycle, combine resident and unloaded snapshots in SaveGame |
| D069 | Historical profiles only | Current traversal measurements | 10+ cycles, entity/scene counts, memory and latency |
| D070 | Historical standalone runners | New route/build | Dedicated scene, focused/full tests and standalone driver |

The bounded loading candidate is authored prefab content roots: instantiate inactive, restore, install navigation, synchronize/validate collision, then publish Ready. Unload captures detached delta before removing runtime content. No Addressables requirement has been demonstrated. The resident service apron retains existing shelter/session composition; two additional enclosed content cells are independently unloadable. Shelter remains explicitly resident; this is not an unrestricted open-world grid. Architecture choice is provisional until measured spike results.

Prior P02-GAP acceptance document is incomplete; actual XML must be inspected before reporting sleep/regression results. S009 population systems are excluded.

## Implementation Closure Evidence

* **D069/D070 Validation:**
  * **Focused PlayMode Tests (`current-focused-playmode.xml`):** 4/4 passed (including 12-cycle soak test, cancellations, NavMesh validation, and guaranteed loot interaction).
  * **EditMode Regression (`regression-editmode.xml`):** 247/247 passed.
  * **PlayMode Regression (`regression-playmode.xml`):** 116/116 passed.
  * **macOS Development Build (`build-result.txt`):** Succeeded, 0 Errors, Unity 6000.5.0f1.
  * **Standalone Acceptance (`standalone-result.txt`):** STANDALONE PASS. 12 full A↔B cycles, memory tracked, session-owned boundaries preserved.
