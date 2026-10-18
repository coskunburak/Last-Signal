# D011 — Definition, state ve uyumluluk kontratları

BLD-P00-DEC-011 · PROPOSED · Belge IMPLEMENTED / TEST_NOT_RUN. Bütün aşağıdaki yeni tipler şematik sözleşmedir; `.cs`, ScriptableObject, prefab veya migration uygulanmadı.
Girdi: D011, [mevcut API](CURRENT_CODE_BOUNDARIES.md), [sahiplik](BLD_S002_DOMAIN_OWNERSHIP.md), LS-DOC-07/14/17. Sahip Burak/teknik geliştirici; phase P01–P03 ve P07. Kart bağlantısı [handoff](BLD_S002_HANDOFF.md).

## Ortak alanlar ve invariantlar

DefinitionId ve instanceId farklıdır. ID: boş olmayan, case-sensitive ordinal canonical string, en çok 128 karakter; rename bir alias migration'dır. Definition revision pozitif integer; mutable state revision monoton int64, overflow ret; catalogSchemaVersion pozitif integer. ID reuse ve duplicate catalog kayıtları ret. Unity GUID yalnız asset resolution manifest'indedir. Save, Unity object reference veya sahne instance ID'si içermez.

`CatalogManifest`: schemaVersion, catalogRevision, contentHash, definitions[(kind,id,revision,sourceHash)], assets[(assetKey,unityGuid,sourceHash,licenseRecord)]. ItemDefinition mevcut StableItemId ile eşleşir; mevcut sınıfta revision bulunmadığından item sourceRevision bu manifest'te tutulur. Catalog revision, container revision, policyRevision ve save generation birbirinin yerine kullanılamaz. Catalog hash tutarsızsa yeni işlem ret; restore ise compatibility/migration kapısına gider.

Miktarlar pozitif integer base unit (adet, ml veya charge); açık unit olmadan dönüşüm yok. Duplicate ingredient satırları normalization'da checked arithmetic ile birleştirilir. Negatif/0 miktar, NaN/Infinity, overflow, unknown enum, duplicate ID, eksik kritik ref, döngüsel recipe/snap graph reddedilir. Tanım boyutları/array limitleri catalog schema'da bounded olur; ilk öneri ingredient/output en fazla 16, sockets en fazla 32; onay/ölçümle değişebilir, sınırsız deserialize yok.

## ResourceNodeDefinition / ResourceNodeState (PROPOSED)

| Alan | Tip / kural | Sahip / anlam |
|---|---|---|
| definitionId, revision, sourceHash | Stable ID + positive revision + content fingerprint | Catalog |
| toolClass, minimumToolTier | Enum/ref, bilinmeyen ret | Gathering uygunluk; tool kullanımı node yield'den ayrı |
| maxHealth, yield[] | Finite >0; itemId/itemRevision/unit/min/max; bounded deterministik seed | Bir generation'ın maksimum toplam yield bütçesi |
| visualRefs | standing/stump/fallen/impact asset keys; runtime script ref yok | Presentation, missing cosmetic fallback açık |
| respawnPolicy | None başlangıç; future cooldown/version | BLD-S020 kararına kadar reload/uzaklaşma respawn üretmez |
| nodeId, worldId/regionId/cellId | Authored stable ID ve adres | Gathering node state authority |
| health, remainingYield, depletion, generation, stateRevision | Bounded, generation başlangıç 0; tükenen health/yield tutarlı | Restore'da definition default'u üzerine delta |
| yieldSequence, lastProcessed, receiptRefs | Monoton, canonical world seconds, dedupe references | Tekrar hit veya reload yield çoğaltmaz |

Her accepted hit, node revision'a bağlıdır. Yield'in commit noktası node delta + world pickup record + harvest receipt aynı generation içinde kalıcı olduğunda oluşur. Pickup ID `nodeId/generation/yieldSequence/outputIndex` kimliğinden deterministik veya persist edilmiş unique ID ile atanır; retry yeni ID yaratamaz. Inventory'ye doğrudan ödül seçilirse world pickup aynı anda oluşturulmaz. US-02/03 için varsayım world pickup. Dünya item'ı sonradan toplandığında sahiplik Inventory'ye geçer. Resource node görüntüsü dekoratif forest instance indeksine bağlanamaz; stabil authoring map gerekir.

## RecipeDefinition / CraftJobState (PROPOSED)

| Alan | Tip / invariant |
|---|---|
| recipeId, revision, catalogSchemaVersion | Stable versionlı tanım |
| ingredients[] | itemId + itemRevision + quantity + unit + allowedQuality; quest/favorite otomatik seçim dışı |
| toolUsage[] | toolClass/ref, required=true/false, consumeMode=None/Charge/Condition; miktar ve boundary açık |
| stationRequirement, powerRequirement | Mevcut Workbench veya versionlı station capability; ücretsiz tüm utility yok |
| duration, timeBasis | Finite >0; WorldSeconds veya SimulationSeconds; legacy adapter WorldSeconds |
| outputs[], qualityPolicy | Açık itemId/revision/miktar; MVP fixed quality; rastgele üstün kalite yok |
| selectionPolicy | Explicit seçilmiş source container; uygun en düşük quality → itemId → slotIndex deterministik; belirsiz seçim ret |
| jobId, recipeId/revision, stateRevision | Stable job; pinned definition; enum Running/CompletedWaitingOutput/CancelledWaitingRefund |
| escrowEntries[], pendingOutputs[], elapsed, lastProcessed | Her girişte sourceId ve kaynak payload; miktar tek owner'da; completion bir kez |

