# Combat feedback overhaul — 2026-10-10

**IMPLEMENTED — VERIFICATION IN PROGRESS. Production acceptance withheld; anatomical cut artwork remains gated.**

No Unity process, compilation, EditMode/PlayMode test, build, automated acceptance or performance run was launched by the assistant. The user later supplied Unity logs/XML, reviewed below. Existing S020 material edits, S013Cabin scene work, input-prompt moves and supplemental imports were preserved. No commit, push, reset, stash or vendor asset edit.

## User verification received on 2026-10-10

| User run | XML/log result |
|---|---|
| StandaloneOSX batch compile | `logs/1_compile.log` ends with successful batchmode exit; no C# compile errors found. This is not a standalone player build. |
| CharacterIntegrationAssetTests EditMode | 7/7 passed. |
| CharacterIntegrationPlayTests PlayMode | 12/13 passed; `VehicleIKBindsToSteeringWheel` failed on an unexpected setup error log. The character test source changed after this run and now expects that log; no newer result exists. |
| `Player|Combat` PlayMode | 32/34 passed; two `CombatFeedbackPlayTests` count assertions failed. The current test source changed after this run, so those counts need a fresh result. |
| Full EditMode | 668/669 passed; `S015AudioTests.SelectedImportsUseStreamingForLoopsAndMonoForWorldCues` failed because new Kenney world-impact clips retained stereo import metadata. |
| Full PlayMode | 342/353 passed; six character cases encountered an Input System `ArgumentNullException(statePtr)`, plus the vehicle case, two combat count cases, one audio cue-order case and one near-muzzle case. |

Follow-up changes: the short muzzle probe now reaches just beyond the camera-selected contact while remaining capped by its obstruction range; project-owned mono-import copies of 20 CC0 Kenney impact clips replace direct vendor references; the audio test accepts an impact cue following a real gunshot; and the unequip presentation assertion compares against its pre-action count. Source-control whitespace checks and a static check of all 105 audio catalog clip references pass. Full PlayMode's Input System exceptions remain unresolved and need fresh evidence after the concurrently edited character tests are rerun. All six earlier runs describe the pre-fix snapshot.

The user then ran `logs/7_combat_audio_editmode.xml` with `CombatFeedbackTests|S015AudioTests`: **40/40 passed**, with no skipped or inconclusive cases. `logs/7_combat_audio_editmode.log` reports a successful test-run exit and no C# compile errors. This supersedes the earlier EditMode import failure for the focused suites. The PlayMode fixes and broader regression remain unverified.

## Diagnosis and decisions

| Finding | Evidence and disposition |
|---|---|
| Discovered target was not a damage receipt | Resolver assigned `Target` before `TakeDamage`; root health explicitly rejects collider-bearing damage. Added a receipt-returning interface for zombie authority; legacy `Target` remains compatible. Rejected damage cannot produce blood/impact confirmation. |
| Ordinary/fatal blood feedback absent | Blood presenter subscribed only to sever presentation. Surviving-only `Damaged` intentionally excluded death shots. Added `DamageCommitted`, preserving `Damaged`/`Died` semantics and publishing the final hit after health mutation, before death cleanup. |
| Invisible trajectory and invalid miss endpoint | Resolver returned zero hit point on misses; no tracer existed. Misses now include the camera maximum-range endpoint, muzzle position and direction. Full muzzle path still chooses the first obstruction. A short-range probe includes a 10 cm endpoint tolerance, capped by its configured obstruction range, to retain nearby hits. |
| Weapon impact/casing integration absent | Production rifle has null `impactPrefab` and `casingPrefab`. New explicitly bound feedback profile supplies world-space marks/debris/tracers and bounded cosmetic casings. Legacy prefab pools now expire and do not travel with the viewmodel. |
| Root blocking is **not proven** in production | Runtime zombie root is layer 2; anatomical regions are non-trigger layer 8. Rifle mask 307 includes 8 and excludes 2. Added regression coverage for this exact setup and for intentionally misconfigured root discovery. Root damage bypass was not introduced. |
| Cut quality is not approved | Prior targeted topology record reports closed arm/hand surfaces but dark neck/shoulder seams and incomplete head topology. No reliable paired wound caps exist. All five production sever bindings now have `visualSeverApproved: false`; authoring preserves that gate. Logical sever, disabled hit regions, right-arm/hand restrictions, death and saves remain active. |
| Reaction repetition | Existing controller already arbitrates cooldown/severity and vehicle priority. Added bounded reinforcement without resetting the reaction clock, anatomical bones, and explicit restoration of procedural offsets. No gameplay stun or recoil balance change. |
| Runtime visibility/quality | Shader import, skin projection drift, culling, flash exposure, collider aim and perceived sound/weight remain user-verification questions, not observed runtime defects. |

