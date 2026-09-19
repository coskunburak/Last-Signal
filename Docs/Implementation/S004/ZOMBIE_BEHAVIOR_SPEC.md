# Shambler behavior — implemented P2 + P3

Updated 2026-09-19 recovery. This document describes current source. Final verification status is in S004_P2_IMPLEMENTATION_REPORT.md. Earlier P1 proposals, including future combat contracts, are preserved in Evidence/20260919-P2-Recovery/prior-documents/.

ZombieController owns the only behavior state machine. Idle → Chasing requires a visible observation and confidence 1. Chasing → Searching occurs after .45 seconds without a visible sample or navigation exhaustion. Searching → Chasing occurs on visible confirmed reacquisition with a fresh loss/reveal cycle allowed to reset exhausted navigation. Searching → Idle occurs after 12 simulation seconds; all knowledge is cleared. P3 adds the melee states and player health below. Zombie damage reception/death, hearing and director gameplay remain out of scope.

Perception runs at most once per .1 simulation seconds, with staggered initial phase and no catch-up loop. Two visibility samples use player camera/head and capsule/chest. Distance is 15 m, full horizontal FOV 120°, vertical half-angle 60°. Only opaque Default-layer colliders obstruct; triggers, player and enemy bodies are excluded. Each visible sample contributes half evidence; full visibility takes .35 seconds of evidence. Confidence decays over .5 seconds. Slow frames are capped to one interval of evidence.

Only a positive observation releases player position/direction to decisions. LastKnownPosition and last visible facing are frozen immediately when occluded; the .45-second chase loss grace never authorizes hidden transform tracking. No prediction is used. Memory clears at Idle/teardown. Perception's diagnostic sample positions are not decision inputs.

Chase requests paths to LastKnownPosition. Refresh requires .3 seconds plus .45 m movement, except bounded failure retries and state changes. Stop at 1.2 m, resume at 1.6 m, with facing while holding. Search first travels to remembered position, then samples up to three points within a 2 m radius using remembered facing, with one-second inspections. Search has a total 12-second budget, including travel; distant or unreachable memory may therefore time out before completing the local pattern. Search has no player transform reference.

An exhausted navigation policy stays stopped unless genuinely reacquired after losing sight. A loss/reveal flag permits fresh visible reacquisition to reset navigation immediately. A continuously visible unreachable target cannot repeatedly reset an exhausted path policy simply by remaining visible; that search expires before retry.

Pause freezes controller time, perception, navigation and animation. Resume resets perception due time, preventing paused evidence accumulation. Session teardown clears references, state, timers, search and movement before the actor/player are destroyed. A new session creates a new actor bound to its new player. Unexpected target deactivation/destruction shuts the actor down safely.


## P3 enemy-to-player melee

Controller remains the authority. Existing Idle/Chasing/Searching edges and knowledge rules are preserved. Added legal edges:

| From | To | Condition |
|---|---|---|
| Chasing | AttackWindup | Live enabled health/capsule, current visible knowledge, valid nav, body LOS, surface distance ≤1.08 m and forward half-angle ≤20° |
| AttackWindup | AttackCommit | Sequence timer reaches .25 s; lock actor forward |
| AttackWindup | Chasing / Searching | Target exceeds 2.5 m surface hard-abort distance before commitment; choose return from visibility |
| AttackCommit | Recovering | Timer reaches .541667 s, regardless of hit/miss |
| Recovering | Chasing / Searching | Timer reaches 1.666667 s; visible valid knowledge returns chase, otherwise search |

Invalid lifecycle/disabled target/dead target/off-NavMesh actor interrupts via Shutdown (reset to Idle, clear target and sequence), not a normal gameplay edge. Target death never starts corpse attacks.

During windup navigation stops; controller yaw tracks only visible remembered position at ≤60°/s and only through .25 s. Maximum possible windup turn is 15°. Commit and recovery have no yaw tracking or gameplay lunge. Position remains NavMesh-owned and root motion disabled.

At .416667 s the sequence consumes its one contact opportunity before invoking any callback. Validation uses root-local (0,1.05,0), target CharacterController.ClosestPoint, ≤1.24 m 3D surface reach, target body center within the locked ±20° horizontal arc and opaque Default-world LOS. A tiny origin overlap check rejects starting inside walls. No overlap collection, animation event, per-collider callback or Animator state grants damage. Misses are spent strikes and continue visibly through recovery. Damage is 20, player max health 100. No i-frames.

Backstep beyond reach, motor-driven sidestep outside arc, rear position and inserted wall each produce a genuine miss. Staying in front produces one hit. P2 perception continues during combat; hidden movement does not update remembered position. A blocked strike returns to Searching after recovery, not omniscient chasing.

P2 stop/resume remains 1.2/1.6 m center distance; stop tolerance is .08 m. Attack entry uses 1.08 m capsule-surface clearance to include this stopping tolerance. Hard abort 2.5 m, entry 1.08 m and contact 1.24 m provide separate envelopes. No frame-by-frame attack/chase oscillation at contact reach.

Pause freezes both controller and manual attack-presentation clocks. Menu/disable/new binding cancels pending sequence; session owns input gating and terminal weapon unequip. New player instances initialize full health. The existing menu panel provides minimal death feedback and ReturnToMenu; no respawn/checkpoint/game-over framework is added.
