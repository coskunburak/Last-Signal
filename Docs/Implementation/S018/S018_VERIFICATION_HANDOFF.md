# S018 doğrulama devri — canonical uygulama eklendi; son testler NOT_RUN

2026-10-09. Kullanıcının sıralaması: uygulanabilir hazırlık/implementation önce; tüm doğrulama en son ve kullanıcı tarafından. Bu tur test, derleme, build, oyun veya kanıt aracı çalıştırılmadı. G5 **BLOCKED**, öneri **RETEST**, S019 **BLOCKED**.


## Güncel öncelik — 2026-10-09 canonical kaynak değişti

Owner eksik oyun sistemlerinin uygulanmasını onayladı. Su/yemek tüketimi, kanama/bandaj, çanta equip/kapasite, HUD ve versioned save extension uygulandı. [Değişiklik ve invariant kaydı](S018_FIX_TRACEABILITY.md#2026-10-09--owner-onaylı-canonical-önkoşul-uygulaması). IMPLEMENTED / NOT_VERIFIED; bu tur test/compile/build çalıştırılmadı.

**QA1 artık tarihsel baseline.** Aşağıdaki QA1 hash ve PowerShell komutları yeni kaynakların Windows kabulü için kullanılamaz. QA1 silinmedi/değiştirilmedi. Yeni QA2 ancak yeni teknik testler ve kaynak-korumalı build sonrasında hash alacak; şimdiden hash veya PASS atanmadı. Eski kayıtlar geri uyum yolu üzerinden açılır; yeni survivalVersion=1 kayıtları eski binary'ye geri yüklemeyin, ayrı kullanıcı profili/fixture kullanın.

### Uygulama sonrası son doğrulama kuyruğu — yalnız kullanıcı yürütür

Aktif adım (2026-10-09): **QA2 Round 8 — windows-build**, NOT_RUN, kullanıcı yürütmesi bekleniyor. Unity Editor kapalı kalmalı. Kullanıcı komutu önce Assets/Packages/ProjectSettings/Tools için Git dışında source-snapshot klonu alır; builder yeni immutable çıktı dizini üretir. QA2 paket ID/hash henüz atanmadı.

QA2 Round 7: [koşu](../S017/Evidence/20261008T215622-621307Z-focused-art/), [terminal](Evidence/qa2-round7-hh1f2x/terminal.txt). Art Editor XML **10/10 PASS**, failed/skipped=0, süre 0.3787857 s, Unity/wrapper exit=0, source-preservation changed=[]. Güncel kaynaklar koşu manifestiyle eşleşiyor. Yeni kaynakta tam EditMode 554/554, PlayMode 314/314 ve Art Editor 10/10 geçti. Kaynak kod değişmedi; yeni Windows build/platform ve insan/kapasite kabulü açık.

QA2 Round 6: [koşu](../S017/Evidence/20261008T214103-002221Z-regression-play/), [terminal](Evidence/qa2-round6-jhLQ24/terminal.txt). Tam PlayMode XML: **314/314 PASS**, yeni S018SurvivalPlayTests 8 vaka dahil; failed/skipped=0, süre 826.5420145 s, Unity/wrapper exit=0, source-preservation changed=[]. Güncel kaynaklar koşu manifestiyle eşleşiyor. Böylece yeni kaynakta tam EditMode 554/554 ve tam PlayMode 314/314 geçti. Kod değişmedi; aynı regresyonlar yeniden istenmiyor. Art Editor, QA2 build, gerçek Windows/process quit-load ve insan/kapasite kabulleri açık.

QA2 Round 5: [koşu](../S017/Evidence/20261008T214005-960072Z-regression-edit/), [terminal](Evidence/qa2-round5-s05mXO/terminal.txt). Tam EditMode XML: **554/554 PASS**, yeni S018SurvivalTests 13 vaka dahil; failed/skipped=0, süre 2.6415827 s, Unity/wrapper exit=0, source-preservation changed=[]. Güncel kaynaklar koşu manifestiyle eşleşiyor. Kaynak kod değişmedi; aynı EditMode kapısı yeniden istenmiyor. Tam PlayMode, Art Editor, QA2 build ve insan/platform kabulleri açık.

QA2 Round 4: [koşu](../S017/Evidence/20261008T213829-886075Z-focused-survival-play/), [terminal](Evidence/qa2-round4-I3tzJG/terminal.txt). Beklenen S018SurvivalPlayTests sınıfı XML'de 8/8 PASS, failed/skipped=0, süre 16.3599742 s, Unity/wrapper exit=0, source-preservation changed=[]. Kayıtlı kaynak hashleri güncel dosyalarla eşleşiyor. Yeni tüketim/bandaj/çanta/save-load dar entegrasyon kapısı PASS. Bu sonuç gerçek Windows process quit/restart veya dış oyuncu kabulü değildir. Kaynak değişikliği yapılmadı.

QA2 Round 3: [koşu](../S017/Evidence/20261008T213726-914789Z-focused-survival-edit/), [terminal](Evidence/qa2-round3-DnQxpM/terminal.txt). Beklenen S018SurvivalTests sınıfı XML'de 13/13 PASS, failed/skipped=0, Unity ve wrapper exit=0, source-preservation changed=[]. Kayıtlı kaynak hashleri güncel dosyalarla eşleşiyor. Round 2 analytics define mutasyonu tekrarlanmadı; dar domain/save kapısı bu snapshot için PASS. Agent test çalıştırmadı ve kaynak kod değiştirmedi. PlayMode/full regression/build/platform/insan kabulleri açık.

QA2 Round 2: [koşu](../S017/Evidence/20261008T213549-958708Z-focused-survival-edit/), [terminal](Evidence/qa2-round2-1PFEOK/terminal.txt). XML'de beklenen S018SurvivalTests sınıfının 13/13 vakası PASS, failed/skipped=0, Unity exit=0; wrapper exit=1, toplam kapı **FAIL**. Tek manifest farkı `ProjectSettings/ProjectSettings.asset`: Standalone define `APP_UI_EDITOR_ONLY;SENTIS_ANALYTICS_ENABLED` → `APP_UI_EDITOR_ONLY`. Güncel settings koşu-sonrası kopyayla byte olarak aynı; pre-run manifestte diğer dosyalar aynı. Yerel inference AnalyticsDefineManager başlangıçta derleme koşulları ve EditorAnalytics.enabled durumuna göre bu sembolü değiştiriyor; gözlenen fark bu mekanizmayla uyumlu, hangi koşulun tetiklediği ayrıca kanıtlanmış değil. Warning stack'indeki EmitExceptionAsError metni tek başına derleme hatası değil; boş vendor NavMesh assembly uyarısına ait. Agent analytics tercihini/define/paketleri değiştirmedi, kaynak kontrolü istisnası koymadı. Bir sabit-kaynak tekrar yapılacak; yeniden mutasyon varsa kör tekrar yerine ortam başlangıç koşulu incelenecek.

QA2 Round 1: kullanıcı offline evidence testlerini çalıştırdı; **13/13 PASS**, 0.018 s, exit=0. [Terminal](Evidence/qa2-round1-PwLDYL/terminal.txt), [exit code](Evidence/qa2-round1-PwLDYL/command-exit.txt) yerel dosyalardan incelendi. Bu sonuç yalnız offline kayıt aracına aittir; Unity derlemesi, gameplay, Windows ve G5 kabulü değildir. Bu koşuda source manifest alınmadı; kaynak korunumu hash ile doğrulandı iddiası yok.

1. `python3 -m unittest discover -s Tools/tests -p 'test_s018_evidence.py'` — offline kayıt doğrulama.
2. `python3 Tools/s017-verify.py focused-survival-edit` — 13 domain/save vakası.
3. `python3 Tools/s017-verify.py focused-survival-play` — 8 production scene vakası.
4. `python3 Tools/s017-verify.py regression-edit`, ardından `regression-play`, ardından `focused-art` — önceki suite + yeni kaynak regresyonu; eski sayılar minimum baseline, güncel XML belirleyici.
5. Başarılı teknik sonuçlardan sonra `python3 Tools/s017-verify.py windows-build`; ayrı immutable QA2 paket/manifest/checksum hazırlanır. Yeni hash olmadan QA1 komutu QA2 diye kullanılmaz.
6. QA2 ile gerçek Windows/quit/restart/load, input/gamepad/UI/art/audio/performance ve canonical rota. Paket ID/hash yeniden devre yazılır. Sonra frozen PT1, 5–8 gerçek kişi, synthesis, gerekirse bulguya bağlı fix/retest ve capacity/owner G5 kararı.

Komutlar kuyruktur; tek otomatik zincir değildir. İlk failure veya kaynak mutasyonunda çıktıyı incelemeden sonraki kapıya geçilmez. Kaynak koruma ve historical evidence koruma gevşetilmedi. Test öncesi hedeflenen su/yemek, bandaj ve çanta implementasyonu tamamlandı; henüz bu uygulamanın doğrulanmış olduğu iddia edilmiyor. Kod/diff incelemesi yapıldı; git diff --check biçim sorunu bildirmedi. Derleme dahil fonksiyonel doğrulama NOT_RUN.

## Önceki hazırlık teslimi ve gerçek günlük kabul

D171 protokol, anonim katılım slotları ve görüşme formu tamamlandı. D172 QA1 Windows paketi ve kaynak kimliği hazır; dış pilot PT1 freeze kabulü açık. D173–D177 oturum, analiz, kontrollü fix ve retest sözleşmeleri; D178–D179 gerçek maliyet toplama ve kapsam karar tutanağı; D180 G5 raporu hazır. Günlük kartların gerçek durumu [kanonik S018 tablosunda](../../Last_Signal_Production_Plan_v1.0_TR/sprints/S018.md).

D173/D174 ve D177 bizzat insan testidir. D175 gerçek kayıtlara, D176 gerçek bulgulara, D178–D179 ölçülmüş üretim maliyetine ve owner kararına bağlıdır. Bu kartların kabulü test öncesinde belge üreterek tamamlanamaz. D171 dışında kalan dokuz kart PARTIAL/NOT_RUN/BLOCKED; hazırlık tamamlanması sprint kabulü anlamına gelmez.

Owner Windows erişimini teyit etti; Round 7 ertelendi. İlk kabul klavye/fare; gamepad modeli/kapsamı açık. 2–3 haftada yaklaşık 1 saat beyanı yalnız oynama/manuel testtir, üretim kapasitesi değildir. Dış oyuncu sayısı 0; accepted üretim maliyeti örneği n=0. Otomatik davet, upload veya hatırlatıcı yok.

## Offline kayıt aracı — uygulanmış, doğrulaması NOT_RUN

`Tools/s018-evidence.py` yalnız yerel insan kayıtlarını işler. `form` boş NOT_RUN form üretir; `validate` şema, rıza, zaman, müdahale ve kaynak dosyalarının SHA-256 tutarlılığını denetler; `summarize` doğrulanmış kayıtları aday/hash/platform/segment/fresh-repeat/kapsam bazında ayırır. Hash doğruluğu insan beyanının doğruluğunu ispatlamaz. G5 daima NOT_EVALUATED; otomatik fun, başarı iyileşmesi veya üretim kapasitesi iddiası yok.

Mevcut çıktının üzerine yazılmaz. Withdrawn rıza, owner QA, yinelenen oturum ve bir kişiye birden fazla FRESH kayıt özete alınmaz. Kaynak notları yerel kalır; araç upload yapmaz. Gerçek katılım oluşmadan T01–T08 form üretmek katılım sayılmaz. Kullanım ve iç liste nesnelerinin alanları [kitte](S018_PLAYTEST_KIT.md#offline-kayıt-aracı).

`Tools/tests/test_s018_evidence.py` yalnız geçici dizinde sentetik unit fixture oluşturur; gerçek Evidence klasörüne oyuncu sonucu yazmaz. Testler hazırlanmıştır, çalıştırılmamıştır. QA1 ZIP değiştirilmedi; canonical uygulama turunda oyun kaynakları değişti. Yeni offline araç/test dosyaları Round 6 full-source manifestine dahil değildir; Round 6 digest yalnız dondurulmuş snapshot'a aittir.

## Önceki QA1 sırası — yukarıdaki QA2 kuyruğu tarafından güncellendi

1. Owner offline araç testlerini çalıştırır: `python3 -m unittest discover -s Tools/tests -p 'test_s018_evidence.py'`. Çıktı ve exit code saklanır; başarısızlık varsa araç kullanıma açılmaz.
2. Aşağıdaki Round 7 ile aynı QA1 hash üzerinde gerçek Windows açılış/input/render ve process quit/restart/load kabulü yapılır. Desteklenecek gamepad, görsel/ses ve performans kabulü ilgili gerçek donanımda kaydedilir.
3. Eksik canonical equip/consume/treatment ve asset lisans/edinim kayıtları kapatılır veya owner açıkça dar araştırma kapsamı seçer; dar araştırma tam slice/G5 kabulü yerine geçmez. PT1 freeze ancak gerekli kabullerle açılır.
4. D173–D175: 5–8 gerçek oyuncu, iki deneyim grubu, 30–45 dakika; aynı adayda kayıt ve kanıta dayalı sentez. D176 yalnız bu bulguların en yüksek etkili düzeltmelerini yapar; ardından ilgili teknik test/regresyon/build ve D177 insan retesti gerekir. Gelecekteki fix'in testini bugünden tamamlamak mümkün değildir.
5. D178–D180: gerçek tam maliyet örnekleri, sürdürülebilir saat/runway, owner kapsam kararı ve bütün kapı kanıtları değerlendirilir. Sonuç CONTINUE/SIMPLIFY/RETEST olarak kaydedilir.

Tarihsel PASS'ler aşağıdaki snapshot'larına aittir. Sırf belge değişti diye Unity paketini yeniden üretmek gerekmez; runtime/ayar/fixture değişirse ilgili teknik kapsam yeniden belirlenir. Son doğrulama, aşağıdaki eski komutların tümünü körlemesine tekrar etmek anlamına gelmez.

## Round 6 — doğrulanmış build

[20261008T104258-416000Z-windows-build](../S017/Evidence/20261008T104258-416000Z-windows-build/): build PASS; Succeeded, Errors=0, Warnings=495, Duration=8.616686 s, process exit=0, changed=[]. Unity 6000.5.0f1, Windows64 Development, Version=0.1.0, S013Cabin. [Transcript](Evidence/round6-vQGRlm/terminal.txt).

Tam kaynak snapshot karşılaştırması [round6-review](Evidence/20261008T104414-917756Z-round6-review/comparison.json): Assets/vendor/meta, Packages, ProjectSettings ve Tools dahil **18.614 dosyada fark yok**. Aynı dizinde tam önce/sonra SHA-256 manifestleri var. Önceki analytics define mutasyonu bu build'de tekrarlanmadı; kaynak koruma istisnası yok. 495 warning: 493 shader başlangıç mesajı (çoğunlukla Sentis/inference), 1 boş vendor NavMesh assembly, 1 RuntimePipelineConfig eksikliği. Devam satırları ayrı warning sayılmadı; gerçek Windows görsel kabulü açık. Bu kaynak için yeni test/runtime değişikliği yapılmadı.

## Sabit owner QA artifact

- ID: **S018-QA1-Win64**; dış pilot PT1 değildir, NOT_PILOT_READY.
- Kaynak build: `Builds/S017/20261008T104258-416000Z-windows-build/`.
- Yerel paket: `Builds/S018/S018-QA1-Win64-20261008T104258-416000Z-windows-build/LastSignal-Windows-QA1.zip`.
- SHA-256: `62a69a653e52098201cd9d045104d7bc48b32fcb2b2fe77463cafcb31c26c448`.
- Aynı dizin `candidate.json` (tüm build dosyalarının hashleri ve full source digest), `SHA256.txt`. ZIP içinde `LastSignal/` altında executable ve tüm DLL/data klasörleri birlikte bulunur. Tek exe kopyalamayın. Harici upload yapılmadı.
- Tam kaynak clone `Evidence/round6-vQGRlm/source-snapshot/` içinde, Git dışında korunur; kalıcı yedek owner sorumluluğunda.

## Tarihsel QA1 ROUND 7 — yeni kaynak kabulü için kullanılmaz

Bu owner teknik QA oturumudur; 5–8 dış oyuncu veya doğal 30–45 dk eğlence ölçümü değildir. Windows sürümü, CPU/GPU/RAM, resolution, quality, FOV, mouse sensitivity ve klavye/fareyi not edin. Gerekirse ekran görüntüsü alın; görsel/ses/readability kusuru ile crash/progression kusurunu ayırın. Test için ayrı Windows kullanıcı profili tercih edin; mevcut kişisel save varsa üzerine yazmadan önce yedekleyin. Kişisel save silmeyin.

ZIP'i Windows'a owner aktarır. PowerShell'de ZIP hashini kontrol edin (tam yolu sorar):

```powershell
$S018Zip = Read-Host 'ZIP dosyasinin tam yolu'
$S018Hash = (Get-FileHash -LiteralPath $S018Zip -Algorithm SHA256).Hash
if ($S018Hash -ne '62a69a653e52098201cd9d045104d7bc48b32fcb2b2fe77463cafcb31c26c448') { throw 'Paket hash uyusmuyor; calistirmayin.' }
$S018Dest = Join-Path (Split-Path -LiteralPath $S018Zip) ('S018-QA1-' + [guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $S018Zip -DestinationPath $S018Dest
Set-Location -LiteralPath (Join-Path $S018Dest 'LastSignal')
$S018Evidence = Join-Path $PWD ('QA-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $S018Evidence | Out-Null
Write-Host "EVIDENCE=$S018Evidence"
Start-Process -FilePath '.\LastSignal.exe' -ArgumentList @('-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',('"' + (Join-Path $S018Evidence 'first.log') + '"')) -Wait
```

Oyun açıkken:

1. Ana menü açılışı, okunur UI/font, materyaller (pembe/siyah yüzey), ses ve mouse capture kontrolü. Yeni oyun başlatın; hareket/bakış, loot/inventory, mevcut combat ve pause/settings deneyin. PC quality/FOV/ayarları forma yazın.
2. Oyundaki uygulanmış relay/shelter ilerlemesinde anlamlı bir checkpoint oluşturun. Player konumu, inventory/ammo, görev aşaması, storage/craft/door/loot durumunu ve görünür sonraki hedefi kaydedin. Eksik consume/treatment adımlarını tamamlanmış saymayın.
3. Pause ekranındaki Save/Kaydet düğmesini kullanın; başarı mesajı ve state ekran görüntülerini kaydedin. Save başarısızsa durun, mesaj/log gönderin. Ardından uygulamayı tamamen kapatın (gerekirse normal pencere kapatma/Alt+F4); yalnız ana menüye dönmek yeterli değildir. Start-Process -Wait bitmeli.
4. Aynı PowerShell oturumunda ikinci komutu çalıştırın:

```powershell
Start-Process -FilePath '.\LastSignal.exe' -ArgumentList @('-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',('"' + (Join-Path $S018Evidence 'reload.log') + '"')) -Wait
```

Yeni oyun açmadan ana menüde Load/Yükle'yi seçin. Önceki checkpoint'in aynı kaldığını ve en az bir sonraki core progression eylemine devam edebildiğinizi doğrulayın. Envanter/ammo/ödül duplication, kayıp, görev sıfırlanması veya kilitlenme FAIL'dir. Ölüm/ceset sunumu ile state kaybını ayırın. Pause/resume ve alt-tab dönüşünde yanlış ateş/stuck input kontrol edin. Bitince kapatın.

### Geri gönderilecekler / kabul

EVIDENCE yolu ve `first.log`, `reload.log`; OS/CPU/GPU/RAM, quality/resolution/FOV/input; açılış sonucu, save mesajı, yeniden açılış load sonucu ve devam edilen görev; görülen render/shader/native-plugin/input sorunları. Loglarda kişisel kullanıcı yolu varsa paylaşmadan önce maskeleyebilirsiniz. Beklenen save konumu Windows Unity varsayılanı `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Last Signal\saves\current.json`; actual yolu/logu teyit edin. Save/schema/seed içerikleri gerektiğinde ayrıca incelenir; kişisel dosyaları topluca göndermeyin.

PASS ancak gerçek Windows'ta açılış/input/render smoke + başarıyla save, process exit/restart/load ve kayıtlı state/progression devamı kanıtlanırsa verilir. Development ölçümü retail performans değildir. Hata çıkarsa yeniden test/build başlatmadan sonucu gönderin; yalnız etkilenen alan incelenir.

**Round 7: NOT_RUN — awaiting user-executed verification.** Build ve source kimliği sabit; dış pilot için canonical kapsam, insan kabulü, fiziksel gamepad desteği, S0/S1 triage, production capacity/owner scope ve G5 açıkları sürer.

## Önceki turların özeti

## Round 5 — build başarılı, kaynak-koruma FAIL

Kullanıcı [20261008T102605-710162Z-windows-build](../S017/Evidence/20261008T102605-710162Z-windows-build/): Unity process exit=0; build.txt Result=Succeeded, Errors=0, Warnings=538, Duration=00:12:49.7333770; Windows64 Development, Unity 6000.5.0f1, S013Cabin, Version=0.1.0. `Builds/S017/20261008T102605-710162Z-windows-build/LastSignal.exe` mevcut. [Transcript](Evidence/round5-TkaZzg/terminal.txt).

Tam source snapshot karşılaştırması: 18.614 dosyanın tamamı Assets/vendor/meta, Packages, ProjectSettings ve Tools kapsamında SHA-256 ile okundu; tek fark ProjectSettings.asset. [Manifest ve karşılaştırma](Evidence/20261008T104135-894879Z-round5-review/comparison.json). Bu salt-okuma incelemesidir; yeni build/test çalıştırılmadı.

**Toplam kapı FAIL**: source-preservation yalnız ProjectSettings/ProjectSettings.asset gösteriyor. Before `APP_UI_EDITOR_ONLY`, after `APP_UI_EDITOR_ONLY;SENTIS_ANALYTICS_ENABLED`. Yerel inference AnalyticsDefineManager InitializeOnLoad sembolü koşullara göre ekler/kaldırır. Bu kez önceki testtekinin ters yönünde eklenmiş. Kesin gözlenen fark bu; target switch mi analytics koşulu mu tetiklediği yalnız diff ile kanıtlanmaz. Güncel ProjectSettings byte olarak build-after ile aynı. Ajan ayar/analytics/paket/define değiştirmedi, kaynak korumayı gevşetmedi, artifact silmedi. Bu artifact dış pilota hazır sayılmaz.

Warning sınıfları: 493 shader warning başlangıç satırı, 41 CS0618 eski API, 2 CS0414 kullanılmayan impactLifetime, 1 boş vendor NavMesh assembly ve 1 RuntimePipelineConfig eksikliği = build raporundaki 538. Shader mesajlarının devam satırları ayrıca uyarı sayılmadı. Shader ve Pipeline uyarıları gerçek Windows render kanıtı gerektirir; tümü zararsız diye kapatılmadı. Paket yükseltme veya ilgisiz API refactor yapılmadı.

Tarihsel takip adımı Round 6 ile tamamlandı: aynı builder ile immutable dizinde kaynak korunarak PASS alındı. Bu paragraf yeni build talimatı değildir. Önceki test PASS'leri kendi kaynak kimlikleriyle korunur; yeni define'lı aday kabulü build/platform kanıtıyla ayrıca değerlendirilir.

## Doğrulanmış kullanıcı sonuçları

| Koşu | Sonuç | Kaynak ve sınır |
|---|---|---|
| [Round 4 / Art Editor](../S017/Evidence/20261008T102018-935006Z-focused-art/) | **10/10 PASS**, failed/skipped=0, exit=0 | changed=[]; PASS kendi koşu snapshot’ına aittir. Scene persistence/socket kimliği ve prefab kontrolleri geçti. [Terminal](Evidence/round4-zPKLj9/terminal.txt). |
| [Round 3 / full PlayMode](../S017/Evidence/20261008T095711-389229Z-regression-play/) | **306/306 PASS**, failed/skipped=0, exit=0 | changed=[]; persistence, S013 rota ve düzeltilen Combat/Scope fixture'ları birlikte geçti. [Terminal](Evidence/round3-N6rNLe/terminal.txt). |
| [Round 2 / Combat+Scope](../S017/Evidence/20261008T094111-397740Z-focused-combat-optics/) | **11/11 PASS**, exit=0 | changed=[]; önceki dört failure tekrarlanmadı. |
| [Önceki EditMode](../S017/Evidence/20261008T082950-052794Z-regression-edit/) | **541/541 PASS** | Round 3 sonrası hash karşılaştırmasında runtime/EditMode/ayar/paket aynı; yalnız PlayMode fixture/helper farklı. Güncel adayla uygulanabilirliği son doğrulamada kaynak farkına göre değerlendirilir. |

Sonuçlar XML case'leri, process-result ve source-preservation üzerinden incelendi. Round 5 analytics define değişimi önceki testleri güncel adaya koşulsuz taşımaz. Round 6 build PASS; gerçek Windows runtime NOT_RUN. Son hazırlıkta S018 belgeleri ve offline kayıt aracı/test kaynağı güncellendi; yeni test/build/compile çalıştırılmadı.
