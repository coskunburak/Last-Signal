# Studio New Punch zombie visual and Phase-1 dismemberment discovery

Date: 2026-09-30
Status: **PHASE-1 RUNTIME BOUND — automated logic tests passed; visual art and full regression acceptance pending**
Unity: 6000.5.0f1; URP package 17.5.0

## Scope decision

Historical art scope excluded dismemberment from the initial scope. This Phase-1 dismemberment implementation is introduced by the user's later explicit instruction and does not retroactively change the historical design record.

## Working-tree baseline

The local tree was already heavily modified before this discovery. `git status --short` failed because Git LFS attempted to write to `.git/lfs/tmp` under the sandbox. With the LFS process and clean filters disabled only for the status command, the baseline counted 594 deleted, 120 modified, and 93 untracked path entries. The imported zombie area at `Assets/LastSignal/Assets/Zombie/` is untracked, while the former `Assets/LastSignal/Enemies/Zombie/`, `SZombie`, and Kevin Iglesias animation paths appear deleted. No reset, clean, checkout, or move was performed for this task.

## Package identification and selection

The user clarified that there are three packages in scope and explicitly includes `ArtStore3D/Fat Zombie(Low Poly)` in the Studio New Punch set. Its local readme carries ArtStore3D branding and publisher ID 71551; both statements are recorded. Four `.fbx` files across ShirtlessZombieFree and ZombieMale_AAB are two regular models and two body-parts models, not four distinct packages.

| Local package | Model / body-parts prefab | Rig and animation | Body-part structure | Cost evidence | Decision |
| --- | --- | --- | --- | --- | --- |
| `Assets/LastSignal/Assets/Zombie/ZombieMale_AAB/` | `Prefabs/URP/ZombieMale_AAB_BodyParts_URP.prefab` | FBX importer `animationType: 3` and `avatarSetup: 1` (Humanoid import intent); no dedicated movement/combat clips in this package | 25 active skinned renderers; separate head, left/right upper arms, forearms, hands, torso, clothing and legs | 21,145 source triangles; 24 PNG textures (20 at 2K, 4 at 512); high renderer count | Clothed alternative; open torso and clothing boundaries need more art work |
| `Assets/LastSignal/Assets/Zombie/NewPunch/ShirtlessZombieFree/` | `Prefabs/ShirtlessZombie_BodyParts_FREE_URP.prefab` | Same Humanoid import intent; no dedicated movement/combat clips | 15 active skinned renderers; separate head, arms, forearms, hands, ribcage and pelvis | 13,769 source triangles; 11 PNG textures (6 at 2K, 5 at 4K); lower renderer count | Selected production model; wound-material acceptance pending |
| `Assets/LastSignal/Assets/Zombie/ArtStore3D/Fat Zombie(Low Poly)/` | `Prefab/FatZombie.prefab` | Humanoid importer, no imported animation curves | Two skinned renderers for LOD0/LOD1; no independent head, arm or hand renderer | Five 4K textures; Built-in Standard material requires URP conversion | Visual variant candidate only; unsuitable for Phase-1 sever |

Other local folders must not be counted as Studio New Punch merely because they sit under the zombie asset root. The `ZOMBİE ATTACK ANIMATION/Human Animations` PDF identifies Kevin Iglesias. `Zombie/` corresponds to a separate Pxltiger package; `SZombie/` is the historical S004 source. Publisher identity for `Zombie_Survival_AssetPack_LowPoly/` is not proven locally.

No local package version, Asset Store license text, or purchase/import date was proven. Those fields require verification. Vendor source redistribution rights are not inferred.

## Rig, animation, material, and geometry evidence

Both body-parts FBX `.meta` files request Humanoid import. Unity's manual candidate validator confirmed valid Humanoid Avatars, required split renderers and URP materials. Neither selected package contains a dedicated idle, walk, chase, attack, hit, or death clip set. The selected Shirtless visual uses the existing `AC_Zombie_Shambler` controller and S004 clips (`Idle`, `Locomotion`, measured right-arm `Attack`, `HitReact`, `Death`), with no Animator parameters. The presenter continues to own attack/damage timing and forces `applyRootMotion = false`. Focused presentation PlayMode tests passed 2/2 animation and actor-root checks; detailed shoulder, hand, foot-slide and contact-pose visual acceptance remains pending.

