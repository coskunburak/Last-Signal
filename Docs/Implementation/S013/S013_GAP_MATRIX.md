# D121–D130 uygulama ve kabul matrisi

Güncel üretim run: `20261004T050720-274690Z`. Ana aile B/Fantasy Landscape ve özgün kırsal kit kullanıcı tarafından onaylandı. Bu onay final POI görsel kabulü değildir. Status değerleri kartın tüm kabulünü gösterir; uygulama miktarını PASS diye sunmaz.

| Kart | Somut uygulama / kanıt | Kabul | Kalan |
|---|---|---|---|
| D121 Art Bible | v0.2, ana aile kararı, gerçek A–E karşılaştırması, kabul/red örnekleri | NOT_RUN | Bitmiş POI üç hava koşuluyla son görsel kabul |
| D122 lisans/registry | Beş paket dosya ve kaynak audit'i; seçili B türevleri ayrı;997 vendor dosyası değişmedi | BLOCKED | Edinim/yeniden dağıtım belgeleri, E ürün eşlemesi |
| D123 import standardı | Ayrı URP türevler, validator; mesh/material/normal/UV/tangent/collider ve kimlik testleri10/10 | NOT_RUN | Build dependency/material incelemesi ve tüm mesafelerde bitki kabulü; distance cull var, azaltılmış LOD yok |
| D124 modüler kit | Duvar1/2m, iç/dış köşe, kapı/pencere açıklığı, floor, roof, door; snap testi; gerçek motor standing/crouch ve canlı agent geçişi PASS | NOT_RUN | Gece ışık sızıntısı ve standalone birleşim incelemesi |
| D125 materyaller | Proje-owned timber/plaster/metal/rust/stone/glass/cloth; B doğa URP; editor r3 incelendi | NOT_RUN | Gerçek build gece/yağmur/flashlight yakın incelemesi; palette son kabul |
| D126 ışık/render | Forward+ korundu; ACES, SMAA, sıcak iç ışık; atlas taşması düzeltildi; kontrollü quality benchmark kodu | BLOCKED | Unity build modalı/Mac kilidi; same-hardware kalite/gölge ölçümü henüz yok |
| D127 iç mekân | Dolap, radyo masası, yatak, raf, soba, tezgâh, joist, ceiling; mevcut etkileşim/save testleri PASS | NOT_RUN | Build FP silah/eller/crouch/ışık okunurluğu ve final sanat kabulü |
| D128 dış mekân | Tek doğa ailesi, temel kesitli zemin, giriş yolu, sundurma, ağaç/kaya colliderları, bağımsız yeni NavMesh; mevcut rotalar PASS | NOT_RUN | Standalone canlı tehdit görünürlüğü, foliage maliyeti, uzak siluet/zemin geçişi refinement |
| D129 gün/gece/yağmur | Altı sabit kamera ve gerçek saat/hava kullanan dev capture kodu; gündüz editor r1–r3 var | BLOCKED | Gerçek standalone üç koşul henüz çekilmedi; final görsel kabul yok |
| D130 maliyet | Üretim başlangıcı ve araç zamanları kayıtlı;213 dosyalık source manifest | NOT_RUN | Tamamlanmış POI/fokus emek süresi yok; future POI maliyeti güvenilir biçimde çıkarılamaz |

## Ortak sonuçlar

- Yeni Art.EditMode10/10 PASS: `Evidence/20261004T050720-274690Z/art-edit.xml`.
- Yeni S013 PlayMode9/9 PASS: `../S012/Evidence/20261004T053251-160186Z/play.xml`. Önceki8/8 run ayrıca korunuyor.
- Full EditMode 444/444 PASS: `../S012/Evidence/20261004T203844-622411Z/edit.xml`. Full PlayMode 214/214 PASS: `../S012/Evidence/20261004T204917-200142Z/play.xml`. Önceki 204/205 zombi arızası bu turda tekrarlanmadı; tek başarılı koşu flakiness kök nedenini kanıtlamaz.
- macOS Development build `Builds/S013/20261004T203200Z-production`: Succeeded, 0 errors,347 warnings. Bu build son HUD/performance düzenlemelerinden eskidir; güncel final build NOT_RUN.
- Standalone teknik kabul eski build ile PASS: `Evidence/20261004T202358-047310Z/standalone-r3/standalone.txt`, 0 error. Güncel final build kabulü ve karşılaştırmalı performans NOT_RUN; yeni build gerekli.
- Windows build/hardware performansı: BLOCKED.
- İnsan30–45dk G3 koşusu ve owner ürün kararı: BLOCKED, otomatik motor fixture'ları yerine geçmez.

S013 kapanmadı. S014 girişi BLOCKED. P04 kapanmadı. Mevcut kullanıcı değişiklikleri korunuyor; commit/push/merge yok.
