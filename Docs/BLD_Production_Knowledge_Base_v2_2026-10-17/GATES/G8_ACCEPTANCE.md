# G8 — Sanat, deneyim ve optimizasyon kabul sözleşmesi

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Bağlayıcı çıkış: G8: görsel kabul + performans + erişilebilirlik doğrulaması.

### Evidence categories
- [ ] Owner approved product scope or explicit N/A.
- [ ] Git branch/commit and source-preservation report.
- [ ] Focused EditMode results with exact runner/filter.
- [ ] Focused PlayMode + domain edge scenarios.
- [ ] Cross-sprint and existing S010/S023 regression results.
- [ ] Save/load, cancel, duplicate receipt and unloaded-cell result or explicit N/A.
- [ ] FPS/Game View visual comparison and owner approval if relevant.
- [ ] Benchmark CPU/GPU/GC if relevant; otherwise NOT_MEASURED.
- [ ] Open blockers, postponed cards and rollback record.

## Phase-specific acceptance risks
- Asset çok ayrıntılı ama collider/LOD yoksa kabul edilmeli mi? Hayır.
- Yeşil ghost materyali alpha ve z-fighting sorunu yaratıyor mu?
- Yağmur/günbatımı ile malzemelerin tepkileri tutarlı mı?

## Related cards / sprints
- [BLD-S023](../PHASES/P08_sanat-deneyim-ve-optimizasyon/BLD-S023_production-asset-kit-ve-animasyon/SPRINT.md): Production asset kit ve animasyon
- [BLD-S024](../PHASES/P08_sanat-deneyim-ve-optimizasyon/BLD-S024_ses-vfx-ve-kullanc-hissi/SPRINT.md): Ses, VFX ve kullanıcı hissi
- [BLD-S025](../PHASES/P08_sanat-deneyim-ve-optimizasyon/BLD-S025_craft-defteri-ux-ve-erisilebilirlik/SPRINT.md): Craft Defteri UX ve erişilebilirlik
- [BLD-S026](../PHASES/P08_sanat-deneyim-ve-optimizasyon/BLD-S026_cpu-gpu-ve-buyuk-dunya-optimizasyonu/SPRINT.md): CPU/GPU ve büyük dünya optimizasyonu

## Owner decision form
- Decision: PASS / PARTIAL / BLOCKED / FAIL / NOT_RUN.
- Log source and exact test environment: ___.
- Manual screenshot paths / comparison condition: ___.
- Migration / regression status: ___.
- Open issues and action owner: ___.
- Permission to proceed to next phase: ___ (owner signoff).
