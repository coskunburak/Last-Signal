# G7 — Dünya ekonomisi ve güvenilir kayıt kabul sözleşmesi

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Bağlayıcı çıkış: G7: regresyon, migration, exploit ve uzun oturum testleri.

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
- Build commit sırasında exception ve disk write kesintisi olursa ikincil yapı kalır mı?
- Delete→save→load sonrası tombstone sayesinde yapı geri gelir mi?
- Cell sınırında build iki kere serialize edilir mi?

## Related cards / sprints
- [BLD-S020](../PHASES/P07_dunya-ekonomisi-ve-guvenilir-kayt/BLD-S020_kaynak-yenilenmesi-ve-ekoloji/SPRINT.md): Kaynak yenilenmesi ve ekoloji
- [BLD-S021](../PHASES/P07_dunya-ekonomisi-ve-guvenilir-kayt/BLD-S021_world-streaming-save-ve-migration/SPRINT.md): World streaming, save ve migration
- [BLD-S022](../PHASES/P07_dunya-ekonomisi-ve-guvenilir-kayt/BLD-S022_economy-pressure-balance-ve-g7/SPRINT.md): Economy/pressure balance ve G7

## Owner decision form
- Decision: PASS / PARTIAL / BLOCKED / FAIL / NOT_RUN.
- Log source and exact test environment: ___.
- Manual screenshot paths / comparison condition: ___.
- Migration / regression status: ___.
- Open issues and action owner: ___.
- Permission to proceed to next phase: ___ (owner signoff).
