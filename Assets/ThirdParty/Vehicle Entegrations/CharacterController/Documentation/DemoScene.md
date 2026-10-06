# Basic Demo Scene

The free demo scene proves the WheelCollider controller can be dropped into a Unity
scene and driven immediately.

Scene path:

```text
Assets/Scenes/VehicleDemo.unity
```

## What The Scene Contains

- a flat asphalt driving surface
- side barriers
- lane markers
- one playable WheelCollider vehicle (`BasicCarController`, rear-wheel drive)
- a chase camera assigned to the vehicle
- directional lighting

The scene uses Unity primitives and does not depend on external art assets.

## How To Rebuild

From the Unity menu:

```text
Tools > MotionCore Auto > Build Vehicle Demo Scene
```

This runs `VehicleDemoSceneBuilder.BuildDemoScene`, which uses the shared
`DemoVehicleFactory` to spawn the vehicle and saves the scene to
`Assets/Scenes/VehicleDemo.unity`. Running it again overwrites the scene.

## Controls

Keyboard:

| Control | Action |
| --- | --- |
| `W` / Up Arrow | Throttle |
| `S` / Down Arrow | Brake / reverse |
| `A` / Left Arrow | Steer left |
| `D` / Right Arrow | Steer right |
| Space | Handbrake |
| Shift | Nitro* |
| C | Switch camera view (Chase / Far / Hood) |

Gamepad:

| Control | Action |
| --- | --- |
| Right Trigger | Throttle |
| Left Trigger | Brake / reverse |
| Left Stick X | Steering |
| South Button | Handbrake |
| Right Shoulder | Nitro* |

\* Read into the shared `CarInput` but ignored by `BasicCarController`; it takes
effect with the Arcade plugin installed.

## Demo Vehicle Layout

```text
Demo Basic Vehicle
  Rigidbody
  BoxCollider              (body)
  BasicCarController
  VehicleInput
  Center Of Mass
  Body
    Chassis
    Cabin
  FL Wheel Collider        (WheelCollider)
  FR Wheel Collider
  RL Wheel Collider
  RR Wheel Collider
  FL Wheel                 (visual, posed from the collider)
  FR Wheel
  RL Wheel
  RR Wheel
```

The front axle steers; the rear axle is driven (RWD) and is locked by the handbrake.

The Arcade and Simulation plugins each provide their own separate demo scene.
