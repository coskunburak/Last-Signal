# S011 card evidence index

Final standalone/build gate is pending. Entries below identify actual implementation and available tests; they do not independently declare sprint PASS.

| Card | Implementation | Automated evidence | Standalone gate |
|---|---|---|---|
| D101 | RelayGraph fixed five-node graph; RelayProgression.Stage | S011PersistenceTests.InvalidGraphRejectsDuplicateMissingAndCycle; BothOrdersReconcileWithoutSecondFuse | A/B return to radio |
| D102 | RelayProgression revision, monotonic facts; InventoryChanged subscription in RelayMission.Begin/End | DuplicateDiscoveryEventsAreBoundedFacts; SessionReplacementDoesNotRetainAcquisitionOrCallbacks | Independent A/B/recovery sessions |
| D103 | RelayPoint radio; retained Clue/Intel text; journal | ClueFirstProductionRoute reread/close; no audio dependency | A/B journal screenshots |
| D104 | Persistent acquired flag; restore reconciliation | BothOrdersReconcileWithoutSecondFuse; FuseFirstProductionRoute with early save/load | Route B |
| D105 | Three-second operation token; actor/session/range/threat/tool validation; InventoryContainer.Exchange | CancellationBoundariesAndNewThreatRetainFuse; ThreatRejectionDoesNotMutateEnemy; route walk-away and stale-token rejection | A/B cancelled then valid repair |
| D106 | One repair/Contact/intel commit; durable stable receipts | CompletedReceiptCannotReplayAfterDetachedRestore; CommittedInventoryObserverSeesCoherentRewardAndFuse; RewardPublicationFaultRetainsOneCoherentGeneration | A/B completed save/load and retry |
| D107 | Existing drop relocation, storage guard, unloaded cell owner transfer | DroppedFuseRecoveryProductionRoute; UnloadedCellFuseRecallPreservesOneOwner; StorageOwnerPreventsRecoveryDuplication; InaccessibleDropRecallPreservesIdentityAndQuantity; DeathReturnsToCheckpointWithoutInventingRecoveryBag | Recovery route |
| D108 | Knowledge-gated journal and approximate spatial hints | Routes assert unknown clue hidden, then retained after discovery/load | Radio journal screenshot |
| D109 | Precommit save; cleared POI; text fallback; global phase independent of cells | StaleCellHydrationCannotRollBackContact; RealS010CheckpointMigratesWithoutChangingOriginalFile; malformed DTO and fault tests | A/B pending and complete save/load |
| D110 | RelayAcceptance production commands, no resource/progression grants | ClueFirstProductionRoute / FuseFirstProductionRoute | Fresh macOS build A/B plus semantic convergence required |

Test artifacts are in `Evidence/20260929-closure/final/`. Runtime implementations are in `Assets/LastSignal/Scripts/Runtime/Objectives/`; tests in `Assets/LastSignal/Scripts/Tests/`.
