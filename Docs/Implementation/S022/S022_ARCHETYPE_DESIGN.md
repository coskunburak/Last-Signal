# S022 archetype design and tuning — v1.0

Status: implementation authored; all runtime/art/balance acceptance NOT_RUN. Scaled gameplay seconds (`ZombieController.Simulate(Time.deltaTime)`) govern cycles. Pause advances neither cycle nor attack. Crowd spacing uses the same scaled Unity clock, shared per player. World-time migration remains on WorldClock.

| Parameter | Shambler | Lurker | Runner |
|---|---:|---:|---:|
| Stable ID | zombie.shambler | zombie.lurker | zombie.runner |
| Current authored HP | 100 | 75 | 60 |
| GDD hypothesis HP | 90 | 75 | 60 |
| Walk / normal chase m/s | .92 / 2.6 | .7 / 1.8 | 1.2 / 2.2 |
| Preparation seconds | baseline clip | .85 | .65 |
| Burst m/s / seconds | none | 3.4 / .9 | 4.6 / 1.6 |
| Post-burst recovery / cooldown | baseline clip | 2.5 / 6 | 2.4 / 5 |
| Acceleration m/s² | 2 | 3 | 5 |
| Burst turn degrees/s | baseline 120 | 65 | 65 |
| Vision metres / acquisition seconds | 15 / .35 | 12 / .5 | 18 / .35 |
| Hearing threshold | .12 | .10 | .16 |
| Attack damage / reach metres | 20 / 1.24 | 20 / 1.24 | 20 / 1.24 |
| Attack playback | 1.2 | .8 | .8 |
| Population weight | 70 | 20 | 10 |
| Risk weight | 1 | 1.5 | 2 |

The 90 HP Shambler proposal is deliberately deferred: local production and old saves use 100. No weapon damage changed. Profiles own runtime health/speed/hearing/clip timing; saved health compatibility ceilings remain 100/75/60. Raising these ceilings requires an explicit compatibility decision, not an unnoticed Inspector change. Prefab HP mirrors profile HP and is covered by asset tests. Catalog population weights are authored independently from encounter risk weights.

## Behavior and counterplay
Shambler retains sustained pursuit, alternating committed attacks and its existing hit/death pipeline. Step out of reach or arc during windup; preserve an exit. Baseline visual and slow voice cadence remain.

Lurker begins dormant only with an authored ambush context (the validation variant enables it; streamed cells opt in through `WorldCellContent.lurkerAmbushContext`). Sight/hearing causes suspicion. Confirmed visible evidence within 6m permits a .85s preparation; then a short approach burst. Heard-only information releases shared investigation without giving player-transform knowledge. After engagement it uses shared pursuit/search with bounded cycles; it does not teleport or magically rehide. Scan corners, inspect the bent/asymmetric upper-body posture, listen for the soft cue, and retreat during preparation/recovery. No CPU lighting estimate or new flashlight system was introduced.

Runner has a .65s preparation, a maximum 1.6s burst, and 2.4s recovery. Cooldown starts at burst interruption/end and includes recovery. During preparation/recovery it cannot begin attacks; visible contact during burst can start the ordinary attack clip. An attack ends burst movement, and completing/aborting a strike enters recovery. LOS loss, stagger and disabled legs interrupt burst. Break sight around cover, force a turn, then move during recovery. Base pursuit is deliberately slower than burst.

## Authority and presentation
Both clips retain authored commit/contact/recovery normalized times; playback .8 lengthens their gameplay and visual timing together. New archetypes reject profiles with either contact earlier than .5s. At most .1s of attack clock is consumed per tick after a hitch. Windup turning and animation receive that same step. Animation events never grant damage. Existing distance, arc and world-LOS validator remains authoritative. A blocked crowd contact consumes the strike and reports `CrowdSpacing`; no delayed hit is queued. The .28s target-owned guard separates different attackers and resets on target disable. Single-actor legitimate strikes retain their own timing.

Temporary visual identity uses the current Shirtless Humanoid and existing animations: Lurker upper-body lean/asymmetry, Runner forward lean and burst rhythm. Pose overlays restore before Animator evaluation, hit reactions, attacks and teardown. They never move the root. No negative scale, material recolor, raw-curve retarget or new controller is used. The archetype prefabs are complete project-owned copies of the production runtime prefab, preserving its anatomy, wound mapping, Animator and nested model reference. They are not Unity Prefab Variants; shared topology changes must be applied to all three prefabs. ZombieMale_AAB and Shirtless import metadata declare Humanoid; that is not evidence of alternate-model retarget quality. SZombie Variant 1 has both generic (animationType 2/avatarSetup 0) and Unity Humanoid (3/1) imports; these are not interchangeable raw-curve or anatomy bindings. Alternate model/silhouette acceptance remains deferred pending actual Unity inspection and benchmark images.

Five state-routed spatial cues: LurkerSuspicion, LurkerTelegraph, RunnerPreparation, RunnerTelegraph, RunnerRecovery. Critical warnings use the existing protected voice allocation/caption path. Local cue subsets give a development identity; listening quality is NOT_RUN. ZombieHorrorPackageFree license evidence remains unresolved; commercial approval is not claimed. Playback is independent of gameplay hearing.

## Population/save
Initial composition is deterministic FNV-1a over authored seed + cell + index, using catalog weights. Existing anonymous saved tokens default to Shambler; changing weights only affects new sessions, never restored IDs. Candidate placement uses bounded deterministic attempts, existing safe radius/frustum/collision/NavMesh checks. One actor is instantiated per materialization call; failed initialization keeps the logical record.

Logical records now carry stable ID/type/health/anatomy. Migration carries those same records and transfers ownership only at arrival. Physical dematerialization returns the same record; death moves it to a non-materializable tombstone and increments the existing dead ledger. Derived type counts never form another mutable ledger. `logical + physical + migrating + dead` remains the accounting authority. Named encounter snapshots retain ID/type/health/anatomy through resident and cell hydration; existing loot ownership is untouched.

Optional `archetypeId`, migration `actors`, and population `tombstones` extend the current schema without incrementing it. Absent/empty ID means Shambler, absent migration actors mean legacy anonymous Shambler, absent tombstones retain aggregate legacy dead counts. Unknown nonempty IDs, excessive health, duplicate IDs, live tombstones and dead living records reject before mutation. Codec normalizes absent nested anatomy exactly as the existing pipeline does. Mid-attack restore starts from non-damaging shared acquisition/windup; it never resumes an already committed hit. No original save is rewritten during failed validation.

## Danger authoring
`ZombieEncounterRisk` weights composition and visibility, interior/ambush context, route count, constrained traversal, pressure, exposure, approach distance and recovery spacing. `ZombieEncounterBudget` exposes selected-object diagnostics; the acceptance route displays risk and reasons. It is an authoring utility, not an adaptive player-power director. Production visibility guards remain independent and authoritative.
