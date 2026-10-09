# S016 çalışma kaydı

## 2026-10-07 — Modal sahipliği, ilk kapı

IMPLEMENTED: SessionFlow.Screen/SessionMenuVisible ve inventory aç/kapat yetkisi; InventoryUI lifecycle; pause/audio/save görünüm çakışması düzeltmesi. Scene fallback kurulumu InventoryViewFactory'ye taşındı, private reflection kaldırıldı; authored production view korunur. Mevcut input neutral gate, gameplay envanter ve kayıt otoriteleri değiştirilmedi.

Yeni S016ModalTests beş PlayMode davranışını production S013Cabin sahnesinde doğrulamak üzere hazırlandı. Tools/s016-verify.py yalnız explicit focused-play kapısını çalıştırır, test keşif sayısını 5 olarak denetler, kaynak hash/XML/log/komut/ortam kaydeder; Editor kilidi varsa durur. Ajan scripti veya Unity testlerini çalıştırmadı.

Tests: NOT_RUN — manual execution reserved for the user.
User-run automated verified: yok. İnsan kabulü/build: NOT_RUN. S016: PARTIAL; sonraki kartlar gap matrisinde açık. Bu ilk kapı tüm sprintin tamamlandığı anlamına gelmez.

Statik kontroller: s016-verify.py AST parse edildi; beş UnityTest bildirimi sayıldı; değişen runtime dosyalarında scoped git diff --check temiz. Genel diff kontrolü önceden mevcut S014 ledger whitespace satırlarını işaretledi; kullanıcı belgesi değiştirilmedi. C# derleme ve runtime doğrulaması NOT_RUN.

## 2026-10-07 — İlk kullanıcı koşusu: derleme BLOCKED

Kullanıcı koşusu `Evidence/20261007T171448-708655Z-focused-play/`: process exitCode=1; NUnit XML oluşmadı, hiçbir test sonucu yok; source-preservation changed=[]. S016ModalTests.cs:54 ve :59 CS1501. Sınıflandırma: test harness derleme hatası. Ajanın eklediği `InputSystem.Update(InputUpdateType.Dynamic)` yerel Input System 1.19.0 paketinde internal; LastSignal.PlayModeTests tarafından çağrılamaz.

Düzeltme yalnız yeni test fixture'ında: iki manuel Update çağrısı kaldırıldı. QueueStateEvent ve mevcut frame yield'leri korunuyor; InputTestFixture UnityTest player-loop köprüsü olayları işler. Runtime ve assertion'lar değiştirilmedi. Hatalı koşu kanıtları korundu. Düzeltme sonrası derleme/test NOT_RUN; aynı beş testlik focused-play komutu kullanıcıya devredildi.

## 2026-10-07 — Kullanıcı koşusu 0/5 FAIL; gerçek sahne input izolasyonu

`Evidence/20261007T171843-404749Z-focused-play/`: XML total=5, passed=0, failed=5, skipped=0; process exitCode=2; source-preservation changed=[]. Runtime kaynak hash'leri mevcut dosyalarla eşleşiyor. İlk test OpenInventory → SetGameplay → InputManager.AddStateChangeMonitor içinde IndexOutOfRangeException verdi; TearDown da aynı yolda bozuldu. Diğer dört test eski sahne kapanışı/UI module açılışında SetUp exception ile durdu. Beş bağımsız modal assertion hatası olarak yorumlanmadı.

Sınıflandırma: test harness input-lifecycle izolasyonu. InputTestFixture cihaz yöneticisini değiştirip Restore ederken asynchronous gerçek sahnenin action/UI tüketicileri yaşamaya devam ediyordu; kontrolün device index'i etkin manager'ın monitor dizisinde geçersizdi. Yeni fixture InputTestFixture kalıtımını kaldırıyor: mevcut manager korunur, önce etkin olan cihazlar geçici devre dışı bırakılır, test keyboard/mouse eklenir. UnityTearDown menü dönüşü/deferred destroy/sahne unload işlemlerini cihazlar kaldırılmadan tamamlar; TearDown test cihazlarını kaldırır, önceki cihazları ve background/input ayarlarını geri getirir. Test runner silinmez.

