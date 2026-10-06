# VEH-001 clean Development build warning audit

2026-10-05 user-run `python3 Tools/veh001-verify.py build --clean-cache`:
`Evidence/20261005T154710-815578Z-build-clean/build/build.txt` reports Succeeded,
0 errors, 391 warnings, Unity 6000.5.0f1. The exact BuildReport messages are in the
same evidence directory's `warnings.txt`.

| Category | Count | Source |
|---|---:|---|
| Shader warnings | 345 | `Packages/com.unity.ai.inference` (Sentis kernels) |
| C# CS0618 | 43 | Existing LastSignal and FirstPersonARPGStarterPack obsolete APIs |
| C# CS0414 | 1 | Existing source |
| Other build warnings | 2 | Missing Pipeline RuntimePipelineConfig; empty ThirdParty NavMeshComponents asmdef |
| **Total** | **391** | Matches BuildReport |

There are no warnings naming `Assets/LastSignal/Scripts/Runtime/Vehicles`,
`Assets/LastSignal/Scripts/Integrations/MotionCore`, or
`Assets/ThirdParty/Vehicle Entegrations`. The earlier `VehicleActor` `EntityId.GetRawData()`
CS0618 messages are absent after replacing six uses with `EntityId.ToULong(...)`.

The entry BuildReport recorded 378 warnings, but no per-warning BuildReport list was
preserved. Its separate compilation diagnostics file includes multiple compile
passes and therefore cannot be compared numerically to a single BuildReport.
Consequently the 13-message total increase cannot be assigned to individual
warnings with available evidence. No vehicle-source warning is present in the
current complete list. The warning count is **not** a zero-warning claim or an
attributed VEH-001 regression.

## Driving closure clean build — 2026-10-05 23:22 UTC

`Evidence/20261005T232231-982273Z-build-clean/build/build.txt`: Succeeded, 0 errors, 396 warnings. Warning text contains 345 shader-warning lines, 46 CS0618 lines, 3 CS0414 lines and the same 2 other build warnings (plus multiline continuations). No Vehicles, MotionCore or Vehicle Entegrations source path occurs. Compared with the earlier 391 report, diagnostic multiplicities and source distribution differ, including repeated C# diagnostics; the net +5 is not evidence of five distinct newly introduced defects. The raw reports are retained. This is not a zero-warning build.
