cd "/Users/burakcoskun/Last Signal"

mkdir -p "Docs/Implementation/VEH-001"

pbpaste > "Docs/Implementation/VEH-001/VEH-ZMB-002-CONTINUATION-PROMPT.md"

# PROJECT LAST SIGNAL — VEH-ZMB-002
# EXACT CONTINUATION FROM CURRENT CHECKPOINT
## Vehicle Impact Dominance, Knockdown, Run-Over, Deterministic Performance Route & Final Production Closure

Continue the existing **VEH-ZMB-002 — Vehicle Impact Dominance, Knockdown & Run-Over** implementation for Project Last Signal.

This is a CONTINUATION.

Do NOT restart this sprint.

Do NOT redesign systems that have already been implemented and validated.

Do NOT repeat earlier architectural research unless the REAL current code contradicts this handoff.

All explanations, progress reports, manual instructions and final reports to Burak must be written in **Turkish**.

Code, technical identifiers, file names, paths, API names and commands remain English.

---

# 1. MOST IMPORTANT CONTINUATION RULE

The current repository contains extensive uncommitted S013/S014/vehicle/animation work.

Preserve everything.

Known previous state:

```text
Branch:
s012-integrated-graybox-slice

HEAD observed by previous session:
f9299ef6
```

VERIFY these first because HEAD may have changed.

Previous session observed approximately:

```text
3258 working-tree changes
```

Do NOT run destructive commands such as:

```text
git reset --hard
git checkout .
git clean -fd
```

Do not revert unrelated changes.

---

# 2. RECOVER CURRENT STATE FIRST

Start with:

```bash
cd "/Users/burakcoskun/Last Signal"

git status --short
git branch --show-current
git rev-parse HEAD
git diff --stat
```

Then inspect only relevant VEH-ZMB/vehicle/zombie/performance diffs.

In particular inspect:

```text
VehiclePerformanceCapture.cs
VehicleActor.cs
VehicleActor.Impact.cs
VehicleImpactGate.cs
VehicleInfectedContactResponse.cs
VehicleZombieInspection.cs
ZombieImpactReaction.cs
ZombieAnimationPresenter.cs
ZombieController.cs
ZombieHealth.cs
ZombieDefinition.cs
VehicleZombieDominanceTests.cs
related VEH-ZMB tests
Tools/veh001-performance.py
Tools/veh001-verify.py
```

Do not assume filenames blindly if current project differs.

---

# 3. HISTORICAL VERIFIED CHECKPOINT

Before the latest VEH-ZMB-002 work the project had verified:

```text
VEH focused:
68 / 68 PASS

Full EditMode:
495 / 495 PASS

Full PlayMode:
246 / 246 PASS
```

Clean Development build:

```text
PASS
Errors = 0
Warnings = 396
```

These are HISTORICAL comparison points.

Do NOT reuse them as final proof after new changes.

Final test counts may be larger.

---

# 4. ORIGINAL PLAYER-VISIBLE PROBLEM

The major defect was:

> A normal zombie could physically push or heavily stop the pickup, making the zombie appear stronger than the car.

This was unacceptable.

The intended relationship is:

```text
1800 kg heavy utility pickup
>>
ordinary human-sized infected
```

during meaningful vehicle impact.

---

# 5. ROOT CAUSE ALREADY IDENTIFIED

The production pickup is:

```text
1800 kg
dynamic Rigidbody
MotionCore / WheelCollider vehicle
```

The normal infected walking body does NOT behave like an ordinary ~80 kg dynamic Rigidbody.

Its main locomotion authority is based around:

```text
NavMeshAgent
+
Transform-driven movement / locomotion collider
```

This caused Unity physics to encounter the zombie more like a transform/kinematic positional obstacle than a lightweight human body.

In practical terms:

```text
Zombie locomotion authority
fought
Vehicle Rigidbody authority
```

which produced the absurd result:

```text
pickup hits zombie
→ zombie keeps positional authority
→ zombie can push/block pickup
```

DO NOT rediscover this from scratch.

Verify current implementation and continue.

---

# 6. CHOSEN PRODUCTION SOLUTION

