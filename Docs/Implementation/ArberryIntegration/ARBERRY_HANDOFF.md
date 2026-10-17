# Arberry integration handoff — 2026-10-14

**IMPLEMENTED / NOT_VERIFIED. Production acceptance pending.** Arberry is the current local Player presentation; this is not production approval. The agent has not executed verification gates. User-operated results from 2026-10-14 are recorded below; the current corrections have not been rerun.

## Implemented

- 12 external URP materials and 37 bounded-resolution texture derivatives, correct body tile assignments, supplied hair coverage, normal interpretation, metallic/smoothness/AO packing and eye specular/gloss.
- Own Unity Humanoid import with valid/human Avatar observed during authoring; actual limbs/fingers mapped, all twist/accessory bones preserved. No embedded animations; existing WorldBody controller reused.
- Modular 11-part world body, one Animator and existing locomotion/visibility/weapon/seat components. Existing gameplay components and capsule retained; Player's world references and five FPS weapon references rebound.
- Eight own world weapon profiles covering five definitions and all three cosmetic pistol variants. Own pistol finger poses; anatomical target seeds and existing physical weapon sockets. Own crowbar contact transferred from the FPS palm. These are authored calibrations, not motion/visual PASS.
- Actual Arberry FPS arms/sleeves/gloves: three skins, 12,294 triangles, shared world materials, seven weapon prefabs. Rifle/crowbar grip components added; all limbs/fingers use actual source bones. Camera-anchored arms, separate FPS elbow hints, weapon-owned ADS contacts and rifle reload support motion implemented.
- Measured glove palm depths (right 36.470 mm / left 36.227 mm) applied to both views; Arberry pistol bind-relative fingers and variant profile resolution retained across swaps.
- Deterministic editor menu switch between Arberry and untouched Diesel. Isolated A/B material fixture scene with six cameras, four lighting presets and actual environment swatches.
- Focused tests and runner filters extended; obsolete active-Diesel and shared-world/FPS-profile assertions updated while independent fallback tests remain.

See [changed files](ARBERRY_CHANGED_FILES.md), [source audit](ARBERRY_SOURCE_AUDIT.md), [material mapping](ARBERRY_MATERIAL_MAPPING.md), [rig report](ARBERRY_RIG_REPORT.md), [weapon matrix](ARBERRY_WEAPON_IK_MATRIX.md), [animation coverage](ARBERRY_ANIMATION_COVERAGE.md), [FPS/vehicle/persistence](ARBERRY_FPS_WORLDBODY.md) and [visual checklist](ARBERRY_VISUAL_QA.md).

## Remaining blockers and limitations

1. Acquired license/receipt not supplied. Exact listing is recorded; distribution approval remains BLOCKED. No-AI handling restrictions were respected.
2. Source Jacket has one self-intersecting polygon discarded by Unity. Source binaries were preserved; inspect for a visible hole.
3. All functional, retarget/deformation, finger/weapon contact, FPS seam, vehicle and visual acceptance is NOT_RUN. Valid Avatar flags alone do not establish animation compatibility. Mouth ORM semantics are corroborated but not documented by a source material graph; normal Y convention and hair culling still need review.
4. Dedicated bandage, vehicle entry/exit, floor pickup, world stow and rifle reload clips are MISSING; existing procedural/state fallbacks and action clips remain. No new gameplay jump or networking was introduced.
5. Source meshes total 120,917 triangles / 13 slots; derived texture estimate ≈157.33 MiB for character textures shared across world and FPS, excluding weapon textures. No LOD source, performance measurements or approved shipping budget. Source body reaches ~1.88 m with soles ~1.4 cm below origin; ground contact must be accepted against the unchanged capsule.
6. FPS now uses the actual Arberry geometry. Shoulder cut boundaries, deformation, near-plane clipping, sight alignment and animated contact still require acceptance. Geometric authoring fits are not runtime or visual PASS.
7. Source ThirdParty content/importer metadata is Git-ignored. Preserve the local licensed asset package and Humanoid importer settings in any handoff; do not assume a code checkout alone contains the character. The existing runner's development-build is macOS; Windows/release acceptance remains outstanding.

