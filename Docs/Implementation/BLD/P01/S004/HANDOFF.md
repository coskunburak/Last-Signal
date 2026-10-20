# BLD-S004 / P01 — implementation and owner verification handoff

**Latest correction, 2026-10-19:** see `PhysicalTreeCorrection/README.md` and `PhysicalTreeCorrection/FILES.json` for the confirmed legacy disappearance path, required physical-authoring guard, v2 configurable 16-level cuts, support-centroid joint loading, persistent visible fallen trunk, generated nearby test spawn and new owner regression gates. That report supersedes this original handoff's four-stage/consumed-body visibility behavior and next-action instructions. Historical PASS results below precede the correction and do not validate it.

Prepared 2026-10-18. Project: `/Users/burakcoskun/Last Signal`. Unity project and installed executable: **6000.5.0f1**. Branch: `s012-integrated-graybox-slice`; entry HEAD: `84aa7358be7b3f47517b7e083ff0734e762e210f`.

**Overall: PARTIAL. Code: IMPLEMENTED / TEST_NOT_RUN. VISUAL_NOT_APPROVED. PERFORMANCE_NOT_MEASURED.** No Unity process, tests, gameplay simulation, authoring menu, build, screenshot capture, scene promotion, commit or push was executed by this continuation. Source compilation does not establish Unity acceptance.

## Owner verification update — 2026-10-19

The implementation report below records the original 2026-10-18 handoff. Subsequent owner-run evidence:

- Focused EditMode **PASS**: `Evidence/OWNER-20261019-002758-edit-wpjty5k9/`; 54 passed, 0 failed, 0 skipped (30 harvest, 12 physical persistence, 12 structure). Reviewed results XML, result JSON and relevant Unity log entries.
- Focused PlayMode **FAIL, focused fix awaiting retest**: `Evidence/OWNER-20261019-003002-play-5qli9iks/`; 15 passed, 1 failed, 0 skipped. The sole failure was `P01TreePhysicsTests.RestoredSettledBodyPreservesPoseAndImpactDeduplication`, at the exact Vector3 position equality assertion. Expected/actual both displayed `(1.00, 0.20, 0.00)`; the log does not retain enough precision to measure the discrepancy.
- Replaced exact equality with a strict distance tolerance of 0.0001 m (0.1 mm) after physics synchronization, adding nine-decimal expected/actual diagnostics. Runtime restoration, kinematic, rotation and impact-state behavior were not changed. This treats native floating-point rounding as the likely cause while retaining a tight pose-accuracy check; the owner retest must confirm it.
- Modified PlayMode source compiles against cached references (exit 0). No agent-run Unity test or simulation occurred. Do not mark PlayMode PASS until owner evidence confirms the fix.

