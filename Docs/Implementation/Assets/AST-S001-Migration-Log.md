# AST-S001 — Uygulama ve doğrulama kaydı

- **AUDIT_COMPLETE:** statik dosya/GUID/shader referansı/bağımlılık taraması; görsel ve runtime kapsamı UNKNOWN/PENDING olarak ayrıldı.
- **MOVE_PLAN_READY:** 72 kaynak/hedef satırı, hepsi korunmuş ve onaysız.
- **SAFE_MOVES_IMPLEMENTED: NO.** Taşınan asset/klasör: **yok**. Silinen: **yok**. Kaynak `.meta` değişikliği: **yok**.
- **OWNER_VALIDATION_PENDING:** Editor raporu, import/compile kontrolü ve gerekli manuel sahne kontrolleri.
- **ACCEPTED: NO.**
- **BLOCKED (fiziksel taşıma):** düşük riskli olduğu kanıtlanmış hazır batch yok. UI Resources bağımlılığı, aktif araç stringleri, özel klasörler ve güncel checkpoint boşlukları korunuyor.

Küçük batch için executable Editor workflow ve salt okunur audit uygulandı; eski `Tools/thirdparty_audit.py` tarihsel manifestini değiştirip yeni importları geçmiş doğrulamaya uydurmadık. Yeni tarayıcı yeni baseline üretir.

Önce/sonra ThirdParty içerik toplamı **9.136.566.941 / 9.136.566.941 bayt**. Gerçek tasarruf **0 bayt**. Yeni rapor/araç disk maliyeti bu kaynak toplamına dahil değil. 20.795 vendor dosyası (meta dahil) SHA-256 tekrar karşılaştırması: **0 değişiklik**. İlk/son GUID-path digest: `544744840fed6319bd25a6333bdfabdd88203fc748f1633fdbe8ffce2a42b490`. Pairing hatası 0, proje Assets kapsamındaki duplicate GUID grubu 0. Bu, runtime object reference veya shader render testi yerine geçmez.

`Evidence/20261023T114820429441Z/post-audit-verification.json`, `checkpoint-comparison.json`, `standalone-csharp-compile.txt` kanıttır. C# aracı kurulu Unity 6000.5.0f1 assembly'lerine karşı standalone Roslyn ile **0 hata** derlendi. Unity Editor process, EditMode/PlayMode test, benchmark veya build çalıştırılmadı. Python syntax kontrolü yapıldı.

Değişiklikler yalnız yeni `Tools/AssetAudit/audit.py`, `Assets/LastSignal/Scripts/Editor/ThirdPartyAssetAudit.cs` ve `.meta`, bu dizindeki AST-S001 rapor/katalog/plan ve ignored Evidence çıktılarıdır. Mevcut gameplay, sahne, materyal, prefab, GraphicsSettings, Package manifest ve vendor kaynaklarına yazılmadı. Unrelated dirty work commitlenmedi. Bu dosyalar tek mantıksal review birimi; commit oluşturulmadı.

Olası sonraki batch geri dönüşü: önce receipt'teki source/destination durumunu inceleyin. Kaynak yok ve hedef varsa ters AssetDatabase.MoveAsset uygulanıp GUID/hash ve scene closure kontrol edilmeli. Bu başarısızsa Editor kapalıyken yalnız batch backup'ının kaynak+metaları uzman incelemesiyle geri yüklenir; çalışan Editor altında shell taşıması yapılmaz. Owner kabulü gelmeden ikinci batch yok.

## Aynı sprint devamı — yeni uygulama

Önceki 72 birim envanter/duplicate/hash taraması yeniden çalıştırılmadı. Yeni manifest dry-run ve seçili-paket backup doğrulaması eklendi; mevcut UI executor manifest onay/risk kapısına bağlandı. Üç authoring sabiti düzeltildi (fern, fir, forest ground); önceki dosyanın tamamı `Evidence/path-fix-20261023T123653607758Z/ForestPrototypeAssets.cs.before` ve before/after hash receipt ile korundu. Metadata, sahne ve vendor dosyaları değiştirilmedi. Yeni C# araçlarının standalone compilation sonucu 0 hata/0 uyarı; Unity derleme/import ve owner testleri NOT_RUN. Kaynak path düzeltmesi yalnız beklenen üç satırla doğrulandı.

Yeni/değişen kesin yollar: `Assets/LastSignal/Scripts/Editor/ThirdPartyMoveManifestAudit.cs` + `.meta`; `Assets/LastSignal/Scripts/Editor/ThirdPartyAssetAudit.cs`; `Assets/LastSignal/Scripts/Editor/FirstTownForest/ForestPrototypeAssets.cs`; `Docs/Implementation/Assets/AST-S001-MovePlan.json`, `AST-S001-Catalog.json` ve güncellenen mevcut raporlar. İlk aşamada oluşturulan `Tools/AssetAudit/audit.py` korunuyor. Fiziksel taşımalar hâlâ yok. SAFE_MOVES_IMPLEMENTED=NO, OWNER_VALIDATION_PENDING, ACCEPTED=NO.
