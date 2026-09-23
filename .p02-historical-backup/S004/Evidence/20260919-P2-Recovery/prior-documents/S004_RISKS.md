> Current B0B: technical asset gate PASS. Model/Idle/Walk/URP blockers closed; license evidence and future gameplay/LOD risks remain.

# S004 risk register

Recorded 2026-09-18. Open unless explicitly stated. Likelihood “Observed” identifies a current fact; other likelihoods are estimates. Burak owns asset/product decisions; implementation and QA ownership refers to the next authorized engineering task, not an assigned external team.

| ID | Risk / evidence | Likelihood | Impact | Mitigation / closure evidence | Blocks which gate? |
|---|---|---|---|---|---|
| R01 | Production model, Idle and Locomotion blocker CLOSED by B0B; all five visual roles accepted | Closed technical gate | Formerly Critical | Hotstrike production adapter + Kevin motions; B0B report/evidence | P2 asset gate closed; P3/P4 gameplay still open |
| R02 | Selected rig retarget accepted; raw Death A/B had floor penetration | Mitigated for selected motions | High on asset upgrades | Project Death correction changes only RootT.y; revalidate updates, slopes and contacts in P2/P4 | P2 visual gate closed; future integration open |
| R03 | Selected zombie URP material validated, vendor source unchanged | Closed for zombie | Formerly High | Actual normal/channel checks and renders in B0B | Zombie material gate closed |
| R04 | Public source terms recorded; local acquisition/entitlement evidence absent | LICENSE_EVIDENCE_PENDING | Commercial release clearance | Burak records actual channel/receipt and applicable terms/seats; no legal certification from import | Commercial clearance remains open |
| R05 | Root trajectory measured; production in-place presentation, applyRootMotion false | Mitigated | P2 speed mismatch still possible | .918734m/s target-rig RM reference; tune actual velocity/foot contacts | P2 tuning |
| R06 | Player-health/death system absent | Observed | High | P3 explicit health owner and terminal action lock; no death bag/inventory expansion | P3 |
| R07 | AI Navigation installed but no surfaces/data/agents in gameplay | Observed | High | P2 bounded arena bake, valid agent profile/spawn/obstacle tests | P2 |
| R08 | Current scene lacks corners/doors/corridor; floor edge near target | Observed | High | Purpose-built deterministic arena and same production encounter in SampleScene | P2 navigation/P5 |
| R09 | Acceptance scene paths restored in B0A; full B0B tests green | Closed | Formerly High | B0A report and fresh B0B regression XML | No current baseline blocker |
| R10 | B0A recovery retained; B0B full PlayMode 23/23 | Closed for baseline; focus-sensitive runner caveat | Medium operational | Keep Unity/GameView focused; initial focus-paused B0B attempt recorded, completed rerun green | No current regression blocker |
| R11 | DamageInfo lacks region/collider/type/transaction ID; ShotResult arrives after damage | Observed | Medium | Local IDamageable region proxies forward once; do not apply damage in ShotFired. Extend envelope only for proven later need | P4 |
| R12 | Single nearest hit + mutable static HashSet is not durable multi-shot/reentrant dedupe | Observed | High if expanded | Keep one Resolve/one proxy/one owner; multi-collider tests; no penetration/pellet expansion | P4 |
| R13 | Weapon short muzzle obstruction rejects a nearby damageable too | Observed code; encounter effect unmeasured | Medium | CMB-09 near-contact fixture; isolate a minimal resolver change only if required | P4 combat acceptance |
| R14 | Serialized masks=51 omit new enemy layers; trigger hitboxes are ignored | Observed | High | Explicit serialized masks/collision table; non-trigger regions, separate movement body; validation | P2/P4 |
| R15 | Bone colliders can block player, interaction or crouch; body capsule can hide headshot | Likely if unfiltered | High | Separate EnemyBody/EnemyHitbox, physics/query filters, CMB-08 and navigation overlap checks | P4 |
| R16 | SessionFlow has no encounter hooks; zombies could retain destroyed player | Observed seam absent | High | Explicit begin/end generation, cancel before player deactivation; ten-cycle test | P2–P5 |
| R17 | Resume unconditionally reenables player input, unsafe after future death | Observed | High | Terminal health-aware session/action lock in P3; Pause/death/Resume negative test | P3 |
| R18 | Presentation event duplicate/late callback can hit after cancellation | Possible | High | Simulation owns contact; generation+consumed guard, events presentation only | P3/P4 |
| R19 | Automatic fire can permanently stagger enemy | Likely without policy | Medium | Reaction cooldown, no timer restart, retain attack recovery, death priority | P4 |
| R20 | 101 skin bones, three 2K maps, no LOD; crowd cost unprofiled | Measured asset risk | Future population performance | P5 1/10/25 profiling, LOD authoring if justified; no low-quality auto LOD | P5 |
| R21 | Third-party prefab changes or missing source references break adapters | Possible | Medium | Nested/source references + project ownership, importer/GUID validation; preserve vendor files | P2–P5 |
| R22 | Windows acceptance unavailable on this Mac; historical builds are stale | Observed evidence gap | High | Windows build/runtime evidence through suitable machine/CI; no Mac substitution | P5 Windows |
| R23 | Old roadmap S004 differs from current user-defined S004; old combat doc claims unsupported behavior | Observed | Medium | Explicit source reconciliation and additive planning notice; no mass renumbering | Handoff |
| R24 | Broad survival/save expectations accidentally expand this slice | Possible | High scope cost | No loot/save/network/infection; record transient corpse policy and future tombstone seam | P2–P5 scope |
| R25 | Rifle audio absent from production prefab; casing/impact prefabs null | Observed | Medium | P5 separate feedback content gate; retain functional rifle source/domain | P5 |
| R26 | Unity Assistant discovery intermittently unavailable during reload; Coplay reports 0 instances | Observed | Medium evidence risk | Reconnect/check actual project; UI fallback for play state; record channel per evidence | Baseline/QA |
| R27 | Input clear does not explicitly clear WeaponController.fireInputHeld; release callbacks are gated by GameplayActive | Observed code; runtime failure NOT_VERIFIED | High | Baseline held-fire/pause/resume regression; explicit action cancellation if reproduced, no P1 rewrite | Baseline repair / P3 lifecycle |

No baseline failure was fixed or concealed in P1. Missing production art/timing is an entry dependency, not a promise that generated placeholders will close later gates.