The correct solution was NOT:

```text
mass = 100000 kg
```

or:

```text
infinite motor torque
```

or:

```text
disable all zombie collision
```

The implemented architecture instead transitions authority:

```text
Normal Zombie AI Locomotion
        ↓
Valid Strong Vehicle Impact
        ↓
Zombie locomotion authority suspended
        ↓
VehicleImpactReaction
        ↓
Fall / Knockdown / Death
        ↓
If alive:
Get-up
        ↓
AI locomotion restored

If dead:
NO recovery
```

Preserve this architecture.

---

# 7. VEHICLE PHYSICAL DOMINANCE ALREADY IMPLEMENTED

The previous implementation introduced vehicle/infected-specific physical handling so the infected cannot feed unrealistic positional impulse back into the heavy pickup during a valid impact.

The fix is specifically scoped to:

```text
vehicle ↔ infected
```

World obstacles such as:

```text
walls
trees
rocks
buildings
```

must continue using normal physics.

Do not generalize the zombie dominance correction to world geometry.

---

# 8. IMPORTANT MEASURED RESULT

An isolated real physics measurement already produced:

```text
Pickup speed before impact:
~18.0 m/s

Pickup speed after one normal infected:
~15.72 m/s
```

Approximate retention:

```text
~87%
```

This is an excellent isolated proof that the pickup now dominates a single human-sized target physically.

BUT this is NOT final production proof.

It still needs confirmation with:

```text
real MotionCore
real wheels
real production zombie AI
real NavMesh
real production scene
real build
```

Do NOT hard-code an 87% requirement.

The production requirement is:

> One ordinary infected must not remove the majority of pickup momentum.

---

# 9. CANONICAL DAMAGE PATH

Vehicle impacts already use:

```text
ZombieHealth.TakeDamage
```

as canonical health authority.

Preserve it.

Do NOT implement:

```text
Destroy(zombie)
```

or a separate vehicle-kill system.

Correct authority remains:

```text
vehicle impact
→ semantic damage
→ ZombieHealth.TakeDamage
→ existing health
→ existing death
→ existing population/persistence
```

---

# 10. IMPACT DAMAGE IMPROVEMENTS ALREADY DONE

Previous work already corrected the impact calculation away from simple vehicle speed magnitude.

The current implementation should use meaningful collision-relative / contact-normal closing speed.

Additionally, where a Collision callback contains multiple contact points:

```text
the strongest meaningful contact-normal component
```

is selected.

Therefore contact-array ordering must not affect gameplay damage.

Preserve this.

---

# 11. IMPACT DEDUPLICATION ALREADY EXISTS

There is an existing:

```text
VehicleImpactGate
```

or equivalent.

Preserve:

```text
Vehicle A
+
Zombie B
+
one continuous contact episode
=
ONE semantic vehicle impact
```

A zombie with several colliders must remain one gameplay target.

Do not create damage per collider.

---

# 12. RE-HIT MUST REMAIN VALID

After:

```text
impact
→ physical separation
→ later valid second collision
```

the same surviving infected may receive another legitimate semantic hit.

Do not turn deduplication into permanent immunity.

---

# 13. DEAD TARGET SUPPRESSION ALREADY ADDRESSED

Dead/corpse contact must NOT generate repeated:

```text
health damage
death
vehicle condition cost
impact gameplay noise
population removal
```

Preserve this.

The initial lethal impact remains the main semantic impact.

---

# 14. INFECTED-SPECIFIC VEHICLE COST

Vehicle impact against an infected is no longer supposed to cost the same as hitting a concrete wall.

Preserve infected-specific condition damage.

Target balance:

```text
single normal zombie
→ small/moderate pickup condition cost

repeated high-speed mowing
→ meaningful accumulated wear
```

Vehicle combat must remain powerful but not free.

---

# 15. REAL PREFAB PHYSICS TESTS ALREADY PASSED

Real production pickup + real zombie prefab physics tests were created.

An earlier high-speed failure was diagnosed as a TEST FIXTURE issue:

```text
Transform moved
but Rigidbody physics position was not synchronized
```

