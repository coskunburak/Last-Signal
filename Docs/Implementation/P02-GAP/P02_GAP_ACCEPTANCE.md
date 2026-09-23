# P02-GAP acceptance — closure in progress

Execution label: P02-GAP / Canonical S008 Completion. Historical implementation S008 remains the shelter loop. No human playtest is claimed.

Evidence root: `Evidence/20260922-entry/`.

| Gate | Actual result so far |
|---|---|
| Entry EditMode | 204/204, zero failed/skipped/inconclusive |
| Entry shelter/persistence PlayMode | 15/15, zero failed/skipped/inconclusive |
| Focused domain | 23/23 after documented legacy-JSON correction |
| Focused scene integration | 10/10 after documented fixture corrections |
| Full EditMode | 227/227, zero failed/skipped/inconclusive |
| Full PlayMode | Running; not yet PASS |
| Fresh macOS Development build | NOT_RUN |
| Standalone | NOT_RUN |
| Player.log and visual review | NOT_RUN |

## Comparison tolerances

30/60/120 FPS-equivalent 60-real-second streams vs 60-world-minute bulk: absolute time 1e-5 world seconds, wetness and transition progress 1e-6, integrated healing 1e-5 HP. RNG/scheduled transition identity is exact after snapshot restoration; generated schedule comparison uses 1e-6 seconds for tick-stream comparisons. Physics is outside this equivalence claim. Large timestamp fixture uses 9e11 world seconds and retains an exact added minute. Multi-day fixture crosses five midnights. Performance fixture advances 100 days analytically through events.

## Acceptance route honesty

WorldTimeAcceptanceRoute uses actual SessionFlow/ShelterLoop/WorldItem/PlayerHealth/SaveSession APIs in the production-derived scene. It positions the player at authored points (not a recorded manual WASD traversal), prepares, leaves, picks up generated loot, advances to rain, returns under the physical marked roof, clears the existing nearby encounter through production damage authority, rests through weather boundaries, saves, performs a second expedition/return, loads and checks inventory/stash/ammo/time/weather/consumed loot. Separate live scene tests prove active threat rejection and interruption. Synthetic death/fuel boundary fixtures are domain scheduling tests, not implemented bleeding/generator gameplay.
