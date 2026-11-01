# WLD-G2 — izole aday aracı

**ACCEPTED — yapı/izolasyon kapsamı.** Owner 24 Ekim'de düzeltilmiş `G20261024T111948295-053b5630` çalıştırmasının `candidate saved and structurally verified` ve `SAVED_CANDIDATE_VERIFIED` sonuçlarını gönderdi. Yerel receipt/hash doğrulaması eşleşti:18.994 nesne,602 asset, onaylı bounds/save slot. `G2_Acceptance.json` kabulü exact receipt'e bağlar. Aşağıdaki ilk hata/yeniden deneme notları geçmiş kayıttır. Terrain/nav/gameplay/performance kabulü verilmedi; üretim promotion yetkisi yoktur. Sonraki owner adımı G3 belgesindedir.

## Uygulanan kapsam

- `WorldStageAuthoring.cs`: iki explicit Editor menüsü; oluşturma ve kaydedilmiş adayı salt okunur doğrulama.
- `WorldStageIsolation.cs`: bağımlılık keşfi, kaynak hashleri, yeni GUID'li kopyalar, prefab/scene YAML referans eşlemesi, binary native asset referans eşlemesi ve importer remap izolasyonu.
- G0/G1 audit/pencere açıklaması onaylanan G2 menüsünü gösterir; kaynak preview veri modeli ve Plan.json değişmez.
- `Tools/WorldReconstruction/verify_foundation.py`: G1 kanıtını korur, varsa başarılı G2 receipt/asset hashlerini de denetler. Native kabul veya oynanış testi değildir.

Kaynak yalnız `Assets/LastSignal/Scenes/Production/FirstTownMain_HDRP.unity`. Aday yalnız `Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_Stage.unity`. Plan SHA-256 onaylanan `22423e0e3085a53a5f37f3ff2465411ee1493d8b43dedd581d471ff44c98a38a` ile sabitlenmiştir; planı değiştirmek sessizce onayın kapsamını genişletmez.

Adayın VehicleWorld bounds min(-750,-40,-250), max(250,80,250), center(-250,20,0), size(1000,120,500). SaveSession yuvası `wld_s001_naturalworld_stage`. Mevcut tek SessionFlow/WorldCellManager/VehicleWorld/SaveSession kalır; cell definitions boş olmalıdır. Runtime save doğrulaması ve source yuvası değiştirilmez. Eski kayıt dosyaları taşınmaz. Ayrı varsayılan slot, elle başka dosya yüklenmesine karşı yeni bir scene kimlik sistemi değildir.

G2 yalnız izolasyondur: aynı tek 500×500 terrain ve bütün yükseklikler korunur. Resident hacmini genişletmek batıda zemin, collider, rota veya navmesh yaratmaz. Bu aşamada batıya yürüyüş/sürüş kabulü istenmez. G3 topografya, G4 yol/NavMesh ve G8 restore testleri sonradır.

## Kaynak/asset koruması

Oluşturma başlamadan tek kaydedilmiş Main, Play/prefab/import dışında çalışma, 23 G0 dosyasının hash eşleşmesi ve kaynak terrain kimliği aranır. Kirli bağımlılık/importer reddedilir. Var olan aday scene, meta veya başarı receipt'i üzerine yazılmaz.

Native `.asset`, `.mat`, `.terrainlayer`, `.prefab`, `.lighting` bağımlılıkları `Assets/LastSignal/Art/WorldReconstruction/WLD-S001/Generations/G<utc>-<unique>/` içine kopyalanır. Referans zincirinin kopmaması için runtime definition assetleri de yeni GUID ile kopyalanabilir; içerik/stable ID değiştirilmez. Yeni bir runtime sistemi kurulmaz.

Bazı FBX importer'ları kaynak materyallere doğrudan referans verir. Böyle modeller de kopyalanıp yalnız kopyanın importer metadata referansları yönlendirilir. Yeni model indirme, vendor kaynağı taşıma/değiştirme veya global materyal dönüşümü yok. Diğer texture/audio/script/shader/model bağımlılıkları salt okunur paylaşılır. Desteklenmeyen immutable→mutable bağlantısı varsa işlem durur.

Salt okunur native inceleme: 590 native dosya / 24.278.772 byte; ilk bağımlılık sınırında 12 ek FBX / 10.091.430 byte bulundu. Araç kendi tam closure listesini hesaplar; 1024 dosya / 1 GiB ve ek disk payı sınırı vardır. Bu sayılar üretilmiş asset kanıtı değildir.

