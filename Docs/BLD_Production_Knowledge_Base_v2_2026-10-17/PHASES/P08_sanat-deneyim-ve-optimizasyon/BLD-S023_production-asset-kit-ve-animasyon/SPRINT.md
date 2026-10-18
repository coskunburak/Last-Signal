# BLD-S023 — Production asset kit ve animasyon

**Parent:** `P08 — Sanat, deneyim ve optimizasyon` · **Giriş:** S015. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Production asset kit ve animasyon.
- **Phase purpose:** Production-quality modeller, animasyon, ses, erişilebilir UI, profiler hedefleri.
- **FirstTownCore reference scenario:** Production quality çit/duvar/tezgâh ve hasat animasyonu asset kıyaslaması.
- **Measurable phase exit:** G8: görsel kabul + performans + erişilebilirlik doğrulaması.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Docs/Implementation/S013/S013_ART_BIBLE.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Docs/Implementation/S013/S013_MODULAR_STANDARD.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Assets/LastSignal/Scripts/Runtime/Inventory/WorldItem.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- S013 Art Bible ve modüler standart
- Approved FirstTownCore stylized realism hedefi
- ThirdParty lisans/URP/import durumu

### Değiştirilemez davranışlar
- Production source asset korunur; project-owned prefab/material varyantları kullanılır.
- İlk kişi kamera ölçeğinde kalite kabulü olmadan final art onayı verilmez.
- Görsel varlık teknik çatışma/collider/LOD gereksinimini karşılamalı.

### Domain anlaşmaları
- Meshy sprinti ayrı ve ertelenmiş; yeni asset zorunlu değil.
- Mevcut free assetlerde ticari lisans kontrolü yapılmadan release onayı yok.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D177 | [Aç](DAYS/BLD-D177_visual-style-sheet.md) | Visual style sheet | Paketler bir arada tutarlı görünür. |
| BLD-D178 | [Aç](DAYS/BLD-D178_asset-mapping.md) | Asset mapping | Eksik asset kanıtsız talep edilmez. |
| BLD-D179 | [Aç](DAYS/BLD-D179_hero-blueprints.md) | Hero blueprints | Primitive-only content final PASS almaz. |
| BLD-D180 | [Aç](DAYS/BLD-D180_lod-collider-policy.md) | LOD/collider policy | Visual ve performance birlikte düşünülür. |
| BLD-D181 | [Aç](DAYS/BLD-D181_harvest-animations.md) | Harvest animations | Mevcut karakter grip onarımları bozulmaz. |
| BLD-D182 | [Aç](DAYS/BLD-D182_build-animations.md) | Build animations | Animasyon süreç durumunu gizlemez. |
| BLD-D183 | [Aç](DAYS/BLD-D183_icons-preview-kit.md) | Icons/preview kit | UI'da art placeholder kalmaz. |
| BLD-D184 | [Aç](DAYS/BLD-D184_art-acceptance.md) | Art acceptance | Referansla gerçek oyun footage yan yana incelenir. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D177**, sprint entry → BLD-D178: Last Signal stylized-realistic palet, ölçü, texel, grunge seviyesi ve silhouette kuralları. **Acceptance:** Paketler bir arada tutarlı görünür. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D178**, BLD-D177 → BLD-D179: Mevcut ThirdParty ağaç/çit/duvar/kapı/tezgâh/araç ve project-owned kaynakları tara. **Acceptance:** Eksik asset kanıtsız talep edilmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D179**, BLD-D178 → BLD-D180: Çit, kapı, temel, duvar, çatı, sandık, masa için yüksek kaliteli prefab varyantları. **Acceptance:** Primitive-only content final PASS almaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D180**, BLD-D179 → BLD-D181: Prefab bazında LOD, collider, shader ve batching kararları. **Acceptance:** Visual ve performance birlikte düşünülür. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D181**, BLD-D180 → BLD-D182: Balta, ağaç vuruşu, wood pickup ve FPS/TPS hands pipeline. **Acceptance:** Mevcut karakter grip onarımları bozulmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D182**, BLD-D181 → BLD-D183: Hammer placement/repair/dismantle kısa feedback animasyonu. **Acceptance:** Animasyon süreç durumunu gizlemez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D183**, BLD-D182 → BLD-D184: Tarif iconları ve ghost outline malzemeleri uyumlu set. **Acceptance:** UI'da art placeholder kalmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D184**, BLD-D183 → sprint gate: Game View sabit kamera gündüz/yağmur/gece kalite karşılaştırması. **Acceptance:** Referansla gerçek oyun footage yan yana incelenir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** S013 stylized-realism palette ve Unity URP shader/material standardına uygun görsel kriter yaz.
2. **Sprint step 2:** Asset envanterinden gerçek prefab/mesh/texture yollarını doğrula; mevcut vendor dosyasını değiştirme.
3. **Sprint step 3:** Hero asset ve repetition çeşitlerini silhouette, UV, normal, scale ve pivot açısından incele.
4. **Sprint step 4:** Viewmodel/FPS oyuncu eli ve aynı anda dünyada görünen modelin tutarlılığını değerlendir.
5. **Sprint step 5:** LOD, collider, shadow, alpha overdraw ve shader variation maliyetlerini test planına bağla.
6. **Sprint step 6:** Rain, day, sunset, dusk, flashlight altında değişen materyal/FX okumasını denetle.
7. **Sprint step 7:** Yerleştirme geçerli/ret materyallerini kontrast ve renk körlüğü açısından kalibre ettir.
8. **Sprint step 8:** Bütün gerçek ekran görüntülerini kullanıcı alır; konsept resimleri Unity acceptance kanıtı sayma.
9. Kartların kaynak/owner dönüşümü ve geriye uyumlu data alanlarının field-level diff’ini kontrol et.
10. Kullanıcı test komutları gerçek local tooling’e göre hazırlanır; implementation sonrası kullanıcıdan evidence gelmeden DONE denmez.

