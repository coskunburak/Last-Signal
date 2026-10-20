# ADR-P01-001 — S003 finite harvest authority

2026-10-18 · Implementation decision; owner validation PENDING. P00 G0 remains ACCEPTED. This ADR narrows the approved contracts to a three-node pilot; it does not approve G1 or S004. Explanations below are the implementation handoff, not changes to canonical BLD source cards.

## Sahiplik ve veri

`GatheringWorld` tek live authority; `GatheringState` detached transition/validation; `ResourceNodeView` yalnız projection. `ResourceNodeDefinition` revisionlı SO: requiredToolId=`weapon.axe`, minimumToolTier=1 (V1 yalnız bu mevcut ekipman), max health 100, strike damage 25, fixed yield 3 × `material.log`. Bir node tek output üretir, randomness/respawn/repair yok. Health/damage/yield ve log 2000 g/stack 8 değerleri BALANCE_PROPOSED.

Manifest GUID ve revision, kalıcı node GUID, cell ID, authored anchor pose, definition ID/revision/output ve finite bütçe snapshot'ta pin edilir. Editor locator/hierarchy/sibling index yalnız ilk kaynak objesini kopyada bulmak içindir; save identity değildir. Runtime yeniden node GUID üretmez. Kaynak dosya SHA256 manifest provenance'ıdır. Node/manifest/definition revision veya anchor pose değişikliği restore'da fail closed; otomatik alias, relocation veya content migration yok. Aynı ID'nin yeni node'a atanması yasak.

SaveHeader.gatheringVersion=1 ve SaveGame.gathering.version=1 additive alanlardır; mevcut schemaVersion 1–4 ve envelope format değiştirilmez. Harvest origin=4 yeni enum değeridir. Node kayıtları ve pickup receipts gathering bölümünde bir kez saklanır; genel world.items/cell.world.items içine ikinci kez girmez. Header mevcut world/content/generation kimliğini taşır. Cell IDs mevcut 64 m half-open `cell:x:z`/`resident` politikasıdır; grid değişmedi.

Node: health, maximumHealth, damage, budget, remaining, revision, stage, yieldSequence, reward location ve optional pickup. Standing/Damaged → Depleted; FelledPresentation yalnız gelecek presentation enum rezervi. Terminal ID `nodeId:yield:1`: V1 generation=0 ve outputIndex=0 sabit, yieldSequence=1. Aynı ID yeniden kullanılamaz. Receipt `(actorSession,lastSwing,sequence)` ve node revision high-water işlevi görür; geçmiş swing başına sınırsız kayıt tutulmaz. Yeni session token yalnız yeni session başlangıcında oluşur. Runtime controller, actor ve session generation kontrolü eski controller replay'ini reddeder.

## Combat ve alet

Mevcut `MeleeAttackState.ActiveWindow` tek dispatch noktasıdır. Mevcut stamina windup başlangıcında bir kez ödenir; miss/reject/disk abort bu swing stamina'sını geri vermez. Hasat ikinci stamina harcaması yapmaz. Range/radius/timing mevcut WoodenAxe definition'ından: range 1.9, stamina 43, windup .48, active .13, recovery .78. Göz/alet origin uzaklığı <=1.5 m, facing dot >=.65; finite range, visibility ve collider ownership yeniden kontrol edilir. Bounded queries saturation'da reddeder.

En yakın görünür aday seçilir; tam mesafe eşitliğinde canlı enemy, aynı tür eşitliğinde yalnız o swing için collider instance ID tie-break. Bu geçici tie-break persist identity değildir. Tree IDamageable yapılmadı. Seçilen aday reddedilirse başka hedefe ikinci dispatch yok. Harvest ya da enemy, ikisi birden değil. Mevcut canonical melee impact noise sadece resolver başarı döndürdüğünde bir kez; restore hiçbir hit/noise replay etmez. Yeni chop/fall audio S004'e bağlı.

Mevcut axe ItemDefinition.worldPrefab=null. Possession envanterde sahte axe stack'i ile kontrol edilmez: PlayerCombatController'ın gerçek owned melee controller'ı, selected Melee slot'u ve FindMelee(AxeId) eşitliği gerekir. Pilot hazırlayıcı player prefab heavyMelee axe ref'ini doğrular; mevcut ekipman yolunu değiştirmez.

`axeCondition` 0–100, tek owned axe'ın yalnız gathering wear kaydıdır. Accepted durable hit başına 1; son puan blow'u tamamlar, sonra sonraki harvest BrokenTool. Combat damage/condition/grip değişmez. Genel instance durability/equipment swapping/repair sistemi değildir; birden fazla axe desteklenecekse versionlı migration gerekir. Owner'ın bu dar wear ve denge politikasını kabul etmesi gerekir.

## Durable transaction sırası

