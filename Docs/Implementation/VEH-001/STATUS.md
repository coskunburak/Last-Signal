# VEH-001 — IN_PROGRESS

## Driving closure — 2026-10-06

Yeni kullanıcı talebine göre bu kapanışın sürüş kabul kapsamı **resident alan**. Seamless portal sürüşü bu kapanışın dışında; destek iddiası yok. Önceki tam PlayMode kanıtı bulundu ve XML'den doğrulandı: 241/241 PASS. Yeni çalışma bu kontrol noktasını korur.

MotionCore dört teker için tek `GetWorldPose` yazıcısı olarak korundu. Gerçek FL/FR açılarından ±270° cockpit direksiyonu, ölçülmüş pivot, semantik fren/geri lambaları, Nox yük/rolling katmanları, MotionCore yüzey sağlayıcısı ve resident gravel/kaldırım rotası eklendi. Göz/koltuk hizası, bakış sınırları ve near-clip geçişi düzeltildi. Vendor assetler değiştirilmedi. `DRIVING.md` geometri, tuning, kararlar ve sınırları; `MANUAL-DRIVING-ACCEPTANCE.md` build kontrol listesini içerir.

Unity import/authoring komutları derlendi ve statik geometri/pivot görüntüleri incelendi. Yeni `VehiclePresentationMathTests` ve `VehiclePresentationTests` odaklı betiğe eklendi. **Yeni focused koşusu başarısız:** `Evidence/20261005T220705-186457Z-focused` içinde Foundation 24/24, Noise 6/6, Exit 5/5, Save 9/9, PresentationMath 7/7 ve Integration 6/6 PASS; Occupancy 5/6. Klavye sürüş testi 2 saniyede >0.3 m beklerken 0.000470742 m ölçtü. Presentation sınıfına henüz ulaşılmadı. Kök neden henüz doğrulanmadı; prefab karşılaştırmasında mevcut fizik ayarları ve teker bağlantıları değişmemiş. Aynı hareket eşiği korunarak nötr giriş ve sürüş sırasında input/engine/torque/brake/ground/grip ölçümleri test hata çıktısına eklendi. Sonraki komut: `python3 Tools/veh001-verify.py focused --test-class VehicleOccupancyTests`. Yeni revizyon için full regresyon/build/performance henüz çalıştırılmadı. Bu revizyon için sıra: focused → edit → play → build → performance → manuel görünür kabul. Sonuçları kullanıcı çalıştırıp paylaşacak.

## Çalışma protokolü

2026-10-05: Burak test/build komutlarını kendi çalıştırıp sonuçları paylaşacak. Agent kod düzeltmelerini yapıp bir sonraki komutu verecek. Son değişikliklerden sonra agent yeni test/build başlatmadı.

## Korunan kanıt

Entry: Evidence/20261005T135440Z-entry, EditMode444/444, PlayMode229/229, macOS build0hata/378uyarı. Eski kanıtlar değiştirilmedi. S013/S014 mevcut değişiklikleri korunuyor.

Önceki doğrulamalar (son çalışma ağacının tamamı için final kabul değildir):

| Koşu | Sonuç | S012 Evidence kaynağı |
|---|---|---|
| VehicleFoundationTests | 24/24 PASS | 20261005T135808-070755Z |
| VehicleIntegrationTests | 6/6 PASS | 20261005T144647-880613Z |
| VehicleNoiseTests | 6/6 PASS | 20261005T144728-799216Z |
| VehicleExitTests | 5/5 PASS | 20261005T144807-037611Z |
| VehicleSaveTests | 9/9 PASS | 20261005T151256-504413Z |
| VehicleOccupancyTests genişletilmiş | 6/6 PASS | 20261005T151324-120318Z |

Gates1-2 kopyaları Evidence/20261005T150350Z-gates1-2 içinde. Başarısız ara koşular da korundu. Eski prefix filtresiyle 0 test çalışması kabul sayılmaz.

## Kullanıcı tarafından çalıştırılan son odaklı kapı

2026-10-05 15:22–15:24 UTC: `python3 Tools/veh001-verify.py focused` 56/56 PASS. Alt sonuçlar 24+6+5+9 EditMode ve 6+6 PlayMode. XML ile summary doğrulandı; kanıt `Evidence/20261005T152235-806359Z-focused/`. Bu sonuç tam EditMode, tam PlayMode veya Development build yerine geçmez.

