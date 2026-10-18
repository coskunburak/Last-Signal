# G0 — Ürün sözleşmesi ve güvenli mimari kabul sözleşmesi

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Bağlayıcı çıkış: G0: onaylı kapsam farkı, teknik tasarım, şema ve bağımlılık matrisi.

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
- Oyuncu sıfır materyalle sığınağı geliştiriyor mu? Hayır; gerekli kaynak yolu görünür.
- Oyuncu inşa etmek istemediğinde temel survival rotası kilitleniyor mu? Kilitlenmemeli.
- İlk sürüme büyük serbest yıkım veya multiplayer sızıyor mu? Kapsam dışı.

## Related cards / sprints
- [BLD-S001](../PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S001_gdd-kapsam-ve-oyuncu-vaadi/SPRINT.md): GDD kapsam ve oyuncu vaadi
- [BLD-S002](../PHASES/P00_urun-sozlesmesi-ve-guvenli-mimari/BLD-S002_mimari-sozlesme-ve-risk-temeli/SPRINT.md): Mimari sözleşme ve risk temeli

## Owner decision form
- Decision: PASS / PARTIAL / BLOCKED / FAIL / NOT_RUN.
- Log source and exact test environment: ___.
- Manual screenshot paths / comparison condition: ___.
- Migration / regression status: ___.
- Open issues and action owner: ___.
- Permission to proceed to next phase: ___ (owner signoff).
