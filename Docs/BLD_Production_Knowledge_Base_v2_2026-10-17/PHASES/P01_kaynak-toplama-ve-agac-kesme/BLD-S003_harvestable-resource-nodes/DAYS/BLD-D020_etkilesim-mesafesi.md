# BLD-D020 — Etkileşim mesafesi

**Phase:** `P01 — Kaynak toplama ve ağaç kesme`  
**Sprint:** `BLD-S003 — Harvestable resource nodes`  
**Kart:** 4/8 · **Alan:** `harvest` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle.

**Kaynak plandaki kabul:** Duvar arkasından veya uzaktan kesim reddedilir.

**Oyuncuya somut çıktı:**
- Bu kart, `Harvestable resource nodes` deneyiminin `etkileşim mesafesi` unsurunu tamamlar.
- Doğrudan bağlam: İlk seçilmiş pine/birch ağaçtan odun, dolu envanter ve tekrar yükleme.
- Değişiklik ancak `Duvar arkasından veya uzaktan kesim reddedilir.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** Harvestable ağaç/prototype seçimi ve dekoratif flora ayrımı.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** Balta/alet kimliği, durumu ve vuruş geometrisi.
- **Girdi 3:** WorldItem spawn/pickup ve Pressure noise kanalı.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Assets/LastSignal/Scripts/Runtime/Inventory/WorldItem.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
2. `Assets/LastSignal/Scripts/Runtime/Inventory/PlayerInventory.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
3. `Docs/Last_Signal_Docs_v0.2/sources/12_World_Pressure.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Harvest/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `Etkileşim mesafesi` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Etkileşim mesafesi` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Duvar arkasından veya uzaktan kesim reddedilir.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Dekoratif ağaçların tamamı sessizce harvestable sayılmaz.
- Ödül yalnız authoritative kabul edilen bir vuruş dizisinden çıkar.
- Tükenmiş resource node, unload/load sonrası ücretsiz yenilenmez.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** FirstTownCore vegetation arasında gerçek kaynak düğümleri ile salt dekorasyonu kesin kimlik ve etiketten ayır. Bu adım `etkileşim mesafesi` için kaynak görevdeki "Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle." hedefiyle sınırlanır.
2. **Adım 2:** Vuruş menzili, yönü, hız sınırı ve alet yetkisini player domain’den doğrulat. Bu adım `etkileşim mesafesi` için kaynak görevdeki "Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle." hedefiyle sınırlanır.
3. **Adım 3:** Node damage/stage/remaining-yield state’ini event/receipt ile monoton ilerlet. Bu adım `etkileşim mesafesi` için kaynak görevdeki "Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle." hedefiyle sınırlanır.
4. **Adım 4:** Accepted hit başına animasyon/ses/FX ayrımını, world pressure event hız sınırıyla bağla. Bu adım `etkileşim mesafesi` için kaynak görevdeki "Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle." hedefiyle sınırlanır.
5. **Adım 5:** Node depletion’ı stable ID/cell delta olarak yaz; save/load sonrasında tekrar ücretsiz kaynak üretme. Bu adım `etkileşim mesafesi` için kaynak görevdeki "Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle." hedefiyle sınırlanır.
6. **Adım 6:** Output spawn point, terrain clearance, container ownership ve full inventory davranışını belirt. Bu adım `etkileşim mesafesi` için kaynak görevdeki "Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle." hedefiyle sınırlanır.
7. **Adım 7:** Eşzamanlı iki hit, broken tool ve death/unload yarışında işlemi tekrar oynatmadan sonlandır. Bu adım `etkileşim mesafesi` için kaynak görevdeki "Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle." hedefiyle sınırlanır.
8. **Adım 8:** Görsel hasat kanıtını FPS kamera ve uzak orman silueti açısından ayrı kabul ettir. Bu adım `etkileşim mesafesi` için kaynak görevdeki "Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Duvar arkasından veya uzaktan kesim reddedilir.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Etkileşim mesafesi`; komşu kartı önceden üstlenme | `BLD-D020` source task / diff |
| Sahiplik | Kesim fiziksel devrilmesi isteniyorsa sınırlı görsel efekt olarak değerlendirilir. | owner diagram / API |
| Gerçek kaynak | Harvestable ağaç/prototype seçimi ve dekoratif flora ayrımı | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Ağaç vuruşu sırasında balta kırılırsa ilerleme ve malzeme ne olur? | rejection code / result snapshot |
| Persist/rollback | Ödül yalnız authoritative kabul edilen bir vuruş dizisinden çıkar. | delta / before-after evidence |
| İlerleme | Duvar arkasından veya uzaktan kesim reddedilir. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Ağaç vuruşu sırasında balta kırılırsa ilerleme ve malzeme ne olur?
- **Sınır 2:** Envanter doluyken kaynaklar sahnede güvenli ve kalıcı kalır mı?
- **Sınır 3:** Aynı ağaca tek frame içinde iki geçerli hit gelirse iki ödül oluşur mu?
- **Durum 1:** Kesim fiziksel devrilmesi isteniyorsa sınırlı görsel efekt olarak değerlendirilir.
- **Durum 2:** Vuruş sesi frame başına değil olay ritmine göre World Pressure kaydı üretir.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D020 | Geçerli state/asset ve komut | **Pozitif yol:** Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle. Beklenen sonuç: Duvar arkasından veya uzaktan kesim reddedilir. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D020 | Önkoşul ihlali | **Negatif yol:** Etkileşim mesafesi uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D020 | Aynı istek iki kere | **Tekrar deneme:** Aynı etkileşim mesafesi isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D020 | FirstTownCore/cell sınırı | **Sahne geçişi:** İlk seçilmiş pine/birch ağaçtan odun, dolu envanter ve tekrar yükleme; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D020 | Önceki sprint fixture | **Eski içerik regresyonu:** etkileşim mesafesi değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D020-1`: Valid/invalid tool-hit matrix. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D020-2`: Harvest+save/load+cell roundtrip. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D020-3`: Inventory full + duplicate hit + FPS invariance. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Duvar arkasından veya uzaktan kesim reddedilir.
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

- [ ] Menzil, görüş, kapsül ve oyuncu hayatta olma kontrolleri ekle.
- [ ] Duvar arkasından veya uzaktan kesim reddedilir.
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

- **Task ID / Branch / HEAD:** `BLD-D020` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S003 Sprint](../SPRINT.md).
- Phase overview: [P01](../../PHASE.md).
- Önceki kart: `BLD-D019` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D021` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
