# S013 import ve doğrulama standardı — teknik hazırlık

1. Kaynak arşiv/hash, yayıncı, edinim sürümü/lisans kaydı alınır. Vendor baytları korunur; türevler `Assets/LastSignal/Art/S013/` altında olur. Meta/GUID ile asset birlikte taşınır; toplu klasör düzenleme yok.
2. Ölçek metre. Root (1,1,1); negatif/zero child scale reddedilir. Floor pivot render LOD0 temasıdır: billboard bounds otomatik floor pivot yapılamaz. Kapı hinge pivotu ayrı tanımlanır. Kaynak E'nin child scale değerleri doğrudan üretim prefaba taşınmaz.
3. UV0/normal/tangent kanalı otomatik denetlenir; overlap, texel density, seam ve ters normal görsel olarak ayrıca kontrol edilir. 256 px/m çevre, 512 px/m hero başlangıç hipotezi.
4. Albedo sRGB, normal map doğru importer, roughness/metal mask linear. Opaque/cutout ayrımı ve cutoff kenarı sahnede doğrulanır. Değerlendirme B dönüşümü metal0/smoothness.12 kullanır; bu final malzeme standardı değildir.
5. Foliage cutout, çift taraf ihtiyacı ve gölge maliyeti ayrı ölçülür. LOD0/1/billboard görünümü üç mesafede karşılaştırılır. Sadece triangle düşüşü kalite kabulü değildir.
6. Trunk/blocking furniture basit collider; yaprak/dekor için collider zorunlu değil. Detailed MeshCollider otomatik eklenmez. Loot/radio/shelter kimlikleri görsel prefaba kopyalanmaz.
7. `S013PrefabValidation.Inspect(root, requiresCollider, licenseApproved)` eksik mesh/material/script, root/negative scale, shader/URP tag, collider ve açık license approval bayrağını denetler. Aynı component türündeki stableId/persistentId tekrarlarını yakalar. Global save identity ve broken dependency doğrulamasının tamamı değildir; mevcut entegrasyon testleri gerekir.
8. Asset audit gerçek mesh index sayısını, UV/normal/tangent kanallarını, prefab shader/LOD/collider/missing slotlarını, texture boyut/import/memory ölçülerini JSON'a yazar. Runtime texture memory Editor bağlamıdır; standalone VRAM ölçümü değildir.

## Tekrar üretim

Mevcut scene'i koru. Yeni laboratuvar için `Last Signal/S013/Create isolated asset comparison`; hedef varsa üzerine yazmayı reddeder. Oluşturucu kaynak package GUID'lerine bağımlıdır; önce extraction-manifest'teki importlar bulunmalı. Var olan comparison'ı incelemek için bu generator tekrar çalıştırılmaz.

Capture: açık ve kaydedilmiş sahnelerle `LastSignal.Art.Editor.S013AssetEvaluation.Capture(yeniEvidenceKlasoru)`. Tek sahne render için scene setup geçici değiştirilir ve finally ile geri yüklenir; dirty sahne varsa reddeder. Kamera/ambient eşitlenir, diğer bayler kapatılır. Her render yeni dizine, aynı dosya varsa reddedilir.

Odaklı test assembly `LastSignal.Art.EditorTests`; açık Editor TestRunner API ile çalıştırıldı. Mevcut `Tools/s012-verify.py` yalnız eski assembly adını seçtiği için bu yeni testleri kapsamaz. Sonuç yazımı mevcut CombatRegressionRunner callback'ini kullanır. S013 kapanışında eski EditMode/PlayMode assembly'leri ayrıca tam çalıştırılmalı.

## Bilinen sınırlamalar

AuditAssets ilk sürümde scene subasset yüklemeye çalışıp ReadObjectThreaded hataları üretti. Dosya türü filtrelenerek düzeltildi; r2 audit/capture sorunsuz tamamlandı. r1 audit korunur; esas r2 JSON'dur. r1 karanlık ambient/kesik ağaç, r2 demo geometrisi, r3 billboard floor hizası sorunları nedeniyle final görsel referans r4'tür. Hiçbir ara görüntü silinmedi.
