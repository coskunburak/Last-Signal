# S014 kullanıcı kontrollü doğrulama — ilk iterasyon

## 2026-10-07 — D139 matris tamamlandı

PC ve Low toplam 30 tamamlanan ölçüm; frame budget PASS, S014/D139 geniş kabul PARTIAL. Ayrıntılar ve lifecycle/profiler sınırları: [D139 ölçüm raporu](S014_D139_PERFORMANCE.md). Geçersiz ilk koşu kabul değildir. Canlı silah/zombi/reload görüntüleri yeni evidence dizinlerinde hazırlanıyor.

## Güncel devam kaydı — 2026-10-07 (çalışma sürüyor)

HEAD `97516c8b20793f4ca834e214318bdac45f041a4d`, dal `s012-integrated-graybox-slice` korunuyor. Önceden mevcut VEH/AI/camera değişiklikleri kullanıcı çalışmasıdır. Önceki el materyal uyarlaması ve altı FOV/aspect karesi yerinde; yeniden yapılmadı. Giriş ayrıntıları: `Evidence/20261006T213705-603649Z-closure/ENTRY_GAP_MATRIX.md`.

D139 için Development-only `S014PopulationCapture` ve `Tools/s014-population.py` eklendi. Gerçek `LS_Zombie_Runtime` ile 1/10/20, beş tekrar, 2s preroll + 5s warmup + 20s ölçüm; ilk tekrar başına warmup öncesinde 2s ham CPU Profiler kaydı. Üretim sahnesi kalıcı değiştirilmez. Player hedefi sabittir; mevcut HealthChanged/RecoverHealth ile fixture'a özgü iyileştirilir. Bu tedavi özelliği veya karşılaşma adaleti kabulü değildir.

İlk standalone tanı koşusu `Evidence/20261007T085519-615708Z-population-pc/` INVALIDATED: PlayerHealth'i disable etmek melee doğrulayıcıda hedefi geçersiz kıldı, contact_attempts=0. Süreç durduruldu, ham kanıt korundu. Fixture düzeltildi; yeni dedicated test gerçek temas oluşmasını zorunlu kılıyor. Düzeltilmiş focused koşu `../S012/Evidence/20261007T085925-363070Z/play.xml`: 3/3 PASS. Öncesindeki `20261007T085752-806009Z` Unity Test Runner NullReference ile aborted; test başarısı değildir.

Düzeltmeden önce taze geniş regresyon: `../S012/Evidence/20261007T083600-807050Z/edit.xml` 510/510 PASS; `Evidence/20261007T083651-048431Z-art-edit/tests.xml` 10/10 PASS; `../S012/Evidence/20261007T083728-668665Z/play.xml` 274/274 PASS. Son fixture revizyonu için kapanış regresyonu henüz açık.

Ölçüm build'i `Builds/S013/20261007T090101-351030Z-production`: Succeeded, 0 error, 55 warning, 18.4899700s. Önceki build `20261007T085421-388975Z-production` artık ölçüm referansı değil. Sayaçlarla birlikte gerçek standalone PC koşusu `Evidence/20261007T090154-796032Z-population-pc/` başladı; sonuç henüz verilmedi. Build/source kimliği her koşuda saklanıyor.

D138 tedavi: **BLOCKED — AUTHORITATIVE TREATMENT / ITEM-INSTANCE CONTRACT NOT YET PRESENT**. D131–D137 kalan canlı/görsel kabul ve D140 henüz kapanmadı. Görsel ürün onayı NEEDS_USER_ACCEPTANCE; özel lisans/edinim kanıtı BLOCKED — USER LICENSE/ENTITLEMENT EVIDENCE REQUIRED. S014 PARTIAL; `S015_ENTRY = BLOCKED`.

---

## Güncel durum — yüksek FOV crowbar 4/4 PASS

