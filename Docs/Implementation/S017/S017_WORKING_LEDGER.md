# S017 — Çalışma ve kabul kaydı

2026-10-08. Kaynak: yerel dirty çalışma ağacı, branch `s012-integrated-graybox-slice`, başlangıç HEAD `7f5ffb783f396f9675c28da18be7021d90a02eb7`. Önceden mevcut S015/S016/scene değişiklikleri korunur. Commit veya temiz-ağaç iddiası yok.

## Kart durumu

| Kart | Uygulama durumu | Kabul |
|---|---|---|
| D161 | Windows KBM zorunlu; Xbox/XInput ve Unity Generic Gamepad aday; kapsam dışı cihazlar ilk audit matrisinde | Fiziksel destek kanıtı NOT_RUN |
| D162 | Analog look, fare/stick birimleri, gamepad hız/deadzone/invert profili, action çakışmaları ve neutral gate uygulandı | Güncel focused-input 6/6 ve flow 9/9 PASS; fiziksel tuning NOT_RUN |
| D163 | SessionFlow üzerinden tek modal otoritesi, UI action sahibi, default/return focus, görünür focus, settings/journal ve scroll navigation uygulandı | Odaklı sahne/controller otomasyonu PASS; görsel ölçek ve fiziksel kabul NOT_RUN |
| D164 | Mevcut domain komutlarıyla inventory split miktarı, revision güvenliği, storage/cargo transfer ve controller odağı uygulandı | Odaklı split/revision otomasyonu PASS; generic equip/consume domain önkoşulu BLOCKED |
| D165 | Anlamlı cihaz takibi, effective binding metni, dar glyph katalog + fallback, button rebind/cancel/reset/persistence uygulandı | Policy 5/5 ve flow 9/9 PASS; fiziksel görsel kabul NOT_RUN |
| D166 | Hareket/loot/recovery olaylarına bağlı profil completed/skipped/reset rehberi uygulandı | NOT_RUN; medikal wound/bandage treatment domain önkoşulu BLOCKED; shelter recovery medikal kabul yerine sayılmaz |
| D167 | Mevcut üretim builder üzerinde Windows64 ve immutable kaynak/build kimliği hazır | Build ve gerçek Windows koşusu NOT_RUN |
| D168 | Windows bulgusu gelmedi; kanıtsız platform değişikliği yapılmadı | NOT_RUN |
| D169 | Drift eşiği, cihaz değişimi, disconnect/focus pause ve neutral gate, abonelik cleanup uygulandı | Odaklı sentetik switch/disconnect PASS; fiziksel kabul NOT_RUN |
| D170 | Gerçek üretim rotası ve aşağıdaki kayıt şablonu hazır | 30–45 dakika canonical slice NOT_RUN |

## Kanıt ayrımı

Dar pause tekrarı [20261008T085205-550991Z-focused-regression](Evidence/20261008T085205-550991Z-focused-regression/): beklenen tek test 1/1 PASS; failed/skipped 0, exitCode 0, kaynak değişikliği yok. XML ve unity.log doğrulandı; runner Ok, C# derleme hatası yok. Settings → Pause → Gameplay ve dünya/input freeze/resume beklentisi doğrulandı. Önceki 46/47 koşusuyla birlikte hedeflenen hatalar dar kapsamda kapandı; bu ayrı koşular tek bir tam regresyon PASS'i sayılmaz. Sonraki kapı regression-play (306 test keşif alt sınırı), mevcut son kaynak ve fixture izolasyonuyla tüm assembly birlikte doğrulanacak.

Odaklı regresyon [20261008T084914-868321Z-focused-regression](Evidence/20261008T084914-868321Z-focused-regression/): 47 test, 46 PASS/1 FAIL; kaynak değişmedi. Önceki InputManager exception, LootPopulation ve Movement hataları bu koşuda tekrarlanmadı. Kalan tek hata S016PauseTests.SoloPauseFreezesWorldAndThreatWhileSettingsStayInteractive: test Settings açıkken tek Resume çağrısından Gameplay bekliyor, gerçek sözleşme önce Settings → Pause. Önceki test uyarlamasındaki CloseSettings yanlışlıkla setup'a eklenmişti; kaldırıldı. Test şimdi ilk Resume sonrası Pause, timeScale=0, gameplay input kapalı ve panel kapalı; ikinci Resume sonrası Gameplay ve dünya saatinin ilerlemesini doğrular. Runtime değiştirilmedi; doğru freeze/resume assertion'ları korunup ara geçiş kapsamı eklendi. Compiler-only PlayMode assembly exitCode 0; kullanıcı dar tekrarı NOT_RUN. Sonraki kapı focused-regression --filter LastSignal.Tests.S016PauseTests.SoloPauseFreezesWorldAndThreatWhileSettingsStayInteractive (1 test).

