# SCN-S001 — güncel ana sahne sözleşmesi

**IMPLEMENTED_OWNER_ACCEPTANCE_PENDING.** M1 izolasyonunun üzerine kullanıcının devam talebiyle M3/M4 prototip görsel aktarımı ve sınırlı M5 HDRP kalibrasyonu uygulandı. M1-only raporu `Evidence/M1-M1_Main_World_Architecture.md` arşivindedir; eski “yalnız üç sahne farkı / geometry tamamen aynı” ifadeleri güncel aşama için geçerli değildir.

Aday `Assets/LastSignal/Scenes/Production/FirstTownMain_HDRP.unity`, GUID `904b09b4f3fa41468f0edf5048174688`; tarihi kaynak, Core ve ForestPrototype120 sahneleri korunur. 80 M1 izolasyon assetine 26 prototype prefab/material/TerrainLayer kopyası eklendi. Yeni GUID'ler SceneOwnership.json/PrototypeVisualAssets.json içindedir. Mesh/texture/vendor kaynakları paylaşılır, değişmez. PRODUCTION_OWNED sahiplik kategorisidir; shippingApproved=false.

Aktif terrain 500×500×20 m. Geometri değişikliği X[-240,-120], Z[-185,-65] 120×120 m içinde; bitki görsel değişimi mevcut authored ağaç/çalı yerleşimini de kapsar. Root koordinatları, spawn, kapı/yol/compound ve harvest kimlikleri korunur. Yeni görsel çocuklar ve yamaç dekoru eklenir; eski dekor renderer/LOD/collider bileşenleri kapatılır. Harvest collider/physics referansları korunur. Navigation kopyası artık yükselti ve yeni colliderlar için eski; owner bake ve yürüme kontrolü gerekiyor.

Tek SessionFlow → ortak GraphicsSettingsManager → SessionUi/GraphicsSettingsPresenter. Yeni settings/weather/gameplay otoritesi eklenmedi. Save slot `scn_s001_main_hdrp_review`. BuildProfiles/boot değiştirilmedi.

Ortak runtime koduna açık istisna: TownLightingProfile, TownAtmosphere, RenderPipelineAdapter fiziksel HDRP opt-in desteği. Yeni bayrak yalnız adayın izole Lighting profilinde açık; eski profiller false davranışını korur. Before/after hash ve tam önceki dosyalar LightingCodeChanges.json/Evidence/PhysicalLightingBefore içindedir. Diğer korunan dosyalar orijinal manifest ile doğrulanır.

Rollback: önce owner'ın sonraki değişikliklerini receipt ile karşılaştırın. M1 aday checkpoint'i PromotionCheckpoint.json'daki dizindedir. Kod geri dönüşü gerekiyorsa yalnız üç kaydın before-image'larını, sonraki çalışma ile diff alarak geri alın. Blanket git reset, vendor restore veya eski sahneleri overwrite etmeyin. Kaynak sahneyi açmak için build/boot değişikliği gerekmez.

Güncel detaylar M3–M8 belgelerinde. Salt okunur `python3 Tools/SceneReconstruction/verify_candidate.py` Unity başlatmaz. Hashler yalnız kayıtlı snapshot'ı doğrular; owner görsel/oynanış/FPS kabulünün yerine geçmez.
