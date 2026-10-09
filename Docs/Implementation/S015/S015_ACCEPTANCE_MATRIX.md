# S015 — Kabul matrisi

2026-10-07. **S015 = PARTIAL.** Burak'ın focused EditMode kanıtı 14/14 PASS ve focused PlayMode kanıtı 6/6 PASS. Son tam EditMode 524/524 ve Art Editor 10/10 PASS; son iki tam PlayMode koşusu 279/280 FAIL. 17 testlik hareket fixture'ı 16/17 FAIL; hedefli tanı tekrarı, build ve işitsel kabul bekleniyor.

| Kart | Uygulama | Verification | Result |
|---|---|---|---|
| D141 | 4 paket, 1745 WAV, 59 seçili klip/32 cue; curated source/status/priority ve license inventory | Dosya/serialized inceleme yapıldı; işitsel eşleşme NOT_RUN | PARTIAL — Temporary cue ve lisans açıkları görünür |
| D142 | Project-owned Master/Effects/Ambience/Voice/Music; log dB, PlayerPrefs, menu/pause slider+caption toggle | EditMode PASS (mixer/log/persistence mapping); manuel ayar kabulü NOT_RUN | PARTIAL |
| D143 | Player displacement ve zombie locomotion; wood/dirt/grass/gravel authored, metadata/fallback, non-repeat | Focused EditMode + AI noise/mute PlayMode PASS; yüzey işitsel kabulü NOT_RUN | PARTIAL |
| D144 | S013Cabin bounded blend, reverb zone, sınırlı presentation occlusion | NOT_RUN — doorway/yön/duvar arkası işitsel kabul | PARTIAL |
| D145 | Fire/dry/reload/bolt/equip; tek-sefer receiver; gerçek rifle prefab bağlantısı | Focused PlayMode gerçek rifle event/iptal PASS; algısal dinamik aralık NOT_RUN | PARTIAL |
| D146 | State event, windup önceliği, cooldown/category/voice budget, death cleanup | Focused PlayMode gerçek zombi windup/disable PASS; kalabalık işitsel okunurluk NOT_RUN | PARTIAL |
| D147 | Gerçek WorldClock, streaming rain/night/wind/interior; smooth gain ve warning duck | NOT_RUN — loop dikişi ve rain masking | PARTIAL |
| D148 | Exploration/Threat/Relief; 4 sn TTL, 8 sn relief; load/session reset | EditMode state model ve PlayMode session/load lifecycle PASS; müzikal dinleme NOT_RUN | PARTIAL |
| D149 | Mute'dan bağımsız yakın caption+kaba yön; mevcut objective metinleri | Focused PlayMode mute/menzil/caption PASS; insan muted kabulü NOT_RUN | PARTIAL |
| D150 | Gerçek S013Cabin production scene ve prefab entegrasyonu | NOT_RUN — build/headphone/speaker/muted/settings/performance | PARTIAL — MANUAL ACCEPTANCE REQUIRED |

S014 PARTIAL durumu korunur. Final S015 PASS için full regression, başarılı production build ve Burak'ın kulaklık/hoparlör/muted/ayar kabul kanıtları gerekir. Focused EditMode `Evidence/20261007T141921-948030Z-focused-edit/` 14/14 PASS ve PlayMode `Evidence/20261007T142322-844665Z-focused-play/` 6/6 PASS; ikisinde de exitCode=0 ve kaynak korunumu boş. İlk tam regresyon kanıtları `Evidence/20261007T142820-974603Z-full-edit/` ve `Evidence/20261007T142854-642143Z-full-play/`; düzeltme sonrası sonuç henüz yok. Hiçbir test veya gameplay kabulü ajan tarafından yapılmadı.
