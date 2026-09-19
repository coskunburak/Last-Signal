> Recovery update 2026-09-19: see the current P2 implementation report for final gates. Historical B0B risks below are retained; the following current ledger supersedes obsolete P2 seam/navigation claims.

| Recovery risk | Current disposition |
|---|---|
| R07/R08 navigation/arena absent | Closed by retained arena bake/tests and new normal scene bake/encounter |
| R16 stale session target | Ten normal-route cycles passed; target/state/timers cleared before destruction |
| Compilation interrupted midway | Missing authoring class completed; zero compile errors after recovery |
| Hidden live tracking | Retained critical A→B wall-memory test passed freshly |
| R05 stride matching | .92 m/s agent, .918734 reference, velocity-derived presenter; slight low-speed turn sliding remains possible |
| Exhausted-search reacquisition | Fresh loss/reveal now resets navigation; explicit regression added |
| R14/R15 body query masks | Body layer 2 physically blocks player but is excluded from rifle/LOS/interaction; enemy-blocked interaction and P4 hitboxes remain deferred |
| R20 population | 1/10/25 Editor measurements, not shipping-density certification; no LOD added |
| Editor-wide allocation/spikes | AI synchronous ticks measure 0 B; Editor/test/plugin allocation remains nonzero and is reported separately |
| Existing player world-body material | Magenta in third-person evidence camera; unchanged layer-30 body is culled by normal first-person view; separate URP cleanup remains |
| Dynamic doors | No moving-door NavMesh adapter certified; fixed world acceptance only |
| Search budget | 12 s includes travel; distant/unreachable memory may expire before all local points |
| Commercial entitlement / Windows / future health and combat | Remain open in original R04/R06/R17/R22 and P3–P5 scope |


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


## P3 risk disposition

- Closed by focused tests: proximity-only damage, wall penetration, homing strike, duplicate per-collider transactions, missed large-step contact, paused damage, stale session attack, repeated corpse damage and dead-player Resume input loophole.
- Resolved during P3: initial attack entry range missed NavMesh stopping tolerance; corrected to measured 1.08 m capsule-surface envelope without enlarging 1.24 m hit reach.
- P2 knowledge/search source preserved; full regression determines final closure.
- Open: final balance/readability with varied human players; .8 speed and 20 damage are first-Shambler tuning, not final difficulty certification.
- Open: simultaneous attackers have no i-frames/coordination; dense crowd fairness and separation are future work. Ten arranged actors are a technical smoke, not crowd acceptance.
- Open: narrow fixed frontal arc approximates the right-arm swipe; no swept limb collision or rewind of moving targets. Contact is validated on the simulation frame crossing its threshold; a stalled render can omit intermediate visual poses.
- Open: live ScriptableObject edits during a running session are unsupported; tuning is activation-validated, not immutable-snapshotted. Existing P2 uses the same convention.
- Open: existing third-person player material/viewmodel rendering; external QA hides layer-30 body and adds a clearly identified capsule visualization. First-person camera uses the real production rig/weapon.
- Open: commercial entitlement evidence, Windows/standalone shipping performance, audio/damage polish and full death UX. These are not falsely closed by P3.
