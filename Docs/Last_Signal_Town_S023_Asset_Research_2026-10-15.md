# LAST SIGNAL — S023 Pivot / Seamless Town Asset Research & Production Specification

**Tarih:** 15 Ekim 2026  
**Durum:** RESEARCH COMPLETE / IMPLEMENTATION NOT VERIFIED  
**Hedef:** Portalsız ve fiziksel sürekliliği olan küçük kasaba: 500 × 500 m oynanabilir çekirdek, 1 × 1 km uzun dönem üretim çerçevesi  
**Teknoloji:** Unity 6000.5.0f1, URP 17.x, önce tek oyunculu  

## 1. Kaynakların otoritesi ve erişim sınırı

GitHub bağlantısıyla `coskunburak/Last-Signal` özel repository'sine erişim doğrulandı. `main` HEAD `3f81ae99a658750d5834cba27bc673ac03b56ef6` (13 Ekim 2026). Bu HEAD S023'ün 15 Ekim yerel authoring ve test dosyalarını içermiyor. `.gitignore` içerisinde `/Assets/ThirdParty/` ile `/Docs/Implementation/**/Evidence/` hariç tutulmuş. Bu nedenle GitHub'daki geçmiş kaynak envanteri, son üç yerel environment paketin kendileri veya güncel yerel S023 kanıtı yerine geçmez.

İncelenen başlıca kayıtlar: `Docs/Architecture/ThirdPartyLayout.md`, `ThirdPartyLayoutManifest.json`, `Docs/Implementation/S013/S013_ASSET_REGISTRY.md`, `S013_ART_BIBLE.md`, `S013_ASSET_COMPARISON.md`, `Docs/Last_Signal_Docs_v0.2/sources/13_World_Streaming_POI.md`, `19_Art_Audio_Asset_Pipeline.md`, `20_Performance_Build_Operations.md`, `Docs/Implementation/VEH-001/STREAMING-DECISION.md`, `Docs/Last_Signal_Production_Plan_v1.0_TR/sprints/S023.md`, ilgili runtime/world cell kaynakları.

**Kritik politika:** Son eklenen üç yerel paketi görmeden eksik diye yeniden indirtme; önce yerel envanter raporunu al. Eski dokümanlarda private/public ve klasör yolları güncelliğini yitirmiş olabilir.

## 2. Doğrulanabilir mevcut paket aileleri ve uygulanabilirlik

| Aile / kayıt | Mevcut konum (tasarım kaydı) | Kasabada kullanım | Durum / risk |
|---|---|---|---|
| Fantasy Landscape (Pxltiger) | `ThirdParty/Environments/FantasyEnvironments` | Ağaç, çalı, kaya, temel terrain | **ONAYLANMIŞ ANA DOĞA AİLESİ**; B Pine/Birch/Bush/Rock onaylı, production türevleri mevcut; lisans edinim kanıtını denetle |
| Silver Cats Hand Painted Nature Kit LITE | `ThirdParty/Environments/Silver_Cats/...` | Alt örtü için dar kapsam | Ana doğa ailesi olarak onaylanmamış; stil karıştırma riski |
| Holotna Mountain | `ThirdParty/Environments/Mountain (...)` | Arka plan/tepelik arazi, taş | Yerel import kaydı 202 varlık; doğrudan production onayı ve ticari lisans kanıtı yok |
| Flawless Normals Abandoned Barn | `ThirdParty/Environments/...barn...` | Terk edilmiş çiftlik POI detayı | Çatısı açık; sığınak olarak RED. Kaynak lisansı doğrulanmalı |
| Modular Wooden Storage Cabinet | `ThirdParty/Environments/...` | İç mobilya | Bazı parça geometri ve texture bağlantısı yetersiz; production ana kit olarak kullanma |
| TT3D/LowPolySci-FiStarterPack | `ThirdParty/Environments/TT3D` | En fazla editor graybox | **REJECT** ana kasaba görseli için (sci-fi/low-poly uyuşmazlık) |
| MedicinePack / Polygon-Lite Survival / FoodKitchen | `ThirdParty/Loot` | Klinik/market loot prop adayları | Loot visual, icon ve inventory referanslarını koru; Polygon-Lite sadece stil geçerse kullanılmalı |
| MotionCore / Pickup / Stylized Vehicles | `ThirdParty/Vehicles` | Sürüş ve terk edilmiş araçlar | Sadece mevcut sürüş kontratını kullan; görsel tür/LOD ayrı kabul |
| NOX, FreeWeaponSFX, Horror Audio, Kenney | `ThirdParty/Audio` | Ambience, şehir/araç/dış ses | Ses otoritesini veya lisansı otomatik onaylama |
| Zombie/character/weapons/animations | ilgili kategoriler | Mevcut oynanış | S013Cabin'de çözülmüş karakter/tutuş problemlerini tekrar açma |

