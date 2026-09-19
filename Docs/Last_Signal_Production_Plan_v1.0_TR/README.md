---
doc_id: LS-PLAN-README
project: Project Last Signal
version: 1.0.0
created: 2026-09-17
language: tr
status: PROPOSED_BASELINE
implementation_status: NOT_VERIFIED
---

# Project Last Signal — ayrıntılı üretim planı

**60 sprint, 20 faz ve 600 günlük iş paketi.** Önce single-player ürün; en son koşullu co-op. Bu paket önceki GDD ve sistem dokümantasyonunu uygulanabilir üretim sırasına dönüştürür. Metin Türkçedir; stable ID ve teknik terimler gerektiğinde İngilizce korunmuştur.

## Önce hangi dosyaları açmalısın?

1. [MASTER_PLAN.md](MASTER_PLAN.md): 20 faz, 60 sprint ve bağımlılıkların tamamı.
2. [Planlama modeli](management/01_Planlama_Modeli_ve_Takvim.md): günlük kapasite, rezervler ve yaklaşık iki yıllık SP takvimi.
3. [S001.md](sprints/S001.md): gerçek proje keşfiyle başlayan ilk 10 günlük iş.
4. [DAY_INDEX.md](DAY_INDEX.md): 600 kart arasında konu/ID bulma.
5. [Günlük çalışma ve Codex handoff](management/03_Gunluk_Calisma_ve_Codex_Handoff.md): uygulama ve doğrulama yöntemi.

## Kapsam ve süre

| Dönem | Fazlar | Sprintler | Günlük kartlar | Takvim varsayımı |
|---|---|---|---|---|
| Single-player | P00–P15 | S001–S048 | D001–D480 | 96 üretim + 8 rezerv = 104 hafta |
| Koşullu co-op | P16–P19 | S049–S060 | D481–D600 | SP sonrasında ayrıca 24 üretim + 4 rezerv = 28 hafta |

Bir sprint iki hafta / 10 iş günü; her günlük kart 4–6 odak saatlik başlangıç plan kutusudur. Haftada 30–40 saat toplam çalışma varsayımıyla kurulmuştur. 600 kart, 600 sprint veya bitmiş iş değildir. Büyük kartlar gerçek üretim hızında bölünür; uzak dönem ayrıntıları taslak olarak yeniden tahminlenir. Gerçek başlangıç tarihi ve haftalık zaman bilinmediğinden takvim tarihleri verilmez.

**Co-op single-player yayınlanıp kararlı hâle gelmeden başlamaz.** G15 PASS ve ayrı co-op GO kararı gerekir. S049 öncesi network SDK, RPC, lobby veya relay yoktur. Eski belgelerdeki post-slice co-op spike seçeneği bu plan için ertelenmiştir.

## Paketin içeriği

- `phases/`: 20 faz dosyası; giriş, sprintler, risk, kapsam kesimi ve çıkış kapısı.
- `sprints/`: 60 sprint dosyası; 600 benzersiz günlük işlem ve kabul ölçütü.
- `management/`: kapasite/takvim, kapsam, Codex çalışma akışı, QA, risk/bütçe, NotebookLM ve kaynak/REQ eşlemesi.
- `templates/`: günlük handoff, sprint review, faz kapısı, karar, bug, Current_State ve kapasite şablonları.
- `DAY_INDEX.md`: günlük kart indeksi.
- `MANIFEST.md`: dosya envanteri ve içerik hashleri.
- `PACKAGE_QA.md`: yalnız belge paketinin doğrulama raporu.

## Kullanım sınırı

Bu bir üretim planı ve tasarım önerisidir; gerçek Unity projesi bu görevde incelenmedi, oyun buildi/testi çalıştırılmadı. Dosyalardaki hedefler IMPLEMENTED veya PASS değildir. Yayın, dış mesaj veya satın alma ayrı somut eylemdir; plan bunlar için otomatik yetki vermez.

Önceki LS-DOC v0.2 sistem sözleşmeleri bu ZIP'te tekrar kopyalanmaz; [kaynak eşlemesi](management/07_Kaynaklar_ve_Izlenebilirlik.md) hangi dosyanın hangi sprintte okunacağını belirtir. NotebookLM'e yükleme ve MCP bağlantısı bu görevde yapılmamıştır; [kullanım rehberi](management/06_NotebookLM_ve_Bilgi_Kullanimi.md) kaynak yönetimini açıklar.

Planın yönetimi: aktif sprinti uygula, gerçek kanıtı kaydet, dört sprintte bir kapasiteyi yeniden hesapla. GDD'deki öneri sayıları uğruna bitirilebilir ürünü büyütme; core kalite ve veri güvenliğini koruyarak kapsamı seç.
