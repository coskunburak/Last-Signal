# P3 entry architecture plan

Repository overrides stale report headers: concrete P2 controller, nav, perception, search, session encounter and tests exist. No changes to vendor assets or P4 systems.

1. Reuse IDamageable/DamageInfo for PlayerHealth; serialized max, finite positive damage only, clamp, changed/death events, fresh Awake per session.
2. Extend ZombieRuntimeState legal edges; ZombieController remains sole state/timer/sequence authority. Perception continues during attack so return uses P2 knowledge, never hidden live chasing.
3. Audit actual retargeted Attack through Unity before selecting normalized commit/contact/strike-end timing. Preserve 1x playback and root-motion off initially.
4. Cache health/capsule on Bind. Stable project-owned chest-height melee origin. Geometry uses target capsule closest point, horizontal locked forward arc and opaque-world segment, including origin-inside-world rejection.
5. Stop nav at attack entry; bounded early yaw using visible remembered data only. Commit locks forward. Movement after commitment produces a miss, then visible recovery. Contact latch spent before health callback.
6. Session owns death action lock using existing input gate plus weapon cancellation. Pause/resume cannot reenable a dead player. Minimal existing HUD extension.
7. Focused health/state/collision/lifecycle tests, production visual sequences, synchronous allocation profiling, full suites and console audit. Update P2 tests whose explicit no-health/no-attack scope assertions are superseded, preserving navigation/perception checks.

Initial entry compile via Unity MCP: isCompiling=False, scriptCompilationFailed=False. Fresh baseline EditMode 52/52 passed, zero failed/skipped. Full PlayMode pending.
