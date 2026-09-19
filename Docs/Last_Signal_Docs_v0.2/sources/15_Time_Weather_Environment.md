---
doc_id: LS-DOC-15
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Zaman, hava, çevresel maruziyet ve uzun işlemler

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §8, §18, §22. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Üç saat

| Saat | Kullanım | Pause | Save |
|---|---|---|---|
| Simulation elapsed | Hareket, melee hit window, reload, AI reaction | Durur | Gerekli semantic state |
| World time | Gün/gece, açlık, hava, spoilage, fuel | Durur | Absolute world seconds |
| Monotonic real time | UI, timeout, loading telemetry | İlerler | Gameplay için kullanılmaz |

**REQ-TIME-001:** Her timer'ın zaman tabanı adında veya şemasında belirtilir. duration=10 tek başına yeterli alan adı değildir. World time değişimi double/long çözünürlüğüyle uzun save'lerde precision kaybını sınırlamalıdır.

Default ratio 12 world second / simulation second. Oyuncu difficulty ayarıyla gün süresini değiştirebilir; ayar save profile'da saklanır. Melee hit window ratio ile on iki kat hızlanmaz. Sistem duvar saatini dünya seed'i dışında runtime otoritesi yapmaz.

## 2. Uzun zaman atlatma

AdvanceWorldUntil(target) en yakın önemli event sınırını bulur: effect tick/death threshold, weather transition, fuel empty, craft completion, threat arrival. Bu noktaya kadar analitik veya bounded integration çalışır, event işlenir, yeniden değerlendirilir. Uyku güvenli değilse target'a gitmeden wake sonucu döner.

İlerleme bölümlenir; tek tick'te 8 saat açlık ve kanama uygulanıp “uyandınız ama 5 saat önce öldünüz” çelişkisi yaşanmaz. Büyük time skip için CPU bütçesi ve maksimum adım sayısı vardır; gerekirse loading overlay, input lock ve iptal politikası sunulur.

**REQ-TIME-002:** Time skip sırasında ölüm veya saldırı sınırına gelinirse kalan süre uygulanmaz. Geri dönüşte timer'lar eski hedef zamana göre sahte completion üretmez.

## 3. Hava grafiği

Clear, Overcast, Rain slice çekirdeği; Fog, Storm, ColdSnap genişleme. Transition; hedef, duration, start weather parameters, seed stream state taşır. Clear → şiddetli storm bir frame'de olmaz; gökyüzü, rüzgar ve ses önceden belirti verir. Radyo forecast deterministik geleceğin tam listesi değil tahmin penceresi olabilir; hata oranı oyuncuya anlaşılır tutulur.

Weather simülasyonu rendering'den bağımsız scalar set üretir: ambientTemp, wind, rainIntensity, visibilityMultiplier. URP efektleri bu veriyi okur. Low quality yağmur particles azaltır ama wetness damage'ını gizlice kapatmaz. Quality ayarları tehlike görünebilirliğini korumalıdır.

## 4. Exposure

Roof, indoor volume, wind shelter ve heat volume girdileri local exposure üretir. İçeri girişte clothes wetness kalır, rain accumulation kesilir. Yağmurluk geçirgenlik ve insulation ayrı parametredir: kuru kalmak sıcak kalmanın tek koşulu değildir. Kıyafet çıkarınca wetness eşya instance'ında kalır; giy-çıkar exploit'i yoktur.

Ateş heat radius ve line-of-sight/room policy ile katkı sağlar. Duvar arkasında kapalı odadaki kamp ateşi sınırsız ısı vermez. Fire particle kapanınca gameplay heat kaynağı “görünmüyor” diye farklı kalmamalı; state tek authority'den gelir.

## 5. Light ve gece

Gün/gece curve'i normalized solar time kullanır. Tek directional sun/moon yaklaşımı önce görsel benchmark'ta seçilir. Dynamic local lights shadow budget'a göre sınırlanır; bütün sokak lambaları gölge çizmez. Flashlight beam görüş için gereklidir; accessibility parlaklık ayarı zombinin algı verisini değiştirmez.

Battery charge world veya sim time seçimi açık olmalı. Öneri flashlight pil tüketimi world time; reload/aim sim time. Uyurken el feneri açık kalacaksa tüketimi UI'da anlaşılır; varsayılan sleep start'ta kapatılması bir convenience kararıdır ve kayıtlıdır.

## 6. Save restore

Weather current/target, transition fraction, RNG state, forecast generation ve absolute time kaydedilir. Load'da ışığın birkaç saniyede gün ortasından geceye kayması önlenir; hydrate sırasında doğru ışık kurulup sonra görüntü açılır. Unloaded bölgeler global weather + local climate offset kullanır; her cell bağımsız rastgele hava seçmez.

## 7. Kabul

**REQ-TIME-003:** 60 world minute küçük tick ile AdvanceUntil sonucu belirlenen tolerans içinde eşleşir. Aynı save aynı weather sequence'i verir; UI beklemek günü ilerletmez. Roof boundary, coat exchange, campfire fuel exhaustion, sleep interruption, 100 gün ileri kayıtta precision ve session resume karşılaştırılır. Bu sonuçlar ölçülmeden sistem “deterministic” diye pazarlanmaz; fizik determinismi ayrıca garanti edilmez.
