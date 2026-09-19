---
doc_id: LS-DOC-20
project: Project Last Signal
version: 0.2.0
date: 2026-09-17
language: tr
status: design_specification
implementation_status: NOT_VERIFIED
source_gdd: v0.1
---

# Performans, platform doğrulama, build ve işletim

> Proje: **Project: Last Signal** (çalışma adı). Bu dosya tasarım ve uygulama sözleşmesidir; çalışan kod veya geçmiş test başarısı kanıtı değildir. GDD kaynağı: §4, §21, §26. Kullanıcının kesin talebi: önce single-player, proje büyürse sonradan co-op. Diğer yeni ayrıntılar v0.2 tasarım önerisidir; durum ayrımı için [karar yönetimi](01_Kararlar_ve_Kapsam.md).

## 1. Donanım gerçekliği

Geliştirme makinesi M3 Pro; RAM kapasitesi, GPU core sayısı ve test resolution bilinmiyor. Windows makinedeki 3060/16 GB bağlamı ayrı cihaz referansıdır, oyunun minimum gereksinimi değildir. Hedef donanım profili ilk ölçümde tam yazılır: OS, CPU, GPU, RAM/VRAM, SSD, display resolution, quality preset, build commit.

**REQ-PERF-001:** Performans raporu Editor FPS'ini Windows retail performansı diye sunamaz. Development build profiling ile release candidate ölçümleri ayrı kaydedilir; deep profiling overhead belirtilir.

## 2. Bütçe hipotezleri

1080p 60 FPS hedefte frame 16.67 ms; başlangıç acceptance p95 ≤16.67 ms, p99 ≤25 ms, 50 ms üstü frame sayısı ayrıca raporlanır. Bu hedefler donanım profili onaylanana kadar geçicidir. CPU ve GPU paralel çalışır; ms değerlerini körce toplamak doğru frame modeli değildir.

| Alan | İlk profil bütçesi | Aksiyon |
|---|---|---|
| AI decisions+perception | CPU ≤2 ms ortalama | Tick spreading, spatial query |
| Physics/player/combat | CPU ≤2 ms | Collider/filter/query denetimi |
| Streaming main-thread work | Normal frame ≤1.5 ms | Activation spread |
| UI | CPU ≤0.8 ms | Event-driven refresh |
| Rendering | GPU ≤14 ms target | Shadow, overdraw, resolution |
| Warmed gameplay allocations | Kritik ticklerde 0 B hedef | Allocating API/capture audit |
| Save snapshot | Main thread p95 ≤4 ms hedef | Chunk/copy-on-write değerlendirme |
| Save toplam | Küçük slice ≤2 gerçek s | Async serialize/disk |

Memory absolute gate cihaz RAM'ine bağlı. İlk geçici küçük slice process target 4 GB; toplam makine memory pressure/swap da izlenir. Daha büyük içerikte bu değer yeniden ölçülür. Belleğin ısınma sonrası aynı rota döngülerinde sürekli artması gate fail'dir.

## 3. Ölçüm senaryoları

P0 boş benchmark; P1 sığınak; P2 yoğun POI 20 yakın agent; P3 yağmur+gece+flashlight; P4 sprintle cell sınırı; P5 gunshot migration; P6 save during inventory/world activity; P7 2 saat soak. Aynı seed ve rota kullanılır, warmup ve measurement ayrı tutulur. Minimum beş tekrar; median/p95 ve aykırı spike nedeni raporlanır.

**REQ-PERF-002:** Optimizasyon kararı ölçülmüş darboğaza dayanır. Draw call azalması gameplay correctness testlerini veya save state'i bozamaz. Yakındaki düşmanı görünmez yapmak performans düzeltmesi değildir.

## 4. Grafik kalite ölçekleri

Low/Medium/High profilleri shadows, foliage density, texture budget, particles ve view distance değiştirir. Navigation, loot, düşman attack range ve kritik collision sabittir. View distance azaltılırken gameplay threat telegraph korunur. Forward+ vs Forward ölçümü aynı scene, ışık sayısı ve platformda yapılır; GDD'deki seçime rağmen otomatik üstünlük varsayılmaz.

URP'nin her platform/sürümde aynı volumetric, decal veya screen-space özelliğe sahip olduğu varsayılmaz. Kullanılan URP paketinin renderer feature desteği resmî paketten ve gerçek buildden doğrulanır. Shader compile, pink material ve build-only stripping riskleri test edilir.

## 5. Repository ve build operasyonu

Assets, Packages, ProjectSettings ve .meta dosyaları version control'de. Library/Temp/Obj ve üretilmiş build cache repo dışında. Büyük binary assetler için Git LFS değerlendirilir; pointer dosyası asset sanılıp build alınmaz. Package manifest ve lock dosyaları birlikte tutulur. Otomatik package upgrade yapılmaz.

Build metadata: semantic version, commit, dirty-tree flag, content catalog hash, save schema, engine/paket sürümleri. Clean checkout'tan build almak release gate'dir. Mac üzerinde script compile başarı, Windows native plugin veya shader validation yerine geçmez. Windows native test makinesi/CI erişimi yoksa sonuç BLOCKED yazılır; PASS denmez.

## 6. CI sırası

Data validation → compilation → ilgili EditMode → ilgili PlayMode → build → smoke acceptance. Her küçük metin değişikliğinde tam 4 saat soak gerekmeyebilir; gate risk temellidir. Save/schema/world streaming değişimi geniş regression gerektirir. Test çıktısı, log, build identity ve profiler capture aynı evidence run'a bağlanır.

Unity batch komutları bu pakette kör sabitlenmez: Editor binary path, projectPath, lisans ve kullanılan test runner sürümü repo keşfinde doğrulanır. Var olan build method yoksa -executeMethod adına hayali metot yazılmaz. Shell komutu önce mevcut script'lerden bulunur, yeni otomasyon görevi ayrıca uygulanır.

## 7. Log ve crash

Structured log: timestamp, build, sessionId, subsystem, eventCode, entityId, severity. Frame başına aynı warning spam yok; rate limiting. Son önemli gameplay transactionların bounded ring buffer'ı save/dupe hatalarını çözmeye yardımcı olabilir. Kullanıcı kimlik bilgileri ve developer secret'ları log'a girmez.

**REQ-PERF-003:** Memory leak, GC spike veya streaming hitch raporu tekrar üretim yolu ve capture taşır. “Optimize edildi” ifadesi öncesi/sonrası aynı koşullu sayı olmadan kullanılmaz.

## 8. Release geri dönüş

Candidate build değişmez artifact olarak tutulur; yayın sonrası hotfix ayrı version. Save migration geriye dönüşü bozuyorsa eski build'e rollback ile oyuncu kaydının açılacağı varsayılmaz. Content ve executable birlikte uyumluluk kontrolü yapar. Steam yayını, mağaza varlıkları ve satış tarihi bu dokümanın hazırlanmasıyla yapılmış değildir.
