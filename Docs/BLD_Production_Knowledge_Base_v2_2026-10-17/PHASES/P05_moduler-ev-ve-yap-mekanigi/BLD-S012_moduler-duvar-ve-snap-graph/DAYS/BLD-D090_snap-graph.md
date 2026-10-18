# BLD-D090 — Snap graph

**Phase:** `P05 — Modüler ev ve yapı mekaniği`  
**Sprint:** `BLD-S012 — Modüler duvar ve snap graph`  
**Kart:** 2/8 · **Alan:** `construction` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Node/socket edge adjacency ve parent-child yaşam döngüsü.

**Kaynak plandaki kabul:** Tüm parçalar tek GameObject hiyerarşisine zorlanmaz.

**Oyuncuya somut çıktı:**
- Bu kart, `Modüler duvar ve snap graph` deneyiminin `snap graph` unsurunu tamamlar.
- Doğrudan bağlam: İki duvar + köşe + giriş açıklığı snap bağlantısı.
- Değişiklik ancak `Tüm parçalar tek GameObject hiyerarşisine zorlanmaz.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** Modüler prefab dimension/socket/snap metrikleri.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** Collision, cover, roof/weather ve NavMesh impact.
- **Girdi 3:** Build placement domain stable ID, condition, graph.
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

- **Trigger / request:** `Snap graph` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Snap graph` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Tüm parçalar tek GameObject hiyerarşisine zorlanmaz.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- 1 Unity unit = 1 metre; karakter ve araç boyutları sabit baseline referansıdır.
- Yapıların snap koordinatı render mesh değil authoring anchor üzerinden belirlenir.
- Kritik geçişleri inşa kapatamaz; yapı sökülünce geometry/nav tutarlı yenilenir.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** Modüler 1m/2m/4m geometry kararını mevcut asset ölçüleri ve 1 Unity unit = 1 metre sözleşmesiyle karşılaştır. Bu adım `snap graph` için kaynak görevdeki "Node/socket edge adjacency ve parent-child yaşam döngüsü." hedefiyle sınırlanır.
2. **Adım 2:** Prefab pivot, snapping anchor, support/connection metadata ve görsel collider ayrımını doğrula. Bu adım `snap graph` için kaynak görevdeki "Node/socket edge adjacency ve parent-child yaşam döngüsü." hedefiyle sınırlanır.
3. **Adım 3:** Temel→duvar→açıklık→çatı bağlarını uyumlu blueprint kümesiyle tasarla, kusurlu kombinasyonu reddet. Bu adım `snap graph` için kaynak görevdeki "Node/socket edge adjacency ve parent-child yaşam döngüsü." hedefiyle sınırlanır.
4. **Adım 4:** Kapı/giriş/player ve araç clearance alanlarını örnek staging sahnesinde boyutlarla kaydet. Bu adım `snap graph` için kaynak görevdeki "Node/socket edge adjacency ve parent-child yaşam döngüsü." hedefiyle sınırlanır.
5. **Adım 5:** Yapının sağlamlık, bağlantı ve özelliği için yalnız gerekli stable state alanlarını öner. Bu adım `snap graph` için kaynak görevdeki "Node/socket edge adjacency ve parent-child yaşam döngüsü." hedefiyle sınırlanır.
6. **Adım 6:** Roof ve shelter/wetness kararlarını mevcut WorldClock/RainRoof kurallarına bağla; yeni weather authority kurma. Bu adım `snap graph` için kaynak görevdeki "Node/socket edge adjacency ve parent-child yaşam döngüsü." hedefiyle sınırlanır.
7. **Adım 7:** Yıkım ve onarımda ilgili graph/physics/nav etkisini bounded işle, performans regresyonu doğurma. Bu adım `snap graph` için kaynak görevdeki "Node/socket edge adjacency ve parent-child yaşam döngüsü." hedefiyle sınırlanır.
8. **Adım 8:** Gece/yağmur/FPS görüş açısı ve döndürme+save/load açısından gerçek gameplay kanıtını planla. Bu adım `snap graph` için kaynak görevdeki "Node/socket edge adjacency ve parent-child yaşam döngüsü." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Tüm parçalar tek GameObject hiyerarşisine zorlanmaz.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Snap graph`; komşu kartı önceden üstlenme | `BLD-D090` source task / diff |
| Sahiplik | Voxel veya ağır runtime physics structural simulation ilk scope dışında. | owner diagram / API |
| Gerçek kaynak | Modüler prefab dimension/socket/snap metrikleri | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu? | rejection code / result snapshot |
| Persist/rollback | Yapıların snap koordinatı render mesh değil authoring anchor üzerinden belirlenir. | delta / before-after evidence |
| İlerleme | Tüm parçalar tek GameObject hiyerarşisine zorlanmaz. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu?
- **Sınır 2:** Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur?
- **Sınır 3:** İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu?
- **Durum 1:** Voxel veya ağır runtime physics structural simulation ilk scope dışında.
- **Durum 2:** Geometri authoring onarımında vendor prefab materyali doğrudan değiştirilmez.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D090 | Geçerli state/asset ve komut | **Pozitif yol:** Node/socket edge adjacency ve parent-child yaşam döngüsü. Beklenen sonuç: Tüm parçalar tek GameObject hiyerarşisine zorlanmaz. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D090 | Önkoşul ihlali | **Negatif yol:** Snap graph uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D090 | Aynı istek iki kere | **Tekrar deneme:** Aynı snap graph isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D090 | FirstTownCore/cell sınırı | **Sahne geçişi:** İki duvar + köşe + giriş açıklığı snap bağlantısı; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D090 | Önceki sprint fixture | **Eski içerik regresyonu:** snap graph değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D090-1`: Corner/window/door/foundation join permutations. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D090-2`: Collision/nav walkable egress routes. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D090-3`: Build/repair/demolish/save/load roundtrip. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Tüm parçalar tek GameObject hiyerarşisine zorlanmaz.
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

- [ ] Node/socket edge adjacency ve parent-child yaşam döngüsü.
- [ ] Tüm parçalar tek GameObject hiyerarşisine zorlanmaz.
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

- **Task ID / Branch / HEAD:** `BLD-D090` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S012 Sprint](../SPRINT.md).
- Phase overview: [P05](../../PHASE.md).
- Önceki kart: `BLD-D089` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D091` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
