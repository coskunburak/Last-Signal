# S004 — Üretim zombi animasyonu, 2026-10-01

## Durum

Kullanıcının bildirdiği ateşte durma, `ZombieController.OnDamaged` içindeki `navigation.Stop`, `HitReact` güncellemesinin navigasyonu atlaması ve tam vücut hit sunumunun yürüyüşü kesmesinden geliyordu. Nonfatal isabet sırasında mevcut takip/araştırma yolu şimdi sürer; algı hafızası ve arama saati kısa reaksiyon boyunca dondurulur. Saldırı işlemi isabette kesilir, sağlık hasarını yalnız `ZombieHealth` belirler. Koşu ve yürüyüşü `NavMeshAgent` taşır; Animator kök hareketi kapalıdır.

Bu çalışma ağacında kullanıcıya ait çok sayıda başka değişiklik vardır. Toplu silme, sıfırlama, commit ve push yapılmadı.

## Eklenen kaynakların üretim kullanımı

Kaynak klasörü: `Assets/LastSignal/Assets/Animation/Zombie Animation/`. Yedi FBX'in GUID'i korundu. Unity denetiminde hepsi geçerli Humanoid Avatar ve `humanMotion` klibi taşır. `ZombieMixamoProductionAuthoring.Apply` kaynakları idempotent biçimde bağlar; bu oturumda başarıyla çalıştırıldı. Başarı kaydı `Evidence/20261001-MixamoProduction/runtime-contract.log` içindedir.

| Kaynak | Üretim kullanımı |
| --- | --- |
| `Zombie Walk.fbx` | `Locomotion`, döngülü; araştırma ve aramada 0,92 m/sn. Kaynak adım hızı 0,38 m/sn referansı üzerinden oynatma hızı eşlenir. |
| `Zombie Run.fbx` | `Run`, döngülü; takipte 2,6 m/sn |
| `Zombie Attack.fbx` | Birinci saldırı; sağ el ileri erişim zirvesi klibin ~%42'si, oyun içi temas saati buna bağlandı |
| `Zombie Attack (1).fbx` | Sıradaki saldırıda kullanılan ikinci varyant; iki el ileri erişim zirvesi ~%32 |
| `Zombie Dying.fbx` | Normal ölüm |
| `Flying Back Death.fbx` | Önden gelen ağır öldürücü gövde darbesinde görsel ölüm varyantı; oyun nesnesine kuvvet uygulamaz |
| `Zombie Crawl.fbx` | Geçerli Humanoid ve döngülü içe aktarım; henüz çalışma animasyonu değildir. Emekleme için NavMesh/collider/hitbox/saldırı sözleşmesi yoktur. Editör önizlemesinde incelenebilir. |

Yürüme/koşma klipleri döngülü, saldırı/ölüm klipleri tek oynatımlıdır. Kaynak kök yatay hareketi klipte tutulur; `Animator.applyRootMotion=false`, böylece görsel animasyon NavMesh yetkisini değiştirmez. İçe aktarma için `lockRootPositionXZ=false` kullanıldı.

## Vurulma ve ölüm

Önden hafif gövde isabetinde eski `Zombie@Damage01` tepkisi Humanoid üst gövde maskesinde oynar; bacakların yürüyüş/koşusu Base Layer üzerinde devam eder. Diğer yön/bölge/şiddette göğüs veya omurga dönüşü mevcut hareket pozuna kısa süre uygulanır. Gerçek yön `DamageInfo.Direction` ve isabet anındaki rotasyonla seçilir. Mevcut hit cooldown ve ağır reaksiyon önceliği korunur.

İki Mixamo ölüm klibinin kaynak son pozları zeminden 0,67–0,78 m yukarıda kaldığı için görsel modelin Y konumu klip boyunca ölçülmüş bir veri eğrisiyle düzeltilir. Oyun kökü ve NavMesh pozisyonu değişmez. `Evidence/20261001-MixamoProduction/motion.csv` ölçümünde düzeltilmiş mesh alt noktası normal ölümde +0,0034…+0,0104 m, geri düşme ölümünde −0,0011…+0,0109 m aralığındadır; son kareler +0,005 m'dir. Bu sayısal zemin ölçümü görsel onayın yerine geçmez.

## Saldırı ve anatomik sınırlar

`ZombieController` commit, tek temas ve recovery saatinin sahibi olmaya devam eder. İlk saldırı teması klip süresinin %42'sinde, ikinci saldırı teması %32'sindedir; ikisi 1,2 oynatma hızında çalışır. Bu değerler üretim iskeletindeki el ileri erişim ölçümüne dayalıdır (`motion.csv`). Sağ kol/sağ el koparsa mevcut saldırı kısıtı iki varyantı da engeller; yeni sol kol saldırı otoritesi varsayılmadı. Kopma görselleri ve Blood VFX mevcut ankrajlarında kalır.