## Kullanıcı tarafından çalıştırılan tam EditMode

2026-10-05 15:26 UTC: `python3 Tools/veh001-verify.py edit` 488/488 PASS; XML `Evidence/20261005T152630-131934Z-edit/edit/edit.xml`.

## Kullanıcı tarafından çalıştırılan tam PlayMode

2026-10-05 15:28–15:39 UTC: `python3 Tools/veh001-verify.py play` 241/241 PASS, 633.539 s; XML `Evidence/20261005T152812-451274Z-play/play/play.xml`.

## Kullanıcı tarafından çalıştırılan production build ve uyarı incelemesi

2026-10-05 15:40 UTC: `python3 Tools/veh001-verify.py build` PASS; 0 hata, 384 uyarı, 28.352s, `Assets/LastSignal/Scenes/Production/S013Cabin.unity`. Kanıt `Evidence/20261005T154005-364093Z-build`. Giriş tabanı 0 hata/378 uyarıydı. Kaynakta `VehicleActor.cs` iki çarpışma yolunda toplam altı eski `EntityId.GetRawData()` çağrısı bulundu; Unity günlüğünde CS0618. Bunlar `EntityId.ToULong(...)` ile değiştirildi.

## İkinci build ve karşılaştırılabilirlik

2026-10-05 15:43 UTC: `python3 Tools/veh001-verify.py build` PASS, 0 hata/18 uyarı, `Evidence/20261005T154328-132731Z-build`. Bu incremental build'deki 18 sayı, girişteki 378 veya ilk production 384 ile doğrudan karşılaştırılabilir değildir; Unity derleme önbelleği farklıdır. Logda yeni `VehicleActor` GetRawData uyarısı görünmedi.

## Temiz Development build

2026-10-05 15:47 UTC: `python3 Tools/veh001-verify.py build --clean-cache` PASS, 0 hata/391 uyarı, `Evidence/20261005T154710-815578Z-build-clean`. `WARNINGS.md` 391 mesajın kategorilerini ve 378 giriş sayısıyla karşılaştırma sınırını kaydeder. Araç kaynaklarında uyarı yok. Temiz build bu raporun sonradan eklenen performans capture kodundan **önce** alındı.

## Performans ölçüm hazırlığı

Gerçek MotionCore FixedUpdate, VehicleActor Update ve gürültü köprüsüne ProfilerMarker eklendi. Opt-in Development player `VehiclePerformanceCapture` üretim kabininde 50 park halinde bin/in, 50 oturum spawn/end döngüsü ve 5 s ısınma + 20 s klavye sürüşünü ölçmek üzere hazırlandı; `Tools/veh001-performance.py` çıktıyı yeni VEH evidence klasörüne yazar. **Henüz kullanıcı tarafından çalıştırılmadı; sonuç PASS sayılmaz.** GC sayacı bütün karenin tahsisidir, yalnızca araç koduna atfedilmez. Oturum yaşam döngüsü cell streaming'in yerine geçmez. Portal geçişi 0'dır; mevcut dünya mimarisinde sürüşle seamless cell geçişi bu testin kapsamı değildir.

## Mevcut kod

- Ayrı ServiceBrake/Reverse, motor kapalı tork koruması, gerçek MotionCore sınırı.
- Vehicle map: mevcut Player/UI GUID ve binding sırası korunarak eklendi; klavye/gamepad; nötrleme, pause/focus.
- VehicleActor/VehicleWorld: aynı oyuncuyla binme/inme, capsule/motor/stance kapatma, cockpit, silah holster/geri alma, yakıt/hasar/gürültü.
- Bagaj/yakıt servis noktaları: mevcut InteractionController ve InventoryContainer işlemleri.
- Additive vehicleVersion1: pose/yakıt/kondisyon/bagaj/ışık/occupancy; yüklemede motor kapalı; araç gövdesi ve zemin doğrulaması. Kritik sigorta bagaj sahipliği sayımına dahil.
- Resident pressure: mevcut WorldPopulationManager içinde decay/receipt/persistence. Yerel AI aynı GameplayNoiseSystem kullanır. Resident için yeni nüfus materialization/migration eklenmedi.
- Nox sunum bileşeni, farlar ve kabin yağmur koruması. Araç yatak/shelter hakkı vermez.
- Production S013Cabin Session köküne VehicleWorld referansı eklendi. Spawn(-340,0.2,-125), yaw90. Production build başarılı; oynanabilir rota ve performans kullanıcı koşusu bekliyor.

