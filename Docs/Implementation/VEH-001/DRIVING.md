# VEH-001 Driving / Presentation — 2026-10-06

Status: IN_PROGRESS. New code imports and authoring commands compile in Unity; focused/full regressions and build have not yet been run for this revision. The previous full PlayMode checkpoint exists: 241/241 PASS in `Evidence/20261005T152812-451274Z-play`. Earlier full EditMode is 488/488 PASS; focused is 56/56. These are historical results, not new presentation acceptance.

## Authority and disposition

| Area | Disposition | Current work |
|---|---|---|
| Domain, fuel, refuel, cargo, impact gating | KEEP | Existing authority and tests retained |
| Noise / pressure | KEEP | Canonical noise path; resident scope |
| Occupancy, safe exit, save/load | EXTEND | Same player/seat authority; camera near clip restored on exit |
| Input / MotionCore brake safety | KEEP | Service brake remains distinct from reverse |
| Wheel spin / steer / suspension | KEEP / validate | MotionCore `UpdateWheelVisuals` uses actual `GetWorldPose`; sole visual writer |
| Interior steering | EXTEND | Last Signal presenter follows actual FL/FR steering |
| Cockpit | FIX | Measured framing; seat/eye share XZ; bounded driving look |
| Audio / lights | EXTEND | Load layer and rolling bed; semantic brake/reverse lamps |
| Surface response | EXTEND | MotionCore surface provider; resident gravel and wet grip multipliers |
| Dust / driver hands | NOT_IMPLEMENTED | Optional dust deferred; hands are a deferred presentation enhancement |

Per the 2026-10-06 closure request, acceptance is the resident area. Seamless portal driving is outside this closure; it is not claimed as implemented.

## Geometry

Source geometry was read from Unity Mesh bounds and actual prefab transforms. Detailed collider values are in `Evidence/20261006-driving-audit/geometry.txt`.

| Mapping | Root-local collider center (m) | Visual |
|---|---|---|
| FL | (-1.0000, 0.4951, 1.3561) | FL_Mesh |
| FR | (0.9500, 0.4951, 1.3561) | FR_Mesh |
| RL | (-1.0000, 0.4951, -1.2939) | BL_Mesh |
| RR | (0.9500, 0.4951, -1.2939) | BR_Mesh |

- Physics root and colliders: scale (1,1,1). Source visual child: uniform 0.5 scale.
- Wheelbase 2.65 m; front and rear track 1.95 m.
- Tire mesh diameter 1.0232 m, width 0.4990 m; collider radius 0.51 m (about 1.6 mm below mesh radius).
- Body mesh bounds 2.2832 × 1.8072 × 4.4306 m. Authored body-mesh bottom is about 0.453 m above root; loaded runtime ground clearance still requires physical measurement.
- Player capsule: height 1.8 m, radius 0.3 m, skin 0.025 m.
- Mesh pivots differ from tire centers by under 8 mm. No destructive source mesh edit or extra visual spin multiplier.

Validation rejects missing/duplicate wheel or visual references, nonpositive radius/suspension, scaled wheel physics, wrong steering axle, missing seat/camera/steering pivot, and nonpositive Rigidbody mass. No vendor demo controller is on the owned pickup.

## Steering and cockpit

FL/FR are the only steering axle. MotionCore owns speed attenuation and rate limiting. Its existing visual update writes all four collider world poses, including spin and suspension. `VehicleSteeringWheelPresenter` runs in LateUpdate with no extra steering smoothing. It maps actual mean front steer ±32° to exposed visual ±270°.

The steering rim's source-local center was measured at approximately (-0.0007464, 0.0893975, -0.251958). A wrapper pivot preserves the source mesh; its rotation axis (0, 0.3310264, -0.9436214) comes from the mesh plane. Positive road steer rotates the wheel clockwise from the driver's view. The chosen ±270° range is presentation only.