## Doğrulama durumu ve sonraki kapı

Unity komut satırında içerik hazırlama ve derleme başarılı; gerçek runtime prefabı `ZombieAnimationPresenter.Initialize`, iki saldırı ve hasar sunumu sözleşmesini geçti. Hareket örnekleme denetimi yapıldı. Kullanıcı son ayarlardan sonra tam EditMode ve PlayMode regresyonlarını tekrar çalıştırdı; sonuçlar aşağıda. Build, performans ve oyun içi görsel kabul henüz tamamlanmadı. Önceki 7/7 yön testi, 1/1 yön PlayMode testi, 52/52 ilgili EditMode ve 20/20 ilgili PlayMode sonuçları bu entegrasyondan **önce** alındı; yeni kodu kanıtlamaz.

Kullanıcının `Evidence/20261001-ProductionAnimationEdit-20shh5/animation-editmode.xml` sonucu yeni entegrasyon için odaklı EditMode kapısını **22/22 Passed**, 0 failed, 0 skipped olarak kaydetti. Günlük `Test run completed. Exiting with code 0 (Ok)` ile bitti. Bu yalnız varlık, saldırı zamanlama doğrulaması ve yön seçimi kapsamıdır; hareketin oyun içinde sürmesini henüz doğrulamaz.

İlk odaklı PlayMode `Evidence/20261001-ProductionAnimationPlay-Jd9R6F/animation-playmode.xml` sonucu **1/2 Passed**: presenter hız/Walk/Run testi geçti. Ateşte hareket testi 0,18 saniyede 0,06854 m gerçek ilerleme ölçtü; eski test eşiği 0,08 m idi. Test takibi hız henüz 0,2 m/sn'yi geçtiğinde başlatıyordu. Test, hız 0,8 m/sn üstünde yerleşince isabet verecek, yol/hareket ve Base Layer `Run` durumunu birlikte denetleyecek biçimde düzeltildi.

`Evidence/20261001-ProductionAnimationMoveRetry-L9B33b/move-playmode.xml`: düzeltilmiş takip sırasında isabet testi **1/1 Passed**, 0 failed, 0 skipped; günlük çıkış kodu 0. Bu, isabet boyunca NavMesh hareketinin ve Base Layer `Run` durumunun devam ettiğini üretim prefabında doğrular.

`Evidence/20261001-ProductionAnimationCombat-jMxREj/combat-playmode.xml`: **3/4 Passed**. Gerçek tüfekle gövde/baş/ölüm rotası, saldırı saatinin 30/60/120 Hz eşleşmesi ve tüm Animator durumlarının hareketi geçti. Ölüm zemin testindeki tek hata, bağımsız görsel prefabın transform'unu oyun kökü sanan test varsayımıydı: ölçülen görsel Y konumu −0,78 m, zemin düzeltmesinin beklenen sonucudur. Test artık görseli bağımsız oyun kökü altına yerleştirip oyun kökünün sabit kaldığını denetler; ağır önden gövde ölümü için ikinci bir zemin/poz sınaması da eklendi.

`Evidence/20261001-ProductionDeathRetry-Wr8cci/death-playmode.xml`: düzeltilmiş `ZombiePresentationTests` **3/3 Passed**, 0 failed, 0 skipped; günlük çıkış kodu 0. Normal ölüm, ağır önden gövde ölümünde `FlyingBackDeath`, ceset zemini, sabit oyun kökü ve tüm sunum durumlarının hareketi doğrulandı. Oyun içi kamera/ışık altında sanat kabulü bu otomatik testin kapsamı dışındadır.

`Evidence/20261001-AnimationSubsystemRetry-u2ABfl/subsystem-playmode.xml`: ilgili PlayMode alt sistem regresyonu **21/21 Passed**, 0 failed, 0 skipped; günlük çıkış kodu 0. Kopma, Blood VFX, saldırı varyantı ve yakın dövüş, vurulma/ölüm kesintileri, navigasyon ile üretim tüfek/nüfus/kaydet-yükle rotası bu seçimde geçti.

`Evidence/20261001-AnimationFullEdit-Qc6KRb/full-editmode.xml`: ilk tam EditMode regresyonu **432/432 Passed**, 0 failed, 0 skipped; günlük çıkış kodu 0.

