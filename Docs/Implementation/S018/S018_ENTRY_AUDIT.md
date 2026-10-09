# S018 giriş denetimi — 2026-10-08

Bu belge ilk giriş anının tarihsel kaydıdır; güncel durum ve geçerli sıra [S018_VERIFICATION_HANDOFF.md](S018_VERIFICATION_HANDOFF.md) içindedir. Aşağıdaki eski NOT_RUN/FAIL değerleri sonraki sonuçları geçersiz kılmaz.

Canonical: **S018 — Dış oyuncu testi ve üretime geçiş kararı**, P05, D171–D180. Hazırlık PARTIAL; dış pilot girişi **BLOCKED**. Bu kayıt kaynak/önceki kanıt incelemesidir, yeni test sonucu değildir. Çalışma single-player kapsamındadır.

## Gerçek yerel kimlik

- Root: `/Users/burakcoskun/Last Signal`; branch: `s012-integrated-graybox-slice`.
- HEAD: `7f5ffb783f396f9675c28da18be7021d90a02eb7`; çalışma ağacı **dirty**. HEAD tek başına oynanabilir adayın kimliği değildir. Yerel S015–S017 runtime, scene, input, test, font/glyph ve araç değişiklikleri korundu; uzak main kullanılmadı.
- Unity: `6000.5.0f1 (88b47c5e7076)`. Executable: `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`. Mac ve WindowsStandaloneSupport kurulu.
- Input System `1.19.0`, URP `17.5.0`, Test Framework `1.7.0`, Navigation `2.0.13`, uGUI `2.5.0`. Paketler değiştirilmedi. Manifestte önceden bulunan Multiplayer Center, multiplayer uygulandığı anlamına gelmez.
- MCP salt-okuma sahne incelemesi: `S013Cabin.unity`, loaded=true, dirty=false, playing=false, compiling=false; PC quality; `overrideSeed=true`, `explicitSeed=12345`. Runtime seed=0 çünkü oturum çalıştırılmadı. Play/test/build başlatılmadı; inspector komutunun derlenmesi oyun derleme/test kabulü değildir.
- S018 inceleme manifesti: [entry-identity.json](Evidence/20261008T090757-870979Z-entry-inspection/entry-identity.json), [source-hashes.json](Evidence/20261008T090757-870979Z-entry-inspection/source-hashes.json), [git-status.txt](Evidence/20261008T090757-870979Z-entry-inspection/git-status.txt).
- Digest: `8c139bef34db396b4c67456cb273feb020b0a3ea761aaf640cafe20a5fb29415` — 1446 dosya; yalnız Assets/LastSignal, Packages, ProjectSettings ve Tools. Vendor varlıkları kapsam dışında; bu bir tam build freeze manifesti değildir.
- İlk git status, sandbox içindeki `.git/lfs/tmp` yazma sınırına takıldı. İkinci salt-okuma sorgusu komuta özel `lfs.storage=/private/tmp/last-signal-s018-lfs` ve `GIT_OPTIONAL_LOCKS=0` ile tamamlandı. Filtre devre dışı bırakılmadı; repo config/index değiştirilmedi. AGENTS.md bulunmadı.

## Önkoşullar

VERIFIED ancak gereken davranış ve kabul kanıtı birlikte varsa kullanılır. Aşağıdaki PARTIAL değerleri kod yok demek değildir; ilgili sprintin bütün kabulü kapanmamıştır.

