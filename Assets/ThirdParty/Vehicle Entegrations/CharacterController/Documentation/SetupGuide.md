# MotionCore Vehicle Basic Setup Guide

This guide explains how to create a playable vehicle using the free MotionCore
Vehicle Basic package.

The controller is built on Unity `WheelCollider` physics. Suspension, tire grip,
motor torque, brake torque, and steering are all real wheel physics. The controller
applies `motorTorque`, `brakeTorque`, and `steerAngle` to the wheels and lets the
physics engine resolve the rest.

## Fastest Path: The Demo Builder

The quickest way to a working car is the editor menu:

```text
Tools > MotionCore Auto > Build Vehicle Demo Scene
```

This spawns a fully wired vehicle and a chase camera, and saves the scene to
`Assets/Scenes/VehicleDemo.unity`. Use it as a reference for manual setup.

## Manual Vehicle Hierarchy

```text
Player Vehicle            (Rigidbody + BasicCarController + body collider)
  Center Of Mass          (empty transform, used to lower the COM)
  Body                    (visual meshes only)
  FL Wheel Collider       (WheelCollider)
  FR Wheel Collider       (WheelCollider)
  RL Wheel Collider       (WheelCollider)
  RR Wheel Collider       (WheelCollider)
  FL Wheel                (visual transform, posed from the collider)
  FR Wheel
  RL Wheel
  RR Wheel
```

The four `WheelCollider`s must be children of the GameObject that holds the
`Rigidbody`. The visual wheel transforms are separate from the colliders — the
controller positions and rotates them every frame from `WheelCollider.GetWorldPose`.

## Required Components

On the vehicle root:

- `Rigidbody`
- a body collider (`BoxCollider`, mesh collider, or primitive set) that does **not**
  overlap the wheels
- `BasicCarController`

For player control, also add:

- `VehicleInput`

For a follow camera, add to a Camera object:

- `VehicleChaseCamera` (assign the vehicle controller as its `Target`)

## Rigidbody Setup

| Property | Suggested value | Notes |
| --- | ---: | --- |
| Mass | `1100`–`1500` | Also set on the controller's `Body Mass`, which is applied on Awake. |
| Linear Damping | `0`–`0.1` | Keep low; the wheels provide rolling resistance. |
| Angular Damping | `0`–`0.3` | Keep low; anti-roll bars and suspension handle stability. |
| Interpolation | `Interpolate` | Set automatically on Awake. |
| Collision Detection | `Continuous Dynamic` | Set automatically on Awake. |

Assign a low, central `Center Of Mass` transform on the controller. A low COM is the
single most important setting for stopping the car from tipping over.

## WheelCollider Setup

Each `WheelCollider` should be configured for the car's size. Reasonable defaults
for a ~1300 kg car (these match the demo factory):

| Field | Suggested value | Notes |
| --- | ---: | --- |
| Radius | `0.4` | Match the visual wheel radius. |
| Suspension Distance | `0.3` | Total suspension travel. |
| Center | `(0, 0, 0)` | Offset of the wheel within its travel. |
| Mass | `20` | Wheel mass. |
| Suspension Spring – Spring | `30000` | Higher = stiffer ride, less body roll. |
| Suspension Spring – Damper | `3800` | Higher = less bounce. |
| Suspension Spring – Target | `0.5` | Rest point along the travel. |
| Forward Friction – Stiffness | `1.5` | Longitudinal grip (drive/brake). |
| Sideways Friction – Stiffness | `1.7` | Cornering grip. |

Place the wheel colliders so that, with the body at its spawn height, the wheels
reach the ground. If the car sinks or bottoms out, raise the spring or reduce mass;
if it floats, lower the spring.

## Axle And Drivetrain Setup

On `BasicCarController`, fill the `Axles` array (front-to-rear). For a standard car:

- **Front axle** — assign FL/FR colliders and visuals, set `Steering = true`.
- **Rear axle** — assign RL/RR colliders and visuals, set `Handbrake = true`.

Set `Drivetrain` to `RearWheelDrive`, `FrontWheelDrive`, or `AllWheelDrive`. RWD
drives the rear axle, FWD drives the front axle, AWD drives both.

## Input Controls

`VehicleInput` supports keyboard and gamepad input. (This project uses the Input
System; the legacy-input path is compiled out.)

Keyboard:

| Control | Action |
| --- | --- |
| `W` / Up Arrow | Throttle |
| `S` / Down Arrow | Brake / reverse |
| `A` / Left Arrow | Steer left |
| `D` / Right Arrow | Steer right |
| Space | Handbrake |
| Shift | Nitro* |
| C | Switch camera view |

Gamepad:

| Control | Action |
| --- | --- |
| Right Trigger | Throttle |
| Left Trigger | Brake / reverse |
| Left Stick X | Steering |
| South Button | Handbrake |
| Right Shoulder | Nitro* |

\* Nitro is read into the shared `CarInput` but ignored by `BasicCarController`; it
takes effect with the Arcade plugin installed.

Steering wheel support is not part of the free core. Add wheel-specific input in
your project without changing the shared vehicle foundation.

## Camera Setup

1. Select your Camera.
2. Add `VehicleChaseCamera`.
3. Assign the car controller component to `Target`.
4. Edit the `Views` array — each view has a follow offset, look-ahead, and field of
   view. The player cycles through them with **C**. The defaults are Chase, Far, and
   Hood.

## Available Add-ons

Both plugins install on top of the free core and subclass `VehicleControllerBase`:

- **MotionCore Vehicle Arcade** — nitro, sharper steering, easier drift.
- **MotionCore Vehicle Simulation** — tighter, more planted handling, intended to
  grow into a full simulation model.
