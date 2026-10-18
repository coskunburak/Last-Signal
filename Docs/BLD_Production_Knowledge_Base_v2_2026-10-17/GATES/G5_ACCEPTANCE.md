# G5 — Modüler ev ve yapı mekaniği kabul sözleşmesi

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Bağlayıcı çıkış: G5: küçük modüler barınak inşası, dolaşım, kapı, barınak durumu.

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
- Köşe birleşiminde duvar/kapı/çatı gap oluşuyor mu?
- Bir destek parçası silinirse bağlı elemanların state sözleşmesi ne olur?
- İnşa ile bina içine spawn edilen oyuncu çıkabiliyor mu?

## Related cards / sprints
- [BLD-S011](../PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S011_temel-ve-zemin-yap-seti/SPRINT.md): Temel ve zemin yapı seti
- [BLD-S012](../PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S012_moduler-duvar-ve-snap-graph/SPRINT.md): Modüler duvar ve snap graph
- [BLD-S013](../PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S013_kap-pencere-ve-cat-paketi/SPRINT.md): Kapı, pencere ve çatı paketi
- [BLD-S014](../PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S014_yap-dayanm-baglantlar-insa-guvenligi/SPRINT.md): Yapı dayanımı, bağlantılar, inşa güvenliği
- [BLD-S015](../PHASES/P05_moduler-ev-ve-yap-mekanigi/BLD-S015_kucuk-barnak-oynanabilir-demo/SPRINT.md): Küçük barınak oynanabilir demo

## Owner decision form
- Decision: PASS / PARTIAL / BLOCKED / FAIL / NOT_RUN.
- Log source and exact test environment: ___.
- Manual screenshot paths / comparison condition: ___.
- Migration / regression status: ___.
- Open issues and action owner: ___.
- Permission to proceed to next phase: ___ (owner signoff).
