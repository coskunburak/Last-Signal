# BLD-S002 — Mimari sözleşme ve risk temeli

**Parent:** `P00 — Ürün sözleşmesi ve güvenli mimari` · **Giriş:** S001. · **Durum:** PROPOSED / TEST_NOT_RUN
> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.


## 1. Sprint mission, player-visible result and scope

- **Mission:** Mimari sözleşme ve risk temeli.
- **Phase purpose:** Serbest yapı inşası kapsam kararını resmileştir; güvenli sistem sınırlarını ve migration stratejisini kilitle.
- **FirstTownCore reference scenario:** S010 storage/craft ve yeni construction service arasındaki command/transaction boundaries.
- **Measurable phase exit:** G0: onaylı kapsam farkı, teknik tasarım, şema ve bağımlılık matrisi.
- **Result character:** Sadece Editor/code işi değil, kullanıcıya görünen ya da domain invariant’ı kanıtlanan çalışır çıktı.

## 2. Existing baseline — do not start from scratch

1. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSite.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
2. `Assets/LastSignal/Scripts/Runtime/Shelter/ShelterSocket.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
3. `Assets/LastSignal/Scripts/Runtime/Persistence/SaveSession.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `Assets/LastSignal/Scripts/Runtime/WorldCells/WorldCellManager.cs` — kaynak bölümünü oku, mevcut uygulama ile bu sprintin hedefini ayır.
4. `git status`, branch, HEAD ve ilgili prefab/scene unsaved state’ini kontrol et.
5. Var olan S010/S013/S023 test kanıtlarını güncel sürüm için otomatik PASS sayma.
6. Vendor asset yerine project-owned prefab/material ve yeni data tanımlarını tercih et.

## 3. Sprint boundary / API ownership

### Girdiler
- Mevcut shelter/recipe/storage domain sahiplikleri
- WorldCellManager readiness ve persistent delta sözleşmesi
- OwnershipTransaction, stable ID ve save migration sınırları

### Değiştirilemez davranışlar
- Aynı kaynağın iki authoritative sahibi olamaz.
- Yeni sistem ShelterSite üç socket validation koşulunu değiştirmez.
- Preview state, save state veya gerçek world mutation üretmez.

### Domain anlaşmaları
- Önerilen ResourceNode, BuildDefinition ve ConstructionService yeni tasarım sınıflarıdır; mevcut repo parçası sayılmaz.
- S010 escrow sözleşmesi uyarlanabilir; kodu kopyalayarak iki ayrı envanter algoritması oluşturulmaz.

## 4. Sekiz günlük kart — sıralı implementation ve handoff

| Kart | Ayrıntılı görev dosyası | Başlıca işlem | Kabul cümlesi |
|---|---|---|---|
| BLD-D009 | [Aç](DAYS/BLD-D009_mevcut-kod-snr.md) | Mevcut kod sınırı | Daha önce çalışan kodun yeniden yazılması engellenir. |
| BLD-D010 | [Aç](DAYS/BLD-D010_domain-sahipligi.md) | Domain sahipliği | Her verinin tek authoritative owner'ı tanımlıdır. |
| BLD-D011 | [Aç](DAYS/BLD-D011_veri-tanmlar.md) | Veri tanımları | Stable ID, revision, prefab ve bağımlılıklar doğrulanabilir. |
| BLD-D012 | [Aç](DAYS/BLD-D012_islem-sralamas.md) | İşlem sıralaması | Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir. |
| BLD-D013 | [Aç](DAYS/BLD-D013_dunya-adresleme.md) | Dünya adresleme | Restore duplicate yapı ve iki hücre sahipliği üretmez. |
| BLD-D014 | [Aç](DAYS/BLD-D014_icerik-import-ilkeleri.md) | İçerik import ilkeleri | Kaynak ve kullanım raporu hazırlanır. |
| BLD-D015 | [Aç](DAYS/BLD-D015_test-matrisi.md) | Test matrisi | Her risk bir hedefli teste eşlenmiştir. |
| BLD-D016 | [Aç](DAYS/BLD-D016_koruma-noktas.md) | Koruma noktası | Kod üretimine girişte kaynak bütünlüğü korunabilir. |

### Kartlar arası entegrasyon sözleşmesi

