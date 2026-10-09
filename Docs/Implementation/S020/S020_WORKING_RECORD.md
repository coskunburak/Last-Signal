# S020 çalışma kaydı — 2026-10-09

Branch `s012-integrated-graybox-slice`, başlangıç HEAD `7f5ffb783f396f9675c28da18be7021d90a02eb7`. Unity proje sürümü ve kurulu binary: `6000.5.0f1 (88b47c5e7076)`, `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`. Unity açılmadı; test/build çalıştırılmadı. Commit, stage, push, paket kurulumu yapılmadı.

Başlangıçta 262 satırlık dirty çalışma ağacı vardı; `Evidence/initial-status.txt` kaydedildi. Normal `git status` LFS clean filtresinin `.git/lfs/tmp` yazma iznine takıldı. Salt-okuma envanteri için yalnız komut kapsamında LFS filtreleri kapatıldı; Git ayarı değiştirilmedi. Önceden var olan değişiklikler geri alınmadı. Düzenlenen dosyaların başlangıç kopyaları `/tmp/s020-before` ve `/tmp/s020-before-assets` altında tutuldu; kalıcı dosya listesi `S020_CHANGED_FILES.md` içindedir. AGENTS.md bulunmadı. Gerçek Unity import/derleme kanıtı yoktur.

## Mimari denetim

| Alan | Başlangıç sınıflandırması | S020 kararı |
|---|---|---|
| Item catalog / stable ID | Existing and evidenced: S019 handoff; S020 NOT_RUN | 10 eski ID ve GUID korundu, 7 yeni kayıt |
| Item ownership / slots | Existing and evidenced | Definition+quantity stack modeli; ikinci instance/owner sistemi kurulmadı |
| Genel gram limiti / overload bantları | Missing | Kütle verisi ve türetilmiş toplam eklendi; kapasite hâlâ yuva temelli. Üretim kapsam farkı açık |
| Food / water | Existing but fixed constants | Etki definition verisine taşındı; eski varsayılanlar 35/40 korundu |
| Injury / treatment | Existing bleeding only | Süre ve kanama azaltma verisi; health potion eklenmedi |
| Equipment | Partially implemented: tek +8 çanta | +12 çanta, toparlanma maliyeti, katalog tabanlı save doğrulaması |
| Clothing / condition | Missing | Giysi koruması ve kondisyon onarımı deferred |
| Craft escrow / refund | Existing and evidenced | Aynı tek-job transaction; çoklu tarif seçimi ve job-ID çözümleme |
| Recipe validation | Existing and evidenced | Eski validator + döngü reddi korundu |
| Loot selection / prefab | Existing and evidenced | Seed algoritması korunarak 3 profil değişti, 3 garantili profil eklendi |
| Save/load | Existing and evidenced | Şema, eski ID, stack ve contentVersion değişmedi; yeni çanta bonusu katalogdan |
| Content revision | Existing audit fingerprint | Fingerprint format 2 yeni gameplay alanlarını içerir; ikon/açıklama hash dışında |
| UI | Partially implemented: ad/miktar | İkon, açıklama, gram, stack, kapasite ve gerçek toplam kütle |
| Artwork | Partially implemented | 17 özgün sembol ikon; 7 yeni dünya görseli provisional, üretim sanat kabulü açık |
| Test assemblies | Existing and evidenced | Yeni EditMode ve PlayMode sınıfları mevcut assembly’lerde; NOT_RUN |

S019 handoff en güncel kullanıcı kanıtı: full EditMode 604/604, PlayMode 315/315; S019 görsel/manuel kabul ve emek ölçümü açık. Bu sayılar S020 doğrulaması değildir. S019 validator/build gate aktiftir. İlgisiz sprint geçmişi değiştirilmedi.

## Günlük kartlar

Bütün satırlarda otomatik doğrulama `NOT_RUN`, gameplay kabulü `NOT_RUN`; sprint kapanışı yapılmadı. IMPLEMENTED, kod/veri işinin kurulu olduğunu belirtir.