| Önkoşul | Durum | Yerel dayanak | S018 etkisi |
|---|---|---|---|
| S012 | PARTIAL | [G3 raporu](../S012/S012_G3_GATE_REPORT.md): geçmiş teknik build/kayıt/rota kanıtı; G3 BLOCKED | Kesintisiz 30–45 dk gerçek rota ve quit/load, iki geçerli yaklaşım ve owner kabulü eksik. Canonical slice kapanışını engeller. |
| S013 | PARTIAL | [Son devir](../S013/S013_VERIFICATION_HANDOFF.md): S013Cabin, üretim builder, tarihsel Mac build | POI final görsel kabulü ve güncel platform/performance eksik. Önceki build son S017 kaynaklarını içermez. |
| S014 | PARTIAL | [Düzeltilmiş kapanış](../S014/S014_FINAL_CLOSURE.md): D138 deferred, D140 insan kabulü açık | Treatment domain yok; corpse load animasyonu bilinen sunum sınırı. Tarihsel 30 performans örneği güncel aday ölçümü değildir. |
| S015 | PARTIAL | [Devir](../S015/S015_VERIFICATION_HANDOFF.md), implementation report: ses sistemi yerelde var | Headphone/speaker/muted kabulü, güncel mix/performance ve üç paketin edinim kanıtı açık. Eski movement FAIL hikâyesi S017 kanıtıyla güncellenir; yeniden sistem kurulmaz. |
| S016 | PARTIAL | [Devir](../S016/S016_VERIFICATION_HANDOFF.md): son eski full PlayMode 296/297 | UI/manual kabul ve generic equip/consume domain açıkları. Movement fix S017 odaklı kanıtında yer alıyor; eski FAIL güncel runtime bug diye sunulmaz. |
| S017 | PARTIAL | [Son devir](../S017/S017_IMPLEMENTATION_HANDOFF.md), [çalışma kaydı](../S017/S017_WORKING_LEDGER.md) ve aşağıdaki XML incelemesi | Son tam PlayMode FAIL sonrası dar düzeltmeler var; yeni tam koşu, Windows build/runtime, fiziksel controller ve D170 canonical rota açık. S017 devri S018 girişini BLOCKED tutuyor. |

İlk üç sprintin eski raporları tarihsel yönlendirme olarak kullanıldı; bütün eski evidence klasörleri açılmadı. Eski test sayıları güncel aday PASS olarak taşınmadı.

## Doğrudan incelenen yakın kanıt

Her satır için XML root, process-result, source-preservation ve o koşunun source-hashes kaydı okundu. Ayrıntı: [prior-evidence-review.json](Evidence/20261008T090757-870979Z-entry-inspection/prior-evidence-review.json). Tarihsel dosyalar değiştirilmedi.

| S017 evidence run | Gerçek sonuç | Bugünkü kaynağa ilişkin sınır |
|---|---|---|
| `20261008T082553-327919Z-focused-flow` | 9/9 PASS | Sonrasında yalnız PlayMode fixture/helper değişiklikleri; runtime aynı. Yeni aday veya fiziksel cihaz kabulü değil. |
| `20261008T082826-106543Z-focused-input` | 6/6 PASS | Runtime/EditMode kaynakları aynı; yalnız PlayMode fixture/helper değişti. |
| `20261008T082950-052794Z-regression-edit` | 541/541 PASS | Failed/skipped=0, exit=0, koşu sırasında kaynak değişmedi. EditMode risk yüzeyi değişmediği için hemen yeniden istenmiyor. |
| `20261008T083059-589475Z-regression-play` | 273/306; 33 FAIL | Son tam koşu FAIL. Sonraki fixture izolasyonu bu sonuçtan daha yeni. |
| `20261008T084914-868321Z-focused-regression` | 46/47; 1 FAIL | Yalnız S016PauseTests daha sonra değişmiş. |
| `20261008T085205-550991Z-focused-regression` | Pause dar tekrarı 1/1 PASS | Kayıtlı 1428 dosyalı kapsam güncel kaynakla eşleşiyor; ek proje dosyası yok. Tüm suite PASS değil. |

İlk tur **tek full PlayMode** tekrarının gerekçesi budur: odaklı düzeltme aşaması zaten kullanıcı tarafından yürütülmüş; geçmişte sıra/izolasyon sorunu yalnız birleşik koşuda görülmüştür. Full EditMode+PlayMode veya build zinciri istenmez.

