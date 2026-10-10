# User-operated verification commands

Every gate is **NOT_RUN**. These commands have been prepared, not executed. Run one command at a time; inspect its result before proceeding. Do not run the authoring menu again as a verification step.

Project: `/Users/burakcoskun/Last Signal`

Unity executable: `/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity`

Save your work and close Unity normally before every batch command below. The runner refuses an active Unity editor or held project lock; it never kills the editor or deletes locks. Python 3 and the installed Unity editor/license are required; the build also needs the installed macOS build support. Commands work from any directory.

## 1 — FIRST: import / compilation

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" compile
```

Send `gate-result.json` and any compiler-error excerpt from `Editor.log`. This is an import/compilation gate only, not a test or runtime acceptance gate.

## 2 — Character EditMode

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" character-edit
```

Uses `LastSignal.EditModeTests`, filter `LastSignal.Tests.CharacterIntegrationAssetTests`: real Player/Diesel/Avatar/controller/masks and cosmetic weapon references.

## 3 — Character PlayMode

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" character-play
```

Uses `LastSignal.PlayModeTests`. Runs CharacterIntegrationPlayTests plus the new real-fixture methods in VehicleOccupancyTests (seat restoration and exclusive inspection ownership) and S018SurvivalPlayTests (late binding, disable/unsubscribe and treatment). Covers measured motion/stance, equip/reload/melee/pause, fall/death/reset, visibility, teardown and missing-combat cleanup. It does not certify visual deformation.

## 4 — Player / combat regression (two independent commands)

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" player-combat-edit
```

After reviewing that result:

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" player-combat-play
```

Edit filters: PlayerHealthTests, WeaponStateTests, AmmunitionTransactionTests, VehicleFoundationTests, S018SurvivalTests. Play filters: MovementAcceptanceTests, CombatAcceptanceTests, S014WeaponPresentationTests, ScopeOpticPlayTests, VehicleOccupancyTests, S018SurvivalPlayTests. Exact namespaces and assembly arguments are in the runner.

## 5 — Full EditMode regression

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" full-edit
```

Actual assemblies: `LastSignal.EditModeTests;LastSignal.Art.EditorTests`. No test filter.

## 6 — Full PlayMode regression

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" full-play
```

Actual assembly: `LastSignal.PlayModeTests`. No test filter. Full suites reuse the existing regression helper to preserve historical fixed-path evidence and copy newly produced artifacts into this run's evidence directory.

## 7 — Development build

```bash
python3 "/Users/burakcoskun/Last Signal/Tools/character-verify.py" development-build
```

Invokes `LastSignal.Editor.CharacterVerification.DevelopmentMac` for StandaloneOSX, BuildOptions.Development, production `S013Cabin.unity`. Produces `LastSignal.app` and `build-summary.txt`; does not launch the game.

## 8 — Manual visual acceptance

After batch Unity exits, reopen the project in Unity 6000.5.0f1 and open `Assets/LastSignal/Scenes/Production/S013Cabin.unity`. Start Play Mode yourself, enter the existing session flow and follow `CHARACTER_MANUAL_QA.md`. Alternatively launch the newly built app yourself. Use F6/F7 with the Game view focused; macOS may require Fn with function keys. Do not use an asset-preview character as acceptance evidence.

## Evidence and result interpretation

Each invocation creates a fresh directory:

`Docs/Implementation/CharacterIntegration/Evidence/<YYYYMMDD-HHMMSS>-<gate>-<unique-id>/`

It records `invocation.json`, `Editor.log`, `gate-result.json`; test gates additionally require `results.xml`. Build requires a successful summary and the app. Missing results, compiler diagnostics, nonzero exit, empty test selection or failing NUnit result are failures. Inspect skipped tests even when a suite result is Passed; skipped/inconclusive coverage is not acceptance. No performance figures are inferred.

If launch is interrupted before the runner finishes, classify the gate as INCOMPLETE, retain its folder and share the focused error; do not call it PASS. Reruns create new evidence. For failure feedback send the gate name, failing test names/assertions/stack traces, JSON summary and relevant log excerpt. Full unrelated logs are unnecessary. Fix only implicated code, then manually rerun the focused gate before broader regression. All execution remains user-operated.