## Architecture and integration

Accepted `WeaponController` action consumes one round → resolver camera selection and muzzle obstruction → one selected `IDamageable` dispatch. `ZombieHitRegion.ReceiveDamage` validates ownership and multiplier → `ZombieHealth` commits anatomy and health → immutable `DamageReceipt` → deferred sever presentation and `DamageCommitted` → original surviving/death event. The returned shot includes ID, weapon category/fire mode, muzzle, resolved endpoint, geometry contact, surface, receipt, applied health loss, terminal/sever flags and severity. Sever-induced terminal health loss can exceed the incoming numerical hit; both values are retained separately.

Presentation consumers never apply damage or emit AI noise. Existing accepted-shot noise, ammunition, reload, equip, ADS, simulation aim and recoil remain authoritative. `PlayerCombatController`, `WeaponRuntimeState`, `WeaponViewPresenter`, `WeaponAnimationPresenter`, `WeaponRecoilController` were inspected and retained.

- `BallisticFeedbackPool`: 8 streak slots; every third accepted shot by default, 7 mm width, 800 m/s visual travel, 45 ms lifetime. Endpoints stop at geometry. A barrel already clipped through geometry suppresses its tracer. 24 surface marks, 18 s lifetime, 45 m effect distance, edge support probes, attached-surface tracking; 8 debris emitters. FIFO replacement. No secondary collision damage.
- `ImpactSurface`: explicit override, existing `AudioSurface` mapping, terrain fallback. Flesh/head critical, metal, stone, wood, soil and unknown. No material-name heuristics or per-shot asset lookup. Unknown surfaces use neutral debris/audio.
- `CosmeticCasingPool`: 10 small project-owned primitive shells, gravity and bounded swept Default-layer contact, 2.5 s lifetime; no Rigidbody, gameplay collision, sound or noise. These are simple brass approximations, not detailed authored cartridge meshes.
- Blood: reuses the installed Vefects derivatives. Adds 12 small ordinary-hit slots, separate from the existing 16 sever slots, 32 marks and 16 corpse leases. Critical/terminal bursts are larger; approved sever uses its existing burst/spurt/drip path without a duplicate ordinary burst. Hit slots live at most 1.5 s; corpse leases also allow the remaining death animation to complete before population cleanup.
- Local wounds: eight hits per actor in a ring. Shader projection onto shared original skinned geometry; bone-following contact centers and normals; only supported split-body renderers draw overlays. No child wound cards, runtime mesh cutting, hit-time materials or per-frame whole-scene search. Lit by SH/main/additional lights, including flashlight. Existing saved torso-wound state stays intact. Localized shot history is temporary presentation and clears on restore/reuse/gore OFF.
- Animation: chest, head and left/right upper-arm selection; existing clips, death and vehicle logic remain. Coalesced hit strength caps at 1.35 without lengthening reaction duration. Applied procedural offsets are removed before the next sample and on cleanup.
- Detached physics: existing bake-on-sever pool retained, volume-related mass, damping, continuous collision, Default-world-only collision, bounded torque/lifetime and owner/session/scene cleanup. Enabled only for disposable art previews until cuts are approved.
- Audio: existing 16-voice production budget, variation, mixer, attenuation, pause and owner cleanup. New fixed-position impact playback follows the hit position rather than moving with the shooter. Existing gunshot composites, reload mechanics and vocal reactions are retained. No additional synthetic firearm tail, second noise event or extra audio emitter framework. Detached landing audio is not added.
- Preferences: Controls/appearance page adds graphic gore, blood intensity and reduced combat effects. Gore OFF clears graphic pools and localized wounds immediately; re-enable does not replay impacts. Intensity applies to newly started particles. Health/anatomy/ammo/noise never read these preferences.

