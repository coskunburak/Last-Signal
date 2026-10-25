# BLD-S005 — owner test handoff

> Current status (2026-10-25): **COMPLETE — owner-authorized sprint closure.** All five automated gates passed (56 + 4 + 104 + 96 + 39 = 299 tests). Full focused-play evidence: `Evidence/OWNER-20261025-144648-focused-play-vmqb8zu2/`. Per-command crash durability, manual gameplay/presentation acceptance, production balance and performance measurement remain documented follow-up scope; closure does not claim these were verified. G2 overall acceptance is separate. Earlier statuses below are historical.

> Update 2026-10-25: owner results supersede the initial TEST_NOT_RUN statements below: focused-edit 56/56, integration 104/104, regression-edit 96/96, regression-play 39/39 passed; focused-play 2/4 failed. Two fixture corrections are IMPLEMENTED / TEST_NOT_RUN. Current evidence and next focused retest: [OWNER_VERIFICATION_20261025.md](OWNER_VERIFICATION_20261025.md). Original handoff below is retained as implementation history.

Status: **IMPLEMENTED / TEST_NOT_RUN**. Agent did not run this script, Unity, a test suite, a build or native authoring. The runner launches exactly one gate and preserves Unity's nonzero exit code. All commands below are owner-operated. Do not chain gates.

## Run now: Gate 1 only

