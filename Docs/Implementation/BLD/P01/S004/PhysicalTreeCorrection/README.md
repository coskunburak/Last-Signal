> **Güncel durum:** [CURRENT-MCP.md](CURRENT-MCP.md). Aşağıdaki rapor önceki hazırlık aşamasının tarihsel kaydıdır; TEST_NOT_RUN ve eksik sahne/hücre ifadeleri güncel MCP kanıtlarıyla değişmiştir.

# S004 physical-tree correction — 2026-10-19

**IMPLEMENTED / TEST_NOT_RUN; overall PARTIAL; VISUAL_NOT_APPROVED; PERFORMANCE_NOT_MEASURED.** This correction continues the existing ownership and package choices. No Unity process, test, runtime simulation, scene authoring, build, capture, commit or push was run by the agent. Historical owner results (54 focused EditMode and 16 focused PlayMode passes) precede these changes and do not verify them.

## A. Confirmed disappearance cause

The saved source scene is `/Users/burakcoskun/Last Signal/Assets/LastSignal/Scenes/Validation/FirstTownHarvest_20261018150846-032db4e9.unity`. Its authored ResourceNodeView entries have null `physicsBody` references. No saved `FirstTownTreePhysics_*.unity` scene exists at handoff. Thus this was the old live S003 path, not a demonstrated failure of the new physical joint:

`MeleeAttackResolver.Resolve → GatheringWorld.TryHit (no physicsBody) → GatheringState.Prepare → health 100−25 per hit → health zero / Depleted / yield → ResourceNodeView.Present → standingVisual.SetActive(false)`.

That produces the observed four-hit disappearance and small WorldItem. It does not activate a Rigidbody, cut mesh or joint. Enabling `clearanceApproved` merely allowed the old gameplay path; it never installed the physical adapter. No evidence was found that destruction of the physical trunk, joint callback timing or cell unloading caused this observed source-scene symptom. Unsaved live Editor state cannot be established solely from the scene file, but the stored wiring and exact legacy lifecycle reproduce its mechanism.

## B. Corrected behavior and compatibility

Live tree chopping now requires a valid physical body with meshes, renderers, profile and colliders. Missing/incomplete physical authoring returns `PhysicsNotReady` before a node transaction. It cannot charge axe durability, mint a pickup or enter legacy health depletion. Existing attack/stamina ownership remains unchanged: swinging still has its usual combat stamina cost. Legacy S003 save validation, pure transaction fixtures and existing pickups remain supported; old health-to-hide behavior is retained only for reconstructing legacy depleted saves, not reachable by new live axe hits.

A new physical tree receives cut snapshot version 2. Each accepted hit advances the same localized notch, removes visible bark/wood via a deeper prepared mesh pair, reduces support, emits bounded feedback and applies the existing contact impulse. No reward is created by a cut. At the deepest cut, additional strikes return `AwaitingFall` without durability/noise/reward; there is no invisible repeated damage after geometry stops advancing. The solver must still break the joint. If the joint will not fail under the authored profile, the integration test fails for investigation rather than forcing a scripted release.

The single durable fracture receipt authorizes one Pending yield; the same visible trunk continues into dynamic physics. Physical contact and stable resting unlock existing safe-drop/pickup projection. Full or partial inventory uses the existing WorldItem transaction. **Collecting the entire yield now leaves the accepted fallen trunk, crown and stump visible/collidable.** Resource exhaustion no longer hides even the settled body. Its save state prevents rechopping/reward duplication. This is a deliberate visual-lifecycle change; road clearance must account for persistent fallen wood.

## C. Physics architecture

The existing one-Rigidbody, two-capsule trunk and static stump design remains. There is no Animator fall, tween, angle curve, Transform.Rotate/Translate, MoveRotation, MovePosition or falling-pose interpolation. Transform assignments are limited to owner authoring, reset and saved-pose restoration.

A FixedJoint constrains all axes to the world. Its anchors now follow the centroid of the **remaining** circular support section, opposite the notch. Both anchors are set to the same current world point, so changing support does not teleport the trunk. For x = 1−2d and remaining area fraction A, centroid offset magnitude is `radius * (2/3) * (1−x²)^1.5 / (PI*A)`. This moves the gravity lever arm as wood is removed, including for a nearly symmetric tree whose original COM sat directly above the trunk center. It is physical support asymmetry, not a forced fall direction.

