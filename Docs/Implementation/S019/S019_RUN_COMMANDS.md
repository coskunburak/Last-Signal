# S019 user-run verification — D186/D189 production authoring pending

## Active step: D186/D189 production authoring (build deferred)

User requested production completion and deferred Development Build. No AI-run Unity execution. Close Unity normally, then run this **authoring** command (it creates new owned assets, not a build):

```zsh
python3 "/Users/burakcoskun/Last Signal/Tools/s019-author.py" --unity "/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity" --project "/Users/burakcoskun/Last Signal"
S019_EXIT=$?
print -r -- "Production authoring exit=$S019_EXIT"
```

Expected: `TECHNICAL AUTHORING PASS`, 3 native presets, 6 reimports, 5 complete navigation routes. It prints a new EVIDENCE directory containing `authoring.json`, `result.json`, `unity.log`, actual `command.json`, Unity `process-result.json` and source-preservation before/after hashes. This is `-executeMethod LastSignal.Editor.S019.S019ProductionAuthoring.Run -quit`, with explicit project/log paths; test platform/filter/XML N/A. Source preservation allows only listed **new** artifacts and forbids modifications to pre-existing files, including vendor sources. Return this result before proceeding. If it fails, do not manually delete or overwrite artifacts; send diagnostics for a targeted fix.

Generated scene: `Assets/LastSignal/Scenes/S019/S019Pilot.unity`; baked data: `Assets/LastSignal/Data/S019/S019PilotNavigation.asset`. Three scoped native presets and importer exemplars are saved under the existing S019Import settings/art folders. Running authoring again validates/reimports existing content, without recreating the scene or changing its IDs. An incompatible pre-existing asset is an explicit failure. Automated elapsedSeconds is not a manual time measurement.

After authoring evidence is accepted, run each gate separately, returning any failure before continuing:

```zsh
python3 "/Users/burakcoskun/Last Signal/Tools/s019-verify.py" production-edit --unity "/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity" --project "/Users/burakcoskun/Last Signal"
S019_EXIT=$?
print -r -- "Production EditMode exit=$S019_EXIT"
```

This resolves to EditMode / LastSignal.EditModeTests / LastSignal.Tests.S019ProductionAuthoringTests; expected 12 cases. New unique `tests.xml`, `unity.log`, actual command and Unity exit recorded by the runner.

```zsh
python3 "/Users/burakcoskun/Last Signal/Tools/s019-verify.py" pilot-play --unity "/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity" --project "/Users/burakcoskun/Last Signal"
S019_EXIT=$?
print -r -- "Pilot PlayMode exit=$S019_EXIT"
```

This resolves to PlayMode / LastSignal.PlayModeTests / LastSignal.Tests.S019PilotPlayTests; expected one test of actual pickup/inventory/save/load and consumed-loot persistence. Test writes only a unique temporary save directory. Afterwards use current s019-edit and full regression commands below as justified by results; prior PASS results are for the previous source snapshot. Keep Development Build deferred until user resumes it.

Manual D189 acceptance: open the **generated** S019Pilot scene. Player starts in the existing shelter; use the existing exit/preparation flow and approach the building at (6,.1,6). Check entrance/retreat, floor/walls, reachable loot and the single existing encounter implementation. Existing scene lighting/UI/gameplay services are retained. Pause Save/Load uses the isolated `saves/s019-pilot.json` slot; original `saves/current.json` remains separate. Record pickup/consumption, inventory quantities, encounter/door state, save success, process/session restart, load and continued interaction. Native reimport snapshots do not replace visual/audio/collision acceptance. Record these against the checklist, not as automatic PASS.

Real D189 cost comparison: in disposable scenes, time (A) manually composing the same 15 existing kit pieces + two loot anchors + entry/retreat/navigation/encounter references, and (B) using the prefab creation command for the same result. Use the same starting scene/asset availability and acceptance checklist; count actual interactions and repairs for each. Record elapsed time, action count, validation errors caught and rework. This is a **new matched comparison**, not invented historical data; PREVIOUS_TIME stays UNKNOWN. If no comparison is measured, leave that acceptance criterion NOT_MEASURED. No speedup is claimed from automation duration alone.

Only the user executes these steps. Run one gate, inspect its result, then continue. Stop on compilation/test/source-preservation/build failure and return that run's evidence for a targeted fix. No AI-run Unity compilation or QA has occurred. R01 was executed by the user; the remaining initial sequence below is retained for subsequent gates.

