# D012 — Construction işlem, durability ve compensation sözleşmesi

BLD-P00-DEC-012 · PROPOSED / PENDING_OWNER (durability trade-off) · Belge IMPLEMENTED / TEST_NOT_RUN.
Girdi: D012, [API farkları](CURRENT_CODE_BOUNDARIES.md), LS-DOC-07 REQ-INV-001, LS-DOC-14 REQ-CRF-001, LS-DOC-17 REQ-SAVE-001/002/003 ve transaction governance. [Kart izi](BLD_S002_HANDOFF.md). Sahip Burak/teknik geliştirici; uygulama BLD-S005/009, ölçek BLD-S021. Bu belge bir transaction implementasyonu değildir.

## Invariant ve başarı anlamı

`Success` = maliyet tek kez tüketildi + bir StructureState oluştu + aynı receipt ve tüm etkilenen state aynı durable world generation'da. Preview/Rejected/Aborted = kalıcı inventory, registry ve node değişimi 0. Reservation geçici spendable miktarı azaltır; item ownership kaynak container'da kalır. Commit öncesi abort rezervasyonu bırakır, item'i ikinci kez eklemez.

Unity Instantiate, managed dictionary mutation ve dosya publish birlikte atomik değildir. Önerilen çözüm: kontrollü staging + kısa synchronous mutation + session freeze altında immutable tam save generation + publish sonrası presentation. Disk publish commit noktasıdır. `OwnershipTransaction` yalnız kısa synchronous uygulama/restoration bölümünde kullanılır; frame veya await boyunca tutulamaz.

| Seçenek | Sonuç / maliyet | Karar |
|---|---|---|
| TryRemove döngüsü → Instantiate → normal save | Partial loss, duplicate retry, callback exception ve crash boşluğu | Reddedildi |
| Write-ahead journal + replay | Düşük write latency potansiyeli; replay/compaction/nonce çok daha karmaşık | Sonraki ölçüm gerekirse ADR; şimdi seçilmedi |
| Candidate full generation + eski backup + session mutation gate | Daha basit recovery; her build'de write ve kısa bekleme, ölçüm gerekli | Seçilen PROPOSED; OD-04 |

Mevcut SaveFileStore.Write candidate temp, readback ve publish kullanır; PhysicalSaveFiles.Flush(true)/Replace/Move yüzeyleri mevcuttur. Bu mevcut kodun yeni Construction için yeterli crash garantisi verdiği iddia edilmez. Yeni section validation, publish ambiguity reconciliation ve bütün writer'ların gate'e katılması gelecekte zorunludur.

## Request, nonce ve serializasyon

Confirm payload: worldId, actorId, sessionEpoch, nonce, immutable payloadHash, definition/catalog/policy revisions, pose, parent/socket, explicit source container IDs + expected revisions, cell epochs. Nonce bir onay intent'inde bir kez üretilir; çift tıklama, timeout veya reconnect aynı intent için aynı nonce'u kullanır. Yeni konum/maliyet onayı yeni nonce gerektirir.

Key `(worldId, actorId, nonce)`. Aynı key + aynı hash: Pending döndür veya mevcut durable receipt'i oku; yeniden maliyet tüketme. Farklı hash: NonceConflict. Client'ın farklı nonce'larla iki hızlı tıklaması da overlap/occupancy ve güncel revision revalidation ile engellenir; tasarım gerçek iki geçerli siparişi tek saymaz. Domain dispatcher tek sıralıdır; lock sırası world gate → sıralı cell IDs → sorted container IDs → registry/parent. Bu local single-player reentrancy kontrolüdür.

Receipt fields: key, payloadHash, outcome, result instanceId, consumed cost vector, before/after revisions, worldSequence, generation, resultHash. Ret retry yeni query/nonce ile; timeout sonucu bilinmiyorsa eski nonce sorgulanmadan yeni intent açılmaz. Durable ledger ve ID allocator state aynı generation'dadır. Bounded retention: actor epoch+sequence high-water ve son receipt penceresi; prune edilmiş eski nonce `ExpiredRequest` ile ret, asla yeni kabul edilmez. Pencere boyutu/perf OPEN; gelecekte migration değiştirirse high-water korunur. Tombstone eski yaratım nonce'unu yeniden kullanılabilir yapmaz.

