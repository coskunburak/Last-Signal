# SCN-S001 M3 — malzeme ve terrain

**IMPLEMENTED_OWNER_ACCEPTANCE_PENDING.** Yalnız adayın izole assetleri değişti. Kaynak prototype ve vendor materyalleri aynı.

| Yüzey | Uygulama | Kalan sınır |
|---|---|---|
| Canopy | 6 prototype Spruce/Birch bark/leaf materyali; LOD prefablarından | Gündüz/gece/yağmur owner görüntüsü bekliyor |
| Understory | fern_02_a/d ve prototype Fern HDRP alpha clip, normal/mask | Overdraw/GPU ölçülmedi |
| Zemin | HDRP/TerrainLit; grass, Woodland, soil, rock | Yalnız Woodland normal + linear mask taşıyor |
| Woodland | Prototype diffuse/normal/terrain mask, 3.5 m tile, normal .75 | Tile tekrarı owner incelemesi |
| Terrain parlaklığı | Unity 6000.5 TerrainLayer smoothnessSource=ConstantOnly (2) | Kaynakta DiffuseAlphaChannel (1), .08/.12 değerlerini yok sayıp parlıyordu |
| Layer geçişi | Ağırlıklı karışım; height blend kapalı | Dört layer'ın üçünde height haritası yok; birinin height'ı karışımı bozmamalı |
| Kaya/dal/çim | Prototype Rock/FallenBranch/GrassDetail | Kaya ve çim tasarımı stilize kökenli, son gerçekçilik kabulü yok |
| Yapılar/yol | M1 izole malzemeleri, layout korunur | Tam bina/asfalt sanat yenilemesi bu aşamada yapılmadı |

Arazi: 513 heightmap; en fazla +4.75737 m, 2909 native height örneği değişti (2736 örnek >1 mm). Değişiklik focus dışında 0; compound, patika koridoru, harvest clearance içinde 0. Üç geniş sırt ile dört kayalık küme, tekdüze tüm dünya yükseltmesi yok. Terrain holes ve mevcut detail dağılımı değişmedi. Yeni yüksekliğe göre dekor görsel çocukları oturtuldu. 155 kaya görselinin son taban oturtması ayrıca düzeltildi.

Kanıt: TerrainAndShadowValidation.txt, FinalNativeValidation.txt, PrototypeObjectMapping.tsv. Unity paketindeki TerrainLit_Splatmap.hlsl açıkça smoothness source 1'in diffuse alpha kullandığını, 2'nin constant kullandığını doğruluyor. Editör önizlemesinde yansıma sorunu giderildi; tüm ışık koşulları ve runtime kabulü yapılmadı.
