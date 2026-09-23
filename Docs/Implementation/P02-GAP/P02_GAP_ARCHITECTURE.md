# Canonical S008 / P02-GAP architecture

Historical implementation S008 remains shelter/preparation/expedition/return. This document describes the new world-time layer, not a rename.

## Authority and units

`WorldSimulation` is the only persistent world timestamp authority. `Seconds` is double absolute world seconds; day starts at 1 and time of day is modulo 86400. Range is explicitly bounded to 1e12 seconds (about 31,688 years); near that limit double spacing is approximately 0.000122 seconds. The code rejects negative, nonfinite, backward or out-of-range time. No deterministic physics claim.

`WorldClock` is a scene/session composition component, not a global singleton. `SessionFlow.BeginSession` initializes it after shelter/player binding, including paused restore creation; `ReturnToMenu` cancels sleep and releases it. A scene without the component retains its historical behavior. Normal `Time.deltaTime` is passed to `TickRealSeconds`, converting once by saved world-seconds-per-real-second (default 60). Menus, preparation, ordinary pause, restoring and death do not advance it. Existing short combat/animation timers retain their real gameplay-second meaning; they are not persistent world clocks.

Settings are copied into the domain and snapshot. Authored changes apply to new games; loaded saves retain their scheduling/rate configuration. UI uses unscaled presentation delta only for a 4Hz refresh throttle. Lighting reads authoritative time; it never advances simulation.

## Advancement and scheduling

`AdvanceUntil(targetAbsoluteSeconds, exposure, protection, recoveryCallback)` advances to the nearest target, weather event or `IWorldTimeParticipant.NextBoundary`. It applies each interval once, inspects interruption/death before recovery, commits scheduled weather transitions at the resulting boundary, then inspects again before continuing. Death takes precedence over other interruption reasons. Registration changes/reentrant advancement and non-forward participant boundaries are rejected. A 100,000-boundary guard returns `BoundaryLimit` with the actual endpoint. Participants must supply valid side-effect-free queries and exception-safe interval effects; this synchronous main-thread scheduler does not roll back arbitrary external callbacks.

Sleep requests return `SleepRequest` / `SleepRejection`, including bed, threat, dead, duration, transition and unsafe-state reasons. Rejection never advances time. An accepted request disables gameplay input, then runs at most 15 world minutes per rendered frame through the same boundary engine. It does not set a giant Unity timeScale, simulate physics for each skipped second, disable all AI, clear threats or reset loot. At each chunk and domain boundary it rechecks living player, pause, bed range, roof and live threats. The coroutine remains observable/cancellable. Existing nearby active enemies reject sleep; dead or disabled actors do not. Collider-query overflow fails conservatively.

Loaded enemy physics/navigation continues on actual rendered frames, not skipped-world seconds. No distant group travel exists yet. S009 must register its logical group ETA/threat boundary so arrival during a skip is processed at its actual timestamp. This is a known pre-S009 simulation limit, not deterministic fast-forwarded navigation.

## Weather

Two logical states: Clear/Rain. A private xorshift32 sequence chooses durations between saved bounds. Seed/state, current state, start and next transition timestamps are persisted. Every logical transition toggles Clear/Rain; only duration is seeded. No global Unity random dependency. Rain gameplay intensity is the authored 0–1 intensity preset; smooth presentation ramps do not alter survival authority. Rain presentation fades over saved transition seconds, with initial clear already settled. No weather reroll on load.

## Exposure and wetness

Actual upward `Physics.RaycastNonAlloc` ignores EnemyHitRegion/raycast-ignore layers and triggers; player colliders have no roof marker and cannot grant protection. Only colliders under an authored `RainRoof` protect; arbitrary props do not. The existing cabin Roof is marked. Exposure polls at 10Hz maximum with a 0.25s stable-state debounce, using real gameplay-active time. Sleep eligibility also requires a fresh actual roof query. Moving/deactivated roof geometry naturally changes the query. Full hit buffer conservatively reports exposed. Clothing boundary is a 0–1 protection fraction (default 0); no equipment system added.

