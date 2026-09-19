# S004-P3 — Production zombie → player combat

Run: `20260919-P3`. Unity 6000.5.0f1 / URP. Evidence: [run index](Evidence/20260919-P3/README.md).

## 1. P3 ENTRY BASELINE

Entry git status and diff stat recorded before changes; existing dirty/untracked project preserved, no reset/clean/revert. Current source contains working P2 despite stale historical P2 report headers. Fresh compilation succeeded. Full entry EditMode **52/52**, PlayMode **35/35**, zero failed/skipped/aborted. PlayMode includes focused P2 behavior/navigation/lifecycle/presentation and normal-route coverage.

## 2. PLAYER HEALTH ARCHITECTURE

Production Player owns one PlayerHealth component, serialized max 100, full initialization per new instance, finite positive damage only, clamp at zero, health-change/death events, transaction count and last damage. Health has no movement/UI/zombie knowledge. Explicit ResetForSession is also covered.

## 3. DAMAGE CONTRACT

Reuses unchanged IDamageable/DamageInfo. Each valid melee transaction carries 20 damage, zombie instigator, source position, capsule hit point and opposite committed forward normal. Existing rifle resolver/weapon state machine unchanged. No i-frames or enemy health added.

## 4. ATTACK STATE MACHINE

Controller retains P2 Idle/Chasing/Searching. Adds Chasing→AttackWindup→AttackCommit→Recovering→Chasing/Searching. Precommit hard abort can return Chasing/Searching. Invalid lifecycle uses Shutdown/reset. Unlisted edges throw; a committed strike cannot skip recovery into a new attack.

## 5. ATTACK ANIMATION AUDIT

Actual `Zombie@Attack01.fbx` / `Zombie@Attack01`, production Humanoid, 1.333333 s, 30 fps, non-looping, zero events. 81 actual-rig samples plus key-pose renders. Right hand peaks forward near source .333–.350 s at z≈1.23 m. Source root average speed and sampled actor displacement zero; root motion remains off. [Measured audit](Evidence/20260919-P3/clip-audit/README.md).

## 6. WINDUP / COMMIT / CONTACT / RECOVERY TIMING

| Milestone | Normalized clip | Gameplay seconds at .8× |
|---|---:|---:|
| Start | 0 | 0 |
| Commit | .15 | .250000 |
| Contact | .25 | .416667 |
| Recovering | .325 | .541667 |
| Recovery end | 1 | 1.666667 |

Modest slowdown makes the original short telegraph more readable. No extra hidden cooldown; recovery plays the real follow-through/return. Locomotion remains .92 m/s. Runtime evidence records actual threshold-crossing times; same-frame health change follows presenter advancement. Controlled 30/60/120 Hz tests produce one hit at .433333/.416667/.425000 s respectively, with matching actual Animator time and at most one-step threshold quantization.

## 7. TARGET TRACKING & COMMITMENT POLICY

Navigation stops at attack entry. Early controller yaw ≤60°/s (≤15° total over .25 s) uses only visible remembered position. Facing locks at commitment and remains fixed through recovery. No root-motion yaw, warp, lunge or contact correction. Precommit surface distance >2.5 m hard-aborts; ordinary evasion after commitment produces a spent miss.

## 8. HIT VALIDATION

Cached production CharacterController and PlayerHealth. Stable project MeleeOrigin at (0,1.05,0); closest capsule surface within 1.24 m, body center inside locked ±20° horizontal arc, enabled/live target and valid actor/session. Entry surface reach 1.08 m accommodates P2 1.2 m stop plus .08 m tolerance. Initial 1.02 m entry failed normal-route approach; retained failing XML and corrected tuning without enlarging hit reach.

## 9. LOS / OCCLUSION

Entry and contact both validate Default opaque-world segment to capsule surface. Origin overlap catches ray-origin-inside-wall cases. Triggers/player/enemy bodies are excluded by existing masks. Inserting a wall after commit rejects the strike and returns P2 search after recovery.

## 10. MISS BEHAVIOR

Backstep, lateral evasion, rear and wall tests produce zero damage. Actual FirstPersonMotor walking sidestep after commitment also misses; no target teleport is needed for this proof. Slight visible escape returns Chasing; obscured target returns Searching using retained P2 knowledge.

## 11. SINGLE-HIT GUARANTEE