Final root-local seat: (-0.53, 0.85, -0.10); eye: (-0.53, 1.75, -0.10). Keeping their XZ equal prevents camera orbit on yaw. Driving yaw is limited to ±110°, pitch to ±60°; on-foot limits and FOV policy remain in FirstPersonLook. Near clip is 0.05 m while seated and is restored on exit. No additional shake or driving-arm rig was added.

Static authoring images are in the audit folder. `cockpit-clear-eye.png` is the final eye position; earlier `cockpit-reframed-*` images show a rejected seat intersection and must not be used as final acceptance. Static pose fixtures validate framing/pivot inspection only, not running suspension, wheel spin or human acceptance. An initial preview cleanup emitted a render-target release error; the preview cleanup was corrected and later captures completed without that error.

## Handling baseline

| Parameter | Authored value |
|---|---:|
| Mass | 1800 kg |
| Center of mass, root-local | (0, 0.65, 0) m |
| Drive | Rear-wheel drive |
| Total motor torque | 2000 Nm |
| Service brake torque | 3200 Nm per wheel |
| Handbrake torque | 4500 Nm, rear axle |
| Reverse torque fraction | 0.45 |
| Forward torque cutoff | 64.8 km/h |
| Reverse torque cutoff | 18 km/h |
| Steering low / high speed | 32° / 8° |
| Turn / return rate | 100 / 200 deg/s |
| Suspension travel | 0.25 m |
| Spring / damper | 35000 / 4500 |
| Wheel mass | 25 kg |
| Anti-roll | 4500 per axle |
| Handbrake lateral grip fraction | 0.9 |
| Downforce acceleration coefficient | 2 |

These measured serialized settings are retained as the utility-pickup baseline, not a handling PASS. Practical top speed, acceleration, loaded ride height, braking distance and curb recovery are NOT_MEASURED. No velocity clamp or second tire simulator was introduced. MotionCore's wet/dirt multipliers affect its existing WheelCollider friction path.

## Surface, lights and audio

The resident route contains a 100 × 10 m hardstanding strip centered (-300,-125), a 20 m gravel segment, a 12 cm raised curb at x=-277 and a parking bay near x=-258. Gravel grip multipliers are 0.85 forward / 0.78 lateral. Rain presentation blends a further 1→0.9 forward and 1→0.85 lateral multiplier using the existing WorldClock. These values require manual handling assessment. A low-speed impact post is at (-255,-130).

Brake lamps follow service/handbrake state and MotionCore's forward deceleration during reverse request; reverse lamps require engine-enabled reverse state, not low speed. Existing headlights retain their toggle. Lens meshes and materials are owned assets.

Nox idle + 2000 RPM recording crossfade from wheel RPM and semantic drive load. MotionCore has no engine-RPM/gear simulation, so the blend is explicitly a presentation approximation. Rolling playback requires ground contact and wheel motion. Gameplay noise remains independent. Dedicated gravel foley and dust are not authored; gravel's present distinction is grip. Existing impact cue remains the Nox trunk-impact recording. HUD text refresh is bounded to 4 Hz; it still allocates on refresh, so total vehicle GC is not claimed to be zero.

## Verification awaiting user runs

`Tools/veh001-verify.py focused` now includes `VehiclePresentationMathTests` and `VehiclePresentationTests`. The latter uses the real owned prefab and MotionCore to check four-wheel mapping, invalid/duplicate references, brake/reverse telemetry, forward/reverse spin direction, actual wheel-pose synchronization and cockpit steering. Existing tests remain unchanged.

After focused, full EditMode, full PlayMode and Development build are required for this driving revision. Performance capture now records wheel, steering, camera and audio markers in addition to physics/update/noise. `Physics.Simulate` covers the scene, not vehicle-exclusive solver cost. GC is the whole-frame counter; unavailable counters are reported as such. Manual visible acceptance: NOT_RUN. Follow `MANUAL-DRIVING-ACCEPTANCE.md` after the build passes.

For build-only exterior observation, `-veh001Inspect` explicitly installs a Development inspection camera. 8 toggles cockpit/exterior while occupied; 9 cycles four wheel/body angles. It does not write Rigidbody or controls and restores the original camera on exit. Ordinary launches retain the existing first-person camera only.