Cancel Running → CancelledWaitingRefund; harcanmamış escrow bir kez collect edilir. Tamamlanma escrow'u bir kez çıktıya çevirir; kapasite doluysa çıktı bekler. Condition/charge tüketimi tanımlı boundary'de transaction'a dahil; legacy tool kontrolü değiştirilmez. Sleep/catch-up world basis için tek elapsed aralığı; simulation basis unloaded ilerlemez. Mevcut recipe revision/duration mismatch kontrollü ret olmayı sürdürür.

## BuildDefinition / StructureState (PROPOSED)

| Tip / alan | Kural |
|---|---|
| BuildDefinition.definitionId/revision/category | Stable catalog key; enum Fence/Gate/Foundation/Wall/Roof/Utility; MVP tek Fence |
| footprint, clearanceVolumes, allowedSurfaces, maxSlope | Pozitif finite metres; rotated oriented volumes; missing policy fail-closed |
| placementPolicyId/revision, collisionRequirements | Zone/claim/path/spawn/nav/overlap ve limit referansı; doğrulama katmanları açık |
| sockets/snaps[] | Stable socket ID, local pose, compatibility class, occupancy max=1; graph cycle yasak |
| materialCost[] | Recipe ingredient ile aynı units/selection; cost rev tanıma pinned, pozitif |
| prefabRefs | Runtime ve render-only preview ayrı asset keys; stable pivot/scale manifest; gameplay callback dependency yok |
| StructureState.instanceId, definitionId/revision, stateRevision | Instance asla yeniden üretilmez; versionlı registry kaydı |
| worldId, regionId, ownerCellId, zoneId/policyRevision | Tek owner; neighbor occupancy referansları authority değildir |
| transform | Finite position + normalize quaternion; MVP scale=1; transform hash canonical payload'a girer |
| condition, lifecycle, operationalProjection | condition [0,1]; Active/Tombstoned; enabled ile derived operational farklı; utility authority Shelter |
| parentInstanceId, parentSocketId, occupiedSockets[] | Nullable parent; parent ve child reciprocity; owner conflict/cycle ret |
| createdReceiptId, modifiedReceiptId, createdGeneration | Durability/dedupe izi; presentation tekrarının receipt üretmemesi |
| tombstone | removedAtGeneration, reason, prior owner/definition; removed ID reuse yasak |

## Adapter seçenekleri ve seçilen geçiş

| Seçenek | Maliyet / risk | Sonuç |
|---|---|---|
| ShelterRecipe'ı multi-input olarak değiştir | En az görünen dosya; eski GUID, save validation, tool/time varsayımları bozulabilir | Reddedilen öneri |
| Ayrı craft/inventory kopyası | İki algoritma ve çift authority; yüksek bakım | Reddedilen öneri |
| Yeni RecipeDefinition + routing facade + read-only legacy adapter | Orta maliyet; iki formatı test etme; shared inventory transaction sözleşmesi gerekir | Seçilen PROPOSED |

Legacy adapter mapping: recipeId/revision aynen; input/inputQuantity → tek ingredients entry; output/outputQuantity → tek outputs entry; tool → Required nonconsuming legacy kontrol; durationWorldSeconds → WorldSeconds; station/power aynen. Adapter görünüm içindir: mevcut işler ShelterProduction üzerinden başlar/iptal/collect olur. Multi-input job yeni versionlı bölümdedir. Legacy active job'u yeni job'a kopyalama veya aynı recipe ID'yi iki executor'a route etme yasaktır. ID namespace collision catalog validation'da ret.

## Schema/migration ve safe default

SaveGame/SaveHeader mevcut header.schemaVersion/contentVersion/worldId/generation taşır. Construction/node/new craft/receipts için versionlı additive sections önerilir; gerçek sonraki schema numarası uygulama anındaki SaveCodec/SaveValidation audit'inde seçilir (P00 sayı ayırmaz). Eski save'de bölümün bulunmaması → empty structures/deltas/newJobs/receipts; eski inventory, legacy job ve shelter claim aynen. Eski dünyaya node eklemek ayrı content authoring migration'ıdır, empty delta kendiliğinden ağaç üretmez.

Unknown future schema → kontrollü ret; eksik kritik item/structure/recipe definition → controlled load error, orijinal dosya korunur. Yalnız cosmetic ref missing ise collision/authority korunarak placeholder mümkün; gameplay collider yoksa CellReady engellenir. Alias graph tek hedefli/acyclic olmalı; kaldırılan definition için migration veya read-only recovery gerekir. Quantity/escrow/reward korunumu doğrulanmadan version artırılmaz. [D013 policy](PERSISTENCE_CELL_AND_ID_POLICY.md) ve [fixture rehberi](SAFETY_CHECKPOINT_AND_MIGRATION.md) bu şemanın kabul yoludur.

Geri dönüş: eski reader yeni save'i açmaya zorlanmaz; eski backup + eski catalog korunur. G0 owner alan/ID/revision/adapter kararını onaylar; bütün runtime fixture'ları gelecekte owner-run TEST_NOT_RUN'dır.
