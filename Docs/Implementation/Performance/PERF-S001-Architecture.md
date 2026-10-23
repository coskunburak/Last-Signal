# PERF-S001 — Graphics settings architecture

Status: IMPLEMENTED, OWNER VERIFICATION PENDING. No Unity tests, Unity builds or new performance sessions executed by the agent. Unity 6000.5.0f1 / HDRP 17.5.0 remains unchanged. Initial profile values are not hardware-calibrated.

## Ownership and initialization

`Assets/LastSignal/Scripts/Runtime/Graphics/` contains `GraphicsSettingsManager`, `GraphicsSettingsState` / `GraphicsQualityProfiles` / `GraphicsSettingsPersistence`, `HdrpGraphicsAdapter`, `VegetationShadowController`, and `GraphicsSettingsPresenter`. Namespace is `LastSignal.RenderingQuality` to avoid colliding with existing `UnityEngine.Graphics.Blit` calls.

SessionFlow initializes one persistent manager; Forest's existing explorer bridge and explicit vegetation registration also ensure initialization when entering a scene directly. Scene-load events register cameras/lights once, unload removes dead references. Player spawning registers its camera. A future dynamically spawned camera/light must call `RegisterRoot(instance)` then `Reapply()`. Future tree spawners call `VegetationShadowController.Attach(tree)`; hierarchy changes below a nested child require explicit `Refresh()` after creation. Do not register overlapping tree hierarchies. No automatic GPU-name recommendation.

Manager owns the current validated configuration, LOD bias, camera quality overrides and cached tree groups. It does not own time, weather, physics, harvesting, resolution or save slots. Tree distance evaluation uses cached groups every 0.25 seconds, active MainCamera preferred, first active Game camera fallback. No active observer means no Low/Medium tree casters. Split-screen/multiple simultaneous observer policies are not implemented.

## Implemented profiles

Single data source: `Assets/LastSignal/Resources/Graphics/QualityProfiles.json`.

| Profile | Global shadows / resolution cap | Tree shadows | Shadow distance | LOD bias | Volumetric fog | SSAO |
|---|---|---|---:|---:|---|---|
| Low | Low / 512 | Off | 40 m | 0.65 | Off | Off |
| Medium | Medium / 1024 | Low: 20 m | 60 m | 0.85 | On | On |
| High | High / 2048 | Medium: 40 m | 80 m | 1.00 | On | On |
| Ultra | High / 2048 | High: authored casters | 120 m | 1.20 | On | On |

Global High permits contact shadows; Low/Medium suppress them. Contact shadows still depend on authored Volume/light support. Resolutions are bounded by pipeline caps. The previous scene sun was 2048 with 80 m shadow distance: High keeps those numerical values. Other values are conservative **initial choices**, requiring owner calibration. Ultra is experimental, not a 60 FPS promise. Fog and AO expose functional On/Off gates, not invented quality tiers. LOD bias is a global LODGroup control, not a terrain grass density control. Tree roots/physics/density do not change.

Manual changes become Custom while preserving other fields; named presets replace all fields. Global Off suppresses tree shadow rendering even if tree preference is High; UI states this dependency.

## Persistence and UI

Version 1 JSON at PlayerPrefs `LastSignal.Graphics.v1`; enum/range/finite validation, malformed or unknown schema fallback to Low. Default Low is used only when there is no valid saved preference or legacy key. Previous `LastSignal.Forest.TreeShadows` migrates to a Custom configuration based on High, preserving original High tree casters or Off; it is not deleted. Apply/Reset saves the entire configuration, independent of world saves.

SessionUi adds **Ayarlar → Grafik** to main/pause settings. Forest uses **Escape** and the same uGUI presenter. Controls stage changes; **Uygula ve kaydet** commits, closing discards unapplied edits, **Değişiklikleri geri al** reloads current values, **Varsayılan: Low** applies and saves Low. **Özgün sahne (geçici)** restores runtime-entry LOD/light-resolution/camera/renderer state without overwriting preferences. This is distinct from High. Applying any profile exits this temporary override. Actual and staged state are displayed separately. Unsupported pipeline features are unavailable; supported features still require authored volumes and build shader variants.

F9 capture and diagnostic isolation lock Apply/Reset/Restore. Capture schema 3 records full graphics state, authored override, registered groups and an evaluated Volume shadow distance. No capture starts automatically. Existing A/B override commands remain diagnostic only.

## Safety and changed files

Branch at start: `s012-integrated-graybox-slice`, HEAD `3fa3161f0d3d22f40d422fd5b404605f25088ce0`. Dirty workspace inventory and 13 before-images: `Checkpoints/PERF-S001-20261023T091106112441Z/`. `manifest.json` hashes the backups; `new-files.json` enumerates new implementation assets. This sprint supersedes the older isolated bool tree-shadow UI documented in `TREE-SHADOW-SETTING.md`.

Modified existing runtime: ForestTreeShadowSettings, ForestPrototypeState, ForestAuthoringState, ForestPrototypeCapture, SessionUi, SessionFlow, ResourceNodeView. Modified editor authoring: ForestPrototypeTerrain, ForestDistribution. Modified scenes: ForestPrototype120 and Production/FirstTownCore — only serialized tree-root references added. Existing scene object counts, distribution, transforms, GUIDs, materials and terrain data remain intact.

Added editor diagnostics: GraphicsSettingsDiagnostics. Added EditMode/PlayMode graphics tests; PlayMode asmdef adds Core/HDRP references. Existing `Tools/HDRPProductionOptimization/manual_checks.py` adds `graphics-edit` / `graphics-play` actions. No pipeline, project quality asset, vendor prefab/material or third-party source changed.

Rollback: stop Play Mode, compare current files against timestamped before-images and restore only this sprint's changes; do not blanket reset the dirty worktree. Remove new assets only after reverting their consumers, preserving unrelated later edits. Saved graphics preferences can remain or be reset through the menu.
