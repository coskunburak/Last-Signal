# BLD-S003 değişiklik ve kart makbuzu

2026-10-18 · Branch `s012-integrated-graybox-slice` · entry HEAD `84aa7358be7b3f47517b7e083ff0734e762e210f`.

G0 accepted; son kullanıcı P01 isteği code/editor/tests yazımını açıkça yetkilendirdi. Ekli paketin genel repository-write yasağı, bu explicit authorization'a karşı uygulanmadı. Test/native execution yasağı korundu. Remote main fetch/compare yapılmadı; attached araştırmadaki remote hash güncel yerel kanıt değildir. Git branch/commit/push/reset/stash işlemi yok.

## Kart durumu

`IMPLEMENTED` kaynak hazır anlamında; bütün owner tests `NOT_RUN`. Data/scene henüz native author edilmediğinden D017/D019 PARTIAL. Sprint checkpoint `IMPLEMENTED_AWAITING_OWNER_TEST`; sprint DONE/PASS değildir.

| Kart | Implementation | Somut karşılık | Scene/asset etkisi ve acceptance |
|---|---|---|---|
| D017 | PARTIAL — data authoring code ready | log taxonomy, existing Axe ownership, wrapper/catalog factory | Native Log.asset/catalog/prefab owner-run; H02/H09 |
| D018 | IMPLEMENTED | ResourceNodeDefinition/State, bounded finite budget, no respawn | New C# only; H01/H06/H08 |
| D019 | PARTIAL — native pilot pending | stable manifest, 2 Pine+1 Birch source-preserving stage tool | Source scenes unchanged; native mesh/collision/nav/IDs owner receipt required; H10/H14/H16 |
| D020 | IMPLEMENTED | melee shared query + owned/session/cell/pose/reach/cone/occlusion guards | Source code; native negative geometry tests pending; H02/H03 |
| D021 | IMPLEMENTED | actual owned selected axe, existing stamina, narrow saved axe wear | No axe prefab/grip edit; H01/H02/H06 |
| D022 | IMPLEMENTED | one ActiveWindow dispatch, high-water receipt, enemy/tree exclusive priority | No extra animation damage event; H04/H05/H13 |
| D023 | IMPLEMENTED — prefab creation pending owner | genuine wrapper factory, Harvest origin, ground/pending/protected volumes, real WorldItem transfer | Native output prefab/clearance unexecuted; H08/H09/H14 |
| D024 | IMPLEMENTED — native fixture pending owner | additive save, full world candidate, publish reconcile, cell registry, old/no-section hydration | Native old/new/cell roundtrip unexecuted; H07/H08/H10/H11/H12 |
| D025 | NOT_STARTED | S004 gate | S003 acceptance needed |
| D026 | NOT_STARTED | S004 gate | S003 acceptance needed |
| D027 | NOT_STARTED | S004 audio gate | AUDIO_ASSET_PENDING_OWNER |
| D028 | NOT_STARTED | S004 cut/fall gate | S003 acceptance needed |
| D029 | NOT_STARTED | S004 log/debris visual polish | S003 pickup authority is not S004 art acceptance |
| D030 | NOT_STARTED | S004 UX gate | S003 acceptance needed |
| D031 | NOT_STARTED | S004 environment acceptance | S003 acceptance needed |
| D032 | NOT_STARTED | S004 QA/G1 | G1 PENDING_OWNER |

## Değişen mevcut C# — tam liste