Break force remains `axialStrength * PI*r²*A`; break torque remains `bendingStrength * PI*r³/4 * A^1.5`. Defaults are effective 4 MPa axial / 8 MPa bending, to be calibrated by the owner. Mass is derived from tree radius/height and the existing taper/crown approximation; custom COM and compound-shape inertia remain active. Existing settings: gravity, ContinuousSpeculative collision, interpolation, linear/angular damping .05/.1, solver 12/6, max angular velocity 8 rad/s, bounded 8 N·s axe impulse.

OnJointBreak requests the existing expected-revision durable transaction. It does not award inventory. The body freezes only around synchronous publication, then resumes solver velocity. A fracture splinter burst was added to the existing particle pool. Missing references, failed publication and runaway states still fail closed through recovery.

Terrain/obstacle impulse drives one primary landing event plus at most two spaced secondary contacts. Quiet contacted motion below .08 m/s and .06 rad/s for two seconds permits rest. The 90-second exceptional timeout is recovery, not fabricated settling. No dynamic secondary log segmentation was added; the bounded main physical trunk and existing pickup representation remain the scoped result.

## D. Geometry and configurable effort

New owner authoring creates **16 depth levels × 8 radial directions**, each with a capped upper trunk and capped lower stump (128 mesh pairs). All slicing is Editor-only using installed OpenFracture MeshSlicer/FragmentData. No package/vendor/source mesh was edited. Bark submesh 0 is extracted and processed separately; foliage is extracted without slicing and retains its original material. Outer bark and inner wood occupy separate submesh slots. Existing production `Wood.mat` remains the interior candidate, not an approved visual result.

The two sloped clipping planes form a genuine V-shaped cavity. Prepared geometry depths are 1/16 through 15/16 and .975 at the final level. Support depth uses that exact same quantization. The first accepted contact chooses one of eight radial sides and anchors the saved local impact/direction; later contacts cannot move/reset the notch. The current pilot intentionally accepts a narrow authored cut region at height .9 m ± .2 m, not arbitrary cuts anywhere up the trunk. Every accepted v2 strike advances at least one depth level; there is no fixed production requirement for four hits.

`ResourceNodeDefinition.cuttingResistance` and `axeEfficiency` configure penetration independently of resource yield. Nominal step is `strikeDamage / health * axeEfficiency / cuttingResistance`. Generation scales the selected definition's health/effort with actual cut radius relative to .25 m (bounded .5–3 scaling), retaining its configured damage and resistance/efficiency. Each accepted strike advances `ceil(step * (.5 + .5*condition/100) * 16)` levels, clamped to the available range. Radius also affects mass and actual support capacity. These are tuneable engineering/gameplay approximations, not measured wood constants.

V1 physical saves and original four-stage profiles remain readable. New v2 snapshots require a 16-stage profile; incompatible wiring is rejected instead of silently selecting the wrong notch. Changing a definition after starting a saved stage requires a compatible/fresh stage checkpoint.

Input/output checks cover finite vertices/normals/UV/bounds, triangle topology/index ranges, transform direction/scale, readable geometry, nonempty interior-cap submeshes, circular/centered cut section and valid physical references. A new owner EditMode fixture actually uses the installed slicer on a closed cylinder and checks progressively removed volume and caps; this is not a claim that the original Pine/Birch has passed visual watertightness review. Unsupported source topology fails with the draft isolated; do not accept holes, inverted caps or material stretching.

The original standing visual stays enabled until the replacement physical body is fully wired and validated. The staged physical tree is clearly named `S004 PHYSICS TEST TREE — …`. The tool finds a grounded, clear player approach approximately three metres from that tree and writes a stage-only `S004 Test Spawn — faces physical tree` marker into the copied SessionFlow's spawn reference. If no suitable approach exists, generation stops instead of placing the player inside geometry. This capsule/slope check does not replace owner road/navigation/encounter review.

## E. Audio and noise

The existing ProductionAudio pool/Effects mixer/spatialization and voice limits remain. Existing melee whoosh remains; physical wood contact suppresses flesh fallback. Wood hit, bark fracture, structural crack, creak, joint failure, fall motion, actual landing, branch contact and settling slots already dispatch from accepted events. Joint failure now also emits a bounded splinter burst. Gameplay TreeChop/TreeImpact events continue through existing pressure/hearing ownership, independently of audio volume. Restoring does not replay sounds or pressure events.