Physics restored the old body pose.

The fixture was corrected by moving/synchronizing the Rigidbody correctly.

After that:

```text
4 / 4 real prefab collision tests PASS
```

covering approximately:

```text
low/parking behavior
medium impact
reverse impact
high-speed impact
```

Do not reintroduce Transform-only Rigidbody positioning.

---

# 16. VEH-ZMB DOMAIN TESTS

Previous VEH-ZMB work also reported:

```text
10 / 10 new domain tests PASS
```

Verify their real current files/evidence.

Do not duplicate them if coverage is already correct.

---

# 17. FALL / KNOCKDOWN ANIMATION ALREADY FOUND

The real project was searched for suitable vehicle-impact animations.

A strong full-body fall animation was found.

A long source clip containing a usable get-up sequence was also found.

Do NOT start searching/downloading new animation assets unless the current implementation is genuinely broken.

No full ragdoll sprint is required.

---

# 18. SURVIVOR VEHICLE-IMPACT REACTION ALREADY IMPLEMENTED

Current intended flow for a surviving infected:

```text
vehicle impact
↓
chase stopped
↓
attack stopped
↓
walking/locomotion authority suspended
↓
strong fall animation
↓
short down state
↓
get-up
↓
AI locomotion restored
```

This sequence already passed a PlayMode verification.

Preserve it.

---

# 19. NAVMESH RECOVERY BUG ALREADY FIXED

The first reaction implementation failed to preserve the NavMesh stop state correctly.

That bug was identified and fixed.

The subsequent test verified:

```text
fall
→ wait
→ get up
→ AI resumes
```

Do not regress this.

---

# 20. DEATH MUST ALWAYS WIN

If the zombie dies while VehicleImpactReaction is active:

```text
Death state
>
Recovery / Get-up state
```

The zombie must never:

```text
stand up
resume NavMesh
resume attack
resume hearing
```

after death.

Guard delayed animation/recovery callbacks.

---

# 21. SAVE/LOAD AFTER VEHICLE KILL

A production encounter vehicle-kill scenario was previously verified through save/load.

Reported behavior:

```text
vehicle impact / zombie death
+
vehicle condition change
↓
disk save
↓
load
```

passed.

Preserve canonical persistence.

Do NOT create:

```text
VehicleKilledZombieSaveList
```

or parallel persistence.

---

# 22. VEHICLE NOISE / ZOMBIE HEARING

An earlier gap was found:

Vehicle noise reached:

```text
World Pressure
```

but the zombie hearing validator originally accepted only older categories such as:

```text
footsteps
melee
gunshot
```

and rejected vehicle noise categories.

This was already being corrected.

Canonical path MUST remain:

```text
Vehicle
↓
GameplayNoiseSystem
├── Zombie Hearing
└── WorldPressureNoiseAdapter
```

Never:

```text
Vehicle → ZombieController.Alert()
```

directly.

Complete/verify this if still unfinished.

---

# 23. MOST IMPORTANT NEW DISCOVERY FROM LAST SESSION

The previous continuation prompt assumed a new development performance-driver abstraction might be needed.

The latest session inspected the actual code and discovered:

> THIS IS ALREADY SOLVED ARCHITECTURALLY.

`VehicleActor` already contains a:

```text
developmentControl
```

path.

`VehiclePerformanceCapture` already uses:

```text
SetDevelopmentControl(...)
```

through the canonical vehicle-control boundary.

Therefore:

DO NOT create a new:

```text
IVehicleDriveInputSource
```

or duplicate performance driver architecture unless real current code proves it is necessary.

This is now an important constraint.

---

# 24. ACTUAL PERFORMANCE ROOT CAUSE

The real performance problem was route design.

`VehiclePerformanceCapture` used roughly this 20-second driving sequence:

```text
forward
→ brake
→ reverse
→ steer left
→ steer right
```

approximately:

```text
6s forward
3s brake
4s reverse
3s left
4s right
```

This route is reasonable for general vehicle profiling.

But it is NOT suitable for deterministic zombie impacts.

---

