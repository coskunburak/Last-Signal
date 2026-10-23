# 11 — MANUAL TEST & VERIFICATION HANDOFF POLICY (USER EXECUTION ONLY)

**Date:** 2026-10-20  
**Policy:** Strict compliance with Section 12 (**USER EXECUTION ONLY**). All Gate 2A, Gate 2B, and Gate 3 batchmode verification gates have been executed by the project owner via `Tools/hdrp-migration.py`, `Tools/first-town-atmosphere.py`, and `Tools/p01-tree-verify.py`.

---

## 0. Verified Owner-Run Evidence Log (Gate 2A → Gate 2B → Gate 3 Active HDRP)

| Stage / Gate | Runner Command | Evidence Directory / File | Result |
|---|---|---|---|
| **Gate 2A (URP Baseline)** | `python3 Tools/first-town-atmosphere.py compile/references/editmode/playmode` & `python3 Tools/p01-tree-verify.py edit/play` | `20261020-164905-593716-compile`, `20261020-164924-070631-references`, `20261020-164937-826703-editmode`, `20261020-165024-469515-playmode`, `OWNER-20261020-165008-edit-ayiodcb5`, `OWNER-20261020-165103-play-xz9a89ha` | **`PASS`** (`31/31`, `15/15`, `74/74`, `21/21`) |
| **Gate 2B (HDRP 17.5.0 Installed, URP Active)** | `python3 Tools/hdrp-migration.py compile/references/editmode/playmode/s004-edit/s004-play` | `20261020-185059-234541-compile`, `20261020-190735-116611-references`, `20261020-190752-606696-editmode`, `20261020-190824-825894-playmode`, `OWNER-20261020-190905-edit-qiyc1wsi`, `OWNER-20261020-190922-play-urvln3ny` | **`PASS`** (`31/31`, `15/15`, `74/74`, `21/21`) |
| **Gate 3 (HDRP Asset Setup, 17 Shaders, 966 Materials, Pipeline Switch)** | `python3 Tools/hdrp-migration.py migrate-and-activate` | `20261020-191437-559927-migrate-and-activate`, `hdrp_assets_setup.json`, `hdrp_shader_validation.json`, `material_migration_audit.json`, `material_migration_execution.json`, `hdrp_isolated_scene_validation.json`, `hdrp_pipeline_activation.json` | **`PASS`** (`17/17` shaders `0` errors; `966` materials `0` errors; active pipeline = `HDRP`) |
| **Gate 3 (Production Scene & S004 Under Active HDRP 17.5.0)** | `python3 Tools/hdrp-migration.py references/editmode/playmode/s004-edit/s004-play` | `20261020-191614-180188-references`, `20261020-191633-522745-editmode`, `20261020-191801-877369-playmode`, `OWNER-20261020-191844-edit-awyjjtdn`, `OWNER-20261020-191900-play-xp52be1n` | **`PASS`** (`31/31`, `15/15`, `74/74`, `21/21`) |

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
