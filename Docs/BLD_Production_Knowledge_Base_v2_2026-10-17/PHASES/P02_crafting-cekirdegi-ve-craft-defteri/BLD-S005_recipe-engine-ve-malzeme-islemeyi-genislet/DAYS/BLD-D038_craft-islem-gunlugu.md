# BLD-D038 — Craft işlem günlüğü

**Phase:** `P02 — Crafting çekirdeği ve Craft Defteri`  
**Sprint:** `BLD-S005 — Recipe engine ve malzeme işlemeyi genişlet`  
**Kart:** 6/8 · **Alan:** `craft` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Request ID, revision, accepted/rejected reason ve receipt üret.

**Kaynak plandaki kabul:** İki kere submit aynı craftı iki kez üretmez.

**Oyuncuya somut çıktı:**
- Bu kart, `Recipe engine ve malzeme işlemeyi genişlet` deneyiminin `craft işlem günlüğü` unsurunu tamamlar.
- Doğrudan bağlam: Kütük + araç + station kullanarak plank üretimi ve escrow güvenliği.
- Değişiklik ancak `İki kere submit aynı craftı iki kez üretmez.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** S010 ShelterRecipe/ShelterProduction ve mevcut escrow refund semantiği.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** StableItemId, stack sayımı, kaynak revision, tools/station gates.
- **Girdi 3:** Çok girdi ve çıktı tanım tasarımı ile recipe catalog.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterRecipe.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
2. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterProduction.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
3. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Crafting/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `Craft işlem günlüğü` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Craft işlem günlüğü` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** İki kere submit aynı craftı iki kez üretmez.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Bir girdi aynı anda iki crafting job tarafından kullanılamaz.
- Cancel, full-output, save/load hiçbir materyali yok etmez veya çoğaltmaz.
- Yeni tarifler eski ShelterRecipe ve legacy save davranışlarını değiştirmez.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** ShelterRecipe tek girdi yolunun korunduğunu doğrula; yeni tarifin ihtiyaç duyduğu çok girdiyi katalogla. Bu adım `craft işlem günlüğü` için kaynak görevdeki "Request ID, revision, accepted/rejected reason ve receipt üret." hedefiyle sınırlanır.
2. **Adım 2:** Her input için itemId, quantity, source, tool, station ve unlock koşullarını deterministik sıralama ile tanımla. Bu adım `craft işlem günlüğü` için kaynak görevdeki "Request ID, revision, accepted/rejected reason ve receipt üret." hedefiyle sınırlanır.
3. **Adım 3:** Craft talebi için inventory revision snapshot al, escrow rezervasyonuna yalnız bir domain API aç. Bu adım `craft işlem günlüğü` için kaynak görevdeki "Request ID, revision, accepted/rejected reason ve receipt üret." hedefiyle sınırlanır.
4. **Adım 4:** Başarısız station/input/output kontrolünde materyal değiştirmeyen reject branch tasarla. Bu adım `craft işlem günlüğü` için kaynak görevdeki "Request ID, revision, accepted/rejected reason ve receipt üret." hedefiyle sınırlanır.
5. **Adım 5:** İptal, dolu output, güç kesintisi, pause ve save/load arasında job lifecycle state diyagramı üret. Bu adım `craft işlem günlüğü` için kaynak görevdeki "Request ID, revision, accepted/rejected reason ve receipt üret." hedefiyle sınırlanır.
6. **Adım 6:** Log→plank veya ilgili üretimin kayıp/çıktı miktarını mass/stack sınırlarıyla simüle et. Bu adım `craft işlem günlüğü` için kaynak görevdeki "Request ID, revision, accepted/rejected reason ve receipt üret." hedefiyle sınırlanır.
7. **Adım 7:** Tarif listesinde zero-cost cycle, self-input output ve integer overflow doğrulamasını yap. Bu adım `craft işlem günlüğü` için kaynak görevdeki "Request ID, revision, accepted/rejected reason ve receipt üret." hedefiyle sınırlanır.
8. **Adım 8:** Tekrar submit/duplicate request için idempotent receipt ve focused QA beklenen sonucu tanımla. Bu adım `craft işlem günlüğü` için kaynak görevdeki "Request ID, revision, accepted/rejected reason ve receipt üret." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `İki kere submit aynı craftı iki kez üretmez.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Craft işlem günlüğü`; komşu kartı önceden üstlenme | `BLD-D038` source task / diff |
| Sahiplik | Craft notebook kaynak sayımı gösterir ama authoritative item mutation yapmaz. | owner diagram / API |
| Gerçek kaynak | S010 ShelterRecipe/ShelterProduction ve mevcut escrow refund semantiği | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Üç ayrı item gerekli ama üçüncü eksikse hiçbir girdi tüketilmemeli. | rejection code / result snapshot |
| Persist/rollback | Cancel, full-output, save/load hiçbir materyali yok etmez veya çoğaltmaz. | delta / before-after evidence |
| İlerleme | İki kere submit aynı craftı iki kez üretmez. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Üç ayrı item gerekli ama üçüncü eksikse hiçbir girdi tüketilmemeli.
- **Sınır 2:** Station üretim ortasında güç kaybederse ne olur: pause/abort açık kural.
- **Sınır 3:** Output doluyken CompletedWaitingOutput davranışı korunur.
- **Durum 1:** Craft notebook kaynak sayımı gösterir ama authoritative item mutation yapmaz.
- **Durum 2:** Time basis açık: handcraft simulation seconds; workstation world time (tasarım kararı).
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D038 | Geçerli state/asset ve komut | **Pozitif yol:** Request ID, revision, accepted/rejected reason ve receipt üret. Beklenen sonuç: İki kere submit aynı craftı iki kez üretmez. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D038 | Önkoşul ihlali | **Negatif yol:** Craft işlem günlüğü uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D038 | Aynı istek iki kere | **Tekrar deneme:** Aynı craft işlem günlüğü isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D038 | FirstTownCore/cell sınırı | **Sahne geçişi:** Kütük + araç + station kullanarak plank üretimi ve escrow güvenliği; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D038 | Önceki sprint fixture | **Eski içerik regresyonu:** craft işlem günlüğü değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D038-1`: Multi-input escrow/refund and overflow. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D038-2`: Recipe cyclic free-resource conversion detection. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D038-3`: Pause/quit/save/reload/double-submit. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** İki kere submit aynı craftı iki kez üretmez.
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

- [ ] Request ID, revision, accepted/rejected reason ve receipt üret.
- [ ] İki kere submit aynı craftı iki kez üretmez.
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

- **Task ID / Branch / HEAD:** `BLD-D038` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S005 Sprint](../SPRINT.md).
- Phase overview: [P02](../../PHASE.md).
- Önceki kart: `BLD-D037` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D039` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
