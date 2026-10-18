# G2 — Crafting çekirdeği ve Craft Defteri kabul sözleşmesi

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Bağlayıcı çıkış: G2: birden fazla malzemeli recipe, craft/iptal, notebook, save testleri.

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
- Üç ayrı item gerekli ama üçüncü eksikse hiçbir girdi tüketilmemeli.
- Station üretim ortasında güç kaybederse ne olur: pause/abort açık kural.
- Output doluyken CompletedWaitingOutput davranışı korunur.

## Related cards / sprints
- [BLD-S005](../PHASES/P02_crafting-cekirdegi-ve-craft-defteri/BLD-S005_recipe-engine-ve-malzeme-islemeyi-genislet/SPRINT.md): Recipe engine ve malzeme işlemeyi genişlet
- [BLD-S006](../PHASES/P02_crafting-cekirdegi-ve-craft-defteri/BLD-S006_craft-defteri-ui-ux/SPRINT.md): Craft Defteri UI/UX

## Owner decision form
- Decision: PASS / PARTIAL / BLOCKED / FAIL / NOT_RUN.
- Log source and exact test environment: ___.
- Manual screenshot paths / comparison condition: ___.
- Migration / regression status: ___.
- Open issues and action owner: ___.
- Permission to proceed to next phase: ___ (owner signoff).
