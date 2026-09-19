# Current implementation → correction map

| Current object | Purpose / real asset? | Decision |
|---|---|---|
| Weapon_AssaultRifle.prefab | Already referenced MR POLY FBX; incorrect inherited scale/rotation, static magazine, guessed sockets | Modify in place, preserve GUID; one real body and magazine under separate VAL bones |
| FPSArms.prefab | Real VAL, inactive reference rifle, also imported environment/camera | Retain legacy asset, remove production dependency |
| FPSArms.controller | Actual VAL clip references, generic trigger state graph | Retain legacy asset; production uses VAL_MRPoly.controller |
| VAL_MRPoly.controller | Production source-clip graph | Keep; gameplay events drive presentation |
| Avatar Masks | No masks in previous combat implementation | None needed for dedicated Generic rig |
| Player.prefab | Working S001 capsule/input/look/movement/interactions; unpopulated weapon parent | Add starting weapon reference and separate tt-3d world body |
| WeaponDefinition_AssaultRifle.asset | Existing authoritative ammunition, damage, recoil and timing settings | Keep |
| WeaponRuntimeState, WeaponFireResolver, damage types | Combat domain | Keep unchanged |
| WeaponController | Event bridge omitted successful reload-start notification | Fix event publication only |
| PlayerCombatController | No default weapon spawning/parenting, ADS FOV not wired | Connect production weapon lifecycle and presentation |
| WeaponViewPresenter | Unverified hardcoded ADS pose | Align measured real MR POLY optic; retain hip sway/bob; reduce extra recoil |
| WeaponAnimationPresenter | Real-clip triggers | Keep event bridge; source mag bone supplies magazine animation |
| WeaponRecoilController | Camera recoil kick/recovery signs inconsistent | Fix signs and wire existing presenter |
| WeaponVfxPresenter | Existing muzzle flash connection unassigned | Wire effect at measured barrel and actual mesh ejection reference |
| CombatAcceptance.unity | Deterministic target fixtures | Keep |
| S001Acceptance.unity | Normal playable route and movement fixtures | Keep scene; shared player now equips production weapon |
| CombatAcceptanceProject editor utility | Created unchecked adapter and test scene | Entry point delegates to measured real-asset adapter; no scene overwrite |
| AnimationExtractor / PathDebugger | Inspection utilities | Keep |
| VAL FBX importer | Source takes already exposed despite empty custom clip list | Preserve take ranges; enable loops on idle, idleAim, walk, sprint |
| MR POLY vendor prefabs | Broken GUID references | Leave untouched; reference source model meshes |
| MR POLY FBX, VAL FBX, tt-3d FBX | Original geometry and animation source | Keep binary source unchanged |

The former prefab was incorrectly assembled, not a generated replacement mesh. That distinction was verified in Unity before correction. Full source hierarchy is saved in the evidence directory.