Runtime ve beş testin assertion'ları değiştirilmedi; exception beklenen log diye yutulmadı. Statik API/syntax-diff incelemesi yapıldı; düzeltme sonrası Unity derleme/test NOT_RUN. Aynı focused-play komutu kullanıcıya tekrar verildi; önceki başarısız kanıtlar korundu.

## 2026-10-07 — Modal kapısı kullanıcı doğrulaması 5/5 PASS

`Evidence/20261007T172319-825447Z-focused-play/`: Unity 6000.5.0f1, macOS, LastSignal.PlayModeTests / LastSignal.Tests.S016ModalTests, XML total=5 passed=5 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Bu kullanıcı tarafından çalıştırılan otomatik test kanıtıdır. Beş modal/lifecycle sözleşmesi PASS; insan UX kabulü veya full regression değildir.

## 2026-10-07 — D152/D153 arayüz kapısı uygulandı

InventoryUI mevcut PlayerInventory.TryMove/TrySplit/TryDrop komutlarını kullanır. Başarısız hareket/drop kaynak ve seçimi korur, sonuç metni üretir. Split-half butonu boş hedef yuva seçimi gerektirir; toplam miktar domain tarafından korunur. Seçilen slotun item tanımı değiştiğinde seçim temizlenir. Slotlarda renk dışında `▶` seçili işareti var. ShelterStorageUI kısmi transferin sığan miktarı taşıdığını buton ve sonuç metninde belirtir; başarısız transferin kaynağı değiştirmediğini bildirir. İki panelde de selection item tanımı değiştiğinde temizlenir. Genel equip/consume/revision/domain instance kimliği eklenmedi.

S016ModalTests sınıfına üç gerçek production UI/domain testi ve bir transfer feedback sonucu testi eklendi; toplam keşif hedefi 9. Önceki 5/5 ilgili InventoryUI değiştiği için yeni kaynak durumuna taşınmadı. Yeni 9 test **NOT_RUN**. C# compile/PlayMode/build/insan kabulü NOT_RUN. Python AST ve scoped diff whitespace kontrolü PASS; Unity başlatılmadı.

## 2026-10-07 — D152/D153 kullanıcı kapısı 9/9 PASS; D154 hazırlığı

Kullanıcı koşusu `Evidence/20261007T172837-898184Z-focused-play/`: Unity 6000.5.0f1, macOS, LastSignal.PlayModeTests / LastSignal.Tests.S016ModalTests; XML total=9 passed=9 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Dört yeni inventory/depo sonucu kontrolü ve önceki beş modal sözleşmesi aynı kaynak durumunda geçti. Bu kullanıcı tarafından çalıştırılan otomatik doğrulamadır; klavye odak ergonomisi veya görsel kabul değildir.

D154: InteractionController ilk blocker ray'ini 16 elemanlık non-alloc hit dizisiyle mesafe ve collider entity ID sırasına göre deterministik seçer; dizi dolarsa güvenli şekilde hedef seçmez. Doğrudan ray hedefi az farkla kaçırdığında yalnız daha önce doğrulanmış, erişilebilir ve dar nişan konisindeki hedef korunur; hedefe ayrı ilk-blocker LOS denetimi uygulanır. Gerçek duvar, disabled/despawn, menzil dışı ve `Available` reddi hedefi temizler. TryInteract hâlâ Resolve ile commit anında yeniden doğrular. Üç odaklı yeni PlayMode testi hazırlandı; `focused-interaction` tek kapı olarak eklendi.

## 2026-10-07 — D154 ilk kullanıcı koşusu: derleme BLOCKED

Kullanıcı koşusu `Evidence/20261007T173239-028764Z-focused-interaction/`: process exitCode=1; NUnit XML yok, test çalışmadı; source-preservation changed=[]. `InteractionController.cs:66` Unity 6000.5.0f1'de kaldırılmak üzere yasaklanan `GetInstanceID()` nedeniyle CS0619 verdi. Aynı çağrı yeni testin overlap beklentisinde de vardı. Her iki yeri mevcut projede kullanılan `EntityId.ToULong(collider.GetEntityId())` düzenine taşıdık; eşit mesafe seçim kuralı değişmedi. Sentetik fixture mevcut InputTestFixture desenini izliyor, production sahne yüklemiyor. Düzeltme sonrası Unity derleme ve üç test **NOT_RUN**; aynı odaklı kullanıcı kapısı bekleniyor.

