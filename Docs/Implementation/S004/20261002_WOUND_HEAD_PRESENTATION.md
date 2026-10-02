# S004 — Torso yarası ve yoğun kafa kopması sunumu

Tarih: 2026-10-02. **IMPLEMENTATION COMPLETE — USER VERIFICATION PENDING**.
Production kabulü verilmedi. Bu kayıt 20261001_BLOOD_VFX_INTEGRATION.md devamıdır.

## Başlangıç ve kapsam

Yerel çalışma ağacı otorite kabul edildi: 153 modified, 594 deleted, 238 untracked durum girdisi. Klasör girdileri dosya sayısı değildir. Önceki kullanıcı işleri korunmuştur; reset/clean/restore/stash/commit/push yapılmadı. Üç yardımcı ajan keşif ve incelemede yalnız okudu; değişiklikleri ana ajan yaptı.

Kanıt klasörü: `Evidence/20261002-WoundHead-131646/`. İlgili önceki dosyalar `before/` altında; `baseline-status.txt`, `files.json`, `implementation.patch`, `changed-files.txt` ve `final-status.txt` bu turun kapsamını ayırır. Önceki evidence klasörleri değiştirilmedi.

## Torso: teşhis ve uygulama

Mevcut PNG zaten detaylıydı. Asıl teknik sorunlar URP/Unlit materyalin ışık almaması, dört köşeli düz quad ve kemikten sabit 15 cm öne konumlandırmaydı. Bunlar gövdeye oturmama ve sticker etkisi riski oluşturuyordu; başlangıç oyun içi görüntüsü alınmadı.

- PNG ve import ayarları korundu; vendor kaynaklar düzenlenmedi.
- Mevcut mesh GUID'i korunarak 9×11 grid (99 vertex, 160 triangle) üretildi. Asset adı uyumluluk için `SM_Zombie_TorsoWound_Quad` kaldı.
- Yalnız editor authoring sırasında Ribcage BakeMesh yüzeyine raycast yapılıyor. Her nokta yüzey normalinde 1,8 mm yükseltiliyor; üçgenin barycentric bone weights değerleri en güçlü dört etkiye indirgenip normalize ediliyor. Dünya noktası ters birleşik skin matrisiyle mesh uzayına taşınıyor.
- Runtime'da aynı bones/bindposes/rootBone kullanan tek SkinnedMeshRenderer çalışıyor. Mesh cutting, runtime raycast veya mesh üretimi yok. Renderer kalite tercihi gövdeninkiyle aynı; kaynak bounds korunuyor.
- Materyal URP/Lit, metallic 0, smoothness .38, albedo çarpanı .88; alpha kenarlar korunuyor, gölge alıyor, emission yok. Ek çizim katmanı veya materyal örnekleme yok.
- `TorsoWound_Visual` adı, başlangıçta kapalı olma ve dismemberment referansı korundu. Wound nesnesi Ribcage altında; mevcut hasar/gore/save/reset akışı yönetmeye devam ediyor.
- `BindPhaseOneBodyParts` yeniden çağrıldığında mevcut yara referansını silen varsayılan null argümanı düzeltildi.

Yeniden üretim gerekirse `Last Signal/Zombie/Bind Torso Wound Visual`; şimdiki assetler zaten üretildi. Bu menü mevcut proje sahipli yara mesh/materyal/prefab ayarlarını yeniden yazar.

## Kafa: yoğunluk ve süre

| Parametre | Önce | Sonra |
| --- | --- | --- |
| Burst parçacığı | 18 | 32 |
| Bölge ölçeği | 1 | 1.1 |
| Burst başlangıç hızı | 2.8 | 4.6 |
| Spurt başlangıç hızı | 1.6 | 3.1 |
| Spurt cone | 18° | 24° |
| Spurt tepe oranı | 25/s | 72/s |
| Spurt süresi | 1.6 s | 2.1 s |
| Basınç tutma | yok | ilk .2 s |
| Pulse | ~3.5 Hz / .15 taban | 3.2 Hz / .28 taban |
| Son hız oranı | sabit | .32; ayrıca pulse etkisi |
| Drip | 2 s / 5/s | 2.2 s / 7/s |
| Yüzey izi boyutu | .30 | .34 |

