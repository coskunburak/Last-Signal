# SCN-S001 M7 — owner manuel kontrolü

**NOT_RUN / OWNER_ACCEPTANCE_PENDING.** Otomatik Play, test suite, build, regeneration veya NavMesh bake yapılmadı.

1. Unity 6000.5.0f1'de yalnız FirstTownMain_HDRP.unity açın; import/compile bitmesini bekleyin. Console'da yeni Missing Script/Material veya pembe yüzey olmamalı. İki eski Avatar eksik referansı M0'da kayıtlıdır.
2. TerrainCollider ile Terrain aynı izole Terrain_49b9c2ad.asset kullanmalı. PrototypeVisuals katmanları ve materyali; Lighting profilinde usePhysicalHdrpLighting açık olmalı.
3. Önce Editör sahne görünümünde X[-240,-120], Z[-185,-65] alanını inceleyin. Çit/kapı/yol/harvest koridoruna kaya engeli, havada duran taş veya ağaç tabanı olmamalı. Görsel/collision düzeni onaylandıktan sonra ana root `FirstTownCore - 500m resident world` NavMeshSurface Inspector'ından yalnız adayın izole Navigation assetine Bake uygulayın. Eski veri yeni yamaçları kapsamaz; AI yürüyüşünü bake öncesi başarılı saymayın.
4. Manuel Play: spawn (-178.20001,.05,-169), tek oyuncu ve UI; safehouse giriş/çıkış, yol ve orman yürüyüşü, envanter, mevcut zombi davranışı. Kayıt/yüklemede yalnız `scn_s001_main_hdrp_review` kullanın.
5. Ayarlar → Grafik: Low/Medium/High/Ultra seçip **Uygula ve kaydet**. Her seçimde etkin preset/mesafe/LOD değerlerini M4 tablosuyla karşılaştırın. Global High + Ağaç Off: ağaç gölgeleri kapanmalı, bina/kaya gölgeleri kalmalı. Ayarı kaydedip yeniden başlatın; kalmalı. Uygulamadan sayfayı kapatmak seçimi kaydetmemeli.
6. Gündüz, günbatımı, gece ve yağmurda okunabilirlik/pozlama; iç mekân ışıkları, ıslak yol ve kuru orman zemini. Parlak beyaz yaprak, aşırı siyah gece, pembe materyal veya görünür terrain seam varsa konum/ekran görüntüsü kaydedin.
7. Üç ResourceNodeView'ın kimlik/collider/world bağları korunur fakat kaynakta physicsBody=null. Balta düşürme/kesme **PhysicsNotReady** ile hâlâ bloklu. Yeni art aktarımı ağaç fiziği tamamlandı anlamına gelmez; uyumlu notch/collider/fizik authoring ayrı iş kalemi.

Mevcut owner-run testler: Window → General → Test Runner → EditMode, LastSignal.Tests.GraphicsSettingsTests; PlayMode, GraphicsSettingsPlayTests. Dosyalar Scripts/Tests/EditMode/GraphicsSettingsTests.cs ve PlayMode/GraphicsSettingsPlayTests.cs. GraphicsSettingsPlayTests bir boş test sahnesi gerektirir: önce aday sahneyi kaydedip kapatın; test fixture'ını aktif ana dünya üzerinde çalıştırmayın. Otomatik olarak çalıştırılmadı. Paket/assembly ayarları değiştirilmedi.

Gönderilecek sonuçlar: Console logu, preset/bağımsız gölge ekranları, aynı kameradan orman görüntüleri, yürüme/save/load/gece/yağmur PASS/FAIL. Yeniden üretim menülerini bu adayda kullanmayın.