**Not:** Bunlar dosyaların güncel local checksum ile yeniden tarandığı anlamına gelmez. Envanter belgesine dayalı tespittir.

## 3. Harita yapısı — portal kaldırma kararı

**Öneri: 1 km² kapalı sınırlarla bir kerede doldurulmasın.** 500×500 m (0,25 km²) kesintisiz, erişilebilir ve içeriği yoğun ilk dilim; 1×1 km sonraki genişleme sınırı.

- NW: Forest/cabins + düşük risk yürüyüş yolu.
- NE: Residential + market / ana cadde.
- E: Industrial garage, warehouse, container yard.
- S: Safehouse, tarla/farm, yakıt istasyonu, dönüş rotası.
- Merkez: Clinic, checkpoint, küçük ticari yapı, görsel landmark.
- Bağlantı: gerçek sürülebilir yollar, en az bir araç loop'u, yaya kestirmesi, savunulabilir geri çekilme hattı.
- Plan türü: tarihî/gelecekteki konum etiketleri değil; ilk oyun haritasına ait konsept tasarımıdır.

Mevcut `CellCoordinate.Size = 64` ve `WorldCellManager.TryEnter / ReturnToResident` teleport/portal davranışı gerçek seamless streaming değildir. Mevcut S013Cabin ve save dosyalarını koruyarak yeni Town scene üzerinde fiziksel süreklilik geliştir. Portallar ana oyunun production akışından kaldırılacak, validation fixtures bozulmayacak. İlk 500m dünyayı tek resident scene içinde çalıştırmak mümkündür; 1km büyümede gerekiyorsa fiziksel world streaming içerik sahipliği tasarlanmalıdır. Portal yerine teleport fonksiyonunu araçta çağırma.

## 4. Güncel ücretsiz adaylar — gerçek ürün linkleri

Aşağıdakiler Unity Store ürün sayfasında FREE görülen veya Poly Haven CC0 olarak listelenen kaynaklardır. **Free / CC0, direkt production-accepted demek değildir.** Kullanımdan önce kaynak hesabında edinim/lisans ve Unity import kontrolü yapılmalıdır. Büyük paketleri üretim projesine doğrudan full-import yapma.