Hızlar ParticleSystem başlangıç parametreleridir; hierarchy ölçeği ve yerçekimi gerçek dünya hareketini etkiler. Burst cone 65° ve olay başına en çok üç yüzey izi aynı kaldı. Ortak pressure eğrisi değiştirilmedi. Kol/el eski ayarlarını korur; yeni alanları açıkça eski değerlere serialize edildi.

Tüm hız/açı/rate ayarları slot başlangıcında mutlak atanır; kafa ayarı el/kol tekrar kullanımına birikmez. Rate, Play'den önce kurulur; emission kapalı vendor kökleri atlanır. Spurt anchor'ı takip eder, parçacıklar world-space kalır. Basınç bitince emission sıfır, ardından drip, sonrasında 1.5 s temizlenme payı. Kafa toplam slot ve terminal corpse lease penceresi **5.8 s**; eski 5.1 s'den .7 s uzun.

## Korunan mimari ve bütçe

ZombieDismemberment, ZombieBloodVfxPresenter, ZombieDetachedPartPool, health/death, save ve WorldPopulationManager runtime kodları değiştirilmedi. Başarılı sever bildirimi, tekrar vuruş guard'ı, restore sırasında replay olmaması, gore OFF temizliği, owner release ve scene/cell cleanup mevcut akışta kalır.

16 efekt slotu, 32 yüzey izi, 16 corpse lease; görünür burst/spurt/drip emitter başına 32/48/24 parçacık sınırı korundu. Teorik görünür toplam tavan 1664 parçacık; bu performans ölçümü değildir. Daha yüksek emisyon ve hız bu tavana daha sık yaklaşabilir; geniş ekran alanı overdraw maliyetini artırabilir. Yeni runtime Instantiate/Destroy, materyal kopyalama, LINQ, emitter framework veya coroutine eklenmedi.

Yüzey izleri owner yok olsa da destek collider/25 s ömür politikasını korur; gore OFF/session reset tümünü temizler. Yeni conformal yara, zemindeki planar blood card çözümünü değiştirmez. Ses ve stump cap eklenmedi.

## Hafif doğrulama

- Sınırlı Unity authoring: `authoring.log`, çıkış 0 ve `LS_WOUND_AUTHORING_COMPLETE`; asset 99 vertex/160 triangle olarak kaydedildi. 90 s watchdog aşılmadı.
- İlk kısa validator `validation.log`; son kalite/test düzeltmeleri sonrası `validation-final.log`: `LS_BLOOD_VALIDATION_PASS`, çıkış 0, C# derleme hatası yok. 75 s watchdog aşılmadı. Bu asset validator sonucudur; test/oyun içi kabul sonucu değildir.
- Loglarda lisans servisinin entitlement/access-token uyarıları ve boş assembly uyarısı var; başarılı authoring/validator marker'ları ayrıca doğrulandı.
- `git diff --no-index --check` çalışma öncesi dosyalarla karşılaştırıldı. Repo altındaki ilk karşılaştırmada `.gitattributes` makro uyarısı çıktı; /tmp izole kopyalarda tekrar kontrol edildi, whitespace hatası yok. `whitespace-isolated.txt`.
- `bash -n run-validation.sh`: sözdizimi başarılı.
- Tam EditMode/PlayMode, odaklı testler, render acceptance, build, stress ve uzun profiler çalıştırılmadı. Önceki test PASS sayıları bu değişikliğe taşınmadı.

## Hazırlanan odaklı kapsam

EditMode `LastSignal.Tests.ZombieBloodVfxTests`: 9 test. Yeni kapsam: production wound skin/material/bone/quality bağları; güçlü sonlu head profil sınırları ve geçersiz hız reddi.

PlayMode `LastSignal.Tests.ZombieBloodVfxPlayTests`: 10 test. Yeni kapsam: gerçek burst count/speed, stump takibi, ara faz hız düşüşü, emission durma/drip/expiry, head→hand slot tekrar kullanımında hız/cone reset. Mevcut testler duplicate sever, ölüm, gore toggle, owner disable/reuse, restore, mark bütçesi ve corpse cell cleanup'ı kapsar.

