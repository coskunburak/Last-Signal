---
doc_id: LS-DOC-12
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# World Pressure, gürültü ve karşılaşma yönetimi

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §13-14. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Amaç ve sınır

World Pressure oyuncunun bir bölgeyi kullanışının hafızasıdır. Üç ayrı kanal tutulur: Disturbance (kısa/orta vadeli rahatsızlık), Presence (uzun süreli insan faaliyeti), Depletion (tükenen kaynak). Depletion'ın azalması rahatsızlığın azalmasıyla karıştırılmaz. Oyuncu marketten uzaklaşınca yiyecek geri doğmaz.

**REQ-PRS-001:** Pressure bir spawn emri değildir. Director önce nüfus, yolculuk süresi, visibility, pacing ve performans bütçesini kontrol eder.

## 2. Gürültü sözleşmesi

NoiseEvent: stable event ID, source actor, position, radiusMeters, intensity, category, simulationTick, expiresAtTick. Sprint gibi sürekli kaynaklar bounded cadence ile event üretir. Generator sürekli source kaydıdır; her render frame'de bir gunshot gibi davranmaz. Audio mixer ses düzeyi NoiseEvent'i etkilemez.

Basit hearing modeli: effectiveRadius = baseRadius × environmentTransmission × weatherMask; listener eşiğiyle karşılaştır. Bu fiziksel desibel simülasyonu değildir. Duvar her sesi sıfırlamaz; açık/kapalı portal ve materyal sınıfları başlangıç için yeterlidir. Bütün dünyaya N×M broadcast yerine cell/spatial index'ten yakın listener listesi alınır.

## 3. Kanal güncellemesi

D(t+dt) = clamp(D(t) × 2^(-dtWorld/halfLife) + eventContributions, 0, 100). Önerilen disturbance yarı ömrü 3 world hour; balance deneyidir. Presence ziyaret başına sınırsız artmaz; per-cell zaman penceresi ve generator exposure katkısı kullanılır. Presence decay daha yavaştır. Depletion = depletedOpportunityCount / initialOpportunityCount ×100; initial=0 özel durumunda 0 tutulur.

Cell boundary gunshot iki kez sayılmaz: her event ağırlıkları normalize edilerek etkilenen cell'lere paylaştırılır. Kaynak kimliğiyle dedupe yapılır. Aynı event'in playback edilmesi pressure yazmaz. Kills-only pressure kaldırılabilir deneydir; ölümün yarattığı gerçek noise yeterliyse ayrıca kill cezası çift sayım yaratır.

**REQ-PRS-002:** Denge formülü idempotent event receipt ve bounded aggregation kullanır; save/load sonrasında eski sesler yeniden pressure üretmez.

## 4. Kullanıcıya görünen tehlike

GDD bantları Quiet 0-24, Uneasy 25-49, Hot 50-74, Overrun 75-100. UI için dangerScore başlangıç önerisi 0.7D+0.3P; Depletion ayrıca “kaynaklar azaldı” işaretidir. Böylece tamamen yağmalanmış ama sessiz bölge sonsuza kadar yüksek combat tehlikesinde kalmaz. Bu GDD üç kanal anlatımını uygulama ayrıntısıyla tamamlayan EXPERIMENT kararıdır.

Map danger bilgisi keşif/radio ile güncellenen estimate olabilir; oyuncuya her cell'in kesin AI değerini vermek zorunlu değildir. Tooltip son gözlem zamanını taşır. Uzak ses, kuşların kaçışı, sürü izleri ve radio uyarısı belirsizliği okunur kılar. Uyarılar gerçek sistem durumuna bağlı olmalı; rastgele korku sesi sürekli yalan söylememeli.

## 5. Director pacing

State'ler Calm → Build → Peak → Recovery. Budget, son meaningful threat zamanı, mevcut düşman, oyuncunun sağlık/yük durumu ve objective context girdidir. Director zombi hasarını gizlice artırmaz. Düşük health oyuncuya kesin saldırı veya kesin kurtuluş garantisi de üretmez; encounter sıklığı üzerinden tempo düzenler.

Peak sırasında yeni sürü yaratmak yerine mevcut grubun yaklaşma rotasını değiştirmek yeterli olabilir. Recovery minimum süresi ilk hipotez 90 simulation seconds. Oyuncu tekrar yüksek ses çıkarırsa mevcut fiziksel düşmanlar duyar; pacing cooldown sağırlaşma anlamına gelmez. Cooldown yalnız director'ın yeni fırsat/event başlatmasını sınırlar.

## 6. Örnek senaryo

Oyuncu benzinlikte pistol ateşler. Yakındaki Shambler hearing ile Investigate'e geçer. Disturbance yükselir. Komşu hücredeki 4 kişilik group rotası, uygun bütçe varsa benzinliğe döner ve ETA hesaplanır. Oyuncu ormana çekilince son bilinen noktayı ararlar. Save/load yapılırsa group geri dönüp yeniden 4 kişi doğurmaz. Oyuncu bir gün sonra geldiğinde rahatsızlık azalmış, boşaltılan dolaplar boş kalmış olabilir.

**REQ-PRS-003:** Testte horde'nun geliş kaynağı ve ETA açıklanabilmeli. Görüşte veya oyuncuya haksız tepki süresi bırakan noktada materialize edilmesi red nedenidir.

## 7. Debug ve kabul

Overlay: D/P/depletion, band, incoming group ETA, event age, contribution source, director state, spawn reject reason. Test: tek shot vs 20 FPS/120 FPS; boundary shot; duplicate event; half-life sonrası değer; max pressure clamp; unloaded cell analytical decay; aynı jeneratör disable/enable farming; göz önünde spawn; nufus budget full. Eğlence kriteri: oyuncu ses bedelini anlayıp sonraki sefer planını değiştirebiliyor mu? Anlamıyorsa yalnız sayıyı düşürmek yerine feedback yeniden tasarlanır.