Sonraki kullanıcı komutu `Temp/UnityLockfile` varlığı nedeniyle testten önce durdu; yeni RUN veya XML oluşmadı. Uygulama listesinde Unity Editor çalışmıyordu, `lsof` kilidi açık tutan süreç bulmadı. Sıfır baytlık eski kilit `/private/tmp/LastSignal-UnityLockfile-stale-20261007T173239` konumuna taşındı; proje kilidi artık yok. D154 kapısı hâlâ **NOT_RUN**.

## 2026-10-07 — D154 kullanıcı kapısı 3/3 PASS; D155 hazırlığı

`Evidence/20261007T173622-391489Z-focused-interaction/`: Unity 6000.5.0f1, macOS, LastSignal.PlayModeTests / LastSignal.Tests.S016InteractionTests; XML total=3 passed=3 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Kenar aim drift, blocker/disabled hedef ve eşit mesafeli örtüşme otomatik olarak doğrulandı. Gerçek sahnede prompt okunurluğu D160 insan kabulüne açık.

## 2026-10-08 — D155 ayar profili, odaklı EditMode kapısı hazır

S015 `AudioPreferences` mağazası genişletildi; mevcut ses/caption PlayerPrefs anahtarları korunuyor, v1 kontrol alanları (FOV 60–100 varsayılan 75; duyarlılık .02–.5 varsayılan .12; UI ölçeği .8–1.3 varsayılan 1) ekleniyor. Önceki versionsuz S015 profil ses değerlerini koruyup yeni alanları varsayılanla açar; bilinmeyen/bozuk kontrol alanları güvenli varsayılan veya clamp alır. SessionFlow yeni oyuncunun FirstPersonLook değerlerini profilden uygular; AudioSettingsView mevcut ses paneli yanında kontrol paneli kurar, canlı değişiklikleri uygular ve ses ayarlarıyla aynı gecikmeli kaydı kullanır. UI ölçeği ScaleWithScreenSize CanvasScaler referans çözünürlüklerine uygulanır. Gameplay save slotlarına yazılmaz.

`S016SettingsTests` üç EditMode davranışını ölçmek için hazır: versionsuz ses profili geçişi, v1 roundtrip, bozuk değer fallback/clamp. `Tools/s016-verify.py focused-settings` tek kapı olarak eklendi. Python syntax ve scoped whitespace kontrolü PASS; Unity derleme/test **NOT_RUN**. Görsel panel ve gerçek sahne etkileşimi EditMode kanıtı sayılmayacak; sonraki PlayMode/D156 kabulünde ölçülecek.

## 2026-10-08 — D155 profil 3/3 PASS; gerçek sahne kapısı hazır

Kullanıcı koşusu `Evidence/20261007T231555-055262Z-focused-settings/`: LastSignal.EditModeTests / S016SettingsTests, XML total=3 passed=3 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Bu profil kalıcılığını doğrular, production panel etkileşimini doğrulamaz.

`S016SettingsPlayTests` tek PlayMode davranışını gerçek S013Cabin sahnesinde ölçmek için hazırlandı: Pause ayar paneli, üç slider'ın canlı FirstPersonLook/CanvasScaler etkisi, gecikmeli PlayerPrefs kaydı ve yeni oturumda look değerleri. Test mevcut PlayerPrefs anahtarlarının varlık ve değerlerini kaydedip sonunda geri yükler. `focused-settings-play` tek kapı olarak eklendi. Yeni PlayMode test **NOT_RUN**; görsel çözünürlük/uzun metin kabulü açık.

## 2026-10-08 — D155 gerçek sahne kapısı 1/1 PASS; D156 hazırlığı

Kullanıcı koşusu `Evidence/20261007T231815-285388Z-focused-settings-play/`: LastSignal.PlayModeTests / S016SettingsPlayTests, XML total=1 passed=1 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Ayar paneli, canlı look/UI ölçeği, gecikmeli kayıt ve yeni oturum davranışı otomatik doğrulandı. Görsel okunurluk ve ayrı Settings ekran geri dönüşü hâlâ açık.

