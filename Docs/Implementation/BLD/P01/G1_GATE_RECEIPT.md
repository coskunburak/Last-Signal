# P01 / G1 kabul makbuzu

- Date: 2026-10-18
- Branch: `s012-integrated-graybox-slice`
- Entry HEAD: `84aa7358be7b3f47517b7e083ff0734e762e210f`
- G0: önceki owner-approved baseline; bu çalışma G0'ı yeniden onaylamaz/değiştirmez.
- S003 checkpoint: **IMPLEMENTED_AWAITING_OWNER_TEST / AWAITING_OWNER_FOCUSED_TEST**. Native data/pilot authoring pending; D017/D019 teslim satırları PARTIAL.
- S004 D025–D032: **NOT_STARTED — WAITING_S003_OWNER_ACCEPTANCE**.
- G1: **PENDING_OWNER — NOT_ACCEPTED**. Test, native gameplay, art/audio/perf evidence yok; release gate kapalı.
- P02 / craft notebook / construction: **NOT_STARTED / OUT_OF_SCOPE**.

## Evidence receipt — owner dolduracak

| Gate | Sonuç | Evidence / zaman / Unity version |
|---|---|---|
| Focused P01HarvestTests | NOT_RUN | ___ / ___ / target6000.5.0f1 |
| Real WorldItem pickup integration | NOT_RUN | ___ |
| Native pilot manifest/source fingerprints/clearance | NOT_RUN | scene ___; manifestId ___; nodeIds ___; sourceSHA ___; slot ___ |
| Old/no-section native fixture + legacy job/storage | NOT_RUN | ___ |
| Partial/final/consumed save/load twice + durable faults | NOT_RUN | ___ |
| Native cell unload/reload + stale topology reject | NOT_RUN | ___ |
| S010/S021/FirstTown regression | NOT_RUN | ___ |
| 30/60/120 FPS impact/noise + enemy/tree priority | NOT_RUN | ___ |
| S004 chop/feedback/fall/stump/pickup/UX/art | NOT_STARTED | ___ |
| Performance + source preservation | NOT_RUN | ___ |
| S003 owner acceptance | PENDING_OWNER | owner ___ / date ___ |
| G1 owner approval | PENDING_OWNER | owner ___ / date ___ |

İlk sonraki action ve tek komut [test handoff](BLD_S003_TEST_HANDOFF.md) içinde. Agent sonucu bekler, testi çalıştırmaz. Fail'de yalnız ilgili ilk evidence/root cause ve focused retest. [Açık kararlar](OPEN_RISKS.md) onaylandığında ve gerekli evidence geldiğinde receipt güncellenir; bu belge yazılmış olması acceptance değildir.
