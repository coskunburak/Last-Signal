# D013 — Dünya adresi, tek-cell sahipliği ve restore

BLD-P00-DEC-013 · PROPOSED · Belge IMPLEMENTED / TEST_NOT_RUN.
Girdi: D013, LS-DOC-13 §1/2, LS-DOC-17, [şemalar](CONTRACT_DATA_MODELS.md), [işlem sırası](CONSTRUCTION_TRANSACTION_SEQUENCE.md). [Kart izi](BLD_S002_HANDOFF.md). Sahip Burak/teknik geliştirici; temel BLD-S003/009, genişleme BLD-S021.

## Adres ve ID

| Kimlik | Politika / mevcut fark |
|---|---|
| WorldId | SaveHeader.worldId ile aynı dünya; yeni dünya yeni namespace. Başka world ID'li request/section ret. |
| RegionId | Versionlı content manifest kimliği, POI/sanat grubu; mevcut cell string'ine gömülmez. Region sınırı storage authority yaratmaz. |
| CellId | Mevcut `resident` veya `cell:x:z` aynen. [CellCoordinate](../../../../Assets/LastSignal/Scripts/Runtime/WorldCells/CellCoordinate.cs):7–21 gerçek grid 64 m, half-open ve negatifte floor. LS-DOC-13'teki 100 m önerisi mevcut kod değeri değildir; P00 grid değiştirmez. |
| BuildZoneId | World/region içinde authored stable key; shelterId + policyRevision referansı. Konumdan türetilerek rename edilmez. |
| StructureInstanceId | Registry tarafından world namespace içinde bir kez oluşturulan UUID; collision registry'de kontrol edilir; durable receipt'te sabitlenir. Retry aynı ID; editor PersistentEntityId.Reset kullanılmaz. |
| ResourceNodeId | Authored stable key veya versionlı authoring manifest; terrain tree array index/pozisyon hash'i tek başına yeterli değil. |

Root yapı owner cell'i placement anchor noktasının half-open cell adresidir; `resident` zone mapping'i explicit authored topology ile öncelik alır. OwnerCellId commit'te kaydedilir, floating-point pozisyon yeniden yorumlanarak load'da değiştirilmez. Boundary epsilon bir query toleransıdır, ID mapping kuralını değiştiremez. Yeni relocation başka owner gerektirirse atomic migration command gerekir; P00/ilk playable relocation yok.

## İki-cell yapı ve snap graph

İlk playable footprint tek cell içinde; sınır aşımı `UnsupportedCellSpan` ret. Gelecek destek şimdiden tek-owner sözleşmesine bağlıdır: root anchor owner'ı authoritative kaydı tutar; intersect edilen komşular yalnız `(instanceId, ownerCellId, bounds, revision)` occupancy referansı tutar. Neighbor ref malzeme, condition veya ayrı spawn hakkı taşımaz.

İki-cell commit için owner ve bütün intersect cell'ler sorted ID sırasıyla lease alınmış, Ready ve epoch'ları güncel olmalı. Komşu yalnız snapshot/query data ile okunabiliyor olsa bile collision/nav hazır değilse commit BusyCell'dir. Neighbor'dan görünür hale gelme owner authority cache/DTO'sunu hazırlar; registry `EnsureHydrated(instanceId)` dedupe eder. Lease varken unload bekler; yüklenen komşu görünür yapıyı gerektiriyorsa owner dependency pinned kalır. Pinning zinciri/cell yük sınırı ölçülmeden destek açılmaz.

Snap child root graph'ın ownerCellId'sini miras alır; child pivot başka cell'de diye ikinci owner oluşmaz. Tek parent, acyclic graph, unique occupied socket, reciprocal parent-child referansları zorunlu. Parent tombstone veya missing olduğunda child havada aktive edilmez. Destruction/relocation ileride atomik graph planı olmadan reddedilir; ilk sürüm otomatik fiziksel çökme içermez. Restore topolojik sırada root→child; graph bozuksa controlled recovery.

## Resident / unloaded authority

Mevcut WorldCellManager hazır cell'i Capture eder, unloaded cell Delta'sını saklar. Yeni domain için registry, yüklenmiş GameObject'lerden bağımsız authoritative record store'dur. WorldCells serialized delta'nın custody'sini taşır; domain hydration lease devri sırasında bir live owner seçilir. Resident kayıtları da aynı generation'a girer; `resident` serbest tarama veya kalıcılık istisnası değildir.

Save candidate = player/inventory + legacy/new jobs + resident dünya + unloaded cell deltas + node changes + structure registry + tombstones + receipt/high-water + clock aynı worldSequence N. GameObject enumeration yalnız projection doğrulaması olabilir, dünyanın toplam kaydı olamaz. Unvisited node'lar authored manifest default'u; depleted/hit node'lar sparse delta. Hasat depletion ve pickup kaynak ilişkisi birlikte saklanır; despawn edilmiş görsel reload'da node'u yenilemez.

Tombstone: instance ID, owner, removal generation/revision, definition ref ve reason. Authoring seed veya eski snapshot tombstone'dan eskiyse yeniden spawn etmez. Silinen ID reuse yasak. Tombstone compaction yalnız onu gerektiren bütün retained backup generations, cell baselines ve receipt pencereleri güvenle ilerletildiğinde mümkündür; başlangıçta compaction kapalı. World size sınırı ve save boyutu QA'da ölçülür.

## Restore sırası ve migration

1. Source dosya immutable; current/backup checksum/size/schema/catalog/worldId validate. Unknown future version controlled ret.
2. Alias migration kopya DTO'da: definition rename, moved cell, region/zone redirect. Cycle, many-to-one ID çakışması veya eksik hedef ret. World ID kendiliğinden yeniden yazılmaz.
3. Global ID index oluştur; duplicate root/child/node/receipt ret. Tombstones active kayıtlarla aynı revision'da çakışırsa sessiz seçim yapma; migration fixture gerekir.
4. Authoritative records ve unloaded deltas yükle; registry instanceId başına tek kayıt. Runtime shell ID'sini activation öncesi saved ID'ye bağla.
5. Owner/neighbor dependency ve snap topolojisi hazırla; collider/nav registration tamamla, Physics sync ve required clearance/readiness doğrula. Missing critical prefab/collider → cell Failed, input kapalı; cosmetics ayrı fallback olabilir.
6. Bir worldSequence'de hydrated flag/epoch yayınla; UI initial snapshot, sonra input. Restore hit/noise/build ödül eventlerini replay etmez.

Old save'de construction/resources/new jobs section yok → empty new state; mevcut legacy escrow/storage/clock olduğu gibi. Topology değişiminde mevcut `ValidateTopology` exact ID kontrolü göz ardı edilmez; alias migration sonrası yeni topology validation ve backup gerekir. Scene reload sadece memory projection'ı değiştirir, logical ID/owner değiştirmez.

Alternatif her overlap cell'e yapı kopyası reddedildi (dupe/çift maliyet); yalnız loaded objeleri save etme reddedildi (unloaded kayıp). Geri dönüş: iki-cell özelliğini kapat, mevcut kaydı silmeden read/load destekle; eski schema için backup. Kabul zinciri: create→save→unload→load üç tekrar→remove→save→load; 1 ID, sonra 0 active+1 tombstone. Root/child iki-cell fixture ve negatif coordinate sınırı ayrıca [QA'da](QA_AND_FAULT_MATRIX.md). Şimdi hiçbiri çalıştırılmadı.
