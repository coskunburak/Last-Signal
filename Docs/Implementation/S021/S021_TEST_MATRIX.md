# S021 verification matrix — user-owned gates

| Area | New authored coverage | Existing regression evidence target | Remaining human check |
|---|---|---|---|
| Definition/catalog | `S021CombatTests.AuthoredDefinitionsHaveDistinctStableIdsAmmoAndRoles`; `S021CombatPlayTests.WorldRigProfilesAndAudioBindingsMatchSelectedWeapons` | `S020CatalogTests`, S019 content suites | Imported mesh/material quality |
| Ammo/condition | Independent magazines, exact reload, cancel, partial reserve, zero condition | `WeaponStateTests`, `AmmunitionTransactionTests`, `InventoryDomainTests` | Rifle/pistol scarcity and condition UX |
| Melee/zone | Attack timeline, region protection, receipt deduplication, actual two-region zombie resolver | `ZombieDamagePlayTests`, `ZombieMeleeTests`, `S014WeaponPresentationTests` | Per-zone visual agreement at 30/120 FPS |
| Switch/input | Actual player cycles 2 firearms/3 melee; pistol fire; precommit switch; modal pause | `CombatAcceptanceTests`, `CharacterIntegrationPlayTests`, `ScopeOpticPlayTests` | Rapid switching, ADS, held fire, vehicle/death teardown |
| Save/load | V2 codec roundtrip, v1 fixture, invalid duplicate/missing/corrupt firearms; production S013Cabin double load | `SaveFoundationTests`, `SaveValidationTests`, `S020CatalogPlayTests` | Existing user save fixture on production data |
| Presentation/audio | Actual prefab and pose profile references, presenter components | S014/S015/Character integration suites | Grip, reload, recoil, clipping, sound quality |
| Full project | S021 runner EditMode/PlayMode regression gates and development build | All discovered tests in current assemblies | Manual gameplay and balance |

`Tools/s021-verify.py` fails on zero tests, skipped/inconclusive/failed cases, compiler diagnostics, nonzero Unity exit, changed source or missing BuildReport output. Existing regression suites are selected by the actual `LastSignal.EditModeTests`, `LastSignal.Art.EditorTests` and `LastSignal.PlayModeTests` assembly names. No test/build/benchmark was executed for this handoff.

User evidence through 2026-10-11: focused compile/EditMode/PlayMode and full EditMode passed before the latest PlayMode corrections. Full PlayMode failed 339/400 with 61 failures; see `Evidence/20261011T130645Z-regression-play-6f9b0a8e`. The new corrections remain unverified. Development build and manual gameplay have not run.
