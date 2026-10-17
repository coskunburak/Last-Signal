# S023 giriş, gap ve implementation kaydı

## Kullanıcı regresyon kanıtı — 15 Ekim 2026, 12:36–12:37

`Evidence/Manual-20261015-123613-focused-play-iddj535e/`: streaming/population/S022 archetype regresyonu **19/19 PASS**, source değişikliği yok. `Evidence/Manual-20261015-123717-regression-edit-wifibyy_/`: full EditMode **805/806 PASS, 1 FAIL**, source değişikliği yok. Blok durduğu için full PlayMode NOT_RUN.

Tek hata: `ArberryIntegrationAssetTests.CharacterOverridesPreserveEveryWeaponIdentityAndVariantGeometry`, satır 72, Diesel_WorldPistol yerine TPS_Pistol_TwoHand. Sınıf **UNRELATED_BASELINE_FAILURE / TEST_FIXTURE_ERROR**: mevcut `TPS_PISTOL_SUPPORT.md` ve Arberry profile authoring, dört tabanca için özel TPS destek prefablarını açıkça kullanıyor; test eski shared-prefab reference beklentisini taşıyordu. Test, dedicated TPS prefab + geçerli rigid support socket + kaynak mesh/material multiset eşitliği üzerinden güncellendi. Firearm/Melee identity, gameplay-authority yokluğu ve finger calibration kontrolleri korundu. Bu dar test değişikliği dışında kullanıcı karakter/prefab/profil üretim kaynaklarına dokunulmadı. İlk teslimdeki tüm kullanıcı dosyalarının korunması kaydı ilk aşamaya aittir; bu açık regression düzeltmesi o tarihten sonraki değişikliktir.

Düzeltme sonrası `Evidence/Manual-20261015-124004-focused-edit-tjvo1s1n/` kullanıcı retesti **1/1 PASS**: beklenen Arberry metodu tests.xml içinde Passed, exitCode=0, source-preservation changed=[]. Mevcut TPS prefablarının mesh/material koruması ve support socket kontrolleri bu koşuda başarılı. Sıradaki işlem full EditMode, ardından full PlayMode; bunların güncel sonuçları henüz bekleniyor. Eski FAIL evidence silinmedi. Agent hiçbir Unity testi çalıştırmadı.

## Kullanıcı kanıtı — 15 Ekim 2026, ilk native/test turu

- `Evidence/Manual-20261015-122203-author-vdm7w8_z/result.json`: authoring PASS, reused=false, unexpected=[]; native çıktılar artık üretildi.
- `Evidence/Manual-20261015-122240-author-arntt5i7/result.json`: authoring PASS, reused=true, unexpected=[]; tekrar authoring kaynak koruması PASS.
- `Evidence/Manual-20261015-122310-focused-edit-5mv3zrj2/result.json`: focused EditMode 20/20 PASS.
- `Evidence/Manual-20261015-122421-focused-play-wxb3vqvc/result.json`: PlayMode 1/2 PASS; cancellation/missing-navigation PASS, roundtrip FAIL (`Enemy revived`, ilk resident save/load sonrası depot dönüşünde). Source-preservation changed=[]; sonraki regression gate'leri çalışmadı.

Triage: **TEST_FIXTURE_ERROR**. Fixture Enter() içinde actor.SetPaused(true) çağırıyor; ZombieController.SetPaused incoming damage'i kapatıyor. Sonraki TakeDamage bu nedenle reddediliyor, fakat fixture ölüm önkoşulunu kontrol etmeden save/load'a ilerliyordu. Üretim persistence hatası bu sonuçtan çıkarılamaz. Düzeltme: aynı frame içinde actor.SetPaused(false) → ReceiveDamage(Melee); Committed/Terminal receipt, ölü health, encounter capture ve unloaded snapshot health=0 assertion'ları eklendi. Production damage/save/streaming kodu değiştirilmedi. Düzeltme sonrası `Evidence/Manual-20261015-123215-focused-play-mpcrxy2c/` targeted retest **1/1 PASS**: tests.xml Passed, process exitCode=0, source-preservation changed=[] doğrulandı. Ölüm receipt/snapshot önkoşulları ve tam roundtrip artık bu kullanıcı koşusunda başarılı. Sıradaki adım streaming/population/archetype ve full regression. Önceki evidence korunuyor.