## 2026-10-08 — D156 üretim font kapısı hazır

Kullanıcının projede bulunan `Assets/Noto_Sans/static/NotoSans-Regular.ttf` fontu S013Cabin sahnesindeki 130 authored uGUI Text bileşenine bağlandı. Runtime oluşturulan envanter, barınak ve ayar etiketleri `ProductionUiFont.Resolve()` üzerinden sahnedeki Noto fontunu yeniden kullanır; sahnede yoksa legacy test/fallback fontuna döner. Yeni font indirilmedi ve var olan Noto asset taşınmadı. `S016ReadabilityTests` üretim sahnesinde tüm Text font kimliğini ve Türkçe glifleri ölçmek üzere hazır; `focused-readability` tek kapı. Python syntax ve scoped whitespace kontrolü PASS; Unity test **NOT_RUN**. 1920×1080, %130 ölçek ve +%30 sahte uzun metin görsel kabulü ayrıca açık.

## 2026-10-08 — D156 ilk kullanıcı koşusu: derleme BLOCKED

`Evidence/20261007T232135-930771Z-focused-readability/`: process exitCode=1; NUnit XML yok, test çalışmadı; source-preservation changed=[]. Tek derleme hatası `S016ReadabilityTests.cs:41` CS0161: `[UnityTest]` IEnumerator gövdesinde `yield` yoktu. Test sonuna bir karelik `yield return null` eklendi; production font bağı veya assertion gevşetilmedi. Batchmode kilidi sıfır bayt olarak kaldı; Unity Editor uygulama listesinde kapalı ve `lsof` boş olduğundan `/private/tmp/LastSignal-UnityLockfile-stale-20261007T232135` konumuna taşındı. Düzeltme sonrası Unity testi **NOT_RUN**.

## 2026-10-08 — D156 font 1/1 PASS; D157 kanıt incelemesi; D158 kapısı hazır

Kullanıcı koşusu `Evidence/20261007T232322-562652Z-focused-readability/`: LastSignal.PlayModeTests / S016ReadabilityTests, XML total=1 passed=1 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Bu font kimliği ve Türkçe glyph otomatik doğrulamasıdır; gerçek 1080p/%130/+%30 metin görsel kabulü değildir.

D157 için yeni değişiklik yapılmadı: önceki kullanıcı `S016ModalTests` 9/9 koşusundaki mouse ile Close→aynı tıkla ateş etmeme, focus loss→basılı saldırı tekrar oynamama ve tekrar oturum lifecycle testleri doğrudan input leakage kapısını kapsıyor. `PlayerInputReader` neutralRequired kapısı SetGameplay geçişlerinde kuruluyor; ayrı Settings ekran geri dönüşü ve drag mekanizması henüz mevcut değil. Mevcut kapsama duplicate test yazılmadı.

D158: SessionFlow tek pause otoritesi, WorldClock pause guard'ı, ZombieEncounter.SetPaused ve VehicleWorld.Suspend sahipleri incelendi. `S016PauseTests` gerçek S013Cabin sahnesinde oyun saatinin pause öncesi/sonrası davranışı, player/interaction/health, zombi state/konum, araç torku ve pause sırasında ayar UI görünürlüğünü bir arada ölçmek üzere hazırlandı. `focused-pause` tek kapı eklendi; Unity test **NOT_RUN**.

## 2026-10-08 — D158 pause 1/1 PASS; D159 kayıt mesajları kapısı hazır

Kullanıcı koşusu `Evidence/20261007T232558-767620Z-focused-pause/`: LastSignal.PlayModeTests / S016PauseTests, XML total=1 passed=1 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Üretim sahnesinde world clock/player/zombi/araç/interaction pause ve Resume davranışı ile ayar UI kullanılabilirliği otomatik doğrulandı. İnsan kabulü ve geniş regresyon açık.

