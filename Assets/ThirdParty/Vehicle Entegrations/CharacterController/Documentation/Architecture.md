# MotionCore Vehicle Basic Architecture

MotionCore Vehicle Basic is the foundation package. It provides a real wheel-physics
vehicle controller built on Unity `WheelCollider`s, plus shared input, camera, and
demo tooling.

Arcade and simulation handling live in separate paid plugins (MotionCore Vehicle
Arcade and MotionCore Vehicle Simulation). Each plugin depends on this package and
subclasses `VehicleControllerBase` rather than duplicating it.

## High-Level Structure

```text
VehicleControllerBase   (WheelCollider driving model)
  BasicCarController     (free concrete controller)

VehicleAxle             (left/right WheelCollider + visuals, per axle)
DrivetrainLayout        (RearWheelDrive / FrontWheelDrive / AllWheelDrive)

VehicleInput        -> VehicleControllerBase.SetInput(CarInput)
VehicleChaseCamera  -> reads VehicleControllerBase public state
DemoVehicleFactory  -> builds a wired WheelCollider vehicle (editor)
VehicleDemoSceneBuilder -> builds the playable demo scene (editor)
```

## Namespaces And Assemblies

```text
MotionCore.Vehicle.Core         (runtime)
MotionCore.Vehicle.Controllers  (runtime)
MotionCore.Vehicle.Input        (runtime)
MotionCore.Vehicle.Camera       (runtime)
MotionCore.Vehicle.Editor       (editor)
```

The runtime code compiles into the `MotionCore.Vehicle.Core` assembly definition,
and the editor tooling into `MotionCore.Vehicle.Core.Editor`. The runtime assembly
references `Unity.InputSystem`.

Add-ons should use their own namespaces and assemblies, for example:

```text
MotionCore.Vehicle.Arcade
MotionCore.Vehicle.Simulation
```

## Core Types

### `CarInput`

`CarInput` is the input contract for all vehicle controllers:

- `Throttle`
- `Brake`
- `Steer`
- `Handbrake`
- `Nitro`

This keeps vehicle physics independent from a specific input package. The basic
controller acts on every field except `Nitro` (which only the Arcade plugin uses).

### `VehicleInput`

`VehicleInput` reads keyboard and gamepad input and sends a `CarInput` value to the
vehicle each frame. It only requires a `VehicleControllerBase` on the same
GameObject and does not know which concrete controller is attached.

### `VehicleAxle`

`VehicleAxle` describes one axle:

- `leftWheel`, `rightWheel` — the `WheelCollider`s
- `leftVisual`, `rightVisual` — the visual wheel transforms
- `steering` — whether this axle steers
- `handbrake` — whether the handbrake locks this axle
- `antiRollForce` — anti-roll bar strength for this axle

Axles are stored in the controller in front-to-rear order.

### `DrivetrainLayout`

Selects which axle(s) receive motor torque:

- `RearWheelDrive` — drives the rearmost axle (default)
- `FrontWheelDrive` — drives the first axle
- `AllWheelDrive` — drives every axle

### `VehicleControllerBase`

`VehicleControllerBase` owns the entire WheelCollider driving model:

- rigidbody initialization and center of mass
- motor torque distribution across the driven wheels
- brake torque, reverse, handbrake, and idle drag
- real steering (`WheelCollider.steerAngle`) with speed falloff and smoothing
- per-axle anti-roll bars
- speed-scaled downforce
- grounded state and drift detection from real tire slip
- wheel visual posing via `WheelCollider.GetWorldPose`

Suspension and tire friction are configured on the `WheelCollider` components
themselves (the demo factory sets sensible defaults).

### `BasicCarController`

`BasicCarController` is the free concrete controller. It uses the base driving model
as-is — all tuning is exposed on the base class inspector fields. It exists so the
package ships a usable controller you can drop on a GameObject.

### `VehicleChaseCamera`

`VehicleChaseCamera` follows any `VehicleControllerBase`. It uses target speed for
FOV blending, drift state for a side-look offset, and the target transform for
follow position and look direction. It does not modify vehicle physics.

## Physics Flow

Each `FixedUpdate`, `VehicleControllerBase`:

1. Updates motion state (forward speed, grounded wheel count).
2. Applies steering to the steering axles.
3. Applies motor / brake / reverse / handbrake torque to the wheels.
4. Applies per-axle anti-roll forces.
5. Applies speed-scaled downforce.
6. Updates drift state from tire sideways slip.

Each `Update`, it poses the visual wheels from `WheelCollider.GetWorldPose`.

## Extension Points

Plugins subclass `VehicleControllerBase` and override these hooks:

- `EvaluateMotorTorque(throttle, normalizedSpeed)` — total drive torque to split
  across the driven wheels (e.g. nitro, torque curves, gearing).
- `EvaluateSteerAngle(steerInput, normalizedSpeed)` — front steer angle (e.g.
  sharper arcade steering, tighter sim steering).
- `DriftSlipThreshold` — sideways slip at which the car counts as drifting.

```csharp
using UnityEngine;
using MotionCore.Vehicle.Core;

public sealed class CustomVehicleController : VehicleControllerBase
{
    [SerializeField] private float boost = 1.5f;

    protected override float EvaluateMotorTorque(float throttle, float normalizedSpeed)
    {
        float torque = base.EvaluateMotorTorque(throttle, normalizedSpeed);
        return InputState.Nitro ? torque * boost : torque;
    }
}
```

Shared abstractions stay in the free core. Add-ons should not duplicate `CarInput`,
`VehicleControllerBase`, `VehicleAxle`, or the camera/input components.

## Surface Support

`IVehicleSurfaceProvider` is an extension point for surface-aware grip (asphalt vs
grass vs sand vs ice). Implement it on a component on the vehicle; the controller
auto-detects it on Awake (or set it with `SetSurfaceProvider`) and queries it each
physics step for every grounded wheel:

```csharp
public interface IVehicleSurfaceProvider
{
    SurfaceProperties GetSurface(WheelCollider wheel, in WheelHit hit);
}
```

`SurfaceProperties` returns `forwardGripMultiplier` and `sidewaysGripMultiplier`
relative to each wheel's authored friction (1 = unchanged). The controller combines
these with the handbrake grip factor and applies the result to the WheelColliders.

The free core does not ship a concrete provider — detecting the surface (physics
material, terrain texture, tags, etc.) is left to the game using the package.