**No clip was newly mapped or auditioned.** Existing candidate references are preserved in the generated Audio-Audition bank:

| Candidate in `Assets/ThirdParty/Audio/Tree` | Owner audition purpose, not an approved mapping |
|---|---|
| `386223__ldezem__wood-chopping-1.wav` | Wood contact onset/tail |
| `537435__wavecal22__wood-chop-8.wav` | Alternate wood/bark contact |
| `753925__xkeril__axe-chopping-wood.wav` | Isolate appropriate contact/crack sections |
| `494071__kinoton__big-tree-fall-in-forest.wav` | Inspect creak/failure/landing sections separately |
| `chop-tree-fall.ogg` | Inspect fall/impact/settling suitability |

Rights evidence and actual listening remain missing. `mappingApproved=false` until owner clearance/audition; unmapped sounds are silent. Do not map a whole multi-hit/fall recording onto every strike. Exact metadata and mixer limits remain in the parent HANDOFF.md. Audio completion/approval is PARTIAL.

## F. Persistence and ownership

The existing Gathering snapshot and SaveSession remain the only owners. ResourceNodeState.treeVersion remains the optional-section marker; inner TreeCutSnapshot.version 2 adds `cutUnits` and `cutStep`. V1 snapshots retain their original validation/depth formula. V2 validates its penetration, quantization, revision bounds and consistent depth. First cut position/direction remains stable, including after reload. Both versions are covered by real SaveCodec envelope fixture cases.

Partial-cut saves restore the corresponding mesh pair/support and resume dynamic loading through the constrained joint, silently. This also fixes a fully cut pre-fracture checkpoint getting stuck kinematic while no further cut can be accepted. Fractured saves retain generation/revision and stable `nodeId:yield:1` identity. Mid-fall restoration reconstructs saved pose and velocities without another fracture transaction or noise replay. Settled/consumed trees retain the accepted physical pose and visible stump/trunk. Repeated hits, callback repetition, full inventory and pending safe-drop failure do not create a competing resource owner.

Existing cell leave/ready, disable/resume and recovery hooks remain. Resident staged integration and isolated disable/restore tests are supplied. **An actual streamed-cell physical-tree stage is still not authored or accepted**; existing WorldCell regressions and resident disable tests are not a substitute. No new forest-wide streaming architecture was introduced.

## G. Files and preservation

See `FILES.json` for absolute paths, exact purposes and hashes before/after this correction. `ENTRY.json` captures branch, HEAD, starting Git status and relevant file hashes. The S003 source scene, existing prefabs/data, source tree assets, imported examples and unrelated local work were not edited by this correction. No C# file was recreated and no existing GUID was regenerated.

Changed runtime sources: GatheringState, GatheringWorld, ResourceNodeDefinition, ResourceNodeView, TreeStructure, TreePhysicsProfile and TreePhysicsBody. Changed Editor source: TreePhysicsAuthoring. Changed fixtures: P01TreeStructureTests, P01TreePersistenceTests, P01TreePhysicsTests and P01TreeStageTests. Changed runner: Tools/p01-tree-verify.py (`--chops` plus new physical-stage filters). Existing HANDOFF.md now points to this correction; the new directory contains this report, scope evidence and source-only compiler logs.

## H. Owner-only verification and exact operations

### First: compilation and actual scene authoring

Stop Play mode and let Unity compile. Invoke **Last Signal → BLD → P01 → S004 → Create Single Tree Physics Stage**. From an empty/untitled scene the menu locates the existing S003 source, presents Unity's save/cancel prompt for any unsaved work, opens it, and offers eligible resident trees. The menu opens a persistent **S004 Ağaç Seçimi** utility window even when a tree is already selected. Click **Seç ve oluştur** on the intended tree. Do not use a production or old generated scene as the source.

Follow-up for the owner's invisible selection menu: replaced `GenericMenu.ShowAsContext()` with the utility window; the native context-menu failure itself has not been reproduced by the agent. `S004_TREE_PICKER_OPENED` records the source and eligible count. Generation is deferred out of the GUI event and revalidates the source/selection before any asset write. Closing/cancelling the window does not generate a stage. No generated FirstTownTreePhysics scene existed on disk during this inspection. Owner must verify window visibility and generation in Unity; the source-only compiler cannot establish either.