| Repository-relative file | Symbols | Amaç |
|---|---|---|
| `Assets/LastSignal/Scripts/Runtime/Combat/MeleeAttackResolver.cs` | MeleeTargetOutcome, MeleeAttackResolver.LastOutcome/Resolve/Blocked | Tek nearest visible enemy/harvest seçimi, exclusive dispatch/occlusion/tie policy |
| `Assets/LastSignal/Scripts/Runtime/Combat/MeleeWeaponController.cs` | Owner/Eye/Origin, GatheringAuthority/BindGathering, LastMeleeOutcome/Resolve | Gerçek owned swing bağlamı; existing ActiveWindow/noise yolu |
| `Assets/LastSignal/Scripts/Runtime/Inventory/WorldItem.cs` | BindPickupGuard, TryInteract, Prompt | Optional guard; mevcut transaction; harvest full-inventory feedback |
| `Assets/LastSignal/Scripts/Runtime/Persistence/SaveGame.cs` | SaveGame.gathering, SaveHeader.gatheringVersion, WorldItemOrigin.Harvest=4 | Additive DTO/marker/provenance |
| `Assets/LastSignal/Scripts/Runtime/Persistence/SaveCodec.cs` | Decode | Exact empty optional gathering normalization |
| `Assets/LastSignal/Scripts/Runtime/Persistence/SaveValidation.cs` | Validate, Pose | Version/state/identity/catalog/cell bounds; shared pose validation |
| `Assets/LastSignal/Scripts/Runtime/Persistence/SaveFileStore.cs` | Write, LastWriteMayHavePublished, Equivalent | Publish ambiguity seam + complete normalized snapshot comparison |
| `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs` | ValidateAuthoring, Capture, CommitGathering, LoadCore, ValidateTopology | Domain owner/capture integration; full generation durable commit; compatible restore |
| `Assets/LastSignal/Scripts/Runtime/Session/SessionFlow.cs` | BeginSession, ReturnToMenu | Optional gathering Begin/End ordering |
| `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs` | OwnerForPosition | Mevcut grid/defined cell lookup; grid ayarı değişmedi |

## Yeni C# — tam liste

| Repository-relative file | Symbols | Amaç |
|---|---|---|
| `Assets/LastSignal/Scripts/Runtime/Gathering/ResourceNodeDefinition.cs` | ResourceNodeDefinition.Valid/ValidOutput | Axe-only revisionlı finite health/yield/log contract |
| `Assets/LastSignal/Scripts/Runtime/Gathering/GatheringState.cs` | ResourceNodeState, GatheringSnapshot, enums; GatheringState.Create/Copy/Find/Prepare/Valid/Normalize | Finite state/candidate/condition/receipt/pending-world-consumed rules |
| `Assets/LastSignal/Scripts/Runtime/Gathering/HarvestManifest.cs` | HarvestManifest.Anchor/Valid | Authored stable IDs, pose, revision, source fingerprint/locator; bounded registry |
| `Assets/LastSignal/Scripts/Runtime/Gathering/ResourceNodeView.cs` | ResourceNodeView.Register lifecycle/Present/TryInteract | Standing projection + pending reward retry interaction |
| `Assets/LastSignal/Scripts/Runtime/Gathering/HarvestDurability.cs` | DurableOutcome, HarvestDurability.Publish | Candidate full-generation publication; committed/aborted/recovery distinction |
| `Assets/LastSignal/Scripts/Runtime/Gathering/GatheringWorld.cs` | Begin/End/Register/CellReady/CellLeaving/Capture/Compatible/Restore/TryHit/Commit/Ground/Project/RetryReward/OwnsPickup/RecordPickup | Session authority, safe projection, cell registry and live inventory receipt |
| `Assets/LastSignal/Scripts/Editor/HarvestPilotAuthoring.cs` | HarvestPilotAuthoring.CreatePilot + helpers; GatheringWorldInspector.OnInspectorGUI | Owner-only isolated stage/data/prefab authoring and read-only diagnostic inspector |
| `Assets/LastSignal/Scripts/Tests/EditMode/P01HarvestTests.cs` | LastSignal.Tests.P01HarvestTests | Pure transition + codec/store faults + existing melee active-window + actual resolver query fixtures |
| `Assets/LastSignal/Scripts/Tests/PlayMode/P01HarvestPickupTests.cs` | LastSignal.Tests.P01HarvestPickupTests | Actual WorldItem/PlayerInventory full/partial/guard path fixtures |

## Yeni meta dosyaları — tam liste

Mevcut meta dosyaları değiştirilmedi. Yeni script/folder GUID kayıtları (Unity import/duplicate-GUID testi NOT_RUN):