| Kart / gereksinim | Dosyalar | Uygulanan davranış | Test kapsamı | Uygulama / kalan belirsizlik |
|---|---|---|---|---|
| D191 / 72 aday eleme | S020_CATALOG_DECISIONS.md, RELEASE_MANIFEST.json | 8 mevcut eşdeğer, 6 yeni aday, 55 deferred, 3 rejected; ayrıca 1 yeni çanta | gerçek catalog sayımı | IMPLEMENTED / ürün ve oyun denge kabulü NOT_RUN |
| D192 / yemek-su | ItemDefinition, PlayerSurvival, food.crackers, drink.soda, Kitchen | 20 besin kraker; 25 su soda; seçilen stack atomik tüketilir | NewFoodAndDrink…, accumulation, full/stale eski S018 | IMPLEMENTED / genel gram pickup limiti mevcut değil |
| D193 / tedavi-alet | SurvivalState, PlayerSurvival, medical.rag, pressure-dressing, sewing-kit | Bez 5 sn/1 kademe; pansuman 1 sn/tüm kanama; dikiş seti gerçek recipe aracı | PartialTreatment…, RagTreats…, Interrupted… | IMPLEMENTED / yalnız kanama domaini; sanat provisional |
| D194 / ekipman | ItemDefinition, PlayerSurvival, SaveValidation, expedition-pack | +12 yuva, %15 toparlanma maliyeti; kapasite düşürme atomik reddedilir | RepeatedPackSwap…, LargePack…, repeated load | IMPLEMENTED / kıyafet/koruma ve genel mass bands deferred |
| D195 / tarif | ShelterProduction, ShelterSite, Rag/ExpeditionPack/SewingKit.asset, S013Cabin | Eski ammo + 3 yeni elektriksiz tarif, aynı escrow/refund; UI seçimi | RepeatedUtility…, PendingJob…, RecipeCycles…, SceneWorkbench… | IMPLEMENTED / kondisyon repair yok, açık deferred |
| D196 / loot | Kitchen/Clinic/Workshop, 3 S020 profil, S013Cabin | Kritik mutlak olasılık korunur; 1 su/1 bandaj/32 kumaş garanti | Exact probabilities, boundary seeds, actual spawn/save | IMPLEMENTED / fizik/erişim ve survival denge NOT_RUN |
| D197 / sunum | 17 item, ItemIcons, InventoryUI/SlotUI, yeni prefabs/materials | İkon/ad/açıklama/birim/UI; provisional dünya görselleri listelendi | Catalog metadata, prefab identity, UI description | PARTIAL / 7 dünya varyantının production görsel ayrımı/kabulü eksik |
| D198 / eski kayıt | SaveValidation, SaveSession, PlayerSurvival, ShelterProduction | Eski ID/stack/job korunur, bilinmeyen ID açık reddedilir; rename olmadığı için alias N/A | Legacy codec 4 tur, unsupported ID, 2 actual load | IMPLEMENTED / Unity kullanıcı kanıtı bekleniyor |
| D199 / uç ekonomi | S020CatalogTests, S020CatalogPlayTests | Yığın/capacity, craft korunum, refund, tool, seed, swap, save | Yeni testler + eski S018/S010/S019 regresyon | IMPLEMENTED / testlerin tamamı NOT_RUN |
| D200 / RC manifest | RELEASE_MANIFEST.json, ECONOMY, RUN_COMMANDS, MANUAL_QA | Gerçek asset sayımı; content/save ayrımı; QA komutları | Statik referans incelemesi; Unity validator bekliyor | IMPLEMENTED / RC VERIFIED değil, D197 PARTIAL |

## Mimari kararlar ve sınırlamalar

- Tek `ItemDefinition` veri kaynağı kullanılır. Yeni serialized field varsayılanları eski 40/35/3 sn/+8 davranışını korur. Eski ID’ler, GUID’ler ve stack limitleri değişmez.
- `InventoryContainer.TotalMassGrams` long aritmetiğiyle stacklerden türetilir; UI takılı çantayı ayrıca dahil eder. Bu bilgi bir ağırlık limiti veya ağır yük hareket sistemi iddiası değildir. Slot/portion ve toparlanma maliyeti gerçek davranış farkıdır.
- Tek `ShelterProduction` ve mevcut tek escrow job korunur. Opsiyonel ek tarif dizisi eski constructor ve `Start(source)` API’sini korur. UI seçimi çalışan job’u değiştiremez; load/refund job’un recipeId’sini çözer. Çok girdili tarif/ikinci crafting sistemi eklenmedi.
- SaveValidation constructor’ının eski çağrıları eski çantayı tanımaya devam eder. Gerçek SaveSession tüm ekipman bonuslarını aynı item catalog’dan geçirir. Recipe ID/revision/miktar/süre eşleşmesi zorunludur. Desteklenmeyen kayıt reddedilir; silerek devam edilmez.
- SaveSchemaVersion sabiti 2; mevcut hücre/population sahnesi schema 4 üretir. Survival ve production extension 1, sahne contentVersion değişmedi. `s020-rc1` release/audit revizyonudur; save schema değildir.
- Garantiler yalnız yeni dünyaya uygulanır. Eski kayıttaki açılmış/tüketilmiş kaynakları resetlemek tekrar üretim exploiti olacağından yapılmaz. Hücre yüklenmeden önce henüz üretilmemiş kaynaklar güncel profili kullanır; eski sürüm başına loot-table pin mekanizması mevcut değildir.
- Genel contamination, kırık/enfeksiyon/ağrı, sıvı hacmi, item kondisyonu, zırh ve nested inventory alanları deferred. Bunlar çalışıyor diye gösterilmez.
- Harici zorunlu paket yok. Yeni 3B sanat için primitive ambalaj/var olan bandaj ve çanta modelleri kullanıldı; production gerçekçilik tamamlanmış sayılmaz. D197 PARTIAL olduğundan tüm sprint için `IMPLEMENTATION COMPLETE` denmedi.

