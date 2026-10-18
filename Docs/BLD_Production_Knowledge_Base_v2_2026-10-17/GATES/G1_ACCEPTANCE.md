# G1 — Kaynak toplama ve ağaç kesme kabul sözleşmesi

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Bağlayıcı çıkış: G1: ağaç kesme/kaynak toplama + mülkiyet, yükleme ve gürültü doğrulaması.

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
- Ağaç vuruşu sırasında balta kırılırsa ilerleme ve malzeme ne olur?
- Envanter doluyken kaynaklar sahnede güvenli ve kalıcı kalır mı?
- Aynı ağaca tek frame içinde iki geçerli hit gelirse iki ödül oluşur mu?

## Related cards / sprints
- [BLD-S003](../PHASES/P01_kaynak-toplama-ve-agac-kesme/BLD-S003_harvestable-resource-nodes/SPRINT.md): Harvestable resource nodes
- [BLD-S004](../PHASES/P01_kaynak-toplama-ve-agac-kesme/BLD-S004_agac-kesme-oynans-ve-geri-bildirim/SPRINT.md): Ağaç kesme oynanış ve geri bildirim

## Owner decision form
- Decision: PASS / PARTIAL / BLOCKED / FAIL / NOT_RUN.
- Log source and exact test environment: ___.
- Manual screenshot paths / comparison condition: ___.
- Migration / regression status: ___.
- Open issues and action owner: ___.
- Permission to proceed to next phase: ___ (owner signoff).