Scene/prefab yerel fileID'leri korunarak GUID referansları değiştirilir. Native asset içindeki referanslar kopyadaki aynı fileID ve türle eşleştirilir; bulunamazsa işlem durur. Candidate kaydedilip yeniden açılır. Her GameObject'in local ID, parent, name, active durumu, local transform'u, component tip listesi ve mevcut persistent/loot/harvest ID'si kaynak snapshot ile birebir karşılaştırılır. Terrain/collider aynı izole veriye bakmalı; 513² yükseklik örneklerinin tümü kaynakla eşit olmalıdır. Transitive dependency kontrolü eski mutable kaynaklardan birine hâlâ ulaşırsa kabul edilmez.

NavMeshData da native bağımlılık olarak kopyalanır; bake yapılmaz. Terrain değişince G3/G4 bunu yeniden ele almalıdır. Kalite/gölge sistemi, hero/resource ağaçlar, spawn ve POI koordinatları korunur. Üç null harvest physicsBody G0 baseline olarak kalır; ağaç kesimi kabulü verilmez.

## İşlem kaydı ve hata

Her çalıştırmanın journal'ı `Docs/Implementation/World/WLD-S001/Evidence/G<utc>-<unique>.json`. Kaynak/kopya yolları ve hashleri, GUID eşlemeleri, hierarchy snapshot, createdPaths, status ve hata saklanır. Başarılı sonuç ayrıca `Docs/Implementation/World/WLD-S001/G2_Latest.json` olur. Receipt bir kaynak yedeği değildir; bu işlem kaynakları değiştirmez.

Hata/iptalde yalnız o çalıştırmanın açtığı aday kapatılıp kaynak tekrar açılır; yalnız createdPaths çıktıları geri alınır. Yeni klasörler sadece boşsa kaldırılır. Cleanup başarısızsa journal'da tutulur; owner'a ait mevcut stage veya başka dosyalar silinmez. Editor/process çökmesinde otomatik sonraki-açılış temizliği yoktur: journal ve kalıntılar incelenmelidir. Mevcut aday engeli bunların üzerine yazılmasını önler.

## Owner çalıştırması

1. Unity importu bitince yalnız kaydedilmiş **FirstTownMain_HDRP** açık olsun. Yeni değişiklik varsa körlemesine kaydedip receipt yenilemeyin; kaynak hash kontrolü durdurursa diff'i iletin.
   Son salt okunur kontrolde açık Main dirty=true, disk kaynak hashleri aynıydı. Araç bu durumda save/discard yapmaz; önce bellekteki değişikliği inceleyin. Bu flag'in nedeni doğrulanmadı.
2. **Last Signal → World Reconstruction → WLD-S001 → 3 Create isolated G2 candidate**.
3. Başarıda `WLD_S001_NaturalWorld_Stage` açık kalır. Görünümün Main ile aynı olması beklenir; henüz dağ/ikinci terrain oluşmaz. Console'da `WLD G2 candidate saved and structurally verified` mesajı beklenir.
4. Adayı değiştirmeden **Last Signal → World Reconstruction → WLD-S001 → 4 Validate saved G2 candidate**. Beklenen `WLD G2 SAVED_CANDIDATE_VERIFIED`; bounds ve slot mesajda yazılır.
5. `G2_Latest.json` ile bu iki Console sonucunu gönderin. Hata varsa ilk tam stack trace ve o çalıştırmanın Evidence journal'ı yeterli; oluşturmayı tekrar tekrar çalıştırmayın.

İsteğe bağlı terminal doğrulaması: `python3 Tools/WorldReconstruction/verify_foundation.py`. Oluşturmadan önce `candidateStage=NOT_RUN`; başarılı owner üretiminden sonra `RECEIPT_CHECKED_STATIC_ONLY` beklenir. Yeni hata yoksa tamamlanmış G1 preview tekrar istenmez.

## Bu teslimde doğrulananlar

23 kaynak hash kontrolü ve mevcut G1 kanıt eşleşmesi geçti. Verifier'ın yanlış plan hash'i, dirty source ve değiştirilmiş envanteri reddettiği geçici fixture ile kontrol edildi. Unity'de yeni authoring type derlenmiş olarak görüldü; authoring metodu çağrılmadı. Console incelemesinde üçüncü taraf `Assets/ThirdParty/Unity-Tree-Chopping-Simulation-main/CombinedResultVid1.wmv` için tanınmayan video formatı hatası görüldü; bu asset değiştirilmedi. Console'un tamamen temiz olduğu iddia edilmez.