1. Ready session/cell, living owned player/equipped axe, active SwingId, revision ve geometri doğrulanır.
2. Detached candidate hazırlanır. Son vuruşta güvenli spawn aranır; bulunamazsa aynı kalıcı reward `Pending` olur.
3. Public SaveSession.Capture mevcut state için çağrılır **busy/barrier alınmadan önce**. Sleep/clock advance, treatment, cells unstable, door busy, inventory ownership, unresolved loot vb. mevcut guard'lar aynen geçerlidir. Busy durumunda hasat kabul edilmez.
4. Candidate gathering section tam captured world snapshot'a takılır; synchronous main-thread SaveSession busy + OwnershipTransaction fence alınır. Arada yield, worker veya gameplay callback yok. S010, inventory, world-time, population, vehicles ve player aynı snapshot generation'ındadır. Bu fence async/all-thread linearizability iddiası değildir; yeni async writer eklenirse bu sözleşme yeniden tasarlanmalıdır.
5. SaveFileStore önceki doğrulanmış generation+1'i temp'e yazar/flush eder, checksum/validation/exact candidate karşılaştırması yapar; atomic replace/move ile yayımlar.
6. Definitive pre-publish hata: live node/wear/yield değişmediğinden refund/rollback gerekmiyor. Publish exception: disk yeniden okunur; tam normalize edilmiş **bütün world snapshot** candidate'a eşitse committed, önceki snapshot'a eşitse aborted; ne olduğu belirlenemiyorsa RecoveryRequired. Yalnız gathering eşitliği yeterli değildir.
7. Committed candidate ancak bundan sonra live authority'ye alınır; ardından standing visual kapanır ve pickup projekte edilir. Projection exception receipt'i geri almaz: harvest/save kapısı kapanır, pause + verified checkpoint load gerekir. Yeni ödül ID'si üretilmez.

Her accepted hit tam synchronous disk write yapar. IO/frame hitch maliyeti ÖLÇÜLMEDİ; production performans kabulü bekler. Interrupted process diskten committed snapshot'ı alır; temp dosya asla committed sayılmaz. Durability physical backend garantileriyle sınırlıdır; işletim sistemi/disk kaybı için ayrı journal garantisi iddia edilmez.

## Ödül, pickup ve cell lifecycle

12 deterministik aday (2/3 m yarıçap), terrain/explicit additionalGround, normal.y>=.85; bounded ray/overlap, protectedAreas ve owner-cell footprint kontrolü. Ödül kutusu 1.2×.5×.6 m; wrapper collider 1×.4×.4 m. NonAlloc buffer doluysa nokta reddedilir. Roads/doors/POI/nav koridorları fizik query ile semantik olarak keşfedilmez: owner explicit exclusion collider'larını atayıp clearanceApproved vermelidir. Varsayılan false. NavMesh bake/asset değişikliği yok.

Yer yoksa Pending quantity=budget kalır; depletion sonrası küçük retry collider + interaction prompt ile yeniden deneme. Retry sequence ilerletir; yield/condition/swing yeniden harcanmaz. WorldItem önce inactive staging root altında identity/quantity/guard/listener ile kurulur, sonra aktive edilir. Collider ve renderer gerçek pickup wrapper'ındadır; Rigidbody/NavMeshObstacle eklenmez.

WorldItem'ın mevcut BeginWorldAdd/CompleteWorldMutation ve OwnershipTransaction yolu korunur. Full inventory: quantity korunur, "Inventory full — wood remains here"; partial: accepted kadar azalır, remainder aynı ID. Zero: consumed tombstone. Guard güncel state'i ID ile çözer; eski candidate closure'ına güvenmez. Pickup transferi mevcut live ownership transaction'ıdır; her pickup ayrıca disk write yapmaz. Sonraki explicit save veya accepted hit inventory+pickup remainder'ı aynı generation'da capture eder. Crash daha eski generation'a bütün sahipleri birlikte geri alır; inventory ve pickup ayrı dosyalarda tutulmaz. Kütle mevcut integer grams türevi, kapasite mevcut slot bazlıdır.

CellLeaving görsel pickup'ları anında pasifleştirir; state domain registry'de kalır. CellReady hazır content'teki authored view'ları bağlar ve aynı IDs'yi yeniden projekte eder. Cell serialized custody bu sürümde merkezi gathering section'dadır; legacy cell.world içine kopyalanmaz. Unloaded cell records da full generation'a girer. İlk pilot resident; nonresident authoring/roundtrip ayrı owner fixture gerektirir, hazır olduğu iddia edilmez.

Eski/no-section save: yalnız explicit authored manifest'in untouched başlangıcı; bütün dekoratif orman kaynak olmaz. Legacy inventory/job/shelter alanları değiştirilmez. Load: preflight topology → session/owned equipment → cells ready → legacy hydration → gathering hydration → physics/player clearance → resume. İkinci hydrate eski pickup projections'ı kaldırır, aynı kayıtları projekte eder. Menu/new game ile reset açık session davranışıdır; load/unload respawn değildir. Yeni pilot ayrı saveSlot kullanır; eski save veya catalog overwrite yok. Eski binary yeni additive alanı bilmiyor olabilir; ileriye dönük eski-reader uyumluluğu iddia edilmez, pilot save eski build ile açılmaz.