| Öncelik | Ürün | Kaynak URL | Fiyat / lisans | İçerik kullanım kararı |
|---|---|---|---|---|
| P0 | Countryside gas station | https://assetstore.unity.com/packages/3d/environments/countryside-gas-station-132485 | FREE / Unity Standard EULA | Benzinlik hero POI; 484.1 MB, eski Unity sürümü ve gerçekçi PBR; ayrı benchmark, color/roughness adaptation |
| P0 | FREE Industrial building/Garage | https://assetstore.unity.com/packages/3d/environments/industrial/free-industrial-building-garage-123146 | FREE / Unity Standard EULA | Sanayi garaj kabuğu/çit; 9 MB, ~1000 triangle bina; URP normalizasyonu ve iç oynanabilirliği denetle |
| P1 | Free Shipping Containers | https://assetstore.unity.com/packages/3d/environments/industrial/free-shipping-containers-18315 | FREE / Unity Standard EULA | Endüstriyel saha ve cover; 21.2 MB, 2016 eski shader/ölçek |
| P1 | Industrial Props Kit | https://assetstore.unity.com/packages/3d/props/industrial/industrial-props-kit-84745 | FREE / Unity Standard EULA | Palet, koni, endüstriyel sokak ayrıntısı; 4.8 MB, shader/material QA |
| P1 | Garage Props Set | https://assetstore.unity.com/packages/3d/props/industrial/garage-props-set-132664 | FREE / Unity Standard EULA | Garaj içi varil/mobilya; 112.4 MB, 4K kaynak texture'ları zorunlu değil |
| P1 | Free Wood Door Pack | https://assetstore.unity.com/packages/3d/props/interior/free-wood-door-pack-280509 | FREE / Unity Standard EULA | İnteraktif kapı görünümü; 99 MB, mevcut DoorInteractable domainini koru |
| P2 | Abandoned Buildings | https://assetstore.unity.com/packages/3d/environments/abandoned-buildings-62875 | FREE / Unity Standard EULA | Terk edilmiş kırsal küçük yapı, ruin; sadece 2 ana bina; 29.2 MB; ana kasaba bina sistemi değil |
| P2 | Modular European House | https://assetstore.unity.com/packages/3d/environments/urban/modular-european-house-294289 | FREE / Unity Standard EULA | Merkezde sınırlı bina/landmark adayı; URP listeleniyor ancak Avrupa cephe biçimi ve renklendirme ilk kırsal kasaba diline uyum testine bağlı |
| P2 | Farm Environment | https://assetstore.unity.com/packages/3d/environments/farm-environment-313619 | FREE / Unity Standard EULA | Çiftlik keşif adayı; ~1 GB, tamamını import etme, görsel uyum/LOD önce değerlendir |
| P2 | Workshop Tools – Free Low Poly Asset Pack | https://assetstore.unity.com/packages/3d/props/industrial/workshop-tools-free-low-poly-asset-pack-326488 | FREE / Unity Standard EULA | Küçük garaj el aletleri; yalnız yakın görünümde kabul geçerse |
| P1 material | Poly Haven Asphalt 02 | https://polyhaven.com/a/asphalt_02 | Free CC0 | Eski/yıpranmış yol için 1K/2K seçilmiş map; mesh yolu Blender/Unity ile authored |
| P1 material | Poly Haven Asphalt 01 | https://polyhaven.com/a/asphalt_01 | Free CC0 | Alternatif ana yol malzemesi ve aşınma varyasyonu |
| P1 material | Poly Haven Factory Brick | https://polyhaven.com/a/factory_brick | Free CC0 | Endüstri duvarı; stilize gerçekçi tona uyarlama |
| P1 material | Poly Haven Corrugated Iron | https://polyhaven.com/a/corrugated_iron | Free CC0 | Çatı, çit ve depo panelleri |
| P1 material | Poly Haven Damaged Concrete Floor 03 | https://polyhaven.com/a/damaged_concrete_floor_03 | Free CC0 | Depo/fabrika zeminleri |
| P1 material | Poly Haven Wooden Planks | https://polyhaven.com/a/wooden_planks | Free CC0 | Sığınak/kırsal çit/ahşap onarım; existing TimberGrain ile karşılaştır |
| P2 graybox | Kenney City Kit Roads | https://kenney.nl/assets/city-kit-roads | Free CC0 | 90 model, collision/layout prototipi ve referans ölçü için; Last Signal final render olarak **REJECT** (low-poly) |

**Özellikle önermediklerim:** Synty POLYGON Town, Apocalipse Survival Pack, Abandoned Facade ve büyük P3D Gas Station paketleri güncel aramada ücretli; ücretsiz shortlist'e koyma. HDRP-only Unity Warehouse de URP üretim hedefiyle uyuşmuyor. Kenney low-poly city/industrial görseli, doğru telafiler yapılmadan final sanat diliyle uyuşmuyor.

## 5. Bölge bazlı gerçek asset ihtiyaç matrisi

