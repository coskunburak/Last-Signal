# VEH-ZMB-001 — Vehicle / infected impact

Durum: **IN_PROGRESS**. Otomatik kapılar, build ve aktif-AI ölçümü aşağıda ayrı kaydedilir. Burak'ın görünür kabulü olmadan CLOSED değildir.

## Architecture

**KEEP:** MotionCore, input/occupancy, VehicleImpactGate, VehicleResources, GameplayNoiseSystem, ZombieHealth, mevcut NavMesh/hit/death, population ve SaveSession.

**EXTEND:** VehicleActor fizik adaptörü, kopyalanan VehicleTuning.zombieImpact, VehicleZombieImpactReceipt, Development inceleme/performance ve test grupları.

**FIX:** yalnız hız büyüklüğünden hasar; enfekteye duvar maliyeti; ölü hedef yan etkileri; kaybolan collider referansları; zombie işitmesinin araç kategorilerini reddetmesi; arka servis kutusunun şasiden önce fiziksel engel olması.

Yetki zinciri: `VehicleActor.OnCollisionEnter → ZombieHealth.TakeDamage → Damaged/Died → ZombieController hit/death → mevcut encounter/population persistence`. Sağlık işlemi gerçekten kabul edilirse tek araç kondisyon maliyeti ve tek GameplayNoiseSystem olayı üretilir. İşitme ve resident World Pressure aynı olayın ayrı aboneleridir. Audio, kabul edilmiş receipt üzerinden çalışır. Doğrudan AI alert, yeni loot/score veya araç öldürmelerine özel kayıt listesi yoktur.

## Physics / target identity

Hasar yetkisi VehicleActor kökündeki iki fiziksel şasi kutusundadır. Trigger, WheelCollider, FuelAnchor, TrunkAnchor ve CabinRainRoof temasları hasar sahibi değildir. FuelAnchor/TrunkAnchor colliderlarının yerel excludeLayers maskesi 2/8 katmanlarını dışlar: arka servis kutusu enfekteyi şasiden önce durduramaz; mevcut non-trigger etkileşim raycast'i korunur ve test edilir. Genel collision matrix, WheelCollider süspansiyonu ve vendor kaynakları değişmedi.

Production zombie fizik kapsülü layer 2, radius .32 m, height 1.81 m; silah bölgesi kutuları layer 8'de ve mevcut fizik matrisinde dışlanmış durumdadır. Rigidbody mode/timestep değiştirilmedi; gerçek 18 m/s prefab testi ve production aktif-AI çarpışmasıyla mevcut fizik yolu doğrulanır. Geniş yüzey/hız kombinasyonlarının görünür kabulü ayrıdır.

FixedUpdate'ta solver öncesi Rigidbody doğrusal/açısal hareketi alınır. Şasi temas noktasının hareketi `linear + cross(angular, point - centerOfMass)`; yaşayan hedef hareketi ZombieNavigation velocity üzerinden çıkarılır. Gerektiğinde hedef Rigidbody point velocity kullanılır. Normal alıcı şasiye doğrudur; `max(0, dot(relativeMotion, -normal))` kapanma hızını verir. Aynı hedef manifoldunun en güçlü normal bileşeni seçilir. Gerçek ileri/geri testleri yönü doğrular. Motor açık/kapalı veya sürücü var/yok koşulu hasar yetkisi değildir.

## Dedupe / lifecycle

Hedef kimliği hitbox değil ZombieHealth EntityId'dir. Sabit 128 girişli fizik collider tablosu, korunan 64 girişli VehicleImpactGate'e bağlanır. Ayrılma ve 1 simülasyon saniyesi cooldown gerekir; OnCollisionStay hiçbir hasar işlemi üretmez. Ayrı hedefler bağımsızdır, global horde throttle yoktur.

Exit teması bırakır; ölüm/despawn/disabled collider FixedUpdate'ta temizlenir. Araç disable ve restore iki tabloyu birlikte temizler. Kapasite dolduğunda canlı temas atılmaz, yeni işlem reddedilir. Sağlığın reddettiği işlem maliyet/gürültü üretmez. Ölü hedef yeni semantik işlem üretemez. Pause, hydration ve hazır olmayan oturumda hasar engellenir. Disable edilmiş MonoBehaviour'e gelebilen Unity callback'i ayrıca reddedilir.

## Tuning / reaction / presentation

VehicleTuning.zombieImpact, konfigürasyonla birlikte derin kopyalanır. Başlangıç tuning'i:

| Normal kapanma hızı | Hasar | Kondisyon maliyeti |
|---|---:|---:|
| ≤2.5 m/s | 0 | 0 |
| 8 m/s ideal doğrusal temas | 49.68 | 1.24 yüzde puanı |
| ≥18 m/s | en fazla 140 | en fazla 3.5 yüzde puanı |

Bu değerler 18 m/s ölçülmüş pickup aralığına göre başlangıç ayarıdır; insan sürüş hissi kabulü beklenir. Aynı hızdaki mevcut duvar maliyeti 22.5 yüzde puanıdır. Tek enfekte aracı yok etmez; tekrarlanan güçlü vuruşlar kondisyonu tüketir. Daha güçlü düşmanın sağlığı otorite olmaya devam eder; hız eşiğinde doğrudan Kill çağrısı yoktur.

Mevcut HitReact, yönlü tepki ve death animation kullanılır. Yaşayan araç-vuruşu hedefi mevcut HitReact içinde yatay NavMeshAgent.Move ile en fazla 1.2 m / .25 s geri itilir; NavMesh kenarında hareket kırpılır. AI tepki sonrasında aynı yetkiyle devam eder. Rakip Rigidbody/ragdoll, dikey fırlatma ve hit-stop yoktur. Torso hasarı mevcut wound sunumunu kullanabilir. Yeni gore/particle/decal sistemi eklenmedi.

VehicleAudioPresenter mevcut Nox impact kaydını şiddete göre ses düzeyiyle çalar. Bu clip'in duyusal uygunluğu manuel kabul konusudur; mixer/audio volume işitme yarıçapını değiştirmez.

İkincil wheel-crush işlemi yoktur. İlk şasi epizodu otoritedir. Mevcut ölüm owned colliderları kapatır; ceset geçişi hasar/gürültü tekrarına dönüşmez. Ragdoll sonraki bir sunum iyileştirmesi olabilir, bu kapanışın gereği değildir.

## Hearing / World Pressure

Var olan VehicleEngine, VehicleHorn ve VehicleImpact kategorileri ZombieHearingEvaluator'dan geçer; sırasıyla mevcut footstep/gunshot/melee duyarlılık ayarlarını kullanır. VehicleDoor kategorisi yoktur; kapı yalnız mevcut audio sunumudur. Motor cadence ve korna rate gate korunur.

Impact yarıçapı mevcut 48 m profilinin .25–1 katsayısıyla, intensity ise şiddetle ölçeklenir. Bir olay normal listener ve WorldPressureNoiseAdapter'a gider. İşitmeden ikinci pressure yazımı yapılmaz. Canonical pressure receipt'in mevcut string tahsisi korunur; bütün işlem için ölçmeden 0 B iddiası yapılmaz.

## Population / persistence

Production resident encounter'ın EnemySnapshot health/anatomy yolu korunur. Gerçek araç ölümü + kondisyon kaybı + disk save + menu + load entegrasyon testi vardır. WorldPopulationManager sahipliğindeki hedefler aynı Died olayını ve mevcut ledger yolunu kullanır; yeni resident materialization eklenmez. Encounter persistence ile population ledger kapsamları birbirinin yerine sunulmaz.

Development inceleme zombileri geçici fixture'dır; kalıcı düşmanlara dönüşmez. Mevcut corpse/loot/kritik-item sahipliği değiştirilmez. Save/load görünür kabulü normal kalıcı encounter üzerinde yapılır.

## Verification scope

Tarihsel 68/68 VEH, 495/495 EditMode, 246/246 PlayMode ve 0 hata/396 uyarılı build kanıtları değişmezdir. Yeni sonuçlar timestamped Evidence klasörlerinde tutulur; ilk 0-test discovery ve başarısız ara koşular silinmez.

İzole prefab fizik testleri uzak test şeridinde AI ve tekerleri kapatır; ayrı S013 production testleri gerçek AI/NavMesh/teker/işitme/ölüm ve disk persistence yolunu korur. İlk fixture hatası Transform-only konumlandırmaydı; Rigidbody.position/rotation ve Physics.SyncTransforms ile düzeltildi.

100 epizot soak cache/listener ve GC sonrası bellek örneklerini kaydeder. Instantiate/destroy fixture belleği resolver'ın tek-vuruş allocation ölçümü değildir; tek başına sızıntısızlık kanıtı sayılmaz. Build capture bütün player GC/Physics/vehicle ve mevcut AI markerlarını ölçer. Önceki yalnız population.PhysicalCount sayacı encounter/fixture zombilerini dışarıda bırakıyordu; yeni sayaç cached ZombieController dizisinden aktif, canlı, pause edilmemiş ve işitmeye hazır aktörleri sayar.

