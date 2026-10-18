# BLD-D201 — Profiling baseline

**Phase:** `P08 — Sanat, deneyim ve optimizasyon`  
**Sprint:** `BLD-S026 — CPU/GPU ve büyük dünya optimizasyonu`  
**Kart:** 1/8 · **Alan:** `qa` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** 1080p Mac development + Windows hedefindeki frame-time profile planı.

**Kaynak plandaki kabul:** Donanım, görüntü ayarları ve koşullar belgelenir.

**Oyuncuya somut çıktı:**
- Bu kart, `CPU/GPU ve büyük dünya optimizasyonu` deneyiminin `profiling baseline` unsurunu tamamlar.
- Doğrudan bağlam: 128+ yapı varken ghost/culling/nav, p95/p99 ve memory.
- Değişiklik ancak `Donanım, görüntü ayarları ve koşullar belgelenir.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** Planın phase gates ve değişiklik kayıtları.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** S010/S023 mevcut automated test entry points.
- **Girdi 3:** GitHub branch/commit, owner manual testing policy.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Docs/Last_Signal_Docs_v0.2/sources/17_Save_Load_Migration.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
2. `Docs/Last_Signal_Production_Plan_v1.0_TR/management/02_Kapsam_Kararlar_ve_Degisiklik.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
3. `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Tests/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `Profiling baseline` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Profiling baseline` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Donanım, görüntü ayarları ve koşullar belgelenir.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Agent manual test çalıştırmaz; kullanıcı evidence ile PASS verir.
- Sadece eski PASS yeni sürüm için otomatik PASS değildir.
- Kritik save/item duplication/regression açıkken gate kapatılamaz.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** İlgili mevcut assembly, Tools runner ve test isimlerini yerel branch’te doğrulat. Bu adım `profiling baseline` için kaynak görevdeki "1080p Mac development + Windows hedefindeki frame-time profile planı." hedefiyle sınırlanır.
2. **Adım 2:** Görevi component contract, regression ve gameplay etkisi olarak üç test düzeyine ayır. Bu adım `profiling baseline` için kaynak görevdeki "1080p Mac development + Windows hedefindeki frame-time profile planı." hedefiyle sınırlanır.
3. **Adım 3:** Başarısızlık senaryoları için input fixture, initial snapshot ve beklenen state farkını yaz. Bu adım `profiling baseline` için kaynak görevdeki "1080p Mac development + Windows hedefindeki frame-time profile planı." hedefiyle sınırlanır.
4. **Adım 4:** Kullanıcı tarafından çalıştırılacak en küçük focused test sırasını oluştur. Bu adım `profiling baseline` için kaynak görevdeki "1080p Mac development + Windows hedefindeki frame-time profile planı." hedefiyle sınırlanır.
5. **Adım 5:** Focused sonuç PASS ise ilgili integration ve regression gate’ini ayrıca iste. Bu adım `profiling baseline` için kaynak görevdeki "1080p Mac development + Windows hedefindeki frame-time profile planı." hedefiyle sınırlanır.
6. **Adım 6:** Source-preservation diff, asset GUID, save schema ve backup kontrollerini doğrulama listesine al. Bu adım `profiling baseline` için kaynak görevdeki "1080p Mac development + Windows hedefindeki frame-time profile planı." hedefiyle sınırlanır.
7. **Adım 7:** Performance ölçümlerini median/p95/p99 CPU/GPU, GC ve memory bağlamında raporla. Bu adım `profiling baseline` için kaynak görevdeki "1080p Mac development + Windows hedefindeki frame-time profile planı." hedefiyle sınırlanır.
8. **Adım 8:** Test çalıştırmadan, görsel owner kabulü olmadan veya eksik logla PASS yazılmasını engelle. Bu adım `profiling baseline` için kaynak görevdeki "1080p Mac development + Windows hedefindeki frame-time profile planı." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Donanım, görüntü ayarları ve koşullar belgelenir.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Profiling baseline`; komşu kartı önceden üstlenme | `BLD-D201` source task / diff |
| Sahiplik | Komut gerçek Tools/ test assembly dosyasından türetilir; hayalî --filter verilmez. | owner diagram / API |
| Gerçek kaynak | Planın phase gates ve değişiklik kayıtları | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Test Unity Editor açıkken batchmode kilitleniyor mu? | rejection code / result snapshot |
| Persist/rollback | Sadece eski PASS yeni sürüm için otomatik PASS değildir. | delta / before-after evidence |
| İlerleme | Donanım, görüntü ayarları ve koşullar belgelenir. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Test Unity Editor açıkken batchmode kilitleniyor mu?
- **Sınır 2:** Focused PASS sonrası full regression FAIL ise false closure yapılır mı?
- **Sınır 3:** Görsel acceptance olmadan performans testinin PASS olması release için yeterli mi?
- **Durum 1:** Komut gerçek Tools/ test assembly dosyasından türetilir; hayalî --filter verilmez.
- **Durum 2:** Editor test, PlayMode, build, perf ve art gate birbirinden ayrıdır.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D201 | Geçerli state/asset ve komut | **Pozitif yol:** 1080p Mac development + Windows hedefindeki frame-time profile planı. Beklenen sonuç: Donanım, görüntü ayarları ve koşullar belgelenir. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D201 | Önkoşul ihlali | **Negatif yol:** Profiling baseline uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D201 | Aynı istek iki kere | **Tekrar deneme:** Aynı profiling baseline isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D201 | FirstTownCore/cell sınırı | **Sahne geçişi:** 128+ yapı varken ghost/culling/nav, p95/p99 ve memory; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D201 | Önceki sprint fixture | **Eski içerik regresyonu:** profiling baseline değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D201-1`: Targeted edit/play tests owner-run. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D201-2`: Source preservation and diff audit. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D201-3`: Evidence integrity, perf counter and gate receipt review. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
4. **Negatif test:** yanlış layer, null reference, invalid GUID, duplicate receipt veya overflow gibi gerçekten anlamlı tek kart özel hata seç.
5. **Integration test:** önceki sprintin asli davranışını kırmadığını kanıtla; yalnız unit mock sonucuyla yetinme.
6. **Scene/runtime kabul:** gerekiyorsa FirstTownCore VisualSlice kopyasında, ana sahneye promote etmeden manuel çalıştırılacak.