## Basamaklar

| Aşama | Sahip / işlem | Başarısızlık ve save davranışı |
|---|---|---|
| 0 Preview | Presentation → ortak validator query; kaynak yazımı yok | Ghost transient; iptal ücretsiz |
| 1 Preflight | Actor alive/ready, catalog ve zone/config, source erişimi | Typed ret; nonce'a henüz maliyet bağlanmaz |
| 2 Admit nonce | Coordinator key/hash dedupe, pending intent kaydı | Duplicate pending/receipt; session epoch stale ret |
| 3 Acquire | Önerilen session mutation gate; affected cell leases ve source revision kontrolü | Yeni build/craft/transfer/save/sleep/unload Busy; lock alınamazsa abort |
| 4 Authoritative revalidate | Aynı validator güncel claim/pose/geometry/nav/snap/material/policy/revision okur | Kaynak veya geometry değiştiyse typed ret, bütün leases release |
| 5 Reserve ve stage | Inventory all-ingredients planı; registry ID tahsisi; before-images; inactive, güvenli prefab shell hazırlığı | Tek ingredient bile eksikse plan uygulanmaz; instantiate/callback exception cleanup, reservation release |
| 6 Readiness ve son kontrol | Staged collider/nav proxy uygunluğu; hiçbir public gameplay mutation yok; epoch/actor tekrar kontrol | Actor death/pause/cell close önce gelmişse abort; gerekirse yeni query |
| 7 Tentative authoritative mutation | Kısa OwnershipTransaction içinde no-throw planned inventory consume + registry insert + parent occupancy; observer yok | Unexpected exception → exact before-image restore, ID/nonce planı abort; stale query'ler geçersizleştirilir |
| 8 Immutable candidate | Gate hâlâ kapalı; tüm domain state N tek world generation DTO; receipt dahil; serialization bounds kontrolü | Public SaveSession.Capture çağırıp busy guard bypass edilmez; proposed internal coordinator capture API gerekir |
| 9 Durable publish | Eski generation korunur; temp yaz/flush/readback/checksum → atomic publish; outcome doğrula | Definite prepublish failure → tentative state rollback; belirsiz publish → reconciliation, gate açılmaz |
| 10 Publish presentation | Durable receipt sonra active projection/nav/collision ready, committed events ve UI | Observer failure kaynak rollback yapmaz; registry'den retry/hydrate; güvenli ready olmazsa session recovery |
| 11 Release | Lease/gate release; queued commands sonraki boundary | Başarı receipt döner, duplicate aynı sonucu alır |

Session mutation gate yeni bir tasarım bağımlılığıdır: inventory, legacy/new craft, gathering, cell load/unload, world clock, actor death/session close ve save bütün writer'ları aynı fence'e katılmadan Step 7 kullanılamaz. Synchronous `OwnershipTransaction`'ı await boyunca açık tutmak çözüm değildir. İlk candidate seçeneği global simulation/input freeze'dir; kısa bekleme oyuncuya gösterilir, elapsed world time bu sırada ilerlemez. Birden fazla asynchronous writer'ı görmezden gelen “single-player zaten güvenli” varsayımı reddedilir. Bekleme bütçesi NOT_MEASURED; user deneyimi kabulü BLD-S009/G3'te gerekir.

## Rollback, crash ve belirsiz write

Rollback before-image yalnız gate altında ve hiçbir observer publish edilmeden uygulanır. Inventory adet/slotları, registry/snap occupancy, node delta varsa tamamı geri döner; revision monotonic kalır ve abort epoch artırılarak eski preview invalidate edilir. “Refund TryAdd” geri alma değildir: container dolabilir, notification yeniden ödül üretebilir. Rollback uygulanamazsa session input kapalı, önceki geçerli generation'dan controlled recovery; yarım state ile devam veya autosave yok.

