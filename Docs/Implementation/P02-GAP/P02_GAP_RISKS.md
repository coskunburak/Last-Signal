# P02-GAP risks and scope boundaries

- S2 pre-existing canonical S007 gap: no two-cell streaming, CellReady, stale cell callbacks or cross-cell navigation. Full canonical S009 entry remains blocked by that prerequisite even when the time foundation passes.
- S2 simulation limitation pending S009: physical enemy navigation is not analytically fast-forwarded. Nearby living enemies reject sleep and live/new registered threats interrupt it; distant group travel requires the S009 ETA participant. No claim that distant AI traverses hours of navmesh during a sleep coroutine.
- S3 art/readability: greybox rain and sun profiles require human review. Agent image inspection is separate evidence; no human playtest claimed.
- S4 tuning: initial rest healing and weather durations are authored defaults, not a completed survival economy. No stamina, bleeding, fuel, crafting or population feature is implied by boundary fixtures.
- Existing local dirty work is preserved and no release commit is created. Evidence hashes describe the tested tree, not a clean checkout release candidate.

## Failure ledger

- Focused domain run 1: 22/23. Unity JsonUtility materialized a null new section on legacy decode. Fixed codec normalization for schema 1 after checksum verification; rerun 23/23.
- Focused scene runs 1/2: 5/10. Threat query narrowed to existing EnemyHitRegion to avoid environment work. Live diagnosis showed actual authored zombie within the 20m safety radius; rejection is correct. Fixtures now place the unrelated encounter beyond the radius; production route explicitly clears it through existing damage authority before resting. Disabled actor reactivation requires existing Initialize/Bind before receiving damage; fixture corrected without changing old tests.
