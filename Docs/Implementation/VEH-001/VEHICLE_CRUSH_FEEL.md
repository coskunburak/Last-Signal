# VEH-ZMB-003 — Crush and Run-Over Feel

Status: IN_PROGRESS. Presentation implementation is integrated; fresh full regression, player build/performance, and human acceptance remain required. VEH-ZMB-002 results are historical baseline evidence, not proof of this revision.

## Existing layers audited

- Production pickup: 1800 kg dynamic body, real MotionCore and four WheelColliders. Existing bounded infected contact response remains the speed/load authority. No new Rigidbody impulse, artificial drag, traction override or physical corpse ramp was added.
- Existing audio: authored NOX trunk-close impact, idle/loaded engine, rolling, horn, starter and door cues. Vehicle AudioSources currently use the default output route; no dedicated mixer group was found in this presenter. New emitters follow the existing event source output. Presentation loudness remains separate from GameplayNoiseSystem.
- Existing presentation: steering wheel follows road-wheel angle; physical wheel/suspension motion is supplied by MotionCore. No infected-specific cockpit camera impulse or front/rear body traversal sequence existed.
- Existing infected: animation-driven fall/hold/get-up for surviving impacts; bounded navigation displacement; all owned colliders disabled at death; death permanently removes AI and hearing. No ragdoll or hard corpse collider was added.

## Implemented feel layers

`VehicleActor.Feel.cs` is an isolated presentation partial. A committed canonical impact emits one initial cue. Up to eight transient body passes retain health/controller references for at most four simulation seconds. Each uses actual production WheelCollider geometry (front z=1.36, rear z=-1.29; track approximately x=-1.00/+0.95). Swept target-root position crossing an axle plane inside the axle/undercarriage span (track plus 0.45 m soft-body allowance) produces one cue per axle, only with a grounded wheel and moving vehicle. This is an animation-body/undercarriage approximation, not anatomical tire collision simulation. It works in either travel direction and does not emit blind delayed thumps if the car stops short, misses laterally or the target recovers/despawns. Both stages, expiry, target invalidation, suspension, restore and vehicle disable release queued references. Teleports over five metres per step invalidate the pass. No coroutine or per-impact container allocation was introduced.

Initial contact adds a bounded longitudinal/pitch cue; axle passage adds vertical compression/rebound and opposing front/rear pitch. A damped spring restores neutral. Combined offsets are capped at 0.04 m and 1.2 degrees. FirstPersonLook applies a temporary late-frame offset without changing aim/Pitch, removes it before ordinary look, and explicitly resets it during enter/exit/disable. Development exterior view receives 40% amplitude. WheelColliders, chassis forces and steering authority remain unchanged; suspension feel is reinforced perceptually, not by moving colliders.

Three preallocated, spatial, low-pass (550 Hz) audio voices reuse the existing authored NOX impact clip. Initial/body/front/rear volume/pitch vary within bounded settings; the ordinary semantic impact sound remains. Voice stealing caps overlap and never changes a different voice's pitch. Pause/session/disable stops body voices. No new asset download, gore, dismemberment, gameplay noise or World Pressure event is introduced.

Lethal vehicle impacts now use the existing full-body VehicleFall state over 0.55 seconds, then freeze its final pose. Death during a vehicle knockdown preserves its progressed fall phase instead of restarting a standing death. Medium surviving impacts retain their established fall/hold/get-up and AI recovery. Lethal cues are stronger than surviving cues. Gun/melee death presentation outside vehicle knockdown retains its existing path.

## Safety and scope

The feel layer has no damage, death, condition, noise or save writes. Body collision remains disabled after death; traversal feedback is spatially inferred from the just-committed impact. Repeated later drives over an old corpse, including one killed independently by a weapon, do not create a new body-pass sequence. No persistent corpse-contact registry was introduced. Visual/audio quality still requires cockpit and exterior human evaluation; automated semantic PASS is not sensory acceptance.

Profiler `LastSignal.Vehicle.BodyFeel` and capture fields `body_feel_impact_front_rear`, `body_feel_pending_end` were added. Fresh player measurements must establish cost; no zero-allocation or performance-budget claim is made from code inspection alone.

## Verification

Initial real-prefab dominance/feel suite: 9/9 PASS, evidence `Evidence/20261006T212111-345894Z-focused`. Real lethal traversal produced impact/front/rear=1/1/1, pending=0, one damage/death, unchanged post-hit condition/noise, settled corpse and retained forward momentum. Despawn, suspension, stopped-before-axle expiry and nonlethal recovery passed. This preceded the final audio voice pool and explicit camera-exit cleanup test.

Latest affected verification results are appended below after completion.

## Manual acceptance after fresh build

- Cockpit initial hit is strong and legible without an excessive camera jump.
- Strong hit immediately collapses the infected; no stiff wall or launch.
- Front and rear traversal are distinct soft load cues, not hard curb bounces.
- Pickup retains momentum through one body and a small group.
- Corpse cannot flip/block the vehicle or repeat damage/death/noise spam.
- Surviving knockdown recovers; dead infected never gets up.
- Braking before reaching a body does not play imaginary traversal.
- Steer away/despawn/pause/exit: no late unrelated cue or lingering camera offset.
- Keys 8/9 still switch cockpit/exterior and exterior angle.
- Audio remains restrained, with no audible harsh metal/rattle tail or repetitive excessive thump; tune by listening if needed.

## Final affected regression — 2026-10-07 Europe/Istanbul

`Evidence/20261006T212423-976690Z-zombie`: **40/40 PASS** (15 domain, 14 integration, 1 hearing, 10 real-prefab dominance/feel). Includes the explicit cockpit offset/exit restoration test and final bounded three-voice audio pool. Real production momentum: 12 to 10.77898 m/s (89.8% retained); this is the existing preassigned start velocity versus sampled post-hit speed, not an isolated solver-only impulse measurement. Real lethal pass impact/front/rear=1/1/1, pending=0. Nonlethal recovery, death during knockdown, stationary dominance, 20-second group route, no duplicated semantic consequences, stop-short, target despawn and Suspend cleanup pass.

Existing 100-impact isolated-physics soak also passes: cache peak/end=1/0; listeners before/after=1/1. Managed warm/mid/end=1395613696/1395576832/1395576832 bytes. This is Editor evidence including fixture allocations, not a player zero-leak or zero-GC guarantee. Wheel-disabled isolated soak does not claim 100 real wheel traversal episodes.

After this group, diagnostic cue counters were restricted to Editor/Development builds and performance acceptance was strengthened to require pending=0, initial presentation count equal to committed impact delta, and front/rear passage in active capture. The same conditions were added to the real 20-second course regression; its result follows below. No runtime gameplay behavior changed after the 40/40 group.


Final strengthened course: **1/1 PASS**, `../S012/Evidence/20261006T212838-694389Z/play.xml`, 27.487 seconds including setup. Group impact/front/rear=4/4/4; pending=0; living AI=1; physical contact cache=0. Scoped compilation and git diff whitespace checks pass.

Next user-operated commands, stopping at the first failed stage:
```bash
python3 Tools/veh001-verify.py focused &&
python3 Tools/veh001-verify.py edit &&
python3 Tools/veh001-verify.py play &&
python3 Tools/veh001-verify.py build --clean-cache
```
Then run normal and active-infected performance against the newly produced successful build, followed by manual cockpit/exterior acceptance. No final VEH-ZMB-003 player performance measurement or manual acceptance exists yet. No new build was launched by the assistant. Large runs remain user-operated as requested.