Step 7–9 sırasında crash: disk önceki geçerli generation'ı okur; orada hem malzeme hem yapı önceki durumdadır. Normal son save'den sonraki kaydedilmemiş gameplay kaybı mümkündür; bu yeni yapının parasını alıp yapıyı kaybetme anlamında parçalı commit değildir. Publish tamamlanıp cevap kaybolduysa yeni generation receipt ile bir kez restore edilir. Publish sonucu belirsizken otomatik refund/başka save yok: current/backup candidate generation+receipt+hash incelenir; yeni doğrulanırsa committed, eski doğrulanır ve yeni publish olmadığı kesinleşirse abort. İkisi de doğrulanamıyorsa RecoveryRequired; dosyalar korunur. Eski backup'a dönülürse hem inventory hem yapılar birlikte eski generation'a döner ve owner'a rollback bildirilir.

Staged prefab `Awake` dahil dış yan etki yapmayacak şekilde authoring gate'inden geçmelidir; inactive instantiate'ın bütün callback'leri susturduğu varsayılmaz. Gameplay activation callback'i commit logic değildir. Postcommit görünürlük hatasında kaynak iade edilmez: durable yapı vardır, projection yeniden kurulur. Nav/collision readiness sağlanmadan input açılmaz. Bu ayrım “başarısız işlemde kaynak eksilmez” sözleşmesini netleştirir: durable success sonrası render failure bir transaction abort değildir, repairable presentation failure'dır.

## Fault tablosu ve beklenen receipt

| Fault | Hangi boundary | Beklenen sonuç |
|---|---|---|
| Double-click / same frame duplicate | Admit | Aynı Pending/Success receipt, en çok 1 instance ve 1 maliyet |
| Preview sonrası stok azalır; craft/build aynı malzemeyi ister | Revalidate/reserve | SourceChanged veya MissingMaterials; yalnız dispatcher'da önce kabul edilen harcar |
| Spawned object callback exception | Staging | Tam cleanup; hiçbir item tüketimi yok; FailedStaging |
| Commit callback exception | Tentative mutation | Before-image rollback; RecoveryRequired yalnız rollback bozulursa |
| Disabled scene/cell unload | Lease öncesi/sonrası | Önce BusyCell; sonra unload bekler; pending cancel durable publish öncesi abort |
| Full storage / refund destination full | Craft collect | CompletedWaitingOutput/CancelledWaitingRefund; item işte kalır, kaybolmaz |
| Death / pause / sleep | Step 6 öncesi; gate sırasında | Önce ActorUnavailable; gate sırasında yaşam döngüsü isteği sıraya alınır, commit/abort boundary sonrası işlenir |
| Cancel preview / cancel pending build | Step 0 / prepublish | Ghost cleanup / güvenli abort; publish başladıysa “sonuç bekleniyor”, receipt sorgusu |
| Save istendi | In-flight mutation | SaveBusy; başka generation yarım state'i yakalayamaz |
| Disk full / permission / temp truncation | Prepublish | PersistenceFailed, tentative rollback, önceki save korunur |
| Interrupt after publish / lost response | Postpublish | Aynı nonce durable success; reload 1 yapı; refund yok |
| Restore/cell load ile race / stale session | Admit veya ready | BusyCell/StaleSession; eski callback yeni session state'ini yazamaz |
| Listener exception | Postcommit event | Hata kaydı; kalan observers izolasyonla sürer; oyun ödülü replay edilmez |

Geri dönüş: full-generation yaklaşımı kabul edilemez latency gösterirse yeni ADR ile journal tasarlanır; güvenlik koşulları gevşetilmez. Owner OD-04 ve [QA fault senaryolarını](QA_AND_FAULT_MATRIX.md) kabul eder; çalışma kanıtı olmadan atomiklik veya crash safety PASS değildir.
