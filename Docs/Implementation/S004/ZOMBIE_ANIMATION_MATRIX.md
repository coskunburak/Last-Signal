# Production animation matrix — S004-B0B

**All five required production roles integrated and visually accepted.** [B0B report](S004_B0B_ZOMBIE_ASSET_INTEGRATION_REPORT.md) · [evidence](Evidence/20260918-B0B/README.md)

All vendor clips below are Humanoid, 30 fps, zero events, source Avatar `ZombieModelAvatar` valid. All six animation FBXs share 52/52 mapped hierarchy paths with their own `Model/ZombieModel.fbx`; retain their existing CopyFromOther setting. They never copy the unrelated Hotstrike Avatar. Mecanim performs the cross-skeleton retarget.

Sources are under `Assets/LastSignal/Assets/Kevin Iglesias/Zombie Animations/Animations/`.

| Role | Exact FBX → Unity sub-asset | Duration / fps | Loop Time / Loop Pose | Imported average root speed m/s |
|---|---|---|---|---|
| ATTACK | `Zombie@Attack01.fbx` → `Zombie@Attack01` | 1.333 s / 30 | False / False | (0.0000, 0.0000, 0.0000) |
| HIT_REACTION | `Zombie@Damage01.fbx` → `Zombie@Damage01` | 0.500 s / 30 | False / False | (0.0000, 0.0000, 0.0000) |
| DEATH SOURCE | `Zombie@Death01_A.fbx` → `Zombie@Death01_A` | 1.333 s / 30 | False / False | (0.0000, 0.0000, 0.0000) |
| REJECTED ALTERNATE | `Zombie@Death01_A.fbx` → `Zombie@Death01_B` | 1.333 s / 30 | False / False | (0.0000, 0.0000, 0.0000) |
| IDLE | `Zombie@Idle01.fbx` → `Zombie@Idle01` | 2.533 s / 30 | True / False | (0.0000, 0.0000, 0.0000) |
| IDLE_VARIATION | `Zombie@Idle01_Action01.fbx` → `Zombie@Idle01_Action01` | 2.833 s / 30 | False / False | (0.0000, 0.0000, 0.0000) |
| LOCOMOTION | `Zombie@Walk01.fbx` → `Zombie@Walk01` | 1.467 s / 30 | True / False | (0.0000, 0.0000, 0.0000) |
| STRIDE REFERENCE ONLY | `Zombie@Walk01.fbx` → `Zombie@Walk01 [RM]` | 1.467 s / 30 | True / False | (0.0010, 0.0000, 0.8998) |

Production `Death` uses **`Assets/LastSignal/Enemies/Zombie/Animations/LS_Zombie_Death.anim`**, derived from `Zombie@Death01_A`. It changes only normalized body `RootT.y` for target-mesh floor clearance. All 129 other curves are identical. Raw A penetrated 23.08 cm; raw B 17.63 cm. Neither raw Death is production accepted. Corrected A holds the final pose at ~5 mm clearance; worst sampled transient penetration is 5.74 mm. Source flip/backward-fall choreography is preserved, not redesigned. B is preview-only and omitted from the production controller.

## Retarget acceptance and timing

- **Idle PASS:** visible body breathing/sway, coherent shoulders/wrists, planted feet with small vertical variation. Hunched posture is authored. Two full cycles in continuous evidence; no root drift or major loop jump. Loop Pose remains off after inspection.
- **Walk PASS:** front/side/rear knee, ankle, arm, hand and spine motion coherent. No tearing, scale pumping or inversion. Minimum skin height .0089–.0362 m: modest foot clearance/slide is a P2 tuning limitation, not an extreme penetration defect. Loop Time on, Loop Pose off. Production is in-place.
- **Attack PASS (visual only):** readable right-arm swing with torso follow-through. Right-hand forward reach peaks around **.344 s**, z≈1.237 m relative to actor root. Candidate contact window **.30–.40 s**, to validate against actual P3 target/reach; not authoritative damage timing yet. Total **1.333 s**; returns to initial pose.
- **HitReact PASS (visual only):** short recoil, coherent skeleton, useful response **.05–.35 s**, returns by **.5 s**. No health or reaction gameplay.
- **Death PASS for corrected A:** strong stylized backward fall/roll; settles by **~1.1 s**, length **1.333 s**, final pose persists for at least two additional seconds in production-controller PlayMode test. Flat-ground acceptance only. No corpse/death gameplay.
- **IdleVariation previewed:** broader head/torso scan; non-looping **2.833 s** and returns to base pose. Main Idle chosen for subtle continuity; variant exposed as a manually driven optional state.
- **Raw Death B previewed and rejected:** different arm finish still intersects ground; unnecessary alternative for first Shambler.

Exact GUID/file IDs, settings, sample data and state mapping: [inventory](Evidence/20260918-B0B/animation-inventory.tsv), [controller](Evidence/20260918-B0B/animator-mapping.tsv), [129 preserved curves](Evidence/20260918-B0B/death-adapter-provenance.txt). Each candidate has a seven-time, three-angle contact sheet. Continuous evidence evaluates the actual production controller at 24 fps; PlayMode tests evaluate at 60 Hz.

**RUN = NOT REQUIRED FOR SHAMBLER V1.** Alert/turn, alternate attacks, crawl/climb and gameplay transitions remain outside B0B.


## P3 authoritative Attack usage (2026-09-19)

Fresh actual-rig samples and accepted baked pose renders: [clip audit](Evidence/20260919-P3/clip-audit/README.md). Source clip remains 1.333333 s, 30 fps, zero events, non-looping. P3 uses .8 playback: commit normalized .15 / .25 s; contact normalized .25 / .416667 s; Recovering begins normalized .325 / .541667 s; completion normalized 1 / 1.666667 s. Right-hand z reaches 1.234 m at source .333333 s. Recovery retains the rest of the real clip, with no extra hidden cooldown.

Attack begins once on AttackWindup entry. Gameplay drives presentation time; pause freezes it. Runtime test checks actual Animator normalized time against gameplay timer (tolerance .03 normalized). Root motion stays off, navigation stops, no lunge or post-commit tracking. HitReact and Death are still presentation assets only, reserved for P4.