`Evidence/20261001-AnimationFullPlay-G4qzdu/full-playmode.xml`: ilk tam PlayMode **182/187 Passed, 5 failed**, 0 skipped. `MovementAcceptanceTests.MouseAndCrouchActionsReachCameraAndCapsule` aynı gün animasyon değişikliğinden önceki `Evidence/20261001-BloodVFX/user-20261001-204615-full-play/results.xml` koşusunda da tek başarısız testti (183/184). Yeni dört zombi hatası ayrıştırıldı: arama hızı 0,38 m/sn ile alanı zamanında incelemiyordu; uzun ikinci saldırının pasif kuyruğu beş vuruşlu ölüm rotasını 12 saniyenin dışına taşıyordu; büyük adım testi eski recovery zamanını sabit 0,9 sn kabul ediyordu; görsel kabul testindeki sabit yakın dövüş hedefi arama sonrası kasa tarafından gizlenebiliyordu. Üretim yürüyüşü 0,92 m/sn'ye geri alındı; görsel kadans gerçek hıza ölçeklendi. İkinci saldırı, gerçek temas ve recovery saatleri korunarak klibin %70'inde Idle'a karışır. İki test varsayımı yeni süre/konuma göre düzeltildi. `regression-tuning.log` içerik hazırlama ve runtime prefab sözleşmesinin başarılı olduğunu gösterir.

`Evidence/20261001-AnimationFullFailureRetry-Rkpo6K/melee-editmode.xml`: son ayarlardan sonra ilgili EditMode testleri **12/12 Passed**, 0 failed, 0 skipped. Aynı klasördeki `affected-playmode.xml`, ilk tam PlayMode'da başarısız olan dört zombi testini ve hız/pause geri kazanımı testini **5/5 Passed**, 0 failed, 0 skipped olarak doğruladı. İki günlük de çıkış kodu 0 ile bitti.

`Evidence/20261001-AnimationFinalRegression-XwEZy7/full-editmode.xml`: son tam EditMode **433/433 Passed**, 0 failed, 0 skipped. Aynı klasördeki `full-playmode.xml`: **186/187 Passed**, 1 failed, 0 skipped; tüm zombi testleri geçti. Tek hata `MovementAcceptanceTests.MouseAndCrouchActionsReachCameraAndCapsule` satır 218'de çömelmenin ikinci tuş basışında kapanmamasıdır. Aynı test, aynı satır ve aynı `Expected: False / But was: True` hatası animasyon çalışmasından önceki `Evidence/20261001-BloodVFX/user-20261001-204615-full-play/results.xml` koşusunda da vardır. Bu bağımsız oyuncu hareketi hatası zombi kapsamı dışındadır; tam PlayMode kapısı bu nedenle yeşil sayılmaz. Son testten sonra yalnız Editor-only build giriş yöntemi eklendi; runtime animasyon kodu değiştirilmedi.

`Evidence/20261001-AnimationBuild-oStzmQ/build-summary.txt`: kullanıcının macOS development build'i **Succeeded**, 0 errors, 374 warnings, Unity 6000.5.0f1; çıktı aynı klasördeki `LastSignal.app` (yaklaşık 411 MB). Günlükteki uyarılar ağırlıkla Unity AI Inference/Sentis shader'ları ve eski Unity API'lerine dair C# CS0618 uyarılarıdır; bu günlükte eksik zombi referansı veya animasyon derleme hatası bulunmadı. Build'in üretilmesi uygulamanın açılıp normal oyun rotasını tamamladığını kanıtlamaz.

Sonraki kapı: build uygulamasının açılması, normal oyun rotası, kalabalık performansı ve görsel kabul. `ZombieMixamoProductionAuthoring.BuildDevelopmentMac` yalnız yeni ve benzersiz S004 evidence klasörüne üretim sahnesi build'i yazar; çalışırken oluşan `build.log` dışında önceden içerik varsa durur ve eski build özetini ezmez. Build komutunda `-nographics` kullanılmaz.

## Kullanıcı görsel ve performans kabulü

1. Unity'de `Last Signal/Zombie/Preview Mixamo On Production Visual` menüsünü açın; Walk, Run, iki Attack, Dying, Flying Back Death ve hazırlanan Crawl klibini önden, yandan ve arkadan izleyin. Omuz/bilek bükülmesi, el pozu, ayak kayması, ölçek sıçraması ve ölümde yerden yükselmeyi kaydedin. Crawl yalnız önizlemedir.
2. `Assets/LastSignal/Scenes/RelayExpedition.unity` içinde normal oyun akışında 1 zombiyle Idle → Walk → Run → durma → yön değiştirme → saldırı → tekrar hareket sırasını izleyin. Ateş ederken takip hızının kesilmediğini, bacakların devam ettiğini ve yakın dövüşte el temasıyla hasarın aynı anda geldiğini kontrol edin. Önden, arkadan, soldan ve sağdan gövdeye ateş edin; yürürken, koşarken, saldırırken ve mevcut reaksiyon sürerken tekrarlayın.
3. Ayrı zombilerde baş, göğüs, sol/sağ kol ve sol/sağ ele isabet uygulayın. Normal gövde ölümü, kafa ölümü, kafa kopması ve uygun ağır ön gövde ölümünde Flying Back'i görün. Her kopmada stump ve kan yönünü, parçanın geri görünmemesini, kopan sağ kol/elden sonra görünmez el hasarı olmamasını ve kopma sonrası ölümü izleyin. Normal ölümde cesedin zemine oturduğunu, oyun kökünün kaymadığını doğrulayın.
4. Yukarıdakileri gündüz, gece ve el feneriyle; önden, yandan ve arkadan tekrarlayın. Aynı üretim prefabıyla 1, 10, 20 ve 30 zombi çalıştırın. Warmup sonrasında Unity Profiler'da frame time p50/p95/p99, CPU/GPU, GC.Alloc, Animator ve NavMesh maliyeti ile pikleri kaydedin; sahne, kalite, çözünürlük ve donanımı not edin. Otomatik test sonucu görsel veya performans kabulü yerine geçmez.