## Açık kapılar

Odaklı testler 56/56, tam EditMode 488/488, tam PlayMode 241/241 ve temiz Development build PASS; ancak bunlar en son eklenen sürüş/sunum kaynaklarından önceki kanıtlardır. Bu revizyon için focused, tam EditMode/PlayMode ve yeni build gerekir. Açık kabul: gerçek gamepad konforu, collision/zombie impact fizik kabulü, eğim/dirt/gravel sürüş benchmarkı, performans/soak, gece/yağmur/görsel/handling insan kabulü. Seamless cell sürüşü uygulanmış değildir; 2026-10-06 kullanıcı kararıyla bu driving closure kapsamının dışındadır.

Ses: starter cue anahtar sesi; impact geçici Nox trunk-impact kaydıdır. Idle ve yük kayıtları artık teker RPM/semantik yük sunumuyla harmanlanır; gerçek motor RPM/şanzıman simülasyonu değildir. Bunlar sunum kabulü gerektirir.

Ekran görüntüleri: Evidence/20261005T151430Z-visual. Cockpit incelendi. Exterior ağaca takılmış; benchmark-exterior additive sahnedeki resident duvarını da gösterir. Bu iki dış görüntü görsel kabul kanıtı değildir.

## Occupancy tanı tekrarı — 2026-10-05 22:24 UTC

Kullanıcının çalıştırdığı `focused --test-class VehicleOccupancyTests`: 6/6 PASS; XML doğrulandı. Kanıt: `Evidence/20261005T222415-963746Z-focused/`. Üretim sürüş kodu değiştirilmedi; testte aynı >0.3 m hareket eşiği korunarak 2 saniyelik bekleme dört adet 0.5 saniyelik tanı örneklemesine bölündü. Önceki hareket hatasının kök nedeni doğrulanmış değildir; bu tek başarılı tekrar kalıcı düzeltme kanıtı sayılmaz. Sonraki kapı `VehiclePresentationTests`, ardından bütün focused dizisi ve tam regresyon.

## Presentation doğrulaması — 2026-10-05 22:27 UTC

Kullanıcının çalıştırdığı `focused --test-class VehiclePresentationTests`: 5/5 PASS; XML doğrulandı. Kanıt: `Evidence/20261005T222708-933910Z-focused/`. Sonraki adım tüm focused sınıflarını birlikte sıralı çalıştırmak; önceki Occupancy hareket hatasının kök nedeni hâlâ doğrulanmadı. Bu sonuç manuel görsel kabul, tam regresyon veya build/performance kabulü yerine geçmez.

## Tekrarlanan focused hatası ve test giriş izolasyonu

`Evidence/20261005T222818-602075Z-focused/`: Occupancy tekrar 5/6; diğer çalıştırılan altı sınıf PASS. XML doğrulandı. Tanı örneklerinde Ready/Driving/Engine true, dört teker grounded ve grip=1; sanal klavye W=false, throttle=0, motorTorque=0. Sorun hareket eşiğinden önce input olayının player güncellemesine ulaşmamasıdır. Kurulu Input System `InputManager.cs` unfocused Game View durumunda klavye olaylarını Editor güncellemesine yönlendiriyor. Test artık geçici InputSettings kopyasında AllDeviceInputAlwaysGoesToGameView + IgnoreFocus ve geçici runInBackground kullanıyor; finally içinde özgün ayarlar geri yükleniyor. Üretim focus/pause politikası değiştirilmedi. Ayrıca her sürüş örneğinde W ve throttle, resume sırasında W hâlâ basılı kontrol ediliyor; hareket eşiği >0.3 m korunuyor. Düzeltmenin kullanıcı koşusuyla doğrulanması bekleniyor: tam `focused`.

## Tam focused doğrulaması — 2026-10-05 22:34–22:36 UTC

