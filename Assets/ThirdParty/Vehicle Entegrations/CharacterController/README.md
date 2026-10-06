# MotionCore Vehicle Basic

MotionCore Vehicle Basic is a free, standalone car controller for Unity, built on
real wheel physics using Unity `WheelCollider`s — per-wheel suspension, tire
friction, motor torque, brake torque, and real speed-sensitive steering, not a
faked rigidbody-force model.

The feel target is a credible, fun "drive around an open world" car (think GTA-style):
real weight and body roll, planted grip, accessible handbrake slides, and forgiving
recovery — not a precision racing sim. It is designed to be extended by add-ons
without modifying the core.

## Requirements

- **Unity 6 (6000.0) or newer** (uses `Rigidbody.linearVelocity` / `linearDamping`).
- **Input System package** (`com.unity.inputsystem`) for the `VehicleInput` component.
  The driving controller itself has no input dependency — you can drive it from your
  own input by calling `SetInput(CarInput)`.
- The demo scene's materials use the **Universal Render Pipeline** shader, with an
  automatic fallback to the Built-in `Standard` shader.

This package has **no third-party dependencies** and does **not** depend on any
add-on. It compiles and runs entirely on its own.

## What's Included

- a `WheelCollider`-based vehicle controller (`VehicleControllerBase`)
- a ready-to-use concrete controller (`BasicCarController`)
- axle model with selectable drivetrain (RWD / FWD / AWD)
- motor torque on the driven axle and brake torque on all wheels
- real steering with speed-based falloff and rate-limited wheel movement
- reverse, handbrake (rear-axle lock + grip drop for slides), and idle drag
- per-axle anti-roll bars for roll stability
- speed-scaled downforce
- slip-based drift detection (from real tire sideways slip)
- wheel visuals driven by `WheelCollider.GetWorldPose`
- keyboard and gamepad input through the shared `CarInput` contract
- a chase camera with switchable views (Chase / Far / Hood, cycled with **C**)
- a surface-grip extension point (`IVehicleSurfaceProvider`)
- an editor demo-scene builder
- assembly definitions for the core (runtime + editor)
- setup, architecture, tuning, and demo documentation

## Folder Layout

```text
Assets/CharacterController
  Documentation/
    Architecture.md
    DemoScene.md
    SetupGuide.md
    TuningGuide.md
  Scripts/
    MotionCore.Vehicle.Core.asmdef
    Camera/
      VehicleChaseCamera.cs
    Controllers/
      BasicCarController.cs
    Core/
      CarInput.cs
      IVehicleSurfaceProvider.cs
      VehicleControllerBase.cs
    Editor/
      MotionCore.Vehicle.Core.Editor.asmdef
      DemoVehicleFactory.cs
      VehicleDemoSceneBuilder.cs
    Input/
      VehicleInput.cs
  README.md
```

## Quick Start

The fastest way to see a working car is the demo scene. From the Unity menu:

```text
Tools > MotionCore Auto > Build Vehicle Demo Scene
```

This spawns a fully wired vehicle — rigidbody, body mesh, four `WheelCollider`s on
two axles with tuned suspension and friction, wheel visuals, input, and a chase
camera — and saves it to `Assets/Scenes/VehicleDemo.unity`. Press Play and drive.

To build a car by hand, see the [Setup Guide](Documentation/SetupGuide.md): a root
`Rigidbody` + `BasicCarController`, four child `WheelCollider`s assigned into the
controller's axle array, a visual transform per wheel, and a body collider.

## Controls

| Input | Action |
| --- | --- |
| `W` / `S` (or triggers) | Throttle / brake & reverse |
| `A` / `D` (or left stick) | Steer |
| Space (or south button) | Handbrake |
| `C` | Switch camera view |

(`Nitro` exists in the input contract but the basic controller ignores it.)

## Public API

For game code (HUD, audio, gameplay) the controller exposes:

- `SpeedKph`, `ForwardSpeed` (m/s, signed), `NormalizedSpeed`
- `IsGrounded`, `GroundedWheelCount`, `IsDrifting`
- `SetInput(CarInput)` — feed input from any source
- `SetSurfaceProvider(IVehicleSurfaceProvider)` — drive surface-aware grip

## Extending It (Add-ons / Advanced)

The controller is built to be subclassed without editing the core. A plugin or
project controller inherits `VehicleControllerBase` and overrides hooks:

- `EvaluateMotorTorque(throttle, normalizedSpeed)` — torque curves, nitro, gearing
- `EvaluateSteerAngle(steerInput, normalizedSpeed)` — steering feel
- `DriftSlipThreshold` — when the car counts as drifting
- `FixedUpdate` (call `base.FixedUpdate()`) — add custom forces/assists

Surface-aware grip (asphalt vs grass vs ice) is added by implementing
`IVehicleSurfaceProvider` on the vehicle — no concrete provider ships with the core.

`protected` members (`Body`, `InputState`, `Axles`, `Drivetrain`, `CurrentSteerAngle`,
`MaxMotorTorque`) give subclasses what they need. See
[Architecture](Documentation/Architecture.md) for details.

## Documentation

- [Setup Guide](Documentation/SetupGuide.md)
- [Architecture](Documentation/Architecture.md)
- [Tuning Guide](Documentation/TuningGuide.md)
- [Demo Scene](Documentation/DemoScene.md)