| Meta file | GUID |
|---|---|
| `Assets/LastSignal/Scripts/Runtime/Gathering.meta` | `af0e26391507641d5bbac5a4d9f286fd` |
| `Assets/LastSignal/Scripts/Runtime/Gathering/ResourceNodeDefinition.cs.meta` | `811d2ffcab37d4ca2bf274df315155c2` |
| `Assets/LastSignal/Scripts/Runtime/Gathering/GatheringState.cs.meta` | `4cd5d79cf5e4645a690a812e17745bb4` |
| `Assets/LastSignal/Scripts/Runtime/Gathering/HarvestManifest.cs.meta` | `63f1a6faf36d4a03a931e751cdd0056a` |
| `Assets/LastSignal/Scripts/Runtime/Gathering/ResourceNodeView.cs.meta` | `a4184de502fb4bd2a79eae858bdcd67a` |
| `Assets/LastSignal/Scripts/Runtime/Gathering/HarvestDurability.cs.meta` | `520e54682ce54013a9601cc1df8a8b3c` |
| `Assets/LastSignal/Scripts/Runtime/Gathering/GatheringWorld.cs.meta` | `8aa8c977ce03445db0423f7a7603aca1` |
| `Assets/LastSignal/Scripts/Editor/HarvestPilotAuthoring.cs.meta` | `4f0c77804e8349a6ba68c107692c88a5` |
| `Assets/LastSignal/Scripts/Tests/EditMode/P01HarvestTests.cs.meta` | `decda8a5efa34130928febaff1fef4e2` |
| `Assets/LastSignal/Scripts/Tests/PlayMode/P01HarvestPickupTests.cs.meta` | `119341150d964cb79baf2250f9f46198` |

## Değişen/yeni belgeler — tam liste

Bu checkpoint'teki bütün belgeler yeni `Docs/Implementation/BLD/P01/` altında:

- ENTRY_BASELINE.json — entry branch/HEAD/status + focused protected file SHA256.
- ENTRY_AUDIT.md — compact source map ve D017 başlangıç taxonomy.
- ADR_001_S003_DURABLE_HARVEST.md — authority, transaction/fault/restore/condition/cell decisions.
- ASSET_DECISION.md — existing asset provenance sınırı ve owner-run output paths.
- BLD_S003_CHANGED_FILES.md — bu dosya, exact paths/methods/meta ve card status.
- BLD_S003_TEST_HANDOFF.md — gerçek test names, one next command, sequential native/verification gates.
- OPEN_RISKS.md — compile/IO/native/art/license/recovery risk ve owner decisions.
- G1_GATE_RECEIPT.md — PENDING_OWNER evidence form, S004 stop boundary.

Canonical BLD phase/sprint/day/GDD/P00 docs değiştirilmedi. Yeni recipe/notebook/construction implementation yok.

## Mevcut dirty kullanıcı çalışması ve kapsam

Entry'deki Lighting.asset, CanopyMetalwork.asset, CanopySheet.asset, FirstTownCore_VisualSlice.unity, SliceRefinement05.cs, SliceVisualReview.cs, REFINEMENT_05.md ve `Docs/MeshyAI LAST SIGNAL/` bu çalışmanın değişiklikleri değildir; bunlara write uygulanmadı. Ayrıntılı yollar ENTRY_BASELINE.status içinde. FirstTownCore üretim/validation sahneleri, terrain, prefabs, forest generation assets, vendor, shaders, ProjectSettings, packages ve legacy S010 recipe/crafting/shelter implementation dosyaları düzenlenmedi. Yeni prefab/SO/scene **çıktıları yok**; authoring code hazırlanmıştır.

Bu kapsam beyanı agent'ın yaptığı write işlemlerine dayanır. End-of-turn protected hash testini user-only static-test kuralı nedeniyle çalıştırmadım; baseline hashes ve odaklı owner komutu hazır. İlişkisiz user değişikliklerinin rollback/reset'i yapılmadı. Son git diff/status kaynak incelemesi test PASS değildir.

## File-access inventory

Core map ENTRY_AUDIT.md'de. Dependency okumaları: owned equipment/startup için PlayerCombatController/SessionFlow; save normalization için SaveCodec/SaveValidation/SaveGame/SaveFileStore; cell readiness ve custody için WorldCellContent/WorldCellManager; actual pickup kapısı için InteractionController; finite schema ve statics için item DTOs; active-window test için MeleeAttackState/PlayerStamina. ForestAuthoringState/ForestDistribution/ForestSources'in yalnız source/selection/Place/Log bölümleri, ilgili prefab ve scene name/GUID aramaları. Test style/API için SaveFoundationTests, S021CombatTests, InventoryDomainTests/InventoryIntegrationTests, iki mevcut persistence class declaration ve mevcut asmdefs. Tools/s021-verify.py ve ProjectVersion.txt command/options için okundu. GDD/P00 ilgili policy pasajları ve aktif S003 cards kullanıldı; tüm Docs/ThirdParty/binary inventory yüklenmedi.

Authority/conservation ayrıntısı [ADR](ADR_001_S003_DURABLE_HARVEST.md), tek sonraki owner action [test handoff](BLD_S003_TEST_HANDOFF.md), G1 sonucu [receipt](G1_GATE_RECEIPT.md).