The focused retest command issued for that failure was:

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py play --filter 'LastSignal.Tests.P01TreePhysicsTests.RestoredSettledBodyPreservesPoseAndImpactDeduplication'
```

Later owner evidence, reviewed 2026-10-19:

- `Evidence/OWNER-20261019-003515-edit-lqb78kno/`: focused EditMode **PASS**, 54/54, no failures or skips.
- `Evidence/OWNER-20261019-003532-play-ye7xlj7_/`: focused PlayMode **PASS**, 16/16, no failures or skips. `RestoredSettledBodyPreservesPoseAndImpactDeduplication` passed, verifying the tolerance fix in the owner-run full focused PlayMode gate.
- The chained `physics` command rejected the S003 `FirstTownHarvest_20261018150846-032db4e9.unity` source path before starting Unity. This is a missing S004 stage prerequisite, not a physics test failure. The following chained persistence/regression gates did not run. No `FirstTownTreePhysics_*.unity` scene exists at this review.
- **Current next owner action:** open the saved S003 source scene in Unity, select one eligible ResourceNodeView tree root and invoke `Last Signal/BLD/P01/S004/Create Single Tree Physics Stage`. Follow the clearance review below before running the physics gate with the newly generated path. Do not rename the S003 scene to bypass validation.

Owner authoring then reported the combined source-scene precondition exception. That message did not identify whether the active scene was invalid, dirty, the wrong source, or accompanied by additional scenes. The authoring preflight now reports separate `S004_SOURCE_SCENE_MISSING`, `S004_MULTIPLE_SCENES`, `S004_WRONG_SOURCE_SCENE` and `S004_SOURCE_SCENE_UNSAVED` diagnostics with relevant paths/counts. Selection must also belong to the active source scene (`S004_SELECTION_OUTSIDE_SOURCE`). Existing source protection remains; no automatic save, close, discard or authoring was executed. Editor source-only compilation returned exit 0. The original scene-state cause and successful generation still require an owner retry after Unity recompiles.

The next owner invocation specifically reported `S004_SOURCE_SCENE_MISSING`, confirming that the active scene was invalid, unloaded or had no saved path. The menu now handles that condition: it resolves a selected existing S003 SceneAsset or the sole existing S003 validation scene; if ambiguous it opens a scene file picker. It uses Unity's standard modified-scene save/cancel prompt before opening the existing source in Single mode. It then displays a menu of intact resident ResourceNodeView candidates; choosing a tree re-enters all preflight checks and generates the isolated stage. The same tree picker handles a missing tree selection in an already-open valid source. Canceling either prompt/menu performs no generation. Existing dirty/wrong-source/multiple-scene guards remain. This recovery implementation compiles source-only; no agent invoked Unity or the authoring menu. **Next owner action: wait for Unity recompilation and invoke Create Single Tree Physics Stage again, then choose the tree from the menu.**

## Resume and completion

Owner screenshot follow-up (2026-10-19): an upright tree showed “Wait for the tree to settle” beneath the pause menu. Source inspection found that all `HarvestReason.Busy` responses shared that message, including an unavailable world. The saved S003 scene currently has `clearanceApproved: 0` and null physical-body references; no saved S004 scene was found during this review. Busy feedback now distinguishes clearance, recovery, load/pause, cell readiness and the actual concurrent-body limit. Gameplay status text is hidden during pause/menu/death. Runtime source-only compilation returned exit 0; no Unity execution occurred. Clearance was not enabled and no scene was modified. A screenshot cannot establish the exact unsaved live scene state or motion; this fixes the confirmed misleading feedback mapping.

The working tree already contained S003 durable harvest, pickup, save and cell integration, its FirstTownHarvest staging scene/assets, visual refinement changes, imported examples and OpenFracture package changes. The interrupted S004 work had established the physical adapter, structural model, bark-only authoring approach and audio inventory. These were continued, not replaced.

This continuation completed versioned physical snapshots and validation, durable fracture coordination, partial-cut/mid-fall restoration, lifecycle suspension, missing-component recovery, late callback guards, accepted-contact feedback, bounded physical contact audio, explicit tree noise integration into existing pressure/hearing, source-only validation, owner-run fixtures and a one-gate verification runner. It retained the existing melee active window and two-handed grip implementation. No competing harvest, inventory, save, audio or AI owner was introduced.

The scene authoring implementation is ready for the owner to invoke; **no S004 generated meshes, prefab, bank, profile or scene exist yet**. Audio candidates remain unmapped pending audition and rights evidence. A single resident tree is the supported authoring pilot. Broad forest conversion and automatic promotion are absent by design.

See `CHANGED_FILES.md` for every source/meta/document change and `PRESERVATION.json` for the entry-hash comparison. Entries already dirty at the start are explicitly distinguished from S004 additions.

## Card status and dependency map

Paths below are relative to `Assets/LastSignal/Scripts/`; complete file paths are in the manifest.

| Card | Status | Files / resulting behavior | Prepared verification | Remaining acceptance risk |
|---|---|---|---|---|
| D025 Axe interaction | IMPLEMENTED_BUT_UNVERIFIED / TEST_NOT_RUN | Existing two-handed grip retained; `Runtime/Combat/MeleeWeaponController.cs`, `MeleeWeaponPresenter.cs`; accepted physical wood contact suppresses flesh fallback | Existing MeleeGrip, CharacterWeaponPose and S021 fixtures; new active-window cases | Owner FPS/TPS grip/contact/recovery/weapon-switch video |
| D026 Target validation | IMPLEMENTED_BUT_UNVERIFIED / TEST_NOT_RUN | `MeleeAttackResolver.cs`, `GatheringWorld.cs`, `TreeStructure.cs`; aimed collider ray contact, cut band, tool, revision, side and availability validation | P01TreeStructure/Persistence; real resolver occlusion/range; authored stage actual axe | Real tree/camera geometry and terrain clearance |
| D027 Audio / pressure | PARTIAL | `TreeAudioBank.cs`, `TreePhysicsBody.cs`, `ProductionAudio.cs`, Noise and AI integration files; 9 event slots and existing swing; bounded contact events | Audio regression, contact feedback fixture, noise receipts at 30/60/120, pressure/hearing eligibility | No clips mapped or auditioned; local license evidence missing; spatial mix and real zombie response unverified |
| D028 Structure / fracture | IMPLEMENTED_BUT_UNVERIFIED / TEST_NOT_RUN | `TreeStructure.cs`, `TreePhysicsProfile.cs`, `TreePhysicsBody.cs`, `TreePhysicsAuthoring.cs`; section weakening and real FixedJoint break | Structure tests, actual PhysX fixtures, owner stage joint/transaction test | Material strength, inertia, notch topology and solver response need measurement |
| D029 Fallen tree / logs | PARTIAL | Physical main trunk, static stump, natural rest, existing stable WorldItem pickup identity; `GatheringState.cs`, `GatheringWorld.cs`, `ResourceNodeView.cs` | Contact/obstacle/settling/disable fixtures, mid-fall stage test, existing full/partial inventory tests | One main trunk body; no secondary physical log segmentation. Pickup remains S003 representation. Visual and real cell validation outstanding |
| D030 Collection UX | IMPLEMENTED_BUT_UNVERIFIED / TEST_NOT_RUN | `ResourceNodeView.cs`, `GatheringWorld.cs`, `Session/AcceptanceHud.cs`; axe/broken/unavailable/pending/resting/full inventory feedback through existing UI | S003 pickup tests and owner route | Fallen tree/stump prompt distance, wording and blocked drop usability |
| D031 FirstTown integration | PARTIAL | Owner-only copied-stage authoring, retained Pine/Birch visuals, fresh manifest and save slot, clearance disabled pending inspection | Owner authoring + authored-stage tests | Not physically authored yet; resident pilot only; navigation/roads/hero-tree protection require owner review |
| D032 Verification handoff | IMPLEMENTED / TEST_NOT_RUN | Four new fixtures; `Tools/p01-tree-verify.py`; this handoff and preservation manifest | Seven independently invoked gates and manual acceptance below | No Unity gate has run; no performance or production-ready claim |

Dependency chain: melee ActiveWindow → resolver contact → Gathering cut candidate → SaveSession durable receipt → physical projection/impulse → PhysX joint failure → Gathering fracture candidate → durable receipt → gravity/contact/rest → existing safe drop/pickup transaction. ProductionAudio and VFX observe accepted events; they never authorize resources. Noise uses the existing gameplay event service, pressure adapter and zombie memory.

## Physics implementation

The stable resource anchor never follows the falling tree. A child body carries the original tree's extracted bark and separate foliage. An intact body starts kinematic. A fully constrained world-connected `FixedJoint` is created at the cut height (0.9 m); there is no free hinge axis. Accepted chops activate the body and apply an 8 N·s initial-tuning impulse at the real contact point. The joint holds every degree of freedom until PhysX exceeds its break force/torque.

There are eight radial cut sides and four authored depth stages. The first accepted contact fixes the side; an opposite-side hit cannot add progress. Normalized depth is capped at 0.975, retaining a small section. With x = 1 − 2 × depth, remaining area fraction is `1 − (acos(x) − x*sqrt(1−x*x))/PI`. Bending capacity is `strength * PI * radius^3 / 4 * area^1.5`; axial capacity is `axialStrength * PI * radius^2 * area`. The model is a bounded engineering approximation, not a calibrated wood solver. Defaults are 8 MPa bending and 4 MPa axial. Health stays at least one before actual joint failure; a health threshold or timer cannot release the body.

Mass scales from cut radius and trunk height using 550 kg/m³, a 0.42 taper factor and 12% crown allowance, clamped 20–10,000 kg. Center of mass uses 36% of the above-cut length plus the cut height and 12% of foliage lateral offset. PhysX derives inertia from the compound colliders via ResetInertiaTensor. Two tapered capsule approximations represent the trunk; a static box represents the stump. There are no dynamic nonconvex MeshColliders or leaf rigidbodies. These approximations need visual/collision review on the selected tree.

Gravity is enabled; linear/angular damping are 0.05/0.1, interpolation is enabled, collision mode is ContinuousSpeculative, solver iterations are 12/6, maximum angular speed is 8 rad/s and sleep threshold is 0.005. Attachment trunk/stump contact is ignored only while attached, then re-enabled. Existing project collision layers/matrix apply; no global physics settings changed.

OnJointBreak freezes only for the synchronous durable fracture receipt, then restores the solver velocities. It cannot mint items. Missing body/collider/joint, stale authority, or failed/ambiguous publication enters existing recovery/pause instead of producing free resources. Repeated, disabled, suspended and teardown callbacks are guarded. Late collisions after depletion/rest are ignored.

Actual collision impulse drives the landing threshold (mass × 0.15), one primary landing event, gain, and dust. Later contacts need mass × 0.3, are spaced at least 0.5 seconds, and produce at most two secondary cues. Contact history plus linear speed <0.08 m/s and angular speed <0.06 rad/s for two seconds permits sleep/kinematic rest. A 90-second runaway/unsettled timeout or invalid/excessive velocity requests recovery; it never marks a moving tree collectible. Bark/dust are bounded ParticleSystem bursts (6/12, pool cap 24 each), not persistent debris bodies.

There is **no scripted falling motion** in the new adapter: no animation, tween, Transform.Rotate, quaternion interpolation, MoveRotation, MovePosition or ground snapping. Position/rotation assignment is limited to reset/save restoration. Runtime does no slicing or per-frame global forest search. Gathering limits new activation to four active simulations; restored bodies are not forcibly dropped to meet that budget. Idle bodies are kinematic. On full yield consumption the main trunk renderer/collision is removed from presentation and the stump remains; multi-section physical log breakup is not implemented.

## Persistence and ownership

`ResourceNodeState.treeVersion` is 0 for legacy behavior and 1 for physical trees. `TreeCutSnapshot` version 1 stores side/depth, fracture generation/revision, local impact/direction, fracture/rest/impact flags, pose and velocities. Existing node ID, definition ID/revision, accepted swing receipts, yield identity, pickup quantity and remaining resource budget continue to belong to Gathering. No alternate save file or global save-version rewrite was added.

A valid accepted chop increments one node revision and the existing sequence, consumes one durability unit, and creates no reward. Stamina remains owned by the existing attack simulation. Fracture checks the expected revision and accepted cut history, marks generation 1, and creates one Pending yield with the existing stable `nodeId:yield:1` identity. Duplicate fracture requests have no second yield or tool charge. The existing SaveSession/HarvestDurability pipeline publishes a validated complete candidate before adopting/projecting it. Ambiguous storage outcomes enter recovery. Failed presentation after successful publication retains committed ownership for reload.

Partial cuts restore their notch and constrained body without replaying a strike. A mid-fall save restores pose, velocity and angular velocity into dynamic physics, without replaying fracture, audio or gameplay noise; subsequent physical outcomes need not be bitwise identical. Rested saves restore their accepted pose kinematically. Suspension samples before disabling; resume respects the snapshot. Death/menu/restore/cell-transition guards suspend physics. A real streamed physical-tree cell has not been authored by this tool: runtime hooks and existing cell regression coverage do not establish streamed-stage acceptance.

Snapshot validation rejects malformed versions, side/depth/revision mismatches, non-finite pose/velocity, invalid rotation, impossible fracture generation and premature World/Consumed rewards. Legacy S003 snapshots retain old health semantics; absent old gathering state creates untouched authored nodes under existing compatibility rules. The new stage uses a fresh manifest ID and save slot, so it does not reinterpret an old S003 checkpoint as a physical-tree checkpoint.

Only a naturally resting fractured tree can request safe drop projection. Existing bounded terrain/clearance queries choose a drop; failure leaves Pending resources. Existing WorldItem partial/full inventory transactions remain authoritative and preserve identity/remaining quantity. No physical collision directly awards inventory. These invariants are encoded and covered by prepared fixtures; they are not yet empirically proven in Unity.

## Asset decisions and owner authoring

| Local input | Finding / use |
|---|---|
| OpenFracture, cached `com.dgreenheck.openfracture@6d0ddff64173` | Version 1.0.2, commit `6d0ddff64173193f024638c2de2dbeb8a2525965`, local MIT license. Package says Unity 2021.1 minimum; dependencies test-framework 1.1.30 and codecoverage 1.0.1. Actual assembly name is `RuntimeAssembly`. Only Editor MeshSlicer/FragmentData geometry is reused. Submesh 1 is treated as cut surface, so whole bark+leaf meshes cannot be passed directly. |
| `Assets/TreeChopping-main` | Loose imported example, not UPM. README targets Unity 6; informal reuse language but insufficient adjacent license evidence. Terrain-wide replacement and timed temporary body conflict with this scope. No code reused. |
| `Assets/Unity-Tree-Chopping-Simulation-main` | Loose legacy example, no adequate local license/version evidence. Piece-heavy physics, global searches and independent controller/lifecycle conflict with project authority. No code reused. |
| Pine/Birch under `Art/FirstTownForest/Generations/G20261016082700052` | Preserve original bark/foliage identity, material and source geometry references. Inspected Pine source has 1,568 vertices (824 bark, 744 foliage), two submeshes, single LOD threshold 0.035 and readable mesh. No source mesh/importer/vendor modifications. |
| `Art/S013/Production/Materials/Wood.mat` | Existing URP wood is the default interior candidate, with its existing texture. Visual suitability is NOT approved. Owner can select an approved URP override alongside the node. |

After focused EditMode and PlayMode gates succeed, owner authoring is one explicit Editor action:

1. Open **only** the saved scene `Assets/LastSignal/Scenes/Validation/FirstTownHarvest_20261018150846-032db4e9.unity`, outside Play mode, compilation complete. Save intentional owner work first.
2. Select exactly one eligible, upright, unit-scale, resident S003 ResourceNodeView root. Keep decorative/hero trees untouched. Inspect enough free space for the full fall and safe pickup; do not choose a road/exit-blocking tree.
3. Invoke **Last Signal → BLD → P01 → S004 → Create Single Tree Physics Stage**. This is a real implemented menu item, not an instruction to run it through an agent.
4. Expected Console line: `S004_STAGE_PREPARED scene=... assets=... sourceSHA256=... TEST_NOT_RUN VISUAL_NOT_APPROVED PERFORMANCE_NOT_MEASURED`. Return the line, generated scene path, Console exceptions if any, and source/stage diff.
5. Inspect the generated meshes, materials, collider bounds, preserved foliage/LOD, profile and manifest. Review roads, approaches, terrain/drop space and navigation. Only after clearance review set the copied GatheringWorld's `clearanceApproved` true and save the copied scene. Audio approval is separate. Close Unity before batch tests.

Each invocation uses a timestamp+random suffix and creates a separate scene `Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_<stamp>.unity` and asset directory `Assets/LastSignal/Data/Gathering/Physics-<stamp>/`. Expected assets: `Trunk.asset`, `Stump.asset`, `Foliage.asset`, 32 `Top-<side>-<level>.asset`, 32 `Stump-<side>-<level>.asset`, `Physics.asset`, `Audio-Audition.asset`, `Bark-Static.asset`, `Resource.asset`, `Manifest.asset`, `HarvestableTree.prefab`, and Unity metas. These are **expected outputs, not already generated deliverables**.

Authoring extracts bark and foliage separately, bakes the instance transform into new meshes, slices bark at cut height, and builds eight-by-four V-notch stages with two sloped planes. Bark uses slot 0, interior wood slot 1, foliage a separate untouched material. A cloned bark material disables wind displacement through `_LS_FOREST_STATIC`; original source materials remain unchanged. Original visuals stay inactive in the stage for comparison. The prefab omits the scene-owned world link; the scene instance retains it. New GUIDs are created only for new derivatives; original GUIDs are retained.

Validation checks input topology, readable mesh, finite vertices/normals/UV/bounds, index range, normals/UV coverage, nonempty caps, transform determinant, cut section circularity/centering, radius and profile bounds. This does not prove watertight appearance on every original tree. The original S003 scene SHA256 is compared before/after. An exception may leave an isolated incomplete draft folder/scene for inspection; do not promote or use it. Reopen the untouched S003 scene for a corrected retry. No automatic source overwrite or promotion exists.

## Audio inventory and audition gate

Folder: `Assets/ThirdParty/Audio/Tree`. All five imported references are collected into the generated bank's `auditionCandidates`; none is automatically mapped or production-approved by filename.

| Actual file | Measured container metadata | Audition candidate purpose (unconfirmed) |
|---|---|---|
| `386223__ldezem__wood-chopping-1.wav` | stereo, 96 kHz, 24-bit, 1.479021 s | Short wood contact; compare attack onset and tail |
| `537435__wavecal22__wood-chop-8.wav` | stereo, 44.1 kHz, 24-bit, 1.558163 s | Alternate wood/bark contact, if appropriate |
| `753925__xkeril__axe-chopping-wood.wav` | mono, 48 kHz, 24-bit, 12.922625 s | Inspect for isolated contacts/cracks; do not map a multi-hit recording wholesale |
| `494071__kinoton__big-tree-fall-in-forest.wav` | stereo, 48 kHz, 24-bit, 7.984125 s | Inspect creak/failure/landing sections separately |
| `chop-tree-fall.ogg` | Vorbis stereo, 48 kHz, 2.670292 s | Inspect short fall/contact/settling suitability |

No listening occurred and no nearby rights evidence was found. Filenames/creator labels alone are not clearance. Record source URL, author, exact license and attribution requirements before approval. Preserve originals; if trimming or mono derivatives are needed, make separately named project-owned derivatives and record provenance. Inspected WAV importer used loadType 0, compressionFormat 1, quality 1, preload off, forceMono off, normalize on and spatial3D on; all-clip importer/performance approval remains outstanding.

Existing axe swing audio stays in the existing melee presenter. Bank slots are woodHit, barkFracture, structuralCrack, creak, fracture (joint failure), fallingMotion, groundImpact, branchContact and settling. A missing slot is silent. `mappingApproved` defaults false and gates tree clip playback. Only set it after rights and listening approval. Select appropriate short sections, avoid stacking long full-fall recordings on every contact, check close/mid/far distance and obstruction, stereo localization, mixer attenuation, clipping, tails, branch scale and timing in FPS/TPS. If supplied recordings cannot cover a slot, leave it unmapped and report the gap.

ProductionAudio reuses its existing 16-voice pool, slots 4 onward, max two simultaneous voices per owner, Effects mixer, 1–40 m linear spatial rolloff, existing occlusion and StopOwner lifecycle. No competing AudioSource manager was added. Contact impulses set impact gain; gameplay noise is independent of clip availability and audio volume. TreeChop uses 12 m/.35/2 s tuning; TreeImpact uses 24 m/.65/3 s. New enum values are appended, preserving existing ordinals. One accepted ActiveWindow emits chop noise; one significant live landing emits impact noise. Restored presentation emits neither. Pressure and hearing use existing adapters/population logic.

## Owner verification sequence — run one gate at a time

Save and close the Unity Editor before every terminal gate. The runner checks the project version, installed Unity executable, actual test assembly and fixture names, and uses the existing S023 live-process/project-lock guard. Do not delete lock files to bypass it. Stop at a failed gate, return evidence, and request a focused fix/retest before broadening.

Every command below starts in the exact project and is owner-run only. Each runner invocation executes only the selected gate. It records `command.json`, `head.txt`, `status.txt`, `unity.log`, `tests.xml`, `result.json`, and the stage SHA256 when supplied, under:

`/Users/burakcoskun/Last Signal/Docs/Implementation/BLD/P01/S004/Evidence/OWNER-<Istanbul timestamp>-<gate>-<random>/`

Expected success: runner exit 0, nonempty test count, NUnit result Passed, no test/Unity failures. A source compile or absent XML is not PASS. Return Terminal output plus `unity.log`, `tests.xml`, `result.json`; include the failing assertion/stack and relevant Console messages. Tests may write temporary or unique persistent test-save slots and normal Unity import/cache files, but do not intentionally save scenes or author/promote assets. Authored-stage fixtures log their unique `S004_INTEGRATION_CHECKPOINT` save path and preserve that checkpoint for diagnosis.

### 1. Focused EditMode

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py edit
```