Subsequent owner evidence: the picker opened with three eligible trees, then generation stopped at `Physical projection is incomplete`. The draft `FirstTownTreePhysics_20261019102748-99bfd179.unity` and its matching Physics asset folder now exist and were left untouched. Authoring checked live `Collider.attachedRigidbody` bindings while the newly assembled trunk was inactive. The corrected handoff activates the kinematic body and synchronizes transforms before checking the existing strict `Valid` predicate. It retains the original visual until success; failure deactivates the replacement and reports individual failed predicates. The old generic exception does not prove which predicate failed in that owner run; the lifecycle correction and detailed diagnostic still require an owner retry. Added owner-run EditMode cases cover initially inactive assembly and missing mesh rejection/source retention. Editor and EditMode test sources compile; no Unity tests or authoring were executed by the agent. Reopen the original FirstTownHarvest source and invoke Create again, rather than using the failed generated draft as input. Success must log `S004_STAGE_PREPARED` before stage validation/gameplay review.

Follow-up for the owner's `S004_SOURCE_SCENE_UNSAVED` log: an already open, modified S003 source now offers **Kaydet ve devam et / İptal** instead of throwing. The affirmative choice saves only that source scene, then continues; cancellation preserves unsaved edits and stops. A failed save also stops before generation. Source-preservation hashing begins after the explicitly approved save. The WMV import error, normal S006/S001 startup messages and Unity AI account timeout do not cause this source-scene guard.

Generated outputs go only into `Assets/LastSignal/Data/Gathering/Physics-<unique stamp>/` and `Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_<unique stamp>.unity`: intact meshes, foliage, 128 top/stump notch pairs, profile, definition, manifest, static-bark derivative, audio bank, HarvestableTree.prefab and AUTHORING.txt. This report describes **expected outputs**, not already-generated assets. AUTHORING.txt records the source path/hash, node, world position, test spawn, profile, cut levels, penetration, material path and unverified gates. The Console prints `S004_STAGE_PREPARED` with the exact path.

Inspect `S004 PHYSICS TEST TREE — …`: physicsBody must reference its physical child; child profile, top/stump MeshFilters, colliders and audio bank must be assigned; Rigidbody must exist; original visual must be inactive only in the copied stage. Confirm the spawned player faces the mature trunk. The source scene and original materials/geometry must remain untouched.

Review the stage's full fall envelope, roads/exits, drop space and navigation. In **Session → Gathering World (Script)**, enable **Clearance Approved** only after that review, then Cmd+S. Invoke **Last Signal → BLD → P01 → S004 → Validate Current Physics Stage**. It checks the saved scene, unique physical tree, renderers, mesh/profile/manifest/definition wiring and logs `S004_STAGE_VALIDATED` or `S004_STAGE_REVIEW_REQUIRED`; it selects Session for inspection. This is a setup check, not a physics PASS. Return that line and AUTHORING.txt if preparation fails.

For rollback, return to the original S003 source scene (the tool compares its SHA256). Keep any failed/new output folder and scene isolated for diagnosis. No automatic deletion is necessary. A generation exception can leave a draft; do not promote it. Retrying from S003 creates a new unique folder/scene/save slot and preserves previous drafts. No production promotion occurs.

### Then: tests, in order, Editor closed

Enter the actual generated path when prompted. This is **not** the FirstTownHarvest source path. Each gate writes an independent evidence directory and `&&` stops at the first failure. `--chops 4` is the requested regression scenario, not a production release threshold; integration continues physical cutting if the configured tree needs more effort. Each gate records fixture settings, command, log, results, HEAD/status and stage hash where supplied.

```bash
cd '/Users/burakcoskun/Last Signal' && \
read -r 'S004_STAGE?Generated Assets/…/FirstTownTreePhysics_….unity path: ' && \
python3 Tools/p01-tree-verify.py edit && \
python3 Tools/p01-tree-verify.py play && \
python3 Tools/p01-tree-verify.py physics --stage "$S004_STAGE" --chops 4 && \
python3 Tools/p01-tree-verify.py persistence && \
python3 Tools/p01-tree-verify.py persistence-stage --stage "$S004_STAGE" && \
python3 Tools/p01-tree-verify.py regression-edit && \
python3 Tools/p01-tree-verify.py regression-play
```

