# S004-B0B — Production Zombie Asset & Animation Integration

2026-09-18 · Unity 6000.5.0f1 · URP · evidence run `20260918-B0B`.
**FINAL STATUS: PASS (technical B0B integration). P2 technical entry: READY; P2 implementation: NOT_STARTED. Local acquisition/entitlement evidence: LICENSE_EVIDENCE_PENDING.**

## 1. SELECTED MODEL

Hotstrike Studio — Free Stylized Dark Fantasy Zombie, Unity SK variant. Actual imported assets were enumerated through Unity AssetDatabase/importers before integration. Model/animation compatibility was proved first; no placeholder/alien substitution.

## 2. SOURCE ASSET PATHS

Model `Assets/LastSignal/Assets/SZombie/SZombie_Variant_1/Unity/SK_SZombie_Variant_1.fbx`; embedded `SK_SZombie_Variant_1Avatar`. Animation root `Assets/LastSignal/Assets/Kevin Iglesias/Zombie Animations`; six source FBXs in `Animations/`, `Model/ZombieModel.fbx` and `Prefabs/Zombie.prefab`. All texture/model/prefab/clip paths are recorded in [inventory](ZOMBIE_ASSET_INVENTORY.md) and `imported-before.txt`. All third-party content remains in its actual directory; scripts are actually under `Assets/LastSignal/Scripts/`, correcting stale historical documentation paths.

## 3. LICENSE STATUS

**LICENSE_EVIDENCE_PENDING** locally for both packages. Public publisher/store/EULA terms are recorded with URLs in [source evidence](Evidence/20260918-B0B/license-source-record.md). User acquisition channel/receipt and exact imported version cannot be inferred. Public Kevin listing version 1.0 is not asserted as installed version. Technical integration is accepted under the supplied-assets request; commercial release clearance remains unverified.

## 4. MODEL TECHNICAL AUDIT

8,284 triangles, 5,468 vertices, one skinned mesh/renderer/submesh/material, 101 skin bones, 103 source hierarchy transforms, zero blend shapes and no LOD. Standing reference skin y≈0–1.808236 m; player capsule 1.8 m. Shoulder joint spacing .380310 m; head reference y1.609738 m. File units convert at .01 with global import scale1; runtime scales remain1. +Y up, +Z facing, ground-origin pivot. No rotation correction required. Final settled skin measurement supersedes an initial transient immediate-import BakeMesh result recorded in `avatar.txt`.

## 5. HUMANOID AVATAR

Selected model importer changed Generic/NoAvatar → Human/CreateFromThisModel. Its own Avatar is valid/human, humanScale1.020219, 52 anatomically appropriate mappings including required spine/head/arms/legs/feet and all fingers. Source reference pose inspected at shoulders/elbows/wrists/hips/knees/ankles: no crossed limbs, inversion or shoulder collapse. [Reference-pose render](Evidence/20260918-B0B/09_reference_pose.png). No forced manual mappings, destructive T-pose change or source hierarchy edits. Animation FBXs retain their own Kevin Avatar; all 52 mapped source hierarchy paths match, not the unrelated Hotstrike rig.

## 6. MATERIAL / URP CONVERSION

Project-owned URP/Lit material; A Albedo sRGB, OpenGL normal copied and imported NormalMap/linear, project packed mask linear. All source channels inspected; Smoothness is exactly inverse Roughness and vendor mask R/G/A exactly matches metallic/AO/smoothness. Correct channels/keywords applied, opaque, no emission. Normal lighting detail is coherent without inverted relief or a major seam; supplied palette preserved. Source texture bytes/importers remain untouched. No pink material.

## 7. ANIMATION PACKAGE INVENTORY

Eight real sub-clips across six FBXs: Idle01 (2.533s), Idle01_Action01 (2.833s), Walk01 plus RM (1.467s), Attack01 (1.333s), Damage01 (.5s), Death01_A/B (1.333s each). 30 fps, all Humanoid, zero events. See [matrix](ZOMBIE_ANIMATION_MATRIX.md) and [GUID/file-ID inventory](Evidence/20260918-B0B/animation-inventory.tsv). No Run required for Shambler V1.

## 8. RETARGET RESULTS

- **Idle PASS:** two coherent cycles, subtle body motion, no major seam/jitter/scale pumping; small contact variation (~1cm).
- **Walk PASS:** examined front/side/rear and continuously; coherent knees/ankles/hands/spine. Modest contact float/slide retained for agent-speed tuning.
- **Attack PASS, presentation only:** readable right-hand strike, candidate contact .30–.40s (maximum forward reach .344s); no damage/events introduced.
- **HitReact PASS, presentation only:** readable .05–.35s response within .5s total; returns coherently.
- **Death PASS using project adapter:** raw A/B failed ground clearance (-.231m/-.176m). Selected A preserves stylized backward fall/roll; measured normalized RootT.y correction places final body at .005m clearance. Worst sampled transient penetration .00574m; 129 other curves unchanged. Settled by ~1.1s; production-controller test proves final hold. Raw B remains preview-only.