Per-actor ulong sequence and consumed contact latch. Latch is spent before validation/callback, so every sequence has zero or one damage transaction even with duplicate colliders, a .9 s simulation step or a disabled Animator. Contact attempts/successes/last sequence/result/distance/angle are exposed for diagnostics. Rebinding clears the current strike before accepting the new target; a committed A strike cannot transfer to B. No per-frame production logging.

## 12. PLAYER DEATH STATE

Fifth 20-damage hit reaches zero, fires one death event, blocks existing gameplay input and unequips weapon through existing lifecycle. Resume does not unlock a dead player. Other zombies stop on dead target; no corpse damage. Existing menu panel shows death and ReturnToMenu. No respawn/checkpoint/cinematic system.

## 13. PAUSE / SESSION LIFECYCLE

Windup/commit pause longer than the whole attack freezes state/presentation and health; resume produces one contact. Menu, actor disable and target death/disable cancel pending attack. Encounter teardown clears binding before destruction. Fresh player/session restores full health and zero sequence state. Health listeners are unsubscribed before teardown. Pause also explicitly clears held weapon fire/aim.

## 14. ANIMATION PRESENTATION

Presenter begins Attack once per sequence and advances it manually from controller delta at .8 speed; automatic Animator speed stays zero between attack ticks. Gameplay never reads Animator time for damage. AlwaysAnimate is temporary during attack and previous culling restored afterward. Normal locomotion uses the retained P2 presenter. Missing origin/tuning/attack presentation fails safely.

## 15. NORMAL GAMEPLAY INTEGRATION

Same production Player and LS_Zombie_Runtime prefabs are used by normal SampleScene and acceptance route. Real normal encounter approaches and hits; health appears beside ammo, formatted only when a displayed value changes. No acceptance-only health component, vendor edit or alternate combat implementation.

## 16. AUTOMATED TESTS

New PlayerHealthTests (7), ZombieMeleeLogicTests (11), ZombieMeleeTests (13). Focused PlayMode revision 01: 6/7, normal-route entry-range failure. Revision 02: **11/11** after correction and expanded edge/performance coverage. Final focused lifecycle/rate audit: **13/13**. Existing exhaustive legal-edge expectation and P2 no-health endpoint updated for authorized P3; P2 navigation/perception/search assertions retained.

## 17. VISUAL ACCEPTANCE

Real first-person and external sequence captures in evidence/visual: telegraph, commit, contact, recovery, next attack, normal approach/hit, evasions, wall, death, pause and restart. Actual Animator time checked against gameplay within .03 normalized. Right-arm draw-back readable in first-person commit frame; contact matches forward strike; sidestep leaves the swing behind. External captures add an explicitly named capsule visualization and hide the pre-existing layer-30 magenta body; they do not alter gameplay collision. Render-only screenshots omit screen-overlay HUD; HUD verification is recorded separately where available.

## 18. PERFORMANCE

[Measurements](Evidence/20260919-P3/performance.txt): one and ten active attackers, >2.5 s warmup, ≥5 s / ≥300 frames. AI, attack, validation and animation recorders valid. Direct synchronous controller+presentation managed allocation **0 B** in both configurations. Multiple actors produce independent transactions, checked against total health transaction count. Editor technical smoke only; no standalone/crowd-density certification.

## 19. FULL REGRESSION

Final EditMode: **70/70**, zero failed/skipped. Full PlayMode closure pending final XML review. Console and final source manifest recorded at closure.

## 20. KNOWN LIMITATIONS

Initial balance and .8 playback require continued human playtesting. No i-frames/group coordination; ten arranged attackers are not crowd certification. Fixed frontal geometry approximates the swipe; no swept limb physics/rewind. Stalled renders can omit intermediate poses; threshold crossing still resolves at most once. Shared tuning is activation-validated and should not be edited live. Existing third-person material/viewmodel and broad audio/death UX polish are outside P3. Windows/commercial entitlement/standalone performance gates remain open.

## 21. P4 ENTRY STATUS

Pending complete P3 closure. No ZombieHealth, hit regions, headshots, zombie hit reaction/death gameplay, hearing or horde systems implemented.

## 22. FINAL STATUS

IN VALIDATION — not yet claiming PASS. Final regression and evidence review determine PASS/PARTIAL/BLOCKED.