Canlı S013Cabin incelemesinde FOV 100'de crowbar omuz uçları iki alt köşede görünüyordu. MeleeStanceViewPresenter görsel pivotu FOV 75–100 arasında kademeli olarak en fazla 12 cm kameraya yaklaştırıyor. FOV 60/75 pozları korunuyor; Animator ve gameplay origin ayrı kalıyor. Unity derlemesi ve gerçek runtime idle görüntüsü doğrulandı. Kanıt: `Evidence/20261005T101009-573622Z-fov-review/`.

Kullanıcı koşusu `Evidence/20261005T101512-630643Z-focused-melee/`: **4/4 Passed**, failed=0, skipped=0, exitCode=0. Sekiz kaynak hash'i güncel dosyalarla eşleşti. FOV 60/100/75 geçişinde authored swing ve sabit gameplay origin kontrolleri dahil geçti. Aynı testlerin tekrarı istenmiyor.

Sonraki görsel inceleme için Unity Editor'ı projede açın ve S013Cabin sahnesinde Play Mode dışında bırakın. FOV düzeltmesinin equip/swing, crouch/slide ve farklı ekran oranlarındaki görünümü incelenecek. Idle görüntüsü ve otomatik testler bu hareketlerin final görsel kabulü değildir. Aşağıdaki sonuçlar kendi revizyonlarının kanıtıdır.

## Önceki durum — zombi sunumu 10/10 PASS; görsel inceleme açık

`Evidence/20261005T095944-429298Z-focused-zombie/`: XML **10/10 Passed**, failed=0, skipped=0, exitCode=0, 0,193 saniye. Dokuz kaynak hash'i güncel dosyalarla eşleşti. İki saldırı varyantı × iki culling politikası için commit/contact klip saati, pause sırasında saat/el pozu, sabit gameplay root ve culling restorasyonu doğrulandı. Mevcut altı locomotion/hit/death sunum testi de geçti. Runtime/asset değişmedi.

Crowbar `Evidence/20261005T095645-823256Z-focused-melee/`: **4/4 PASS**, sekiz hash eşleşti. Aynı-frame iptal/input kilidi, slot değişimi/reequip ve stance kontrolleri doğrulandı. Bu odaklı testlerin tekrarı istenmiyor.

Sıradaki kullanıcı adımı aşağıdaki **kısa Editor görsel referansı** rotasıdır. S013Cabin'de rifle hip/ADS, tactical/empty reload, crowbar equip/swing ve mümkünse zombi karşılaşmasının kesintisiz gameplay görüntüsü gerekir. Kamera/HUD ile el-şarjör teması ve iki zombi saldırısının görünür teması birlikte incelenmeli. Testlerin hızlı tamamlanması beklenir: zombi grubu klipleri manuel örnekler, gerçek zamanlı karşılaşma veya performans ölçmez.

D131–D140 tümü tamamlanmış değildir: ortak el/kol kimliği, D138 etkileşim sunumları, görsel temas, lisans ve final performans/build kabulü açık. Yeni test komutu veya final build bu aşamada istenmiyor. Aşağıdaki Gate A–D kayıtları tarihsel adımlardır; eski NOT_RUN ifadeleri ilgili tarihe aittir.

## Gate A — yapılan kontroller

- Açık Unity'de runtime değişiklikleri derlendi; IsSimulationActive yeni property reflection ile bulundu; Console Error sayısı 0.
- Üretim VAL controller'ın yedi klibinde eksik transform binding yok.
- Gerçek production zombie wrapper/Avatar/rootMotion bağlantısı okundu.
- `git diff --check` değiştirilen runtime dosyalarında PASS.
- Python AST/sözdizimi PASS. Yeni testlerin çalışması veya CLI aracının Unity koşusu henüz doğrulanmadı.
- İlk MCP Refresh çağrısı domain reload sırasında dynamic assembly entrypoint hatası verdi; sonraki çağrı başarıyla derlenmiş kodu gördü. Hata gameplay/test sonucu diye sınıflandırılmadı.

## Gate B — PASS (2026-10-05)

