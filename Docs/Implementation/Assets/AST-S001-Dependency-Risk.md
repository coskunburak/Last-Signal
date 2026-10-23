# AST-S001 — Bağımlılıklar ve koruma

Statik kullanım dağılımı: {'DYNAMIC_USAGE_POSSIBLE': 24, 'ACTIVE_PRODUCTION': 34, 'ACTIVE_DEVELOPMENT': 9, 'REFERENCED_LEGACY': 1, 'UNREFERENCED_CANDIDATE': 4}. Sahnelerin recursive GUID kapanışı, materyal/controller/timeline/vfx/terrainlayer/asset/meta referansları; Resources, ProjectSettings ve kod yolları tarandı. Binary serialization, GUID olmayan dinamik adlar ve reflection kullanımını tam çözmez. Tüm proje prefabları doğrudan tüketici listesine dahil; bu doğrudan listenin yokluğu bağımsız subasset kullanımını dışlamaz. Addressables paketi manifestte bulunmadı; kodda Addressables/AssetBundle/Resources taraması yapıldı.

**Build listesinde yalnız S001Acceptance ve CombatAcceptance var.** FirstTownCore ve ForestPrototype120 ayrıca kritik kök olarak analiz edildi. Build listesine bakarak orman/town paketlerini kullanılmıyor saymayın.

| Kritik sahne | GUID ile çözülen kapanış (root dahil) | ThirdParty asset yolları |
| --- | --- | --- |
| Assets/LastSignal/Scenes/Production/FirstTownCore.unity | 1093 | 342 |
| Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity | 91 | 4 |

**Taşıma engelleri:**

- Kenney UI: `Assets/LastSignal/Resources/InputGlyphCatalog.asset` doğrudan referanslıyor. İlk küçük aday elendi. Dry-run bu mevcut bağımlılıkta hata vermeli; Move komutu denenmemeli.
- Quaternius animasyonları: `DieselCharacterAuthoring.cs` sabit yolları ve rollback manifesti. Yeni yerlerine taşımak ayrı araç/path güncellemesi gerektirir.
- Fir/fern/ground: `ForestPrototypeAssets.cs` 18–20. satırlar eski kök yollarını kullanıyordu. Devam aşamasında yalnız bu üç sabit ThirdParty kaynaklarına düzeltildi; değişiklik öncesi dosya checkpointte saklandı. Var olan scene GUID referansları ve prefablar değiştirilmedi. Yeni kaynak yolları artık taşıma sırasında korunması gereken açık authoring bağımlılıklarıdır.
- RainMaker: Editor ve nested Standard Assets. MotionCore: Editor/asmdef. Free Blood VFX ve MRPoly: Resources. Standart klasör semantiği korunmalı.
- `MaterialRollbackManifest.json` source yollarını saklıyor. Geçmiş kayıt gibi görünse de rollback aracı bunu kullanabileceğinden aktif yol bağımlılığı olarak korunuyor.
- Kod namespace/ShaderGraph/include işaretleri `pipeline_indicators` ve `material_evidence`; yol satırları `Evidence/20261023T114820429441Z/path-loading-risks.json` içinde. UNKNOWN shader'lar unsupported diye kesinleştirilmedi.

## Checkpoint

`/Users/burakcoskun/LastSignal-Recovery/HDRPProduction-20261021-181319/project` gerçek dosyaları güncel SHA-256 manifestine karşı tekrar okundu: **20.666 aynı**, **92 eksik**, **37 farklı**. Eski VERIFIED etiketi güncel tam koruma sayılmadı. Paket bazında coverage katalogda, ayrıntı `Evidence/20261023T114820429441Z/checkpoint-comparison.json` içinde. Bu aynı diskte kurtarma checkpointidir; disk arızası yedeği değildir. `.p02-historical-backup` eski sprint kanıtlarından oluşur; güncel vendor yedeği yerine kullanılamaz. Mevcut zip dosyalarının güncelliği doğrulanmadı.

Yeni toplu kopya yapılmadı. Editor mover, izinli bir küçük batch uygulanacak olursa yalnız o paketi meta dahil benzersiz Evidence dizinine kopyalayıp her SHA-256'yi doğrular; sonra MoveAsset kullanır. Güncel kaynak kapsamı ve bağımlılık engelleri çözülmeden fiziksel taşıma kapalıdır.