Save your current Editor work, then close Unity normally. Do not delete project lock files. An activated Unity 6000.5.0f1 installation is required. The runner defaults to `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`; `--unity` can select another matching executable.

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/bld-s005-verify.py focused-edit
```

Assembly `LastSignal.EditModeTests`; exact classes:

- `LastSignal.Tests.P02RecipeValidationTests`
- `LastSignal.Tests.P02CraftingTests`
- `LastSignal.Tests.P02CraftTimeTests`

Source declares 56 cases; actual discovery is for the owner's runner to establish. Expected: every selected case passes, none skipped/inconclusive, no compile/import failure. Safety checks cover missing last ingredient, quantities/revisions, escrow, full targets, receipts, old epoch, rejected durability requirement, invalid catalogs and explicit clocks.

Evidence is a new directory each run: `Docs/Implementation/BLD/P02/S005/Evidence/OWNER-<Istanbul timestamp>-focused-edit-<unique>/`, containing `request.json`, `unity.log`, `tests.xml`, `result.json`. Return the XML, result JSON and relevant log exception/stack trace. Then stop for review.

Focused retry example (only after a failure is diagnosed/fixed):

```bash
python3 Tools/bld-s005-verify.py focused-edit --filter LastSignal.Tests.P02CraftingTests.ThirdIngredientMissingConsumesNothingAndRecordsOnlyRejection
```

The runner accepts a literal class or method from its chosen gate, not arbitrary regex. Parameterized method filters rerun that method's cases. No `--stage`, authoring or build mode is exposed.

## Later gates — wait for preceding results

| Order | Owner command after review | Scope |
|---|---|---|
| 2 | `python3 Tools/bld-s005-verify.py focused-play` | `P02CraftIntegrationTests` in LastSignal.PlayModeTests: real inventory/deposit, actual ShelterSite panel/access, world clock pause, save/load retry, temporary disable, death |
| 3 | `python3 Tools/bld-s005-verify.py integration` | P02CraftPersistenceTests; S010ProductionTests; InventorySnapshotTests; SaveValidationTests; SaveFoundationTests |
| 4a | `python3 Tools/bld-s005-verify.py regression-edit` | ShelterDomainTests; P01HarvestTests; WorldTimeTests; WorldCellTests |
| 4b | `python3 Tools/bld-s005-verify.py regression-play` | S010IntegrationTests; S020CatalogPlayTests; P01HarvestPickupTests; PersistenceIntegrationTests; WorldTimeIntegrationTests |
| 5 | Manual isolated gameplay route below | Physical gathering prerequisite, station route, save/reload and item accounting |

Each gate has its own evidence directory. A failing native scene prerequisite is a fixture/authoring failure to investigate; never bypass clearance, missing bindings or save validation to pass. Wider regressions are not requested before focused failure resolution.

## Invariant fixture contract

`P02CraftFixture` creates a separate donor for module costs, then the test source contains A=6 scrap, B=4 cloth, C=2 water, tool=1 axe, output=0 rifle ammunition. The fixture recipe consumes two of each A/B/C and produces three output. These are synthetic test quantities, not log/plank balance. `r` denotes the captured source revision after setup; commands carry the actual epoch, recipe revision and explicit source ID. Unless overridden, payload is nonce 1/current r/storage/fixture recipe. All assertions check exact quantities, not success flags alone.

| Safety test or cases | Initial/request variation | Expected status, source / escrow / pending output | Revision / ownership / save change |
|---|---|---|---|
| ThirdIngredientMissingConsumesNothingAndRecordsOnlyRejection | C removed before r; nonce1/r | MissingIngredients; 6/4/0+tool; no escrow/output | r unchanged; total 11 units unchanged; rejection receipt only |
| StartCommitsAllInputsAndOneRevisionBeforeAnyObserver | Baseline | Accepted; 4/2/0+tool; escrow 2/2/2; no output | r+1, one notification; 13 owned units including escrow; job+receipt saved |
| ThrowingObserverCannotUndoCommitOrSuppressLaterObserver | Baseline; first observer throws | Same accepted ownership as Start; later observer runs; retry returns accepted | One revision; no rollback/duplication; one job and receipt |
| StaleRevisionNeverConsumesOrCreatesEscrow | Request captures r, then A+1 | StaleSource; 7/4/2+tool; no escrow/output | revision after A add unchanged by request; 14 units; receipt only |
| SameNonceSamePayloadReturnsDetachedReceiptAndDifferentPayloadConflicts | Accepted baseline; retry old payload, then changed revision | Accepted replay / NonceConflict; 4/2/0+tool + original escrow | No further revision/material/job changes; detached receipt cannot mutate authority |
| CancelAndFullRefundDestinationPreserveExactOwnership | Baseline start, half progress, cancel; full separate target | Refund pending with 2/2/2; failed target has unchanged full ammo stack; then source returns 6/4/2+tool | Failed target revision unchanged; total 13 source+escrow units plus target baseline; successful refund r+1 once; null job thereafter |
| FullOutputDestinationRetainsOneResultAndCollectsOnlyOnce | Baseline start and complete; full separate target | CompletedWaitingOutput; escrow empty, pending output=3; then source 4/2/0+tool+3 output | Transformation converts 6 inputs to 3 outputs; total 10 units plus unchanged full-target baseline; failed collect no revision, successful collect one |
| MultipleOutputsAreAllOrNothingWhenLastOutputCannotFit | Add synthetic second output fuel=1; empty one-slot target | DestinationFull; target output=0/fuel=0; both outputs pending | Target revision unchanged; no partial output ownership; job retained |
| MultiStackIngredientsCommitInOneNotification | A += MaxStack; required A=MaxStack+2 | Accepted; A=4, B=2, C=0+tool; full declared inputs in escrow | One revision/event; no partial stack loss; job+receipt |
| EvictedNonceCannotExecuteAfterCollectionOrReload | Cancel/refund nonce1; stale attempts through nonce40; restore | ReceiptExpired for nonce1; 6/4/2+tool; no job/output | No replay mutation; 32 receipts bounded with high-water 40 |
| LegacyAndMultiJobsCannotOwnInputsAtTheSameTime | Start one path, attempt other | Rejection/Busy; quantities match the sole active job | No second removal/revision; no dual escrow |
| GatesRejectWithoutMaterialMutation | Missing station/tool, required upgrade, wrong source policy, wrong recipe revision | Exact typed rejection; source equals pre-request snapshot; no job/escrow/output | r unchanged; receipt only; tool-removal setup excluded from request deltas |
| PriorSessionRequestAndUnknownContainerAreRejectedWithoutReceiptMutation | New epoch; then vehicle source ID | InvalidRequest / UnsupportedSource; baseline source; no escrow/output | r and high-water unchanged; no save change |
| AliasedFinalIngredientAbortsEntireStagedPlanWithoutPartialConsumption | A has a different definition object with same ID | MissingIngredients; baseline quantities remain; no escrow/output | r unchanged after earlier detached removals; rejection receipt only |
| StaleCollectionAndWrongRefundOwnerLeavePendingPayloadUntouched | Cancelled job; r-1 or player ID for storage refund | StaleSource / UnsupportedSource; 4/2/0+tool + escrow2/2/2 | No revision or save mutation; 13 units retained |
| UpgradeCannotChangeActiveJobAndNewWorkbenchJobUsesUpgradedDuration | Upgrade during job; then refund and valid upgrade | Active attempt rejected; next job has upgraded duration and compatible snapshot | No cost for rejected upgrade; valid upgrade uses existing approved cost; next start consumes exactly fixture inputs |
| UnsupportedDurableCommandIsRejectedBeforeAnyAuthoritativeMutation | Baseline; RequireDurableGeneration=true | DurabilityUnavailable; baseline source, no escrow/output/receipt | r unchanged, 13 owned units; no save change |
| Recipe validation parameter cases | Null/zero/negative/overflow/duplicates/ID/revision/catalog/time/station/tool/unlock/quest/aliases/cycles | Exact RecipeError; no production constructor accepted for unsafe definitions | Existing source revision and quantities unchanged; no new save state |
| P02CraftTimeTests | Baseline or power/handcraft variant; contiguous supplied clocks | Pause preserves 0%/50%; fuel loss preserves escrow; refuel finishes once; simulation ignores world skip; cancellation keeps refund | Time/progress changes only when authorized; no extra inventory revisions or output; repeated catch-up rejected |
| Persistence running/refund/completed/collected | Full inventory+production fixture, encode/decode/restore then old request | One matching job or no job after collect; retained accepted receipt does not consume again | Exact saved source/escrow/output ownership; hydration does not append rewards |
| Legacy fixtures | Pre-S010 missing section; old wire empty/running job without new fields | Old job preserved; no fabricated multi job/receipt | Existing legacy collection behavior and quantities retained |
| InvalidSnapshotCannotReplaceLiveState | Version/payload/dual ownership/revision/hash/status/receipt/high-water/epoch/marker/shelter ID corruption | CanRestore false; Restore rejects; original snapshot and source unchanged | No live or saved mutation |
| PublishedGenerationContainsInventoryEscrowAndReceiptAndInvalidWriteKeepsBytes | Publish before/after start, reject unknown-item candidate, read backup | Current generation retains source4 + escrow2 A and accepted receipt; previous generation recoverable | Invalid candidate leaves current bytes unchanged; no crash-durability claim for unsaved commands |
| InventoryRevisionRoundtripPreservesNewRevisionAndAcceptsLegacyShape | Capture new revision marker; restore after later inventory mutation; also load legacy marker zero | New revision restored exactly; legacy slots load with existing revision behavior | Invalid marker rejected before mutation |
| CraftGenerationWithoutMatchingPersistedSourceRevisionFailsClosed | Valid multi generation; remove source marker or lower saved revision below receipt | SaveValidation rejects candidate | No invalid generation published or live state mutated |
| ChangedRecipeWithoutRevisionBumpCannotReinterpretSavedEscrow | Output definition edited after snapshot | Fingerprint mismatch rejects hydration | Original save and inventory not reinterpreted |

The persistence fixture declares 24 cases, including real inventory-adapter revision restoration and rejection of mismatched receipt/source generations. The 4 native tests add real SaveSession and panel ownership checks, including exact storage revision restoration and stale-request rejection after refund. Temporary fixture catalogs and test save directories are isolated; tests do not save authored scene changes. Native gathering/chopping is not substituted by the inventory fixture.

## Gate 5 owner gameplay route (only after previous gates)

Open a saved, clean, single scene containing the intended ShelterSite and SaveSession. The existing later S004 pine/birch stages have historical physics evidence; confirm the intended scene and current authoring before use. Invoke **Last Signal → BLD → P02 → S005 → Create Isolated Craft Stage**. This creates `S005Craft_<unique>.unity`, a separate catalog and save slot. It does not grant G1 approval, add a new station, change source gathering clearance, alter terrain or promote the copy. The agent has not invoked this menu.

1. Acquire logs through the existing physical tree/pickup path. Confirm the gathering prerequisite and finite yield; do not fabricate clearance or bypass `PhysicsNotReady`.
2. Deposit logs and the required existing tool into shelter storage. Record starting counts. Read conversion quantities/duration from the prototype recipe or panel; the asset is the only balance authority.
3. Claim/install/access the existing storage/workbench with the existing module costs. Select the labeled prototype recipe using Next recipe.
4. Start once. Inputs leave storage once; the tool and unrelated items remain. Repeat the same submitted request through the fixture/API: it must return its original receipt, not consume again.
5. Close the panel so world time can advance. On completion, fill the destination before collection: the result must remain pending. Make space; collect once; record exact input/output deltas.
6. Attempt with a required ingredient/tool absent. Source quantities and revisions must remain unchanged. A rejection receipt may be saved.
7. Start another valid job, cancel during progress and collect the full input refund. A full refund destination keeps the refund pending; repeating cancel/collect creates no extra units.
8. Save while running, while refund-pending and while output-pending. Return to menu and load each published checkpoint; count source plus escrow/pending output and verify one collection.
9. Close/reopen the panel and exercise pause/sleep and a power-required test recipe. Interrupted power preserves progress/escrow; resuming fuel progresses only the powered interval. The prototype itself uses its authored power policy.
10. Record current scene, save slot, recipe revision, before/after counts and actual owner result. Screenshots are useful only for station/physical-tree presentation acceptance.

Per-command crash-durable requests are BLOCKED and explicitly rejected. Normal explicit checkpoint behavior remains available; do not treat an in-memory Accepted receipt as a published disk generation. Do not downgrade a save with active S005 state into an old binary.