Input test izolasyonu sonrası kullanıcı koşusu 68/68 PASS: Foundation 24, Noise 6, Exit 5, Save 9, PresentationMath 7, Integration 6, Occupancy 6, Presentation 5. Sekiz XML doğrulandı; kanıt `Evidence/20261005T223414-381595Z-focused/`. Önceki hata veren tam sıralı koşu bu revizyonda geçti; W/throttle ve resume sırasında held-W kontrolleri de başarılı. Sonraki kapı tam EditMode, ardından tam PlayMode, build/performance ve manuel kabul. Üretim kabul durumu IN_PROGRESS.

## Tam EditMode doğrulaması — 2026-10-05 22:41 UTC

Kullanıcı koşusu 495/495 PASS; XML doğrulandı. Kanıt: `Evidence/20261005T224103-185630Z-edit/edit/edit.xml`. Sürüş/sunum revizyonu sonrası tam EditMode kapısı geçti. Sonraki adım tam PlayMode; build, performance ve manuel kabul bekleniyor.

## Tam PlayMode kesintisi — 2026-10-05 22:43–22:57 UTC

Kullanıcı koşusu `S012/Evidence/20261005T224306-002944Z` XML üretemeden Test Runner hatasıyla sonlandı. Editor log: `Playmode tests were aborted because the player was stopped.` Play Mode durdurulmasının kim/ne tarafından tetiklendiği belirlenmedi; test assertion başarısızlığı olarak sınıflandırılmadı ve PASS sayılmadı. Runner cleanup ve production sahnesine dönüş logda mevcut. Hata alıntısı `VEH-001/Evidence/20261005T224306-002944Z-play-aborted/runner-error-excerpt.txt` içinde korundu. Yeniden tam PlayMode doğrulaması gerekli.

## Tam PlayMode doğrulaması — 2026-10-05 23:00–23:11 UTC

Kesilen koşudan sonraki kullanıcı koşusu 246/246 PASS; XML doğrulandı. Kanıt: `Evidence/20261005T225957-431996Z-play/play/play.xml`. Süre 654.93 saniye. Bu revizyonda focused 68/68, tam EditMode 495/495 ve tam PlayMode 246/246 başarılı. Sonraki adım temiz önbellekli production build (`python3 Tools/veh001-verify.py build --clean-cache`), ardından performance ve manuel sürüş/sunum kabulü. Durum IN_PROGRESS.

## Temiz build doğrulaması — 2026-10-05 23:22 UTC

Kullanıcı build koşusu Succeeded, 0 hata, 396 uyarı; rapor doğrulandı. Kanıt `Evidence/20261005T232231-982273Z-build-clean/build/`; build `Builds/S013/20261005T232232-003098Z-production`. Uyarılarda Vehicles/MotionCore/Vehicle Entegrations yolu bulunmuyor. Sonraki adım aynı build üzerinde `Tools/veh001-performance.py`, ardından manuel sürüş/sunum kabulü. Performans ve görsel kabul henüz doğrulanmadı.

## Development capture — 2026-10-05 23:25 UTC

`Evidence/20261005T232538-234824Z-performance`: PASS capture completed, Errors=0. 11193 frames, frame p95 2.0565 ms, p99 2.1792 ms, distance 149.152 m, max speed 18.046 m/s. 50 enter/exit and 50 session cycles completed; recorded vehicle/noise-listener/cell-listener counts remained 1/1/1. This does not establish zero memory leaks. Allocated memory increased 107312 bytes during drive; GC allocated/frame mean 3485 bytes, collection deltas 51/51/51, Main Thread maximum 224.554292 ms. Capture lists grow beyond initial 4000 capacity and may contribute allocation spikes; measurements are whole-player and cannot attribute allocations to vehicle code alone. No zero-GC or hitch-free claim. Active AI was 0 throughout; AI-load acceptance remains unverified. No portal transitions (outside resident scope). Performance budget acceptance remains separate; next independent step is manual driving/presentation acceptance on the same build.

## VEH-ZMB continuation — 2026-10-06 son durum

**IN_PROGRESS.** Araç/enfekte 24/24, VEH focused 92/92, EditMode 505/505 tamamlandı. Tam PlayMode runner hatasıyla XML üretmeden durdu; sonraki build başlamadı. Bu yeşil sonuçlardan sonra temas başına açısal hareket düzeltmesi ve development capture kontrolleri eklendi, yeniden test bekliyor. Büyük test/build komutlarını Burak çalıştıracak. Yeni build, aktif-AI performance ve görünür kabul henüz yok. Ayrıntı, kanıt yolları ve komutlar: [VEHICLE_INFECTED_IMPACT.md](VEHICLE_INFECTED_IMPACT.md). Önceki bölümler tarihsel kontrol noktalarıdır.