# 25. ACTIVE-INFECTED TARGET PLACEMENT

`VehicleZombieInspection` places approximately five infected relative to the pickup.

Observed offsets include approximately:

```text
center z = 10
center z = 24
center z = 44
x ≈ 1.1, z ≈ 62
x ≈ 7, z ≈ 28
```

Intent:

```text
3+ direct collision targets
+
additional hearing/AI witnesses
```

Zombies use real production AI/prefabs.

---

# 26. PREVIOUS ACTIVE-AI PERFORMANCE RESULT

The previous build performance run showed:

```text
active_ai_count_min = 5
active_ai_count_max = 5
active_ai_count_end = 5
```

So active AI setup existed.

However:

```text
infected_impact_count_during_capture = 1
```

Acceptance requires:

```text
>= 3
```

Therefore the performance run correctly FAILED.

DO NOT lower this requirement.

---

# 27. ACTIVE-INFECTED PERFORMANCE METRICS FROM FAILED RUN

The previous failed active-AI run still produced useful data:

```text
frames=10902

frame_avg_ms=1.8346
frame_p50_ms=1.8175
frame_p95_ms=2.2425
frame_p99_ms=2.7756

distance_m=50.228
max_speed_mps=11.954

active_ai_count_min=5
active_ai_count_max=5
active_ai_count_end=5

infected_impact_count=1
infected_impact_count_during_capture=1

impact_contact_cache_end=0
```

Profiler markers included:

```text
LastSignal.Vehicle.ZombieImpact_mean_raw=346 ns
LastSignal.Zombie.AI_mean_raw=14341 ns
LastSignal.Zombie.Perception_mean_raw=7925 ns
LastSignal.Zombie.Navigation_mean_raw=2369 ns
LastSignal.Zombie.Hearing_mean_raw=66 ns
```

This is historical comparison evidence.

Do not treat it as PASS.

---

# 28. NORMAL PERFORMANCE FAILURE

The standard performance mode also previously failed:

```text
System.InvalidOperationException:
Pickup did not complete a measurable drive.
```

This still needs final investigation/verification.

Do NOT automatically assume actual player driving is broken.

This is the automated capture path.

---

# 29. EXACT LAST CODE CHANGE BEFORE TOKEN INTERRUPTION

The latest AI session edited:

```text
VehiclePerformanceCapture.cs
```

The intended change:

### Normal performance mode

Preserve existing multi-segment vehicle route.

### Active-infected mode

Use:

```text
continuous straight-forward driving
```

for the entire capture period instead of:

```text
forward → brake → reverse → turns
```

The purpose is for the pickup to pass through:

```text
z=10
z=24
z=44
...
```

infected targets and reliably produce at least three real impacts.

THIS CODE CHANGE WAS MADE.

BUT:

> It was NOT yet fully compiled/tested/built/performance-verified before model/token interruption.

This is the exact resume point.

---

# 30. LAST FILE BEING REVIEWED

Immediately after changing the active-infected route, the previous AI began reviewing:

```text
ZombieAnimationPresenter.cs
```

specifically the current:

```text
knockdown
fall
get-up
```

presentation.

The model interrupted while reading approximately two sections of that file.

No additional animation change after this review is confirmed by the handoff.

Therefore:

DO NOT assume there are unfinished animation edits.

Inspect the current file/diff.

---

# 31. YOUR FIRST REAL TASK

DO NOT re-plan VEH-ZMB-002.

Recover the exact final diff in:

```text
VehiclePerformanceCapture.cs
```

Verify the active-infected branch actually implements:

```text
continuous deterministic forward drive
```

while normal performance mode preserves the broader driving course.

Check compile safety.

---

# 32. VERIFY ZOMBIE ANIMATION PRESENTER

Finish the interrupted review of:

```text
ZombieAnimationPresenter.cs
```

Confirm the implemented vehicle-impact reaction uses the intended:

```text
strong fall
down state
get-up
```

presentation correctly.

Check especially:

```text
animation state ownership
fall transition
get-up timing
death override
```

Do NOT modify it simply because you are reviewing it.

Only fix proven defects.

---