## Build, kayıt, input ve açık riskler

Üretim sahnesi `Assets/LastSignal/Scenes/Production/S013Cabin.unity`. EditorBuildSettings hâlâ S001/Combat validation sahnelerini listeler; normal Build Settings ile yanlış slice üretilmemeli. Mevcut S013 builder sahneyi açıkça seçer. Mac: `Tools/s013-build.py production`; Windows: `Tools/s017-verify.py windows-build` → `LastSignal.Art.Editor.S013ProductionAuthoring.BuildS017Windows`. İkisi Development Build; retail performans kanıtı değildir.

Güncel S018 artifact **yok**. S014 raporundaki `Builds/S013/20261007T121645-898215Z-production/LastSignal.app` tarihsel referanstır; yeni aday sayılmaz. SaveSession `world.s012.graybox`, `s012-graybox-v1`; cell+population bileşiminde schema 4 seçer. Gerçek aday save header'ı ile ayrıca doğrulanmalı. `SaveValidation.SchemaVersion=2` tek başına bu sahnenin schema değeri değildir.

Windows keyboard/mouse zorunlu yol; Xbox/XInput ve generic Unity Gamepad aday cihazlar. Sentetik kontrolcü kanıtı mevcut, fiziksel kabul NOT_RUN. Windows runtime **NOT_RUN**; gerçek Windows makinesi erişimi bu oturumda bildirilmedi. Erişim olmadığı doğrulanırsa `BLOCKED — real Windows runtime validation unavailable` yapılır. macOS, Windows yerine sayılmaz.

S0/S1 için **güncel sayım bilinmiyor**; sıfır beyanı yok. Önceki S014 “yok” kaydı current candidate triage yerine geçmez. Tam PlayMode 33 eski failure doğrudan 33 gameplay S1 demek değildir; fixture/root-cause ve tekrar sonucu ayrılır. Generic consume/equip ve wound/bandage treatment eksikleri canonical su tüketimi/tedavi deneyini eksik bırakır. Bunlar yeni asset ihtiyacı değildir. Owner kapsam kararı veya ayrı kontrollü önkoşul onarımı olmadan shelter recovery, treatment PASS sayılmaz. Dar usability araştırması sonradan açık kapsamla yapılabilir; canonical G5 yerine geçemez. Bilinen S0/S1'li aday genel pilot için açılmaz.

## D171–D180 fark matrisi

| Kart | Bu iterasyon | Kalan girdi / kabul |
|---|---|---|
| D171 | Tarama, tarafsız protokol ve boş oturum formu hazır | 5–8 gerçek katılımcı; henüz katılım yok |
| D172 | Freeze sözleşmesi ve build yolları hazır | Teknik koşular, artifact, tam kimlik ve checksum; NOT_FROZEN |
| D173 | NOT_RUN | İlk gerçek grup |
| D174 | NOT_RUN | Farklı deneyim segmenti, aynı aday |
| D175 | Boş synthesis şablonu | Gerçek gözlem olmadan bulgu yok |
| D176 | Boş traceability şablonu | Gerçek en yüksek etkili bulgular; gameplay değişikliği yok |
| D177 | NOT_RUN | Gerekirse PT2 teknik kapısı ve öğrenme etkili retest |
| D178 | Veri açığı ve tam maliyet yöntemi hazır | Kabul edilmiş bitmiş birim actual saatleri yok |
| D179 | Dar/geniş senaryo karşılaştırması hazır | Kapasite, nakit dayanımı ve owner scope kararı |
| D180 | G5 BLOCKED; öneri RETEST | İnsan, platform, kayıt, teknik ve kapasite kanıtı |

**NO NEW ASSET PACKAGE REQUIRED.** Mevcut sistemler korunur. Yeni paket/SDK, refactor, gameplay içerik genişlemesi, commit/push/publish yapılmadı. Bir sonraki işlem [kullanıcı doğrulama devri](S018_VERIFICATION_HANDOFF.md).