## Kullanıcı koşusu — 2026-10-06 09:23–09:39 UTC

EditMode 506/506 PASS (`Evidence/20261006T092600-583854Z-edit`); PlayMode 259/260 FAIL (`Evidence/20261006T092614-393783Z-play`). Zombie/focused koşuları domain 11/11 ve integration 12/13 sonrasında durdu; tüm grubun PASS sonucu yok. Üç koşudaki tek başarısız test `ActiveSurvivorReactsAndWitnessHearsThroughCanonicalNoise`: fiziksel işlem ve HitReact geçiyor, witness Heard=0. Kök neden henüz bilinmiyor; testte eşik, mesafe, engellenme, aday/physics query sayısı ve listener durum tanısı eklendi. Üretim tuning'i değiştirilmedi.

Clean build PASS: `Builds/S013/20261006T093817-504014Z-production`, 0 hata, 382 uyarı, 38.589 s. Bu build test başarısızlığını kapatmaz. Sonraki komut yalnız `python3 Tools/veh001-verify.py focused --test-class VehicleZombieIntegrationTests`. Aktif-AI performance ve manuel kabul bekleniyor. **VEH-ZMB-001 IN_PROGRESS.**

## İşitme fixture tanısı — 2026-10-06 09:42 UTC

`Evidence/20261006T094145-898249Z-focused` integration 12/13. Olay dağıtıldı (registered/canHear=true, paused=false, candidates=1, queries=1). Pickup ışını engelledi; clear=0.1591946, occluded=0.06367783, threshold=0.12. Bu engelli geometride Heard=0 doğru davranış. Açık-hat fixture tanığı ileri/yan konuma alındı ve açık hat/eşik önkoşulları eklendi. Eski geometri ayrı `ChassisOcclusionRejectsQuietImpactButPreservesDamageAndPressure` testine korunarak rejection + damage + pressure doğrulanır. Üretim ses/occlusion tuning'i değişmedi. Yeni integration sınıfı 14 test; henüz koşulmadı.

## Temas yüzeyi kaynak düzeltmesi — 2026-10-06 09:51 UTC

Önceki “gerçek gövde engeli” yorumu eksikti. Açık tanık hattında ray mesafesi=5.685596 m, şasi hit mesafesi=5.670175 m: kaynak son 1.5 cm içinde şasiye gömülüydü. Fizik penetration noktası yayıcıyı kendi kendine occlude ediyordu. VehicleActor.Impact artık enfekte impact gürültüsü konumunu temas colliderının dış yüzeyine Collider.Raycast ile projekte edip outward yönde 2 cm dışarı alır. Hasar/receipt temas noktası, noise intensity/radius, eşik ve dünya occlusion maskesi korunur. İşlem başına tek collider raycast vardır; ölçüm maliyeti yeni build performance ile doğrulanacak.

Açık witness local (2,0,9); gerçek gövde arkasındaki witness (2,0,-2). Açık test 1/1 PASS, engelli test 1/1 PASS. Kanıt `Evidence/20261006T095139Z-hearing-surface-fix` (S012 kaynakları 20261006T094940-374484Z ve 20261006T095109-077557Z). Açık durumda Heard=1, Accepted=1; engelli durumda Heard=0 ve fiziksel damage/World Pressure devam eder. Önceki başarısız tanı koşuları korunur. Sınıfın tamamı, regresyon ve yeni build henüz tekrar koşulmadı. Önceki 382 uyarılı build bu runtime düzeltmesinden öncedir. Burak'ın sıradaki komutu: `python3 Tools/veh001-verify.py focused --test-class VehicleZombieIntegrationTests`. **IN_PROGRESS**.

## Kullanıcı tarafından doğrulanan integration — 2026-10-06 09:53 UTC

