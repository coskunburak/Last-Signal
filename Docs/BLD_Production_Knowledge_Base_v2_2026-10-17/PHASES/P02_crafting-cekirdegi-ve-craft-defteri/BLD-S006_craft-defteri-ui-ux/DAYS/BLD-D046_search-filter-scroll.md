# BLD-D046 — Search/filter/scroll

**Phase:** `P02 — Crafting çekirdeği ve Craft Defteri`  
**Sprint:** `BLD-S006 — Craft Defteri UI/UX`  
**Kart:** 6/8 · **Alan:** `notebook` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Liste filtreleme, kategori ve detay sayfasını hafif implement et.

**Kaynak plandaki kabul:** Yüzlerce öğe varken her frame tam liste kurulmaz.

**Oyuncuya somut çıktı:**
- Bu kart, `Craft Defteri UI/UX` deneyiminin `search/filter/scroll` unsurunu tamamlar.
- Doğrudan bağlam: Craft Defteri -> Yapılar -> Çit: malzeme gösterimi -> seç ve kapat.
- Değişiklik ancak `Yüzlerce öğe varken her frame tam liste kurulmaz.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** Inventory quantity snapshots ve revision events.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** Category/recipe/build definition registry.
- **Girdi 3:** Mevcut UI input/cursor/pause kuralları ve accessibility.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Assets/LastSignal/Scripts/Runtime/Inventory/PlayerInventory.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
2. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
3. `Docs/Last_Signal_Docs_v0.2/sources/07_Item_Inventory_Equipment.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Construction/UI/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `Search/filter/scroll` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Search/filter/scroll` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Yüzlerce öğe varken her frame tam liste kurulmaz.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- UI doğrudan item eksiltmez veya world prefab instantiate etmez.
- Aynı seçimi çift tıklamak iki build/craft isteği başlatmaz.
- Geçersizliğin nedeni yalnız yeşil/kırmızı renk ile anlatılmaz.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** Tarif/yapı tanımından yalnız read-only UI modeli üret; katalog ile runtime inventory’yi karıştırma. Bu adım `search/filter/scroll` için kaynak görevdeki "Liste filtreleme, kategori ve detay sayfasını hafif implement et." hedefiyle sınırlanır.
2. **Adım 2:** Kategori, arama, malzeme gereği ve unlock durumlarını gerçek inventory revision event’iyle güncelle. Bu adım `search/filter/scroll` için kaynak görevdeki "Liste filtreleme, kategori ve detay sayfasını hafif implement et." hedefiyle sınırlanır.
3. **Adım 3:** Mouse/keyboard/controller odağını, Inventory/Reload/Pause ile çakışmayacak modal kontrata bağla. Bu adım `search/filter/scroll` için kaynak görevdeki "Liste filtreleme, kategori ve detay sayfasını hafif implement et." hedefiyle sınırlanır.
4. **Adım 4:** Seçim callback’ini build-selection request veya craft request’e bağla; UI’dan Instantiate çağırma. Bu adım `search/filter/scroll` için kaynak görevdeki "Liste filtreleme, kategori ve detay sayfasını hafif implement et." hedefiyle sınırlanır.
5. **Adım 5:** Hatalı/eksik malzemede sadece renk değil ikon ve kısa anlaşılır gerekçe göster. Bu adım `search/filter/scroll` için kaynak görevdeki "Liste filtreleme, kategori ve detay sayfasını hafif implement et." hedefiyle sınırlanır.
6. **Adım 6:** Kategori değişimi ve hızlı seçimin stale UI state üretmemesi için version/selection guard tanımla. Bu adım `search/filter/scroll` için kaynak görevdeki "Liste filtreleme, kategori ve detay sayfasını hafif implement et." hedefiyle sınırlanır.
7. **Adım 7:** UI kalıcılığını değil domain state kalıcılığını koru; ölüm/scene switch sırasında geçici UI’ı kapat. Bu adım `search/filter/scroll` için kaynak görevdeki "Liste filtreleme, kategori ve detay sayfasını hafif implement et." hedefiyle sınırlanır.
8. **Adım 8:** En az bir düşük çözünürlük, uzun tarif ismi ve gamepad senaryosuyla erişilebilirliği kontrol ettir. Bu adım `search/filter/scroll` için kaynak görevdeki "Liste filtreleme, kategori ve detay sayfasını hafif implement et." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Yüzlerce öğe varken her frame tam liste kurulmaz.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Search/filter/scroll`; komşu kartı önceden üstlenme | `BLD-D046` source task / diff |
| Sahiplik | Büyük catalog her frame rebuild edilmez; diff/event update. | owner diagram / API |
| Gerçek kaynak | Inventory quantity snapshots ve revision events | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Craft Defteri açıkken reload, ADS, movement ve pause birbirini bozar mı? | rejection code / result snapshot |
| Persist/rollback | Aynı seçimi çift tıklamak iki build/craft isteği başlatmaz. | delta / before-after evidence |
| İlerleme | Yüzlerce öğe varken her frame tam liste kurulmaz. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Craft Defteri açıkken reload, ADS, movement ve pause birbirini bozar mı?
- **Sınır 2:** Kategori boşsa veya tarif unlock edilmemişse UI ne gösterir?
- **Sınır 3:** 1920×1080 dışı çözünürlükte, gamepad ve klavyede kartlar erişilir mi?
- **Durum 1:** Büyük catalog her frame rebuild edilmez; diff/event update.
- **Durum 2:** Kullanıcının verdiği konsept görsel UI davranışı referansıdır, birebir hazır asset değil.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D046 | Geçerli state/asset ve komut | **Pozitif yol:** Liste filtreleme, kategori ve detay sayfasını hafif implement et. Beklenen sonuç: Yüzlerce öğe varken her frame tam liste kurulmaz. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D046 | Önkoşul ihlali | **Negatif yol:** Search/filter/scroll uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D046 | Aynı istek iki kere | **Tekrar deneme:** Aynı search/filter/scroll isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D046 | FirstTownCore/cell sınırı | **Sahne geçişi:** Craft Defteri -> Yapılar -> Çit: malzeme gösterimi -> seç ve kapat; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D046 | Önceki sprint fixture | **Eski içerik regresyonu:** search/filter/scroll değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D046-1`: Keyboard/mouse/controller input focus tests. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D046-2`: Missing materials and unlock reason readability. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D046-3`: UI open/close and life-cycle without side effects. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Yüzlerce öğe varken her frame tam liste kurulmaz.
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

- [ ] Liste filtreleme, kategori ve detay sayfasını hafif implement et.
- [ ] Yüzlerce öğe varken her frame tam liste kurulmaz.
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

- **Task ID / Branch / HEAD:** `BLD-D046` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S006 Sprint](../SPRINT.md).
- Phase overview: [P02](../../PHASE.md).
- Önceki kart: `BLD-D045` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D047` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
