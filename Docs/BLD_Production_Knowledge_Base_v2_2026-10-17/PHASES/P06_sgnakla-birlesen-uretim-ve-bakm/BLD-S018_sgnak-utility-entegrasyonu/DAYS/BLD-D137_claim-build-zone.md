# BLD-D137 — Claim+build zone

**Phase:** `P06 — Sığınakla birleşen üretim ve bakım`  
**Sprint:** `BLD-S018 — Sığınak utility entegrasyonu`  
**Kart:** 1/8 · **Alan:** `shelter` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir.

**Kaynak plandaki kabul:** Eski 3 socket doğrulaması aynen çalışır.

**Oyuncuya somut çıktı:**
- Bu kart, `Sığınak utility entegrasyonu` deneyiminin `claim+build zone` unsurunu tamamlar.
- Doğrudan bağlam: Barınağa yatak, su toplama, jeneratör ve ışık işlevi kazandırma.
- Değişiklik ancak `Eski 3 socket doğrulaması aynen çalışır.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** ShelterSite claim/installed socket ve ShelterProduction.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** Power/fuel/generator noise ve world time.
- **Girdi 3:** BuildZone, structure function and room shelter evaluation.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
2. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterProduction.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
3. `Docs/Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Shelter/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `Claim+build zone` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Claim+build zone` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Eski 3 socket doğrulaması aynen çalışır.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Üs claim zombies’i silmez veya invulnerability eklemez.
- Mevcut sabit modüller ve storage erişim koşulları geriye uyumlu kalır.
- Power kaybı yatak/claim/ownership kaybı anlamına gelmez.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** Mevcut ShelterSite claim/Bed/Storage/Workbench soketlerini değiştirmeden yeni yapının faydasını tanımla. Bu adım `claim+build zone` için kaynak görevdeki "ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir." hedefiyle sınırlanır.
2. **Adım 2:** Utility state için unlocked capability ile currently operational flag ayrımını uygula. Bu adım `claim+build zone` için kaynak görevdeki "ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir." hedefiyle sınırlanır.
3. **Adım 3:** İstasyon uzaklık/erişim kontrolünü depo, player ve hangi cell loaded bilgisine bağla. Bu adım `claim+build zone` için kaynak görevdeki "ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir." hedefiyle sınırlanır.
4. **Adım 4:** Jeneratör/ışık/radyo/su gibi kullanımların yakıt, world time ve gürültü maliyetlerini açıkla. Bu adım `claim+build zone` için kaynak görevdeki "ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir." hedefiyle sınırlanır.
5. **Adım 5:** Sığınak çatı/kapı/yağmur güvenliği için görselden bağımsız fiziksel doğrulama ve anlamlı kullanıcı mesajı seç. Bu adım `claim+build zone` için kaynak görevdeki "ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir." hedefiyle sınırlanır.
6. **Adım 6:** Unloaded shelter üzerinde üretim/enerji/su simülasyonu için world time analytical advancement kararı yaz. Bu adım `claim+build zone` için kaynak görevdeki "ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir." hedefiyle sınırlanır.
7. **Adım 7:** Fayda kaybında ownership/storage/claim korunumu ve recovery yolu belirt. Bu adım `claim+build zone` için kaynak görevdeki "ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir." hedefiyle sınırlanır.
8. **Adım 8:** Yeni yapı etkisi için öncesi/sonrası gameplay kıyasını, kaynak ve tehdit maliyetleriyle tanımla. Bu adım `claim+build zone` için kaynak görevdeki "ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Eski 3 socket doğrulaması aynen çalışır.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Claim+build zone`; komşu kartı önceden üstlenme | `BLD-D137` source task / diff |
| Sahiplik | Kullanılabilir çalışma tezgâhı varsa yeni crafting ownership kopyası oluşturma. | owner diagram / API |
| Gerçek kaynak | ShelterSite claim/installed socket ve ShelterProduction | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Kısmi çatıda RainRoof/inside flag yanlışlıkla kuru kabul ediyor mu? | rejection code / result snapshot |
| Persist/rollback | Mevcut sabit modüller ve storage erişim koşulları geriye uyumlu kalır. | delta / before-after evidence |
| İlerleme | Eski 3 socket doğrulaması aynen çalışır. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Kısmi çatıda RainRoof/inside flag yanlışlıkla kuru kabul ediyor mu?
- **Sınır 2:** Jeneratör kapalıyken crafting istasyonu çalışıyor mu; tasarım kuralları ne?
- **Sınır 3:** Yeni depo iki farklı authoritative envanter açıyor mu?
- **Durum 1:** Kullanılabilir çalışma tezgâhı varsa yeni crafting ownership kopyası oluşturma.
- **Durum 2:** Sığınak güvenliği kararlı ve anlaşılır; zorunlu nightly raids kapsam dışıdır.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D137 | Geçerli state/asset ve komut | **Pozitif yol:** ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir. Beklenen sonuç: Eski 3 socket doğrulaması aynen çalışır. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D137 | Önkoşul ihlali | **Negatif yol:** Claim+build zone uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D137 | Aynı istek iki kere | **Tekrar deneme:** Aynı claim+build zone isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D137 | FirstTownCore/cell sınırı | **Sahne geçişi:** Barınağa yatak, su toplama, jeneratör ve ışık işlevi kazandırma; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D137 | Önceki sprint fixture | **Eski içerik regresyonu:** claim+build zone değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D137-1`: Claim+storage+socket regression. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D137-2`: Roof/wetness/power status transitions. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D137-3`: Unloaded shelter fuel/jobs deterministic recovery. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Eski 3 socket doğrulaması aynen çalışır.
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

- [ ] ShelterSite'ı bozmadan zone privilege ve zone claim arasını ilişkilendir.
- [ ] Eski 3 socket doğrulaması aynen çalışır.
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

- **Task ID / Branch / HEAD:** `BLD-D137` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S018 Sprint](../SPRINT.md).
- Phase overview: [P06](../../PHASE.md).
- Sonraki kart: `BLD-D138` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