Mevcut `LastSignal.Tests.ZombieDismembermentPlayTests`: 4 test, torso damage/save/reduced gore dahil. İlk PlayMode komutu bu sınıfı da çalıştırır. Beklenen ilk kapsam **9 EditMode + 14 PlayMode**, fakat testler henüz çalıştırılmadı.

## Kullanıcının ilk komutları

Bu projenin açık Unity editörünü kapatın. Komutları sırayla çalıştırın; ilk koşu başarısızsa log/XML ile geri dönün. Script grafik desteğini korur; `-nographics` eklemeyin. Tam regresyon/build bu ilk döngüde istenmiyor.

1. Wound bağları, profil/bütçe ve mevcut event sözleşmeleri — EditMode:

```bash
bash '/Users/burakcoskun/Last Signal/Docs/Implementation/S004/Evidence/20261002-WoundHead-131646/run-validation.sh' edit
```

2. Burst/takip/decay/drip, pooling, gore, torso ve kopma yaşam döngüsü — grafik PlayMode:

```bash
bash '/Users/burakcoskun/Last Signal/Docs/Implementation/S004/Evidence/20261002-WoundHead-131646/run-validation.sh' play
```

Her çağrı `mktemp -d` ile ayrı `Evidence/20261002-WoundHead-131646/user-YYYYMMDD-HHMMSS-edit-XXXXXX/` veya `...-play-XXXXXX/` oluşturur; eski çıktı ezilmez. Tam klasör yolu terminalde yazılır. XML: `<yeni klasör>/results.xml`; log: `<yeni klasör>/unity.log`; süreç kodu: `<yeni klasör>/exit-code.txt`.

Kabul: XML mevcut, kapsam sıfır değil, failed=0, tüm seçilen testler Passed (beklenen 9/14), derleme hatası/çökme/beklenmeyen exception yok. Sadece çıkış 0 PASS değildir. XML yoksa log/çıkış kodunu gönderin.

## Manuel görsel kabul

`Assets/LastSignal/Scenes/ZombieAcceptance.unity` veya production sahnesi:

1. Bir zombi, gore ON: torso hasar eşiğini geçirip gündüz/gece/el feneri altında önden ve yandan inceleyin. Idle, yürüme, saldırı, hit ve ölüm pozlarında yüzeyden ayrılma, kart kenarı, gömülme ve plastik parlama olmamalı. Low ve normal kaliteyi karşılaştırın.
2. Torso açıkken gore OFF: yara gizlenmeli; aynı hasar/anatomi/ölüm sonucu korunmalı. ON geri açıldığında mevcut yara durumu geri gelebilir; kan burst'ü tekrar oynamamalı. Restore ve actor reuse'da eski görsel sızmamalı.
3. Kafa kopması: ilk kare tek güçlü burst; ilk .2 s basınç ve devamında okunur pulse, 2.1 s civarında spurt sonu, 2.2 s drip, en geç ~5.8 s slot temizliği. Fog görünümü, yanlış yön veya havada sabit emitter olmamalı. Ayrılmış baş bağımsız davranmalı.
4. Aynı ölüye tekrar vurun: ikinci sever spray oluşmamalı. Kanama sırasında gore OFF, actor disable, hücre çıkışı ve corpse cleanup deneyin; aktif efekt kalmamalı. Gore tekrar ON eski efekti oynatmamalı.
5. Zemin/yüzey izleri aynı bounded havuzda büyüyüp silinmeli; kenar taşıması ve büyük kart görünümünü kontrol edin. Surface mark reddi nedeniyle her kopmada iz oluşması garanti değildir.
6. Önce bir, ardından 10/20/30 zombi: art arda kopma, kafa→el/kol ve gore toggle. Profiler'da warmup sonrası GC, ParticleSystem CPU, transparent overdraw, materyal sayısı ve 16 slot FIFO kesilmelerini kaydedin. Ölçülmüş FPS/GC hedefi iddia edilmedi.