Runs P01TreeStructureTests, P01TreePersistenceTests and existing P01HarvestTests in LastSignal.EditModeTests. No S004 scene is required. Covers accepted/stale/duplicate/tool/side/condition inputs, structural capacity, fracture identity, legacy and physical saves, invalid poses, real melee range/occlusion, noise eligibility, and 30/60/120 active-window receipts. Existing harvest tests cover durable before/after/unknown publication outcomes. This gate does not simulate a production tree fall.

### 2. Focused PlayMode

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py play
```

Runs P01TreePhysicsTests and P01HarvestPickupTests in LastSignal.PlayModeTests. No authored stage required. Isolated PhysX fixtures cover constrained support, actual break with absent authority failing closed, gravity/contact/obstacle/rest, live and restored feedback, missing joint/collider, delayed disabled contact, disable/resume, settled pose, and full/partial inventory. Synthetic meshes are test fixtures, not proposed production tree assets.

### 3. Owner authoring, then actual physics integration

Perform the Editor authoring instructions above only after focused gates are green. In a terminal set the scene path printed by `S004_STAGE_PREPARED` (the value cannot be known until the owner generates it):

```bash
read -r 'S004_STAGE?Paste the generated project-relative FirstTownTreePhysics scene path: '
```

Then, with the clearance-approved copied scene saved and Unity closed:

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py physics --stage "$S004_STAGE"
```

