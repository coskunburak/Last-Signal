# S022 test matrix

Gate 1 focused-edit **PASSED, 53/53**; Gate 2 focused-play **PASSED, 28/28**. Gate 3 full EditMode passed **773/773** with the corrected runner, then the DXT/Glock17/Custom9mm finger poses were adjusted again, so Gate 3 must be repeated for these latest assets. The focused pistol PlayMode rerun **FAILED 0/1** during DXT reload: finger 6 crossed the frame after the first pose repair. The static pose reconstruction showed only about 0.8 mm clearance at that segment. All three variant poses now have a larger authored clearance while preserving bone lengths and distal contact; this second repair is **UNVERIFIED in Unity**. Gate 4 full PlayMode previously **FAILED, 426/427** on this pistol anatomy test. Gates 5–7 remain **NOT_RUN**. Gate 0 has no reported outcome.

| Gate | Implemented coverage / procedure | Owner |
|---|---|---|
| 0 Editor preflight | Import new metadata/scene/prefabs, allow ordinary compilation, return Console errors | User; outcome unreported |
| 1 focused-edit | `LastSignal.EditModeTests`: S022ArchetypeTests plus ZombieLogicTests, WorldPopulationManagerTests, ZombieAssetTests | User; PASS 53/53 |
| 2 focused-play | `LastSignal.PlayModeTests`: S022ArchetypePlayTests plus ZombieMeleeTests, WorldPopulationPlayTests | User; PASS 28/28 |
| 3 regression-edit | All LastSignal.EditModeTests and LastSignal.Art.EditorTests | User; PASS 773/773 before second pose repair, rerun required |
| 4 regression-play | All LastSignal.PlayModeTests | User; prior FAIL 426/427; focused pistol rerun FAIL 0/1 during DXT reload; second repair unverified |
| 5 development-build | S022Build.DevelopmentMac delegates to S013ProductionAuthoring.Build; real S013Cabin scene | User |
| 6 manual | Eight-step route, lower quality/gore/audio variations, learning form | User |
| 7 performance | 10/20/30 mixed loads, doorway/indoor queries, repeated streaming; Profiler | User |

New EditMode coverage: canonical IDs/catalog uniqueness, profile health and validation, both clip contact orders/minimum warning, legacy JSON/default health, full SaveCodec roundtrip using unchanged SaveFoundation fixture, death/loot ownership, unknown IDs, bounded burst/preparation/recovery, interruption/LOS policy, lurking/hearing transitions, deterministic selection, contextual risk, typed migration conservation/duplicates, live/dead rejection, prefab Humanoid/anatomy/root-motion bindings, and critical cue references/priority.

New PlayMode coverage: real variant initialization and NavMesh binding, preparation pause, stagger during burst, Lurker warning before contact, teardown, Shambler baseline/death during windup, Runner death during windup, committed miss after retreat, same-target crowd spacing, typed migration and rejected restore non-mutation, actual materialize/dematerialize/rematerialize and tombstone preservation, dedicated mixed-route prefab bindings and restart cleanup. Existing focused suites cover sight memory, 30/60/120Hz contact alignment, LOS/arc rejection, rebind/disable cancellation and population integration. Full regression includes damage/severing/wounds, audio, save, cell and session tests.

Human visual/audio readability, low-quality parity, narrow-doorway fairness, learning response and hardware performance are manual acceptance, not inferred from unit tests. The development build is the real production scene; the dedicated scene is opened separately in Editor. No test pretends to approve alternate-model rigging or unresolved licenses.

Gate 1 PASS evidence: `Evidence/Manual-20261013-084301-focused-edit-c_sld93e/`. Gate 2 FAIL evidence: `Evidence/Manual-20261013-084335-focused-play-nq_6iq9p/`. Both report unchanged source. The earlier Gate 1 failure evidence remains at `Evidence/Manual-20261013-083300-focused-edit-2ctwab4s/`; Unity changed `ProjectSettings/ProjectSettings.asset` in that attempt by removing `SENTIS_ANALYTICS_ENABLED` from Standalone scripting defines. This change was not reverted automatically.

Latest user evidence: Gate 2 PASS `Evidence/Manual-20261013-084809-focused-play-k2_t0o9_/`, Gate 3 PASS `Evidence/Manual-20261013-085012-regression-edit-bnrb3az1/`, Gate 4 FAIL `Evidence/Manual-20261013-085049-regression-play-rbmfazem/`. All three report unchanged source. The failed pistol test was also unresolved in prior TriplePistolIntegration evidence; this is not an S022 gameplay failure. Its previous outer-span approximation now uses exact triangle crossing parity for finger samples, preserving the test's no-penetration invariant while allowing actual mesh cavities.

Focused pistol rerun FAIL evidence: `Evidence/Manual-20261013-091326-regression-play-uqjznd26/` (0/1; DXT finger 6 penetrated the frame; unchanged source). The later pose-asset repair has not been run in Unity. The frame mesh has open cross-sections; the revised test asserts penetration only where a closed section supports an inside/outside parity result.

Latest Gate 3 runner FAIL evidence: `Evidence/Manual-20261013-094252-regression-edit-hs2el_91/`. Unity exit 0 and NUnit 773/773 passed; the sole source change was removal of `SENTIS_ANALYTICS_ENABLED` from Standalone scripting defines by the installed Unity Inference package. The evidence remains unchanged and its runner result remains FAIL. The setting was not rolled back.

Newer Gate 3 PASS evidence: `Evidence/Manual-20261013-094735-regression-edit-8re8azd3/` (773/773; no source change). Focused pistol FAIL evidence: `Evidence/Manual-20261013-094811-regression-play-9z2k66ce/` (0/1; DXT finger 6 penetration during Reloading; no source change). The second pose calibration and clearer failure coordinates in the test occurred after these runs. Rerun the single case first, then Gate 3, full Gate 4 and Gate 5.

Runner fails on zero discovered cases, missing/malformed XML, failed/skipped cases, nonzero Unity exit or unexpected source mutation. Its source report retains exact expected Inference define toggles. Focused runs must include S022 cases and only the configured class filters. No automatic gate chaining. Each new run preserves command/environment/HEAD/status, XML/log, exit codes, source hashes and affected historical evidence through the existing S015 preservation helper. Any failed gate blocks progression until repaired.
