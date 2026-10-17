# S023 kullanıcı doğrulama handoff'u

Sonraki kullanıcı turu: streaming/population/archetype 19/19 PASS. Full EditMode 805/806 PASS; tek Arberry testi eski shared-world-prefab beklentisinde başarısız oldu. Mevcut iki elli TPS authoring sözleşmesine uygun mesh/material ve support-socket kontrolüyle güncellendi. Arberry dar retesti `Manual-20261015-124004-focused-edit-tjvo1s1n` koşusunda 1/1 PASS, exitCode=0, source değişikliği yok. Şimdi full EditMode ve ardından full PlayMode kullanıcı tarafından çalıştırılmalı; başarılı focused/authoring gate tekrarına gerek yok. Full PlayMode henüz çalışmadı; önceki “sıradaki adım” notları bu güncellemeden önceki aşamadır.

Güncel kullanıcı sonucu (15 Ekim): native authoring ve tekrar authoring PASS; focused EditMode 20/20 PASS. Cancellation/missing-navigation testi ilk focused PlayMode koşusunda PASS. Roundtrip fixture düzeltmesi sonrası `Manual-20261015-123215-focused-play-mpcrxy2c` dar retest 1/1 PASS, exitCode=0, changed=[]. Sıradaki adım streaming/population/archetype regresyon filtresi, ardından full EditMode ve full PlayMode. Authoring veya başarılı dar testleri tekrar çalıştırmak gerekmiyor. Native çıktılar mevcut; aşağıdaki ilk teslim adımları referanstır.

Durum: **PARTIAL**. Native scene/prefab/nav üretimi ve yukarıda belirtilen dar doğrulamalar PASS; geniş regresyon, build, görsel kabul ve performans ölçümleri kullanıcıya aittir. Aşağıdaki adımlar otomatik zincirlenmez. Hata çıkarsa sonraki gate'e geçmeyin; kanıtı paylaşın.

## İlk adım — sadece native authoring

Unity'deki çalışmalarınızı kaydedip Editor'ü normal şekilde kapatın. Runner ikinci Editor başlatmayı reddeder; lock dosyasını silmez. Aşağıdaki executable yerel dosya olarak bulundu; Unity 6000.5.0f1 kullanılmalı.