Durum: **PARTIAL — D197 üretim dünya görselleri açık; kurulmuş gameplay uygulaması MANUAL QA PENDING.** Süre/emek ölçümü yapılmadı. Sonraki sprint veya P06/G6 kapanışı READY/VERIFIED ilan edilmedi.


## Kullanıcı EditMode E01 — testler PASS, kaynak koruma FAIL

Kanıt: `Evidence/Manual-20261009-153811-s020-edit-kcro6v0m`. XML doğrudan incelendi: tam `LastSignal.Tests.S020CatalogTests` sınıfı, 32/32 Passed, failed/skipped 0; Unity exitCode 0. Genel runner sonucu FAIL olarak korunur.

Tek kaynak farkı `ProjectSettings/ProjectSettings.asset`: Standalone `scriptingDefineSymbols`, `APP_UI_EDITOR_ONLY;SENTIS_ANALYTICS_ENABLED` değerinden `APP_UI_EDITOR_ONLY` değerine değişmiş. Önce/sonra snapshot diff'i bunu doğrular; mevcut settings dosyası after snapshot ile byte-identical. Kurulu `com.unity.ai.inference` paketinin `Editor/Analytics/AnalyticsDefineManager.cs` dosyasında InitializeOnLoadMethod ile analytics durumuna göre bu sembolü ekleyen/kaldıran kod mevcut. Sınıflandırma ENVIRONMENT / source-preservation; test assertion veya S020 gameplay hatası değil. Logdaki boş NavMeshComponents assembly uyarısı compile failure değildir.

Kod, paket, analytics ayarı veya ProjectSettings değiştirilmedi; kaynak koruma kontrolü gevşetilmedi. Sonraki kullanıcı adımı: Editor'ü yeniden açmadan aynı `s020-edit` komutunu çalıştırarak mevcut settings ile `changed=[]` olduğunu doğrulamak. Sonraki temiz sonuç gelene kadar gate FAIL/RETEST; PlayMode'a geçilmez. Ajan Unity çalıştırmadı. D197 ve manuel gameplay kabulü açık kalır.


## Kullanıcı EditMode E02 ve PlayMode P01 — temiz PASS

E02 `Evidence/Manual-20261009-155328-s020-edit-r1k8ag56`: XML/result/process/source-preservation incelendi; tam S020CatalogTests 32/32 PASS, failed/skipped 0, Unity exit 0, changed=[]. Settings before/after byte-identical. Önceki E01 kaynak koruma FAIL kaydı korunur; temiz tekrar koşusu başarılıdır.

P01 `Evidence/Manual-20261009-155404-s020-play-bf9ev5qk`: XML/result/process/source-preservation incelendi; tam S020CatalogPlayTests 9/9 PASS, failed/skipped 0, Unity exit 0, changed=[]. Settings before/after byte-identical. Gerçek consumption/treatment, interrupted treatment, pack capacity/recovery, rejected downgrade, repeated save/load, guaranteed pickup/depletion, recipe UI/refund ve UI metadata senaryoları doğrulandı.

Kod düzeltmesi gerekmedi. Ajan test çalıştırmadı. Sonraki kullanıcı adımı full EditMode regression, başarılıysa full PlayMode regression. Yeni kaynak değişikliği olmadıkça hedefli testlerin tekrarı gerekmiyor. Build, manuel gameplay ve D197 production görsel işi açık; sprint closure/production readiness ilan edilmez.


## Kullanıcı full EditMode E03 — PASS

Kanıt `Evidence/Manual-20261009-163835-regression-edit-ry5mgy5g`: XML/result/process/source-preservation doğrudan incelendi. 636/636 PASS: LastSignal.EditModeTests 626, LastSignal.Art.EditorTests 10; failed/skipped 0; Unity exitCode 0; changed=[]. ProjectSettings before/after byte-identical. Bu koşu EditMode'dur; kullanıcının terminal echo etiketindeki PlayMode yazısı koşu türünü değiştirmez.

Kod onarımı gerekmedi; ajan Unity çalıştırmadı. Sonraki kullanıcı adımı full PlayMode (`regression-play`). Hedefli EditMode/PlayMode ve full EditMode yeni kaynak değişikliği olmadan tekrar gerektirmez. Full PlayMode, build, manuel gameplay ve D197 production dünya görselleri açık kalır; sprint kapanışı ilan edilmez.