# 33. FIRST VALIDATION AFTER RESUME

After confirming code state:

run the narrowest relevant tests first.

Use current test infrastructure.

If there is already a VEH-ZMB-specific mode, use it.

Otherwise run the precise affected tests through the existing Unity test tooling.

Do NOT start with full 246-test PlayMode suite.

Verify at minimum:

```text
VehiclePerformanceCapture route logic
VehicleZombieDominance tests
fall/get-up recovery
death override
real prefab impact
```

---

# 34. ACTIVE-INFECTED ROUTE EXPECTATION

The revised performance route must produce:

```text
measurable drive
+
real active AI
+
>= 3 semantic infected impacts
```

The vehicle must travel via normal:

```text
developmentControl
→ VehicleActor
→ MotionCore
→ Rigidbody/WheelCollider
```

No transform teleporting.

No velocity injection.

---

# 35. DO NOT FAKE THREE IMPACTS

Never increment the impact counter artificially.

Every counted impact must originate from:

```text
real production pickup physical contact
+
real production infected target
+
normal semantic VehicleImpact commit
```

No fake telemetry.

---

# 36. ACTIVE ZOMBIES MUST REMAIN REAL

Do not replace them with static collider dummies.

They must retain:

```text
ZombieHealth
real colliders
real AI
NavMesh
hearing/perception
```

Deterministic initial placement is acceptable.

---

# 37. IF STRAIGHT DRIVE STILL PRODUCES <3 IMPACTS

Do not immediately lower the gate.

Investigate:

```text
vehicle route
target spacing
target collider positions
AI moving out of lane
spawn timing
warm-up timing
vehicle steering drift
target registration
impact gate
```

Use telemetry.

Then make the smallest deterministic correction.

Possible safe changes include:

```text
adjust direct-hit spawn positions
adjust capture spawn timing
slightly constrain initial acceptance-target behavior
correct course geometry
```

Do not turn real zombies into fake dummies.

---

# 38. WARM-UP BEHAVIOR

Pay special attention to the order:

```text
vehicle warmup
→ active infected creation
→ measured capture
```

Ensure targets are created relative to the vehicle position at the CORRECT time.

Do not let:

```text
warmup forward movement
```

cause the vehicle to pass expected collision locations before measured capture begins.

Verify this explicitly.

---

# 39. NORMAL PERFORMANCE MODE

After active route work:

also fix/verify the standard performance mode.

Command:

```bash
python3 Tools/veh001-performance.py "<LATEST_BUILD>"
```

must no longer fail with:

```text
Pickup did not complete a measurable drive.
```

Determine whether the failure came from:

```text
route state
vehicle startup
developmentControl
capture timing
distance measurement
```

Do not weaken distance validation unless genuinely incorrect.

---

# 40. REAL-AI VEHICLE DOMINANCE

Once the deterministic route works, measure actual real-AI collision behavior.

For one ordinary infected:

capture:

```text
pickup speed immediately before impact
pickup speed shortly after impact
```

Confirm:

```text
pickup retains the majority of forward momentum
```

The isolated ~87% result is only reference.

Do not require exact 87%.

---

# 41. PARKED PICKUP DOMINANCE

Mandatory production validation:

```text
pickup parked
normal zombie walks into pickup
```

Measure pickup displacement over a controlled period.

Expected:

```text
no visually meaningful displacement
```

Tiny solver movement is acceptable.

A zombie must not shove the 1800 kg pickup around.

---

# 42. MEDIUM NON-LETHAL IMPACT

If current tuning permits survival:

verify real behavior:

```text
impact
↓
locomotion authority suspended
↓
strong fall
↓
down state
↓
get-up
↓
NavMesh/AI restored
```

No frozen AI.

No walking during fall.

No attack while down.

---

# 43. HIGH-SPEED LETHAL IMPACT

For ordinary infected at meaningful high road speed:

expected:

```text
strong physical impact
↓
canonical high damage
↓
ordinary zombie generally dies
↓
Death authority wins
↓
no get-up
↓
no NavMesh resume
↓
no attack resume
```

Do not bypass `ZombieHealth`.

