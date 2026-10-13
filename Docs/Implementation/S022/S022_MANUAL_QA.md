# S022 manual acceptance and performance

Status **NOT_RUN**. No playtester responses, screenshots, timing or performance numbers have been fabricated.

After automated gates/build: open `Assets/LastSignal/Scenes/Validation/S022ArchetypeAcceptance.unity`, enter Play, start/resume the existing session. Use Escape/pause for the cursor and bottom S022 route buttons, then resume. This is a separate clone of ZombieAcceptance with its baked navigation and a route component; production scenes are untouched. Route actors spawn at most once per 0.1s. They are explicit validation spawns, not production visibility-safe materialization. Selection clears the old validation cast, hides the resident, and restores the resident when leaving the route. Runtime walls/cutouts are temporary, with NavMeshObstacle carving. A BLOCKED message means the fixture failed and must be reported, not accepted.

## Repeatable learning route
1. **Shambler:** alone, walk backward/outside its committed arc. Recognize slow sustained threat.
2. **Lurker:** alone, approach from 8m to 4m; inspect bent/asymmetric posture and listen before contact. Retreat during .85s preparation.
3. **Runner:** alone in the open; observe .65s preparation, limited burst, then recovery. Break LOS around the existing wall.
4. **Interior Lurker:** select interior preset; scan the temporary doorway/corner, keep one retreat route. Compare at common sun intensity and a darker scene-light setting in Play only. Do not save altered production lighting. This is a greybox interior fixture, not finished environment art.
5. **Open Runner:** deliberately force turns and exploit recovery; compare against available player sprint/stamina.
6. **Mixed:** all three, then vary positions around existing obstacle and corner wall. Kill or stagger one during commitment. All damage must have a visible cause.
7. **Doorway mixed:** retreat through the 1.5m opening, test cornering, losing/reacquiring sight, no penetration, no permanent jam, no same-frame multi-attacker damage. Use the explicit Pair Shambler / Pair Lurker / Pair Runner buttons for two-of-one-type comparisons; reselect to reset.
8. **Save/load:** select `8 Save/load resident` before using the existing session save/load UI. Then open `WorldPopulationAcceptance.unity` for streamed mixed population: enter cell:1:0, allow hidden materialization, wound a Runner/Lurker, unload/reload, save/load, kill it, repeat. Record stable ID/type/health/anatomy/tombstone and unchanged total. The ad-hoc validation cast is intentionally not an extra persistence authority; mixed persistence uses the production population manager and its focused PlayMode test.

Repeat warning/contact observations at Low and normal quality, reduced gore on/off, and audio zero/normal. Audio volume must not change enemy sensing. Record low-quality screenshots of all three at identical distance/light. Compare shoulder/head posture, locomotion rhythm, clip grounding, skinning, hands, melee origin/contact, body-part wounds and corpse settling. Current variants have labeled temporary shared-model visuals; separate-model acceptance is not claimed.

## Feedback form (one row per scenario/person)
- Run ID / build or Editor / date / hardware / quality / input / prior familiarity:
- Type recognized before damage? Which visual/audio cue?
- Attack warning understood? Estimate usable reaction time; attach clip timestamps.
- Damage cause understandable? Expected hit or unexplained hit? State/sequence/contact result:
- Escape possible? Route attempted, obstacle and stamina:
- Countermeasure discovered without coaching? Describe:
- Frustration and concrete cause (speed, warning, congestion, contact, learning):
- Visual/audio consistency and low-quality readability:
- Save/load IDs/type/health/dead state/loot ownership before and after:
- Verdict KEEP / TUNE / DEFER / CUT, supporting evidence, blocker severity:

## Performance procedure — 10 / 20 / 30 nearby mixed actors
Use route buttons `10/20/30 mixed benchmark`; record actual active count, never just requested count. Warm up 15s, capture 60s per count on the same machine, quality, resolution and camera route. Keep the player alive by moving/retreating; report early termination. Repeat with doorway and indoor occlusion by adding the same temporary obstacle arrangement in the validation scene, then a 10-cycle cell:1:0 ↔ cell:2:0 streaming route in WorldPopulationAcceptance. Do not turn a logical-only population mode into a physical crowd result.

Capture Unity Profiler CPU Timeline and memory/GC with Deep Profile off initially: `LastSignal.Zombie.AI`, `.Navigation`, `.Perception`, `.Attack`, `.MeleeValidation`, `.Presentation`, `.Damage`; AI CPU and frame-time median/p95/max, warmed-tick GC, raycast deltas (`Perception.Raycasts`), path-request deltas (`Navigation.PathRequests`), active NavMesh agents, active ProductionAudio voices, spawn/teardown costs and streaming spikes. Optional Editor `MeasureManagedAllocations` is already available on each controller. Record sample duration and actor count when interpreting counters. Compare equal counts/types across the same route; no performance gain is claimed before measurement.

Create a new timestamped evidence directory; keep profiler `.data` captures, scenario/count/settings/hardware notes, route video, screenshots, observer responses and before/after population snapshots. Avoid writing into old evidence directories. Capture at least one session restart and reduced-gore/audio comparison. If a scene grid/asset fails, record BLOCKED and return its exact message.

## Provisional D220 decision
Shambler: KEEP candidate subject to regression. Lurker: TUNE candidate pending warning/recognition/interior acceptance. Runner: TUNE candidate pending escape/recovery/crowd/performance acceptance. Temporary shared-model visuals: DEFER final art approval. Audio release: DEFER until licensing evidence is resolved. Cut/defer an archetype if correctness or maintenance cost remains unacceptable; three names are not a release goal by themselves. None of these are final acceptance decisions.
