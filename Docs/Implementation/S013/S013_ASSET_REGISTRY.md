# S013 kaynak ve hak kaydı

Durum: yerel teknik değerlendirme; ticari yayın lisans onayı yok. İnceleme: 4 Ekim 2026.

## A — Hand Painted Nature Kit LITE

- Yayıncı: Silver Cats. [Ürün kaynağı](https://assetstore.unity.com/packages/3d/environments/hand-painted-nature-kit-lite-69220).
- Yerel yol: `Assets/ThirdParty/EnvironmentAsset/Silver_Cats/Hand_Painted_Nature_Kit_LITE`.
- Yerel sürüm: 3.0 (.meta assetOrigin).
- Format/sayı: `{".meta": 75, ".unity": 2, ".asset": 6, ".png": 29, ".mat": 3, ".terrainlayer": 7, ".prefab": 8, ".fbx": 8, ".unitypackage": 1, ".txt": 1}`.
- Edinim makbuzu/hesap kaydı: doğrulanmadı. Yerel paket lisans metni bulunamadı; Silver Cats readme yalnız eski sürümü açıklıyor.
- Kullanım: yalnız S013AssetComparison; üretim/publish onayı bekliyor.

## B — Fantasy landscape

- Yayıncı: Pxltiger. [Ürün kaynağı](https://assetstore.unity.com/packages/3d/environments/fantasy-landscape-103573).
- Yerel yol: `Assets/ThirdParty/EnvironmentAsset/FantasyEnvironments`.
- Yerel sürüm: 2.0 (.meta assetOrigin).
- Format/sayı: `{".meta": 415, ".pdf": 1, ".mat": 33, ".prefab": 154, ".png": 28, ".asset": 2, ".tga": 7, ".fbx": 154, ".unity": 2, ".cs": 1, ".anim": 3, ".controller": 3, ".terrainlayer": 5}`.
- Edinim makbuzu/hesap kaydı: doğrulanmadı. Yerel paket lisans metni bulunamadı; Silver Cats readme yalnız eski sürümü açıklıyor.
- Kullanım: yalnız S013AssetComparison; üretim/publish onayı bekliyor.

## C — Mountain — Stylized Fantasy Environment

- Yayıncı: Holotna. [Ürün kaynağı](https://holotna.itch.io/mountain-stylized-fantasy-environment).
- Yerel yol: `Assets/ThirdParty/EnvironmentAsset/Mountain (Unity 2022.3 1.16f1)`.
- Yerel sürüm: Ürün sürümü belirsiz; arşiv Unity 2022.3.16f1.
- Format/sayı: `{".unitypackage": 3, ".meta": 3}`.
- Edinim makbuzu/hesap kaydı: doğrulanmadı. Yerel paket lisans metni bulunamadı; Silver Cats readme yalnız eski sürümü açıklıyor.
- Kullanım: yalnız S013AssetComparison; üretim/publish onayı bekliyor.

## D — Abandoned Barn Village House Destroyed

- Yayıncı: Flawless Normals. [Ürün kaynağı](https://www.fab.com/listings/3f970523-4bb5-4fdd-8b08-1497a7dda3cc).
- Yerel yol: `Assets/ThirdParty/EnvironmentAsset/barn-village-abandoned-house`.
- Yerel sürüm: Bilinmiyor.
- Format/sayı: `{".meta": 19, ".png": 15, ".jpeg": 1, ".zip": 1}`.
- Edinim makbuzu/hesap kaydı: doğrulanmadı. Yerel paket lisans metni bulunamadı; Silver Cats readme yalnız eski sürümü açıklıyor.
- Kullanım: yalnız S013AssetComparison; üretim/publish onayı bekliyor.

## E — Modular Wooden Storage & Cabinet Pack (aday eşleme)

- Yayıncı: 3dShowdown. [Ürün kaynağı](https://www.fab.com/listings/e20580f0-cd24-4f58-9f15-1c4c20615893).
- Yerel yol: `Assets/ThirdParty/EnvironmentAsset/cupboard_pack.fbx`.
- Yerel sürüm: Bilinmiyor; Burak kaynak eşlemesi bekliyor.
- Format/sayı: `{".fbx": 1}`.
- Edinim makbuzu/hesap kaydı: doğrulanmadı. Yerel paket lisans metni bulunamadı; Silver Cats readme yalnız eski sürümü açıklıyor.
- Kullanım: yalnız S013AssetComparison; üretim/publish onayı bekliyor.

## Uygulanabilir şartlar ve açık kanıt

A ve B ürün sayfaları Standard Unity Asset Store EULA bildiriyor. [Unity EULA §2.2.1–2.2.1.1](https://unity.com/legal/as-terms), assetin özgün içerik içeren ürüne gömülerek kullanımını/düzenlenmesini düzenliyor; ham kaynakların public repoda dağıtım izni varsayılamaz. Paket bazlı ek atıf şartı saptanmadı; edinim ve uygulanabilir seat koşulları ayrıca tutulmalı.

C için itch.io sayfasında açık lisans metni görülmedi. Aynı ürünün [Fab kaydı](https://www.fab.com/listings/60487a72-3f20-4bb2-aa78-247ed1582656) var; indirme kanalı bilinmeden Fab lisansı itch.io dosyasına uygulanmış sayılamaz. D/E Fab sayfalarında alınabilen metindeki License terms alanı “-”; bu lisanssız veya unrestricted demek değildir. Burak'ın Library/edinim kaydındaki lisans türü gerekir.

[Fab Standard License özeti](https://www.fab.com/eula) ticari projeye katılımı, değişikliği ve proje işbirlikçileriyle özel paylaşımı açıklar; assetin tek başına ücretsiz/ücretli yeniden dağıtımını yasaklar, Standard için atıf istemez. Fab'da CC-BY ayrı seçenektir; ürünün gerçekten hangi lisansla edinildiği bu genel özetten çıkmaz. C/D/E için ticari kullanım, atıf ve kaynak dağıtım statüsü **BLOCKED: edinim lisansı kanıtı eksik**.

## Kaynak/türev ayrımı

- A/B: vendor materyalleri değişmedi; URP değerlendirme materyalleri `Assets/LastSignal/Art/S013/Evaluation/Materials/` altında. A URP kopyası, B base texture/UV dönüşümü ve alpha clip normalizasyonu. Final roughness/palette onayı değildir.
- C: `Assets/ThirdParty/EnvironmentAsset/HolotnaMountainURP/` kaynak arşivin değişmeden açılmış alt kümesidir; proje-owned özgün eser değildir. GUID'ler korundu. Orijinal pipeline/settings/scene ve baked lighting alınmadı.
- D: `Assets/ThirdParty/EnvironmentAsset/BarnExtracted/` ZIP'in byte-exact açılımıdır. Orijinal FBX embedded material/texture verisi korunur; değerlendirme materyali kopyalandı.
- E: kaynak FBX dokunulmadı; texture üretildi veya lisansı doğrulandı denemez.
- Primitive zemin/ölçek referansı, kamera düzeni ve Editor kodu proje sahipli. Renderlar yerel inceleme kanıtıdır.
- Referanslar: `Prefabs/Combat/FPSArms.prefab`, MRPoly `World/Assault Rifle (Black).prefab`, `Prefabs/Enemies/Zombie/LS_Zombie_Shirtless_Visual.prefab`. Mevcut proje bağımlılıklarıdır; yeni edinim yapılmadı. Zombie için S004 `Evidence/20260918-B0B/license-source-record.md` kaydı entitlement pending durumunu korur. Hands/MRPoly yayın izni ayrıca doğrulanmalı. Karşılaştırma clone'unda FPSArms demo Environment geometrisi çıkarıldı; kaynak prefab korunur.

## Public repository stratejisi

GitHub get_repo sonucu public. Şimdilik hiçbir commit/push yok. Öneri: lisanslı kaynakları yalnız yetkili ekip erişimli private asset deposunda tutmak; public kod deposunda kurulum/manifest ve yeniden edinim talimatları bırakmak. Git LFS tek başına erişim kontrolü değildir. Türetilmiş materyal/mesh de kaynak lisansına bağlı olabilir. Mevcut staged dosyalar otomatik unstage edilmedi, geçmiş temizlenmedi, repo visibility değiştirilmedi. Sonraki commit öncesi vendor/türev staging listesi ayrıca gözden geçirilmeli.

## Onay sonrası yerel üretim entegrasyonu

`20261004T050720-274690Z`: Kullanıcı B/Fantasy Landscape ana ailesi ve özgün kit önerisini onayladı. Pine_tree1, Birch_tree1, Bush1, Rock1, Grass1 ve ground texture referansları ayrı `Art/S013/Production` URP materyal/prefab türevlerinde kullanılıyor. Bu sanat kararı edinim lisansı veya public source redistribution belgesi yerine geçmez. Diğer aileler final cabin assembly'ye karıştırılmadı; C/D/E ertelendi.997 önceden mevcut vendor dosyası hash bazında değişmedi (`integrity.json`). Yalnız yerel çalışma; push/yayın yok.

### Proje-owned TimberGrain_v2

2026-10-04: yerleşik imagegen ile özgün, mat, nötr timber albedo adayı üretildi. Kaynak tool çıktısı `exec-49275ad0-4fe7-4f64-b1d4-4febdf1792c7.png`; proje kopyası `Art/S013/Production/Textures/TimberGrain_v2.png`. Import max512,mipmap,repeat,aniso4. Ahşap/koyu timber/boyalı timber materyallerinde gerçek sahne renderıyla değerlendirildi (`20261004T202358-047310Z/wood-review`). Eski procedural dokular silinmedi. AI üretim kaynağı vendor paket lisansı gibi sunulmaz; tile edge kabulü sahnede incelenir, salt prompt “seamless” dediği için kanıt sayılmaz.