---

# 44. CORPSE / RUN-OVER

This is a mandatory visible behavior.

After lethal collision:

```text
pickup continues
↓
body/corpse no longer acts as active locomotion wall
↓
pickup can pass over / through body collision profile
```

A small bump is acceptable.

Unacceptable:

```text
instant stop
vehicle launch
vehicle flip
corpse pushes pickup
repeated kill
repeated condition damage
repeated impact noise
```

---

# 45. NO FULL RAGDOLL SCOPE

Do not introduce a full ragdoll framework.

Current production target is:

```text
directionally plausible reaction
+
strong fall animation
+
controlled displacement
+
death animation
+
stable corpse collision behavior
```

That is sufficient for this closure.

---

# 46. SMALL GROUP BEHAVIOR

Test approximately three ordinary infected.

Desired:

```text
single zombie
→ little vehicle resistance

three close zombies
→ noticeable but manageable resistance

vehicle remains controllable
```

Each distinct zombie can receive one semantic impact.

One zombie with several colliders must still receive one.

---

# 47. ATTACK INTERRUPTION

If an infected is attacking when struck by a strong vehicle impact:

interrupt existing attack state correctly.

It must not continue dealing player damage while:

```text
falling
down
dead
```

---

# 48. HEARING

Finish/verify vehicle noise category acceptance through canonical hearing.

Vehicle categories actually present may include:

```text
VehicleEngine
VehicleHorn
VehicleImpact
VehicleDoor
```

Use actual current enum/config.

Do not invent unused categories.

Required:

```text
vehicle event
↓
GameplayNoiseSystem
↓
Zombie hearing
```

No direct alert.

---

# 49. WORLD PRESSURE

Preserve:

```text
GameplayNoiseSystem
↓
WorldPressureNoiseAdapter
```

The same canonical noise can be consumed by AI hearing and World Pressure.

Do not emit two different gameplay noises solely to service both systems.

---

# 50. SAVE / LOAD REGRESSION

After locomotion/reaction adjustments, rerun the existing vehicle-kill save/load scenario.

Verify:

```text
vehicle condition persists
dead encounter remains coherent
no duplicate infected
vehicle remains valid
```

Transient reaction state must NOT be persisted.

Do not serialize:

```text
current knockdown timer
current animation progress
temporary displacement
impact dedupe contact state
```

---

# 51. SOAK

After core behavior passes:

run at least approximately:

```text
50 legitimate vehicle/infected impact episodes
```

using deterministic/resettable test targets.

Monitor:

```text
impact cache
reaction-state cleanup
zombie references
listener counts
vehicle instance count
managed memory
exceptions
```

No monotonic stale-reference growth.

Expected final:

```text
impact_contact_cache_end = 0
```

or equivalent stable cleaned state.

---

# 52. VERIFICATION ORDER

Use this exact high-level order.

### A — Focused VEH-ZMB

Verify new/affected impact dominance and reaction tests.

### B — Existing VEH focused suite

Run:

```bash
python3 Tools/veh001-verify.py focused
```

Historical checkpoint:

```text
68 / 68 PASS
```

Final count may be higher.

### C — Full EditMode

```bash
python3 Tools/veh001-verify.py edit
```

Historical:

```text
495 / 495 PASS
```

### D — Full PlayMode

```bash
python3 Tools/veh001-verify.py play
```

Historical:

```text
246 / 246 PASS
```

Previous full run duration was around:

```text
~655 seconds
```

Do not mistake long duration for hang.

### E — Clean build

```bash
python3 Tools/veh001-verify.py build --clean-cache
```

Historical:

```text
Errors=0
Warnings=396
```

Require final:

```text
Result=Succeeded
Errors=0
```

---

# 53. PLAYMODE ABORT WARNING

A historical PlayMode attempt failed because:

```text
Playmode tests were aborted because the player was stopped.
```

A later rerun passed.

Therefore if this happens again:

inspect:

```text
Unity state
test XML
log
heartbeat/process
```

before touching production code.

Infrastructure abort != gameplay regression.

---

# 54. PERFORMANCE MUST USE THE NEW BUILD

