# S021 user-operated verification

Run one command at a time from `/Users/burakcoskun/Last Signal`. Save and close the running Unity Editor first; the runner refuses to start a second Editor or remove its lock. It resolves `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity` from `ProjectSettings/ProjectVersion.txt`; `--unity PATH` overrides this if needed.

| Order | Single command | Advance only when |
|---|---|---|
| 1 | `python3 Tools/s021-verify.py compile` | `gate-result.json` PASS |
| 2 | `python3 Tools/s021-verify.py s021-edit` | Focused EditMode PASS |
| 3 | `python3 Tools/s021-verify.py s021-play` | Focused PlayMode PASS |
| 4 | `python3 Tools/s021-verify.py regression-edit` | Full EditMode PASS |
| 5 | `python3 Tools/s021-verify.py regression-play` | Full PlayMode PASS |
| 6 | `python3 Tools/s021-verify.py development-build` | BuildReport `Result=Succeeded` and source preserved |

Each invocation creates `Docs/Implementation/S021/Evidence/<UTC>-<gate>-<id>/` with `gate-result.json`, `process.json`, `source-preservation.json`, `unity.log`, Git state, compiler failure excerpt and (where relevant) `tests.xml` or `Builds/S021/<run>/build.txt`. Prior evidence is never overwritten. The runner records before/after hashes even for initially dirty files and reports ProjectSettings changes. It never reverts them. On a failure, return only that run's evidence; a filtered retest may use `--filter NUnit.FullName` with a test gate.

The installed Inference package's `AnalyticsDefineManager` toggles `SENTIS_ANALYTICS_ENABLED` according to Editor analytics availability. The runner permits only addition/removal of that single Standalone define when every other byte of ProjectSettings is identical. It records this as `expectedChanged`; all other changes remain fatal (`unexpectedChanged`). Nothing is automatically reverted. This addresses the observed source-preservation failures in `20261011T124831Z-s021-edit-eb49ab20` and `20261011T125054Z-compile-dcfff12e`; historical gate results remain FAIL. The latter Unity process exited 0 with zero compiler diagnostics. The updated runner and S021 fixture still require user verification.

No Unity gate was executed by the assistant. At the user's request, commands may be chained with `&&` so execution stops at the first failed gate.