Bu kullanıcı kanıtları aşağıdaki ilk teslimdeki NOT_RUN/native-output kayıtlarının ilgili satırlarını günceller; görsel kabul, build, full regression, performans ve gerçek süre ölçümleri hâlâ bekleniyor. Genel durum PARTIAL.

## Kullanıcı compilation triage

Kullanıcı CS0117 (`Is.AnyOf`) ve CS1061 (`PlayerInventory.Container`) bildirdi: **COMPILATION_ERROR / TEST_FIXTURE_ERROR**. Projenin NUnit API'si için üyelik assertion'ı `Does.Contain` olarak değiştirildi; PlayMode fixture `internal Container` yerine public `Capacity` ve `GetSlot` kullanıyor. Production inventory API'si ve asmdef erişimleri değiştirilmedi. İlk bildirilen compilation sonucu FAIL idi; düzeltmelerin ardından kullanıcı authoring, focused EditMode ve targeted PlayMode koşularını başarıyla çalıştırdı. İlk başarısızlık kayıtları korunuyor.

15 Ekim 2026. Genel durum **PARTIAL — geniş regresyon, manuel kabul, build ve ölçümler bekleniyor**. Native authoring ve ilgili dar testler kullanıcı tarafından çalıştırıldı; aşağıdaki kart tablosu ilk teslim kaydıdır, güncel sonuçlar yukarıdadır. Agent Unity/test/build/profiling çalıştırmadı; commit/push yapılmadı.

## Yerel giriş

- Branch `s012-integrated-graybox-slice`, HEAD `9191a5cd7bd245ee52e3bd9dab11466702ac2931`. GitHub main ile eşitlik varsayılmadı; fetch/checkout yok.
- Başlangıçtaki değişiklikler `entry-status.txt` içinde. Karakter, silah, Player.prefab, S013Cabin, testler, ProjectSettings ve Arberry untracked içerik kullanıcıya aittir. Bunlar düzenlenmedi. Yerel başlangıç hash kontrolü `static-review.json` içinde raporlanır.
- Unity `6000.5.0f1 (88b47c5e7076)`; URP 17.5.0, AI Navigation 2.0.13, Input System 1.19.0, Test Framework 1.7.0. Package manifest/lock değiştirilmedi.
- Library/LastSceneManagerSetup kaydı S013Cabin'i gösteriyor; canlı Editor durumu denetlenmedi. EditorBuildSettings hâlâ eski validation sahnelerini içeriyor; değiştirilmedi. S023 build aracı sahneyi açıkça seçer.
- İlgili dizinlerde AGENTS.md bulunmadı. Başlangıç belgeleri: S023, LS-DOC-13/19/20 ve S019 checklist. Somut bağımlılıklar için S013 kit/lisans kaydı ve mevcut authoring/runner kaynakları incelendi; geçmiş evidence arşivi okunmadı.

## Gap analizi ve karar