Geri gönderilecekler: iki koşunun `results.xml`, `unity.log`, `exit-code.txt`; başarısız assertion/stack trace; aynı yara için gündüz/gece/fener önden-yandan görüntüler; kafa kopmasından cleanup'a 6–8 s video; kullanılan kalite/gore ayarı ve zombi sayısı. Çoklu kabul yapıldıysa profiler capture/ölçüm ve görsel gözlem de ekleyin.

## Dosyalar ve açık sınırlar

9 kod/asset dosyası değişti: StudioNewPunchVisualAuthoring.cs; ZombieBloodVfxProfile.cs; ZombieBloodVfxPool.cs; ZombieBloodVfxTests.cs; ZombieBloodVfxPlayTests.cs; ProjectOwned/ZombieBlood.asset; LS_Zombie_Runtime.prefab; M_Zombie_TorsoWound_URP.mat; SM_Zombie_TorsoWound_Quad.asset. Tam yollar `changed-files.txt` içinde. Bu rapor ve test scripti eklendi; önceki kan devrine devam bağlantısı eklendi.

Yara gerçek oyuk/geometri kesimi değildir; kaynak gövdeyi izleyen alpha yüzeyidir. En güçlü dört ağırlığa yaklaşım ve sınırlı grid, aşırı pozlarda/Low skinning'de klip yapabilir: manuel kabul kapısıdır. Ayrı normal/roughness texture üretilmedi. Yeni materyalin gündüz/gece/fener görünümü render ile onaylanmadı. Dünya/save tam regresyonu ve 30-zombi performansı kullanıcı kanıtı bekler.

Sonraki adım: ilk iki odaklı koşu ve manuel görsel kanıt; dönen hatalara minimal düzeltme ve sonraki gerekli komutlar. **PRODUCTION ACCEPTED değildir.**

## Kullanıcı doğrulaması — 2026-10-02 13:30–13:31

Kullanıcının çalıştırdığı iki koşunun yerel XML dosyaları ve Unity logları incelendi:

| Koşu | Kanıt klasörü | Sonuç | Test süresi |
| --- | --- | --- | --- |
| EditMode | `Evidence/20261002-WoundHead-131646/user-20261002-133036-edit-6q4COD/` | 9/9 Passed, failed/skipped/inconclusive 0 | 0.164 s |
| PlayMode | `Evidence/20261002-WoundHead-131646/user-20261002-133120-play-mB7bCw/` | 14/14 Passed, failed/skipped/inconclusive 0 | 10.376 s |

Her klasörde `results.xml`, `unity.log`, `exit-code.txt` mevcut. Her iki çıkış kodu 0; karar yalnız çıkış koduna değil XML test-case sonuçlarına dayanır. Beklenen 9 EditMode ve 10 kan + 4 dismemberment PlayMode testi çalıştı. Yeni wound binding/quality, head burst/decay/stump/expiry ve head→hand pool reset testleri geçti.

EditMode logundaki `InvalidOperationException: Expected blood presentation failure`, `PresentationExceptionCannotPreventFatalHealthCommit` testinin kasıtlı ve LogAssert ile beklenen hatasıdır; regresyon değildir. Loglarda lisans/entitlement, boş assembly ve editör/işletim sistemi kapanış uyarıları mevcut. İncelenen çıktılarda C# veya shader derleme hatası, beklenmeyen gameplay exception ya da native crash göstergesi bulunmadı.

Bu döngüde kod/asset düzeltmesi veya yeniden test çalıştırma gerekmedi; yalnız doğrulama kaydı güncellendi. Önceki “testler çalıştırılmadı” ifadeleri uygulama oturumunun tarihsel durumudur; bu bölüm kullanıcı tarafından tamamlanan odaklı doğrulamayı kaydeder.

**Odaklı otomatik doğrulama tamamlandı; manuel görsel ve çoklu zombi performans kabulü bekliyor. PRODUCTION ACCEPTED değildir.** Sonraki adım: gündüz/gece/fener, önden/yandan ve hareket/ölüm pozlarında yara görüntüleri; gore ON/OFF; kafa kopmasından cleanup'a 6–8 s video; ardından 10/20/30 zombi gözlemleri. Aynı testleri değişiklik veya yeni hata olmadan tekrar çalıştırmak gerekmiyor.
