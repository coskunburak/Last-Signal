# P2 implementation gate plan

Entry revalidation precedes runtime additions. Existing B0B model, controller, material and valid Humanoid resolved in live Unity; compilation was clean. Entry suites run sequentially to completed XML.

Small composition matching existing namespace/assemblies:
- ZombieDefinition: serialized read-only tuning.
- ZombieRuntimeState: plain state/confidence/memory, explicit legal transitions and game delta.
- ZombieController: only orchestration owner; commands perception, navigation, search and presentation.
- ZombiePerception: bounded chest/head world-occlusion queries; no state writes.
- ZombieNavigation: agent-only position, bounded paths/failures/stuck, gameplay yaw.
- ZombieAnimationPresenter: actual-speed Idle/Walk presentation only.
- ZombieEncounter: explicit SessionFlow begin/end/pause participant, creates one actor per session.

Current request supersedes P1 immediate Searching/stand-only scan: freeze memory at the first failed visibility observation; tolerate short LossGrace in Chasing; then move to frozen memory and inspect deterministic local points within a finite total search budget. No hidden live position is passed from sensor to decisions. Omit velocity prediction initially: frozen last-seen information provides the clearest testable fairness contract.

Agent speed begins .92 m/s, radius .32 m, height 1.81 m, base offset 0. Dedicated agent bake settings match these dimensions. Player capsule remains .3 m radius; initial engagement stop 1.2 m, resume 1.6 m. Movement yaw uses desired velocity and a bounded turn rate; root motion and agent rotation disabled.

Preserve current player layer 2 and presentation layer 30. Add EnemyBody at an unused layer; world LOS uses Default only, ignores triggers. Enemy movement capsule physically blocks player but is excluded from rifle rays until P4 hit-region integration. Update interaction/stance blocking masks deliberately if needed.

Create a dedicated fixed-geometry acceptance arena with open lane, opaque corner, navigable obstacle, narrow clearance, and disconnected island. Bake project-owned ground/world only. Same prefab and encounter integrate one zombie into SampleScene after automated behavior gates.