## 1. Environment preflight (macOS Terminal)

Close Unity normally before batchmode; never run two Editors against this project. Do not delete a live lock file. These are the inspected local paths:

```zsh
export S019_PROJECT='/Users/burakcoskun/Last Signal'
export S019_UNITY='/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'
cd "$S019_PROJECT"
test -x "$S019_UNITY"
cat "$S019_PROJECT/ProjectSettings/ProjectVersion.txt"
git -C "$S019_PROJECT" branch --show-current
git -C "$S019_PROJECT" rev-parse HEAD
git -C "$S019_PROJECT" status --short
pgrep -fl '/Unity.app/Contents/MacOS/Unity'
if test -e "$S019_PROJECT/Temp/UnityLockfile"; then echo 'STOP: Unity lock present'; else echo 'No project lock'; fi
python3 --version
```

Expected version 6000.5.0f1; `pgrep` prints nothing and exits 1 when no Editor is running. Existing uncommitted work is expected; record it, do not reset/clean. Missing executable, version mismatch, active process or lock: resolve environment before proceeding. Initial source import/compilation happens under user QA ownership.

The new runner executes exactly ONE gate and reuses the existing S013 production build entry point and S015 historical-evidence preservation. It records the actual fully expanded Unity command, HEAD/status, package/version, all Assets/Packages/ProjectSettings/Tools source hashes, process exit, logs and results. Do not bypass the runner for full regressions: existing tests have historical fixed evidence paths that need preservation.

Every run creates a unique directory:
`/Users/burakcoskun/Last Signal/Docs/Implementation/S019/Evidence/Manual-YYYYMMDD-HHMMSS-<gate>-<random>/`

It prints `EVIDENCE=...`. Inside: `command.json`, `unity.log`, `process-result.json`, `source-before.json`, `source-preservation.json`, `result.json`; tests also produce `tests.xml`. Earlier evidence is never reused. Build XML/test platform/filter are N/A; a build has `build.txt` in the immutable output directory instead.

Verified gate expansion (common flags always include explicit `-batchmode -projectPath "$S019_PROJECT" -logFile "$EVIDENCE/unity.log"`):

| Gate | Actual Unity flags |
|---|---|
| s019-edit | `-runTests -testPlatform EditMode -assemblyNames LastSignal.EditModeTests -testFilter LastSignal.Tests.S019ContentToolsTests -testResults "$EVIDENCE/tests.xml"` |
| regression-edit | `-runTests -testPlatform EditMode -assemblyNames 'LastSignal.EditModeTests;LastSignal.Art.EditorTests' -testResults "$EVIDENCE/tests.xml"` (no filter) |
| focused-play | `-runTests -testPlatform PlayMode -assemblyNames LastSignal.PlayModeTests -testFilter 'LastSignal.Tests.LootPopulationTests;LastSignal.Tests.LootCompatibilityTests' -testResults "$EVIDENCE/tests.xml"` |
| regression-play | `-runTests -testPlatform PlayMode -assemblyNames LastSignal.PlayModeTests -testResults "$EVIDENCE/tests.xml"` (no filter) |
| windows-build | `-buildTarget Win64 -executeMethod LastSignal.Art.Editor.S013ProductionAuthoring.BuildS017Windows -quit` |

Tests intentionally omit `-quit`, following the existing project Test Framework runner conventions. Each following command specifies the Unity executable and project; the gate resolves the verified platform/filter/XML/log fields above. The runner saves Unity's own process exit; the shell separately captures the runner's acceptance exit. Exit 0 requires passing tests/build AND no source changes. Source drift remains FAIL even with green XML.

## 2. Focused S019 EditMode (28 authored cases)

```zsh
python3 "$S019_PROJECT/Tools/s019-verify.py" s019-edit --unity "$S019_UNITY" --project "$S019_PROJECT"
S019_EXIT=$?
print -r -- "S019 focused EditMode runner exit=$S019_EXIT"
```

Expected 28 discovered cases, no failures/skips, no source drift. Inspect `result.json`, `tests.xml`, `unity.log`; zero tests is failure. Cases cover invalid definitions/catalog/recipes/loot, inactive duplicate identities/build rejection, real-selector preview state/determinism, hash ordering/cosmetics, import scoping/idempotence, template references/new IDs/Undo. Unity compilation is not established until this succeeds.