## 9. ROOT MOTION ANALYSIS

Production root application disabled. Selected loops/actions have baked Y/XZ/rotation and zero measured accumulated root movement/yaw. No actor teleport in presentation tests. Walk RM retains real forward translation; it is excluded from the production controller. Death correction modifies only body vertical presentation, not root XZ, muscles, rotations or gameplay root.

## 10. VISUAL WALK SPEED ESTIMATE

Target-rig Mecanim delta integration: 1.347476m/1.466667s = **.918734m/s**, approximately **.92m/s** at cadence1. Imported source average .899837m/s differs because retarget scale matters. P2 should tune actual ground travel to this starting estimate; no agent added. Measurements are not a guarantee of zero sliding on slopes or during acceleration.

## 11. FINAL PREFAB

`Assets/LastSignal/Enemies/Zombie/Prefabs/LS_Zombie_Shambler.prefab` → VisualRoot → nested source model with its Animator. HeadReference/ChestReference are added under stable mapped bones; FeetReference is at actor ground origin. Animator must stay with its rig, not a detached sibling. One project material override. Zero runtime scripts, colliders, Rigidbody, AI, NavMeshAgent, health or hitboxes. Gameplay scene/player/rifle remain unchanged.

## 12. ANIMATOR CONTROLLER

`Assets/LastSignal/Enemies/Zombie/Animations/Controllers/AC_Zombie_Shambler.controller`: Idle default, Locomotion, Attack, HitReact, Death, optional IdleVariation. One layer, zero parameters and transitions; manually driven presentation states, no pretend combat authority. Six real assigned motions. Raw alternate death omitted. Editor-only `Last Signal > Zombie Assets > Animation preview` provides Play/Pause/scrub and final hold; acceptance scene contains one production prefab, neutral ground/lights/camera and no gameplay systems.

## 13. PERFORMANCE CHARACTERISTICS

One main color draw estimated plus URP-dependent shadow/depth work. Three unique 2K texture maps: ~13.33MiB compressed GPU mip-chain estimate; current Editor runtime-memory API totals ~26.67MiB including Editor residency. Animator skinning uses101 bones: meaningful later 10/25-population risk alongside missing LOD. No crowd/CPU/GPU benchmark or Windows build claim. LOD = MISSING; RISK = FUTURE POPULATION PERFORMANCE.

## 14. TEST RESULTS

**Full EditMode 38/38 PASS; full PlayMode 23/23 PASS. No skips/inconclusive cases.** Existing 34+21 baseline plus four asset checks and two presentation tests. PlayMode includes existing runtime/three-session smoke, and new actual-controller animated states/root-stability/death-hold assertions.

Initial EditMode was32/34: two exact source-path expectations still referenced pre-existing old vendor paths. Only the two constants were synchronized to actual relocated VAL/MR POLY sources; no assertions, gameplay or B0A recovery logic changed. First PlayMode attempt stalled after Editor focus loss paused SessionFlow; no completion XML was counted. It was stopped and the entire suite rerun with Unity foreground/GameView focused: 23/23. Both incidents are preserved in evidence, not hidden. Final Console snapshot: zero errors, one Unity AI account-service timeout warning unrelated to project code.

## 15. VISUAL ACCEPTANCE

**PASS for the scoped asset.** Required eight PNGs, reference pose, all eight candidates' front/side/rear seven-time sheets, 121-sample motion metrics and a 24fps three-view continuous production-controller video are included. Evidence is actual Unity rendering of the imported Hotstrike model. No severe limb inversion, tearing, body pumping, uncontrollable drift or extreme floor penetration remains on selected motions. Movie is deterministic Editor evaluation, not standalone gameplay proof.

## 16. KNOWN LIMITATIONS

Local entitlement evidence pending; no LOD;101-bone crowd scaling unprofiled;2K texture residency; modest foot float/slide pending velocity tuning; stylized death choreography and flat-floor-only height correction. Reaudit after model/avatar/animation upgrades. No P2 AI, P3 attack authority/player health, P4 regions/death gameplay, spawning/population/save/loot/hearing, Windows release or crowd performance was implemented/certified.

## 17. P2 ENTRY STATUS

**READY for technical P2 work**, based on actual selected model, valid Avatar, URP material, accepted five motions, project prefab/controller, root policy, visual evidence and full regressions. Documentation closes production model/Idle/Locomotion blockers. P2 itself is NOT_STARTED; its navigation, layer/agent tuning, perception and lifecycle gates remain future work. Commercial acquisition evidence remains a release/intake follow-up, not falsely marked legally verified.

## 18. FINAL STATUS

**PASS — S004-B0B technical integration complete.** Source safety audit confirms only one vendor importer `.meta` changed; all original binaries/textures/vendor animation settings and gameplay assets are preserved. [Evidence index](Evidence/20260918-B0B/README.md). License status remains explicitly pending as allowed by this task; this PASS does not assert commercial release clearance.