Expected: exit 0, all selected tests Passed, zero failed/skipped and no unexpected Unity errors. Return `unity.log`, `tests.xml`, `result.json`, `fixture-settings.json` and Terminal output from `Docs/Implementation/BLD/P01/S004/Evidence/OWNER-<timestamp>-<gate>-<random>/`. Stage fixtures use unique test save slots and print `S004_INTEGRATION_CHECKPOINT`; do not inject failures into owner gameplay saves. The runner does not intentionally save scenes or author/promote assets; Unity still writes normal cache/import data.

The critical physical fixture checks actual melee dispatch, successive mesh assignment, visible trunk/stump after each accepted strike, actual joint failure, dynamic body, collision/settling, one safely projected WorldItem identity, rejected repeated projection and reload visibility. A second stage fixture removes the physical binding and verifies configured repeated axe attempts cannot use the old deletion path, debit durability or mint yield. Isolated PlayMode covers consumed-body visibility and exact 16-stage mesh selection as well as existing joint/gravity/rock/rest/recovery tests. EditMode adds configurable effort, condition/resistance, stable notch position, invalid quantization, support centroid and actual OpenFracture volume/cap cases.

Focused disappearance retest after a diagnosed fix:

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py physics --stage "$S004_STAGE" --chops 4 --filter 'LastSignal.Tests.P01TreeStageTests.ConfiguredChopsNeverDeleteTreeAndActualFractureRetainsOneYield'
```

Do not skip a failing geometry/physics assertion or replace a joint failure with a scripted fall to pass. The agent will inspect only the failing evidence and return the necessary focused retest.

## I. Visual/art and performance acceptance

Record real owner Game View video with audio from the generated nearby spawn. Show the selected full-sized tree in FPS and TPS, first notch, second deeper cavity, third structural weakening, final separation without disappearance/teleport, unforced rotation, terrain/rock contact, impact, natural final pose, stump cap, and safe pickup. Inspect all sides of the notch for missing faces, bark/wood boundary and UV stretching. Verify the canopy/material identity, axe grip/timing, daylight and dusk/rain. Repeat Pine and Birch in separate generated stages. No source-only check establishes any of these.

Use the physical child's read-only Inspector evidence button for mass, COM, angular velocity, joint presence, cut depth, support and contact impulse. Compare 30/60/120 render caps at the same fixed timestep: accepted cut count, durability, one yield identity and noise deduplication must agree; trajectories need not be bitwise identical. Test partial-cut, mid-fall, resting and consumed save/reload twice, death/pause/disable during the action, blocked drop and full/partial inventory.

After correctness/visual approval, profile unchanged S003 versus one active S004 tree, then an explicitly authored multi-tree stage with distinct node identities (up to the activation budget). Record hardware, frame-time percentiles, physics/script time, GC allocations, bodies/contacts, voices, and authoring time/asset size separately. No runtime slicing, per-leaf bodies, material creation per strike or global per-hit tree scan is added. Persistent settled trunks are kinematic but remain collision obstacles. Performance is not measured. The existing baseline development-build command and its limitation (it does not include/promote this stage automatically) remain documented in the parent handoff; no build is required to diagnose the current defect.

## J. Status and next action

Source implementation and source-only four-assembly compilation: complete for this correction. Owner Unity execution: **TEST_NOT_RUN**. Actual generated tree, strength/COM/solver tuning, mesh/cap/art acceptance, audio rights/audition/mapping, streamed-tree cell integration, stage player build and hardware performance remain open. Secondary physical log segmentation is not implemented. Overall sprint: **PARTIAL**, not production-complete.

**First owner action:** stop Play mode, wait for Unity compilation, then invoke **Last Signal → BLD → P01 → S004 → Create Single Tree Physics Stage** and choose the intended resident tree.

Owner retry follow-up: Create invoked from a generated FirstTownTreePhysics scene now routes to the existing S003 source-selection recovery instead of throwing WRONG_SOURCE_SCENE. The native save/discard/cancel dialog protects modified open scenes, cancellation stops recovery, and multiple sources still require explicit selection. Production scenes remain rejected. Generated drafts are never reused as source or deleted by this recovery. Editor source-only compilation and whitespace checks passed; Unity scene opening/authoring were not executed by the agent.