## 3. Full EditMode regression (including Art Editor assembly)

```zsh
python3 "$S019_PROJECT/Tools/s019-verify.py" regression-edit --unity "$S019_UNITY" --project "$S019_PROJECT"
S019_EXIT=$?
print -r -- "Full EditMode runner exit=$S019_EXIT"
```

Record actual discovered count, failures and source-preservation report. Historical S018-era counts are not the expected count for this changed source tree.

## 4. Existing loot PlayMode integration, then full regression

No new runtime authority or PlayMode fixture was introduced; use real existing loot/save integration coverage. These commands remain user-operated:

```zsh
python3 "$S019_PROJECT/Tools/s019-verify.py" focused-play --unity "$S019_UNITY" --project "$S019_PROJECT"
S019_EXIT=$?
print -r -- "Focused PlayMode runner exit=$S019_EXIT"
```

Only after a passing focused result:

```zsh
python3 "$S019_PROJECT/Tools/s019-verify.py" regression-play --unity "$S019_UNITY" --project "$S019_PROJECT"
S019_EXIT=$?
print -r -- "Full PlayMode runner exit=$S019_EXIT"
```

## 5. Development Build (existing production entry point)

Windows64 support must be installed for this Unity version. Do not install packages automatically. Existing builder selects `Assets/LastSignal/Scenes/Production/S013Cabin.unity`; the pilot is not added to the integrated slice or build settings.

```zsh
python3 "$S019_PROJECT/Tools/s019-verify.py" windows-build --unity "$S019_UNITY" --project "$S019_PROJECT"
S019_EXIT=$?
print -r -- "Development Build runner exit=$S019_EXIT"
```

The runner sets `LASTSIGNAL_S017_BUILD_OUTPUT` to unique `Builds/S019/<run-name>/`, despite the existing method's historical S017 name. Expect `LastSignal.exe`, `build.txt`, `warnings.txt`; inspect `result.json` and source preservation. Platform, filter, XML: build = StandaloneWindows64, filter/XML N/A. The source-level validator must reject persistence identity errors before a successful build. Do not rerun against an existing destination or label a Mac-only check as Windows runtime acceptance.

## 6. Original manual Editor acceptance (production command now handles preset setup)

Open `/Users/burakcoskun/Last Signal` in Unity 6000.5.0f1. Wait for compilation; report errors before proceeding.

1. `Last Signal > S019 > Validate project and open scenes`. Expect location/field/fix diagnostics, deterministic ordering, and no asset/scene edits. Project scan includes the unbaked pilot as a warning; an instantiated unbaked pilot in a saved scene is an error. Other pre-existing content errors must be triaged, not bypassed.
2. `Last Signal > S019 > Loot distribution preview`. Select Workshop or Kitchen, seed 12345, 1000 samples and fixed point ID. Preview twice: identical empty/item occurrence/quantity/percentage totals. Select an actual anchor and use **Use selected loot anchor** to compare its real ID. No PlayMode required. Source assets/save files stay unchanged. Placement collisions are not simulated.
3. `Last Signal > S019 > Record content fingerprint`. Expect content-addressed text in `Docs/Implementation/S019/Evidence/Fingerprints/`. Compare with source audit hash `748cfea38f3af8534f12b6841c45741c63ce642af3449085d48ce859017180bb` only if catalog semantics have not changed. A mismatch requires diagnosis, not save deletion. It must not modify SaveHeader/contentVersion/schema/build ID.
4. Native Preset inspection (D186): the production authoring command already creates all three presets and exemplar copies. Do not capture them again: capture intentionally refuses overwrite. Inspect the three assets in `Assets/LastSignal/Settings/S019Import/` and their matching copies in `Assets/LastSignal/Art/S019Import/`. Confirm only documented whitelist properties are included; source exemplars and vendor originals stay untouched.
5. Inspect the authoring evidence for six reimports and unchanged protected importer settings. Visually/audibly check scale, texture interpretation, material/shader compatibility, collider policy and audio behavior. Native snapshots cannot prove rendered or audible quality. Changing a preset later requires an explicit scoped reimport and renewed evidence; no global reimport command exists.

Manual actions intentionally change authored QA assets. Record the new source state before a subsequent runner invocation; these are different from unexpected mutations during a test/build.

