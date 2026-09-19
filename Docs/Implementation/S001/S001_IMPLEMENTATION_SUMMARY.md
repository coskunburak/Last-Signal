# S001 implementation summary

## Starting point

Unity URP template with SampleScene, a template Input System asset, tutorial scripts and user-imported art. No pre-existing player/door/session code. Existing user package/settings edits and deleted HubForceResolve files were retained. No package or engine upgrades, networking, inventory, save, combat or AI implementation.

## Player-visible implementation

- CharacterController first-person player, mouse yaw/pitch (±85°), configurable FOV/sensitivity; normalized diagonal and analog magnitude.
- Walk 3.2 m/s, hold Shift sprint 5.5 m/s, C toggle crouch 1.6 m/s; crouch overrides sprint. Feet stay fixed as capsule/eye height change together. Head obstruction rejects stand, new C request succeeds after leaving obstruction.
- Serialized primitive parkour: steps, threshold, allowed/forbidden slopes, narrow corridor, crouch tunnel, ramp to a 3 m drop, bounded ground and hidden door.
- E press resolves the first blocking collider within 2.2 m; prompt and commit use the same resolver, with fresh validation on commit. Disabled/destroyed targets clear safely. No pickup system.
- Rotating door owns open/closed/busy state. Busy requests are rejected; physical obstruction pauses rotation and resumes after clearance. Box collider stays on the same visual leaf. Test counters expose accepted transitions without changing behavior.
- Escape pause/resume, uGUI menu return and new session. Pause/focus loss disables gameplay map, clears input and releases cursor. Resume requires neutral movement/buttons before gameplay resumes. World simulation freezes while paused/menu.
- Session owns a spawned player prefab; menu deactivates/destroys it and unsubscribes events. No persistent singleton, global bus or static runtime mutable lists.

## Implementation ownership

`Assets/LastSignal/Runtime/Player/`: input, motor, look, stance. `Runtime/Interaction/`: resolver/contract/door. `Runtime/Session/`: composition and HUD. Player and door are separate prefabs; acceptance scene combines the prefabs/fixtures and spawns player through SessionFlow.

`Assets/LastSignal/Editor/S001Project.cs`: actual CreateAssets, Validate, Build entry points. Uses Unity prefab/scene serialization APIs. Menu entries under Last Signal/S001; CLI wrapper `Tools/s001.sh`. Batch authoring refuses PlayMode or unsaved current scenes.

InputSystem_Actions is reused, with one new Pause binding and Interact changed from Hold to tap. Unused template actions remain, without implementing their gameplay. No PlayerInput duplication: PlayerInputReader owns and disposes its own cloned action asset and named subscriptions.

## Verification and corrections

First 14-test run: 12 PASS / 2 FAIL. Input Test Fixture in this installed package queues UnityTest input; bit-addressed key events sent in the same frame overwrote earlier queued key state. Corrected test fixture to queue a complete KeyboardState. Subsequent 14/14 passed. Additional authored-scene parkour, physical mouse/C action tests, and domain-reload-disabled 10-session test are separate result files. No production behavior was weakened to make tests pass.

Unity 6.5 deprecation warnings in initial test code were fixed using FindAnyObjectByType and the unsorted FindObjectsByType overload. Final source compile checks have no S001 C# warnings/errors. Build and visual status are recorded in S001_ACCEPTANCE.md.

## Git and preserved work

Rollback commit and exact initial status are in D001 and Evidence. No commit, push, merge or broad restore. All new Assets files/folders have .meta files; ignore excludes caches/builds, includes source and text evidence. Existing LFS binary policy unchanged; Unity YAML remains text. New code/assets are delivered in the working tree and must be included with their .meta files in the eventual commit.

Unity itself serialized additional defaults in DefaultVolumeProfile.asset and UniversalRenderPipelineGlobalSettings.asset, and created SceneTemplateSettings.json. These are recorded as Editor-generated integration changes. Existing dirty PC/Mobile pipeline assets, package files, build settings and imported assets were preserved. Full-tree diff-check reports pre-existing trailing whitespace in PackageManagerSettings.asset:39; S001's tracked input/ignore diff is clean.

Shared ownership risks: InputSystem_Actions and the acceptance scene authoring recipe; avoid concurrent scene regeneration. Existing URP global settings and project settings require coordination. Player and door features remain separately editable.

## Limitations / S002 boundary

MCP was unavailable; all Unity compilation/serialization/tests/build operations used CLI, not MCP. Human input feel, complete standalone manual route and Windows platform acceptance must remain separate from automated tests. No absolute FPS or zero-GC claim. No final art/audio, localization framework, gamepad certification or rebinding UI.

S002 owns item/inventory/first save. Do not begin expanding scope to compensate for unclosed S001 evidence gates. S002 entry is BLOCKED until mandatory remaining acceptance is recorded.

Card-by-card actual labor hours were not measured; no invented 40–60-hour completion claim. Evidence timestamps measure test/build runs only. Save/schema impact: N/A (no persistence).
