# BLD-D074 — Pilot asset kit

**Phase:** `P04 — Entegre ilk playable loop`  
**Sprint:** `BLD-S010 — İlk entegre playable vertical slice`  
**Kart:** 2/8 · **Alan:** `art` · **Durum:** `PROPOSED / NOT_RUN`  
**Sorumluluk:** Coding agent implementasyonu hazırlar; Unity test/build/profiler yalnız kullanıcı tarafından çalıştırılır.

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Ürün davranışı ve görev sözleşmesi

**Kaynak plandaki işlem:** Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir.

**Kaynak plandaki kabul:** Vendor kaynakları değişmez.

**Oyuncuya somut çıktı:**
- Bu kart, `İlk entegre playable vertical slice` deneyiminin `pilot asset kit` unsurunu tamamlar.
- Doğrudan bağlam: Orman -> odun -> tahta -> çit -> save/load ilk 20–35 dk deneyim.
- Değişiklik ancak `Vendor kaynakları değişmez.` durumunu oyuncu veya teknik kanıt ile gösterirse kabul edilebilir.
- Yeni menü/işaretçi/araç üretmek kendi başına kullanıcı değeri değildir.

## 2. Mevcut durum / hedef durum / kapsam dışı

- **Kontrol edilecek girdi (mevcut/proposed ayrımı yerel kodda doğrulanmalı):** S013 Art Bible ve modüler standart.
- **Hedef:** Yukarıdaki davranış sözleşmesinin tekrar üretilebilir, save-safe ve kullanıcı tarafından doğrulanabilir karşılığı.
- **Girdi 2:** Approved FirstTownCore stylized realism hedefi.
- **Girdi 3:** ThirdParty lisans/URP/import durumu.
- **Kapsam dışı:** Aynı göreve zorunlu olmayan network/co-op, yeni dünya oluşturma, shader framework ve kapsam dışı büyük içerik.
- **Uygulama izni:** Eski kodda davranış zaten varsa yeniden yazma; farkı ve kanıtını göster.

## 3. Hedefli inceleme — minimum context

Bu göreve başlamadan sadece gerçek ilişki kurulabilen kaynakları oku:

1. `Docs/Implementation/S013/S013_ART_BIBLE.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
2. `Docs/Implementation/S013/S013_MODULAR_STANDARD.md` — tasarım sözleşmesini bul, yalnız ilgili sembol/bölümü context’e al.
3. `Assets/LastSignal/Scripts/Runtime/Inventory/WorldItem.cs` — runtime/doğrulama sınırını bul, yalnız ilgili sembol/bölümü context’e al.
4. `git status --short`, branch/HEAD ve ilgili dosyaların dirty diff’ini kaydet.
5. Yapılması planlanan değişiklik gerçekten bu dosyaların içinde mi, değilse yeni owner yolunu belirle.
6. Büyük ThirdParty texture/FBX dosyalarının binary içeriğini context’e alma; `.meta`, prefab ve importer bilgisini hedefli tara.
7. Önerilen klasör başlangıcı: `Assets/LastSignal/Scripts/Runtime/Construction/Art/` (KESİN DOSYA YOLU DEĞİL; yerel ağaçta doğrula).

## 4. API, veri ve sahiplik kontratı

- **Trigger / request:** `Pilot asset kit` için kullanıcı girdisi, world event veya Editor authoring entry point açıkça belirlenir.
- **Request payload:** `Pilot asset kit` işleminin kullandığı ID, hedef konum/bağlam, beklenen revision ve tercih edilen parametreler alan alan yazılır.
- **Validation:** Vendor kaynakları değişmez.
- **Success receipt:** Eski ve yeni state, miktar/kimlik/koordinat farkı ve request sonucu raporlanır.
- **Failure receipt:** Hatalı koşulda state değiştirilmeden nedeni ve kontrollü tekrar stratejisi döndürülür.
- **Owner:** UI/preview doğrudan authoritative inventory veya persistent world state değiştiremez.
- **Timing:** Sim time, world time veya Editor-only bağlamı açıkça seçilir; frame time’a istemeden bağlanmaz.
- **Reentrancy:** Çift event, hızlı tıklama, pause ve scene unload sıraları tasarımda tanımlıdır.
- **Serialization:** Persisted ise stable ID/revision/owner/delta kullan; değilse transient kalmasının nedenini yaz.

### Bozulmaması gereken kurallar

- Production source asset korunur; project-owned prefab/material varyantları kullanılır.
- İlk kişi kamera ölçeğinde kalite kabulü olmadan final art onayı verilmez.
- Görsel varlık teknik çatışma/collider/LOD gereksinimini karşılamalı.
- Kaynak tüketimi, save ve instantiation ilgili domain transaction dışına taşınmaz.
- Korunan reference/prefab/scene GUID’leri gereksiz yeniden üretilmez.

## 5. Detaylı uygulama sırası (agent iş emri)

Aşağıdaki adımları küçük, gözden geçirilebilir diff’ler halinde yürüt:

1. **Adım 1:** S013 stylized-realism palette ve Unity URP shader/material standardına uygun görsel kriter yaz. Bu adım `pilot asset kit` için kaynak görevdeki "Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir." hedefiyle sınırlanır.
2. **Adım 2:** Asset envanterinden gerçek prefab/mesh/texture yollarını doğrula; mevcut vendor dosyasını değiştirme. Bu adım `pilot asset kit` için kaynak görevdeki "Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir." hedefiyle sınırlanır.
3. **Adım 3:** Hero asset ve repetition çeşitlerini silhouette, UV, normal, scale ve pivot açısından incele. Bu adım `pilot asset kit` için kaynak görevdeki "Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir." hedefiyle sınırlanır.
4. **Adım 4:** Viewmodel/FPS oyuncu eli ve aynı anda dünyada görünen modelin tutarlılığını değerlendir. Bu adım `pilot asset kit` için kaynak görevdeki "Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir." hedefiyle sınırlanır.
5. **Adım 5:** LOD, collider, shadow, alpha overdraw ve shader variation maliyetlerini test planına bağla. Bu adım `pilot asset kit` için kaynak görevdeki "Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir." hedefiyle sınırlanır.
6. **Adım 6:** Rain, day, sunset, dusk, flashlight altında değişen materyal/FX okumasını denetle. Bu adım `pilot asset kit` için kaynak görevdeki "Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir." hedefiyle sınırlanır.
7. **Adım 7:** Yerleştirme geçerli/ret materyallerini kontrast ve renk körlüğü açısından kalibre ettir. Bu adım `pilot asset kit` için kaynak görevdeki "Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir." hedefiyle sınırlanır.
8. **Adım 8:** Bütün gerçek ekran görüntülerini kullanıcı alır; konsept resimleri Unity acceptance kanıtı sayma. Bu adım `pilot asset kit` için kaynak görevdeki "Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir." hedefiyle sınırlanır.
9. **Adım 9 — Kart özel kabul bağlantısı:** Üretilen sınıf/prefab/veri ile `Vendor kaynakları değişmez.` cümlesi arasındaki birebir kanıtı yaz; varsayım veya tahmini PASS kullanma.
10. **Adım 10 — Teslim:** Changed-files, before/after, risk, test runner ve yeniden test komutunu `HANDOFF` standardında hazırla; kullanıcı sonucu gelmeden gate kapatma.

## 6. Implementation karar tablosu

| Karar boyutu | Bu karttaki zorunlu seçim | İnceleme kanıtı |
|---|---|---|
| Kapsam | `Pilot asset kit`; komşu kartı önceden üstlenme | `BLD-D074` source task / diff |
| Sahiplik | Meshy sprinti ayrı ve ertelenmiş; yeni asset zorunlu değil. | owner diagram / API |
| Gerçek kaynak | S013 Art Bible ve modüler standart | ilgili `.cs` / GDD pasajı |
| Hata davranışı | Asset çok ayrıntılı ama collider/LOD yoksa kabul edilmeli mi? Hayır. | rejection code / result snapshot |
| Persist/rollback | İlk kişi kamera ölçeğinde kalite kabulü olmadan final art onayı verilmez. | delta / before-after evidence |
| İlerleme | Vendor kaynakları değişmez. | owner-run validation |

## 7. Edge cases ve karşı oyun örnekleri

- **Sınır 1:** Asset çok ayrıntılı ama collider/LOD yoksa kabul edilmeli mi? Hayır.
- **Sınır 2:** Yeşil ghost materyali alpha ve z-fighting sorunu yaratıyor mu?
- **Sınır 3:** Yağmur/günbatımı ile malzemelerin tepkileri tutarlı mı?
- **Durum 1:** Meshy sprinti ayrı ve ertelenmiş; yeni asset zorunlu değil.
- **Durum 2:** Mevcut free assetlerde ticari lisans kontrolü yapılmadan release onayı yok.
- **Zayıf ağ veya servis:** Single-player ana döngüsü dış hesaba/ağa bağımlı hâle getirilemez.
- **Scene/asset mismatch:** Dosyada/prefabda beklenen özellik yoksa sessiz fallback ile PASS deme, kapsamlı blocker ve alternatif üret.

## 8. Kabul matrisi — örnek senaryolar

| Senaryo | Hazırlık | Beklenen gözlemlenebilir sonuç | Kanıt |
|---|---|---|---|
| POS-BLD-D074 | Geçerli state/asset ve komut | **Pozitif yol:** Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir. Beklenen sonuç: Vendor kaynakları değişmez. | Test receipt + gerekiyorsa screenshot |
| NEG-BLD-D074 | Önkoşul ihlali | **Negatif yol:** Pilot asset kit uygulaması için en yakın engelleyici önkoşul sağlanmadığında ret nedeni ve değişmemiş authoritative state kanıtlanır. | Hata kodu + unchanged state |
| DUP-BLD-D074 | Aynı istek iki kere | **Tekrar deneme:** Aynı pilot asset kit isteği iki defa gönderildiğinde tanımın izin verdiği dışında ilave ödül, nesne veya kaynak işlemi oluşmaz. | Before/after quantity/IDs |
| WORLD-BLD-D074 | FirstTownCore/cell sınırı | **Sahne geçişi:** Orman -> odun -> tahta -> çit -> save/load ilk 20–35 dk deneyim; scene/cell unload veya yeniden yükleme varsa geriye kalan state açıklanır. | Save/load veya scene diff |
| REG-BLD-D074 | Önceki sprint fixture | **Eski içerik regresyonu:** pilot asset kit değişikliğinden sonra shelter inventory, S010 craft ve FirstTownCore davranışı hâlâ geçerli olmalıdır. | Focused regression |

## 9. Test tasarımı ve kullanıcıya devir

### Kod eklenirken hazırlanacak test tanımları

1. `BLD-D074-1`: FPS screenshot before/after matrix. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
2. `BLD-D074-2`: LOD/material/UV/pivot/collider inspection. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
3. `BLD-D074-3`: Shader/URP/night/weather quality check. Girdi/çıktı assertions, beklenen mesaj ve state farkını açıkça tanımla.
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

- **Ürün göstergesi:** Vendor kaynakları değişmez.
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

- [ ] Bir kesilebilir ağaç, log drop, plank, çit ve küçük platform prefablarını eşleştir.
- [ ] Vendor kaynakları değişmez.
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

- **Task ID / Branch / HEAD:** `BLD-D074` / ___ / ___
- **Changed files + neden:** ___
- **Runtime/asset değişimi ve ownership:** ___
- **Beklenen oyuncu davranışı / uygulanmış gerçek davranış:** ___
- **Testler (komut, receipt, PASS/FAIL/NOT_RUN):** ___
- **Visual/UX kabul (Game View):** ___
- **Save ve world delta durumu:** ___
- **Blocked/open risks / sonraki en küçük action:** ___

## 14. Bağımlılıklar ve navigasyon

- Üst doküman: [BLD-S010 Sprint](../SPRINT.md).
- Phase overview: [P04](../../PHASE.md).
- Önceki kart: `BLD-D073` (aynı DAYS klasörü).
- Sonraki kart: `BLD-D075` (aynı DAYS klasörü).
- Paylaşılan canonical standartlar: `../../../00_GOVERNANCE/` altındaki ownership, manual QA, asset ve persistence kontratları.

> **Kapanış kuralı:** Kartın kodu yazılmış olabilir; kullanıcı test ve görsel kabulü olmadan PASS veya sprint DONE ilan edilemez.
