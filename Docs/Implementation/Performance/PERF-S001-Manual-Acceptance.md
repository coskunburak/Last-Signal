# PERF-S001 — Owner verification

Status: OPEN — owner acceptance required. No Unity tests/builds/benchmarks have been run by the agent. C# syntax parsing is not Unity compilation or proof of rendered behavior.

## Compile and targeted suites

Save unrelated work before opening changed scenes; reload the on-disk scene changes rather than saving a stale in-memory scene over the added references. Let Unity reimport/compile normally. Check Console first. Do not run all suites by default.

macOS: save and close Unity normally before batch commands. From project root:

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/HDRPProductionOptimization/manual_checks.py graphics-edit --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'
python3 Tools/HDRPProductionOptimization/manual_checks.py graphics-play --unity '/Applications/Unity/Hub/Editor/6000.5.0f1/Unity.app/Contents/MacOS/Unity'
```

Windows PowerShell: locate Unity 6000.5.0f1 in Unity Hub → Installs → Show in Explorer. Copy the full Editor\Unity.exe path. Open PowerShell at this project's actual Windows root; save/close Unity first:

```powershell
$unityEditorPath = Read-Host 'Unity 6000.5.0f1 Editor\Unity.exe tam yolu'
if (!(Test-Path $unityEditorPath)) { throw 'Unity yolu bulunamadı' }
py -3 Tools/HDRPProductionOptimization/manual_checks.py graphics-edit --unity $unityEditorPath
py -3 Tools/HDRPProductionOptimization/manual_checks.py graphics-play --unity $unityEditorPath
```

Alternatively Window → General → Test Runner: EditMode `LastSignal.Tests.GraphicsSettingsTests` in LastSignal.EditModeTests; PlayMode `LastSignal.Tests.GraphicsSettingsPlayTests` in LastSignal.PlayModeTests. Play fixture requires an empty test scene, preserves/reinstates preference keys, and cleans up owned objects. Runner writes timestamped logs/results under its evidence output and refuses to remove Unity lockfiles.

Coverage prepared: profile copy/round-trip/corruption/custom, default/reset/persistence, actual presenter state, Off/high original modes on all fixture LODs, unrelated renderer, live Rigidbody/collider, distance tiers/hysteresis, late registration/unload/singleton, camera shadow gates/light resolution, effective Volume distance and profile immutability. Full tree harvesting, movement, actual scene reload/restart, build shader variants and visual crossfade still need the following owner checks. Existing `tree-stage` and `regression-play` actions exercise current tree/movement suites if needed; agent has not run them.

## Scene/UI/physics acceptance

1. Open `Assets/LastSignal/Scenes/Validation/ForestPrototype120.unity` alone. Enter Play; click Game; Escape opens Grafik. F-keys on Mac may require **Fn/🌐 + F9**.
2. Select Low then Uygula ve kaydet. Trees remain visible and solid; grass/rocks/buildings remain. Tree shadows Off; distant LOD appearance changes with LOD bias, no tree removal.
3. Apply Medium then High. Move across 20/23 and 40/43 m from a tree (3 m exit hysteresis); inspect all LODs and crossfades. Ultra restores original tree modes and extends global distance; it is not guaranteed fast.
4. Select High, change only Ağaç gölgesi to Off, Apply. Active profile becomes Custom; global High, 80 m, fog/AO and LOD 1 remain. Non-tree real-time shadows remain. Test global Off separately; lighting and AO must remain distinguishable from real-time shadows.
5. Set 30 m and run menu `Last Signal → Graphics → Report effective shadow distance (Play Mode)`. Console should report approximately 30 m. Confirm visually/Frame Debugger as well. Switch F7/F8 weather before a capture; distance should remain applied while art lighting changes.
6. Stage edits and close/reopen: unapplied edits discarded. Apply, stop/restart Play or application: persisted Custom restored. Defaults saves Low. Özgün sahne restores runtime-entry values temporarily without deleting preference; applying High again exits this override.
7. Open `Assets/LastSignal/Scenes/Production/FirstTownCore.unity`. Main menu or pause → Ayarlar → Grafik. Confirm same saved settings, no duplicate manager, registered decorative/interactive trees, independent building/character shadows. Cut, damage, drop and collect a tree; verify rigidbody fall, stump/reward state, inventory and player movement. Shadow changes must not change any of these.
8. Transition/reload supported scenes and additive test scenes; no stale references, extra managers, leaked runtime Volumes or modified asset indicators. Check Console. Unsupported custom/terrain rendering must not be reported as covered.
9. Repeat in owner-created HDRP standalone builds on macOS Metal and Windows independently. Release UI works; F9 instrumentation is restricted to Editor/Development builds. Capture logs/screenshots and specify build type. Inspect fog/SSAO variants, text fit, keyboard/gamepad focus and mouse controls.

## Optional owner-triggered controlled measurement

No new benchmark is required just to use tree shadows Off. Performance claims and profile calibration remain pending until the owner chooses to measure.

For evidence: use a fixed Game-view pixel resolution, camera F3 and unchanged orientation, F7 or F8, VSync/cap, weather and AC/power mode. Record Editor and standalone separately and Mac/Dell separately. Do not move mouse/camera or change focus during static capture. First capture **Özgün sahne** with a clearly labeled scenario. Then High base with only tree level changed: High, Off, Low, Medium; keep all other fields identical. Do not apply four named presets for a single-variable shadow experiment.

Set ForestPrototypeCapture.scenario in Play Mode before each run. Focus Game and manually press F9 (Fn+F9 if necessary). Warmup requires BOTH 180 frames and 10 seconds, then 30 seconds recording. Wait for `Forest performance capture written`. Settings remain locked while capturing. No automatic benchmark. Outputs: `Application.persistentDataPath/ForestPrototype120/<capture-id>/` (macOS normally `~/Library/Application Support/DefaultCompany/Last Signal/ForestPrototype120/`). Preserve summary.json and frames.csv together.

Compare average/P50/P95/P99, Main/Render thread, GPU when available, draws/batches, memory/GC. Use Profiler/Frame Debugger for actual shadow pass time and visible renderer counts: unavailable values stay UNAVAILABLE/NOT_MEASURED, not zero. New capture metadata includes configured quality plus effective Volume distance; actual GPU shadow cost still needs profiling. Existing A/B is not a controlled shadow-only comparison.

Acceptance remains open until compilation, requested suites, actual scene/UI/gameplay checks and standalone platform checks have owner results. Later sprints: measured profile calibration, foliage geometry/alpha-overdraw and culling optimization, terrain/custom GPU adapter if adopted, platform-specific shadow tradeoffs. No DLSS/FSR, streaming overhaul or auto FPS quality system in this sprint.