## Kullanıcı full PlayMode P02 — PASS

Kanıt `Evidence/Manual-20261009-164315-regression-play-7esoa_ph`: XML/result/process/source-preservation doğrudan incelendi. LastSignal.PlayModeTests 324/324 PASS; failed/skipped 0; Unity exitCode 0; changed=[]. ProjectSettings before/after byte-identical; test süresi 850.82 saniye. Full EditMode 636/636 ve full PlayMode 324/324 artık kullanıcı kanıtıyla PASS.

Kod onarımı gerekmedi; ajan Unity çalıştırmadı. Yeni kaynak değişikliği/failure yoksa otomatik test tekrarı gerekmez. Sonraki kullanıcı adımı `development-build`, ardından S013Cabin manuel gameplay kabulü. Build/manual acceptance ve D197 production dünya görselleri henüz açık; sprint closure/production readiness ilan edilmez.


## Kullanıcı Development build B01 — teknik build başarılı, kaynak koruma FAIL

Kanıt `Evidence/Manual-20261009-170215-development-build-8gr_4xou`; çıktı `Builds/S020/Manual-20261009-170215-development-build-8gr_4xou`. result/process/source-preservation, settings before/after diff ve build.txt incelendi. Unity exitCode 0; BuildReport Succeeded, Errors=0, Warnings=381, build süresi 24.306352 saniye, StandaloneOSX Development, gerçek S013Cabin sahnesi. LastSignal.app ve Contents/MacOS/Last Signal binary dosyası mevcut. Uygulama çalıştırılmadı; runtime NOT_RUN.

Genel runner FAIL doğru biçimde korunur: tek değişen kaynak ProjectSettings/ProjectSettings.asset; Standalone sembolleri APP_UI_EDITOR_ONLY → APP_UI_EDITOR_ONLY;SENTIS_ANALYTICS_ENABLED. Bu kez sembol EKLENMİŞ (E01'de kaldırılmıştı). Mevcut settings after snapshot ile aynı. Kurulu Inference AnalyticsDefineManager.Initialize metodu bu sembolü analytics derleme/runtime koşullarına göre ekler/kaldırır; spesifik tetikleyen koşul yalnız snapshot üzerinden kesinleştirilemez. Sınıflandırma ENVIRONMENT / source-preservation, compile/build failure değil.

381 warning sıfırlanmış veya kabul edilmiş sayılmaz: obsolete Unity API, kullanılmayan alan, Sentis shader/compute varyant uyarıları, boş üçüncü taraf assembly ve RuntimePipelineConfig yokluğu bildiriliyor. Ayrı runtime kabulü gerekir; bunlar runner FAIL nedeni değildir.

Gameplay, paket veya proje ayarı değiştirilmedi; kaynak koruma kontrolü gevşetilmedi. Sonraki kullanıcı: Editor'ü yeniden açmadan aynı development-build komutuyla mevcut settings tabanında temiz tekrar. Yeni build benzersiz çıktı dizinine yazılır; B01 kanıtı/çıktısı korunur. Ajan build/test çalıştırmadı. Otomatik testlerin önceki PASS kanıtı korunur; build gate RETEST, manuel gameplay ve D197 sanat işi açık kalır.


## Kullanıcı Development build B02 — temiz PASS

Kanıt `Evidence/Manual-20261009-170502-development-build-tx15ul32`; çıktı `Builds/S020/Manual-20261009-170502-development-build-tx15ul32/LastSignal.app`. result/process/source-preservation/build.txt incelendi. BuildReport Succeeded, Errors=0, Warnings=347; Unity exitCode 0; changed=[]. ProjectSettings before/after byte-identical. Unity 6000.5.0f1, StandaloneOSX Development, S013Cabin; CleanCache=False, build süresi 12.292244 saniye. App ve MacOS binary mevcut; ajan uygulamayı açmadı, runtime NOT_RUN.

B01 kaynak koruma FAIL tarihçesi korunur; B02 temiz build gate PASS. Uyarı sayısının 381'den 347'ye düşmesi düzeltme kanıtı değildir (incremental build). Paket shader uyarıları, boş üçüncü taraf assembly ve RuntimePipelineConfig yokluğu hâlâ raporlanıyor; warning-free/release acceptance ilan edilmez.

Güncel kullanıcı kanıtı: targeted EditMode 32/32, targeted PlayMode 9/9, full EditMode 636/636, full PlayMode 324/324 ve Development build PASS. Yeni kaynak değişikliği/failure olmadıkça tekrar test/build gerekmez. Sonraki adım S020_MANUAL_QA.md ile gerçek gameplay, process-restart save/load ve D197 production dünya görsel kabulü. Genel sprint PARTIAL; D197 görselleri açık. Ajan test/build/uygulama çalıştırmadı; kod değişikliği gerekmedi.
