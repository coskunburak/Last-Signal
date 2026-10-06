# S013 giriş denetimi — 4 Ekim 2026

Run: `20261003T224451-879557Z`. S013 üretimi tamamlanmadı; mevcut aşama D121/D122 karşılaştırma ve D123 teknik hazırlık.

## Gerçek kaynak

Proje `/Users/burakcoskun/Last Signal`; dal `s012-integrated-graybox-slice`; HEAD `f9299ef69bb72abc92c05aff22ba0418d85b6af0`. Başlangıç Git durumu `Evidence/20261003T224451-879557Z/initial-git-status.z` içinde eksiksiz NUL ayrımlı kaydedildi. Binlerce önceden staged taşıma, ekleme ve unstaged değişiklik var. Bunlar S013 katkısı olarak sayılmadı. Commit/push/merge yapılmadı.

Unity 6000.5.0f1; kurulu package cache ve manifest URP 17.5.0. PC_RPAsset aktif, PC_Renderer `m_RenderingMode: 2`; kurulu URP enum'unda bu Forward+. Mobile renderer 0/Forward. Renderer kararı değiştirilmedi. Açık sahne girişte `Assets/LastSignal/Scenes/Production/RelayExpedition.unity`, dirty=false idi. Güncel S012 sahnesi taşıma sonrası `Assets/LastSignal/Scenes/Production/IntegratedGraybox.unity`.

## S012/G3

G3 **BLOCKED**. Yardımsız 30–45 dakikalık gerçek build oturumu, kapat/aç/load ve Burak ürün kararı bulunamadı. Tarihsel fixture sonuçları bunların yerine geçmez. Yeni XML taraması eski rapordaki tam PlayMode PASS'ın en yeni sonuç olmadığını gösterdi:

- EditMode: `20261003T134744-882884Z/edit.xml`, 444/444 PASS.
- PlayMode: `20261003T140852-800755Z/play.xml`, **204/205 PASS, 1 FAIL**. `ZombieBehaviorTests.InitiallyOccludedPlayerIsNotAcquiredAndSearchExpires`: Expected Idle, actual Searching.
- Bunlar bu çalışmadan önceki kanıtlardır; güncel kaynak regresyon onayı sayılmaz. Tam indeks: `Evidence/20261003T224451-879557Z/s012-results-index.json`.
- S012 D112/D113/D115/D116/D117/D119 insan kabulü ve D120 kapısı korunur. S014 READY değildir.

## Gerçek sahne

Salt okunur additive inceleme sonrası S012 sahnesi kapatıldı, kaydedilmedi. Cabin floor (-360,-0.25,-140); shelter root (-345,0,-149), yani root konumu bina tabanı değildir. Spawn (-358.2,0.05,-139). Cabin 13 renderer/10 collider içerir. Radyo, yatak, storage/workbench socketleri ve mevcut RainRoof/otoriteler korunmalı. Resident market, clinic, maintenance, security ve iki hücre kapısı mevcuttur. Tam root/konum/collider dökümü `integrated-scene-audit.txt`.

## Paket durumu

| Paket | Girişte gerçek durum | Teknik bulgu |
|---|---|---|
| A | 8 FBX, 8 prefab; 29 PNG; 3 MAT; eski sürüm arşivi ayrıca | URP/Lit; mevcut LOD; LITE içinde kaya ve gerçek çalı yok |
| B | 154 FBX, 154 prefab; 33 MAT; 35 PNG/TGA | Built-in shaderlar; yalnız değerlendirme kopyaları URP'ye çevrildi |
| C | 3 unitypackage, model olarak import edilmemiş | URP arşivinden 68 model/texture/material/shader/prefab dosyası GUID korunarak ayrı açıldı; scene/settings alınmadı |
| D | ZIP + 16 dış texture, model import edilmemiş | ZIP'ten FBX + 16 TIF ayrı açıldı; 5.558 üçgen, 4 submesh |
| E | cupboard_pack.fbx ve meta | 4 mesh, 72.864 üçgen toplam; en pahalı parça 59.562; texture bağlantısı yok, ürün eşlemesi teyit bekliyor |

Tam yollar/sayımlar `S013_ASSET_REGISTRY.md` ve `package-inventory.json`. Unity denetimi 271 mesh, 351 model/prefab kaydı, 120 texture: taranan meshlerin UV0/normal/tangent kanalları mevcut; UV kalitesi ve texel density bundan kanıtlanmış olmaz. Missing script=0; beş B prefabında toplam 28 boş materyal slotu. Eksik kayıtlar JSON'da dosya bazlı. Texture referansı olmayan beyaz materyal teknik olarak null slot değildir; E bu ikinci sorunu taşır.

## Koruma ve açık kapsam

Kaynak hash manifesti `vendor-before.json`; mevcut vendor baytları değiştirilmedi (`vendor-preservation.json`). Çıkartılan 85 dosya `extraction-manifest.json` ile kaynak arşivine bağlı. Orijinaller silinmedi/taşınmadı. Lisans netleşmeyen içerik yalnız yerel değerlendirme sahnesindedir; hiçbir build ayarına eklenmedi. GitHub metadata denetiminde repo **public** çıktı.

D121–D130 boşluk ve kabul tablosu `S013_GAP_MATRIX.md`. Tam POI kurulumundan önce brief §7.5 uyarınca Burak sanat kararı bekleniyor.