D159: `SaveFeedback` mevcut SaveResult/SaveError sonuçlarını Türkçe oyuncu metinlerine çevirir. Eksik dosya, bozuk kayıt, geçersiz veri, desteklenmeyen sürüm, içerik/dünya uyumsuzluğu, dosya erişimi, meşgul ve eski oturum sonuçları ayrılır; teknik Message metni UI'ye taşınmaz. Başarı yalnız SaveResult.Success ile ve doğru işlem adıyla gösterilir. WorldTimeSaveControls yükleme sırasında durum metni, sonuç için sekiz saniyelik unscaled görünürlük kullanır; başarılı yükleme sonrası gameplay ekranında sonuç okunabilir. Diğer modal ekranlarda sonuç gizlenir. SaveSession, dosya işlemleri ve recovery otoritesi değiştirilmedi; otomatik backup kurtarma mevcut olmadığı için kurtarma başarısı uydurulmadı.

`S016SaveFeedbackTests` üç EditMode testi: gerçek SaveCodec eksik/bozuk/checksum uyuşmayan veriyi reddettiğinde başarısızlık mesajı; anlaşılır kategorilerin ayrılığı; teknik ayrıntıların oyuncu metnine sızmaması ve save/load başarısının ayrılması. `focused-save-feedback` kapısı eklendi. Python AST ve scoped whitespace kontrolü PASS; Unity derleme/test NOT_RUN. Gerçek UI yükleme/sonuç süresi PlayMode doğrulaması bu EditMode kapısından sonra açık.

## 2026-10-08 — D159 mesajlar 3/3 PASS; üretim UI kapısı hazır

Kullanıcı koşusu `Evidence/20261007T232953-777420Z-focused-save-feedback/`: LastSignal.EditModeTests / S016SaveFeedbackTests, XML total=3 passed=3 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Gerçek codec reddi ve oyuncu mesajları doğrulandı; UI görünürlüğü bu kanıta dahil değil.

`S016SaveFeedbackPlayTests` iki production PlayMode testi hazırlandı: authored Load düğmesiyle eksik/checksum bozuk kayıt menüde hata gösterir ve dosyayı değiştirmez; authored Save/Load düğmeleriyle gerçek checkpoint roundtrip, gameplay ekranında başarı görünürlüğü ve pause sırasında unscaled süre sonunda kapanma. WorldTimeSaveControls.Configure mevcut SaveSession path parametresini opsiyonel olarak geçirir; varsayılan davranış aynı, test yalnız benzersiz geçici slot kullanır. Mevcut kullanıcı kayıt dosyasına dokunulmaz. Test teardown coroutine'i kapatır, sahneyi boşaltır ve geçici test klasörünü temizler.

`focused-save-feedback-play` kapısı eklendi; beklenen 2 test. Python AST ve scoped diff whitespace kontrolü PASS. Unity derleme/test NOT_RUN — kullanıcı çalıştıracak. Görsel kabul ve geniş regresyon açık.

## 2026-10-08 — D159 gerçek UI 2/2 PASS; persistence regresyon devri

Kullanıcı koşusu `Evidence/20261007T233209-747098Z-focused-save-feedback-play/`: LastSignal.PlayModeTests / S016SaveFeedbackPlayTests, XML total=2 passed=2 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Authored düğmelerle eksik/bozuk kayıt menüde hata gösterdi ve kaynak dosya korundu; gerçek checkpoint save/load, gameplay sonucunun görünürlüğü ve pause sırasında unscaled sürenin dolması doğrulandı.

Sıradaki tek kapı mevcut `PersistenceIntegrationTests` sınıfının 9 PlayMode testidir: ownership/loot tekrar oturum roundtrip, bozuk kayıt, eski oturum hydration, hydration hata geri dönüşü, oyun sırasında yükleme reddi, eksik dünya nesnesi, iptal edilen yükleme ve iki reload/ölüm/checkpoint akışı. Yeni production kodu veya duplicate test eklenmedi. Testler benzersiz geçici kayıt klasörü kullanır. `Tools/s016-verify.py persistence` eklendi; validation sahneleri de kaynak hash kapsamına alındı. Python AST ve scoped whitespace kontrolü PASS. Unity test/build NOT_RUN; bu kapıyı kullanıcı çalıştıracak. Açık menü/settings/UX maddeleri ve D160 insan kabulü tamamlandı sayılmadı.

## 2026-10-08 — Persistence 9/9 PASS; tam EditMode devri

