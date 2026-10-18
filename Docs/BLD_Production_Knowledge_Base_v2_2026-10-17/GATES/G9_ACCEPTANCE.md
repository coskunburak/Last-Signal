# G9 — Alpha kabul ve scope kapatma kabul sözleşmesi

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Bağlayıcı çıkış: G9: alpha playtest, release scope ve teknik devir kararı.

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
- Test Unity Editor açıkken batchmode kilitleniyor mu?
- Focused PASS sonrası full regression FAIL ise false closure yapılır mı?
- Görsel acceptance olmadan performans testinin PASS olması release için yeterli mi?

## Related cards / sprints
- [BLD-S027](../PHASES/P09_alpha-kabul-ve-scope-kapatma/BLD-S027_end-to-end-alpha-qa-ve-regresyon/SPRINT.md): End-to-end alpha QA ve regresyon
- [BLD-S028](../PHASES/P09_alpha-kabul-ve-scope-kapatma/BLD-S028_stabilizasyon-gdd-guncelleme-teslim/SPRINT.md): Stabilizasyon, GDD güncelleme, teslim

## Owner decision form
- Decision: PASS / PARTIAL / BLOCKED / FAIL / NOT_RUN.
- Log source and exact test environment: ___.
- Manual screenshot paths / comparison condition: ___.
- Migration / regression status: ___.
- Open issues and action owner: ___.
- Permission to proceed to next phase: ___ (owner signoff).
