# S023 kabul ve regresyon matrisi

Son regresyon durumu: streaming/population/archetype 19/19 PASS. Full EditMode ilk koşuda 805/806 PASS; eski Arberry prefab beklentisi güncellendikten sonra ilgili dar retest 1/1 PASS (`Evidence/Manual-20261015-124004-focused-edit-tjvo1s1n/`, exitCode=0, changed=[]). Güncel full EditMode ve full PlayMode sonucu bekleniyor; tek metodun retesti full suite PASS sayılmaz.

Güncel kullanıcı kanıtı (15 Ekim 2026): native authoring + tekrar authoring PASS; focused EditMode 20/20 PASS; cancellation/missing-navigation PASS; fixture düzeltmesi sonrası roundtrip dar retest 1/1 PASS (`Evidence/Manual-20261015-123215-focused-play-mpcrxy2c`, exitCode=0, source değişikliği yok). Önceki roundtrip FAIL kaydı korunur. Ayrıntılı run yolları IMPLEMENTATION_REPORT içinde. Geniş regresyon, process restart, build ve görsel kabul NOT_RUN; performans/maliyet NOT_MEASURED. Aşağıdaki matris doğrulama kapsamını listeler; mevcut dar sonuçlar tüm satırların kabul edildiği anlamına gelmez.

| Risk / gereksinim | Hazır doğrulama | Evidence | Sonraki adım |
|---|---|---|---|
| Bölge rolü, D221 | REGION_DESIGN + oyuncu rota kararları | Manual/design-notes.md | Kullanıcı kabulü |
| Geometri/CellReady, D222–224 | Authoring.Validate: S019 Roots, ground/solids, native agent filter, 11 rota, loot placement | author-*/authoring.json | İlk author |
| Koordinat/katalog/GUID/ID | S023RegionTests + native dependency manifest; script/meta statik kaydı | focused-edit/tests.xml; source-before/after | EditMode |
| Eksik prefab/material/script, yanlış catalog | Mevcut S019 ContentValidation + author dependency preflight; source bozuksa fail closed | author unity.log | Hata sınıflandır |
| Yinelenen POI/entity ID | S023RegionTests iki instance ile mevcut S019 duplicate check | focused-edit/tests.xml | Testi çalıştır |
| Eski save topology | Isolated world/slot; 2-cell snapshot 4-cell pilotta reddedilir | focused-edit/tests.xml | Eski kişisel save kullanma |
| Tekrar authoring | Aynı komut iki kez, ikinci reused=true ve hashler değişmez | İki author run source hashleri | İlk author sonrası |
| Kapı, consumed loot, drop, enemy | DepotYardResidentRoundTripPreservesDoorLootDropAndEnemy | focused-play/tests.xml | Kullanıcı çalıştırır |
| Nav yoksa giriş reddi, activation iptali | PilotCancellationAndMissingNavigationFailClosed (geçici definition clone) | focused-play/tests.xml | Kullanıcı çalıştırır |
| Stale token/session callback, readiness prereq | Mevcut WorldCellTests, WorldCellIntegrationTests | focused-edit + regression filter | Yeniden fixture yazılmadı |
| Save esnasında yükleme/unload, rapid traversal | Save busy assertion + mevcut WorldCellIntegrationTests; manuel hızlı geri/ileri | tests.xml + video/log | Runtime acceptance |
| Population accounting/archetypes/noise | Mevcut WorldPopulationPlayTests/S022ArchetypePlayTests | regression filter/tests.xml | Bounded pilot population ile manuel de kontrol |
| Eksik navigation/collider | Pilot missing nav fixture + mevcut MissingCollisionFailsClosed | focused/regression tests.xml | Fail nedeni görünmeli |
| Bozuk save/topology, atomik yazma | Mevcut SaveValidationTests/PersistenceIntegrationTests, full regression | regression-edit/play | Kişisel save üzerinde fault injection yok |
| 50 load/unload, plateau, stale listeners | PERFORMANCE_REPORT prosedürü | capture CSV + Profiler snapshot | Kullanıcı profiling |
| Gerçek process restart | HANDOFF adım 7 | Manual/restart.md + video + log | Aynı process coroutine testi yeterli değil |
| Day/night/rain/flashlight | S023Capture condition + kullanıcı sabit kamera görselleri | Manual/visual/ | Art acceptance NOT_RUN |
| Build ve standalone | Gerçek S023Build.DevelopmentMac + kullanıcı gameplay | development-build + Manual | Focused/full pass sonrası |
| Üretim saatleri / S019 kazanımı | cost-log.csv + cost report | Gerçek kullanıcı zaman kayıtları | Uydurma geriye dönük süre yok |

Hata sınıfları: COMPILATION_ERROR, TEST_INFRASTRUCTURE, TEST_FIXTURE_ERROR, PRODUCTION_CODE_BUG, SCENE_AUTHORING_ERROR, ASSET_IMPORT_ERROR, NAVIGATION_ERROR, PERSISTENCE_ERROR, STREAMING_ERROR, PERFORMANCE_REGRESSION, ENVIRONMENT_ISSUE, UNRELATED_BASELINE_FAILURE. Hata satırı/stack trace üzerinden en dar düzeltme ve retest uygulanır. Bir önceki sprintin PASS'i S023 kabulü sayılmaz.
