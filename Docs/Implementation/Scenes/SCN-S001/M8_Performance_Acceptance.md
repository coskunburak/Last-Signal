# SCN-S001 M8 — kabul

**OWNER_ACCEPTANCE_PENDING.** Editör görüntüsü gözlemi ve statik/native authoring kontrolleri yapıldı. Oyuncu runtime, test suite, standalone build, Mac/Windows FPS, GPU zamanı, P95/P99 ve bellek ölçümleri NOT_RUN. Önceki prototype ölçümleri yeni Main sonucu değildir.

Owner aynı çözünürlük, kamera konumu/rotasyonu ve rota ile Low/Medium/High/Ultra karşılaştırmalı. Özellikle Global High + Tree Off ile Global High + Tree High koşulları aynı kamera/rota/havada olmalı. FPS artışı veya 60 FPS sözü yok. 346 LOD ağacının varlığı tek başına performans kabulü değildir.

Statik kanıt: FinalNativeValidation.txt, TerrainAndShadowValidation.txt, StaticValidation.json. Görsel kalite: prototip varlıkları entegre, tüm mimari/çit/kaya/çim için gerçekçilik kabulü eksik. İşlev: eski physicsBody eksikliği ve yeni terrain için NavMesh bake kontrolü açık.
