# BLD-D188 — Build placement feedback

**Phase:** `P08 — Sanat, deneyim ve optimizasyon`  
**Sprint:** `BLD-S024 — Ses, VFX ve kullanıcı hissi`  
**Kart:** 4/8 · **Alan:** `placement` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları.

**Kaynak plandaki kabul:** Yanlış konum nedeninin görsel/ses karşılığı var.

**Oyuncuya somut çıktı:**
- Bu kart, `Ses, VFX ve kullanıcı hissi` deneyiminin `build placement feedback` unsurunu tamamlar.
- Doğrudan bağlam: Balta isabeti, ağaç devrilmesi, yeşil/kırmızı preview ve yapı kurulum FX.
- Değişiklik ancak `Yanlış konum nedeninin görsel/ses karşılığı var.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** BuildDefinition prototype footprint, anchor ve zone policy.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** Physics.Raycast/OverlapBoxNonAlloc ve layer/trigger filtering.
- **Girdi 3:** Door/vehicle/player spawn/nav clearances.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSocket.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
2. `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
3. `Docs/Last_Signal_Docs_v0.2/sources/13_World_Streaming_POI.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Construction/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `Build placement feedback` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Build placement feedback` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Yanlış konum nedeninin görsel/ses karşılığı var.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Kırmızı preview materyal tüketmez.
- Aynı transform ve same world snapshot aynı validation result üretmeli.
- Görsel ghost collider, AI veya persistent entity oluşturamaz.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** Yapı prefabının raycast anchor, footprint, normal ve rotation toleranslarını prefab metadata ile doğrula. Bu adım `build placement feedback` için kaynak görevdeki "Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları." hedefiyle sınırlanır.
2. **Adım 2:** İnşa iznini zone, POI, cell, player/vehicle, door ve nav koridor kurallarıyla sırayla değerlendir. Bu adım `build placement feedback` için kaynak görevdeki "Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları." hedefiyle sınırlanır.
3. **Adım 3:** OverlapBoxNonAlloc ve Physics.Raycast filtresini gerçek Layers/Trigger düzeninden türet. Bu adım `build placement feedback` için kaynak görevdeki "Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları." hedefiyle sınırlanır.
4. **Adım 4:** Ghost’u yalnız render-only tut; component side effects, collider ve persistent ID üretimini yasakla. Bu adım `build placement feedback` için kaynak görevdeki "Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları." hedefiyle sınırlanır.
5. **Adım 5:** Geçerli/ret nedenini structured enum + açıklama + ikon + yeşil/kırmızı görünümde göster. Bu adım `build placement feedback` için kaynak görevdeki "Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları." hedefiyle sınırlanır.
6. **Adım 6:** Rotasyon, yükseklik/snap değişiminde preview invalidation ve deterministic result uygulamasını tasarla. Bu adım `build placement feedback` için kaynak görevdeki "Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları." hedefiyle sınırlanır.
7. **Adım 7:** Confirm anında inventory ve world revision değişmişse aynı sorguyu yeniden çalıştır; stale preview’i reddet. Bu adım `build placement feedback` için kaynak görevdeki "Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları." hedefiyle sınırlanır.
8. **Adım 8:** Cancel, pause, death, cell leave ve scene reload sırasında ghost/material geçici durumlarını temizle. Bu adım `build placement feedback` için kaynak görevdeki "Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Yanlış konum nedeninin görsel/ses karşılığı var.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Build placement feedback`; komşu kartı önceden üstlenme | `BLD-D188` source task / diff |
| Sahiplik | Green/red preview yalnız presentation; ConstructionService authoritative confirm tekrar kontrol eder. | owner diagram / API |
| Gerçek kaynak | BuildDefinition prototype footprint, anchor ve zone policy | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Çit kapıyı tamamen bloke ediyorsa ret kodu ayrı olmalı. | rejection code / result snapshot |
| Persist/rollback | Aynı transform ve same world snapshot aynı validation result üretmeli. | delta / before-after evidence |
| İlerleme | Yanlış konum nedeninin görsel/ses karşılığı var. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Çit kapıyı tamamen bloke ediyorsa ret kodu ayrı olmalı.
- **Sınır 2:** Eğim sınırında preview geçerli ama confirm geçersiz olmamalı; son anda revalidate.
- **Sınır 3:** Yerleştirme ortasında world cell unload olursa ghost kapanmalı.
- **Durum 1:** Green/red preview yalnız presentation; ConstructionService authoritative confirm tekrar kontrol eder.
- **Durum 2:** Physics layers user project gerçeğiyle denetlenir, sabit sayılar körlemesine kopyalanmaz.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D188 | Geçerli state/asset ve komut | **Pozitif yol:** Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları. Beklenen sonuç: Yanlış konum nedeninin görsel/ses karşılığı var. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D188 | Önkoşul ihlali | **Negatif yol:** Build placement feedback uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D188 | Aynı istek iki kere | **Tekrar deneme:** Aynı build placement feedback isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D188 | FirstTownCore/cell sınırı | **Sahne geçişi:** Balta isabeti, ağaç devrilmesi, yeşil/kırmızı preview ve yapı kurulum FX; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D188 | Önceki sprint fixture | **Eski içerik regresyonu:** build placement feedback değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D188-1`: Slope/overlap/door/road/vehicle rejection matrix. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D188-2`: Rapid rotate + edge-of-zone + pause/cancel. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D188-3`: Ghost leaks, GC budget and deterministic result. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Yanlış konum nedeninin görsel/ses karşılığı var.
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

- [ ] Valid/invalid snap, rotate, place, fail SFX ve UX animasyonları.
- [ ] Yanlış konum nedeninin görsel/ses karşılığı var.
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

- **Task ID / Branch / HEAD:** `BLD-D188` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S024 Sprint](../SPRINT.md).
- Phase overview: [P08](../../PHASE.md).
- Önceki kart: `BLD-D187` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D189` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
