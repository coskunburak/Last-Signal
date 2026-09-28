# PRE10-DEC-001

## Decision
Keyboard key `3` selects the dedicated melee combat slot (Crowbar).

## Date
2026-09-26

## Reason
The implementation of the Combat Equipment Foundation (R01) introduces the Crowbar as a permanent melee capability. To ensure deterministic and fast weapon selection in survival combat situations without complex inventory cycling, a dedicated hotkey is assigned.

## Affected Systems
- `PlayerInputReader`
- `PlayerCombatController`
- `InputSystem_Actions.inputactions`

## Input Consequence
- The `<Keyboard>/3` binding explicitly selects the `CombatSlot.Melee`.
- The firearm remains on its original binding (`<Keyboard>/1`).

## Persistence Consequence
- The `CombatEquipmentSnapshot` saves the selected slot.
- Loading the save game restores the slot explicitly, ensuring the correct viewmodel and combat authority are active.

## Future Compatibility
- The `PlayerCombatController` uses an explicit `CombatSlot` enum (Firearm, Melee) which can be naturally extended if further permanent tool slots are added.
- The separation prevents melee actions from interfering with the retained firearm state (e.g., loaded magazine).

# PRE10-DEC-002 — Canonical committed gameplay noise

Date: 2026-09-27. R02 introduces one immutable `GameplayNoiseEvent`, emitted only by committed gameplay consequences. It carries epoch/sequence identity, session-local source identity, optional original action ID, position in meters, category, normalized intensity, radius, simulation time and expiry. A rifle wall hit is a committed round under existing R01 rules and is therefore noisy; a blocked weapon state is not. Crowbar air swings and blocked/rejected damage attempts remain silent. R02 does not introduce world-surface melee damage/impact semantics.

# PRE10-DEC-003 — Separate local and regional consumers

Date: 2026-09-27. Local physical candidate distribution and World Pressure consume the same canonical event independently. A 16 m XZ grid broadphase followed by 3D radius checks supplies generic listeners. Only Gunshot forwards to regional pressure; footsteps, sprint and melee are local only. Existing population ledger, decay, migration, materialization and persistence remain authoritative.

The population save contract accepts only `noise:<long>` receipts and persists its own high-water mark. The adapter maps each accepted canonical gunshot to the next legacy receipt and exposes the canonical ID/receipt correlation. This deliberately preserves schema compatibility instead of persisting raw local events. Position selects exactly one half-open 64 m world cell; there is no duplicated border contribution. Undefined/resident cells without a population ledger receive local noise only, matching the absence of regional pressure state there.

# PRE10-DEC-004 — R03 behavior boundary

Date: 2026-09-27. R02 candidate delivery does not mean a zombie heard the event. Auditory thresholds, environment transmission, memory, confidence, Investigate/Suspicious, stimulus priorities, search redirection and vision/hearing fusion belong to R03. R02 changes no zombie state logic. The explicit QA probe is a generic listener only.

# PRE10-DEC-005 — Presentation independence

Date: 2026-09-27. Audio playback and gameplay noise are sibling consequences of the committed action. Audio volume, mute, clip presence, Animator and mixer state never determine the gameplay event. Unique IDs and timestamps naturally differ between separate shots; category, source, position, radius and intensity remain unchanged by presentation mute.

# PRE10-DEC-006 — Session ownership, bounded ordering and initial tuning

Date: 2026-09-27. `SessionFlow` owns one pure C# noise service and explicitly injects it into its player. Each new session/load gets a process-unique monotonic epoch. No static event bus, Unity-object event authority, persistent singleton or raw-noise save schema is introduced. Source 1 denotes the single local player within that epoch; weapon shot/swing IDs remain action correlation, not cross-category event identity.

Delivery is immediate, synchronous and ordered. An O(1) high-water mark permanently rejects duplicate/old sequence IDs in the current epoch, even after the 64-event diagnostic history wraps. Out-of-order, expired, future-time, foreign-epoch and reentrant events fail closed; this is not an asynchronous network transport. Listener registrations cap at 4096, positions at ±1,000,000 m and radii at 128 m, bounding a query to 289 buckets. Registration/movement may allocate when creating a new bucket; warmed emission and movement cadence use reusable storage.

Production time comes from `WorldClock.Simulation.Seconds`. Earlier acceptance scenes without WorldClock use Unity scaled `Time.timeAsDouble`, with the same explicit session/pause/death gate. TTL is in that clock's simulation seconds; it is validated in (0,30] and does not enable delayed replay. Local state is discarded on menu/load/teardown; pressure persists only through its existing snapshots.

INITIAL PRODUCTION TUNING — NOT FINAL BALANCE: walk 6 m / .20 intensity, sprint 12 m / .40, crouch 3 m / .10, melee 10 m / .35 (all 2 simulation-second TTL), gunshot 96 m / 1.0 (3-second TTL). A 1.6 m planar grounded stride applies to walking/sprint/crouch. This reflects the existing 3.2/5.5/1.6 m/s motor and 64 m regional cells. Noise radius is candidate reach, not audio loudness or a hearing decision. Movement is measured inside motor substeps, so teleport/load deltas are never accumulated; pause and airborne transitions clear partial cadence.

# PRE10-DEC-007 — Separate auditory memory

