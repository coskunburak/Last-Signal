# P01 açık riskler / owner kararları

2026-10-18. Hiçbir satır PASS değildir; bu kayıt implementation handoff'tur.

| ID | Risk / mevcut kontrol | Kapanış için gereken |
|---|---|---|
| R01 | Yeni runtime/editor/tests compile edilmedi. Kaynak incelemesi compilation kanıtı değil. | Önce tek P01HarvestTests focused runner sonucu |
| R02 | Her accepted hit tam synchronous world save yapar; large world/cell/population snapshot latency, JsonUtility allocations ve disk IO ölçülmedi. Physics queries bounded; accepted-hit path allocation-free değildir. | Owner profiler/slow-disk ölçümü; G1 perf bütçesi gerçek baseline'dan |
| R03 | Mutation fence synchronous main-thread kodu kapsar; async writer veya reentrant third-party callback garantisi yok. Unknown publish'de harvest + save kapalı/pause; Resume ile normal gameplay'e dönülmesi bu recovery'yi çözmez. | Fault tests; verified checkpoint reload; gelecekte async writer eklenirse tasarım tekrar incelenir |
| R04 | Native pilot scene/prefab/SO henüz üretilmedi. Source VisualSlice önceden dirty; unsaved Editor state araçlardan okunmadı. | Owner checkpoint, native menu, stage receipt; kaynak overwrite olmadan authoring |
| R05 | Yol/kapı/POI/nav exclusions semantic metadata ile otomatik bulunmuyor. clearanceApproved=false default; owner explicit protectedAreas gerektirir. | Selected tree/drop footprint/door/road/water/nav native review; kutu açıklığı ve path testi |
| R06 | S003 terminal görsel yalnız standing off + pickup/pending. Cut/stump/fall/chop/audio/UX polish yok; yakın FPS görünüm üretim kalitesi iddia edilmiyor. | S003 acceptance ardından S004; AUDIO_ASSET_PENDING_OWNER |
| R07 | 100-point wear yalnız tek mevcut owned axe ve gathering için; combat axe damage davranışı aynı. Multi-instance durability, repairing ve tool inventory transfer yok. | OD01 aşağıdaki dar policy; genişletme için schema/version migration |
| R08 | Manifest/definition revision/anchor pose fail closed; aynı save'e yeni node ekleme, removal/relocation/source-generation update migration'ı yok. Stage yeniden üretmek yeni IDs/saveSlot yaratır. | OD03 content freeze; değişiklik gerektiğinde explicit migration ve backup |
| R09 | İlk native pilot resident. Nonresident cell mapping kodu var fakat hazır test cell prefab/staged fixture henüz yok. | D024 native cell unload/reload owner fixture + evidence; unsupported traversal'da kabul yok |
| R10 | Current snapshot node+reward+receipt atomic; pickup live transfer mevcut inventory transaction'ı, ayrıca immediate disk publication değil. Daha eski checkpoint bütün inventory/world'u birlikte restore eder. | Old/new native save, partial/consumed inventory+pickup roundtrip |
| R11 | Scene composition old save fixture, source unknown refs, absent section JsonUtility behavior ve full gameplay noise/AI listener counts test edilmedi. | Focused codec ardından native old-save + cadence + regression evidence |
| R12 | Source log bark mesh/material yerel; yeni license/release audit yapılmadı. Scrap icon provisional. | Owner mevcut asset provenance ve S004 art/sound kararı |
| R13 | Pilot sadece3 node, maksimum runtime64; 10–20 repeated-harvest perf senaryosu için yeni açık test manifest gerekir. Repair/respawn ile sahte loop yapılmaz. | Owner onaylı perf fixture, gerçek accepted-hit/snapshot object counts |
| R14 | Yeni meta GUID'ler üretildi, bütün repo GUID inventory taranmadı. Existing GUID değiştirilmedi. | Unity import/compiler ve asset refs/duplicate GUID Console kontrolü |
| R15 | Additive extension yeni reader için eski-save desteğidir; eski binary yeni gathering alanını tanımayabilir. Base schema numarasının aynı kalması eski reader uyumluluğu garantisi değildir. | Pilot slot/source backup ayrımı korunur; eski binary ile yeni pilot save açılmaz. Release migration ayrı owner kabulü |

## Owner decisions — şimdi ikinci bir izin talebi değil, gate listesi

- **OD01:** Health100/damage25/yield3/log2000g/stack8 ve harvest-only axe100→0 wear dengesi kabul edilecek mi? Şimdilik BALANCE_PROPOSED.
- **OD02:** Gerçek 2 Pine+1 Birch pilot seçimi ve protectedAreas/ground/native nav clearance kabulü. clearanceApproved yalnız owner incelemesi sonrası true.
- **OD03:** Manifest/revision pin ve ayrı `harvest_<stamp>` save slot; bu pilot content'i freeze edilsin mi? Source regen/promotion/migration ayrıca karar gerektirir.
- **OD04:** Focused → pickup → old/new save + cell → S010/S021/FirstTown regression evidence sonrasında S003 acceptance. O zamana kadar S004 başlamaz.
- **OD05:** S004 audio/local-license/art seçimi ve finale G1 imzası. Yeni external asset şimdilik gerekmez; indirme/import izni çıkarılmadı.

Known feasible work prepared: runtime, isolated authoring code, typed state/validation, durable publication reconciliation, focused tests and handoff. Native operations/tests owner'a ait olduğu için bunların yokluğu PASS olarak kapatılmadı. P02 başlamaz.