Runs `P01TreeStageTests.ActualAxeAndJointCommitSurviveMidFallSaveReloadExactlyOnce`. Uses the actual stage profile, controller/resolver active window, SaveSession and solver. It requires actual joint failure rather than accepting a scripted release. A failure to fracture/settle is a tuning/collision investigation, not permission to bypass physics. Population materialization is disabled only inside this isolated test; manual zombie acceptance remains necessary. No scene or authoring assets are intentionally saved by the test.

### 4. Persistence

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py persistence
```

Runs new physical persistence plus existing harvest/save foundation/save validation/world cell EditMode suites.

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py persistence-stage --stage "$S004_STAGE"
```

Runs `P01TreeStageTests.PartialCutDisableReloadRetainsRevisionAndDoesNotMintYield`. This exercises resident disable/reload; it is not proof of streamed-cell physical-tree integration. Manually retain before-cut, partial-cut, mid-fall, settled, partial-pickup and consumed checkpoints. Reload each twice, check one yield identity/quantity, no replay noise/audio, preserved tool condition and correct pending/full inventory behavior. On a copied test slot, verify interrupted/failing publication enters recovery and reloads a verified checkpoint. Do not inject faults into owner gameplay saves.

### 5. Targeted regression

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py regression-edit
```

Runs S021Combat, MeleeGrip, CharacterWeaponPose, R02Noise, R03Auditory, S015Audio, ShelterDomain and WorldCell EditMode fixtures.

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py regression-play
```