`VehicleZombieIntegrationTests` 14/14 PASS, 47.755 s. XML doğrulandı: `Evidence/20261006T095216-972390Z-focused/VehicleZombieIntegrationTests/play.xml`. Temas yüzeyi noise source düzeltmesinden sonraki tam integration sınıfı başarılı. Yeni full VEH focused, full PlayMode ve clean build bekleniyor; son runtime düzeltmesi için eski build final kanıt değildir. EditMode 506/506 önceki kanıt olarak korunur; son runtime helper değişikliğinden sonra focused domain kapısı yeniden doğrulanacaktır. Aktif-AI performance ve manuel kabul bekleniyor. **VEH-ZMB-001 IN_PROGRESS**.

## Kullanıcı tarafından doğrulanan VEH focused — 2026-10-06 09:59 UTC

11 sınıfın XML sonuçları doğrulandı: 94/94 PASS. İçindeki VEH-ZMB 26/26 (11 domain + 14 integration + 1 hearing). Kanıt `Evidence/20261006T095426-881776Z-focused`. Bu sonuç temas yüzeyi noise source düzeltmesini içerir. Sonraki kapı full PlayMode; PASS sonrası clean build, aynı build üzerinde paired active-AI performance ve görünür manuel kabul. Full EditMode 506/506 önceki kanıtı korunur. **VEH-ZMB-001 IN_PROGRESS**.

## Full PlayMode ve clean build — 2026-10-06 10:15 UTC

Kullanıcı koşuları disk kanıtından doğrulandı: full PlayMode 261/261 PASS, 703.049 s, `Evidence/20261006T100221-512598Z-play`. Clean Development build PASS, 0 hata / 378 uyarı, 36.119 s, `Evidence/20261006T101512-065645Z-build-clean`; player `Builds/S013/20261006T101512-092212Z-production`. Son temas yüzeyi noise source düzeltmesi bu build'dedir. Uyarı sayısındaki azalma tek başına uyarı çözümü kanıtı değildir; içerik karşılaştırması ayrıca gerekir. Sonraki kapılar aynı build üzerinde baseline ve active-infected capture, ardından görünür manuel kabul. **VEH-ZMB-001 IN_PROGRESS**.

## Performance koşuları ve 8/9 kamera tuşları — 2026-10-06

Baseline `Evidence/20261006T101624-405455Z-performance`: capture PASS; avg 1.7948 ms, p95 2.1455, p99 2.5754, GC frame mean 3313 B; active AI 0. Aktif `Evidence/20261006T101704-248397Z-performance`: FAIL (üç gerçek impact koşulu). Active AI min/max/end=5; avg 2.1201 ms, p95 2.6968, p99 3.2250, GC frame mean 3684 B; toplam impact=1, ölçüm içi impact=0. Bu koşu aktif AI varlığını gösterir ancak aktif impact performans kapanış kanıtı değildir. İki koşuda GC collection deltas sırasıyla 43/43/43 ve 40/40/40; 0 GC iddiası yok.

Capture fixture kurulumu hareketli 5 saniyelik warmup sonrasına taşındı; ilk temasın düşük hızda warmup'ta tüketilmesi önlenir. Aynı production prefab/AI/physics kullanılır; >=3 impact şartı artık ölçüm penceresindeki delta üzerinden kontrol edilir. Yeni capture henüz koşulmadı.

Kullanıcı ek talebi uygulandı: VehicleInspectionView üst sayı satırındaki digit8Key ile cockpit/exterior, digit9Key ile dört dış açı değiştirir. UI açıklaması ve manuel belgeler güncellendi. Bu kamera mevcut Development opt-in `-veh001Inspect` kapsamındadır. Yeni build gereklidir; önceki player F8/F9 içerir. **IN_PROGRESS**.

## Clean build — 2026-10-06 10:24 UTC

8/9 kamera kontrolü ve warmup sonrası fixture kurulumu içeren build PASS: `Builds/S013/20261006T102343-920192Z-production`, 0 hata, 405 uyarı, 32.799 s. Kanıt `Evidence/20261006T102343-895824Z-build-clean`. Bu player üzerinde yeni paired performance ve manuel 8/9 kabulü bekleniyor. **IN_PROGRESS**.

Uyarı karşılaştırması: toplam +27 satır, fakat yeni benzersiz mesaj yok; mevcut compiler uyarılarının ek tekrarları. Vehicle kaynaklarında compiler uyarısı yok.

## VEH-ZMB-002 continuation — 2026-10-06 23:06 Europe/Istanbul