| Bölge | Yerleşim / çevre kiti | İç mekan / prop | Önce mevcut kaynak | Dış kaynak ihtiyacı |
|---|---|---|---|---|
| Safehouse | S013 rural cabin + wood modules | Kapı, depo, raf, crafting | S013, S019, ThirdParty Loot | Yeni paket gerekmez; malzeme uyumu ve oyuncu loop'u |
| Town center | 2–3 katlı modüler dükkan cephesi, kısa ana cadde, kaldırım, park yeri | Market rafı, kasa, tabela, vitrin, mobilya | S019 RuralStore, existing food/kitchen | **YÜKSEK GAP**: oyunlaştırılmış dükkan cephe kitini proje-owned üret veya yerel 3 paketten çıkar |
| Residential | En az 3 farklı ev silueti, çatı, veranda, bahçe, garaj | Yatak, dolap, mutfak, loot noktası | S013 cabin kit, existing food/kitchen, furniture | **ORTA-YÜKSEK GAP**: mevcut cabin dışı ev varyasyonu, tekrar hissi |
| Gas station | Benzinlik ve kapalı servis dükkânı | Pompa, yedek parça, iç raf, fuel | Existing loot + vehicle | **P0 external hero candidate**: Countryside gas station |
| Industrial yard | Garaj, çit, depolama alanı | Konteyner, palet, varil, tamir aracı | S023 eski pilot geometry varsa uyarlanır | **P0/P1**: Garage, Shipping Containers, Industrial Props |
| Clinic | Küçük bina + okunur klinik tabelası | MedicinePack, yatak, dolap, müdahale masası | MedicinePack, S019/owned walls | Klinik yerleşim ve görsel signage authored; satın alma gerekmeyebilir |
| Police/checkpoint | Barrier, sahaya uygun kiosk, security gate | Koniler, tabelalar, tel çit | Existing props ve owned modular | Çit/bariyer modüler üretim; paket opsiyonel |
| School / motel | Uzaktan okunur hero silhouette | Az sayıda seçili oda ve loot anchor | Existing interior props | Faz-2; başlangıç 500m core için zorunlu değil |
| Farm/forest | Organik terrain, çalı, ağaç, kaya | Çit, barn harabesi, farming loot | Fantasy Landscape approved primary nature; Barn candidate | Yeni nature kit **GEREKMİYOR**; barn lisansı/sanat review |
| Roads / drainage | Road mesh, T junction, kavşak, kaldırım, yol kenarı, dere geçidi | Yol çizgisi, çukur/decal, levha, korkuluk | Existing terrain/mat, project-authored mesh | **P0 Teknik GAP**: fiziken sürülebilir yol üretimi + asfalt CC0 materyal |

## 6. Stil/performans kabul standardı

1. `Assets/ThirdParty` vendor kaynaklarına dokunma. Normalizasyon `Assets/LastSignal/Art/Town` altında olmalı.
2. Aynı benchmark ışığı/FOV/gündüz-gece-yağmur ile S013 ana ağaç, ev, araç, zombi, silah, yeni benzinlik ve garajı yan yana değerlendir.
3. PBR reality ile stylized primary kit'i albedo, roughness ve contrast üzerinden eşleştir; shader/texture slap-on ile PASS deme.
4. 1 Unity unit=1 m, kapı collision/crouch/AI agent ve araç genişliği gerçek ölçülmeli; hangarı sırf kare cell'e sığsın diye bölme.
5. Doku import: 1K–2K başlangıç hedefi, material atlasing/trim sheets, normal+mask maps, sensible mip/anisotropy, alpha-overdraw LOD.
6. Terrain/ağaç yoğunluğu GPU ve belleğe göre; low-quality sırasında threat, collision, loot aynı kalmalı.
7. Static batching, occlusion, GPU instancing, LOD vs gerçek profiler ile kararlaştır; görsel aldatma yok.
8. Scene gizli teleport barındırmamalı; araçla sığınaktan sanayiye fiziksel sürüş ve geri dönüş zorunlu.
9. WorldStateStore, loot tombstone, door state, zombi persistence, session generation ve save topology değişirse migration-fixture gerekli.
10. Lisans: Unity Standard EULA proje içinde embedding için uygundur ama kaynak dosyalarının açık repoda serbestçe dağıtımı varsayılmaz. Poly Haven CC0 kullanımı daha esnektir. Tüm dosyalar için kaynak URL, lisans ekran görüntüsü/entitlement, versiyon, vendor/original hash, local folder tut.

## 7. S023-Pivot günlük kart değerlendirme ve gerçek durum

