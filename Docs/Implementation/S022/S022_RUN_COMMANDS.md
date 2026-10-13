# S022 user-operated commands

**The agent has not run any Unity command below.** The user's Gates 1–2 passed (53/53, 28/28), and Gate 3 passed 773/773 with unchanged source before the latest pistol pose correction. The focused pistol rerun failed 0/1 during DXT reload after the first correction. The latest three-variant pose adjustment adds frame clearance but is unverified in Unity. Run the single case through Gate 4's `--filter` first, then rerun Gate 3, full Gate 4 and Gate 5 if each passes; a shell block using `set -e` stops at the first failure. Gates 5–7 remain NOT_RUN. Gate 0 outcome was not reported.

## Gate 0 — Editor preflight
Open `/Users/burakcoskun/Last Signal` normally with Unity 6000.5.0f1. Allow import and normal automatic compilation to finish. Preserve unsaved user scenes. Return Console errors if present. Inspect that S022 assets/variant references resolve. Ordinary import is not an automated compile/test PASS. Close Unity normally before any command below. Never delete a live lock or start a second Editor for this project.

## Gate 1 Focused EditMode

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s022-verify.py' focused-edit --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

## Gate 2 Focused PlayMode

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s022-verify.py' focused-play --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

## Gate 3 Full EditMode

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s022-verify.py' regression-edit --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

## Gate 4 Full PlayMode

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s022-verify.py' regression-play --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

## Gate 5 macOS Development Build

```bash
python3 '/Users/burakcoskun/Last Signal/Tools/s022-verify.py' development-build --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity' --project '/Users/burakcoskun/Last Signal'
```

## Output and evidence
Each invocation creates a unique `Docs/Implementation/S022/Evidence/Manual-YYYYMMDD-HHMMSS-<gate>-<suffix>/` (Istanbul timestamp). Terminal prints `EVIDENCE=` and the actual Unity command. Keep `command.json`, `environment.json`, `head.txt`, `status.z`, `process-result.json`, `tests.xml` for test gates, `unity.log`, `result.json`, `source-before.json`, `source-after.json`, `source-preservation.json`, and any legacy-preservation outputs. Failed/skipped cases and runner exit code appear in `result.json`; the process exit code remains separate. Missing XML or zero tests is failure even if Unity returned zero.

Build output: unique `Builds/S022/<run-name>/LastSignal.app`, `build.txt`, `warnings.txt`. `S022Build.DevelopmentMac` reads `LASTSIGNAL_S022_BUILD_OUTPUT`, then calls the existing production builder with Development/StandaloneOSX and `Assets/LastSignal/Scenes/Production/S013Cabin.unity`. No authoring regeneration or scene replacement is performed by this wrapper. Missing macOS build support is an environment blocker, not PASS. Gates 6/7 follow S022_MANUAL_QA.md after prior gates succeed.

The runner checks the project path, executable permission/version directory, assembly names, build entry, Editor process/lock, and protects existing historical output through the S015 preservation helper. It separately reports the exact expected Unity Inference `SENTIS_ANALYTICS_ENABLED` Standalone toggle and any unexpected source mutation; it does not reset or roll back source. It uses no `-quit` for test runs, preserving the established Test Runner convention. Use `--filter 'LastSignal.Tests.Class.Method'` only for an explicitly requested focused repair; that is not a substitute for complete gate evidence.

Return the focused pistol retest's or Gate 4 rerun's `result.json`, `process-result.json`, `source-preservation.json`, and the first failure/Console excerpt with `tests.xml` or `unity.log` as needed. No request for all historical logs is necessary.