**VEH-ZMB-002 IN_PROGRESS.** Branch/HEAD preserved: s012-integrated-graybox-slice / f9299ef69bb72abc92c05aff22ba0418d85b6af0. Active capture uses continuous forward Development control; normal capture retains the multi-segment route. Animation lifecycle guards completed. Latest VEH-ZMB group **35/35 PASS** (15+14+1+5), XML evidence `Evidence/20261006T200346-444548Z-zombie`. 100-impact soak cache_end=0 and listeners=1/1; Editor memory samples do not independently prove absence of leaks. Full regression and new build/performance pending; manual acceptance NOT_RUN. Implementation and evidence boundaries are recorded under Vehicle Impact Dominance in VEHICLE_INFECTED_IMPACT.md. Large test/build runs remain user-operated; next command `python3 Tools/veh001-verify.py focused`.

## VEH-ZMB-002 full VEH focused — 2026-10-06 23:14 Europe/Istanbul

User-run full VEH focused verified from all 12 XML files: **103/103 PASS**, including VEH-ZMB 35/35. Evidence: `Evidence/20261006T200908-365320Z-focused`. Next gates: full EditMode, full PlayMode, fresh clean build, normal and active-infected player performance, manual acceptance. **VEH-ZMB-002 IN_PROGRESS**.

## VEH-ZMB-002 full regression and build — 2026-10-06 23:30 Europe/Istanbul

User-run XML verified: full EditMode **510/510 PASS** (`Evidence/20261006T201600-514939Z-edit`, 11.704 s), full PlayMode **266/266 PASS** (`Evidence/20261006T201649-962855Z-play`, 713.740 s). Clean Development build **Succeeded**, 0 errors / 392 warnings, 35.753 s; evidence `Evidence/20261006T202922-228132Z-build-clean`; player `Builds/S013/20261006T202922-249575Z-production`. This is the new VEH-ZMB-002 build for both performance captures. Normal/active performance and manual visible acceptance remain pending. **VEH-ZMB-002 IN_PROGRESS**.


## VEH-ZMB-002 capture boundary correction — 2026-10-06 20:40 UTC

User-operated latest player captures verified on disk, using build `20261006T202922-249575Z-production`:

- Normal `Evidence/20261006T203336-067150Z-performance`: PASS, Errors=0; 11388 frames, average/p95/p99=1.7563/2.0640/2.1891 ms, distance=149.011 m, maximum speed=18.045 m/s, cache_end=0. GC collections=45/45/45, mean frame allocation=3312 B. Allocated memory start/end=160318849/160458961 B.
- Active `Evidence/20261006T203423-624126Z-performance`: FAIL, Errors=0; 11219 frames, average/p50/p95/p99=1.7827/1.7794/2.1212/2.2771 ms, distance=311.855 m, maximum speed=18.066 m/s. Active AI min/max/end=1/5/1; four real impacts during capture; cache_end=1. Physics.Simulate mean/max=10102/186875 ns normal and 10783/1054459 ns active. Active Vehicle.ZombieImpact mean/max=335/2684500 ns; AI=4710/1270709 ns; Perception=1957/251958 ns; Navigation=1186/601250 ns; Hearing=69/711791 ns. Active GC collections=49/49/49, mean frame allocation=3683 B; allocated memory start/end=161744805/162196646 B. Failed capture remains failed evidence, not closure evidence.

The original course regression drove only six measured seconds. Extending it to the full twenty seconds reproduced cache_end=1 with `East boundary (UnityEngine.BoxCollider)`, infected=False, car position=(-2.66,0.14,-141.92). Source evidence: `../S012/Evidence/20261006T203737-307516Z/play.xml`. The contact was a live world-wall contact, not a stale infected reference. Continuous forward throttle reached the resident boundary.

Active capture now drives forward for eight seconds, then brakes for the remaining twelve. Normal control segments are unchanged. No contact clearing, damage/AI change, or gate reduction was introduced. The development-only end report now describes remaining collider identities/bounds. The extended regression uses the same forward-duration constant and retains >=3 impacts, living AI>0, cache_end=0 assertions.

