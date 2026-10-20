# BLD-S003 — owner verification handoff

2026-10-18 · `IMPLEMENTED_AWAITING_OWNER_TEST` / `AWAITING_OWNER_FOCUSED_TEST`. Code preparation is complete for this checkpoint; native pilot authoring and acceptance remain open. Agent did not run compilation, EditMode, PlayMode, build, benchmark, profiler, static-test script or native menu. Every result below starts **NOT_RUN_BY_AGENT**. No runtime gate is N/A for this C# change.

## Şimdi yalnız bir odaklı adım

Mevcut Unity değişikliklerini kendi checkpoint tercihinizle koruyun ve Editor'ü normal biçimde kapatın. Sonra:

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/s021-verify.py" s021-edit --filter LastSignal.Tests.P01HarvestTests
```

Bu mevcut runner'ın `--filter`, `s021-edit`, `LastSignal.EditModeTests` assembly ve `EditMode` seçenekleri kaynak kodundan incelendi; komut çalıştırılmadı. ProjectVersion `6000.5.0f1`. Default executable `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`; kurulum başka yerdeyse runner'ın mevcut `--unity PATH` seçeneğini kullanın. Unity açık/lock mevcutsa runner durur. Ayrı compile/build komutu şu anda gerekmiyor; focused test invocation gerekli compilation'ı yapar.

Runner S021 isimli olduğu için evidence **`Docs/Implementation/S021/Evidence/<timestamp>-s021-edit-<suffix>/`** altındadır; bu P01 filter'ı S021 acceptance anlamına gelmez. `EVIDENCE=...`, exit code, `gate-result.json`, `tests.xml`, `unity.log`, varsa `failure-excerpt.txt` gönderin. Beklenen: yalnız P01HarvestTests test cases discovered >0; passed, skipped/inconclusive=0; process exitCode=0; unexpected sourceChanged=0; compile/runtime exception yok. Bunlar beklenen çıktılardır, mevcut sonuç değildir. Runner baseline snapshot'ı bu invocation başlangıcındaki dirty tree'yi koruma için alır; bizim entry hash karşılaştırmamızın yerine geçmez.

Başarısızsa yalnız ilk failure/compile diagnostics incelenecek, en küçük düzeltme yapılıp aynı filter veya tek fully-qualified method retest'i verilecek. Sonuç gelmeden başka suite/PlayMode/scene authoring istenmiyor.

## Yazılmış odaklı testler

File: `Assets/LastSignal/Scripts/Tests/EditMode/P01HarvestTests.cs`; class `LastSignal.Tests.P01HarvestTests`.

| Method / cases | Kabul / beklenen invariant |
|---|---|
| AcceptedHitDetachesCandidateAndDebitsOneConditionPoint | H01: source unchanged, health75/revision1/condition99 |
| WrongToolNeverMutatesSource (4 tools) | H02: WrongTool, candidate null |
| DuplicateAndStaleRevisionCannotAdvanceBudget | H04: Duplicate/StaleRevision, budget3 |
| FinalHitCreatesOneDeterministicRewardAndNoRespawn (clear/no clear) | H08/H14: final health0, yieldSeq1, fixed ID, World/Pending |
| LastConditionPointPaysForBlowThenBrokenToolRejectsNextHit | H06: final paid wear, no free next hit; second fixture terminal yield at condition0 |
| MalformedGatheringNeverEncodes (6 faults) | future version, quantity over budget, revision, condition, NaN, cross-domain duplicate ID rejected |
| PartialAndDepletedSavesRoundtripTwiceWithoutChangingLegacyOwners | H07/H08: codec roundtrip, health/pickup/legacy owners unchanged |
| RestoredReceiptRejectsSameSwingAndNewSessionUsesNewReceiptNamespace | H04/H08: replay rejected; legitimate new session namespace accepted |
| TrulyAbsentLegacySectionRemainsAbsent | H12: actual legacy envelope without section, gathering null, inventory/loot intact |
| ExistingActiveWindowChargesOnceAndIsFrameRateIndependent (30/60/120/1) | H01/H04: real MeleeAttackState callback once, stamina once; no Physics/scene assertion implied |
| NearestTreeRejectionNeverFallsThroughToEnemy (2 layouts) | H05: actual resolver/Physics query, nearest enemy hit or unbound tree rejection; no fallthrough. Fully bound native harvest/enemy overlap still pending |
| CancelBeforeActiveWindowProducesNoNodeMutation | cancel before ActiveWindow, zero callback |
| RealSaveStoreFaultBoundaryResolvesPublicationWithoutDuplicatingReward (4 faults) | H11: actual SaveFileStore with ISaveFiles fault seam; disk-full/prepublish abort, postpublish exact reconciliation, unreadable recovery |
| EquivalentChecksInventoryAsWellAsHarvestState | H11: whole snapshot equivalence, inventory mismatch is not commit evidence |

Fixtures are constructed inside tests; existing SaveFoundationTests.Fixture supplies legacy world owners, new in-memory ISaveFiles supplies fault injection. No existing fixture file overwritten. Codec/fault tests do not prove real filesystem replacement under physical power loss, native cell/nav hydration or final scene gameplay.

## Sonraki adımlar — şu anda çalıştırmayın

Focused owner PASS sonrasında tek incremental test: class `LastSignal.Tests.P01HarvestPickupTests`, file `Assets/LastSignal/Scripts/Tests/PlayMode/P01HarvestPickupTests.cs`, existing `LastSignal.PlayModeTests` assembly. Its methods:

- FullInventoryRetainsStableWorldPickupAndDoesNotNotifyMutation.
- PartialCapacityThenCompletePickupConservesTenLogsWithoutRepeatReward.
- GuardRejectsUnavailableOwnerBeforeInventoryMutation.

Bunlar gerçek WorldItem/PlayerInventory/InventoryContainer yolu + explicit guard/listener seam kullanır; full GatheringWorld scene fixture olduğunu iddia etmez. PlayMode command bir sonraki owner-result turunda verilecek. Sonrasında native stage + save/cell ve dar legacy regression ayrı ayrı açılır; mevcut S011PersistenceTests, FirstTownPersistenceTests ve S021CombatTests sonraki inceleme noktalarıdır. S010 suite filter henüz bu checkpoint için keşfedilmedi; tahmini komut yok.

## Native stage — focused test kabulünden sonra owner setup

1. VisualSlice dirty çalışma ve açık unsaved sahne için kendi checkpoint'inizi alın; bu dosyayı kaydetme kararı size aittir. Yalnız kayıtlı/clean FirstTownCore_VisualSlice açık olsun.
2. Generated altında tam 2 Pine +1 Birch prefab root seçin; HeroTrees hariç, birbirinden >=7 m, yol/door/POI/exit/nav kritik bölgeler dışında. Generic decoration/root/terrain tree index seçimi kabul edilmez.
3. [Asset kaydındaki](ASSET_DECISION.md) tek menu çağrısı yeni isolated scene/catalog/manifest/prefab üretir. Console `S003_STAGE_PREPARED ... clearanceApproved=false` receipt'ini saklayın. Source SHA, new scene path, manifestId/nodeIds ve saveSlot'u receipt'e ekleyin. Kopyada selected trees overlay'e taşınır; production/VisualSlice kaynağı promote edilmez.
4. Inspector GatheringWorld: explicit protectedAreas exclusion volume'larını (road, entrances, POI, evacuation/nav corridor, water-edge gereken alanlar) atayın; trigger collider kullanılırsa solid gameplay obstacle yaratmaz. Terrain dışı yürünebilir zemin gerekiyorsa additionalGround explicit atanır. Native collision/nav/route incelemesi sonrası **yalnız stage'de** clearanceApproved=true. Eksik değerlendirmede false bırakın.
5. Mevcut new-game akışını açın. Mevcut equipment/hotbar yolundan Wooden Axe seçin; sahte inventory axe grant'i yok. Inspector read-only snapshot düğmesi `S003_SNAPSHOT` JSON ve gerçek save path'i Console'a yazar. Gameplay üzerinden vurun; bu inspector düğmesi hasat/test başlatmaz.
6. Dört accepted hit için beklenen node health 100→75→50→25→0, node revision0→4, axe100→96; reward qty3, yieldSequence1, ID aynı. Her hit yalnız bir accepted impact/noise; not-ready/disk failure hit kabul edilmez, paid swing stamina geri verilmez. No safe point: pending + retry prompt, daha sonra aynı ID world'e geçer. Terminal standing visual kapanması S003 teknik projection; stump/fall kalitesi S004 değildir.
7. Full/partial pickup, snapshot, menu/load twice ve old/no-section save'i **ayrı pilot slot/backuplar** ile doğrulayın. Baseline legacy save dosyasını overwrite etmeyin. Old fixture requires same world/content/legacy topology and explicit authored pilot migration; codec test tek başına bu native kabul değildir.
8. Nonresident test gerektiğinde: aynı manifest/nodeId/pose ile mevcut test cell content'inin project-owned kopyasına view; source/cell prefabs overwrite yok. Bu checkpoint resident pilot üretir, nonresident fixture hazır değildir. Cell leave/return aynı pickup ID/remainder ve depleted state'i göstermeden D024 kabul edilmez.

## Gerekli evidence matrisi

| ID | Owner kontrolü | Durum |
|---|---|---|
| H01 | valid swing, one health revision/stamina/noise | NOT_RUN |
| H02 | wrong axe/slot/unowned controller, no mutation | NOT_RUN |
| H03 | wall, roof, range edge, behind camera, dead/menu | NOT_RUN |
| H04 | duplicate colliders/input; 30/60/120 native cadence | NOT_RUN |
| H05 | overlapping zombie/tree; one nearest result only, no dual damage | NOT_RUN |
| H06 | last condition point + broken next hit + reload | NOT_RUN |
| H07 | damaged node save/load | NOT_RUN |
| H08 | terminal save/load twice + partial/consumed pickup tombstone | NOT_RUN |
| H09 | full/one slot/partial, mass and integer conservation | NOT_RUN |
| H10 | native unload/reload, pose/definition mismatch controlled error | NOT_RUN; nonresident fixture pending |
| H11 | publish faults/recovery + committed-before-projection reload | NOT_RUN |
| H12 | real old save no section + legacy job/inventory intact | NOT_RUN |
| H13 | canonical melee noise count vs audio/AI listeners | NOT_RUN; final audio S004 |
| H14 | slopes, roots, water, protected zone, pending retry, collider buffers | NOT_RUN |
| H15 | S010/S021/FirstTown regression | NOT_RUN |
| H16 | wind/LOD/day/dusk/night/rain and chop/fall presentation | NOT_RUN; S004 dependency |
| H17 | IO hitch/allocations and 10–20 sequential harvests | NOT_RUN; pilot only3 nodes, later explicit larger test manifest needed |

## Owner-only static preservation — ayrı gate için hazır komut

Bu komut agent tarafından çalıştırılmadı; ilk focused sonucu gelmeden gerekli değil. Baseline tracked source hashes ile karşılaştırır, hiçbir dosya yazmaz:

```bash
cd "/Users/burakcoskun/Last Signal"
python3 - <<'PY'
import hashlib, json
from pathlib import Path
baseline=json.loads(Path('Docs/Implementation/BLD/P01/ENTRY_BASELINE.json').read_text())
changed=[p for p,h in baseline['protected'].items() if not Path(p).exists() or hashlib.sha256(Path(p).read_bytes()).hexdigest()!=h]
print(json.dumps({'protectedDrift':changed},indent=2))
raise SystemExit(bool(changed))
PY
```

Entry hash alınmasından sonra owner'ın bilerek yaptığı kaynak save de drift sayılır; önce actual diff incelenir, otomatik geri alma yapılmaz. Hash kapsamı ENTRY_BASELINE.protected ile sınırlı; Meshy untracked klasörünün bütün binary içeriklerini taradığı iddia edilmez.