PC renderer has SSAO, Mobile renderer has no renderer features. Neither has a Decal Renderer Feature. Both remain unchanged. Supported-surface quads and skinned overlays avoid adding a renderer-wide decal pass; their extra draw/overdraw cost is still unmeasured. Warmup allocates overlay renderers and fixed pools. Wounds render only for relevant hit anatomy. New markers: `LastSignal.Combat.BallisticFeedback`, `.BloodHit`, `.LocalizedWounds`, `.DetachedBake`.

## Assets and provenance

- Actual installed blood source: `Assets/ThirdParty/VFX/Vefects/Free Blood VFX/` (the old `Assets/LastSignal/Blood VFX/` location is no longer current). Reused existing project-owned burst/spurt/drip/material derivatives and torso wound texture. No vendor asset or imported source geometry was modified; existing vendor licensing remains applicable.
- Kenney Impact Sounds: supplied `License.txt` explicitly states CC0. Metal/wood/mining/soft/generic variants use project-owned copies with mono Unity import metadata; vendor originals remain unchanged. See `Assets/LastSignal/Audio/CombatFeedback/PROVENANCE.md`.
- Supplied `423301__u1769092__visceralbulletimpacts.wav`: [original Freesound page](https://freesound.org/people/u1769092/sounds/423301/) checked on 2026-10-10, Creative Commons 0. Seven project-owned mono impact excerpts with short fades were authored from the supplied waveform; the original was preserved. Envelope-selected edits, not audition-approved mastering.
- Supplied `BloodDecals` PBR textures/materials were found. No local license/provenance file was found in the targeted listing; they are not integrated. Existing verified project derivatives provide the fallback.
- New shaders/materials/profile are project-owned. No downloads, paid assets or framework dependencies were added.

## Changed files

See `changed-files.txt` for exact source/asset/meta paths. Main responsibilities:

| Group | Files |
|---|---|
| Contract/trajectory | DamageReceipt, WeaponFireResolver, WeaponController, WeaponDefinition, ZombieHealth, ZombieHitRegion |
| Weapon/world presentation | CombatFeedbackProfile, ImpactSurface, BallisticFeedbackPool, CosmeticCasingPool, WeaponVfxPresenter |
| Blood/wounds | ZombieBloodVfxPresenter/Pool/Profile, ZombieLocalizedWounds, LS_LocalizedWound shader |
| Reaction/anatomy | ZombieAnimationPresenter, ZombieController, ZombieDismemberment, ZombieDetachedPartPool, StudioNewPunchVisualAuthoring |
| Audio/preferences | WeaponAudioPresenter, ProductionAudio, AudioCatalog, AudioSettingsView, S015AudioAuthoring, S015AudioCatalog, seven WAV derivatives |
| Bindings/assets | Runtime zombie and rifle prefabs, ZombieBlood and CombatFeedback profiles, four materials, two new shaders and metadata |
| Verification handoff | CombatFeedbackTests, CombatFeedbackPlayTests; explicit disposable preview setup in three existing sever test files; CombatFeedbackManualRoute, validation scene, CombatFeedbackBuild, runner |

## Test coverage authored for the initial handoff

New focused EditMode cases cover committed vs discovered targets, rejected intake/invalid damage, exact fatal notification ordering, reentrant terminal ownership, hit identity, miss range/parallax, root/child selection, multi-collider single dispatch, muzzle wall/clip obstruction, region multipliers, material mapping, gated anatomical thresholds/dependencies/restore, profile budgets and audio bindings.

New focused PlayMode cases exercise the actual production rifle and zombie regions: torso/head/arms, fatal impacts, misses/empty/rejected actions, ammo conservation, localized skin surfaces, ordinary blood, wall marks, missing support, tracer capacity/expiry, wound/burst capacity, gore toggle, disable and reset. Existing sever/blood tests explicitly enable visual bindings on disposable instances; their assertions are unchanged. The production gate has its own test. This isolates lifecycle behavior from art acceptance; it does not declare caps acceptable.

Existing focused blood/sever, reaction, weapon, vehicle and full regression groups remain available. The user-run results above supersede the initial unverified status; full compatibility remains blocked by the listed failures.

## Initial user verification handoff

Close this project's Unity Editor. Do not delete its lock file. Run only this first group:

```bash
bash '/Users/burakcoskun/Last Signal/Docs/Implementation/Combat/CombatFeedbackOverhaul/Evidence/run-verification.sh' focused-edit
```

Each invocation creates `Evidence/user-YYYYMMDD-HHMMSS-<mode>-XXXXXX/` (Istanbul timestamp) containing `request.txt`, `unity.log`, `results.xml`, `exit-code.txt`. The terminal prints the exact directory. Send the complete XML/log/exit code; if XML is missing, send the remaining files. Missing XML, zero tests, skipped/failed/inconclusive cases, process failure or unexpected compile/runtime errors block advancement. The script does not infer success from exit code alone. Log review remains required.

Runner operations, **for later steps after reviewing preceding evidence**: `focused-play`, `regression-edit`, `regression-play`, `full-edit`, `full-play`, `build`, `performance`. Build uses a unique `LastSignal.app` and `build-summary.txt` inside that run's evidence directory. `performance` prints the manual capture route; it does not manufacture an automated performance verdict. Some legacy full-suite fixtures write their own historical diagnostic paths; the new runner's own evidence remains unique.

Open-Editor alternative: Window → General → Test Runner → EditMode → select `LastSignal.Tests.CombatFeedbackTests` → Run Selected. Export XML and copy the Editor log. Do not run both workflows concurrently. Next comes focused PlayMode, evidence review/fixes, broader regressions, development build, visual acceptance, performance and only then a production decision.

## Manual visual acceptance (after automated evidence review)

The current scene path is **`Assets/LastSignal/Scenes/Validation/ZombieAcceptance.unity`**, not the old root Scenes path. Start the session with its normal UI. Mouse aims, left mouse fires, right mouse ADS, R reloads; use the existing control prompts for movement. Manual controls are present only in Editor/development builds:

| Control | Action |
|---|---|
| F6 | Replace the encounter with 1, then 10, 20, 30 production zombies on the existing baked arena |
| F7 | Toggle graphic gore |
| F8 | Day/night directional and ambient lighting |
| F9 | Toggle reduced ballistic effects |
| F10 | Spawn a disposable single actor with unapproved-cut preview toggled; production prefab remains gated |
| F11 | Add reserve rifle ammunition; R still performs the authoritative reload |
| F4 / F5 | Toggle a bounded inspection canopy / camera-mounted inspection flashlight |

macOS may require Fn with function keys. The canopy is at (-6,3,3); the CornerWall/CornerReturn remain useful for occlusion and grazing impacts. Tests, firing, camera movement, profiling and screenshots are never driven automatically by this component. Preview cuts are explicitly labeled NOT ART ACCEPTED.

Capture close and medium torso/head/left-right arm shots, missed shots, a muzzle against a wall, grazing/edge impacts, sustained automatic fire and death. Expected: one round/accepted action; endpoints at the actual obstruction; localized burst and wound on accepted damage; fatal confirmation still present; distinct material audio; bounded cleanup. Move around and animate the wounded actor to check surface drift/clipping. Inspect open daylight, canopy shade, darkness and flashlight. Toggle gore during active particles; re-enable must not replay them. Repeat after scene/session restart and cell unload in the production route.

F10 is an **art inspection**, not completed dismemberment delivery: capture neck/shoulder/wrist cuts on both actor and detached piece in several poses. Do not approve or ungate production until paired cap geometry, material continuity and physics are satisfactory. Unsupported leg/crawl anatomy remains out of scope.

For 1/10/20/30 actors, warm the pools, capture at least 30 seconds each of ordinary hits, automatic fire, terminal hits and optional preview severs. Record CPU frame time, GPU/transparent overdraw, GC allocations, voice count, effect counts and named markers; export Profiler captures. No measured frame-rate or allocation claim is made here. Acceptance thresholds require actual target-machine evidence.

Return XML/log/exit code first, then day/night/flashlight screenshots and short videos covering hit-to-cleanup, plus the profiler captures. Authoritative integration, rendering, cut artwork, sound mix and 30-zombie cost all remain pending.

## Explicit limitations / risks

Anatomical cut meshes were not authored or claimed production-quality: visible sever is deliberately gated. Bone-following projected centers may slide near highly deforming joints; overlay drawing cost and shader compilation require Unity evidence. Environmental marks are conservative small flat supported patches, not full deferred decals; sharply curved/thin geometry may reject them. Unknown material tags receive a default; untagged production materials are not guessed. Ballistic dust/brass is a restrained procedural approximation. Existing composite gunshots were retained, and detached-part landing audio is deferred. No visual, audio, build, automated-regression or performance acceptance exists for this implementation yet.