Runs corresponding combat/grip/pose/noise/hearing/audio PlayMode fixtures and harvest pickup tests. Does not run the entire project automatically.

For a diagnosed failure, use the same gate/platform with the exact NUnit fullname from its XML, for example:

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py play --filter 'LastSignal.Tests.P01TreePhysicsTests.RemovedColliderFailsClosedWithoutReward'
```

Stage retests also need `--stage "$S004_STAGE"`. Return the focused retest before declaring a fix verified.

### 6. Manual visual, audio and gameplay acceptance

Open only the approved copied S004 scene. Play manually; capture real Game View video with audio and screenshots. Use the selected TreePhysicsBody Inspector's read-only evidence display/button to record node, mass/dynamic state, center of mass, angular velocity, joint and contact impulse. Preserve the profile asset and scene hash with each run.

Required captures: standing source/derivative Pine or Birch comparison; FPS and TPS two-handed grip; first wood contact; each notch stage/interior; visible weakening; joint fracture without teleport; free gravity-driven rotation; obstacle collision; ground impact; bounded chips/debris; stump/interior; final pose; partial/full resource pickup; dusk/rain; zombie response to accepted chop and landing. Check missed/wrong-tool/blocked swings, broken axe, weapon switching, inventory full, player death mid-action, save/reload at every phase, pause/resume and repeated callbacks. Review critical roads, exits, navigation and safe drop locations after the fall. Repeat with the other family in a separate owner-generated stage when the first is accepted. Real streamed-cell unloading needs a separately reviewed authored integration case; it is currently an open gate.

At render caps 30, 60 and 120, keep the same fixed timestep/profile and record accepted hits, durability, noise count, fracture identity, contacts and resting result. Compare conservation and cadence, not bitwise identical trajectories. Inspector/Scene View alone is not visual acceptance. Reject toy-like fall, floating notch, invisible/mismatched caps, jitter/explosion, clipping, bad stump/foliage, free rewards or mistimed sound.

### 7. Performance and build

Use Unity Profiler on the accepted copied stage after correctness/visual gates: capture idle, one active fall, contact/settling and multiple falls up to the activation budget. Record machine/OS, resolution, render cap, fixed timestep, tree profile, CPU Physics.Simulate, script time, GC.Alloc, active bodies, contacts, voice count and frame-time percentiles. Compare the unchanged S003 scene with the S004 stage. Record authoring time/asset size separately from runtime; no runtime mesh slicing is expected. Multiple-tree scene authoring requires explicit distinct node IDs/manifest entries; do not duplicate a stable node blindly. No generic frame-time budget is claimed without the project's target hardware baseline.

Existing verified build entry (after earlier gates, Editor closed):

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/s023-verify.py development-build --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

This is the **existing production-baseline macOS development build**, not automatic inclusion/promotion of the new S004 stage. Evidence goes to `Docs/Implementation/S023/Evidence/Manual-<timestamp>-development-build-<random>/`; output to `Builds/S023/<run-name>/LastSignal.app`. Return `unity.log`, `result.json`, `build.txt` and build report/output path. Success requires build Result=Succeeded; runtime remains NOT_RUN until owner launches it. It writes build outputs/cache/evidence, not a production scene promotion. Stage player profiling needs an owner-approved build scene selection; that step is not automated or claimed complete here.

## Evidence available now and open gates

Static checks only: runtime, Editor, EditMode test and PlayMode test C# sources compiled with Roslyn against installed/cached Unity references, outputs confined to `/tmp/lastsignal-s004-static`; all four exit 0, with existing obsolete-API warnings. No Unity Editor compilation was performed. Cached package assemblies are a limitation: clean Unity import/compile is still required. `StaticChecks/` retains compiler logs and the source-only harness. Python runner syntax, fixture/assembly paths and `git diff --check` were checked without running the runner. No prohibited falling-animation calls were found in the adapter. Preservation report compares initially dirty file hashes and identifies the six intentionally extended S003 files.

Open: owner-generated geometry/prefab/stage; real Unity compile/tests; audio rights/audition/mapping; visual material/notch/physics/grip acceptance; physical tuning; real streamed-cell tree case; optional secondary log bodies; multi-tree/hardware performance; stage player build acceptance; source scene promotion. These are PARTIAL/TEST_NOT_RUN gates, not inferred PASS results. No currently known Unity runtime failure is hidden by this report, because no Unity run has occurred.

**Single next owner action:** save and close Unity, then run:

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py edit
```
