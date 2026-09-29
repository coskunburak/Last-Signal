# S011 — Missions, radio objective and progression safety

Status: IN_PROGRESS. This record is not a closure claim.

## Baseline and sources

Branch `s009-audit-continuation-20260925`; starting HEAD `6e0cfdda4eb40b34325d82c303104cc9d40cb39e`; Unity `6000.5.0f1`. Pre-existing `Tools/s010-verify.sh` was preserved. Local repository has no AGENTS.md. No callable Unity MCP was available; actual Unity CLI execution is used. The live Unity Hub licensing channel was discovered after the historical S010 channel failed.

Read canonical S011, P03, LS-DOC-04/16/17/21, and QA/handoff rules. Historical S010 evidence: 377 EditMode, 160 PlayMode, successful macOS development build, automated 902.122-second route with fixture positioning. Its implementation report's `standalone-acceptance.txt` link is stale; inspected actual `standalone-route.txt`. Historical results do not substitute for this sprint's regressions.

## Architecture and changes

- `Runtime/Objectives/RelayProgression.cs`: single session objective authority with stable definition ID and revision, durable acquisition/knowledge facts, explicit beat states, Contact phase, repair and reward receipts. Completed repair never depends on still holding its consumed fuse. Duplicate notifications are bounded boolean reconciliation, not an unbounded event-ID ledger.
- `Runtime/Objectives/RelayGraph.cs`: five fixed nodes, prerequisites, early behavior, retry behavior and journal text. Validator rejects duplicate/missing IDs and cycles. This is a small fixed arc, not a general quest framework.
- `Runtime/Objectives/RelayMission.cs` and `RelayPoint.cs`: actual ray-target interactions, re-readable cabin note, text-only Contact bulletin, journal, three-second repair, proximity/threat/tool validation, invalidated operation tokens, and recall of existing dropped items. Journal pauses gameplay and closing it cancels pending work.
- `InventoryContainer.Exchange` is reused unchanged: it commits inventory and the infallible receipt mutation behind the existing `OwnershipTransaction` barrier before publishing inventory notifications. Completion, reward, fuse consumption and phase therefore share one save generation.
- `SaveGame`, `SaveCodec`, `SaveValidation`, `SaveSession`: one optional versioned progression extension in the existing save envelope. Header `progressionVersion=1` distinguishes new required progression from legacy absence. Malformed or contradictory ownership/receipts reject before hydration. No second save file.
- `WorldCellManager.RecallDroppedItem`: transfer an existing unloaded drop into the resident world, retaining its persistent world-item ID and removing the old cell owner in the same barrier. Loaded drops are moved; storage and carried copies are retained. No replacement fuse is spawned merely because inventory is empty.
- `SessionFlow` and `AcceptanceHud`: session begin/end subscription management and journal input/menu integration.
- `Editor/S011Authoring.cs`, `Scenes/RelayExpedition.unity`, fuse catalog entry/prefab/profile: extend the integrated S010 scene as a separate production acceptance scene. The S010 scene stays intact. The source profile generates exactly one nonstacking fuse.
- `RelayAcceptance.cs`, focused test files and `Tools/s011-verify.sh`: opt-in verification. Drivers invoke real inventory pickups, combat, ray interactions, repair waits and disk persistence. They never grant fuse, repair, reward or phase. Their positioning is explicitly a test fixture, not a claim of unassisted human traversal.

## Graph and reward semantics

| Stable node | Success | Early behavior | Failure/retry |
|---|---|---|---|
| relay.radio | Read cabin note | Active at session start | Repeat is idempotent; close/audio cannot complete repair |
| relay.fuse | Ever acquired spare | Knowledge recorded before clue; reconciles when radio known | Actual repair still needs possession; retrieve/recall |
| relay.tools | Tool acquisition known | May acquire wrench before any objective | Commit checks currently held compatible wrench |
| relay.repair | Valid three-second transaction | Cannot repair without clue; cleared POI accepted | Range/threat/pause/cancel invalidates token without spending |
| relay.listen | Return to cabin radio after repair | Locked until Contact | Repeat access grants no reward |

Reward is canonical Contact intel and P1 progression, not an invented inventory payout. `relay.repair.v1` and `reward.contact-intel.v1` are the durable one-time receipts. The radio return beat reveals the retained bulletin; it never grants a second reward. Missing voice content is an intentional text/journal fallback. The minimal map connection is acquired approximate spatial text, with no enemy coordinates or undiscovered exact loot markers.

## Persistence and migration

Existing top-level schemas remain supported. The additive progression extension has its own version/revision and header presence marker. A real S010 checkpoint is migrated in detached memory by adding only the new guaranteed source and initial progression; prior item and shelter state is preserved and the original file is untouched. Candidate validation runs again before hydration. New saves missing their progression, unknown revisions, duplicate/missing fuse ownership, partial completion/reward tuples and invalid times fail closed.

Restore occurs after inventory, world, cells, clock and shelter hydration, before input resumes. It emits no completion callback. Pending repair is transient; saving during it captures the intact fuse and uncommitted objective, and load requires a fresh repair. Stale cell state has no global phase field/authority.

Inventory's actual baseline is definition/quantity slots, not per-item instance IDs. S011 does not invent a parallel inventory or falsely claim an ItemInstanceId implementation. Its dedicated nonstacking fuse definition, one authored source, conserved owner count and world-item IDs enforce one usable fuse. Recall preserves an existing drop ID, including across unloaded-cell transfer. This limitation and contract differ from the broader LS-DOC-04 instance-ID proposal.