## 7. Template-only checks (generated playable pilot route is above)

Template artifact: `Assets/LastSignal/Prefabs/S019/S019RuralStore.prefab`. The production command above additionally creates a playable S019Pilot scene; this section only describes the reusable template. Open it in Prefab Mode to inspect the 15 S013 kit instances, local Entry/Retreat markers, Navigation and building root, Workshop/Kitchen anchors and Encounter spawn. Exit Prefab Mode before creation.

1. Create a new default scene and save under a fresh path such as `Assets/LastSignal/Scenes/S019/Manual-R01.unity`. Never save over S013Cabin/IntegratedGraybox. Start a real timer and note manual actions.
2. `Last Signal > S019 > Create new POI from pilot template`. Root appears at origin; it contains no runtime session/population owner. Inspect anchors/profiles, encounter prefab and local metadata references. Record initial IDs.
3. Select **Navigation and building**. Use its existing native NavMeshSurface Inspector **Bake** action (agent type corresponds to project zombie, children/colliders). Verify navigability of Entry, Retreat and Encounter spawn with Unity's navigation overlay. Save the disposable scene; do not apply instance identities back to the shared prefab. Bake/visual acceptance was NOT_RUN by AI.
4. Validate. Deliberately Cmd-D the POI: duplicate identity diagnostics must point to copied objects; no ID changes should occur automatically. Undo the duplicate. Use the S019 creation menu for a fresh independent POI: identities must differ and the source prefab IDs must remain fixed. Undo removes only that new creation.
5. On a disposable instance, clear a loot profile reference and validate: expect actionable `LOOT_POINT` diagnostic. Undo. Missing/negative data cases are also covered in the focused tests, without modifying real assets. Restore all deliberate invalid states before build.
6. Inspect collision/material/lighting/entry route; fill S019_PRODUCTION_CHECKLIST. Stop timer; record actions, repairs, native validator errors and actual elapsed time. If no measured previous POI exists, `PREVIOUS_TIME=UNKNOWN`; no savings percentage. Runtime systems duplicated should remain 0. Record missed or blocked criteria honestly.

The pilot's nav bake and production timing are outstanding D189 work. Do not turn this authoring-only scene into an assumed save acceptance scene. Adding a pilot encounter to the integrated scene can change its single-encounter/session topology; deliberate integration requires separate review, fresh saves and actual supported ownership.

## 8. Runtime/save regression acceptance

Use the newly built existing S013Cabin production route on Windows, under a dedicated QA OS account or established disposable QA profile. Preserve original user saves; if isolation is unavailable, mark save acceptance BLOCKED. This package neither deletes nor overwrites saves automatically.

Record build path/hash/source identity. Start a new disposable run; exercise movement, entry/retreat, loot pickup/inventory, combat and supported relay/shelter progression. Save via the existing pause Save button; record inventory/ammo, loot consumption, doors/encounter and progression. Exit the process completely, relaunch that same build, Load and continue. Check no regenerated loot, lost/duplicated ownership, reset objectives or migration prompt caused by S019. Also run current S018 survival acceptance as required by its handoff; this does not close G5 automatically. Pilot-only runtime save acceptance remains N/A until it has a supported integration context.

## Returning failures / focused retry

Send the exact evidence directory plus `result.json`, `process-result.json`, `source-preservation.json`, failing XML cases and relevant log/error excerpts. Next session reads S019_HANDOFF first and classifies COMPILATION / TEST_INFRASTRUCTURE / FIXTURE / ASSERTION / PRODUCTION_BUG / BUILD_CONFIGURATION / ENVIRONMENT. Smallest correct fix first; every rerun still belongs to you.

Example focused retest after a fix (actual authored test fullname):

```zsh
python3 "$S019_PROJECT/Tools/s019-verify.py" s019-edit --unity "$S019_UNITY" --project "$S019_PROJECT" --filter 'LastSignal.Tests.S019ContentToolsTests.PreviewMatchesRealSelectorAndDoesNotMutateInputsOrGlobalRandom'
S019_EXIT=$?
print -r -- "Focused retest runner exit=$S019_EXIT"
```

Same verified EditMode assembly, explicit filter, new unique XML/log and saved Unity exit status. Expand from failed case → class → all S019 → affected regression → full EditMode/PlayMode → build → manual acceptance only as justified. S019 closure stays PENDING_USER_VERIFICATION.
