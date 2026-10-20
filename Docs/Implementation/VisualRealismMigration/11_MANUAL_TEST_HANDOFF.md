# 11 — MANUAL TEST & VERIFICATION HANDOFF POLICY (USER EXECUTION ONLY)

**Date:** 2026-10-20  
**Policy:** Strict compliance with Section 12 (**USER EXECUTION ONLY**). The agent never executes Unity EditMode tests, PlayMode tests, builds, NavMesh bakes, scene promotions, or performance benchmarks without explicit permission.

---

## 1. Verified Local Verification Runners in `Tools/`

All commands below have been verified against the actual Python scripts in `/Users/burakcoskun/Last Signal/Tools/`. No invented flags or nonexistent test filters are used.

| Tool Script | Verified Subcommands | Purpose & Target Scope |
|---|---|---|
| `Tools/thirdparty_audit.py` | *(no args)* | Static filesystem audit of `Assets/ThirdParty/` structure, `.meta` pairing, and GUID uniqueness (Safe, does not launch Unity). |
| `Tools/p01-tree-verify.py` | `edit`, `play`, `physics --stage <path>`, `persistence`, `persistence-stage --stage <path>`, `regression-edit`, `regression-play`, `--filter <NUnitFullName>` | Official BLD P01 S003/S004 tree harvesting & physics verification runner. Writes evidence to `Docs/Implementation/BLD/P01/S004/Evidence/OWNER-<timestamp>-<gate>-<rand>/`. |
| `Tools/first-town-atmosphere.py` | `compile`, `references`, `editmode`, `playmode` | Official `FirstTownAtmosphere` compilation, reference integrity, and world-time/atmosphere test runner. |
| `Tools/first-town-forest.py` | `compile`, `editmode`, `playmode`, `full-editmode`, `full-playmode` | Official `FirstTownForest` verification runner (`ForestVerification`, `FirstTownPlayTests`). |
| `Tools/first-town.py` | `bake`, `build`, `editmode`, `playmode` | FirstTown navigation bake, standalone build, and core scene test runner. |
| `Tools/s023-verify.py` | `development-build --unity <path> --project <path>` | Verified macOS development build runner (`Builds/S023/<run-name>/LastSignal.app`). |

---

## 2. Mandatory Gate Execution Order (When Reaching Gate 3 Owner Verification)

Before running any terminal command that invokes Unity in batchmode, **save your work and close the Unity Editor** so the project lock guard (`Temp/UnityLockfile`) does not reject the run.

### Step 1 — Focused Compilation & Atmosphere/Forest Reference Check
```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/first-town-atmosphere.py compile
cd '/Users/burakcoskun/Last Signal' && python3 Tools/first-town-atmosphere.py references
```

### Step 2 — Focused EditMode Suites (Tree Harvesting + World Time / Atmosphere)
```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py edit
cd '/Users/burakcoskun/Last Signal' && python3 Tools/first-town-atmosphere.py editmode
```

### Step 3 — Focused PlayMode Suites (Tree Physics + Atmosphere / World Time)
```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py play
cd '/Users/burakcoskun/Last Signal' && python3 Tools/first-town-atmosphere.py playmode
```

### Step 4 — Staged Physical Tree Integration Gate (S004 Pine or Birch Stage)
```bash
cd '/Users/burakcoskun/Last Signal' && \
python3 Tools/p01-tree-verify.py physics --stage 'Assets/LastSignal/Scenes/Validation/FirstTownTreePhysics_20261019163434-8e0f05b7.unity' --chops 4
```

### Step 5 — Persistence & Targeted Regression Gates
```bash
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py persistence
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py regression-edit
cd '/Users/burakcoskun/Last Signal' && python3 Tools/p01-tree-verify.py regression-play
```

---

## 3. Failure Diagnosis & Retest Loop

1. **Stop on First Failure:** Do not chain later gates if an earlier gate fails.
2. **Provide Evidence to Agent:** Share the generated `Docs/Implementation/.../Evidence/.../unity.log`, `tests.xml`, and `result.json` paths.
3. **Targeted Fix & Single-Test Retest:** The agent will inspect only the failing test/component, apply a minimal fix, and provide a single-test `--filter` command (e.g. `python3 Tools/p01-tree-verify.py play --filter 'LastSignal.Tests.P01TreePhysicsTests.<TestName>'`) before re-running the suite.
4. **Status Separation:** Code authored by the agent is always marked `IMPLEMENTED / TEST_NOT_RUN` until the user returns a verified XML/log receipt showing `Passed`, and `VISUAL_NOT_APPROVED` until the user approves in-game screenshots.
