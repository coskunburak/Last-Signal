# D111–D120 başlangıç fark matrisi

Kaynak yolları aksi belirtilmedikçe `Assets/LastSignal/Scripts/Runtime/` altındadır. Tarihsel kanıt güncel PASS değildir.

| Kart | Canonical gereksinim | Mevcut uygulama | Kaynak | Mevcut kanıt | Eksik | Risk | Doğrulama | Başlangıç durumu |
|---|---|---|---|---|---|---|---|---|
| D111 | 5–10 bina bütçesi | S011 küçük cabin/cache | Editor/S011Authoring.cs | S011 authoring.txt | POI maliyet/rol bütçesi | Gereksiz bina üretimi | Sahne/bütçe eşleşmesi | MISSING |
| D112 | 400×400 m ve iki ölçülmüş yol | 24×26 m resident, iki portal cell | WorldCells/WorldCellManager.cs; Player/FirstPersonMotor.cs | S007 tarihi testler | Gerçek büyük alan ve yürüyüş | Cell drop ownership, navmesh, eski save koordinatları | Motor/capsule ve AI traversal | PARTIALLY_IMPLEMENTED |
| D113 | Tehdit/dinlenme/dönüş | Tek resident encounter ve cell population | AI/WorldPopulationManager.cs; Session/ZombieEncounter.cs | R05 tarihi kanıt | Büyük alanda akış | Kapı yığılması, algı kuralları | Üretim sahnesi encounter ve geri çekilme | PARTIALLY_IMPLEMENTED |
| D114 | Kaynak/görev entegrasyonu | Guaranteed fuse, scrap/fuel/wrench | Loot/LootPopulationService.cs; Objectives/RelayMission.cs | S011 alternatif rota | Yeni POI yerleşimi ve kaynak kontrolü | Tek fuse sahipliği | Clue-first/fuse-first, refill yok | PARTIALLY_IMPLEMENTED |
| D115 | Kesintisiz 30 dakika | Fixture konumlandırmalı S011 driver | Objectives/RelayAcceptance.cs | S011 standalone geçmiş | Yardımsız gerçek build koşusu | Teleport süreyi kanıtlamaz | Burak 30–45 dakika ve kayıt | MISSING |
| D116 | Generation tutarlılığı stres | SaveSession + S010/S011 testleri | Persistence/SaveSession.cs; Persistence/SaveValidation.cs | S011 XML | Yeni sahnede stres + güncel regresyon | Eski checkpoint kaybı | Combat/yara/job/repair/cell quit/load | IMPLEMENTED_NOT_VERIFIED |
| D117 | Checkpoint ölüm toparlanması | S011 checkpoint testi | Session/SessionFlow.cs; Player/PlayerHealth.cs | S011IntegrationTests | Yeni sahne varyantları | Fuse/equipment duplicate | Beş ölüm sınırı | IMPLEMENTED_NOT_VERIFIED |
| D118 | Gün/gece/yağmur performansı | R05 benchmark | WorldTime/WorldClock.cs | R05 tarihi performans | Aynı S012 rota/standalone ölçümü | Editor retail karışıklığı | Frame percentiles, counters, soak | MISSING |
| D119 | Üç gözlenen büyük engel | Başlangıç denetimi | S012 yeni authoring/test | Henüz yeni runtime yok | Tekrar üretim ve güvenli düzeltme | Spekülatif hata üretme | Önce/sonra aynı test | MISSING |
| D120 | G3 kanıt ve ürün kararı | Canonical kapı | P03.md; S012.md | Tarihsel S011 G3 değildir | Kabul paketi ve insan değerlendirmesi | S013 erken READY | Tüm zorunlu kontroller | BLOCKED |
