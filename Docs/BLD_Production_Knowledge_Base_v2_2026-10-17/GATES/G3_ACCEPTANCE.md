# G3 — Yapı yerleştirme çekirdeği kabul sözleşmesi

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Bağlayıcı çıkış: G3: geçerli/bozuk placement, kaynak korunumu ve save/load roundtrip.

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
- Çit kapıyı tamamen bloke ediyorsa ret kodu ayrı olmalı.
- Eğim sınırında preview geçerli ama confirm geçersiz olmamalı; son anda revalidate.
- Yerleştirme ortasında world cell unload olursa ghost kapanmalı.

## Related cards / sprints
- [BLD-S007](../PHASES/P03_yap-yerlestirme-cekirdegi/BLD-S007_placement-validation-engine/SPRINT.md): Placement validation engine
- [BLD-S008](../PHASES/P03_yap-yerlestirme-cekirdegi/BLD-S008_yesil-krmz-ghost-ve-etkilesim/SPRINT.md): Yeşil-kırmızı ghost ve etkileşim
- [BLD-S009](../PHASES/P03_yap-yerlestirme-cekirdegi/BLD-S009_yap-commit-ve-kalc-kayt/SPRINT.md): Yapı commit ve kalıcı kayıt

## Owner decision form
- Decision: PASS / PARTIAL / BLOCKED / FAIL / NOT_RUN.
- Log source and exact test environment: ___.
- Manual screenshot paths / comparison condition: ___.
- Migration / regression status: ___.
- Open issues and action owner: ___.
- Permission to proceed to next phase: ___ (owner signoff).