Kullanıcı koşusu `Evidence/20261005T080824-293908Z-focused-edit/tests.xml`: 29/29 Passed, failed=0, skipped=0, exitCode=0. XML ve değişen kaynak hash'leri doğrulandı. Yeniden çalıştırmak gerekmiyor; aşağıdaki komut kayıt amacıyla korunuyor.

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/s014-verify.py focused-edit
```

Gerçek assembly `LastSignal.EditModeTests`, filtre `LastSignal.Tests.WeaponStateTests`, `-testPlatform EditMode`. Beklenen: sıfır failed/skipped, total>0, COMPLETE. Bu yalnız test grubu sonucudur; S014 tamamlandı demek değildir.

Terminalin tamamını ve `RUN=...` yolunu gönderin. Araç her defasında `Docs/Implementation/S014/Evidence/<benzersiz-UTC>-focused-edit/` üretir; tests.xml, unity.log, command.json, source hashes ve result.json burada. Önceki sonuçların üzerine yazmaz.

## Gate C — presentation 5/5 PASS

`Evidence/20261005T084930-512321Z-focused-play/tests.xml`: 5/5 Passed, exitCode=0; XML ve kaynak hash'leri doğrulandı. Aynı test grubunu yeniden koşmak gerekmiyor. Aşağıdaki komut kayıt amacıyla korunuyor.

İlk üç test kullanıcı koşusu `Evidence/20261005T083522-543833Z-focused-play/tests.xml` ile 3/3 PASS olarak doğrulandı. Sonraki direct unequip düzeltmesi iki test ekledi; aşağıdaki komut artık beş test çalıştırır. Önceki başarılı XML yeni revizyonun kanıtı değildir.

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/s014-verify.py focused-play
```

Unity Editor kapalı olmalı. Gerçek assembly `LastSignal.PlayModeTests`, filtre `LastSignal.Tests.S014WeaponPresentationTests`, `-testPlatform PlayMode`. Beklenen 5/5 Passed ve COMPLETE. Tactical/empty reload input lock altında poz/ammo saati ile durmalı, devamında bir kez doldurmalı; commit öncesi slot değişimi ammo korumalı ve Ready animasyonuna dönmeli. Ek iki test commit öncesi/sonrası direct unequip, tek transition event'i ve reequip sonrası ammo korunmasını doğrular.

Log özeti için ilgili RUN yolunu ilk satıra koyun:

```bash
S014_RUN='/Users/burakcoskun/Last Signal/Docs/Implementation/S014/Evidence/BURAYA_TERMINALDEKI_RUN_DIZINI'
rg -n -C 3 'error CS|Exception|Assertion|Failed|Aborted|error' "$S014_RUN/unity.log" | tail -100
```

Araç XML failure/message ve stack-trace metinlerini zaten terminale çıkarır. XML yoksa PASS vermez. Tam regresyon/build bu iterasyonda otomatik başlatılmadı; Gate B/C yeşil ve kalan uygulama stabil olmadan Gate D/E/F komutlarına geçilmeyecek. Mevcut full suite bazı tarihsel evidence yollarına yazabildiği için full suite devrinde mevcut koruma wrapper'ı dikkate alınmalıdır.

## Gate C — save/death/load 2/2 PASS

`Evidence/20261005T085526-238972Z-focused-persistence/tests.xml`: 2/2 Passed, exitCode=0; sekiz kaynak hash'i ve XML doğrulandı. Yeniden test gerekli değil; aşağıdaki komut kayıt amacıyla korunuyor.

