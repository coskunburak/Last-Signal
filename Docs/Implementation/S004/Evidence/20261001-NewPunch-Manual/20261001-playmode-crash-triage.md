# 2026-10-01 PlayMode command-line crash triage

- `all-editmode.xml`: 418 total, 418 passed, 0 failed, 0 skipped. The corresponding log ends with `Exiting with code 0 (Ok)`; the EditMode result is valid.
- `all-playmode.xml`: absent. The PlayMode run did not complete and must not be reported as a test failure or pass.
- `all-playmode.log` and `Unity-2026-10-01-110704.ips`: SIGSEGV / `EXC_BAD_ACCESS` while `ZombieDamagePlayTests.ActualProductionRifleBodyHeadReactionAndDeath` called `Camera.Render()` from its screenshot capture helper. The run used `-nographics`. Earlier `02-zombie-playmode.log` and `05-all-playmode.log` show the same render stack, so this is repeatable for that invocation.
- `build-summary.txt`: `Result=Succeeded`, `Errors=0`, `Warnings=387`, duration 19.53 seconds. The development build is a separate successful result; warnings have not been triaged here.

Next verification: run that single screenshot test with `-batchmode` **without** `-nographics` into fresh XML/log names. If it passes, run the full PlayMode suite with the same graphics-enabled invocation. If it still crashes, run the test in the Unity Editor Test Runner and inspect the new crash report; do not change gameplay code based solely on this graphics-device crash.