### Definition of Ready

- [ ] Bütün gerçek sınıf/asset yolları yerel çalışma ağacında mevcut veya net blocked.
- [ ] Eski çalışmalardaki önemli dirty değişiklikler korunmuş ve kaynak commit kaydı tutulmuş.
- [ ] Her kartın implementasyon sahibi, çıktısı, bağımlılığı ve kabul metriği belli.
- [ ] Save/scene/asset destructiveness için rollback/checkpoint hazır.
- [ ] User manuel test ownership ve klasör/evidence konvansiyonu doğrulandı.

## 6. Sprint-level integration design

| Etki | Planlanan kontrol | Fail durumundaki karar |
|---|---|---|
| Domain sahibi | S013 Art Bible ve modüler standart | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Production source asset korunur; project-owned prefab/material varyantları kullanılır. | Partial mutation rollback |
| İstisna | Asset çok ayrıntılı ama collider/LOD yoksa kabul edilmeli mi? Hayır. | Explicit reject receipt |
| World/scene | Production quality çit/duvar/tezgâh ve hasat animasyonu asset kıyaslaması | Source scene korunur |
| Test | FPS screenshot before/after matrix | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S023-EDGE-1:** Asset çok ayrıntılı ama collider/LOD yoksa kabul edilmeli mi? Hayır. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S023-EDGE-2:** Yeşil ghost materyali alpha ve z-fighting sorunu yaratıyor mu? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S023-EDGE-3:** Yağmur/günbatımı ile malzemelerin tepkileri tutarlı mı? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-REG-1:** Mevcut S010 shelter inventory/recipe/claim davranışı hatalı değişmiyor.
- **S-REG-2:** Save/load veya scene loading sonucu kayıp/dupe yok.
- **S-REG-3:** Aynı girdi/seed ile deterministik davranış kanıtlanıyor.
- **S-REG-4:** Çatışmasız cancel veya retry sonrası transient obje kalmıyor.

## 8. Sprint verification order — owner run only

1. Agent yalnız stage authoring/test tanımı/kod/prefablarını hazırlar; tests/build çalıştırmaz.
2. Kullanıcı test alanını koruyan ayrı Editor session açar ve hazırlanan authoring komutunu gerekiyorsa çalıştırır.
3. Kullanıcı focused EditMode, ardından focused PlayMode komutlarını çalıştırır.
4. FAIL halinde agent yalnız failure receipt ve gerekli sınıfları okuyup root cause düzeltir.
5. Kullanıcı başarısız testi yeniden çalıştırır; PASS sonrası integration/regression genişletilir.
6. Save-sensitive sprintte legacy fixture, crash/rollback ve cell tests zorunlu.
7. Görsel konu ise fps/day/dusk/rain karşılaştırması kullanıcı tarafından alınır.
8. Gerekli benchmark son kapıda ve gerçek cihaz bilgisiyle çalıştırılır.

- **Focused test theme 1:** FPS screenshot before/after matrix
- **Focused test theme 2:** LOD/material/UV/pivot/collider inspection
- **Focused test theme 3:** Shader/URP/night/weather quality check

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Asset çok ayrıntılı ama collider/LOD yoksa kabul edilmeli mi? Hayır. Sorumlu alan: `art`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Yeşil ghost materyali alpha ve z-fighting sorunu yaratıyor mu? Sorumlu alan: `art`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** Yağmur/günbatımı ile malzemelerin tepkileri tutarlı mı? Sorumlu alan: `art`; kanıt olmadan sessiz workaround yasak.
- **Regression risk:** Yeniden test etmeden eski PASS bu branch için transfer edilemez.
- **Visual risk:** Low-poly/graybox görünüm sadece asset sayısıyla production kabul almaz.
- **Schedule risk:** Her kart ertelenirse neden, gereken girdi ve downstream sprint etkisi kaydedilir.

## 10. Sprint exit gate / closure checklist

- [ ] Bütün sekiz kart için öncesi/sonrası davranış ve changed files kayıtlı.
- [ ] No duplicate item/structure ID ve hiçbir yarım transaction bulunmuyor.
- [ ] Kaynak GDD ve domain sözleşmesiyle çelişki yok veya onaylı ADR mevcut.
- [ ] Owner focused tests PASS; başarısız varsa sprint PARTIAL/BLOCKED.
- [ ] Gereken PlayMode, save migration, navigation ve visual checks tamamlandı.
- [ ] User has signed specific art acceptance where needed.
- [ ] Kritik perf metric baseline’ı ölçüldü veya `NOT_MEASURED` denildi.
- [ ] `../PHASE.md` gate’e doğru kanıt bağlantısı eklendi.

## 11. Handoff receipt

- **Sprint:** BLD-S023
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P08](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
