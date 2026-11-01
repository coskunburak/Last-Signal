# WLD-G0/G1 — yeni dosyalar

Mevcut tracked kaynak dosyaları değiştirilmedi. Stage/terrain/prefab/material asseti oluşturulmadı. Yeni dosyalar ve Unity metaları:

- `Assets/LastSignal/Data/WorldReconstruction/WLD-S001/Plan.json`
- `Assets/LastSignal/Data/WorldReconstruction/WLD-S001/Plan.json.meta`
- `Assets/LastSignal/Data/WorldReconstruction/WLD-S001.meta`
- `Assets/LastSignal/Data/WorldReconstruction.meta`
- `Assets/LastSignal/Scripts/Editor/WorldReconstruction/NaturalTerrainPlan.cs`
- `Assets/LastSignal/Scripts/Editor/WorldReconstruction/NaturalTerrainPlan.cs.meta`
- `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldReconstructionAudit.cs`
- `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldReconstructionAudit.cs.meta`
- `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldReconstructionProfile.cs`
- `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldReconstructionProfile.cs.meta`
- `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldReconstructionWindow.cs`
- `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldReconstructionWindow.cs.meta`
- `Assets/LastSignal/Scripts/Editor/WorldReconstruction.meta`
- `Docs/Implementation/World/WLD-S001/CHANGED_FILES.md`
- `Docs/Implementation/World/WLD-S001/CURRENT_STATUS.md`
- `Docs/Implementation/World/WLD-S001/G0_Source_Audit.md`
- `Docs/Implementation/World/WLD-S001/G1_World_Design_Contract.md`
- `Docs/Implementation/World/WLD-S001/ProtectedObjectInventory.json`
- `Docs/Implementation/World/WLD-S001/SourceReceipt.json`
- `Docs/Implementation/World/WLD-S001/SourceScenes.json`
- `Docs/Implementation/World/WLD-S001/StaticValidation.json`
- `Tools/WorldReconstruction/verify_foundation.py`

24 Ekim owner kanıtı değerlendirmesi: yukarıdaki verifier, CURRENT_STATUS, G1 sözleşmesi, StaticValidation ve bu liste güncellendi. Yeni kanıt: `Docs/Implementation/World/WLD-S001/Evidence/G1_OwnerPreview_20261024.json`. Unity C# / plan / sahne / terrain / runtime değişikliği yapılmadı. Verifier artık gönderilen raporu kaynak/plan hashlerine ve önceki envantere bağlayıp `OWNER_EVIDENCE_MATCHED` olarak ayrı raporlar; tasarım kabulünü otomatik vermez.

24 Ekim mimari onay sonrası G2 uygulaması:

- Yeni `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldStageAuthoring.cs` ve `.meta`.
- Yeni `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldStageIsolation.cs` ve `.meta`.
- Yeni `Docs/Implementation/World/WLD-S001/G2_Stage_Ownership.md`.
- Audit ve Window G2 açıklamaları; verifier, CURRENT_STATUS, G1 sözleşmesi, StaticValidation ve bu liste güncellendi.
- Plan.json, mevcut scene/terrain/runtime/vendor dosyaları değişmedi. G2 scene-generation komutu çalıştırılmadı.

24 Ekim owner G2 başarısızlık düzeltmesi:

- `WorldStageIsolation.cs`: explicit TerrainData layer/detail/tree referans remap ve detaylı dependency-edge tanısı.
- `WorldStageAuthoring.cs`: rollback import batch ve başarısız Create sonrasında açıklayıcı Validate kontrolü.
- `verify_foundation.py`: başarısız/geri alınmış owner çalıştırmasını NOT_RUN'dan ayırır.
- CURRENT_STATUS, G2 sözleşmesi, StaticValidation ve bu liste güncellendi. Owner Console metni ve salt okunur tanı sonucu Evidence altında kaydedildi; ilk başarısız journal değiştirilmedi.
- Kaynak scene/terrain/runtime/vendor değişmedi; düzeltilmiş G2 komutu agent tarafından çalıştırılmadı.

24 Ekim G2 kabulü / G3 araç teslimi:

- Yeni `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldTerrainSampling.cs` ve `.meta`.
- Yeni `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldTerrainAuthoring.cs` ve `.meta`.
- Yeni `Assets/LastSignal/Scripts/Runtime/Forest/WorldTerrainNeighbors.cs` ve `.meta` (yalnız G3 terrain link component; asset üretmez).
- Audit kabul edilmiş G2 baseline'ını okuyabilir; WorldStageAuthoring'in mevcut guard/snapshot yardımcıları aynı Editor assembly'si içinde tekrar kullanılır.
- Yeni `G2_Acceptance.json`, `G3_Terrain_Generation.md`; CURRENT_STATUS, G2 sözleşmesi, StaticValidation ve verifier güncellendi.
- Owner'ın oluşturduğu G2 scene/602 asset vardır; bu teslimde değiştirilmedi. G3 scene/terrain üretimi agent tarafından çalıştırılmadı.

24 Ekim G3 owner Play navigasyon hatası:

- Yeni `Assets/LastSignal/Scripts/Editor/WorldReconstruction/WorldNavigationAuthoring.cs` ve `.meta`: owner-run izole navigasyon bake +13 spawn kontrolü.
- Yeni `G4_Navigation_Foundation.md` ve `Evidence/G3_OwnerPlay_NavMissing_20261024.txt`; CURRENT_STATUS/G3 notları/verifier/StaticValidation güncellendi.
- Owner'ın oluşturduğu G3 scene ve iki terrain korunur. Yeni NavigationStage/nav asset agent tarafından oluşturulmadı. Runtime zombie davranışı, spawn pozisyonları, kaynak prefablar ve vendor içerik değişmedi.
