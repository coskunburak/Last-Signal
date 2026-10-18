# BLD-D012 — İşlem sıralaması

**Phase:** `P00 — Ürün sözleşmesi ve güvenli mimari`  
**Sprint:** `BLD-S002 — Mimari sözleşme ve risk temeli`  
**Kart:** 4/8 · **Alan:** `architecture` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et.

**Kaynak plandaki kabul:** Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir.

**Oyuncuya somut çıktı:**
- Bu kart, `Mimari sözleşme ve risk temeli` deneyiminin `i̇şlem sıralaması` unsurunu tamamlar.
- Doğrudan bağlam: S010 storage/craft ve yeni construction service arasındaki command/transaction boundaries.
- Değişiklik ancak `Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** Mevcut shelter/recipe/storage domain sahiplikleri.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** WorldCellManager readiness ve persistent delta sözleşmesi.
- **Girdi 3:** OwnershipTransaction, stable ID ve save migration sınırları.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
2. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSocket.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
3. `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
4. `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Construction/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `İşlem sıralaması` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `İşlem sıralaması` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Aynı kaynağın iki authoritative sahibi olamaz.
- Yeni sistem ShelterSite üç socket validation koşulunu değiştirmez.
- Preview state, save state veya gerçek world mutation üretmez.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** Bu kartın dokunduğu mevcut runtime sahibi ile önerilen domain owner’ı dependency diagram üzerinde ayır. Bu adım `i̇şlem sıralaması` için kaynak görevdeki "Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et." hedefiyle sınırlanır.
2. **Adım 2:** Public API komutlarını command/query/event olarak sınıflandır; UI’nın domain state yazmasını engelle. Bu adım `i̇şlem sıralaması` için kaynak görevdeki "Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et." hedefiyle sınırlanır.
3. **Adım 3:** Veri tanımlarında stable ID, revision, asset identity ve migration gereksinimlerini tek tek yaz. Bu adım `i̇şlem sıralaması` için kaynak görevdeki "Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et." hedefiyle sınırlanır.
4. **Adım 4:** Transaction başlama, commit, rollback ve busy/save guard sırasını örnek akışla belirt. Bu adım `i̇şlem sıralaması` için kaynak görevdeki "Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et." hedefiyle sınırlanır.
5. **Adım 5:** SaveSession/WorldCellManager ile unloaded world state arasında mutation devrini tasarla. Bu adım `i̇şlem sıralaması` için kaynak görevdeki "Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et." hedefiyle sınırlanır.
6. **Adım 6:** Girdi doğrulamasını negatif/NaN/overflow/duplicate/lost reference vakalarıyla sertleştir. Bu adım `i̇şlem sıralaması` için kaynak görevdeki "Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et." hedefiyle sınırlanır.
7. **Adım 7:** Eski S010/S013 path davranışlarının değişmeyeceğini kod sınırı ve regression matrix ile sabitle. Bu adım `i̇şlem sıralaması` için kaynak görevdeki "Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et." hedefiyle sınırlanır.
8. **Adım 8:** Aynı owner’a sahip iki yeni sistem doğurmadan en dar uygulanabilir klas/sözleşme setini öner. Bu adım `i̇şlem sıralaması` için kaynak görevdeki "Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `İşlem sıralaması`; komşu kartı önceden üstlenme | `BLD-D012` source task / diff |
| Sahiplik | Önerilen ResourceNode, BuildDefinition ve ConstructionService yeni tasarım sınıflarıdır; mevcut repo parçası sayılmaz. | owner diagram / API |
| Gerçek kaynak | Mevcut shelter/recipe/storage domain sahiplikleri | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Oyuncu build confirm sırasında save talep ettiğinde in-flight transaction nasıl ele alınır? | rejection code / result snapshot |
| Persist/rollback | Yeni sistem ShelterSite üç socket validation koşulunu değiştirmez. | delta / before-after evidence |
| İlerleme | Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Oyuncu build confirm sırasında save talep ettiğinde in-flight transaction nasıl ele alınır?
- **Sınır 2:** Loaded cell kapanırken yeni yapı kaydı hangi otoriteye yazılır?
- **Sınır 3:** Yapı iki cell sınırına dokunursa tam bir owner seçilmiş midir?
- **Durum 1:** Önerilen ResourceNode, BuildDefinition ve ConstructionService yeni tasarım sınıflarıdır; mevcut repo parçası sayılmaz.
- **Durum 2:** S010 escrow sözleşmesi uyarlanabilir; kodu kopyalayarak iki ayrı envanter algoritması oluşturulmaz.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D012 | Geçerli state/asset ve komut | **Pozitif yol:** Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et. Beklenen sonuç: Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D012 | Önkoşul ihlali | **Negatif yol:** İşlem sıralaması uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D012 | Aynı istek iki kere | **Tekrar deneme:** Aynı i̇şlem sıralaması isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D012 | FirstTownCore/cell sınırı | **Sahne geçişi:** S010 storage/craft ve yeni construction service arasındaki command/transaction boundaries; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D012 | Önceki sprint fixture | **Eski içerik regresyonu:** i̇şlem sıralaması değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D012-1`: Contract unit/integration mapping. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D012-2`: State ownership and rollback simulations. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D012-3`: Old save/no construction section compatibility. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir.
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

- [ ] Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et.
- [ ] Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir.
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

- **Task ID / Branch / HEAD:** `BLD-D012` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S002 Sprint](../SPRINT.md).
- Phase overview: [P00](../../PHASE.md).
- Önceki kart: `BLD-D011` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D013` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
