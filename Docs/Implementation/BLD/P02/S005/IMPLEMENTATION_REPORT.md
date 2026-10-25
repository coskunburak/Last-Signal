# LAST SIGNAL — BLD-S005 implementation

> Current status (2026-10-25): **COMPLETE — owner-authorized sprint closure.** All five automated gates passed (56 + 4 + 104 + 96 + 39 = 299 tests). Full focused-play evidence: `Evidence/OWNER-20261025-144648-focused-play-vmqb8zu2/`. Per-command crash durability, manual gameplay/presentation acceptance, production balance and performance measurement remain documented follow-up scope; closure does not claim these were verified. G2 overall acceptance is separate. Earlier statuses below are historical.

> Update 2026-10-25: owner results supersede the initial TEST_NOT_RUN statements below: focused-edit 56/56, integration 104/104, regression-edit 96/96, regression-play 39/39 passed; focused-play 2/4 failed. Two fixture corrections are IMPLEMENTED / TEST_NOT_RUN. Current evidence and next focused retest: [OWNER_VERIFICATION_20261025.md](OWNER_VERIFICATION_20261025.md). Original handoff below is retained as implementation history.

25 Ekim 2026. **IMPLEMENTED / TEST_NOT_RUN** for the domain implementation; overall **PARTIAL** pending owner gates, native integration and the explicitly blocked per-command durability path. No Unity tests, Unity builds, gameplay automation or native authoring commands were executed. No commits or pushes.

## Baseline and scope

Branch `s012-integrated-graybox-slice`, entry HEAD `a446d8d9daab09bdb7a9f99332cd1d51ad181581`. Unity `6000.5.0f1`; HDRP `17.5.0`; active graphics pipeline GUID `15722f09b35214c7cac8dee7a2a4c2e3` resolves to `Assets/LastSignal/Settings/Rendering/HDRP/LastSignal_HDRP_High.asset`. Entry has 722 Git status records, including owner tree-package deletions and WorldReconstruction additions. All pre-existing status records remain present. Scoped modified source files were clean at entry. See `PRESERVATION.json`, `CHANGED_FILES.md`, `STATIC_INSPECTION.json`.

No existing scene, prefab, item/recipe asset, package, rendering setting or GUID was changed. The new prototype has its own item definition, recipe and simple drop proxy. No package or marketplace research/import was needed.

## Existing versus new architecture

| Existing authority | S005 extension |
|---|---|
| `InventoryContainer` owns quantities/revisions | Internal `ExchangeBatch` validates complete detached slot state, commits once, publishes once |
| `ShelterProduction` owns S010 jobs, fuel/modules and world time | Partial implementation owns a mutually exclusive multi job and bounded receipts; no parallel job owner |
| `ShelterRecipe` and legacy `CraftSnapshot` | Existing semantics retained; separate `RecipeDefinition` and compiled `RecipeCatalog` |
| `ShelterSite` resolves player access/storage | Same selection/start/cancel/collect panel includes opt-in new recipes; `SubmitCraft` resolves trusted container IDs |
| `SaveSession` captures full inventory + production generation | Same capture/hydrate path; additional catalog validation, optional nested snapshot and codec normalization |
| `WorldSimulation` schedules workstation/fuel boundaries | New jobs join the existing owner; simulation-time recipes use a separate contiguous session simulation clock |

`RecipeDefinition` supplies explicit revision/catalog compatibility, sorted inputs/outputs, non-consuming tools, selected/player/storage source policy, handcraft/workbench station, bounded capabilities, existing shelter unlocks, time basis and power requirement. Compiled quantities and requirements cannot be changed by modifying an authoring array while a job runs. Recipe fingerprint mismatch rejects incompatible hydration even when an author forgot to increment revision. ItemDefinition has no invented revision field.

A request uses one explicit container. Multiple stacks and multiple ingredient/output types are supported. Mixed player/storage aggregation is unsupported: conflicting policies reject before transfer. The current inventory has no favorite attribute. Existing `quest.*`/`item.quest.*` identities cannot be recipe inputs. No new durability/charge cost is applied to tools.

Cycle validation includes legacy edges whenever multi recipes are installed. All cycles, including apparently lossy/ambiguous transformations, require a future approved economy policy; none are whitelisted. Null/aliased IDs, duplicates, zero costs, quantity/stack bounds, invalid times/revisions/stations/unlocks/tools and unsupported compatibility versions have typed errors.