Canonical S023 D221–D230, portal üzerinden ikinci cell pilotuna odaklanmıştı. Yeni kullanıcı kararı portalın ana oyun deneyiminde kullanılmaması ve gerçek fiziksel kasaba olmasıdır. Dolayısıyla bu **scope change / architecture pivot** olarak ayrı karar kaydında yönetilmeli; canonical test evidence silinmemeli.

| Kart | Portalsız kasaba için güncellenen hedef | Şimdiki kanıt / sonuç |
|---|---|---|
| D221 | 500m çekirdek + 1km envelope, POI ve resource budget | Tasarım önerisi hazır; oyuncu onayı/yerel audit bekliyor |
| D222 | Araç/yaya kesintisiz topoloji ve rota landmarkları | Level authoring henüz yapılmadı |
| D223 | Resident scene + kontrollü streaming ve save topology | Tasarım/riske bakıldı; Unity implementasyonu NOT_RUN |
| D224 | Modüler ana ticari POI | Eski S019/S023 prefabları yeniden kullanım adayı; yeni town entegrasyonu NOT_RUN |
| D225 | Garaj/sanayi+konut/market ikinci POI | Asset shortlist hazır; scene production NOT_RUN |
| D226 | Art Bible benchmark, gece/yağmur visual QA | Sanat politika hazır; Unity benchmark NOT_RUN |
| D227 | Zombie/loot mevcut authoritative mekaniklerle | Dağıtım tasarımı; yeni kasabada balance/runtime NOT_RUN |
| D228 | Save/load ve loot/door state dahil fiziki gidip gelme | Eski portal roundtrip PASS, **seamless roundtrip NOT_RUN** |
| D229 | 1080p CPU/GPU/p95/VRAM ve araçla traversal profiling | NOT_MEASURED, yalnız kullanıcı çalıştırır |
| D230 | Saat/farklı POI içerik üretim hızı tahmini | Gerçek süre yok; NOT_MEASURED |

**Gerçek bitiş koşulları:** walking/vehicle route portalsız, 8–12 oynanabilir bina hedefli ilk dikey dilim, özellikle 1 market+1 ev+1 klinik+1 benzinlik+1 tamirhane hero POI, loot/survival loop, bütün stateler fiziksel dünyada kalıcı; karakter/silah/araç mevcut düzeltmeler korunmalı. İlk sprintte 1km² tüm bölgelerin production-quality tamamlanması 40–60h tahmininden otomatik çıkmaz.

## 8. Kullanıcının Mac'inde gereken minimum yerel ThirdParty kanıtı

**Lütfen üç yeni paketi yeniden indirmeyin; önce aşağıdaki salt-okunur kayıt:**

```bash
cd '/Users/burakcoskun/Last Signal'
find Assets/ThirdParty -mindepth 1 -maxdepth 3 -type d \
  \( -iname '*environment*' -o -iname '*town*' -o -iname '*city*' -o -iname '*house*' -o -iname '*building*' -o -iname '*industrial*' -o -iname '*nature*' -o -iname '*road*' -o -iname '*farm*' \) -print
python3 Tools/thirdparty_audit.py
```

Bu komut test/build çalıştırmaz; kaynak hash/GUID ve klasör envanterini gözlemlemeye yarar. Kod değişikliği veya import gerektiğinde uygulamayı Codex'e, testleri kullanıcıya bırak.

## 9. Net satın alma/indirme kararı

**Şimdi minimum ihtiyaç:** Mevcut üç yeni paketin adını doğrula; `Countryside gas station`, `FREE Industrial Building/Garage`, `Free Shipping Containers`, `Industrial Props Kit` ve `Poly Haven Asphalt 02` adaylarını aynı art benchmark'a sok. Paketlerin tamamını aynı anda ana Unity projesine import etme. Özellikle Town Center/Residential art ana kit boşluğunu mevcut yerel üç paket kapatabiliyorsa yeni bina paketi satın alma/indirme.

**Bu oturumda S023 Unity scene, prefab, source ve tests dosyaları değiştirilmedi, Unity çalıştırılmadı.** Bu araştırma ve üretim spesifikasyonu tamamlandı; gerçek sprint production ve QA yürütmesi local checkout olmadan bitirilemez.