Çalışma dizini: `/Users/burakcoskun/Last Signal`.

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/s023-author.py --project "$PWD" --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'
```

Beklenen: yeni S023 sahnesi, iki cell prefabı, iki native NavMesh ve üç LootProfile oluşur. Authoring sırasında S019 content kontrolü, katalog/ownership/placement ve **11** native rota denetlenir. TestRunner, PlayMode veya build başlamaz; Unity'nin script compilation/import aşaması doğal önkoşuldur.

Kanıt: terminal `EVIDENCE=...` satırı, `Docs/Implementation/S023/Evidence/Manual-<tarih>-author-<unique>/` altında `authoring.json`, `unity.log`, `result.json`, `source-preservation.json`, source hashleri, HEAD ve dirty status. Başarı: process exit code 0, `authoring=PASS`, 11 rota, beklenmeyen kaynak değişikliği yok, sekiz native dosya ve metaları mevcut. Bu sadece native authoring başarısıdır.

Hata durumunda terminal çıktısı/exit code ile yukarıdaki dört JSON/log dosyasını gönderin. Tüm Unity logunu sohbet içine yapıştırmak yerine dosya paylaşabilirsiniz. Önce compilation/import/scene/nav hatası sınıflandırılacak ve yalnız ilgili gate yeniden verilecek.

## Sonraki gate'ler — ilk sonuçtan sonra, birer birer

1. Author komutunu ikinci kez çalıştırın: `reused=true`, mevcut asset hash/GUID/ID değişmemeli. Aynı kod yolunun tekrar çalışması destructive rebuild yapmaz. İki run'ın source-before/after kayıtları idempotence kanıtıdır. Sahneyi elle değiştirdiyseniz validator değerlendirsin; script üstüne yazmaz.
2. Native authoring/compilation başarılıysa focused EditMode:

```bash
python3 Tools/s023-verify.py focused-edit --project "$PWD" --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'
```

3. Focused EditMode başarılıysa focused PlayMode:

```bash
python3 Tools/s023-verify.py focused-play --project "$PWD" --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'
```

Her gate yeni `Evidence/Manual-...-<gate>-.../` dizini oluşturur: `tests.xml`, `unity.log`, `process-result.json`, `result.json`, `source-preservation.json`. Başarı: exit 0, tüm seçili testler Passed, en az bir test, her istenen fixture temsil edilmiş, beklenmeyen source mutation yok. Skipped/empty test run PASS değildir. `--filter 'LastSignal.Tests.S023RegionPlayTests.DepotYardResidentRoundTripPreservesDoorLootDropAndEnemy'` gerçek dar retest adıdır; yalnız gerekli düzeltmeden sonra kullanın.

4. Streaming/population regresyonu: aynı runner'ın `focused-play` gate'ine `--filter 'LastSignal.Tests.WorldCellIntegrationTests;LastSignal.Tests.WorldPopulationPlayTests;LastSignal.Tests.S022ArchetypePlayTests'` verilir. Standalone süreç restart aşağıda ayrıca yapılır.
5. Dar testler sonrası `regression-edit`, ardından `regression-play`; aynı `--project/--unity` parametreleri kullanılır. Mevcut legacy evidence koruma helper'ı kullanılır; eski başarısız sonuçlar silinmez.
6. Full regression sonrası `development-build`; gerçek executeMethod `LastSignal.Editor.S023.S023Build.DevelopmentMac`. Runner yalnız bu sahneyi `Builds/S023/<unique-run>/LastSignal.app` içine development macOS build eder. Beklenen `build.txt` Result=Succeeded ve exit 0; runtime henüz NOT_RUN. Windows kabulü ayrı ortam/kanıt gerektirir.
7. Manuel gameplay/process restart, ardından görsel QA ve profiling. Tümünü başlatan toplu komut yok.

## Unity görsel ve oynanış rotası

Authoring sonrası `Assets/LastSignal/Scenes/S023/S023RegionPilot.unity` açın. Kaynak S013Cabin'i değiştirmeyin. Edit Mode'da yeni cell içeriği prefab assetidir; PlayMode'da terminal yakınında yüklenir. Her iki yeni prefabı Prefab Mode'da açarak kalıcı geometriyi de inceleyebilirsiniz; root inactive authoring tercihi runtime loader tarafından aktive edilir.

1. Resident sığınakta başlayın. Hierarchy'de `S023 Maintenance district gate` arayın: ShelterLoop.OutsideAnchor'ın iki metre yanında. Orijinal `Two-cell expedition gate` eski test hücrelerine gider; yeni bölgede yeni tabelayı izleyin. `s023-pilot` slotunu doğrulayın.
2. Yeni terminale yaklaşın; yükleme tamamlanmadan etkileşimin geçişe izin vermediğini gözleyin. Depo entry dünya `(201,.05,10)`; solda return terminali, batıda retreat şeridi, ileride kontrol noktası, sağda depo.
3. Ön kapı dünya `(224.9,0,24)`; kapıyı iki yönde açın/kapatın, oyuncu ve zombie yaklaşımını izleyin. Batı servis açıklığından alternatif giriş/geri çekilme yürüyün. Door collision/obstruction ve nav carving görsel kabulü zorunlu.
4. Scrap dünya `(228,.1,34)`; alın, inventory/drop ile sahipliği değiştirin. Checkpoint cloth `(206,.1,31)` %50 boş olabilir; boş sonuç hata değildir. Depo Lurker'ı öldürün veya hasarlayın; gerçek desteklenen state'i not edin.
5. `LOADING YARD >` terminali dünya `(246,1,52)`. Yükleme sahası entry `(265,.05,10)`; batı dolanma veya açık iç şerit. Gantri `(304,6,43)`, shipment `(304,.1,46)`, Runner `(304,.05,39)`. Güvenli dönüş terminali entry yakınında, depo terminali `(310,1,52)`.
6. Yard→resident→depot yapın. Önceki hücre unloaded olmalı; kapı state'i, tüketilen loot tombstone'u, tek dropped-item ve enemy state korunmalı. Art arda tekrar girerek spawn duplication arayın.
7. Oyunun normal Save kontrolüyle `s023-pilot` kaydedin. Gerçek process'i kapatın, aynı generated scene/build'i yeniden açın, Continue/Load ile yükleyin. Resident'te kaydetme ve depot içindeyken kaydetme senaryolarını ayrı yürütün. Diğer kişisel save slotlarına dokunmayın. World/cell/door/drop/death durumlarını tekrar kaydedin.

Görseller: resident-gate; depot-entry landmark; ön kapı açık/kapalı; içeriden loot okunabilirliği; checkpoint; yard entry/gantri; batı retreat. Her açı için gündüz/gece/yağmur aynı konum/yaw/FOV/resolution. Karanlıkta landmark yalnız renk sayesinde değil siluet/konumla okunmalı. Koridor/lintelde capsule veya agent takılması kabul engelidir.

Pilot SessionFlow üzerindeki **S023Capture** component context menüsü mevcut world clock üzerinden `Visual condition - day clear`, `night clear`, `day rain` uygular. PlayMode'da çalışır; zamanı geri almaz. Karşılaştırma için her koşula yeni disposable session'dan başlayın. Kısayollar: Alt+F12 gündüz, Ctrl+F12 gece, Shift+F12 yağmur; yalnız F12 capture başlatır/erken bitirir. macOS'ta Fn gerekebilir. Normal flashlight kontrolünü kullanın; ayrı sahte ışık üretilmez. Yalnız kullanıcı PlayMode'u başlatır.

## Kapanış koşulu

İlk author sonucu gelmeden sahne/prefab kartları PARTIAL kalır. Kod teslimi olan kartlar için implementation hazırdır; sprint `TECHNICAL_COMPLETE`, `FULLY_VERIFIED`, `PRODUCTION_ACCEPTED` veya `FULL_PASS` değildir. Performans ve gerçek iş maliyeti ölçümleri ayrıca beklenir.