Son kod revizyonunda `WorldAssetCopy.destinationMetaHash` alanının yüklü Editor assembly'sinde bulunması ve compiling=false ayrıca gözlendi. Böylece aday importer metadata değişiklikleri de receipt hash karşılaştırmasına dahildir.

İlk teslimde sahne oluşturma/roundtrip/rollback NOT_RUN idi; aşağıdaki owner çalıştırması bu durumu günceller. Play/EditMode/PlayMode, bake ve benchmark hâlâ **NOT_RUN**. G2 kabulü başarılı owner kanıtından sonra verilecek. Sonraki gate G3 tam örneklemeli terrain üretimi, seam ve grounding denetimidir.

## 24 Ekim — owner G2 hatası ve düzeltme

Çalıştırma `G20261024T110617351-9e3110a5`: **FAILED**. Asıl exception `Candidate retains a mutable source dependency`. Sonrasındaki Validate hatası, geri alma sonrasında aday bulunmamasından kaynaklanır. Başarısız journal orijinal haliyle Evidence altında korunuyor; Console metni `Evidence/G2_OwnerFailure_20261024.txt` olarak saklandı.

Salt okunur native dependency incelemesi kesin eksik kapsamı ortaya çıkardı: üç binary TerrainData assetinde 11 terrain layer/detail prefab bağlantısı `SerializedObject.GetIterator` içinde görünmüyordu. Eski remapper bu alanları hiç değiştirmiyordu. Kaynak typed getter'larıyla kapsam genişletildiğinde genel/native asset grubunda açıklanmayan mutable dependency sayısı0 oldu. Geri alınmış aday artık bulunmadığından, eski adayın tüm bağımlılık yolları sonradan ölçülemez; bu bir yeni üretim başarısı iddiası değildir.

`WorldStageIsolation.RemapTerrain` artık yalnız kopyadaki terrainLayers, detailPrototypes (prototype ve prototypeTexture) ve treePrototypes prefab referanslarını açıkça eşler. DetailPrototype/TreePrototype copy constructor'ları mevcut boyut/renk/yoğunluk/render ayarlarını korur. Height/splat/density üretimi yapılmaz. Kopya asset kaydedilir; mevcut tam bağımlılık kontrolü gevşetilmez. Sonraki olası hata, tam parent→source yollarını geri alma öncesi journal'a yazacak.

Console'daki Missing Variant/Nested Prefab mesajlarının stack'i `DeleteAsset` rollback satırına aittir. Ebeveyn prefab silinip bağımlı prefab henüz silinmeden araya giren importlar bunları üretmiştir. Cleanup artık try/finally ile StartAssetEditing/StopAssetEditing arasında, aynı dar createdPaths listesi üzerinde yapılır. Yeni retry'de bu davranışın owner doğrulaması beklenir.

Read-only doğrulama: 602/602 source asset ve source meta hashleri önceki journal'la aynı; 607 createdPaths girdisinden kalan dosya/klasör/meta0; başarılı G2_Latest.json yok. 23 G0 source hash eşleşmesi korunuyor. CS0618 uyarıları eski API kullanımıdır; G2'yi durduran exception değildir. Üçüncü taraf WMV formatı, Account API timeout ve kaynak Jacket mesh polygon uyarıları bu düzeltmenin kapsamında değiştirilmedi.

Owner sonraki adım: kayıtlı Main açıkken menü **3 Create isolated G2 candidate** yeniden çalıştırılır. Yalnız başarı mesajı gelirse **4 Validate saved G2 candidate** çalıştırılır. Başarısız olursa ilk tam exception ve yeni journal gönderilir; doğrulama menüsüne devam edilmez. Agent üretim veya Unity test komutunu otomatik çalıştırmadı.

Düzeltme sonrası statik verifier geçti (`FAILED_ROLLED_BACK_OWNER_RETRY_PENDING`, failures=[]). Geçici fixture ile hiç çalıştırılmamış durum, tamamen geri alınmış hata ve kalmış `.meta` dosyasının reddi kontrol edildi. Editor assembly değişen iki C# dosyasından daha yeni; son Console Error/Exception sorgusunda yalnız mevcut WMV format hatası vardı, C# compiler hatası görülmedi. Bunlar başarılı native kopyalama/authoring roundtrip kabulü değildir.