| Bağımlılık | Yerel gerçek | S023 kararı |
|---|---|---|
| CellCoordinate | Size=64, half-open koordinatlar | Ölçek ve mevcut cell kimlikleri değişmez; yeni 3/4 eklenir |
| WorldCellManager | Prefab instantiate, token/generation, collision/nav/hydration readiness, snapshot/unload, explicit portal | Aynı manager; otomatik kesintisiz sürüş streaming'i eklenmez |
| WorldCellContent | Tek loot owner ve tek authored encounter, door/item/enemy capture/restore | Her yeni hücrede bir encounter; küçük POI aynı cell sahibine bağlı |
| SaveSession/SaveValidation | WorldId ve exact cell topology; tek resident encounter; tombstone ve ownership transaction | Yeni worldId + s023-pilot slot. Eski save üzerinde migration/yazma yok |
| S019 | CreateNew identity rekey, PoiAuthoringLayout, ContentValidation, native surface | Yeni instance template'ten oluşturulur, yeni geometriye dönüştürülür; S019 validator kullanılır |
| S013 | Proje-owned kit, URP materyalleri, mevcut resident sahne | Kaynak mesh/material referansları paylaşılır; yeni doku/asset paketi yok |
| S022 | Lurker/Runner prefabs, population ledger, hidden candidate materialization, saved archetypes | Mevcut davranışlar; seed ve küçük population ayarı yalnız kopyada |
| Eksik ikinci bölge | Hazır production bölgesi bulunmadı | İki cell authoring, testler, rota/perf/maliyet handoff'u eklendi |

## D221–D230 kayıtları

Kod teslimi ve native içerik kabulü ayrıdır. Evidence henüz üretilmedi; yollar beklenen çıktılardır.

| Kart | Implement edilen teslim | Durum | Verification | Kanıt / sonraki işlem |
|---|---|---|---|---|
| D221 | REGION_DESIGN: rol, risk/ödül, 64 m alan, asset/bellek/saat hipotezi | IMPLEMENTED | NOT_RUN | Kullanıcı tasarım/oynanış kabulü |
| D222 | Authoring: giriş, alternatif şerit, retreat, landmark, karşılıklı terminaller; nav route uzunlukları | PARTIAL | NOT_RUN | authoring.json routes + day/night/rain rota videosu |
| D223 | Kaynak sahneyi koruyan ek 3/4 tanımları; tek nav owner; dependencies kaydı; slot izolasyonu | IMPLEMENTED (kod) | NOT_RUN | Authoring → focused-edit → save uyumluluğu |
| D224 | Depo kabuğu, gerçek Door prefabı/ID, rain roof, navigation bake ve clearance kontrolü | PARTIAL | NOT_RUN | Native üretim, kapı açma ve agent traversal |
| D225 | Küçük açık kontrol noktası + farklı açık sahada Runner/gantri/yığınlar | PARTIAL | NOT_RUN | Kullanıcı rota/risk/ödül kabulü |
| D226 | S013 ortak palet/mesh/material, sınırlı anlatı, gündüz/gece/yağmur koşulları | PARTIAL | NOT_RUN | Render/LOD/collision karşılaştırması; screenshot |
| D227 | 3 sınırlı mevcut-catalog loot profile, mevcut encounter/population sahipliği | IMPLEMENTED (kod) | NOT_RUN | Loot placement, tombstone ve population regression |
| D228 | İzole roundtrip test: pickup/drop/door/death→yard→resident→save/load→depot; cancellation/nav failure | IMPLEMENTED (test/handoff) | NOT_RUN | Focused PlayMode + gerçek process restart |
| D229 | Opt-in capture, offline matched comparison, 50-cycle prosedürü | IMPLEMENTED (altyapı) | NOT_RUN / NOT_MEASURED | Kullanıcının capture.json/raw CSV/Profiler verisi |
| D230 | Süre kayıt şeması, maliyet sınıfları ve ölçüme bağlı tahmin yöntemi | IMPLEMENTED (kayıt altyapısı) | NOT_MEASURED | Kullanıcının gerçek zaman kayıtları |

## Dosyalar ve sahiplik

Yeni kod:

- `Assets/LastSignal/Scripts/Editor/S023/S023RegionAuthoring.cs`: kullanıcı çalıştırır; CreateNew/S019 validation, asset üretimi, native bake, güvenli tekrar çalıştırma.
- `Assets/LastSignal/Scripts/Editor/S023/S023Build.cs`: sonraki aşama için açık S023 development build girişi.
- `Assets/LastSignal/Scripts/Runtime/Regions/S023Capture.cs`: yalnız pilotta opt-in ölçüm ve mevcut weather sunumu; gameplay authority değil.
- `Assets/LastSignal/Scripts/Tests/EditMode/S023RegionTests.cs`, `PlayMode/S023RegionPlayTests.cs`: mevcut test asmdef'lerinde.
- `Tools/s023-author.py`, `Tools/s023-verify.py`, `Tools/s023-compare.py`: mevcut runner güvenlik/evidence düzeni; her invocation tek gate.
- İlgili `.meta` dosyaları ve S023 hedef klasör metaları; `Docs/Implementation/S023/` kayıtları.

**Henüz üretilmemiş native çıktılar:** `Scenes/S023/S023RegionPilot.unity`, `Prefabs/S023/{MaintenanceDepot,LoadingYard}.prefab`, `Data/S023/{Cell3Navigation,Cell4Navigation,Maintenance,Checkpoint,Shipment}.asset`. Kullanıcı author komutu üretir. Bunlar bugün varmış gibi raporlanmaz.

Kritik mevcut kaynaklar **değişmedi**: WorldCellManager/Content, CellCoordinate/Portal, SaveSession/Validation/DTO, LootPopulationService/Profile/ItemCatalog, OwnershipTransaction, ZombieEncounter, WorldPopulationManager, S019 araçları, S013Cabin, Player.prefab, ThirdParty, paketler ve test asmdef'leri.

## Güvenlik ve geri alma

İlk author çalıştırması herhangi bir hedef dosya/meta zaten varsa partial-output hatasıyla durur. Tam sahne varsa tekrar çalıştırma sadece doğrular; GUID/ID, kullanıcı sahne düzenlemesi veya bake'i yeniden yazmaz. Yeni çağrının oluşturduğu assetler hata durumunda temizlenir; önceden var olan dosyalar silinmez. Harici crash sonrası kısmi çıktılar otomatik silinmez: evidence incelenir. Kaynak scene hiçbir zaman SaveScene hedefi değildir.

Normal runtime save migration eklenmedi. Eski `current.json` değişmez; kaynak hücrelere yeni tanım eklemek mevcut kaydı otomatik uyumlu yapmaz. S023 slotu farklı worldId kullanır. Kabul testleri temp path kullanır. Manuel yeniden denemede `s023-pilot` kaydını silmek yerine açıkça yedekleyin veya yeni disposable slot tasarlayın; author aracı kayıt dosyalarına dokunmaz.

## Açık riskler

- **S0:** Bilinen/doğrulanmış veri kaybı yok; kullanıcı verisi üzerinde test yapılmadı. Bu, S0 yokluğunun testle kanıtı değildir.
- **S1 kabul blokları:** Unity compilation, native bake/sahne üretimi, gerçek kapı/agent geçişi, iki yönlü save/load ve process restart henüz NOT_RUN. Genel sprint PARTIAL kalır.
- Source clone mevcut karakter/Arberry çalışmalarını referanslarıyla alır. Bunlardaki compilation/import hataları S023'ten ayrı sınıflandırılmalı; kaynak sessizce değiştirilmemeli.
- S019 validator source scene'de eski bir hatayı bulursa raporlamak gerekir; S023 için bypass eklenmedi.
- Resident ve eski prefab referansları saklandığından unload tüm referenced mesh/texture belleğinin boşalacağı anlamına gelmez. Memory plateau ve duplicate texture ölçümü bekliyor.
- Simplified metal kabuk, düşük detaylı yığınlar, yeni alanlarda baked lighting/özel ambience/LOD tuning yok. Day/night/rain görsel kabulü bekliyor; native sahne üretimi final art anlamına gelmez.
- Portal geçişinde sürüş desteklenmez. Native authoring kontrolü player capsule ve runtime nav carving kabulünün yerine geçmez.
- Yeni testler save/reload'u aynı process içinde yapar; gerçek exit/relaunch ayrıca zorunludur.
