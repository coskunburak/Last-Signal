# MotionCore Vehicle Basic Tuning Guide

The vehicle is tuned in two places:

1. **The controller fields** (`BasicCarController` / `VehicleControllerBase`) — how
   much torque, brake, steering, and stability force the car gets.
2. **The `WheelCollider` components** — suspension and tire friction.

Get the WheelCollider suspension and a low center of mass right first; most "the car
feels wrong" problems come from those, not from the controller numbers.

## Controller Fields

### Body

| Field | What it does | Tuning advice |
| --- | --- | --- |
| `Center Of Mass` | Transform used as the rigidbody center of mass. | Keep it low and central — the main fix for tipping/rolling. |
| `Body Mass` | Rigidbody mass, applied on Awake. | `1100`–`1500` for a car. Heavier = more planted, slower. |

### Drivetrain

| Field | What it does |
| --- | --- |
| `Drivetrain` | Which axle(s) get motor torque (RWD / FWD / AWD). |
| `Max Motor Torque` | Total drive torque split across the driven wheels. |
| `Max Brake Torque` | Braking torque applied to all wheels. |
| `Reverse Torque Scale` | Fraction of motor torque used for reversing. |
| `Handbrake Torque` | Brake torque on the handbrake axle when handbrake is held. |
| `Handbrake Grip Factor` | Fraction of rear cornering grip kept while the handbrake is held (lower = the rear slides out more easily). |
| `Idle Brake Torque` | Light drag applied when there is no throttle or brake. |
| `Max Speed Kph` | Soft forward speed cap (motor cuts out above it). |
| `Max Reverse Speed Kph` | Reverse speed cap. |

### Steering

| Field | What it does |
| --- | --- |
| `Steer Angle At Zero Speed` | Steering angle (degrees) when parked/crawling — near full lock. |
| `Steer Angle At Top Speed` | Steering angle (degrees) at top speed — small, for stability. |
| `Steer Input Rate Deg Per Sec` | How fast the wheels can turn toward the target angle. |
| `Steer Return Rate Deg Per Sec` | How fast the wheels return toward center (usually faster). |

### Stability

| Field | What it does |
| --- | --- |
| `Downforce` | Speed-scaled downward force for high-speed grip. |
| Axle `Anti Roll Force` | Per-axle anti-roll bar strength (resists body roll). |

## WheelCollider Fields

Set on each `WheelCollider` component.

| Field | What it does | Increase when | Decrease when |
| --- | --- | --- | --- |
| Suspension Spring – `Spring` | Suspension stiffness. | The car bottoms out or sits too low. | The ride is too stiff/bouncy. |
| Suspension Spring – `Damper` | Suspension damping. | The car bounces or oscillates. | The suspension feels sticky. |
| `Suspension Distance` | Total travel. | Wheels can't reach uneven ground. | The body sits too high. |
| Suspension Spring – `Target Position` | Rest point along the travel. | You want the body to sit higher. | You want it to sit lower. |
| Forward Friction – `Stiffness` | Drive/brake grip. | Wheels spin or lock too easily. | You want more wheelspin. |
| Sideways Friction – `Stiffness` | Cornering grip. | The car slides in corners. | You want it to slide/drift more. |

## Tuning Recipes

### Car feels too slow

- Increase `Max Motor Torque`.
- Increase `Max Speed Kph`.

### Car tips over or rolls in corners

- Lower the `Center Of Mass` transform.
- Increase the axle `Anti Roll Force`.
- Increase `Downforce`.
- Stiffen the suspension `Spring`.

### Car understeers (won't turn enough)

- Increase front-wheel Sideways Friction `Stiffness`.
- Slightly lower rear Sideways Friction `Stiffness`.
- Increase `Steer Angle At Zero Speed` (and `Steer Angle At Top Speed` for fast corners).
- Move the `Center Of Mass` slightly forward.

### Car oversteers / spins out

- Increase rear Sideways Friction `Stiffness`.
- Lower `Steer Angle At Top Speed` so it calms down at speed.
- For RWD, lower `Max Motor Torque` to reduce power-on slides.

### Handbrake slides too little / too much

- For bigger slides, lower `Handbrake Grip Factor` (e.g. `0.35`).
- For tighter, more controllable slides, raise it (e.g. `0.65`).
- Raise `Handbrake Torque` if the rear does not lock enough.

### Suspension bounces or bottoms out

- Increase `Damper` to stop bounce.
- Increase `Spring` or `Suspension Distance` to stop bottoming out.

### Wheels sink into the ground

- Make sure each `WheelCollider` radius matches the visual wheel and the colliders
  are positioned so the wheels reach the ground at the spawn height.
- Raise the suspension `Spring` so it holds the body weight.

### Steering feels twitchy at speed

- Lower `Steer Angle At Top Speed`.
- Lower `Steer Input Rate Deg Per Sec` for smoother, slower wheel movement.

## Add-on Tuning

Nitro, drift assists, custom tire curves, weight transfer, and gearing live in the
paid plugins (MotionCore Vehicle Arcade and MotionCore Vehicle Simulation), which
subclass `VehicleControllerBase` and override its torque/steering/drift hooks. The
free core intentionally keeps to a clean WheelCollider baseline.
