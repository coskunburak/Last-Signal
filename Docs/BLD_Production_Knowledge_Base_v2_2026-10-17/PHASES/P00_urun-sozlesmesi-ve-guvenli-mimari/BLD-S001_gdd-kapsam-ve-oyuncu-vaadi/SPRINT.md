# BLD-S001 — GDD kapsam ve oyuncu vaadi

**Parent:** `P00 — Ürün sözleşmesi ve güvenli mimari` · **Giriş:** Program girişi: GDD/branch audit. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** GDD kapsam ve oyuncu vaadi.
- **Phase purpose:** Serbest yapı inşası kapsam kararını resmileştir; güvenli sistem sınırlarını ve migration stratejisini kilitle.
- **FirstTownCore reference scenario:** FirstTownCore sığınaktan çıkış sonrası çit/barınak yapma, oyuncu beklentisi ve kontrollü zone ayrımı.
- **Measurable phase exit:** G0: onaylı kapsam farkı, teknik tasarım, şema ve bağımlılık matrisi.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Docs/Last_Signal_Docs_v0.2/sources/02_Vizyon_Donguler_ve_Dunya.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Docs/Last_Signal_Docs_v0.2/sources/14_Siginak_Crafting_Progression.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Docs/Last_Signal_Production_Plan_v1.0_TR/management/02_Kapsam_Kararlar_ve_Degisiklik.md` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- GDD v0.2 karar hiyerarşisi ve anti-hedefler
- Oyuncu vaadi: hazırlık → keşif → toparlanma → ilerleme
- Tek geliştiricili ürün kapsamı ve FirstTownCore ilk dilimi

### Değiştirilemez davranışlar
- Mevcut no-free-building kararı kayıtsız silinmez; ADR olarak açıkça değişir.
- Yeni Must eklenirse eşdeğer işin çıkması veya takvimin uzaması kayıtlanır.
- UI veya teknik özellik tek başına oyuncu değeri sayılmaz.

### Domain anlaşmaları
- Karar sahibi ürün tasarımını onaylar; teknik agent onay verilmiş gibi GDD değiştirmez.
- Mevcut GDD içindeki tek-player ve yoğun harita önceliği korunur.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D001 | [Aç](DAYS/BLD-D001_ilk-surum-snr-degisikligi.md) | İlk sürüm sınır değişikliği | Eski tasarım silinmez; değişiklik açık ve onaya hazırdır. |
| BLD-D002 | [Aç](DAYS/BLD-D002_kullanc-hikayeleri.md) | Kullanıcı hikâyeleri | Her hikâyede tetik, geri bildirim, başarısızlık ve ödül tanımlanır. |
| BLD-D003 | [Aç](DAYS/BLD-D003_insa-alan-politikas.md) | İnşa alanı politikası | Başlangıç build zone coğrafyası ve açık istisnalar listelenir. |
| BLD-D004 | [Aç](DAYS/BLD-D004_kapsam-snflamas.md) | Kapsam sınıflaması | 1.0 kapsamı sınırsız voxel, tam yıkım ve co-op ile karışmaz. |
| BLD-D005 | [Aç](DAYS/BLD-D005_survival-entegrasyonu.md) | Survival entegrasyonu | Her MVP öğesinin oyuncuya somut faydası belgelenir. |
| BLD-D006 | [Aç](DAYS/BLD-D006_ilk-demo-senaryosu.md) | İlk demo senaryosu | Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir. |
| BLD-D007 | [Aç](DAYS/BLD-D007_kapsam-butcesi.md) | Kapsam bütçesi | Efor aralıkları garanti değil; erteleme sınırları net. |
| BLD-D008 | [Aç](DAYS/BLD-D008_karar-kaps.md) | Karar kapısı | G0 altında kabul edilmeden release kapsamı güncellendi sayılmaz. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D001**, sprint entry → BLD-D002: LS-DOC-14 soketle sınırlı inşa kararına yeni controlled-building önerisini ADR olarak yaz. **Acceptance:** Eski tasarım silinmez; değişiklik açık ve onaya hazırdır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D002**, BLD-D001 → BLD-D003: Ağaç kesme, odun taşıma, craft defteri, yerleştirme ve üs büyütme için akışlar yaz. **Acceptance:** Her hikâyede tetik, geri bildirim, başarısızlık ve ödül tanımlanır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D003**, BLD-D002 → BLD-D004: Claimed shelter çevresi, fiziksel yollar, POI, nav ve koruma alanlarını tasarla. **Acceptance:** Başlangıç build zone coğrafyası ve açık istisnalar listelenir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D004**, BLD-D003 → BLD-D005: Must/Should/Could/Out listelerini yapılar ve süreçler için sabitle. **Acceptance:** 1.0 kapsamı sınırsız voxel, tam yıkım ve co-op ile karışmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D005**, BLD-D004 → BLD-D006: Yapıların barınma, depolama, risk, gürültü ve seyahat faydasını yaz. **Acceptance:** Her MVP öğesinin oyuncuya somut faydası belgelenir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D006**, BLD-D005 → BLD-D007: Sığınak-orman güvenli yolunda 15–30 dk oynanış senaryosu planla. **Acceptance:** Gidiş, kaynak, üretim, yerleştirme ve dönüş kesintisizdir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D007**, BLD-D006 → BLD-D008: Solo ekip üretim/asset/QA tahmini ve kesme sırasını oluştur. **Acceptance:** Efor aralıkları garanti değil; erteleme sınırları net. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D008**, BLD-D007 → sprint gate: GDD farkı, tasarım kabul ve ADR kimliği ile onay paketi hazırla. **Acceptance:** G0 altında kabul edilmeden release kapsamı güncellendi sayılmaz. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** Var olan GDD kararını satır/sürüm bazında bul; uyumlu olanı ve açık çelişkiyi ayrı tabloya yaz.
2. **Sprint step 2:** Bu kartın oyuncuya verdiği eylem→geri bildirim→sonuç zincirini beş dakikalık bir senaryoda ifade et.
3. **Sprint step 3:** Yeni kararın inşa alanı, oyuncu güvenliği ve single-player kapsamına etkisini kaydet.
4. **Sprint step 4:** Must/Should/Could/Out değişiminin mevcut S010/S023/FirstTownCore planlarıyla bağını kur.
5. **Sprint step 5:** Kapsamı bozabilecek iki alternatif tasarımı kıyasla ve maliyet/gain gerekçesini yaz.
6. **Sprint step 6:** Bağımlılık ve görünür demo düzeyi için başarı/başarısızlık eşiklerini ölçülebilir tanımla.
7. **Sprint step 7:** Bu kartın tamamlanması için gerekli ADR, traceability ve sahibi tanımlarını doldur.
8. **Sprint step 8:** Son tasarımı owner onayından önce “proposed” bırak; başka belgeyi sessizce yeniden yazma.
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
| Domain sahibi | GDD v0.2 karar hiyerarşisi ve anti-hedefler | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Mevcut no-free-building kararı kayıtsız silinmez; ADR olarak açıkça değişir. | Partial mutation rollback |
| İstisna | Oyuncu sıfır materyalle sığınağı geliştiriyor mu? Hayır; gerekli kaynak yolu görünür. | Explicit reject receipt |
| World/scene | FirstTownCore sığınaktan çıkış sonrası çit/barınak yapma, oyuncu beklentisi ve kontrollü zone ayrımı | Source scene korunur |
| Test | GDD contradiction scan | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S001-EDGE-1:** Oyuncu sıfır materyalle sığınağı geliştiriyor mu? Hayır; gerekli kaynak yolu görünür. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S001-EDGE-2:** Oyuncu inşa etmek istemediğinde temel survival rotası kilitleniyor mu? Kilitlenmemeli. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S001-EDGE-3:** İlk sürüme büyük serbest yıkım veya multiplayer sızıyor mu? Kapsam dışı. İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** GDD contradiction scan
- **Focused test theme 2:** MUST→oyuncu değeri ve kabul izi
- **Focused test theme 3:** Sprint→bağımlılık→kaynak bütçesi tutarlılık incelemesi

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Oyuncu sıfır materyalle sığınağı geliştiriyor mu? Hayır; gerekli kaynak yolu görünür. Sorumlu alan: `product`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Oyuncu inşa etmek istemediğinde temel survival rotası kilitleniyor mu? Kilitlenmemeli. Sorumlu alan: `product`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** İlk sürüme büyük serbest yıkım veya multiplayer sızıyor mu? Kapsam dışı. Sorumlu alan: `product`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S001
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P00](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