Tam PlayMode [20261008T083059-589475Z-regression-play](Evidence/20261008T083059-589475Z-regression-play/): 306 keşif, 273 PASS/33 FAIL, source-preservation changed boş. Tam PlayMode FAIL; önceki odaklı PASS sonuçları bu kapının yerine geçmez.

Hatalı sekiz fixture: Combat 4, LootPopulation 9, Movement 17, R01Combat 3, RuntimeSmoke 1, S014WeaponPresentation 9, S016Interaction 3, S016Pause 1 test içerir (toplam 47, gerçek XML keşfinden). Tekrarlayan InputManager monitor exception'ları SessionInput.Update üzerinden eski cihaz action'larının yeniden açılmasına işaret eder. Ortak test isolation helper artık aktif SessionInput/UI sahiplerini cihaz yöneticisi değiştirilmeden önce durdurur. İlgili standalone fixture'lar önceki authored sahne köklerini geçici pasif yapan SceneScope kullanır; runner korunur, kalan kökler InputTestFixture teardown sonrası geri açılır. Bu aynı zamanda default Physics'i paylaşan eski sahne collider'larını loot/movement ölçümlerinden ayırır. Runtime davranışına exception yutma veya test bypass eklenmedi. Loot/Movement hatalarının bu izolasyonla kapanması henüz NOT_RUN.

S016Pause eski doğrudan pause panel beklentisi yeni Settings ekranına uyarlandı; panel/slider ve dünya, combat, health, araç torku, input freeze/resume assertion'ları korunur. Geçici test fixture izolasyonu değişiklikleri compiler-only kontrolünde hatasız. Helper'a gerçek sekiz sınıflı 47 testlik focused-regression kapısı eklendi; tek Unity süreci ve tarihsel evidence koruması kullanır. Sonraki kullanıcı kapısı focused-regression; ardından gereken dar düzeltmeler ve tam regression-play tekrarı. Testler agent tarafından çalıştırılmadı.

Güncel tam EditMode kullanıcı koşusu [20261008T082950-052794Z-regression-edit](Evidence/20261008T082950-052794Z-regression-edit/): 541 test keşfedildi, 541/541 PASS; failed/skipped 0, exitCode 0, source-preservation changed boş. XML ve unity.log doğrulandı; runner Ok, C# derleme hatası yok. Tarihsel yol koruma kaydı aynı koşuda saklandı. Tam EditMode kapısı PASS; tam PlayMode henüz NOT_RUN. Sıradaki kapı regression-play; S016'dan devralınan Movement regresyonu dahil assembly'nin tamamı doğrulanacak.

Güncel kullanıcı koşusu [20261008T082826-106543Z-focused-input](Evidence/20261008T082826-106543Z-focused-input/): altı test keşfedildi, 6/6 PASS; failed/skipped 0, exitCode 0, source-preservation changed boş. XML ve unity.log doğrulandı; runner Ok, C# derleme hatası yok. Eski ProjectSettings mutasyonu nedeniyle açık kalan focused-input kapısı bu yeni koşuyla PASS. Güncel odaklı kapılar: policy 5/5, flow 9/9, input 6/6 PASS; full regression ve fiziksel kabul henüz NOT_RUN. Sıradaki kullanıcı kapısı regression-edit.

Güncel kullanıcı koşusu [20261008T082553-327919Z-focused-flow](Evidence/20261008T082553-327919Z-focused-flow/): beklenen dokuz test keşfedildi, 9/9 PASS; failed/skipped 0, süreç exitCode 0, source-preservation changed boş. XML ve unity.log doğrulandı; C# derleme hatası yok, runner Ok ile tamamlandı. Kurulum sırası ve tek gamepad olayına birleştirme düzeltmeleri sonrası analog, Journal/inventory ve disconnect dahil focused-flow kapısı PASS. Tarihsel FAIL koşuları aşağıda korunur; güncel flow durumunu temsil etmez. Bu sonuç full regression veya fiziksel cihaz/Windows kabulü değildir. Sıradaki kapı focused-input: eski kaynak-mutasyon FAIL'ini son kaynakta temiz koşuyla kapatmak.

