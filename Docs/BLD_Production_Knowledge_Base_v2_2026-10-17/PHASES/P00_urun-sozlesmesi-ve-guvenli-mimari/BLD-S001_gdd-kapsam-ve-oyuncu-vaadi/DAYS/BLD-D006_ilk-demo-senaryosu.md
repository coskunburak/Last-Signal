# BLD-D006 — İlk demo senaryosu

**Phase:** `P00 — Ürün sözleşmesi ve güvenli mimari`  
**Sprint:** `BLD-S001 — GDD kapsam ve oyuncu vaadi`  
**Kart:** 6/8 · **Alan:** `product` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla.

**Kaynak plandaki kabul:** Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir.

**Oyuncuya somut çıktı:**
- Bu kart, `GDD kapsam ve oyuncu vaadi` deneyiminin `i̇lk demo senaryosu` unsurunu tamamlar.
- Doğrudan bağlam: FirstTownCore sığınaktan çıkış sonrası çit/barınak yapma, oyuncu beklentisi ve kontrollü zone ayrımı.
- Değişiklik ancak `Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** GDD v0.2 karar hiyerarşisi ve anti-hedefler.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** Oyuncu vaadi: hazırlık → keşif → toparlanma → ilerleme.
- **Girdi 3:** Tek geliştiricili ürün kapsamı ve FirstTownCore ilk dilimi.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Docs/Last_Signal_Docs_v0.2/sources/02_Vizyon_Donguler_ve_Dunya.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
2. `Docs/Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
3. `Docs/Last_Signal_Production_Plan_v1.0_TR/management/02_Kapsam_Kararlar_ve_Degisiklik.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Design/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `İlk demo senaryosu` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `İlk demo senaryosu` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Mevcut no-free-building kararı kayıtsız silinmez; ADR olarak açıkça değişir.
- Yeni Must eklenirse eşdeğer işin çıkması veya takvimin uzaması kayıtlanır.
- UI veya teknik özellik tek başına oyuncu değeri sayılmaz.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** Var olan GDD kararını satır/sürüm bazında bul; uyumlu olanı ve açık çelişkiyi ayrı tabloya yaz. Bu adım `i̇lk demo senaryosu` için kaynak görevdeki "Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla." hedefiyle sınırlanır.
2. **Adım 2:** Bu kartın oyuncuya verdiği eylem→geri bildirim→sonuç zincirini beş dakikalık bir senaryoda ifade et. Bu adım `i̇lk demo senaryosu` için kaynak görevdeki "Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla." hedefiyle sınırlanır.
3. **Adım 3:** Yeni kararın inşa alanı, oyuncu güvenliği ve single-player kapsamına etkisini kaydet. Bu adım `i̇lk demo senaryosu` için kaynak görevdeki "Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla." hedefiyle sınırlanır.
4. **Adım 4:** Must/Should/Could/Out değişiminin mevcut S010/S023/FirstTownCore planlarıyla bağını kur. Bu adım `i̇lk demo senaryosu` için kaynak görevdeki "Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla." hedefiyle sınırlanır.
5. **Adım 5:** Kapsamı bozabilecek iki alternatif tasarımı kıyasla ve maliyet/gain gerekçesini yaz. Bu adım `i̇lk demo senaryosu` için kaynak görevdeki "Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla." hedefiyle sınırlanır.
6. **Adım 6:** Bağımlılık ve görünür demo düzeyi için başarı/başarısızlık eşiklerini ölçülebilir tanımla. Bu adım `i̇lk demo senaryosu` için kaynak görevdeki "Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla." hedefiyle sınırlanır.
7. **Adım 7:** Bu kartın tamamlanması için gerekli ADR, traceability ve sahibi tanımlarını doldur. Bu adım `i̇lk demo senaryosu` için kaynak görevdeki "Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla." hedefiyle sınırlanır.
8. **Adım 8:** Son tasarımı owner onayından önce “proposed” bırak; başka belgeyi sessizce yeniden yazma. Bu adım `i̇lk demo senaryosu` için kaynak görevdeki "Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `İlk demo senaryosu`; komşu kartı önceden üstlenme | `BLD-D006` source task / diff |
| Sahiplik | Karar sahibi ürün tasarımını onaylar; teknik agent onay verilmiş gibi GDD değiştirmez. | owner diagram / API |
| Gerçek kaynak | GDD v0.2 karar hiyerarşisi ve anti-hedefler | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Oyuncu sıfır materyalle sığınağı geliştiriyor mu? Hayır; gerekli kaynak yolu görünür. | rejection code / result snapshot |
| Persist/rollback | Yeni Must eklenirse eşdeğer işin çıkması veya takvimin uzaması kayıtlanır. | delta / before-after evidence |
| İlerleme | Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Oyuncu sıfır materyalle sığınağı geliştiriyor mu? Hayır; gerekli kaynak yolu görünür.
- **Sınır 2:** Oyuncu inşa etmek istemediğinde temel survival rotası kilitleniyor mu? Kilitlenmemeli.
- **Sınır 3:** İlk sürüme büyük serbest yıkım veya multiplayer sızıyor mu? Kapsam dışı.
- **Durum 1:** Karar sahibi ürün tasarımını onaylar; teknik agent onay verilmiş gibi GDD değiştirmez.
- **Durum 2:** Mevcut GDD içindeki tek-player ve yoğun harita önceliği korunur.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D006 | Geçerli state/asset ve komut | **Pozitif yol:** Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla. Beklenen sonuç: Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D006 | Önkoşul ihlali | **Negatif yol:** İlk demo senaryosu uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D006 | Aynı istek iki kere | **Tekrar deneme:** Aynı i̇lk demo senaryosu isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D006 | FirstTownCore/cell sınırı | **Sahne geçişi:** FirstTownCore sığınaktan çıkış sonrası çit/barınak yapma, oyuncu beklentisi ve kontrollü zone ayrımı; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D006 | Önceki sprint fixture | **Eski içerik regresyonu:** i̇lk demo senaryosu değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D006-1`: GDD contradiction scan. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D006-2`: MUST→oyuncu değeri ve kabul izi. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D006-3`: Sprint→bağımlılık→kaynak bütçesi tutarlılık incelemesi. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir.
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

- [ ] Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla.
- [ ] Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir.
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

- **Task ID / Branch / HEAD:** `BLD-D006` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S001 Sprint](../SPRINT.md).
- Phase overview: [P00](../../PHASE.md).
- Önceki kart: `BLD-D005` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D007` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
