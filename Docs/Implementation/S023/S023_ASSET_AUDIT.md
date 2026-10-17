# S023 asset ve ownership kaydı

Status: kaynak denetimi IMPLEMENTED; native dependency çıktısı NOT_RUN; görsel kabul NOT_RUN. Yeni harici paket gerekmiyor. Hiçbir asset indirilmedi, yeni lisans hakkı varsayılmadı, ThirdParty dosyası düzenlenmedi.

| Kaynak | Kullanım / owner | Yeni kopya |
|---|---|---|
| S013Cabin.unity | Ayrı S023 scene başlangıcı; mevcut user changes dahil disk snapshot | Yeni scene; source overwrite yok |
| S019RuralStore.prefab | PoiAuthoring.CreateNew + PoiAuthoringLayout sözleşmesi; yeni instance'ın geometri çocukları değiştirilir | İki S023 cell prefab; orijinal template korunur |
| S013 Production Wall_2m/Window_2m | Checkpoint parçaları, unit scale | Mesh/material shared ref |
| S013 Metal/Stone/Iron/RoofMetal/GroundPath/Wood/PaintedTimber | Yeni metal depo, apron, yığın, gantri, yönlendirme | Materyal veya texture kopyası yok |
| Unity primitive cube / built-in LegacyRuntime font | Basit kabuk/prop render ve çevresel tabela | Shared built-in mesh/font; özel shader yok |
| Interactions/Door.prefab | `s023.depot.service-door`; mevcut interaction, yeni instance material override ve nav obstacle | Proje-owned nested instance |
| LS_Zombie_Lurker / LS_Zombie_Runner | Tek authored encounter/cell; mevcut S022 behavior ve health/anatomy snapshot | Prefab referansı; AI source kopyası yok |
| ItemCatalog, material.scrap, material.cloth, fuel.generator | Üç yeni sınırlı LootProfile | ItemCatalog/item/pickup prefab kopyası yok |
| WorldPopulationManager / production audio/time | Kaynak session owner'ları | Yeni global manager/audio loop yok |

Yeni prefab root'u world origin + unit scale; cell geometry global cell offset altında. Kapı/loot/encounter kimlikleri `s023.*`, güncel koordinatlar korunur. Door hareketli collider kendi nav obstacle'ına sahip; sabit collider'lar WorldCellContent.requiredSolids'te. Root disabled prefab, runtime manager tarafından aktive edilir. Native NavMeshSurface bake için vardır fakat prefabda disabled; navigation install/remove tek WorldCellContent sahibindedir. NavMesh yeni mesh/geometri dependency'dir; native bake çalıştırılmadı.

`authoring.json.assets` her yeni cell prefab için recursive AssetDatabase dependency listesi üretir. Aynı texture path/GUID'nin iki prefabdan referans edilmesi beklenen paylaşımdır; iki ayrı texture asset oluşturulmaz. Materyal instance override'ı texture duplication değildir. Generated prefablarda yeni model/texture importer yok; mevcut importer'lar değiştirilmez.

LOD: büyük kabuklar yakın pilot mesafesinde görünür kalır, yeni foliage yok; sade prop geometri bütçesiyle başlanır. Ayrı LOD zinciri/mesh birleştirme/occlusion optimizasyonu eklenmedi. Culling, materyal slot ve renderer sayısı kullanıcı capture'ıyla değerlendirilecek; ölçülmeyen tasarruf iddiası yok.

Lisans: S013_ASSET_REGISTRY içindeki B/Fantasy Landscape ailesi 2026-10-04 kullanıcı sanat seçimiyle mevcut üretimde kullanılıyor; bu edinim/public redistribution kanıtı değildir. S004 zombie, hands/MRPoly ve kopyalanan resident içeriklerinin mevcut entitlement notları devam eder. S023 yeni lisans açığı yaratacak paket edinmedi, mevcut yayın lisanslarını kapatmış sayılmaz. Bu durum yerel pilot authoring'i engellemez; yayın onayı verilmez.

Rollback: yalnız yeni S023 çıktıları ve kodu task diff üzerinden gözden geçirerek geri alınabilir. Generated dosyalar silinip yeniden üretilirse GUID/save identity ilişkisi ayrıca incelenmelidir; araç otomatik rebuild/rekey yapmaz. Mevcut save'leri silerek rollback yapılmaz.