Wetness is bounded 0–1. Logical rain * environmental exposure * (1-protection) scales gain; shelter/clear/complete protection permits drying. Rates default to full wet in 30 world minutes and full dry in 60. Constant interval integration computes the exact clipped linear wetness area. Rest healing uses that integral and existing `PlayerHealth.RecoverHealth`: 8 HP/world hour at dry, down to 4 at fully wet, capped by existing maximum health and never reviving death. There is no added stamina, temperature or disease stat. HUD explains wetness, roof status, healing modifier, drying and sleep feedback.

## Persistence and elapsed state

`SaveGame.worldTime` is part of the existing detached `SaveSession.Capture` snapshot. Schema 2 requires validated time state; schema 1 remains a supported historical fixture format. Schema 1 hydration into the new scene uses fixed 08:00 / clear / dry / seed 7183 defaults, independent of wall time/randomness. JsonUtility's materialized null object is normalized for schema 1 after checksum verification. Existing atomic writer, checksum, backup/recovery and staged hydration are preserved. Time-enabled scene writes schema 2; historical scenes continue schema 1. Loading schema 2 into a scene with no clock rejects before session creation, avoiding silent time loss.

Capture refuses active sleep/advancement/ownership transactions/restore. Saves occur at settled coherent boundaries. Hydration replaces the domain; it never applies elapsed time from the previous session. `lastProcessed == seconds` proves owned weather/wetness have already been integrated. Persistent last sleep reason/elapsed supports interrupted-save continuity; an in-flight coroutine is never serialized.

`ElapsedWorldState.TakeElapsed(now)` holds a monotonic processed timestamp for inactive future systems. Persist both that timestamp and the system's resulting state together. Repeated activation at the same timestamp returns zero. The fixture proves save/load/rebind idempotence; it does not implement cell streaming. Apply effects and save the timestamp within the future owner's atomic state transaction.

## S009 / S010 consumption

Use `clock.Simulation.Seconds` to timestamp noise receipt, pressure updates, group departure and ETA. Compute elapsed as `now-lastProcessed`; compute ETA as `departure+travelSeconds`. Register a participant whose next boundary is its absolute pending ETA, apply elapsed once, and return an interruption reason for an arrival that threatens the resting player. Store group/pressure state and processed timestamps in the existing SaveGame transaction. Re-register runtime participants after hydration; do not replay consumed receipts. Craft completion/fuel depletion use the same contract later. Synthetic death/fuel tests prove scheduling only; neither bleeding nor generators are shipped here.

## Authoring and checkpoint UI

`WorldTimeAuthoring.Create` derives `WorldTimeAcceptance.unity` from the current persistence scene. It adds a marked roof, visible bed, local rain particles, sun binding and existing-uGUI text. `WorldTimeSaveControls` offers Save checkpoint on pause and Load checkpoint in the session menu through SaveSession; it never auto-loads or starts a second save implementation. `WorldClockInspector` shows absolute time, weather transition, exposure, wetness and sleep state in PlayMode. Future clothing supplies `SetRainProtection` without changing wetness integration.

## Final hydration order (continuation fix)

SaveSession decodes/checksums/validates before creating the restore session. BeginRestoreSession sets Restoring=true and pauses gameplay; the existing single-frame weapon Start boundary remains unchanged. Hydrate restores containers, doors, world items, player transform/stance/look/health, weapon, shelter lifecycle and enemy pose/health. It then synchronizes physics and checks player clearance. Only after those steps does WorldClock.Restore install the validated world-time/weather/wetness snapshot, derive roof exposure from the final saved position, reset exposure debounce caches and refresh presentation. CompleteRestore releases the restore gate. No arbitrary wait or next-frame exposure workaround is used. Consumers observing Restoring=false see final exposure. The rainy outdoor and rainy indoor tests assert this immediately after Load returns, then advance wetness to prove the consequence; the legacy scene test loads a real schema-1 file through the same pipeline.

DateTimeOffset.UtcNow remains solely human-readable save header metadata, not gameplay simulation time. Existing Time.timeScale checks are the established pause/combat gates. Search evidence: `Evidence/20260922-entry/time-authority-audit.txt`.