Eski `ZombieAssetIntegration.Build` yolu güncel üretim denetleyicisini hedeflemez; bu entegrasyon için `ZombieMixamoProductionAuthoring.Apply` kullanılır. `ZombieDamageAuthoring.Compose` artık mevcut hit/ölüm kliplerini yeniden eski varlıklara yazmaz.

## 2026-10-01 gövde vuruşu ve kafa kopması görsel düzeltmesi

Kullanıcı oyun içi incelemede gövde vuruşu tepkisinin eksik, normal ölümün başarılı olduğunu; kafa kopmasında `Flying Back Death.fbx` istediğini bildirdi. Kaynak taramasında `Zombie@Damage01.fbx` (Humanoid, üretim `Shambler.asset` içinde zaten bağlı), `HumanM@CombatDamage01.fbx` ve `HumanF@CombatDamage01.fbx` (Humanoid, genel insan), `zombie@gethit.fbx` (Generic; üretim Humanoid iskeletine doğrudan uygun değil) bulundu. Mevcut zombi klibi ve üst gövde maskesi korunur. Kullanıcı kaynak FBX ve GUID'leri değiştirilmedi.

İlk seçim kuralı `Zombie@Damage01` klibini yalnız **hafif ön gövde** hasarında oynatıyordu; üretim tüfeğinin 30 HP gövde isabeti ağır sayıldığı için yalnız prosedürel açı tepkisi görünüyordu. Artık canlı zombinin baş dışındaki anatomik isabetlerinde bu klip üst gövde katmanında oynar. Yan/arka isabetlerde aynı anda isabet anı yönüne uygun omurga tepkisi eklenir. Bacak katmanını ve NavMesh yolunu kesmez. `HitReactSpeed` Animator Float parametresi üst katmandaki klibi gerçek sürede tutar; yürüyüşün 0,38→0,92 m/sn hız ölçeklemesi vuruş klibini erken bitirmez. Mevcut cooldown ve ağır hasar önceliği aynıdır.

Kafa kopması yalnız `ZombieDismemberment` tarafından gerçekten commit edilmiş ve `ZombieHealth` tarafından ölüm olarak kesinleştirilmişse `FlyingBackDeath` seçer. Baş hasarı kopma yaratmadan ölümle sonuçlanırsa normal `Death` kalır. Ön ağır gövde ölümü için önceki `FlyingBackDeath` seçimi korunur. Görsel ölüm oyun köküne kuvvet vermez; kafa renderer'ı kopuk kalır, kan ve tek ölüm olayı mevcut otoritede kalır. Kafa arkadan kopsa da istenen Flying Back klibi kullanılır; bu yön özelinde sanatsal uygunluk manuel kabulde incelenmelidir.

Yeni/yenilenen testler: `ZombieImpactReactionTests` gövde klibi, yön, kafa kopması ve ölüm ayrımını; `ZombieAssetTests` gerçek kaynak yolu ve katman hız parametresini; `ZombiePresentationTests` duran/yürüyen gövde vuruşu ile kafa kopması ölüm pozunu; `ZombieDamagePlayTests` üretim prefabında 30 HP vurulurken koşu/klip birlikteliğini ve kafa kopmasında tek ölüm, gizli kafa, Flying Back ve sabit oyun kökünü denetler. `Evidence/20261001-BodyHitHeadSever-qw5DSb`: `Apply` başarılı, EditMode **11/11 Passed**, PlayMode **10/11 Passed**. Tek başarısız test `WalkingBodyReactionKeepsClipAtRealTimeWhileLegsKeepWalking`: ilk zamanlı Animator güncellemesinden sonra overlay state `HitReact` idi fakat `normalizedTime=0` kaldı. Nonlethal `BeginDamage` artık ölüm dalında olduğu gibi `Animator.Update(0)` ile state girişini ilk zamanlı adım öncesinde commit eder; hız parametresinin controller varsayılanı 1 yapılır. Bu düzeltmenin yeniden test sonucu bekleniyor. Önceki build bu değişiklikleri içermez.