Dar kullanıcı koşusu [20261008T082229-885340Z-focused-flow](Evidence/20261008T082229-885340Z-focused-flow/): 0/1 FAIL; doğru runtime action nesnesi doğrulandı, action yine disabled. NonSerialized değişikliğinin bu hatayı çözmediği kesinleşti.

Kök neden yerel test-framework `NUnitExtensions/Runner/TestCommandBuilder.cs` içinde doğrulandı: normal SetUpTearDownCommand önce iç komut olarak kuruluyor (satır 75), EnumerableSetUpTearDownCommand dışına sarılıyor (satır 108). Bu yüzden UnitySetUp sahneyi yükledikten SONRA normal SetUp çalışıp DisableLiveActions ile yeni reader'ı kapatıyordu. Önceki teardown da scene unload öncesinde cihazları kaldırıyordu. S017 fixture artık SetupInput'ı UnitySetUp'ın ilk adımı, CleanupInput'ı UnityTearDown'ın scene unload sonrası finally adımı olarak çağırır; normal SetUp/TearDown attribute'ları kaldırıldı. Geçici reflection tanısı kaldırıldı; tek Journal isteği, callback sonrası Journal ve release sonrası açık kalma davranış assertion'ları korundu. Native clock ve neutral/input sahipliği değişmedi. Journal/SkipTutorial NonSerialized tutarlılığı korunur ama kök neden düzeltmesi olarak sayılmaz.

Bu değişiklik bütün fixture'ın izolasyon sırasını etkilediği için sıradaki en küçük ilgili grup dokuz testlik focused-flow'dur. Önceki yedi PASS yeni fixture'a taşınmaz. Test assembly compiler-only exitCode 0; kullanıcı koşusu NOT_RUN.

Dar kullanıcı koşusu [20261008T081904-934267Z-focused-flow](Evidence/20261008T081904-934267Z-focused-flow/): Journal 0/1 FAIL, kaynak değişmedi. Before enabled=False/neutral=False; held raw device=1, action=0, performed=0. Nötr gate ve cihaz olayının ulaşmaması elendi; gerçek reader Journal action'ı devre dışı kalmış. Yeni Journal/SkipTutorial runtime cache alanlarında mevcut action cache alanlarının aksine NonSerialized eksikti. İki alan aynı NonSerialized sözleşmesine alındı; test, Journal referansının gerçek reader asset'indeki action ile aynı nesne olduğunu da denetler. Bu yaşam döngüsü düzeltmesinin asıl hatayı kapattığı henüz kullanıcı koşusuyla doğrulanmadı. Runtime ve PlayMode assembly compiler-only kontrolü hatasız; Unity test kabulü NOT_RUN. Sıradaki kapı aynı tek Journal testidir.

Dar kullanıcı koşusu [20261008T081516-134949Z-focused-flow](Evidence/20261008T081516-134949Z-focused-flow/): Journal testi 0/1 FAIL; kaynak değişmedi. Mission/Progress ve GameplayActive önkoşulları geçti, JournalRequested sayısı 0. Dolayısıyla hata domain/UI sonrasına değil reader giriş/dispatch aşamasına daraldı. Sonraki tanı aynı testte reader'ın gerçek Journal action referansını, action enabled/controls, neutralRequired, raw shoulder değeri ve performed sayısını basılı örnekte kaydeder. Reflection yalnız test tanısında salt okumadır; map etkinleştirme, neutral bypass veya domain çağrısı yok. Runtime henüz değiştirilmedi. Tanı değişikliği compiler-only exitCode 0; kullanıcı tekrarı NOT_RUN.

Kullanıcı koşusu [20261008T081028-013506Z-focused-flow](Evidence/20261008T081028-013506Z-focused-flow/): 9 test keşfedildi; 7 PASS, 2 FAIL, kaynak değişikliği yok. AnalogStickAndDeadzoneReachTheRealPlayerWithoutMouseScaling Move.y=0; JournalAndInventoryAreReachableByControllerWithSafeBack Journal yerine Gameplay. Flow kapısı FAIL olarak açık.

Analog fixture kök nedeni: aynı input update öncesinde StateEvent.From ile alınan iki tam gamepad snapshot'ının ikincisi ilk stick değişikliğini eziyordu. İki stick aynı olayda yazılacak şekilde düzeltildi. Disconnect testindeki move/fire çifti de tek olay oldu; disconnect öncesi Move/FireHeld assertion'ları eklendi. Runtime analog mantığı veya doğru beklentiler gevşetilmedi. Bu düzeltmenin kullanıcı tekrarı NOT_RUN.