## First manual gate

Save and close Unity normally, then run **only**:

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" compile
```

Return that run's `gate-result.json` and focused errors. No gate results were fabricated. [Verification workflow](ARBERRY_TEST_COMMANDS.md) lists later gates; execute them individually only after the previous result is reviewed. Visual and performance statuses stay NOT_RUN until the user records results.

## User-operated gate results and focused corrections — 2026-10-14

Original run: compile PASS; character-edit 43/43 PASS; player-combat-edit 92/92 PASS; full-edit 799/799 PASS; macOS development-build PASS. Character-play failed 19/53, player-combat-play 1/51, locomotion-play 1/18, full-play 22/436. Evidence is under CharacterIntegration/Evidence/20261014-*; these results describe the version before the following corrections.

The full-play failures reduce to these groups:

- 14 tests: WeaponGripProbe unconditionally accessed a distal finger child that Arberry does not have. It now transforms the same bind-relative 75%-length terminal proxy used by finger authoring. Anatomical intersection/contact assertions remain intact; a proxy is not proof of glove-surface contact.
- 3 tests: axe support palm near the bottom of the viewport. Raise the actual FPS ready profile by 4 cm, also retained in authoring.
- 1 test: rifle support arm penetrated the jacket envelope by about 15.5 mm, with exact hand contacts and the existing 12-degree hip clearance correction saturated. Move the authored hip/aim stock pocket 8 cm medially and 6 cm upward in torso coordinates. A geometric fit of the reported failing pose indicates elbow clearance is possible without lengthening arms. Runtime transitions, ADS, stock appearance and all other poses remain pending rerun/review. No envelope shrink, threshold relaxation or runtime clearance-limit increase.
- 1 test: character melee assertion still demanded the legacy crowbar Animator layer. It now requires an active authoritative attack and verifies that procedural ownership suppresses the legacy animation while retaining palm contact.
- 1 test: VAL sprint animation test used the active procedural Arberry asset. It now explicitly selects the preserved VAL fallback before Start; an additional production test checks actual sprint lowering, contact and restoration.
- 2 tests: persistence waited on the disabled VAL Animator after restoring the production rifle. It now requires Ready and valid actual Arberry hand contacts, retaining all ammunition and stale-transaction assertions.

Corrections are IMPLEMENTED / NOT_RERUN. Original user evidence remains unchanged. Run character-play next; review its result before broader player-combat/locomotion/full-play reruns. Build PASS from before these changes is not a production acceptance statement. License, visual and performance blockers above remain.

## Second user rerun and pose corrections — 2026-10-14 00:37

Evidence: `20261014-003635-compile-b38714f9` PASS, `20261014-003653-character-edit-0436779d` 43/43 PASS, `20261014-003726-character-play-0bbcf651` 49/53 passed, four failed. The fail-fast shell correctly stopped subsequent gates. These results supersede the earlier character-play failure count, but precede the corrections below.

- Rifle hip direction had converged within 1.58 degrees of ADS after clearance correction. Authored HipEuler is now (14,-8,0), leaving more than three degrees separation even under the existing maximum 12-degree clearance correction. ADS orientation and clearance limit are unchanged.
- Axe slide pose reached the existing 10 cm shared-root correction budget with 7.4 mm right-arm penetration. Its world ready grip seed moves forward 35 mm. No envelope or test tolerance is reduced.
- Machete neutral FPS wrist was 105 degrees. Joint wrist/framing authoring selected FPS Euler (22,0,-25): neutral analytic wrist 77.48 degrees, silhouette viewport (0.84,0.91) at 60 degrees / 16:9. This is authoring geometry, not a PlayMode/visual PASS.
- DXT middle finger entered the rigid frame. Pistol authoring now uses measured 36.470 mm palm depth and fits middle/ring/little three-segment routes around the actual selected frame cross-section. Joint lengths and bind-relative rotations are retained; no runtime collision solver is added. DXT/Glock17/Custom9mm finger assets were regenerated locally. Legacy pistol retains its model contract. The FPS authoring menu now regenerates these routes after palm measurement, preventing stale finger poses after changing palm depth.

Unity was opened normally for local authoring; no Play session, test gate or build was executed by the agent. Current changes are NOT_RERUN. Save and close Unity normally before the next user-operated character-play gate. Test assertions and tolerances were unchanged in this correction round.

## Third user rerun and bounded solver correction — 2026-10-14 00:57

User evidence: `20261014-005653-compile-dfdffd4e` PASS, `20261014-005714-character-edit-58046177` 43/43 PASS; `20261014-005747-character-play-f4592f53` 50/53 passed. Pistol frame routing, machete wrist/framing and rifle hip/ADS distinction passed. Three remaining failures: rifle reported 12.002918 degrees against a 12.001 limit; axe swing left sleeve penetration 3.016 mm; axe slide left sleeve penetration 17.991 mm with the 10 cm placement correction saturated.

Current correction, NOT_RERUN:

- Use normalized quaternion clamping and atan2-based angle measurement for actual hip clearance rotation. This bounds the applied rotation rather than clipping its diagnostic value. Add repeated-compound-rotation tests at 0/6/12 degrees; include CharacterArmClearanceTests in character-edit.
- Search the full bounded melee translation volume before local refinement; previous axis-only descent could stick at the reach projection boundary. Refine to 1.25 mm steps instead of stopping at 5 mm.
- Add optional profile-bounded whole-weapon rotation about the primary wrist, jointly optimized with translation. Arberry WoodenAxe opts into at most 12 degrees; other profiles default to zero. Both grip sockets and their rotations move together. Final weapon-root displacement remains bounded to the existing 10 cm; no bone length, gameplay origin or damage authority changes.
- Penetration (<3 mm), contacts, reach and 10 cm placement assertions are unchanged. Additional orientation freedom changes the presentation trajectory and requires visual review. Runtime cost of the expanded conditional search remains unmeasured; no performance PASS is claimed.

The agent did not launch Unity or execute any verification gate during this correction round. Re-run the fail-fast block after saving/closing the Editor; current source must be compiled by the user before accepting results.

## Fourth user rerun and slide-specific grip seed — 2026-10-14 01:05

User evidence: `20261014-010406-compile-52649f55` PASS; `20261014-010441-character-edit-6a159431` 52/52 PASS; `20261014-010513-character-play-7f8d68a8` 52/53 passed. Remaining failure is axe slide left-sleeve penetration (13.136 mm); rifle angle budget and axe swing now pass.

The two-hand world slide pose previously reused the ready target with a hardcoded 2 cm drop, despite the retargeted slide pulling the support shoulder inward/backward. Add a validated, torso-space WorldMeleeSlideOffset to the profile. Its default (0,-0.02,0) preserves the previous behavior for other assets. Only Arberry WoodenAxe sets (-0.06,0.04,-0.03), smoothly weighted by the existing slide blend. This shifts its slide seed 6 cm medially, 6 cm upward and 3 cm back relative to the prior slide seed. Local geometry fitting of the reported shoulder/wrist configuration admits clear elbows at this displacement; it is not runtime evidence. The 10 cm correction budget, 12 degree rotation budget, arm lengths and all test assertions remain unchanged. FPS presentation and non-slide poses are unchanged. Probe diagnostics now include slide state and authored slide offset.

Current correction NOT_RERUN. No Unity launch, verification gate, Play session or build was executed by the agent. Re-run the fail-fast block and return the first failing result or completion.
