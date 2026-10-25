# BLD-S005 working state

> Current status (2026-10-25): **COMPLETE — owner-authorized sprint closure.** All five automated gates passed (56 + 4 + 104 + 96 + 39 = 299 tests). Full focused-play evidence: `Evidence/OWNER-20261025-144648-focused-play-vmqb8zu2/`. Per-command crash durability, manual gameplay/presentation acceptance, production balance and performance measurement remain documented follow-up scope; closure does not claim these were verified. G2 overall acceptance is separate. Earlier statuses below are historical.

> Update 2026-10-25: owner results supersede the initial TEST_NOT_RUN statements below: focused-edit 56/56, integration 104/104, regression-edit 96/96, regression-play 39/39 passed; focused-play 2/4 failed. Two fixture corrections are IMPLEMENTED / TEST_NOT_RUN. Current evidence and next focused retest: [OWNER_VERIFICATION_20261025.md](OWNER_VERIFICATION_20261025.md). Original handoff below is retained as implementation history.
Status: IMPLEMENTED / TEST_NOT_RUN (implementation handoff; no acceptance).
Baseline: s012-integrated-graybox-slice / a446d8d9daab09bdb7a9f99332cd1d51ad181581. Unity 6000.5.0f1 / HDRP 17.5.0; GraphicsSettings pipeline GUID 15722f09b35214c7cac8dee7a2a4c2e3.
Entry has unrelated Realistic Tree deletions and WorldReconstruction additions. Scoped existing source files clean. Full entry status and hashes: PRESERVATION.json.

| Existing | Required addition |
|---|---|
| ShelterRecipe single input/output, world seconds | Separate versioned RecipeDefinition; immutable compiled catalog |
| ShelterProduction single escrow owner | Same owner, mutually exclusive additive multi job and receipts |
| InventoryContainer.Exchange stages one pair | Atomic batch on detached slots; one revision and notification |
| SaveSession captures production with inventory | Versioned nested extension and source revisions validated by SaveValidation and topology |
| ShelterSite storage selection and access checks | New recipes in same panel; trusted source resolution |

Decisions: explicit single selected source per request (player inventory or shelter storage); mixed-source aggregation unsupported and rejected. Tools non-consuming. Cycles conservatively rejected including legacy cross-edges. Bounded monotonic nonce receipts plus high-water tombstone; old evicted requests never execute again. New progress explicit world/simulation seconds. No separate job authority.
Verified APIs: InventoryContainer.Exchange/PublishTransfer, OwnershipTransaction; production Capture/CanRestore/Restore; SaveSession.ValidateTopology; WorldSimulation participants.
G1: later S004 physical-tree/cell/persistence owner evidence exists in PhysicalTreeCorrection/CURRENT-MCP.md; current full production visual/audio acceptance is not claimed. S005 native acceptance is pending.
Durability: consistent explicit save generations only; per-command crash durability not available/claimed. Autonomous cross-domain durable commands remain blocked pending integration evidence.
D033–D040 implementation and scoped fixtures are prepared. D035 native gameplay acceptance and D038 per-command durability remain PARTIAL. All S005 Unity tests/builds are TEST_NOT_RUN; performance NOT_MEASURED. Standalone source compilation succeeds for four assemblies; this is not Unity/test evidence. Owner results: none for S005. Next: owner runs focused-edit only using TEST_HANDOFF.md, then returns evidence for review.