- **BLD-D009**, sprint entry → BLD-D010: S010 ShelterSite/ShelterProduction, envanter, cell ve SaveSession giriş/çıkışlarını haritala. **Acceptance:** Daha önce çalışan kodun yeniden yazılması engellenir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D010**, BLD-D009 → BLD-D011: Gathering, Crafting, Construction, Shelter ve Persistence sorumluluklarını ayır. **Acceptance:** Her verinin tek authoritative owner'ı tanımlıdır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D011**, BLD-D010 → BLD-D012: ResourceNodeDefinition, RecipeDefinition, BuildDefinition ve StructureState taslağı çiz. **Acceptance:** Stable ID, revision, prefab ve bağımlılıklar doğrulanabilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D012**, BLD-D011 → BLD-D013: Preview→Validate→Reserve/Commit→Spawn→Persist basamaklarını formalize et. **Acceptance:** Kaynak eksilmeden yapı başarısızlığı güvenli yönetilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D013**, BLD-D012 → BLD-D014: World/cell/build-zone ID ve yapı kimliği üretme politikasını tanımla. **Acceptance:** Restore duplicate yapı ve iki hücre sahipliği üretmez. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D014**, BLD-D013 → BLD-D015: Vendor assetlere dokunmama, project-owned variant, lisans/LOD gereksinimi kaydet. **Acceptance:** Kaynak ve kullanım raporu hazırlanır. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D015**, BLD-D014 → BLD-D016: EditMode/PlayMode/save fault injection/visual QA kabul stratejisini kur. **Acceptance:** Her risk bir hedefli teste eşlenmiştir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.
- **BLD-D016**, BLD-D015 → sprint gate: Branch, dirty tree, save fixtures ve migration checkpoint planını kaydet. **Acceptance:** Kod üretimine girişte kaynak bütünlüğü korunabilir. **Evidence:** source diff + focus test/fixture + gerekirse Game View.

## 5. Proposed implementation order and stop conditions

1. **Sprint step 1:** Bu kartın dokunduğu mevcut runtime sahibi ile önerilen domain owner’ı dependency diagram üzerinde ayır.
2. **Sprint step 2:** Public API komutlarını command/query/event olarak sınıflandır; UI’nın domain state yazmasını engelle.
3. **Sprint step 3:** Veri tanımlarında stable ID, revision, asset identity ve migration gereksinimlerini tek tek yaz.
4. **Sprint step 4:** Transaction başlama, commit, rollback ve busy/save guard sırasını örnek akışla belirt.
5. **Sprint step 5:** SaveSession/WorldCellManager ile unloaded world state arasında mutation devrini tasarla.
6. **Sprint step 6:** Girdi doğrulamasını negatif/NaN/overflow/duplicate/lost reference vakalarıyla sertleştir.
7. **Sprint step 7:** Eski S010/S013 path davranışlarının değişmeyeceğini kod sınırı ve regression matrix ile sabitle.
8. **Sprint step 8:** Aynı owner’a sahip iki yeni sistem doğurmadan en dar uygulanabilir klas/sözleşme setini öner.
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
| Domain sahibi | Mevcut shelter/recipe/storage domain sahiplikleri | Owner duplication varsa tasarım BLOCKED |
| Revision ve mutation | Aynı kaynağın iki authoritative sahibi olamaz. | Partial mutation rollback |
| İstisna | Oyuncu build confirm sırasında save talep ettiğinde in-flight transaction nasıl ele alınır? | Explicit reject receipt |
| World/scene | S010 storage/craft ve yeni construction service arasındaki command/transaction boundaries | Source scene korunur |
| Test | Contract unit/integration mapping | Focused root cause & retest |

## 7. Product and QA scenarios

- **S-BLD-S002-EDGE-1:** Oyuncu build confirm sırasında save talep ettiğinde in-flight transaction nasıl ele alınır? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S002-EDGE-2:** Loaded cell kapanırken yeni yapı kaydı hangi otoriteye yazılır? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
- **S-BLD-S002-EDGE-3:** Yapı iki cell sınırına dokunursa tam bir owner seçilmiş midir? İlgili kartın test dosyasında girdi ve beklenen state ayrı kaydedilecek.
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

- **Focused test theme 1:** Contract unit/integration mapping
- **Focused test theme 2:** State ownership and rollback simulations
- **Focused test theme 3:** Old save/no construction section compatibility

## 9. Riskler ve kabul başarısızlığının ele alınması

- **Risk 1:** Oyuncu build confirm sırasında save talep ettiğinde in-flight transaction nasıl ele alınır? Sorumlu alan: `architecture`; kanıt olmadan sessiz workaround yasak.
- **Risk 2:** Loaded cell kapanırken yeni yapı kaydı hangi otoriteye yazılır? Sorumlu alan: `architecture`; kanıt olmadan sessiz workaround yasak.
- **Risk 3:** Yapı iki cell sınırına dokunursa tam bir owner seçilmiş midir? Sorumlu alan: `architecture`; kanıt olmadan sessiz workaround yasak.
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

- **Sprint:** BLD-S002
- **Local branch/HEAD:** ___
- **Date / owner test executor:** ___
- **Changed files:** ___
- **Implemented / not implemented cards:** ___
- **Test commands (verified on local Tools):** ___
- **Test receipts and images:** ___
- **Known open regressions and blocker:** ___
- **Decision NEXT/REWORK/DEFER:** ___

Parent: [Phase P00](../PHASE.md) · Governance: [QA](../../../00_GOVERNANCE/03_MANUAL_TEST_POLICY.md)
