# BLD-S005 open risks and rollback

> Current status (2026-10-25): **COMPLETE — owner-authorized sprint closure.** All five automated gates passed (56 + 4 + 104 + 96 + 39 = 299 tests). Full focused-play evidence: `Evidence/OWNER-20261025-144648-focused-play-vmqb8zu2/`. Per-command crash durability, manual gameplay/presentation acceptance, production balance and performance measurement remain documented follow-up scope; closure does not claim these were verified. G2 overall acceptance is separate. Earlier statuses below are historical.

> Update 2026-10-25: owner results supersede the initial TEST_NOT_RUN statements below: focused-edit 56/56, integration 104/104, regression-edit 96/96, regression-play 39/39 passed; focused-play 2/4 failed. Two fixture corrections are IMPLEMENTED / TEST_NOT_RUN. Current evidence and next focused retest: [OWNER_VERIFICATION_20261025.md](OWNER_VERIFICATION_20261025.md). Original handoff below is retained as implementation history.

Overall: PARTIAL / TEST_NOT_RUN. No owner acceptance is recorded.

| Scope | Evidence / limitation | Required next action |
|---|---|---|
| D038 command durability — BLOCKED | StartCraft returns in-memory receipts. Existing SaveSession publishes explicit full generations; no prepared generation coordinator connects each craft command to durable publication and disk-failure rollback. RequireDurableGeneration requests return DurabilityUnavailable before mutation. | Keep explicit checkpoints for this implementation. Before enabling crash-durable commands, implement the shared generation coordinator and owner-verify disk failure, restart, lost response and retry. This cannot be accepted by changing a status label. |
| D035 native route — PARTIAL | Prototype assets and isolated authoring menu exist; no new stage was authored or played by the agent. | Owner gates, followed by isolated stage creation and real physical gathering → deposit → craft → collect/save/load route. |
| D033–D040 runtime verification | Standalone source compilation is not Unity import, serialization or test execution. | Owner runs focused-edit first; review evidence before later gates. |
| Economy authority | Log-to-plank costs/duration are explicitly prototype values in one recipe asset. | Owner approves balance before production promotion. |
| G1 prerequisite | Later physical-tree test evidence exists, but does not prove current full visual/audio/production acceptance. | Retain existing gathering clearance gates and complete human presentation acceptance. |
| Lifecycle/checkpoint scope | Unsaved crash/session teardown returns to the last checkpoint; saving still requires living-player state. | Do not infer autosave or unsaved-state recovery. |
| Contract scope | One selected source; mixed-container aggregation rejected. No favorite-item field exists. Tool is non-consuming. Cycles all rejected. Handcraft has time semantics but no standalone portable UI. | S006 can consume these contracts; any broader ownership/economy policy requires explicit implementation and tests. |
| Performance | NOT_MEASURED; receipt history bounded at 32 and recipe amounts at 32, but native allocations/frame time unmeasured. | Measure in an owner-approved native session before performance acceptance. |

## Rollback

No rollback was executed. No commits, resets, stashes, package operations or scene promotion were performed. CHANGED_FILES.md is the scoped manifest; PRESERVATION.json records original hashes and pre-existing dirty paths. Preserve the owner's current work and compare any later edits before reverting only this manifest's changes. Do not use blanket git reset/clean or restore the entire project.

An old executable must not load a save containing an active S005 job/receipt extension: it may ignore ownership-bearing new fields. Preserve save files and use an owner-selected pre-S005 valid generation with the matching old executable, or finish/refund work and explicitly prepare a compatible migration. Do not delete current generations as an automatic rollback. The isolated stage authoring tool uses its own save slot; no stage was created during this task.