Both candidate packages include URP material variants. Structural URP shader validation passed; actual rendered appearance and absence of pink materials remain **NOT_RUN**. The separate body renderers support distinct head, arm and hand visibility in principle, but the prefabs have no explicitly named stump/cap mesh. An initial sandboxed Blender 4.5.3 run crashed; a later read-only elevated run imported both body-parts FBX files and measured topology. Shirtless `ArmL/R`, `ForeArmL/R`, `HandL/R`, and `Ribcage` have zero open boundary edges; its head has 136 open edges away from its lowest 10% in imported coordinates. ZombieMale arms, forearms and hands also have zero open edges, while its torso has 72 open edges and clothing has additional open seams. Closed topology supports the Shirtless selection, but wound appearance, bone pose, head neck cut and material quality still need Unity/visual verification. Source mesh counts are not final rendered triangle counts if hidden renderers or LODs differ.

A read-only Blender Workbench preview of the selected source with head and left arm hidden is saved as `Evidence/20260930-NewPunch/shirtless-cut-workbench.png`. The neck and shoulder appear dark at the cut. Workbench did not use the URP textures or production lighting, so this is an art risk rather than a Unity acceptance result. Project-owned wound caps/materials and posed Unity inspection remain required before final art approval.

## Existing gameplay authority and integration seam

`Assets/Resources/LS_Zombie_Runtime.prefab` is the scene-facing gameplay wrapper. It owns `ZombieController`, `ZombieHealth`, navigation, perception, auditory listener, and eleven explicit child hit regions. Its nested visual is now the project-owned `LS_Zombie_Shirtless_Visual.prefab`, a variant of the selected vendor body-parts URP prefab with the existing controller, root motion disabled, reference transforms, and the vendor eye-glow script removed. The historical `LS_Zombie_Shambler.prefab` remains available and unmodified. The firearm resolver dispatches through the collider's `IDamageable`; `ZombieHitRegion` forwards multiplied damage once to `ZombieHealth`. `ZombieHealth` owns the health mutation and single `Died` event. `ZombieController` owns melee contact timing and the death transition. Any regional sever logic must attach after this region/health path, keep per-instance sever state, and route fatal sever through `ZombieHealth`.

The measured existing `Zombie@Attack01` is a right-arm swing. Right-arm loss disables that attack until a visually valid fallback clip and timing are authored. The current `WorldPopulationManager` deactivates/destroys a streamed actor immediately in its death callback; detached pieces have an independent bounded lifetime, and corpse presentation needs a separate lifecycle decision. Living population and persistent encounter snapshots now carry an optional anatomy extension for mask, torso damage and regional accumulation; legacy saves without it restore intact zombies.

## Phase-1 gate and exact art task

The selected `ShirtlessZombie_BodyParts_FREE_URP` visual has been switched into the production runtime wrapper. The wrapper has strongly typed anatomical attribution for head, torso, arms, hands and legs. This preserves the original Head/Body damage multipliers and single `ZombieHealth` mutation. `ZombieDismemberment` and a bounded, scene-local `ZombieDetachedPartPool` are implemented; head, arms and hands are now bound to the production prefab with independent hand hit colliders. Fatal head sever still commits through `ZombieHealth`. A sever during a right-arm attack aborts its pending contact. A project-owned transparent torso wound texture, URP material and quad are attached to the Humanoid chest bone; the visual appears only after the torso threshold and is hidden immediately by reduced gore.

The first automatic approval review rejected `StudioNewPunchVisualAuthoring.BindPhaseOneBodyParts` because of production behavior changes before visual and broad gameplay validation. The user then gave explicit approval to continue production integration. The same direct authoring method was retried with that authorization and succeeded; no indirect workaround was used. The production prefab now contains `ZombieDismemberment`, `Damage_LeftHand` and `Damage_RightHand`.