Date: 2026-09-27. R03 adds value-only `ZombieAuditoryStimulus` and `ZombieAuditoryMemory`. Visual `Confidence`, `LastKnownPosition` and `LastSeenDirection` retain their existing sight-only contract. Auditory age advances during active controller simulation, with linear confidence decay over 15 seconds; event TTL remains a separate canonical simulation-clock receipt gate.

# PRE10-DEC-008 — Snapshot investigation and bounded replacement

Date: 2026-09-27. An accepted sound retains the R02 event ID/source/category/position/time and listener-to-event approach direction, never a source Transform or player-position delegate. Priority compares incoming effective strength to decayed current strength; ties within .0001 prefer newer event time, then sequence. R02's synchronous ordered stream permits an O(1) epoch/high-water replay guard. A replacement can update the heard route, but never resets an active investigation's 12-second budget. Destination displacement below the existing .45 m repath threshold does not change the current destination. Existing .3-second navigation throttling and bounded stuck/path retries remain authoritative.

# PRE10-DEC-009 — Controller authority, visual priority and search source

Date: 2026-09-27. Only `ZombieController` decides transitions. Sound produces the appended `Investigating` enum value, preserving all previous numeric enum values. Confirmed sight takes precedence; chase loss grace is retained. Committed attacks are not interrupted by hearing. After unseen combat/recovery, valid auditory memory may seed investigation. Investigation completion consumes active auditory memory and seeds Search explicitly from the heard position/approach direction without writing visual memory. Search can finish normally after memory expiry. Hearing does not call attack or damage APIs.

# PRE10-DEC-010 — Deterministic initial hearing transmission

Date: 2026-09-27. Strength = intensity × category sensitivity × clamp01(1 − 3D distance/radius) × transmission. Initial threshold .12; sensitivities walk 1, sprint 1.1, melee 1.2, gunshot 1.5. Clear transmission 1, blocked transmission .4. Weather transmission is 1 (detailed weather acoustics deferred). At most one allocation-free world-mask raycast per otherwise audible candidate, from the existing perception origin; authoring rejects owned collider layers in that mask. No per-frame acoustic query, probability, audio playback dependency or regional-pressure write. Values are INITIAL PRODUCTION TUNING, not final balance.

# PRE10-DEC-011 — Transient memory and explicit session dependency

Date: 2026-09-27. Existing encounter persistence stores health/pose; population snapshots store actor identity/cell/health and population accounting, not active visual/search/attack memory. R03 therefore adds no save schema. SessionFlow supplies a `GameplayNoiseContext` before actor creation, allowing the existing `Bind(player)` seam to inject its canonical service into physical listeners. Rebind, shutdown, death and new sessions clear transient hearing; menu ends the service and removes registrations. Saved sound IDs, listener objects, queues, paths and investigation clocks are never replayed.
012: Replaced SaveValidation.cs cell identity/time exact comparison with a 0.01s tolerance-safe comparison (cellLastProcessed <= worldTime + 0.01) to protect against floating point precision drift during double serialization.

# PRE10-DEC-013 — R04 Integrated Tuning and Trade-offs

Date: 2026-09-27. R04 integrates existing systems without creating new stealth frameworks (no visibility meters, no crouch/prone special states). Walking produces lower auditory exposure while sprinting produces stronger footsteps, consumes stamina, and causes nearby zombie investigation. The rifle trades strong combat power for large local noise and exactly one regional World Pressure contribution. Crowbar impacts produce a smaller/local consequence without World Pressure. Vision remains absolutely authoritative over hearing; a visual chase cannot be stolen by an irrelevant footstep.

# PRE10-DEC-014 — De-escalation and Anti-Omniscience

Date: 2026-09-27. Physical zombie reaction is individual; there is no telepathy or group communication of undetected stimuli. The threat naturally de-escalates (Investigating → Searching → Idle) when legitimate information expires and the player breaks line of sight. Anti-omniscience is strictly enforced: a zombie follows only legitimately heard positional snapshots, and does not gain live tracking of a player who moves silently and unseen. Hearing never directly authorizes an attack or damage.

# PRE10-DEC-015 — Session Scoping and Memory Leaks
Date: 2026-09-27. Foundation services are explicitly session-scoped and must return to baseline registration counts after ReturnToMenu. There are no static singleton gameplay services or lingering runtime allocations across sessions.

# PRE10-DEC-016 — Serialization Strictness
Date: 2026-09-27. Transient AI perception state and auditory memories are never serialized. Physical snapshot restoration respects strict 0.01s time drift tolerances to prevent deserialization loops.

# PRE10-DEC-017 — Acceptance / Debug Separation
Date: 2026-09-27. Acceptance helpers and test probes (e.g. `R01Acceptance`, `R02AcceptanceRoute`) are test/development support only and guarded by UNITY_EDITOR or DEVELOPMENT_BUILD boundaries. They may never become shipping gameplay authority.

# PRE10-DEC-018 — Pre-S010 Performance Baseline
Date: 2026-09-27. R05 establishes the final PRE-S010 performance baseline. Integrated loop metrics (0 hearing queries in silence, exactly 1 regional pressure write per gunshot, bounded local path requests) are strict regression gates for all future S010 additions.
