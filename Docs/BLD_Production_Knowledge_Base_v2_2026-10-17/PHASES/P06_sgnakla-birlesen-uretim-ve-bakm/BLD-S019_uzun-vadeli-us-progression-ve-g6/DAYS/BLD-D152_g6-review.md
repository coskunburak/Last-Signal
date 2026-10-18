# BLD-D152 — G6 review

**Phase:** `P06 — Sığınakla birleşen üretim ve bakım`  
**Sprint:** `BLD-S019 — Uzun vadeli üs progression ve G6`  
**Kart:** 8/8 · **Alan:** `qa` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Stability, player satisfaction ve art/perf kabul raporu.

**Kaynak plandaki kabul:** Tamamlanmamış faydalar PARTIAL kalır.

**Oyuncuya somut çıktı:**
- Bu kart, `Uzun vadeli üs progression ve G6` deneyiminin `g6 review` unsurunu tamamlar.
- Doğrudan bağlam: Oyuncunun bir sefer sonrası üssündeki somut kazanım ve hedef yenileme.
- Değişiklik ancak `Tamamlanmamış faydalar PARTIAL kalır.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
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

- **Trigger / request:** `G6 review` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `G6 review` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Tamamlanmamış faydalar PARTIAL kalır.
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

1. **Adım 1:** İlgili mevcut assembly, Tools runner ve test isimlerini yerel branch’te doğrulat. Bu adım `g6 review` için kaynak görevdeki "Stability, player satisfaction ve art/perf kabul raporu." hedefiyle sınırlanır.
2. **Adım 2:** Görevi component contract, regression ve gameplay etkisi olarak üç test düzeyine ayır. Bu adım `g6 review` için kaynak görevdeki "Stability, player satisfaction ve art/perf kabul raporu." hedefiyle sınırlanır.
3. **Adım 3:** Başarısızlık senaryoları için input fixture, initial snapshot ve beklenen state farkını yaz. Bu adım `g6 review` için kaynak görevdeki "Stability, player satisfaction ve art/perf kabul raporu." hedefiyle sınırlanır.
4. **Adım 4:** Kullanıcı tarafından çalıştırılacak en küçük focused test sırasını oluştur. Bu adım `g6 review` için kaynak görevdeki "Stability, player satisfaction ve art/perf kabul raporu." hedefiyle sınırlanır.
5. **Adım 5:** Focused sonuç PASS ise ilgili integration ve regression gate’ini ayrıca iste. Bu adım `g6 review` için kaynak görevdeki "Stability, player satisfaction ve art/perf kabul raporu." hedefiyle sınırlanır.
6. **Adım 6:** Source-preservation diff, asset GUID, save schema ve backup kontrollerini doğrulama listesine al. Bu adım `g6 review` için kaynak görevdeki "Stability, player satisfaction ve art/perf kabul raporu." hedefiyle sınırlanır.
7. **Adım 7:** Performance ölçümlerini median/p95/p99 CPU/GPU, GC ve memory bağlamında raporla. Bu adım `g6 review` için kaynak görevdeki "Stability, player satisfaction ve art/perf kabul raporu." hedefiyle sınırlanır.
8. **Adım 8:** Test çalıştırmadan, görsel owner kabulü olmadan veya eksik logla PASS yazılmasını engelle. Bu adım `g6 review` için kaynak görevdeki "Stability, player satisfaction ve art/perf kabul raporu." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Tamamlanmamış faydalar PARTIAL kalır.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `G6 review`; komşu kartı önceden üstlenme | `BLD-D152` source task / diff |
| Sahiplik | Komut gerçek Tools/ test assembly dosyasından türetilir; hayalî --filter verilmez. | owner diagram / API |
| Gerçek kaynak | Planın phase gates ve değişiklik kayıtları | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Test Unity Editor açıkken batchmode kilitleniyor mu? | rejection code / result snapshot |
| Persist/rollback | Sadece eski PASS yeni sürüm için otomatik PASS değildir. | delta / before-after evidence |
| İlerleme | Tamamlanmamış faydalar PARTIAL kalır. | owner-run validation |

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
| POS-BLD-D152 | Geçerli state/asset ve komut | **Pozitif yol:** Stability, player satisfaction ve art/perf kabul raporu. Beklenen sonuç: Tamamlanmamış faydalar PARTIAL kalır. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D152 | Önkoşul ihlali | **Negatif yol:** G6 review uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D152 | Aynı istek iki kere | **Tekrar deneme:** Aynı g6 review isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D152 | FirstTownCore/cell sınırı | **Sahne geçişi:** Oyuncunun bir sefer sonrası üssündeki somut kazanım ve hedef yenileme; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D152 | Önceki sprint fixture | **Eski içerik regresyonu:** g6 review değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D152-1`: Targeted edit/play tests owner-run. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D152-2`: Source preservation and diff audit. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D152-3`: Evidence integrity, perf counter and gate receipt review. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Tamamlanmamış faydalar PARTIAL kalır.
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

- [ ] Stability, player satisfaction ve art/perf kabul raporu.
- [ ] Tamamlanmamış faydalar PARTIAL kalır.
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

- **Task ID / Branch / HEAD:** `BLD-D152` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S019 Sprint](../SPRINT.md).
- Phase overview: [P06](../../PHASE.md).
- Önceki kart: `BLD-D151` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