Corrected twenty-second course: **1/1 PASS**, 27.025 s including setup; four impacts, one living AI, zero contacts. Evidence: `../S012/Evidence/20261006T203909-320368Z/play.xml`. Compile and scoped diff whitespace check passed. Existing 103/103 focused, 510/510 EditMode and 266/266 PlayMode are the pre-route-correction full results; they were not rerun or relabeled. Fresh clean build and both player captures remain required. Manual acceptance NOT_RUN. **VEH-ZMB-002 IN_PROGRESS**.


## VEH-ZMB-002 bounded-route clean build — 2026-10-06 20:42 UTC

User-operated clean Development build verified from disk: `Builds/S013/20261006T204143-881929Z-production`, Result=Succeeded, Errors=0, Warnings=406, Duration=30.864413 s, Unity=6000.5.0f1, CleanCache=True. Evidence: `Evidence/20261006T204143-857693Z-build-clean`. Includes the eight-second forward / twelve-second brake active capture route and end-contact diagnostics. Warning-line set comparison against the previous 392-warning build found no new unique warning lines; the increased total is not a new unique warning finding. Normal and active-infected captures must now use this build. Manual acceptance NOT_RUN. **VEH-ZMB-002 IN_PROGRESS**.


## VEH-ZMB-002 final player captures — 2026-10-06 20:45 UTC

Both user-operated captures on `Builds/S013/20261006T204143-881929Z-production` verified from result.txt and drive.txt: **PASS, Errors=0**.

| Metric | Normal | Active infected |
| --- | --- | --- |
| Evidence | 20261006T204326-962309Z-performance | 20261006T204423-263719Z-performance |
| Frames | 11420 | 11355 |
| Average / p50 / p95 / p99 (ms) | 1.7513 / 1.7614 / 2.0829 / 2.2217 | 1.7614 / 1.7663 / 2.1030 / 2.2266 |
| Distance (m) / max speed (m/s) | 148.982 / 18.045 | 147.994 / 18.064 |
| Active AI min / max / end | 0 / 0 / 0 | 1 / 5 / 1 |
| Impacts total / during capture | 0 / 0 | 4 / 4 |
| Contact cache end | 0 | 0 |
| Allocated memory start / end (bytes) | 160344944 / 160450736 | 161824829 / 162046119 |
| Managed used end (bytes) | 9560064 | 9736192 |
| GC collections | 45 / 45 / 45 | 49 / 49 / 49 |
| Mean frame allocation (bytes) | 3312 | 3681 |
| Physics.Simulate mean / max (ns) | 11504 / 227166 | 9704 / 1294709 |

Active profiler mean/max (ns): Vehicle.ZombieImpact 314/2862209; Zombie.AI 4194/1183875; Zombie.Perception 1868/267209; Zombie.Navigation 765/577248; Zombie.Hearing 67/703125. Full vehicle marker data is retained in each evidence directory's drive.txt. Minimum up-dot=0.9981 in both runs; active maximum vertical speed=0.5675 m/s. Reserved memory end=292388864 B in both. No zero-GC or zero-leak claim. These captures validate the required impact/AI/cache gates; they do not independently establish a platform-wide performance budget. Compared with the prior failed active run, four impacts and one remaining active AI are preserved while the actual boundary contact is avoided. Different route segments prevent treating normal/active timing differences as a pure isolated AI cost.

Automated implementation evidence is complete with the full-regression and route-test scope recorded above. **IMPLEMENTATION COMPLETE — AWAITING MANUAL VEH-ZMB-002 ACCEPTANCE.** Manual acceptance NOT_RUN; **VEH-ZMB-002 IN_PROGRESS**, not CLOSED.

Manual launch:
```bash
open -n "/Users/burakcoskun/Last Signal/Builds/S013/20261006T204143-881929Z-production/LastSignal.app" --args -veh001Inspect -veh001ZombieInspect
```
Check parked pickup dominance, interruption/fall/get-up, death override, momentum through one infected and the small group, corpse run-over without launch/duplicate kills, canonical hearing, and camera keys 8/9.

Repository housekeeping: added local IDE, Python environment/cache, root .env, scratch and historical-backup ignore rules; Assets, Packages, ProjectSettings and canonical Docs evidence remain eligible for versioning. Validated 22 representative ignored/retained paths with git check-ignore --no-index. Existing tracked scratch/backup files require a separate index-only removal; no staging, commit, push or deletion of their local contents was performed by the assistant.
