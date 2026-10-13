# S021 implementation report

Status is **IMPLEMENTED_NOT_VERIFIED** for source-complete cards, **PARTIAL** where art or user evidence is essential. No card has a verified PASS.

| Card | Status | Actual work / changed files | Acceptance and authored tests | Unresolved / dependency |
|---|---|---|---|---|
| D201 | IMPLEMENTED_NOT_VERIFIED | Role choice and pistol-over-shotgun decision in `S021_WEAPON_ROLE_MATRIX.md`; existing combat classes audited | Five unique IDs and role data in EditMode test | Balance measurements pending |
| D202 | PARTIAL | Machete definition, FPS/world prefabs, hand pose, audio; `MeleeWeaponDefinition`, `MeleeAttackState`, player prefab | Timing, stamina, controller selection tests | Human animation/grip/material review |
| D203 | PARTIAL | Heavy axe definition and prefabs with long windup/recovery, 43 stamina and exhausted rejection | Definition and selection tests | Heavy feel/stagger and release decision after playtest |
| D204 | PARTIAL | Tactical pistol, distinct 9 mm item/pickup/catalog/loot, input switching, audio | Live prefab firing and ammo-conservation tests | Pistol reload animation/art review |
| D205 | PARTIAL | Region protection field and accepted-receipt melee response; preserves existing wounds/gore | Protection order, duplicate/rejected hit tests | Existing zombie regions remain at zero protection; authored armor assignment and visual zone QA await asset approval |
| D206 | PARTIAL | Per-firearm 0–1000 condition, accepted-shot decay, bounded damage, broken feedback, persistence | Clamp/zero-fire/save tests | Repair deferred until item-instance equipment transaction exists |
| D207 | PARTIAL | S021 FPS/Diesel wrappers, three world poses, weapon-specific fallback cues | Prefab/pose/audio structural checks | Inherited pistol rifle reload and grip need dedicated animation QA |
| D208 | PARTIAL | Versioned asset tuning and 10-scenario balance sheet | Deterministic transaction/timing tests | `PLAYTEST_PENDING`: no kills/time/noise/pressure metrics measured |
| D209 | IMPLEMENTED_NOT_VERIFIED | Retained inactive weapon instances, cancellation/FOV reset, reload transaction guard, vehicle holster | Switch, cancel, modal and double-load tests | Held-fire/death/inventory interaction manual QA |
| D210 | PARTIAL | Catalog, docs, tests, runner and first-gate handoff authored | User-owned compile → focused → regression → build sequence | Verification, art/audio acceptance and balance pending |

The S021 source builds on the existing rifle, crowbar, inventory, S015 audio and zombie damage stack. Imported vendor source remains untouched. The repository was already dirty before S021; unrelated pending changes and user-imported assets were preserved. See `S021_CHANGED_FILES.md` for the specific S021 touch points.