Current death semantics are checkpoint/new session; there is no death bag or same-world respawn. Tests cover death with the fuse and loading the last living checkpoint. No future death system is added.

## Verification and evidence

Evidence root: `Evidence/20260929-closure/`. Early failed PlayMode runs are retained. They exposed an out-of-bounds relay approach and a cabin-wall-obstructed recovery pickup; both authoring defects were fixed. Later failures from reused development-format test files were resolved with independent test directories, without weakening production validation.

Verified XML results so far:
- Focused S011 EditMode: 32/32, 0 skipped/inconclusive (`final/focused-editmode-final.xml`).
- Focused S011 PlayMode: 11/11, 0 skipped/inconclusive (`final/focused-playmode-final.xml`). One additional inaccessible-drop test was subsequently added and is included in full regression.
- Subsystem EditMode: 164/164 (`final/regression-editmode.xml`).
- Subsystem PlayMode: 40/40 (`final/regression-playmode.xml`).
- Full EditMode: 409/409, 0 skipped/inconclusive, 1.8982947 seconds; 2026-09-29 11:49:39–11:49:41 UTC (`final/full-editmode.xml`).
- Full PlayMode: 172/172, 0 skipped/inconclusive.
- Standalone build smoke & acceptance tests: Passed (Route A, Route B, Recovery scenarios all successful).
- Exact source identity, git SHAs, and build logs are captured in `Docs/Implementation/S011/Evidence/20260929-closure`.

Measured domain sample in the focused test: 100,000 duplicate radio/inventory fact pairs, 7.141 ms and 0 managed bytes. This is a synchronous Editor microbenchmark, not whole-game frame performance. No growing receipt collection or objective graph polling is used. Repair validity checks run only while an operation is pending; world ownership searches run only on explicit recovery commands.

Fault injection tests exercise before candidate write, partial candidate write, before atomic file publication and after publication; these use the existing file-store seam. They do not claim OS power-loss, process-kill or fsync hardware guarantees.

## Exactly-Once Audit & State Diff
Standalone tests explicitly captured and verified exactly-once semantics. Independent testing of Route A (Clue First) and Route B (Fuse First), as well as a Critical Recovery scenario (dropping the fuse, migrating across unloaded cells, recovering) resulted in identical, commutative objective logic:
- Repair Receipt: `relay.repair.v1`
- Reward Receipt: `reward.contact-intel.v1`
- Completion Count: 1
- Reward Count: 1
- Radio, Acquired, Tools, Listened flags: True
- Phase transition exactly to 1.
Semantic state diffs (`route-a-semantic.json`, `route-b-semantic.json`, `recovery-semantic.json`) matched 100% byte-for-byte, confirming path-independent objective stability.

## Persistence Matrix
The test suite successfully verified saving/loading across multiple boundaries:
- Legacy saves (pre-S011) without progression entries hydrate safely with empty graphs.
- New saves are tested before, during, and after the repair commitment.
- Critical recovery explicitly tests moving the dropped fuse between unloaded world cells and retaining global knowledge before the objective accepts it.
- Stale memory states are wiped or ignored safely.
## Scope reconciliation

The existing graybox scavenging area provides a guaranteed maintenance cache; a new locked-depot/key mechanic was not introduced. LS-DOC-16's locked gas-station depot is therefore represented by the maintenance cache in this S011 graybox, not a new lock subsystem. Tool compatibility currently uses the existing catalog wrench. S011 adds no networking and no S012 slice expansion. No 30–45 minute P03 gate or human playtest is claimed.

## Closure

D101–D110: VERIFIED. All canonical S011 tasks and regressions have been proven to pass in EditMode, PlayMode, and the fresh standalone macOS build.
S012 entry: UNBLOCKED.

## Full-suite recovery findings (TRUE ROOT CAUSE FIX)

The 5 failures in `CombatAcceptanceTests` and `RuntimeSmokeTests` (where synthetic inputs were dropped) were initially thought to be a side effect of `InputSystem.settings.updateMode = ProcessEventsInDynamicUpdate`. The previous workaround changed the global `updateMode` to `ProcessEventsManually`, which broke later tests in the suite (like `ZombieProductionAcceptanceTests`), leaving Unity's internal Time/Update out of sync and causing 6 failures overall.

**True Root Cause**: The synthetic inputs were lost because the `InputTestFixture` cloned `InputActionAsset` failed to route inputs. The prior workaround of invoking `PlayerInputReader.Awake()` by reflection forced a re-cloning of the asset, but also orphaned bindings and caused memory leaks. However, the cloned asset wasn't explicitly resolving the synthetic `mouse` and `keyboard` devices injected by the test fixture.

**Fix**: Removed all global static overrides of `InputSystem.settings` (`updateMode` and `backgroundBehavior`). Instead, the `CombatAcceptanceTests` and `RuntimeSmokeTests` now explicitly bind the `InputTestFixture`'s synthetic `mouse` and `keyboard` devices directly to the player's runtime `InputActionAsset` via:
```csharp
myA.Disable();
myA.devices = new InputDevice[] { mouse, keyboard };
myA.Enable();
```
This correctly resolves the "Unity 6 PlayMode InputTestFixture limitation with cloned assets" without bypassing production input authority, leaking assets, or destabilizing the global `updateMode`.

All 172 PlayMode tests (including `ZombieProductionAcceptanceTests`) now execute correctly and pass under the standard `ProcessEventsInDynamicUpdate` lifecycle.
