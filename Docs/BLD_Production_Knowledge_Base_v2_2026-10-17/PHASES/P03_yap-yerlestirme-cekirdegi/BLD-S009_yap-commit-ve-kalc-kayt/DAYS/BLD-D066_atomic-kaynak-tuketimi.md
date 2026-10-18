# BLD-D066 — Atomic kaynak tüketimi

**Phase:** `P03 — Yapı yerleştirme çekirdeği`  
**Sprint:** `BLD-S009 — Yapı commit ve kalıcı kayıt`  
**Kart:** 2/8 · **Alan:** `persistence` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Validasyon sonrası OwnershipTransaction ile materyal commit uygula.

**Kaynak plandaki kabul:** Başarısız create malzeme eksiltmez.

**Oyuncuya somut çıktı:**
- Bu kart, `Yapı commit ve kalıcı kayıt` deneyiminin `atomic kaynak tüketimi` unsurunu tamamlar.
- Doğrudan bağlam: Çit kurma onayı ile inventory çıkarma ve stable structure snapshot.
- Değişiklik ancak `Başarısız create malzeme eksiltmez.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** SaveSession save/capture/hydrate önkoşulları.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** WorldCellManager loaded/unloaded delta ve readiness.
- **Girdi 3:** PersistentEntityId, tombstone, schema migration.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
2. `Assets/LastSignal/Scripts/Runtime/Persistence/PersistentEntityId.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
3. `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
4. `Docs/Last_Signal_Docs_v0.2/sources/17_Save_Load_Migration.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Persistence/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `Atomic kaynak tüketimi` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Atomic kaynak tüketimi` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Başarısız create malzeme eksiltmez.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Aynı structureId iki aktif yapı sahibi olamaz.
- Unloaded cell authoritative deltas kaybolamaz.
- Eski save construction bölümü olmadığı için bozuk sayılmaz.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** Entity/snapshot için stable identity, worldId, cellId, transform, definition revision ve tombstone şemasını yaz. Bu adım `atomic kaynak tüketimi` için kaynak görevdeki "Validasyon sonrası OwnershipTransaction ile materyal commit uygula." hedefiyle sınırlanır.
2. **Adım 2:** Capture öncesi busy/OwnershipTransaction/cell readiness ve world clock durumlarını denetle. Bu adım `atomic kaynak tüketimi` için kaynak görevdeki "Validasyon sonrası OwnershipTransaction ile materyal commit uygula." hedefiyle sınırlanır.
3. **Adım 3:** Yüklü sahne içeriği yerine authoritative world delta ve dynamic structure registry kullanımını tasarla. Bu adım `atomic kaynak tüketimi` için kaynak görevdeki "Validasyon sonrası OwnershipTransaction ile materyal commit uygula." hedefiyle sınırlanır.
4. **Adım 4:** Yeni construction section yoksa legacy save için boş koleksiyon migration davranışı belirle. Bu adım `atomic kaynak tüketimi` için kaynak görevdeki "Validasyon sonrası OwnershipTransaction ile materyal commit uygula." hedefiyle sınırlanır.
5. **Adım 5:** Insert/remove/relocate yapısını tek mutation receipt ve request nonce ile idempotent hale getir. Bu adım `atomic kaynak tüketimi` için kaynak görevdeki "Validasyon sonrası OwnershipTransaction ile materyal commit uygula." hedefiyle sınırlanır.
6. **Adım 6:** Crash, disk error ve load failure durumunda eski geçerli generation korunmasını sağlayacak protokol yaz. Bu adım `atomic kaynak tüketimi` için kaynak görevdeki "Validasyon sonrası OwnershipTransaction ile materyal commit uygula." hedefiyle sınırlanır.
7. **Adım 7:** Cross-cell neighbor lease/owner seçimini deterministik tek hücreye sabitle, çift hydrated structure’ı engelle. Bu adım `atomic kaynak tüketimi` için kaynak görevdeki "Validasyon sonrası OwnershipTransaction ile materyal commit uygula." hedefiyle sınırlanır.
8. **Adım 8:** Save/load, unload/reload, delete/tombstone ve duplicate identity için focused test fixture üret. Bu adım `atomic kaynak tüketimi` için kaynak görevdeki "Validasyon sonrası OwnershipTransaction ile materyal commit uygula." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Başarısız create malzeme eksiltmez.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Atomic kaynak tüketimi`; komşu kartı önceden üstlenme | `BLD-D066` source task / diff |
| Sahiplik | Sahne GameObject listesi save için global truth kabul edilmez. | owner diagram / API |
| Gerçek kaynak | SaveSession save/capture/hydrate önkoşulları | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Build commit sırasında exception ve disk write kesintisi olursa ikincil yapı kalır mı? | rejection code / result snapshot |
| Persist/rollback | Unloaded cell authoritative deltas kaybolamaz. | delta / before-after evidence |
| İlerleme | Başarısız create malzeme eksiltmez. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Build commit sırasında exception ve disk write kesintisi olursa ikincil yapı kalır mı?
- **Sınır 2:** Delete→save→load sonrası tombstone sayesinde yapı geri gelir mi?
- **Sınır 3:** Cell sınırında build iki kere serialize edilir mi?
- **Durum 1:** Sahne GameObject listesi save için global truth kabul edilmez.
- **Durum 2:** Migration yanlışsa original save değişmez; backup korunur.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D066 | Geçerli state/asset ve komut | **Pozitif yol:** Validasyon sonrası OwnershipTransaction ile materyal commit uygula. Beklenen sonuç: Başarısız create malzeme eksiltmez. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D066 | Önkoşul ihlali | **Negatif yol:** Atomic kaynak tüketimi uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D066 | Aynı istek iki kere | **Tekrar deneme:** Aynı atomic kaynak tüketimi isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D066 | FirstTownCore/cell sınırı | **Sahne geçişi:** Çit kurma onayı ile inventory çıkarma ve stable structure snapshot; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D066 | Önceki sprint fixture | **Eski içerik regresyonu:** atomic kaynak tüketimi değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D066-1`: Cross-cell 50 unload/reload cycles. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D066-2`: Crash injection at transaction/save boundaries. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D066-3`: Legacy fixture/missing definition/duplicate ID load. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Başarısız create malzeme eksiltmez.
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

- [ ] Validasyon sonrası OwnershipTransaction ile materyal commit uygula.
- [ ] Başarısız create malzeme eksiltmez.
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

- **Task ID / Branch / HEAD:** `BLD-D066` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S009 Sprint](../SPRINT.md).
- Phase overview: [P03](../../PHASE.md).
- Önceki kart: `BLD-D065` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D067` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
