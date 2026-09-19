---
doc_id: LS-DOC-22
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# QA stratejisi, acceptance senaryoları ve kanıt standardı

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §26-27. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Testin amacı

Test, yanlış değişiklik riskini azaltır. Satır/coverage sayısını yükseltmek tek başına kalite değildir. Veri kaybı, çoğaltma, ilerleme kilidi, unfair hit ve memory leak önceliklidir. Değişiklik küçük ve geri alınabilir belge düzeltmesiyse yeni oyun testi yazılmaz; gameplay transaction değişiminde anlamlı negatif test gerekir.

**REQ-QA-001:** Çalıştırılmayan test NOT_RUN, erişim engeli BLOCKED, geçen gerçek test PASS, assertion/acceptance hatası FAIL olarak raporlanır. Kaynak kodu incelemek PlayMode PASS sayılmaz.

## 2. Katmanlar

EditMode: saf inventory, status effect, loot stream, pressure decay, schema validation. PlayMode: physics query, collider, input context, Animator adapter, nav links, cell lifecycle. Build acceptance: gerçek UI, renderer, platform, save path ve paket referansları. Soak: repeated load/unload, memory, uzun oturum. İnsan playtest: anlaşılırlık ve eğlence.

Hedefli test suite önce; geniş regression schema/save/core contract değişince veya release gate gerektirince. Her sprintte bütün oyunu yeniden saatlerce test etmek geliştirme hızını gereksiz düşürebilir. Somut risk kalan kapsamı belirler.

## 3. Kritik senaryo kataloğu

| Test ID | Given / When | Then | Katman |
|---|---|---|---|
| T-INV-001 | Tek iteme iki pickup | Bir başarı, tek owner | Edit+Play |
| T-INV-002 | Full bag'e transfer | Kaynak değişmez | Edit |
| T-INV-003 | Dolu bag çıkar | Red, item kaybı yok | Edit+UI |
| T-CMB-001 | Reload her committe kes | Ammo conservation | Edit+Play |
| T-CMB-002 | Beş hitboxlı hedefe swing | Tek izinli hit | Play |
| T-CMB-003 | Namlu duvar içinde | Engel ötesine hasar yok | Play+build |
| T-SURV-001 | 8 saat uyku, fuel 2 saat | Yakıt biter, sonuç işlenir | Edit+Play |
| T-SURV-002 | Son health'te çift hasar | Tek death bag | Edit+Play |
| T-AI-001 | Hedef LOS kaybeder | LastKnownPosition search | Play |
| T-AI-002 | Cell geçişinde group | Population sabit | Edit+Play |
| T-PRS-001 | Duplicate NoiseEvent | Tek pressure katkısı | Edit |
| T-WORLD-001 | Load iptal sonra callback | Eski cell aktif olmaz | Play |
| T-WORLD-002 | Broken door unload/load | Broken korunur | Play+build |
| T-SAVE-001 | Disk full save | Eski generation açılır | Fault integration |
| T-SAVE-002 | Pointer öncesi process kill | Geçerli generation bulunur | Fault integration |
| T-SAVE-003 | Eski schema fixture load | Invariant korunur | Edit+build |
| T-OBJ-001 | Ödül sonrası reload | Tek ödül | Edit+Play |
| T-UX-001 | UI click sonrası kapama | Yanlış ateş yok | Play+build |
| T-SESSION-001 | 10 NewGame/Menu | Listener ve world state temiz | Play |
| T-SLICE-001 | Golden path+quit/load | State ve ilerleme korunur | Build+insan |

## 4. Fixture standardı

Fixture dosyası seed, definitions version, initial state ve beklenen invariants içerir. Bir testin kullandığı item test sırasında runtime catalog'dan rastgele seçilmez. JSON/property fixture'ları stable ID ve units taşır. Physics testleri kontrollü sabit scene geometrisi kullanır; gerçek floating precision toleransı açık yazılır.

**REQ-QA-002:** Test kendisinin assert ettiği sonucu oyun kodunu çağırmadan kopyalayıp üretmemeli. Örneğin transfer testinde beklenen ownership uygulamanın private field'ını değiştirmekle hazırlanmaz; public contract kullanılır.

## 5. Kanıt paketi

Evidence run klasörü önerisi docs/Implementation/Evidence/<work-package>/<run-id>. İçerik: README.md, build metadata, editmode.xml/playmode.xml varsa, editor/build logs, acceptance checklist, screenshot/video, performance summary, known issues. Video tek başına inventory conservation ispatı değildir; log/state diff ile tamamlanır.

Rapor alanları: test ID, requirement ID, status, environment, setup, steps, expected, actual, artifact path, limitation. Sonuç zaman damgası ve commit'e bağlıdır. Eski commit'teki PASS yeni kodda otomatik geçerli olmaz.

## 6. Severity ve triage

S0 crash/data loss/save corruption, S1 ana loop/progression blocker veya kritik duplicate, S2 workaround'lu önemli sorun, S3 cosmetic/minor, S4 improvement. Save corruption nadir diye S3'e indirilmez. Öncelik severity + frequency + exposure ile belirlenir; ticari yayın S0/S1 açıkken kapanmaz.

Bug formatı: başlık, reproducibility, seed/save, last good build, steps, actual/expected, player impact, evidence, candidate subsystem. “Zombi kötü çalışıyor” yerine “kapı kapandıktan sonra 5 sn duvar içinden hasar” yazılır.

## 7. DoD ve değişiklik etkisi

**REQ-QA-003:** Bir sistem DONE olmak için domain davranışı, data validation, kritik negatif akış, görünür feedback, save etkisi ve ilgili build doğrulamasını tamamlar. İlgisiz gate N/A yazılabilir; gerekçe olmalıdır. Render-only decal değişikliğinde save migration zorunlu tutulmaz.

Domain event schema değişirse tüketiciler; item payload değişirse migration; world LOD değişirse nüfus ve memory; UI context değişirse input leakage test edilir. Her PR/task etki alanını belirtir.

## 8. Mevcut durum

Bu dokümantasyon paketinin link/kimlik/format kontrolleri belge QA'sıdır. Unity compile, EditMode, PlayMode, Windows build ve gerçek oynanış bu görevde çalıştırılmamıştır. Bu ayrım tüm handoff'larda korunur.
