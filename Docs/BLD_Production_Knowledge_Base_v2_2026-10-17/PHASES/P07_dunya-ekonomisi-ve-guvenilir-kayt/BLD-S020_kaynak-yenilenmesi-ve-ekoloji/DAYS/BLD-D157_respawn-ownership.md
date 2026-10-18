# BLD-D157 — Respawn/ownership

**Phase:** `P07 — Dünya ekonomisi ve güvenilir kayıt`  
**Sprint:** `BLD-S020 — Kaynak yenilenmesi ve ekoloji`  
**Kart:** 5/8 · **Alan:** `economy` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Drop ve item tombstone'ları aynı harvested node'a bağla.

**Kaynak plandaki kabul:** Harvest reward tekrar yüklemede çoğalmaz.

**Oyuncuya somut çıktı:**
- Bu kart, `Kaynak yenilenmesi ve ekoloji` deneyiminin `respawn/ownership` unsurunu tamamlar.
- Doğrudan bağlam: Ormandan kaynak toplanması sonrası kalan/depleted node ve makul dönüş.
- Değişiklik ancak `Harvest reward tekrar yüklemede çoğalmaz.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** Resource source/sink spreadsheet ve loot tables.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** Harvest node replenishment ve world depletion flags.
- **Girdi 3:** World Pressure/event pacing ve geriye dönüş seyahat maliyeti.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Docs/Last_Signal_Docs_v0.2/sources/12_World_Pressure.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
2. `Docs/Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
3. `Assets/LastSignal/Scripts/Runtime/Inventory/PlayerInventory.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Progression/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `Respawn/ownership` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Respawn/ownership` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Harvest reward tekrar yüklemede çoğalmaz.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Gerekli tarife RNG kaynaklı kalıcı softlock yaratamaz.
- Kaynak azalması noise decay ile karıştırılmaz.
- Grind, ilerlemeye kıyasla ölçüsüz tekrar üretmez.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** Her kaynağın ilk source, expected yield, taşıma maliyeti ve craft sink’ini ayrı satıra çıkar. Bu adım `respawn/ownership` için kaynak görevdeki "Drop ve item tombstone'ları aynı harvested node'a bağla." hedefiyle sınırlanır.
2. **Adım 2:** Kritik ilerleme tarifleri için deterministic obtain/recovery yolu yaz; RNG softlock’u yasakla. Bu adım `respawn/ownership` için kaynak görevdeki "Drop ve item tombstone'ları aynı harvested node'a bağla." hedefiyle sınırlanır.
3. **Adım 3:** Yenilenme politikasını dünya saati, cell unload ve prior depletion receipt ile bağla. Bu adım `respawn/ownership` için kaynak görevdeki "Drop ve item tombstone'ları aynı harvested node'a bağla." hedefiyle sınırlanır.
4. **Adım 4:** Tamir/söküm iadesini net kaynak çıkışı pozitif döngü üretmeyecek şekilde hesapla. Bu adım `respawn/ownership` için kaynak görevdeki "Drop ve item tombstone'ları aynı harvested node'a bağla." hedefiyle sınırlanır.
5. **Adım 5:** Kesim/gürültü etkisini Pressure API’na bounded event olarak bağla; hostile spawn’ı doğrudan tetikleme. Bu adım `respawn/ownership` için kaynak görevdeki "Drop ve item tombstone'ları aynı harvested node'a bağla." hedefiyle sınırlanır.
6. **Adım 6:** Oyuncunun 15/30/60 dakika hedeflerinde kaynak ve risk dengesini telemetri planına dök. Bu adım `respawn/ownership` için kaynak görevdeki "Drop ve item tombstone'ları aynı harvested node'a bağla." hedefiyle sınırlanır.
7. **Adım 7:** Kıtlık ve uzun yürüyüş eğlenceyi engelliyorsa azaltılacak content/tuning öğesini belirle. Bu adım `respawn/ownership` için kaynak görevdeki "Drop ve item tombstone'ları aynı harvested node'a bağla." hedefiyle sınırlanır.
8. **Adım 8:** Katlanarak artan envanter/üretim ve yeniden ziyaret durumlarında conservation testlerini belirt. Bu adım `respawn/ownership` için kaynak görevdeki "Drop ve item tombstone'ları aynı harvested node'a bağla." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Harvest reward tekrar yüklemede çoğalmaz.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Respawn/ownership`; komşu kartı önceden üstlenme | `BLD-D157` source task / diff |
| Sahiplik | Zombi spawn sayısı doğrudan inşa sayısına orantılanmaz; pressure bütçesi geçerli. | owner diagram / API |
| Gerçek kaynak | Resource source/sink spreadsheet ve loot tables | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Bütün yakın ağaçları kestikten sonra başlangıç barınağı yapılabiliyor mu? | rejection code / result snapshot |
| Persist/rollback | Kaynak azalması noise decay ile karıştırılmaz. | delta / before-after evidence |
| İlerleme | Harvest reward tekrar yüklemede çoğalmaz. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Bütün yakın ağaçları kestikten sonra başlangıç barınağı yapılabiliyor mu?
- **Sınır 2:** Yıkım/söküm kaynak iadesi sonsuz pozitif döngü yaratıyor mu?
- **Sınır 3:** Uzun oturumda aynı bölgede sonsuz odun spawn oluyor mu?
- **Durum 1:** Zombi spawn sayısı doğrudan inşa sayısına orantılanmaz; pressure bütçesi geçerli.
- **Durum 2:** Denge sayıları başlangıç hipotezidir; kullanıcı playtest verisi olmadan PASS yazılmaz.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D157 | Geçerli state/asset ve komut | **Pozitif yol:** Drop ve item tombstone'ları aynı harvested node'a bağla. Beklenen sonuç: Harvest reward tekrar yüklemede çoğalmaz. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D157 | Önkoşul ihlali | **Negatif yol:** Respawn/ownership uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D157 | Aynı istek iki kere | **Tekrar deneme:** Aynı respawn/ownership isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D157 | FirstTownCore/cell sınırı | **Sahne geçişi:** Ormandan kaynak toplanması sonrası kalan/depleted node ve makul dönüş; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D157 | Önceki sprint fixture | **Eski içerik regresyonu:** respawn/ownership değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D157-1`: Source/sink conservation spreadsheet. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D157-2`: 10/30/60-minute survival pacing sessions. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D157-3`: Softlock and dismantle/refund farming. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Harvest reward tekrar yüklemede çoğalmaz.
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

- [ ] Drop ve item tombstone'ları aynı harvested node'a bağla.
- [ ] Harvest reward tekrar yüklemede çoğalmaz.
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

- **Task ID / Branch / HEAD:** `BLD-D157` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S020 Sprint](../SPRINT.md).
- Phase overview: [P07](../../PHASE.md).
- Önceki kart: `BLD-D156` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D158` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