## Eight-card acceptance matrix

File references below are under `Assets/LastSignal/Scripts/` unless data/tool paths are given. Exact paths including metadata are in `CHANGED_FILES.md`.

| Card | Implemented symbols / files | Added tests / expected assertions | Limitation and verification |
|---|---|---|---|
| D033 | `RecipeDefinition`, `CompiledRecipe`, `MultiCraftSnapshot`; Runtime/Shelter/RecipeDefinition.cs, RecipeCatalog.cs, MultiCraftState.cs, ShelterRecipe.cs | P02RecipeValidationTests; P02CraftPersistenceTests legacy empty/running wire fixtures remain readable | IMPLEMENTED / TEST_NOT_RUN; forward loading of old saves, no downgrade guarantee |
| D034 | `RecipeCatalog.Amounts`, `ShelterProduction.Requirements`, `ShelterSite.SubmitCraft` | P02CraftingTests: missing C leaves A/B/revision unchanged; multi-stack accounting; source/tool/unlock/station gates | IMPLEMENTED / TEST_NOT_RUN; one selected source; no favorites contract exists |
| D035 | PrototypeLogToPlank.asset, PrototypePlank.asset/prefab; `S005CraftAuthoring.CreateStage`; same ShelterSite UI | Prototype asset validation; P02CraftIntegrationTests: actual inventory → storage → escrow → plank → single collect | PARTIAL; prototype balance only; native stage tool prepared but not invoked; gameplay acceptance pending |
| D036 | `RecipeCatalog.Validate/TryCreate` | P02RecipeValidationTests: valid catalog, invalid entries, alias, zero/overflow, self conversion, multi/legacy cycles | IMPLEMENTED / TEST_NOT_RUN; ambiguous cycles fail closed |
| D037 | `InventoryContainer.ExchangeBatch`, `StartCraft`, `CancelMulti`, `CollectCraft`, `ProgressMulti`; Inventory/ItemAmount.cs; ShelterProduction.cs/.Multi.cs | P02CraftingTests: staged failure, observers/reentrancy, pending refund/output, full target, duplicate collect; PlayMode death/disable fixtures | IMPLEMENTED / TEST_NOT_RUN for in-memory lifecycle; no new autosave/quit guarantee |
| D038 | `CraftRequest`, `CraftIdentity`, `CraftReceipt`, `WithReceipt`, `CraftHighWater`, `CraftEpoch` | P02CraftingTests and persistence: same request returns detached result; conflict/expired/old-session rejection; published-save retry never spends twice | PARTIAL; persisted checkpoint receipts implemented, command-level durable publication BLOCKED and explicitly rejected when requested |
| D039 | `ApplySimulationElapsed`, `MultiBoundary`, `ApplyMultiWorld`, `SuspendCraftClockUntil`; ShelterSite.Update/disable/enable | P02CraftTimeTests: pause, sleep/skip, half-power pause/refuel, repeated timestamp rejection, cancellation, frame partition, restore | IMPLEMENTED / TEST_NOT_RUN; no wall-clock/offline progression; handcraft outside existing panel belongs to S006 |
| D040 | P02CraftFixture, four EditMode fixture classes, P02CraftIntegrationTests; Tools/bld-s005-verify.py | Gate 1 source declares 56 cases; persistence declares 24; native fixture 4. Counts are not Unity discovery/pass evidence. Existing S010/storage/gathering/time tests mapped | IMPLEMENTED / TEST_NOT_RUN; every Unity gate and performance remains unverified |

## Transaction and ownership contract

1. Reject invalid session identity, nonce, shape, source, busy state or an unsupported durability request.
2. Resolve retained nonce before checking current inventory revision. Same payload returns its stored receipt; a conflicting payload rejects. Nonces at/below the retained high-water mark that have aged out return `ReceiptExpired` permanently for that saved epoch.
3. Validate the compiled recipe, module/capability/unlock, source revision, tools and **all** inputs.
4. Prepare job, receipt, output plan and next state before live mutation. Inventory stages all removals/additions on cloned slots. A failure changes no live quantity or revision; valid rejections may persist only their receipt.
5. Under `OwnershipTransaction` and the container/production busy guards, swap slots and install the prepared domain state. Commit delegates are bounded field assignments; rollback restores slots, revision and domain reference if a delegate throws.
6. Increment revision once and notify only after both owners are coherent. Reentrant commands reject; snapshot capture rejects. Observer exceptions are logged individually and do not undo committed ownership or suppress later observers.