After clean build obtain the actual newest path.

Do NOT profile the older:

```text
20261006T102343-920192Z-production
```

build after source changes.

Use the fresh build.

---

# 55. NORMAL PERFORMANCE FINAL GATE

Run:

```bash
python3 Tools/veh001-performance.py "<NEW_BUILD>"
```

Require:

```text
PASS
Errors=0
measurable drive distance
```

Record actual:

```text
frames
avg
p95
p99
distance
max speed
GC
memory
vehicle profiler markers
```

---

# 56. ACTIVE-INFECTED PERFORMANCE FINAL GATE

Then:

```bash
python3 Tools/veh001-performance.py "<NEW_BUILD>" --active-infected
```

Mandatory final conditions:

```text
PASS
Errors=0

active_ai_count > 0

infected_impact_count_during_capture >= 3

impact_contact_cache_end = 0

measurable drive distance
```

Do NOT lower any of these to manufacture closure.

---

# 57. ACTIVE-AI PERFORMANCE DATA

Report actual final:

```text
frame_avg_ms
frame_p50_ms
frame_p95_ms
frame_p99_ms

distance_m
max_speed_mps

active_ai_count_min
active_ai_count_max
active_ai_count_end

infected_impact_count
infected_impact_count_during_capture

impact_contact_cache_end

Physics.Simulate

LastSignal.Vehicle.ZombieImpact

LastSignal.Zombie.AI
LastSignal.Zombie.Perception
LastSignal.Zombie.Navigation
LastSignal.Zombie.Hearing
```

Compare with historical capture meaningfully.

Do not require exact identical timing.

---

# 58. MANUAL BUILD ACCEPTANCE

After automated PASS, give Burak ONE exact command to launch the final build.

Expected form:

```bash
open -n "<FINAL_BUILD>/LastSignal.app" --args -veh001Inspect -veh001ZombieInspect
```

Use actual latest build path.

---

# 59. MANUAL ACCEPTANCE CHECKLIST

Burak must visibly verify:

```text
[ ] Parked normal zombie cannot shove pickup significantly.

[ ] Medium/high impact instantly interrupts zombie locomotion.

[ ] Strong impact produces visible full-body fall/knockdown.

[ ] Surviving zombie gets up and correctly resumes AI.

[ ] Dead zombie never gets up.

[ ] High-speed clean hit generally kills ordinary infected.

[ ] Pickup retains most momentum against one normal zombie.

[ ] Zombie does not behave like a concrete wall.

[ ] Pickup can continue over/past dead body.

[ ] Corpse does not launch or flip vehicle.

[ ] No repeated impact/death spam occurs while driving over corpse.

[ ] Three-zombie small group remains driveable.

[ ] Nearby zombie can hear relevant impact/engine/horn through canonical hearing.
```

Do not mark these PASS yourself unless actual visible automation proves them.

If Burak has not checked them:

```text
MANUAL ACCEPTANCE = NOT_RUN
```

---

# 60. CURRENT COMPLETION EXPECTATION

At this checkpoint:

```text
Core implementation:
roughly 80% complete

Full production closure:
roughly 65–70% complete
```

Do not use this estimate as engineering status.

Actual final status comes from evidence.

---

# 61. DOCUMENTATION

Update:

```text
Docs/Implementation/VEH-001/VEHICLE_INFECTED_IMPACT.md
Docs/Implementation/VEH-001/STATUS.md
```

Capture actual:

```text
root cause
1800 kg pickup
NavMesh/Transform locomotion authority conflict
vehicle-impact dominance solution
fall/get-up
death override
contact-normal severity
momentum measurements
corpse/run-over behavior
hearing
normal performance
active-AI performance
soak
manual acceptance
```

No invented numbers.

---

# 62. DO NOT CLOSE IF...

VEH-ZMB-002 MUST remain IN_PROGRESS if any of these remain:

```text
zombie still visibly pushes heavy pickup

one zombie stops moving pickup unrealistically

high-speed ordinary zombie impact does not produce strong/lethal response

surviving reaction never recovers AI

dead zombie gets up

corpse acts as concrete wall

run-over produces repeated kills

active-infected performance has <3 impacts

normal performance cannot complete measurable drive

impact cache leaks

full regression fails

build fails

manual visible acceptance fails
```