In Blender or Unity, inspect `ShirtlessZombie_BodyParts_FREE.fbx` at the neck and each shoulder, elbow and wrist cut. For every chosen sever point, confirm a suitable wound-colored cap on both surviving stump and detached piece in multiple animation poses; closed topology alone does not prove visual quality. If absent, author project-owned stump/cap and detached meshes with pivots aligned to the matching `head`, `upperarm_*`, `lowerarm_*`, or `hand_*` bone; retain source scale and orientation and assign a dedicated URP wound material slot. Export as project-owned assets while preserving the vendor FBX and `.meta`. Only the regions whose cuts pass this inspection should be bound for Phase-1. Torso remains a hit/wound state; no full torso split is required. Legs/crawling remain deferred.

## Verification state

Cheap checks performed: local file/prefab/meta inspection, Unity version/package inspection, source-path tracing, Blender topology audits of both candidate FBX files, manual Unity candidate validator (both structural candidates and runtime authority passed), focused EditMode damage/asset/composition tests 24/24, focused EditMode save/population/damage tests 80/80 after an old-save normalization fix, production-prefab sever plus presentation PlayMode tests 5/5, and final torso sever/gore tests 4/4. The first save test run exposed `JsonUtility` materializing absent anatomy into an empty object (49/80); the codec normalization was corrected and the exact focused run passed 80/80. No full EditMode/PlayMode regression, development build, or 10/20/30-zombie stress run was performed. `NOT_RUN` is not `PASS` under `22_QA_Acceptance_Traceability.md`.

The selected vendor PNG/FBX/material sources were not changed. Four selected vendor texture `.meta` import settings (body/clothes MetallicSmoothness and AO) were changed from sRGB to linear because packed data had been imported as color. This is an explicit local import-setting exception; no vendor geometry or texture pixels were edited. The new project-owned visual variant and the existing runtime wrapper carry the actual integration.

`T_Zombie_TorsoWound.png` is a newly generated project-owned transparent image, not part of the vendor package. Unity imports it at a 512-pixel maximum through its `.meta`; `M_Zombie_TorsoWound_URP.mat` and `SM_Zombie_TorsoWound_Quad.asset` are likewise project-owned. Their final appearance in actual Unity lighting still needs visual acceptance.

## Remaining production gates

- Inspect the selected head/shoulder/wrist cut surfaces under idle, attack, hit and death poses in Unity, including day/night/flashlight. Blender's manifold edge count does not verify wound coloration or skinning deformation.
- Review the data-driven thresholds in `StudioNewPunchVisualAuthoring.BindPhaseOneBodyParts` (head 75, arms 50, hands 35 final hit damage units) against actual weapon balance. The production binding currently uses these values.
- The optional anatomy extension now stores sever mask, torso damage and per-region accumulated damage in both population and encounter snapshots; older saves with no anatomy normalize to an intact zombie. Run full save/load and streamed-cell PlayMode regression to confirm the integration boundary.
- Visually accept and if necessary reposition/refine the new project-owned torso wound plane under attack/death poses and day/night/flashlight. Expose `ZombieGorePreference` through a player-facing reduced-gore control. The current component has a presentation toggle API, including immediate hiding of active detached parts, but no UI.
- Run focused PlayMode sever/pool checks for renderer visibility, hit-collider removal, detached pose/physics, pool expiration and reuse, plus the manual 10/20/30-agent visual and performance acceptance.

User approval resolved the earlier automatic review block. The direct production prefab binding succeeded. Remaining manual visual and full-regression gates must be completed before declaring production acceptance.

Requirement anchors: `10_Combat_Weapons_Damage.md` (damage order and hit dedupe), `11_Zombie_AI_Perception_Population.md` (death/navigation/population), `19_Art_Audio_Asset_Pipeline.md` (visual performance, root-motion and historical scope), `21_Vertical_Slice_ve_Playtest.md`, `22_QA_Acceptance_Traceability.md`, and `18_UI_UX_Accessibility.md` (gore parity).