### Owner-run çalışma politikası

- Agent EditMode, PlayMode, build, benchmark, runtime capture çalıştırmaz.
- Önce yerel `Tools/` ve test assembly gerçek CLI seçeneklerini keşfeder; yalnız doğrulanmış komut gönderir.
- Kullanıcı logları gönderince failure fixture/line/root cause analiz edilir; önce odaklı test yeniden çalıştırılır.
- Manuel screenshot gerçek Game View’dan alınır; referans/AI görseli kabul kanıtı değildir.
- `IMPLEMENTED` ≠ `PASS`; test koşmadıysa `TEST_NOT_RUN`.

## 10. Ölçüm, performans ve gözlemlenebilirlik

- **Ürün göstergesi:** Donanım, görüntü ayarları ve koşullar belgelenir.
- **Kod göstergesi:** değişiklik sahibi ve bağlı test/evidence yolları kayıtta; gizli yan etki yok.
- **Veri göstergesi:** ilgili kayıtlar değişiyorsa source/target owner ID, revision ve geri dönüş korunur.
- **Render/runtime:** `Update()` taraması, material instancing, physics queries ve GC maliyeti yalnız ilgili kart için ölçülecek.
- **Bütçe:** nicel hedef baseline karşılaştırması yapılmadan sabit sayı “PASS” sayılmaz.
- **Log:** game-facing hata kısa ve anlaşılır; debug receipt ayrıntılı; frame başına spam yasak.

## 11. Dokunulmaması gereken alanlar

- Var olan `FirstTownCore` ve `FirstTownCore_VisualSlice` güvenlik/hashing akışı korunur.
- Vendor `Assets/ThirdParty` kaynak assetleri üzerinde düzenleme yapılmaz.
- Silah tutuşu, zombie AI, vehicle dynamics ve lighting framework bu kartın konusu değilse değiştirilmez.
- Eski S010 recipe/shelter save fixtures migration/adapter olmadan değişmez.
- Git commit/push/reset/clean veya source scene promote kullanıcı onayı olmadan çalıştırılmaz.

## 12. Definition of Done — kartı kapatma kriterleri

- [ ] 1080p Mac development + Windows hedefindeki frame-time profile planı.
- [ ] Donanım, görüntü ayarları ve koşullar belgelenir.
- [ ] Positive case fail-proof state farkıyla kayıtlı.
- [ ] Negative case malzeme/owner/state değiştirmeden reddedildi.
- [ ] Duplicate/rapid input idempotence kanıtı hazır.
- [ ] Null/invalid/overflow, cancellation ve scene lifecycle ele alındı.
- [ ] Mevcut korunan sistemlerin regresyon haritası güncellendi.
- [ ] Save/migration ilişkisi açık; N/A ise gerekçe yazıldı.
- [ ] Yalnız ilgili belge/kod/prefab değişti; source diff gözden geçirildi.
- [ ] Bütün gerçek test komutları doğrulandı ve kullanıcıya verildi.
- [ ] Kullanıcı test logu ve gerekirse Game View görüntüsü gönderdi.
- [ ] Status yalnız kanıtla güncellendi; eksikler yeni iş kartı olarak tutuldu.

## 13. Gün sonu handoff — doldurulabilir kayıt

- **Task ID / Branch / HEAD:** `BLD-D201` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S026 Sprint](../SPRINT.md).
- Phase overview: [P08](../../PHASE.md).
- Sonraki kart: `BLD-D202` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