---

# 63. FINAL CLOSURE CRITERIA

Only declare:

```text
VEH-ZMB-002 CLOSED
```

when all are true:

```text
focused VEH-ZMB PASS
existing VEH focused PASS
full EditMode PASS
full PlayMode PASS
clean build PASS
normal performance PASS
active-infected performance PASS
>=3 real infected impacts
impact cache clean
soak PASS
manual visible acceptance PASS
```

If everything except human visual acceptance passes:

use:

```text
IMPLEMENTATION COMPLETE
AWAITING MANUAL VEH-ZMB-002 ACCEPTANCE
```

---

# 64. FINAL REPORT FORMAT

At completion respond to Burak in Turkish with:

# VEH-ZMB-002 Final Closure

## 1. Sonuç

PASS / PARTIAL / BLOCKED / FAIL

## 2. Korunan mevcut implementasyon

What was already present.

## 3. Bu continuation'da yapılan değişiklikler

Especially the performance-route correction and any final reaction fixes.

## 4. Physics root cause

Explain why NavMesh/Transform zombie previously behaved stronger than the pickup.

## 5. Pickup dominance

Report actual production momentum numbers.

## 6. Zombie impact reaction

Explain:

```text
locomotion suspension
fall
down state
get-up
AI restore
```

## 7. Lethal impact

Explain canonical high-speed kill behavior.

## 8. Death override

Confirm dead targets never recover AI.

## 9. Corpse / run-over

Explain physical behavior and semantic dedupe.

## 10. Hearing / World Pressure

Confirm canonical routing.

## 11. Save/load

Confirm regression.

## 12. Focused tests

Exact X/X.

## 13. EditMode

Exact X/X.

## 14. PlayMode

Exact X/X.

## 15. Build

```text
Result
Errors
Warnings
Path
```

## 16. Normal performance

Actual metrics.

## 17. Active-infected performance

Actual:

```text
active AI
impact count
distance
max speed
avg
p95
p99
impact cache
```

## 18. Soak

Impact episodes and leak/cache result.

## 19. Manual acceptance

PASS / FAIL / NOT_RUN.

## 20. Remaining limitations

Only genuine ones.

## 21. Closure decision

Exactly:

```text
VEH-ZMB-002 CLOSED
```

or:

```text
VEH-ZMB-002 IN_PROGRESS
```

with evidence-based reason.

---

# 65. EXACT RESUME POINT

Do NOT begin by discussing architecture again.

Begin with this:

> The previous session already discovered that `VehiclePerformanceCapture` uses `VehicleActor.SetDevelopmentControl(...)`; no new performance-driver abstraction is required. The actual active-infected failure came from the multi-segment performance route. `VehiclePerformanceCapture.cs` was just modified so active-infected mode drives straight forward for the full capture window while normal performance retains its broader route. The session was interrupted immediately afterward while reviewing `ZombieAnimationPresenter.cs`. This latest route change has NOT yet been fully compile/test/build/performance verified.

Therefore do:

```text
1. Recover and inspect the current VehiclePerformanceCapture diff.
2. Finish the interrupted ZombieAnimationPresenter review.
3. Compile.
4. Run narrow VEH-ZMB tests.
5. Verify fall/get-up/death behavior.
6. Verify parked-pickup dominance.
7. Verify real-AI momentum retention.
8. Verify corpse/run-over.
9. Verify group behavior.
10. Verify canonical vehicle hearing.
11. Run VEH focused regression.
12. Run full EditMode.
13. Run full PlayMode.
14. Create fresh clean build.
15. Run normal performance on fresh build.
16. Run active-infected performance on fresh build.
17. Require >=3 real impacts.
18. Run impact soak.
19. Provide final build manual acceptance command.
20. Update documentation.
21. Close only if all required evidence passes.
```

Do not restart completed work.

Do not create a new development-control architecture.

Continue from the existing code and finish the production closure.