Kullanıcı koşusu `Evidence/20261007T233413-676690Z-persistence/`: LastSignal.PlayModeTests / PersistenceIntegrationTests, XML total=9 passed=9 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Mevcut persistence/ölüm/checkpoint entegrasyonu bu kaynak durumunda geçti.

Sıradaki tek kapı `Tools/s016-verify.py regression-edit`: yalnız LastSignal.EditModeTests, filtre yok, tek Unity süreci. S015 son kullanıcı EditMode kanıtında 524 test (`20261007T145716-990329Z-full-edit`), S016 iki EditMode sınıfında toplam 6 yeni test olduğundan keşif alt sınırı 530. Bu sayı yeni koşunun sonucu değildir. Art.EditorTests ayrı assembly'dir; bu komutun kapsamına dahil değildir.

Eski EditMode testleri sabit tarihsel evidence yollarına yazdığı için S015 helper'ın yalnız save_legacy/restore_legacy fonksiyonları yeniden kullanıldı; çok aşamalı main çağrılmıyor. Üretilen farklı çıktılar S016 RUN/generated-artifacts altında korunup tarihsel dosyalar önceki baytlarına döndürülür. Python AST ve scoped whitespace kontrolü PASS. Yeni Unity kapısı NOT_RUN; testi kullanıcı başlatacak. Sprint açık maddeleri ve insan kabulü henüz tamamlanmadı.

## 2026-10-08 — Tam EditMode 530/530 PASS; tam PlayMode devri

Kullanıcı koşusu `Evidence/20261007T233607-435838Z-regression-edit/`: LastSignal.EditModeTests, XML total=530 passed=530 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Tarihsel koruma raporu WorldTime domain-performance ve R02 performance dosyalarının yeni çıktılarını RUN içinde koruyup önceki baytlarına döndürdüğünü kaydeder. Art.EditorTests bu koşuya dahil değildir.

Sıradaki tek kapı `Tools/s016-verify.py regression-play`: yalnız LastSignal.PlayModeTests, filtre yok, tek Unity süreci; aynı tarihsel kanıt koruması etkin. Keşif alt sınırı 297: önceki S015 XML'lerinde keşfedilen 280 + mevcut S016 sınıflarındaki 17 test. S015 eski tam PlayMode koşuları yeni PASS kanıtı değildir; son incelenen `20261007T151448-490448Z-full-play` 279/280 idi (MovementAcceptanceTests.MouseAndCrouchActionsReachCameraAndCapsule). Yeni koşunun sonucu bağımsız kaydedilecek; başarısızlıkta yalnız ilgili test incelenip odaklı tekrar hazırlanacak.

Production/test kodu değişmedi. Python AST ve scoped whitespace kontrolü PASS; yeni Unity PlayMode kapısı NOT_RUN — kullanıcı çalıştıracak. Build, Art.Editor ve insan kabulü açık; sprint PARTIAL.

## 2026-10-08 — Tam PlayMode 295/297; iki odaklı fixture düzeltmesi

Kullanıcı koşusu `Evidence/20261007T233716-121972Z-regression-play/`: total=297 passed=295 failed=2 skipped=0 result=Failed(Child), process exitCode=2, source-preservation changed=[]. Sprint regresyonu PASS değildir.

1. `LootCompatibilityTests.RebindingSlotDoesNotDoubleInvokeClick`: kapalı, paneli yapılandırılmamış ve boş inventory üzerinde seçim 0 bekliyordu; S016 görünür/nonempty slot kuralı -1 döndürdü. Fixture public ConfigureView/Bind ile görünür panel ve gerçek bandage stack kurar. İki Configure sonrası bir click seçer, ikinci click temizler; duplicate callback olursa ilk assertion yine başarısız olur. Private selectedSlot reflection kaldırıldı; public SelectedSlot kullanıldı. Runtime guard gevşetilmedi.
2. `MovementAcceptanceTests.MouseAndCrouchActionsReachCameraAndCapsule`: release sonrası device key hâlâ basılı; event sayısı 2→2, yani failure crouch domain kararından önce event delivery aşamasında. Yerel InputTestFixture kaynakları UnityTest için Set/Press/Release'ın InputState.currentTime ile queue edip player loop'u beklediğini gösteriyor; eski test default runtime timestamp'lı QueueStateEvent ve manuel Update karıştırıyordu. C girişleri fixture Press/Release + yield ile değiştirildi. Device release, reader release, stance/capsule/look beklentileri korundu; tam iki crouch isteği ve release'in toggle üretmemesi assertion'ları eklendi. Bu teşhis yeni koşu ile doğrulanacak; PASS iddiası yok.

