# Manual Unity doğrulama protokolü

**LS-BLD v2 governance · PROPOSED · owner acceptance pending**

> **Dayanak / durum:** Bu belge 17.10.2026 tarihli onay bekleyen LS-BLD üretim önerisinden türetilmiştir. “Mevcut” denilen sistemler GitHub `s012-integrated-graybox-slice` branch'inde incelenmiştir; yeni Construction/Harvest/Notebook tanımları **TASARIM HEDEFİ** olup kodda uygulanmış değildir. Yerel working tree uygulama gününde yeniden incelenecektir.

## Yetki ve komut
- Agent test sınıflarını yazabilir ve static analysis yapabilir; Unity EditMode, PlayMode, build, benchmark veya visual gameplay verification **çalıştırmaz**.
- Native authoring gerekiyorsa gerçek menü/metot uygulanır, kullanıcıya explicit komut aktarılır.
- Komut `Tools/`, assembly ve test filtresi yerel kodda doğrulanmadan uydurulamaz.
- Test kanıtı her sprintte `Evidence/OWNER-YYYYMMDD-...` altında tutulur; log, `tests.xml`, `result.json`, source hash ve screenshot varsa birlikte kaydedilir.
- Başarı şartları: Focused Edit→Focused Play→focused gameplay→regression→build/performance ve visual gate.
- Bir test FAIL ise root cause, delta, tiny safe fix ve **yalnız failed test** için tekrar komutu; PASS sonrası gerekli üst seviyeye genişlet.
- `IMPLEMENTED` test geçmek demek değil. Status: PROPOSED, READY, IMPLEMENTED, VERIFYING, PASS, FAIL, PARTIAL, BLOCKED, TEST_NOT_RUN, VISUAL_NOT_APPROVED.

## Handoff zorunlu alanlar
1. Phase/sprint/day ID, branch, HEAD, worktree snapshot.
2. Existing behavior vs newly implemented behavior.
3. Changed files/asset GUIDs, no unrelated changes kanıtı.
4. Owner-run commands and clean Editor state requirement.
5. Initial data, expected receipt, invalid state and rollback.
6. Test results and design exceptions.
7. Player visual capture viewpoints and manual art acceptance where relevant.
8. PASS için receipt ve owner signoff.

## Uygulama zamanı
Bu sözleşme ilgili phase/sprint/day belgelerinde referans alınır. Gerçek kodu ve yerel scene state’i implementation gününde doğrulamadan hiçbir önerilen sınıfı mevcut sayma.