## Manual acceptance

**NOT_RUN.** Son Development build `-veh001ZombieInspect -veh001Inspect` ile açılır. Mevcut resident NavMesh üzerinde beş gerçek prefab: A düşük hız, B orta, C yüksek, D glancing/reverse, E işitme tanığı. AI aktiftir; ses duyunca yer değiştirebilirler. 8 dış kamera, 9 açı. Fixture'lar transient olduğundan save/load için kalıcı encounter kullanılır.

Kontrol listesi: park/çok düşük hız; orta; yüksek; geri; sıyırma; ceset geçişi; üç hedef grubu; yakındaki işitme; motor/korna; kalıcı encounter öldükten sonra park/save/menu/load. Tepki, ses, tek kondisyon maliyeti, sürüş kararlılığı ve ölü hedefte tekrar olmaması gözlenir. Burak yalnız somut görünen kusurları bildirmelidir. Manuel PASS gelmeden CLOSED yazılmaz.

## 2026-10-06 continuation checkpoint

Son tamamlanan XML kanıtları: `Evidence/20261006T083804-194334Z-zombie` 24/24; `Evidence/20261006T083946-091109Z-focused` 92/92; `Evidence/20261006T084346-297267Z-edit` 505/505. Bunlar son temas-noktası düzeltmesi ve capture düzenlemesinden ÖNCEDİR; final revizyon PASS değildir.

Tam PlayMode `../S012/Evidence/20261006T084359-823104Z/play.error.txt` ile sonuçlandı: `An unexpected error happened while running tests.` Sonuç XML'i yok. Wrapper exit 2 ile durdu, sonraki build çalışmadı; tarihsel dokümanların geri yükleme kaydı aynı klasörde `preserved-historical-paths.json` içindedir. Editor sonrasında playing=False, paused=False, compiling=False durumunda görüldü. Hatanın kök nedeni doğrulanmadı.

Son düzenleme her temas noktasında açısal hız ve hedef nokta hızını ayrı hesaplar; RotationUsesEachContactPointAndTargetMotion testi eklendi. Performance capture eşlenik koşularda uzaktaki encounter'ı duraklatır, aktif varyanta beş production prefab ekler; GUI telemetry ölçümde kapalıdır. Aktif koşu en az üç gerçek impact ve ölçüm süresince en az bir aktif AI gerektirir. Ölçüm araç up-dot/dikey hızını kaydeder, devrilmede başarısız olur. Bu capture henüz yeni build üzerinde çalıştırılmadı.

100 epizot odaklı soak: cache_peak=1, cache_end=0, listeners=1/1; managed warm=1385979904, mid=1385943040, end=1385943040 bytes. Bu izole fizik fixture'ı aktif-AI build performansı veya genel sızıntısızlık kanıtı değildir.

Büyük test/build koşularını Burak çalıştıracak. Yeni build, aktif-AI performance ve manuel kabul bekleniyor. **VEH-ZMB-001 IN_PROGRESS**.

Komutlar (Unity açık, Play kapalı ve derleme bitmişken; sırayla, önceki başarılıysa):

```bash
cd "/Users/burakcoskun/Last Signal"
python3 Tools/veh001-verify.py zombie
python3 Tools/veh001-verify.py focused
python3 Tools/veh001-verify.py edit
python3 Tools/veh001-verify.py play
python3 Tools/veh001-verify.py build --clean-cache
```

FAIL/BLOCKED/TIMEOUT durumunda sıradakini çalıştırmadan çıktıyı paylaş. Build PASS sonrası `BUILD=` yoluyla aynı build'in iki performance varyantı çalıştırılır:

```bash
python3 Tools/veh001-performance.py "BUILD= çıktısındaki dizin"
python3 Tools/veh001-performance.py "BUILD= çıktısındaki dizin" --active-infected
```

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

## Vehicle Impact Dominance — VEH-ZMB-002

Status: IN_PROGRESS. The current runtime has not yet passed the full post-change regression/build/performance gates. Manual acceptance: NOT_RUN. Historical VEH-ZMB-001 results are not final VEH-ZMB-002 evidence.

### Authority and tuning

The production pickup remains a 1800 kg dynamic Rigidbody with MotionCore/WheelCollider authority. The living infected has a NavMeshAgent-driven Transform and a root CapsuleCollider, without a Rigidbody or CharacterController. Animator root motion is disabled. A moving static capsule previously presented effectively unbounded positional resistance to the pickup.