`focused-regression-fixes` tek Unity çağrısında yalnız bu iki tam test adını seçer; beklenen 2, otomatik devam/tekrar yok. Production kodu değişmedi. Python AST/scoped diff kontrolü PASS. Unity test NOT_RUN — kullanıcı çalıştıracak. Yeşil odaklı sonuç eski tam koşuyu PASS yapmaz; mevcut regresyon durumu FAIL olarak kalır.

## 2026-10-08 — İki regresyon düzeltmesi 2/2 PASS; bütünleşik tekrar devri

Kullanıcı koşusu `Evidence/20261007T235250-917507Z-focused-regression-fixes/`: LastSignal.PlayModeTests içinde slot-rebind ve mouse/crouch testleri total=2 passed=2 failed=0 skipped=0 result=Passed; process exitCode=0; source-preservation changed=[]. Görünür/dolu slot fixture'ı ve player-loop Press/Release düzeltmeleri odaklı koşuda doğrulandı.

Sıradaki tek kapı tam `regression-play` tekrar koşusudur. Önceki 295/297 FAIL kanıtı korunur; iki ayrı PASS sonucu tam regresyon PASS olarak birleştirilmez. Bu tekrar, test sırası ve paylaşılan Input System yaşam döngüsünün bütün suite içinde etkisini doğrulamak içindir. Bu tur production/test/tool kodu değiştirilmedi. Yeni tam PlayMode koşusu NOT_RUN; kullanıcı çalıştıracak. Art.Editor/build ve insan kabulü açık, sprint PARTIAL.

## 2026-10-08 — Tam PlayMode 296/297; Movement input fixture izolasyonu

Kullanıcı koşusu `Evidence/20261007T235356-149532Z-regression-play/`: total=297 passed=296 failed=1 skipped=0 result=Failed(Child), process exitCode=2, source-preservation changed=[]. Slot-rebind artık tam suite içinde geçti. Kalan test `MovementAcceptanceTests.MouseAndCrouchActionsReachCameraAndCapsule`: C release sonrası cihaz hâlâ basılı. Önceki odaklı 2/2 PASS bu sıra bağımlı sorunu kapatmadı.

Yerel Input System kaynak incelemesi: InputTestFixture yeni InputTestRuntime kuruyor; bu saat sıfırdan ilerlerken InputManager.ShouldDiscardEditModeTransitionEvent Editor'ın enter/exitPlayMode zaman aralığındaki olayları atıyor. Önceki diagnostikte release güncellemesi ilerlediği halde event sayısı değişmiyordu. Bu mekanizma gözlenen sıra/zaman bağımlılığıyla uyumlu; doğrudan discard trace olmadığı için kesin doğrulanmış kök neden olarak sunulmaz.

MovementAcceptanceTests, S016ModalTests'te kullanılan mevcut manager/native clock korunumu düzenine taşındı. Fiziksel etkin cihazlar geçici devre dışı, sentetik keyboard/mouse eklenir; test sonu yalnız bu cihazlar kaldırılıp önceki cihaz/settings/background durumu geri yüklenir. Set/Press/Release StateEvent.From ile doğal timestamp'lı olayları player loop'a kuyruğa verir. Mock runtime ve manuel Update yok. Production input veya assertion gevşetilmedi; cihaz release, reader release ve tam iki crouch isteği kontrolleri korunuyor.

Fixture bütün Movement sınıfını etkilediğinden sıradaki kapı `focused-movement`: mevcut 17 test, tek Unity süreci. Tam 297 test tekrar istenmedi. Python AST ve scoped diff kontrolü PASS. Yeni Movement koşusu NOT_RUN — kullanıcı çalıştıracak. Son tam regresyon FAIL olarak kalır.
