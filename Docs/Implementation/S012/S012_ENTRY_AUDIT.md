# S012 giriş denetimi

Durum: çalışma sürüyor; G3 kabulü verilmedi.

Yerel dal `s012-integrated-graybox-slice`, başlangıç HEAD `5bcdaf823affa1111a1076b2006c487c8893e2aa`. Dal adı uygulama kanıtı değildir. Yerel/remote dal envanteri ve son commitler incelendi; S012 authoring, test veya rapor bulunmadı. Kullanıcının optik, MRPoly, animasyon, input fixture ve geçmiş test çıktı değişiklikleri korunur. Başlangıç durum ve SHA256 manifesti: `Evidence/20261002T234046Z/`. LFS filtre yazımı sandbox tarafından engellendiğinden başlangıç diffi kod/Tools/ProjectSettings ile sınırlı; tüm dosyaların bayt hashleri ayrıca kayıtlı.

Unity MCP üzerinden gerçek proje doğrulandı: Unity 6000.5.0f1, macOS, Mac15,7, Apple M3 Pro CPU/GPU, 18432 MB RAM. Editor açık, PlayMode kapalı, başlangıç sahnesi WorldPopulationAcceptance ve dirty=False. İkinci batch Editor açılmayacak.

Canonical S012, P03, MASTER_PLAN, LS-DOC-13/18/21/22, günlük handoff ve QA kuralları okundu. Tasarım önerileri, geçmiş rapor ve güncel yürütme kanıtları ayrı tutuldu. S006 loot raporu, S009/R05 foundation raporu, S010 ve S011 uygulama/authoring/test araçları incelendi.

## Başlangıç mimarisi

- SessionFlow oturum, oyuncu, pause/death, inventory/loot/shelter/clock/cells/population/mission yaşam döngüsünün bileşim noktası.
- FirstPersonMotor gerçek CharacterController hareketi: varsayılan yürüyüş 3.2, sprint 5.5, crouch 1.6 m/s; stamina sprinti sınırlar. Prefab değerleri ve runtime süre ayrıca ölçülmeli.
- PlayerHealth ölüm/iyileşme; PlayerStamina efor; WorldClock/WorldSimulation wetness, hava, uyku ve iyileşme. Su/yiyecek katalog varlığı tüketim/hunger sisteminin çalıştığını kanıtlamaz.
- PlayerInventory/InventoryContainer ve OwnershipTransaction mevcut sahiplik otoriteleri. Mevcut item slotları definition/quantity; yeni instance envanteri yok.
- WeaponController/MeleeAttackResolver ve ZombieController/Perception/Navigation gerçek combat/AI. ZombieEncounter tek resident encounter; SaveSession tam bir resident encounter gerektirir.
- WorldCellManager CellReady öncesi collision+restore+navigation doğrular; mevcut iki cell 64 m koordinat sisteminde portal üzerinden erişilir. Bunlar kesintisiz arazi streaming'i değildir. Nüfus baskısı tanımlı cell'lerde işler; resident alan için ikinci otorite eklenmeyecek.
- ShelterLoop/Storage/Site/Production: 4 başlangıç storage slotu, scrap 2 → rifle ammo 10, 600 dünya saniyesi ve güç; wrench gerekir. Uyku/üretim gerçek clock'tan ilerler.
- RelayProgression tek görev otoritesi; tek nonstack fuse source, wrench, üç saniye repair, exactly-once receipt; kayıp drop recall korunur.
- SaveSession/SaveGame/SaveCodec/SaveValidation tek kayıt zinciri. Ölüm checkpoint/New Game; death bag yok.
- AcceptanceHud/InventoryUI/ShelterStorageUI/RelayMission journal sunum katmanı.

## Tarihsel kanıt çelişkisi

S011 `final/full-editmode.xml`: 409/409 PASS. `final/full-playmode.xml`: 167 PASS, 5 FAIL (rapordaki kapanış iddiasıyla çelişiyor). Aynı run kökündeki daha geç `full-playmode.xml` 172/172 PASS, `full-editmode.xml` 409/409 PASS. Zamanlar ve dosyalar `historical-s011-results.json` içinde. Bunların hiçbiri güncel çalışma ağacının regresyonu sayılmaz.

## Somut başlangıç açığı

RelayExpedition ana zemin 24×26 m, radyo (-15,9), fuse (11,10), relay (8,-6). Ayrı küçük hücreler 400×400 m bütünleşik rotayı oluşturmaz. S010 ve S011 sahneleri regresyon referansı olarak korunacak; yeni S012 sahnesi aynı authority bileşenlerini kullanacak. Yeni yerleşim eski kayıtların pozisyonlarını yanlış yorumlamaması için ayrı worldId ile mevcut SaveSession doğrulamasını kullanmalı; ikinci dosya/şema eklenmez.