Journal kök nedeni henüz doğrulanmadı. Testte mevcut production mission/Progress ve gameplay önkoşulları, tek JournalRequested olayı, callback sonrası ekran ve release sonrası ekran ayrı assert edilir. Domain çağrısıyla tuş akışı bypass edilmedi. Sonraki dar kapı yalnız JournalAndInventoryAreReachableByControllerWithSafeBack. Değişen PlayMode test assembly'si bağımsız C# compiler ile exitCode 0; Unity test sonucu değildir.

Kullanıcı koşusu [20261008T080902-003754Z-focused-policy](Evidence/20261008T080902-003754Z-focused-policy/): beklenen beş S017PolicyTests testi keşfedildi, 5/5 PASS; skipped/failed 0, süreç exitCode 0, source-preservation changed listesi boş. Policy kapısı PASS. XML ve unity.log incelendi; C# derleme hatası bulunmadı. Logdaki boş üçüncü taraf NavMesh assembly uyarısı ve licensing/Editor kapanış tanıları test veya süreç başarısızlığına yol açmadı. Bu sonuç sahne/controller, tüm profil persistence veya fiziksel cihaz kabulü değildir.

Kullanıcı koşusu [20261008T002246-601828Z-focused-input](Evidence/20261008T002246-601828Z-focused-input/): XML 6/6 Passed, süreç exitCode 0; helper kaynak-koruma FAIL. `ProjectSettings/ProjectSettings.asset` içindeki Standalone define `APP_UI_EDITOR_ONLY;SENTIS_ANALYTICS_ENABLED` → `APP_UI_EDITOR_ONLY` oldu. Önceki hash HEAD dosyasıyla eşleşti. Yerel Unity AI inference paketinin `AnalyticsDefineManager` InitializeOnLoad kodu bu sembolü Editor analytics durumuna göre değiştiriyor. Kullanıcı analytics tercihi değiştirilmedi, dosya geri alınmadı, kaynak-koruma denetimi gevşetilmedi. Helper artık dosyanın önce/sonra metnini de saklıyor.

[Bağımsız C# derleyici logları](Evidence/20261008T080703-201891Z-static-csharp/): Runtime, EditModeTests, PlayModeTests, Art.Editor derlemelerinde derleyici hatası yok. Geçici dizindeki çıktılar mevcut Unity referanslarıyla üretildi; Unity import, analyzer/source generator, IL postprocessing, runtime, test veya build kabulü değildir. Python AST/InputActionAsset JSON ayrıştırması başarılı. Bunlar Unity test PASS olarak sayılmadı.

Devralınan S016: son tam EditMode 530/530 PASS; son tam PlayMode 296/297 FAIL. Sonraki Movement native-clock fixture değişikliği için yeni tam koşu yok. Yeni S017 kaynaklarına bu eski sonuçlar taşınmaz. S0/S1 sayısı henüz ölçülmedi; sıfır iddiası yok.

## Gerçek cihaz ve slice kayıt şablonu

Her koşu için UTC tarih, build kimliği ve source digest; Windows sürümü; CPU/GPU/RAM; çözünürlük/quality; controller tam model/bağlantı türü; rota adımı, beklenen/gerçek sonuç, PASS/FAIL/NOT_RUN/BLOCKED/N/A, log/görsel ve tekrar adımları kaydedilmeli.

Aynı oturumda ana menü → yeni oyun → settings/rebind ve reset → movement/look → loot/inventory split/move/drop → mevcut combat/weapon slots → araç sürüş/cargo → shelter/storage/production/progression → save → quit → yeniden aç/load rotası tamamlanmalı. Tedavi/equip/consume önkoşulları açıkken o adımlar BLOCKED olarak tutulmalı; atlayıp tüm rota PASS denmemeli.

Gameplay/menu/inventory/pause sırasında KBM↔gamepad, held input ile çıkar-tak, alt-tab/focus return ve tekrar eden menü dönüşleri ayrı kaydedilmeli. Kaydedilen ayar/rebind ve tutorial skip/reset yeniden açılışta doğrulanmalı. Drift, analog düşük/yüksek hız, metin/glyph eşleşmesi, focus görünürlüğü, scroll ve kullanılan çözünürlük/UI scale de kapsanmalı.

## Devam sınırı

Sonraki işlem [devirdeki](S017_IMPLEMENTATION_HANDOFF.md) tek regression-play kullanıcı koşusu. S017 kabulü açık; S018 girişi BLOCKED. Genel item/medikal domain eksikleri ve gerçek Windows/fiziksel slice kanıtı tamamlanmadan production-ready veya tüm kapılar PASS beyanı yapılamaz.