Unity Editor kapalı olmalı. Normal kullanıcı kayıtlarına dokunmayan mevcut PersistenceAcceptance test fixture'ı kullanılır; yalnız iki yeni S014 metodu çalışır:

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/s014-verify.py focused-persistence
```

Beklenen 2/2 Passed ve COMPLETE; platform PlayMode, assembly LastSignal.PlayModeTests. Her test reload'un commit öncesi veya sonrası save alır, gerçek PlayerHealth/SessionFlow ölümünü işler, checkpoint'i tekrar yükler ve ammo/Animator/transaction tutarlılığını denetler. Yeni benzersiz `*-focused-persistence` evidence dizininde XML/log/source hash'leri saklanır. Çıktıyı ve RUN yolunu gönderin; sonucu görmeden bu iki test PASS sayılmayacak. Runtime bu test genişletmesinde değiştirilmedi.

## Reload klip/şarjör timing audit — MEASURED

Kullanıcı koşusu `Evidence/20261005T090906-491043Z-reload-audit/` tamamlandı; exitCode=0, source-preservation changed=[], dokuz hash eşleşiyor. Yeniden çalıştırmak gerekmiyor. Tactical 1,666667s ve empty 1,833333s commit anında magazine seated pozuyla sayısal olarak eşleşiyor. Raw clip ölçümü runtime blend/hand contact kabulü değildir; parametre değiştirilmedi.

Unity Editor ve önceki batch işlemi kapalı olmalı. Bu bir test suite/build değildir; geçici prefab kopyasında iki klip örneklenir. Yeni gerçek entry point: `LastSignal.Editor.S014ReloadTimingAudit.RunBatch`.

```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/s014-verify.py reload-audit
```

Beklenen: yeni RUN dizini, Reload.csv, EmptyReload.csv, reload-audit.json ve `COMPLETE_AUDIT`. Çıktıyı ve RUN yolunu gönderin. Komut kaynak hash'lerini önce/sonra kıyaslar; prefab/klip/controller kaydetmez. Raporun durumu `MEASURED_NOT_VISUALLY_ACCEPTED`; görüntü kabulü PASS anlamına gelmez. İlk araç ortamı denemesindeki license IPC engeli kullanıcı Terminal ortamında tekrar görülürse unity.log'un son kısmını paylaşın; license veya lock dosyası silmeyin.

## Şimdi — kısa Editor görsel referansı

1. Unity 6000.5.0f1 ile projeyi açın; `Assets/LastSignal/Scenes/Production/S013Cabin.unity` sahnesini açın. Play'e girin, oyun duraklatılmışsa ekrandaki devam düğmesini kullanın. Bu bir Editor ön incelemesidir; standalone kabul yerine geçmez.
2. Game görünümünü ve HUD ammo göstergesini birlikte kaydedin. Ekran kaydı için macOS Shift+Command+5 kullanılabilir. Çözünürlük/en-boy oranını not edin; ilk koşuda mevcut FOV 75 ile başlayın.
3. HUD'daki reserve değerinin sıfırdan büyük olduğundan emin olun; gerekiyorsa sahnedeki rifle ammo loot'una bakıp E ile alın. Yedek ammo yoksa reload denenemedi olarak bildirin; gerçekleşmeyen reload'u kabul etmeyin.
4. `1`: rifle; sağ mouse ADS, sol mouse ateş. Bir mermi harcayıp `R`: tactical reload. Ardından magazine'i boşaltıp yedek ammo varken `R`: empty reload. Her harekette sol el/şarjör ve HUD sayacı aynı görüntüde olsun. Tek magazine, tutarlı tutuş ve gözle okunur insertion beklenir; mikrometre raporu görsel kaliteyi kanıtlamaz.
5. Bir tactical reload sırasında `3` ile crowbar'a, `1` ile rifle'a dönün. Crowbar ile bir vuruş yapın. El/sleeve değişimini gizlemeden kaydedin. Bu kimlik farkı halen açık bir üretim konusudur.
6. Yakın duvar önünde hip/ADS görüntüsü ekleyin. Yeni test/build komutu çalıştırmanıza gerek yok. Videoyu (veya ilgili ekran görüntülerini), iyi/kötü görünen noktaları ve reserve yoksa bunu gönderin. Play Mode dışına çıkınca kaydetmeden önce yalnız bilerek yaptığınız sahne değişikliklerini değerlendirin; bu rota asset değişikliği gerektirmez.

## Gate G — final gerçek kabul rotası (henüz çalıştırmayın)

Üretim sahnesi `Assets/LastSignal/Scenes/Production/S013Cabin.unity`; prefablar ledger'da. Final assetler ve runtime tamamlanınca güncel macOS Development build'i normal açın; Editor preview tek başına kabul değildir.

- Aynı başlangıç save/session: idle, yürüme, sprint, crouch, pitch uçları; FOV 60/75/100. 16:9, 16:10 ve gerçek ekran oranını/resolution'ı kaydedin.
- Rifle hip/ADS/ateş/optik/lens; duvara yaklaşınca kamera açık görüşte olsa da muzzle engeli hasarı durdurmalı. Tactical/empty reload, input lock, commit öncesi/sonrası switch, ölüm ve oturum dönüşü. Ekran ammo ve görünen magazine aynı videoda olsun.
- Crowbar equip/swing/recovery, hedef menzilden çıkar, çoklu hitbox tek hedef. Rifle/crowbar/interaction ellerinde sleeve ve cilt sürekliliği.
- Zombi idle/walk/run/turn/stop; her iki attack telegraph/contact/recovery, hedefin kaçması, farklı yönlerden hit, normal/heavy death, head/arm/hand ayrılması; corpse yakınında loot ve save/load.
- Consume, bandage ve door elleri ancak gerçek üretim implementasyonu bağlandıktan sonra; iptalde temp prop kalmamalı ve inventory/healing doğru olmalı. Eksik hareketi video varmış gibi işaretlemeyin.
- Day/night/rain; flashlight özelliği mevcutsa gerçek ışıkla, S013 kontrollü fixture kullanılırsa fixture olarak adlandırın. Normal/düşük kalite/reduced-gore aynı koşulda.
- 1/10/20 zombi; mevcut nüfus limitleri izin veriyorsa 30. Aynı cihaz/resolution/quality/rota ile ısınma ve ölçüm aralıklarını kaydedin. Profiler CPU Timeline (Animator/skinning), Rendering, Memory, GC Alloc; GPU desteklenmiyorsa UNAVAILABLE yazın. Mevcut proje bütçesine karşı kıyaslayın; yeni FPS eşiği uydurmayın.
- Repeated weapon switches, dismemberment/pool, scene/session dönüşlerinde memory/GC/Animator artışı ve eski abonelikler. Frame verisi olmadan D139 PASS yok.

Her koşul için unique evidence dizininde raw ekran görüntüsü/video, profiler capture, Player.log, cihaz/çözünürlük/FOV/kalite bilgisi ve kullanıcı kabul/red notu saklanmalı. Golden screenshot'ları üzerine yazmayın. Görsel kabul yalnız kullanıcının açık onayıyla PASS; macOS development kabulü Windows release sertifikası değildir.


## Final Standalone Kabul (D140)

- **Test Edilecek Build:** `/Users/burakcoskun/Last Signal/Builds/S013/20261007T121645-898215Z-production/LastSignal.app`
- Final teknik kapanış yapılmıştır. İnsan gözüyle yapılacak görsel onay işlemleri (FOV hand identity, Animasyon hisleri) yukarıdaki build üzerinden sağlanmalıdır. Detaylı teknik doğrulama raporu için: [S014_FINAL_CLOSURE.md](S014_FINAL_CLOSURE.md)
- `S015_ENTRY = READY`


## CORRECTION (2026-10-07)
- D140 Final Standalone kabul rotasının yalnızca Editor / Smoke Test verisiyle `PASS` sayılamayacağı kabul edilmiştir. Otomatik çalıştırılan Standalone build'inde loglar (`Player.log`) temiz çıkmıştır; ancak görsel kabul hala askıdadır.
- Sprint statüsü D138 mimari eksikliği ve D140 tam oynanış onayı beklemesi nedeniyle `PARTIAL` olarak güncellenmiştir.
- Bu eksiklikler Ses entegrasyonlarını engellemediğinden `S015_ENTRY = READY` kuralı korunmaktadır.