VehicleInfectedContactResponse limits solver impulses only for registered vehicle/infected collider pairs, using an authored 80 kg effective infected mass. Immutable EntityId snapshots are read from normal and CCD contact modification callbacks; callbacks do not commit gameplay damage or overwrite vehicle velocity. World obstacles retain their ordinary collision response. Pair friction and restitution are zero for this limited character contact profile. This is a gameplay approximation for NavMesh-controlled bodies, not a full dynamic-body or ragdoll simulation.

Contact-normal severity remains canonical. The current curve starts at 2.5 m/s and reaches full severity at 11.5 m/s, with maximum damage 140 and condition cost 0.035. An undamaged 100-health ordinary infected reaches lethal damage at approximately 8.93 m/s normal closing speed. Tangential vehicle speed is not a kill threshold.

Surviving vehicle damage enters existing HitReact. The locomotion capsule is temporarily disabled while weapon hit regions remain available; pursuit and attack stop. Controlled horizontal NavMesh movement is bounded to 2.5 m over 0.45 s and clipped at NavMesh boundaries. Authored fall/down/get-up durations are 0.8/0.6/2.0 s. Existing source animation ZombieWakeUp_Transform_mixamo was copied into project-owned Source/LS_Zombie_VehicleGetUp.fbx and imported as Humanoid, with VehicleFall (0–2 s) and VehicleGetUp (18–22 s). These are played at the authored reaction durations. No external assets were downloaded. Directional hit/death selection remains the existing ZombieImpactReaction path; visual direction/plausibility of the shared full-body fall still requires manual acceptance.

Death cancels recovery, disables navigation, attack and hearing, and disables owned colliders through the existing death authority. The current corpse is an animated visual, not a colliding ragdoll; traversal does not create wheel-by-wheel damage. Canonical ZombieHealth, gameplay noise, World Pressure and persistence remain the transaction owners.

### Verified checkpoints before this continuation

S012/Evidence/20261006T104004-180288Z: isolated real-prefab high-speed test 1/1 PASS; 18 m/s initial velocity and 15.72069 m/s after the test drive (~87.3%). This sample includes the drive interval and is not an instantaneous solver-only measurement.

S012/Evidence/20261006T104759-235689Z: survivor reaction/recovery test 1/1 PASS.

S012/Evidence/20261006T105055-521520Z: dominance group 5/5 PASS. Production AI/wheel impact: commanded approach 12 m/s, first post-impact observation 10.77898 m/s (~89.8%). Parked displacement over the controlled agent-contact test: 0.00002288818 m. Active course: 4 impacts, 1 living AI, 0 retained contacts. Death-during-knockdown and explicit production authority checks passed. These are Editor tests, not final player performance evidence.

### Continuation changes

Confirmed active-infected capture holds forward input throughout the 20-second measured window. Normal capture retains its throttle/brake/reverse/turn segments. Both use the existing Development control port, and targets spawn after warmup. No additional driver abstraction was added. The >=3 measured-impact, active-AI, measurable-distance and zero-contact-cache requirements remain. Current build-level verification is pending.

ZombieAnimationPresenter now rejects knockdown calls after death or while knockdown is already active, and clears the transient knockdown flag on Initialize. This closes a stale presentation state on reuse; no new AI state machine was added.

### Verification boundaries

The parked-contact test applies repeated NavMeshAgent.Move toward the production chassis to stress Transform authority; it does not by itself prove a natural AI approach animation. The sequential performance course uses real active AI and real wheel drive. The separate three-target cluster test uses the isolated prefab lane and verifies one transaction per target, not full production horde handling. Manual parked approach, group feel, impact direction and full-body pose continuity remain required.

The latest 100-episode soak records cache_peak=1, cache_end=0 and listeners=1/1. Managed samples: warm=1388765184, mid=1388732416, end=1389002752 bytes (end minus warm=237568 bytes). This does not establish a leak-free reaction or contact registry; it includes fixture allocation and Editor activity. New build performance and visual acceptance remain pending.

### Latest focused gate

2026-10-06 23:06 Europe/Istanbul: VEH-ZMB group 35/35 PASS (15 domain, 14 integration, 1 hearing, 5 dominance). Evidence: `Evidence/20261006T200346-444548Z-zombie`. Includes canonical vehicle-kill save/load, open/occluded hearing, survivor recovery, death override, corpse traversal, contact deduplication and the 100-episode isolated soak. Full VEH regression, full EditMode/PlayMode, fresh clean build and both player performance captures remain pending.

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
