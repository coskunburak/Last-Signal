# S001 acceptance checklist — 20260917-S001

Scene: Assets/LastSignal/Scenes/S001Acceptance.unity. Default spawn (0, .05, 0), facing +Z. Keyboard/mouse. C is toggle, Shift is hold, E is tap, Escape is pause/resume. FOV 75° (Inspector 60–100), mouse sensitivity .12°/pixel, pitch ±85°. Pause freezes simulation; focus loss pauses until explicit resume.

## Serialized fixture route

| Fixture | World position / dimensions | Automated evidence |
|---|---|---|
| Ground | 40×40 m, top Y=0; perimeter walls | StepsThresholdWallAndDropUseRealCollision; YIntegratedParkourUsesAuthoredGeometry |
| Steps | X=-10, Z=3 / 4.5; top .15 / .40 m | 0.15 + 0.25 m rises passed |
| Allowed slope | X=-5, start Z=3, 30°, length 5 m | climb assertion passed |
| Steep slope | X=0, start Z=5, 60°, length 4 m | advancement/height limit passed |
| Corridor | X=5, Z=2.5–7.5, clear width 1 m | full traversal passed |
| Tunnel | X=10, Z=3–7, clear height 1.3 m | standing entry blocked; crouch traverse; stand rejected inside, allowed outside |
| Drop | X=-10, deck top 3 m, Z=19–23; access ramp starts Z=14 | ramp/deck access and grounded landing passed |
| Door / threshold | visible door hinge (-.65, .1, 1.8), threshold .1 m | open, threshold traversal, close in lifecycle test |
| Occlusion | hidden door at (14,0,15), wall Z=14.5 | commit rejected from front of wall |

## Build route (record actual observation; pending items are NOT_RUN)

| Check | Automated PlayMode | Standalone visual / manual |
|---|---|---|
| Application loads gameplay | PASS integrated scene | Pending final build |
| Cursor lock and UI unlock | Context set; build observation needed | NOT_RUN |
| Mouse look / pitch / yaw | Pitch clamp PASS; action test pending | NOT_RUN |
| WASD / diagonal / speed / sprint | PASS 30/60/120 Hz, 3.2/5.5/1.6 m/s | NOT_RUN |
| Steps, slopes, wall, threshold | PASS real authored scene | NOT_RUN |
| Crouch tunnel / rejected stand / clear stand | PASS real authored scene | NOT_RUN |
| Range / occlusion / disabled / destroyed targets | PASS | NOT_RUN |
| Open / close / busy / obstruction | PASS | NOT_RUN |
| Pause movement / camera / interact leakage | PASS | NOT_RUN |
| Focus loss / held-key neutral return | PASS | NOT_RUN (physical alt-tab route) |
| Ten gameplay/menu cycles | PASS with domain reload on and off | NOT_RUN |
| Hitch, input feel and warmed GC | NOT_RUN; no optimization claim | MANUAL_REQUIRED |

Windows build/runtime: BLOCKED, no Windows runtime device is connected. These macOS and PlayMode results do not certify Windows.