Running/refund states own only `escrow`. Completion removes that ownership and creates `pendingOutput`; the snapshot never owns both. Cancellation never spawns items. Refund targets must use the original source ID; output may go to either supported destination. Full capacity/stale revision preserves the complete payload. After collect, the job is null; retry cannot mint another output. The single existing job sequence also spans old/new jobs, which cannot run together. Workbench upgrades apply to future workstation jobs and are blocked while any job is outstanding.

## Persistence and lifecycle

Existing `ShelterProductionSnapshot.version=1`, all S010 fields and the outer save schema are unchanged. Production adds `craftingVersion` plus `crafting`; containers additionally carry optional `revisionVersion` and `revision` fields. Version zero means absent; version one contains persisted epoch, nonce high-water, at most 32 receipts, simulation clock and optional multi job. Exact zero/null Unity representations are normalized; unversioned populated data and unknown versions fail closed. Receipts hold canonical payload hashes, requested/observed before/after revisions, outcome, job/sequence and consumed metadata. Receipt metadata does not own items.

A complete `SaveSession` snapshot includes inventory, storage, their revisions, production/escrow/output and receipts in the same existing save generation. New captures persist version-one container revisions; legacy missing fields remain accepted. Hydration restores the saved revision exactly, so an old expected revision cannot accidentally become valid after reload. A craft generation without persisted player/storage revisions or with a receipt ahead of its source revision is rejected. Restore validates shape/known item bounds and the current compiled recipe before hydration; it does not replay reward events. SaveFileStore still controls publication, previous-generation backup and explicit recovery. Invalid candidates must not replace the saved file; tests cover this using temporary owner-test paths.

These are separate guarantees:

- In-memory atomicity: IMPLEMENTED / TEST_NOT_RUN.
- Consistent snapshot and published-generation retry: IMPLEMENTED / TEST_NOT_RUN.
- Automatic per-command crash durability / rollback on disk failure: BLOCKED; no implemented prepared full-generation craft coordinator. `RequireDurableGeneration=true` returns `DurabilityUnavailable` without quantity, job or receipt changes. No command is silently downgraded.
- Unsaved session exit/crash/death recovery: existing last-valid-checkpoint policy. No automatic save was added, and the living-player save restriction remains. New session epochs reject old callbacks; loading a published epoch restores its retry history.

Closing the panel does not clear jobs. Pause/death stop session clocks. Simulation-time work does not receive sleep or remote-cell elapsed time. World-time work follows existing canonical sleep/catch-up and fueled intervals. Temporary Site disable preserves S005 state, unregisters its clock participation and skips the unavailable interval on re-enable. Full session teardown follows existing checkpoint semantics. Station access while disabled is denied.

## G1 and asset findings

`material.log` is the actual authored S003 item in `Data/Gathering/Pilot-20261018150846-032db4e9/Log.asset`; log stack and mass are retained. No authored plank ItemDefinition existed. New `item.resource.plank` uses the balance catalog's item stack/mass values; conversion quantity/duration live solely in `Data/Crafting/PrototypeLogToPlank.asset`, explicitly prototype. The existing axe is required and retained. The drop proxy is necessary for current SaveSession's material-prefab validation and reuses a project-owned wood material with a built-in cube; no external asset was acquired.

The original G1 receipt is stale as a current engineering summary. `P01/S004/PhysicalTreeCorrection/CURRENT-MCP.md` records later owner-authorized physics, persistence and cell tests. Saved birch and pine stages exist; pine `FirstTownTreePhysics_20261019163434-8e0f05b7.unity` has `clearanceApproved: 1`. This is historical evidence, not S005 native acceptance or approval of the current HDRP/visual/audio state. S005 owner stage creation copies a clean existing scene and creates a new catalog/save slot; it never promotes the stage or edits original gathering assets.

## Static inspection and owner boundary

Standalone Roslyn source compilation returned exit code 0 for Runtime, Editor, EditModeTests and PlayModeTests against local cached references. No test method or Unity process was executed. This is not Unity compilation/import acceptance, test success or a performance measurement. Runner syntax, fixture file names, metadata and scoped whitespace were inspected. FirstTownCore/VisualSlice, package files and rendering references remain equal to entry HEAD.

Next action is only Gate 1 in `TEST_HANDOFF.md`. S006 can inspect the new contracts, but S005/G2 acceptance remains open.
