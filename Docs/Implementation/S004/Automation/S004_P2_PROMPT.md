PROJECT LAST SIGNAL
S004-P2 — PRODUCTION ZOMBIE AI FOUNDATION
PERCEPTION, MEMORY, NAVIGATION, CHASE, SEARCH, LIFECYCLE & READABILITY

ROLE
======================================================================

Act as the:

- Lead Gameplay Engineer
- Senior Unity AI Engineer
- Navigation Engineer
- Combat Systems Engineer
- Technical Animator
- Technical Designer
- QA Automation Engineer
- Performance Engineer
- Build / Release Engineer

for the commercial PC survival FPS:

PROJECT LAST SIGNAL

Engine:
Unity 6000.5.0f1

Render Pipeline:
URP

Platform direction:
Windows / Steam
Development currently performed on macOS.

You have access to:

- repository filesystem
- docs/
- Unity Editor
- Unity MCP
- current production zombie prefab
- existing player/session systems
- automated tests
- current scenes
- current weapon/combat foundation

This is production implementation.

Do NOT create a disposable AI prototype.

======================================================================
CURRENT VERIFIED BASELINE
======================================================================

S004-B0A:
PASS

Latest known baseline before P2:

EditMode:
38 / 38 PASS

PlayMode:
23 / 23 PASS

No current test runner abort.

Three-session lifecycle regression exists and passes.

S004-B0B:
production zombie integration completed.

Production zombie:

Hotstrike Stylized Dark Fantasy Zombie

Production animation source:

Kevin Iglesias
Zombie Monster Animations FREE

Verified production animation roles:

- Idle
- Walk
- Attack
- HitReact
- Death

Production Humanoid Avatar:
VALID

URP material:
READY

Production prefab:
READY

Presentation Animator:
READY

Measured Walk visual displacement:
approximately 0.92 m/s

Treat actual repository state as source of truth if anything differs.

======================================================================
PRIMARY GOAL
======================================================================

Build the first genuinely production-quality Last Signal zombie AI.

The result must NOT feel like:

distance check
→ SetDestination(player.position)
→ walk forever

The first Shambler should demonstrate the behavioral philosophy for the entire game.

The zombie must be:

- readable
- threatening
- fair
- non-omniscient
- persistent without cheating
- spatially aware
- believable around obstruction
- stable around corners
- lifecycle safe
- scalable

The player should be able to understand:

"I was seen."

"It remembers where I was."

"It is checking where I disappeared."

"It lost me."

rather than:

"The AI always knows exactly where I am."

======================================================================
IMPORTANT GAMEPLAY IDENTITY
======================================================================

The first Last Signal enemy is a slow Shambler.

It is NOT dangerous because it sprints like an action-game enemy.

Its threat should come from:

- persistence
- pressure
- uncertainty
- positioning
- poor escape routes
- later group behavior
- later resource pressure

P2 establishes the foundation for this.

Do not compensate for weak AI by simply increasing movement speed.

======================================================================
STRICT P2 SCOPE
======================================================================

IMPLEMENT:

- production zombie runtime architecture
- zombie tuning definition
- authoritative AI state machine
- target ownership
- visual perception
- field of view
- distance detection
- line of sight
- multiple visibility sample points where justified
- perception confidence / acquisition stability
- finite target memory
- last known position
- chase
- destination refresh policy
- NavMesh movement
- stopping behavior
- path validation
- partial-path handling
- invalid-path handling
- stuck detection
- bounded recovery
- search behavior
- reacquisition
- search timeout
- player lifecycle handling
- pause handling
- session restart handling
- animation presentation
- editor debugging tools
- automated tests
- performance checks
- normal gameplay integration
- evidence
- documentation

DO NOT IMPLEMENT:

- player health
- zombie damage
- actual melee attack transaction
- AttackWindup
- AttackCommit
- AttackRecovery
- ZombieHealth
- hitboxes
- headshots
- bullet reaction gameplay
- zombie death gameplay
- loot
- inventory
- hearing
- gunshot attraction
- smell
- group communication
- horde director
- spawn director
- persistence/save
- doors opened by zombies
- climbing
- vaulting
- crawling
- multiple zombie archetypes

Attack/HitReact/Death animations may exist but remain presentation assets for later
P3/P4.

======================================================================
1. READ THE CURRENT PROJECT BEFORE CODING
======================================================================

Read the ACTUAL current versions of:

Docs/Implementation/S004/

especially:

S004_P1_DISCOVERY_REPORT.md
S004_P2_ENTRY_REPORT.md
S004_B0A_BASELINE_RECOVERY_REPORT.md
S004_B0B_ZOMBIE_ASSET_INTEGRATION_REPORT.md

ZOMBIE_ASSET_INVENTORY.md
ZOMBIE_ANIMATION_MATRIX.md
FIRST_ZOMBIE_DECISION.md
ZOMBIE_LOCOMOTION_DECISION.md
ZOMBIE_BEHAVIOR_SPEC.md
ZOMBIE_ARCHITECTURE.md
S004_ACCEPTANCE.md
S004_RISKS.md
S004_IMPLEMENTATION_BACKLOG.md

Also inspect the actual:

- SessionFlow
- Player prefab
- PlayerInputReader
- FirstPersonMotor
- player camera
- production zombie prefab
- Animator Controller
- scene ownership
- tests
- asmdefs

Do not rely purely on this prompt when project documentation contains more specific
current information.

======================================================================
2. P2 ENTRY REVALIDATION
======================================================================

Before adding runtime AI code confirm:

- project compiles
- production zombie prefab resolves
- Animator resolves
- valid Humanoid Avatar remains
- Idle/Walk remain available
- URP material is valid
- current test baseline is green
- AI Navigation package exists
- no existing zombie AI implementation already owns the same responsibility

If a foundational regression occurred after B0B:

fix or report it before adding AI.

Do not stack AI implementation on top of a broken baseline.

======================================================================
3. ARCHITECTURE — AVOID GOD MONOBEHAVIOUR
======================================================================

Do NOT build:

ZombieAI.cs
with 1,000+ lines controlling everything.

Use clear responsibilities following repository conventions.

Conceptually expect responsibilities equivalent to:

ZombieController
    authoritative state orchestration

ZombiePerception
    visibility / perception evaluation

ZombieNavigation
    NavMesh movement and path policy

ZombieAnimationPresenter
    presentation only

ZombieRuntimeState
    mutable runtime data if appropriate

ZombieDefinition
    immutable tuning

Names may differ according to project conventions.

Do not create unnecessary abstraction layers.

The architecture must be:

small enough for one zombie
but clean enough for future population scaling.

======================================================================
4. SINGLE WRITER AUTHORITY
======================================================================

There must be ONE authoritative AI state owner.

Do not allow:

Perception changes state directly
+
Navigation changes state directly
+
Animator changes state directly

without orchestration.

Preferred flow:

Sensors
    ↓
observations

Controller
    ↓
decision

Navigation
    ↓
movement request

Animator
    ↓
presentation

Gameplay state owns truth.

Animator never owns AI truth.

======================================================================
5. AUTHORITATIVE P2 STATES
======================================================================

Implement only the states required for P2.

Expected:

Idle
Chasing
Searching

Do NOT add active combat states yet.

If the existing behavior spec has exact enum names, preserve them.

Each state requires:

ENTER
UPDATE
EXIT

and explicit legal transitions.

Example:

Idle
    → Chasing
        when confirmed visual acquisition occurs

Chasing
    → Searching
        when target visibility is lost beyond configured grace

Searching
    → Chasing
        when target is visually reacquired

Searching
    → Idle
        when search expires

No hidden transitions.

No Animator-driven transitions.

======================================================================
6. DO NOT USE BINARY "MAGIC VISION"
======================================================================

Avoid simplistic:

if distance < sightRange && angle < fov:
    targetSeen = true

Build production perception.

The AI should distinguish:

possible visibility

from

confirmed acquisition.

Use a small perception-confidence model where appropriate.

Conceptually:

visibility evidence accumulates
when the player is genuinely visible.

visibility evidence decays
when evidence disappears.

This prevents one-frame edge cases such as:

player touching one pixel of FOV
→ instant perfect chase

and:

one-frame pillar occlusion
→ instant complete amnesia.

Do NOT create an overengineered ML perception system.

Simple deterministic confidence is enough.

======================================================================
7. PERCEPTION TUNING
======================================================================

Use data-driven tuning for values such as:

SightDistance
HorizontalFOV
VerticalAllowance if needed
PerceptionInterval
AcquisitionTime
LossGrace
ConfidenceGain
ConfidenceDecay
NearDetectionModifier if justified
SearchDuration

Do not scatter constants through source files.

Use current project configuration conventions.

======================================================================
8. FAIR FIELD OF VIEW
======================================================================

Build an explicit field-of-view test.

Use:

distance
+
angle
+
line of sight

Do not make the Shambler see behind itself.

Provide a debug visualization for:

forward vector
FOV edges
maximum vision range

Editor-only Gizmos are acceptable.

======================================================================
9. MULTI-POINT VISIBILITY
======================================================================

One center-point raycast often produces poor FPS AI.

Where appropriate, evaluate a small bounded set of player visibility points.

For example:

Head
Chest

Potentially one additional point only if evidence warrants it.

Do NOT raycast every bone.

A player whose feet are behind a crate but torso is fully visible should not be treated
as completely invisible.

Likewise a tiny exposed fingertip should not automatically grant perfect target
knowledge.

Define a deterministic visibility policy.

======================================================================
10. LOS / OCCLUSION
======================================================================

A solid world obstacle must break visual detection.

Use explicit physics masks.

Do not raycast against arbitrary EVERYTHING.

Ensure compatibility with:

- player collider
- weapon raycasts
- interaction raycasts
- world geometry
- future enemy layers

Avoid accidental self-hit from zombie render/colliders.

======================================================================
11. NO WALLHACK KNOWLEDGE
======================================================================

When the zombie loses direct visibility:

it must NOT continue consuming:

Player.transform.position

as its chase destination indefinitely.

After LOS is lost, freeze the remembered information.

Store:

LastKnownPosition

and optionally:

LastSeenTime
LastSeenDirection

Potentially store a short bounded velocity estimate if justified.

Do not track the player's hidden live position.

This requirement is critical.

======================================================================
12. SHORT PREDICTION — OPTIONAL BUT VALUABLE
======================================================================

For more believable search behavior, allow a very small capped extrapolation from the
last visible player movement.

Example concept:

LastKnownPosition
+
LastVisibleVelocity * shortPredictionTime

Strictly cap:

distance
time

The zombie must not predict through entire buildings.

Prediction should create:

"I saw him moving toward that doorway."

not:

"I know where he is."

If this adds unnecessary complexity, omit it and document why.

======================================================================
13. ACQUISITION HYSTERESIS
======================================================================

Prevent vision flickering around FOV boundaries.

Use acquisition / loss hysteresis.

For example:

Acquisition may require:
confirmed visual evidence for a short duration.

Loss may tolerate:
brief occlusion before entering Search.

Do not use arbitrary large delays.

The player should still receive responsive enemy behavior.

======================================================================
14. SIGNATURE LAST SIGNAL SEARCH BEHAVIOR
======================================================================

Searching should be a meaningful behavior.

Do NOT implement:

stand still for X seconds
→ Idle.

When target is lost:

1. retain LastKnownPosition
2. travel toward that position
3. once near it, enter local search behavior
4. inspect plausible nearby directions
5. finish within a finite duration
6. return to Idle if no reacquisition

This is one of the first behaviors intended to make Last Signal zombies feel better
than generic store-asset AI.

======================================================================
15. SEARCH MUST REMAIN FAIR
======================================================================

During Search:

do NOT pull hidden Player.transform.position.

Search decisions may use only knowledge acquired while the target was visible.

Allowed:

- last known position
- last seen direction
- bounded local search points
- NavMesh geometry

Not allowed:

- current hidden player position
- magic player distance while occluded
- searching directly toward a hidden player

======================================================================
16. LOCAL SEARCH POINTS
======================================================================

When reaching LastKnownPosition, generate a small bounded search pattern.

Possible approach:

center
+
2–4 NavMesh-valid nearby inspection points

The pattern should:

- remain local
- be deterministic enough for tests
- not produce random chaotic running
- respect NavMesh
- expire

Do NOT build a general investigation framework yet.

No hearing system.

======================================================================
17. SEARCH VISUAL READABILITY
======================================================================

At the last known position the zombie should visibly appear to be looking.

Without implementing complex procedural animation, use controlled gameplay yaw:

turn toward:
- last seen direction
- selected search direction

Allow short, bounded pauses if useful.

The zombie should not instantly rotate through 180° every frame.

======================================================================
18. NAVMESH OWNERSHIP
======================================================================

Use Unity AI Navigation already installed in the project.

Do not introduce:

A*
third-party navigation
custom grid navigation

without an existing project decision.

NavMeshAgent owns locomotion.

Animator presents locomotion.

Root motion remains disabled for authoritative movement.

======================================================================
19. NAVMESH ACCEPTANCE SCENE
======================================================================

Create a dedicated project-owned acceptance scene if it does not already exist:

ZombieAcceptance.unity

This is a technical gameplay scene.

It must include geometry sufficient to test:

- open direct sight
- FOV entry
- wall occlusion
- corner break
- chase
- path around obstacle
- temporary LOS loss
- last known position
- search
- reacquisition
- search timeout
- unreachable destination
- path partial/invalid behavior

Keep visual complexity low.

This is not a shipping level.

======================================================================
20. NAVMESH SURFACE
======================================================================

Create/configure NavMeshSurface intentionally.

Record:

Agent Type
Layer Mask
Collect Objects mode
geometry source
bake bounds/settings

Do not bake unrelated assets.

Validate the actual resulting triangulation.

======================================================================
21. AGENT PHYSICAL FIT
======================================================================

Configure the zombie NavMeshAgent based on actual character dimensions.

Audit:

Radius
Height
BaseOffset

Ensure:

- zombie does not visually float
- feet stay near floor
- zombie does not pass through openings narrower than body
- character does not hover above stairs

Do not blindly use Unity defaults.

======================================================================
22. MOVEMENT SPEED
======================================================================

B0B measured the Walk animation around:

0.92 m/s

Use this as the INITIAL technical reference.

Start navigation speed around the measured visual walk speed.

Then tune visually.

Goal:

minimal foot sliding.

Do not increase movement to 3–4 m/s simply to make the zombie more dangerous.

The Shambler's threat should come from behavior.

======================================================================
23. ANIMATION SPEED MATCHING
======================================================================

If final navigation speed differs slightly from measured clip speed:

adjust Animator playback speed within reasonable limits.

Do not create obvious slow-motion or fast-forward animation.

Document:

Agent speed
Animation playback multiplier
Observed sliding

======================================================================
24. DESTINATION REFRESH POLICY
======================================================================

Do not call:

SetDestination(player.position)

unconditionally every Update.

Use controlled destination refresh.

Consider:

time threshold
+
position delta threshold

Example concept:

refresh when:

timeSinceLastRefresh >= interval

OR

target moved sufficiently far

This reduces path churn.

Measure actual behavior.

======================================================================
25. CHASE BEHAVIOR
======================================================================

While Chasing:

- zombie moves toward confirmed visible Player
- destination refresh is controlled
- zombie path remains stable
- movement stops near future melee engagement distance
- orientation becomes target-facing near stop distance
- no player overlap
- no jitter around stop radius

P2 does NOT attack.

When within future attack range:

the zombie may stop and face the Player.

Do NOT deal damage.

======================================================================
26. STOPPING HYSTERESIS
======================================================================

Avoid:

move
stop
move
stop
move
stop

when the target hovers near stopping distance.

Use different thresholds if necessary:

EnterStopDistance
ResumeChaseDistance

This creates stable behavior.

======================================================================
27. PATH STATUS
======================================================================

Handle:

PathComplete
PathPartial
PathInvalid

explicitly.

Do not assume SetDestination means destination is reachable.

Record path status where useful for debugging.

======================================================================
28. UNREACHABLE PLAYER
======================================================================

If Player is visible but unreachable:

do NOT:

spin forever
spam SetDestination
teleport
throw errors

Use a bounded failure policy.

Potentially:

remain oriented toward player
retry path at low frequency
eventually transition according to documented search/failure policy

Keep it simple in P2.

======================================================================
29. OFF-NAVMESH SAFETY
======================================================================

Handle:

NavMeshAgent.isOnNavMesh == false

safely.

Do not call invalid NavMeshAgent APIs repeatedly and spam Console.

Do not teleport the zombie silently across the map.

If spawn is invalid:

attempt only a small bounded NavMesh.SamplePosition correction where appropriate.

If correction fails:

disable AI safely and log a clear diagnostic.

======================================================================
30. STUCK DETECTION
======================================================================

Implement simple bounded stuck detection.

Example signals:

desired velocity exists
but
actual displacement remains near zero

for a configured duration.

Do not detect stuck every frame from velocity alone.

On confirmed stuck:

- refresh path
- attempt a small valid recovery
- do NOT teleport to Player

After repeated failure:

fail safely.

Record the condition for debugging.

======================================================================
31. CORNER BEHAVIOR
======================================================================

Specifically visually test:

Player runs behind a wall corner.

Desired:

Zombie sees player
→ chases
→ player disappears
→ zombie continues toward last known location
→ turns the corner based on remembered location
→ either sees player again
or
→ searches

Undesired:

player disappears
→ zombie instantly stops

or:

player disappears
→ zombie continues perfectly tracking through wall.

======================================================================
32. PLAYER TARGET OWNERSHIP
======================================================================

Do not search scene every frame.

Do NOT use:

FindObjectOfType<Player...>() in Update
GameObject.Find in Update
tag lookup every perception tick

Use an explicit lifecycle-compatible target source.

Integrate with the existing SessionFlow/player ownership.

======================================================================
33. SESSION START
======================================================================

When BeginSession creates Player:

AI must safely acquire the current active Player as a potential target.

No stale references.

No hidden static Player singleton unless the project already explicitly owns one.

======================================================================
34. RETURN TO MENU
======================================================================

When ReturnToMenu occurs:

zombie must:

- clear target
- clear path intention
- clear last known target information
- stop perception progression
- stop search timers
- not retain destroyed Player
- not throw MissingReferenceException

======================================================================
35. SESSION RESTART
======================================================================

After:

BeginSession
→ gameplay
→ ReturnToMenu
→ BeginSession

the zombie must use the NEW Player.

Never retain the destroyed previous Player instance.

Test this explicitly.

======================================================================
36. PAUSE
======================================================================

Use existing pause architecture.

When gameplay is paused:

- AI decision timers freeze appropriately
- NavMesh progression does not continue as active gameplay
- perception does not silently acquire player
- search timer does not expire incorrectly
- no state transitions happen behind pause menu

Resume should continue safely.

======================================================================
37. TIME AUTHORITY
======================================================================

Prefer game-time-compatible timers.

Avoid mixing:

Time.time
realtimeSinceStartup
custom wall-clock

unless explicitly justified.

Pause behavior must remain predictable.

======================================================================
38. ANIMATION PRESENTATION
======================================================================

Use the production presentation layer created during B0B.

At minimum:

Idle state
→ Idle clip

moving
→ Walk clip

Animator receives gameplay presentation data.

Animator must not decide AI state.

======================================================================
39. SPEED PARAMETER
======================================================================

Drive locomotion presentation from actual movement.

Prefer smoothed locomotion speed rather than binary instant snapping if beneficial.

But avoid excessive smoothing that causes:

zombie stopped
while animation continues walking.

======================================================================
40. ORIENTATION AUTHORITY
======================================================================

Choose exactly one gameplay yaw authority.

Do not allow:

NavMeshAgent updateRotation
+
custom controller rotation
+
root motion rotation

to fight simultaneously.

For example:

NavMeshAgent handles locomotion rotation while moving.

Near stopped/search orientation:
controller performs bounded yaw.

If you disable agent rotation entirely:

implement the complete replacement properly.

Document the decision.

======================================================================
41. TURN RATE
======================================================================

Zombie turning must feel physically plausible.

Do not instantly snap 180°.

Tune:

AngularSpeed
or custom yaw rate

according to Shambler identity.

The player should be able to read zombie facing.

======================================================================
42. READABILITY OVER CHEATING
======================================================================

When detection happens, the zombie should visually communicate it.

Do not add audio production if unavailable, but behavioral cues may include:

- brief orientation toward detected target
- coherent turn before movement
- state debug indication during acceptance

Do not add a full Alert state unless existing architecture justifies it.

Detection readability may occur inside state transition handling.

======================================================================
43. PERCEPTION DEBUG TOOLING
======================================================================

Create editor-only/debug visualization.

Show where practical:

State
Target
Sight radius
FOV
visibility rays
LastKnownPosition
Search destination
NavMesh destination
Path status

Do not ship expensive Debug.DrawLine spam unconditionally.

Use:

#if UNITY_EDITOR

or project debug conventions.

======================================================================
44. OPTIONAL AI DEBUG HUD
======================================================================

If useful for development only:

create a minimal debug overlay for selected zombie:

State: Chasing
Visible: Yes
Confidence: 0.82
Path: Complete
LastSeen: 0.4 sec
Distance: 6.2 m

Do NOT merge this into final player HUD.

Development-only.

======================================================================
45. PERCEPTION UPDATE PERFORMANCE
======================================================================

Do not perform full LOS work every rendered frame without reason.

Use a configurable perception tick.

Initial example:

5–10 Hz

but measure behavior.

The player must not perceive excessive latency.

Do not hardcode the example blindly.

======================================================================
46. TICK STAGGERING SUPPORT
======================================================================

Design perception ticking so multiple zombies do not necessarily raycast on exactly
the same frame.

For one zombie this changes little.

For future populations it matters.

Use deterministic/stable staggering if simple.

Do NOT create a massive global AI scheduler in P2.

======================================================================
47. ALLOCATION-FREE HOT PATH
======================================================================

Avoid hot-path allocations.

No LINQ in AI Update/perception.

Avoid:

new List every tick
new arrays every tick
string formatting every tick
GetComponents every tick

Cache required component references.

Use NonAlloc physics APIs where appropriate.

======================================================================
48. FUTURE POPULATION CONSIDERATION
======================================================================

P1 long-term design anticipates multiple nearby AI.

Do NOT optimize prematurely for 100 enemies.

But do not create architecture that obviously fails at 10–25.

P2 should profile:

1 zombie
10 zombies

and preferably:

25-zombie technical smoke

if safe.

======================================================================
49. MULTI-ZOMBIE TEST — NO CROWD SYSTEM YET
======================================================================

For 10/25 technical smoke:

do NOT implement group tactics.

Only inspect:

- path update cost
- perception cost
- Animator cost
- allocations
- excessive overlap/jitter
- NavMesh load

Crowd behavior belongs later.

======================================================================
50. PLAYER COLLISION / BODY OVERLAP
======================================================================

The zombie should not walk directly through the Player.

Reserve future melee space.

Check agent stopping distance relative to Player capsule.

Do not create invisible giant blocking radii.

======================================================================
51. SIGNATURE BEHAVIOR: UNCERTAINTY
======================================================================

Last Signal zombies should eventually be memorable because they react to incomplete
information.

P2 begins this identity.

The zombie should distinguish:

VISIBLE TARGET

from

REMEMBERED TARGET

from

NO TARGET.

These are not necessarily separate enum states.

They are knowledge states.

Avoid generic AI where "target exists" always means exact perfect knowledge.

======================================================================
52. SIGNATURE BEHAVIOR: COMMITMENT
======================================================================

A zombie that saw the Player disappear behind a nearby doorway should not instantly
give up.

Likewise it should not chase forever across the entire world.

Use finite commitment.

This tension:

persistent but fallible

is important.

======================================================================
53. SIGNATURE BEHAVIOR: PLAYER COUNTERPLAY
======================================================================

The behavior should allow the player to intentionally break pursuit by:

- leaving LOS
- creating distance
- changing direction after disappearing
- waiting out search

Future noise systems can complicate this later.

For now visual stealth/counterplay should already be possible.

======================================================================
54. TESTING — EDITMODE
======================================================================

Add focused logic tests where practical.

Potential tests:

StateMachine_InitialStateIsIdle

StateMachine_ConfirmedTargetTransitionsIdleToChasing

StateMachine_LostTargetTransitionsChasingToSearching

StateMachine_ReacquisitionTransitionsSearchingToChasing

StateMachine_SearchTimeoutTransitionsSearchingToIdle

Perception_ConfidenceAccumulatesWhenVisible

Perception_ConfidenceDecaysWhenNotVisible

Memory_LastKnownPositionDoesNotFollowHiddenPlayer

Search_HasFiniteDuration

NavigationDestination_DoesNotRefreshWithoutThreshold

Tuning_ValuesAreValid

Do not unit-test Unity NavMesh internals.

======================================================================
55. CRITICAL MEMORY TEST
======================================================================

Create a test proving:

Player visible at position A.

Zombie records A.

Player becomes fully occluded.

Player moves to position B while hidden.

Zombie's remembered position MUST remain A
(or only use bounded pre-loss extrapolation if implemented).

It must NOT become B.

This is a crucial Last Signal AI acceptance test.

======================================================================
56. PLAYMODE — DIRECT PERCEPTION
======================================================================

Test:

Player outside FOV
→ zombie remains Idle.

Player moves into FOV with clear LOS
→ detection builds
→ zombie enters Chasing.

======================================================================
57. PLAYMODE — WALL OCCLUSION
======================================================================

Player behind opaque wall.

Zombie must not initially acquire.

Move player into clear LOS.

Zombie acquires.

Move player behind wall.

Zombie retains only finite memory.

======================================================================
58. PLAYMODE — CORNER PURSUIT
======================================================================

Player visible
→ chase begins.

Player turns behind wall.

Zombie continues toward LastKnownPosition.

Zombie must not immediately know new hidden Player location.

======================================================================
59. PLAYMODE — REACQUISITION
======================================================================

While Searching:

Player becomes visible again.

Zombie:

→ reacquires
→ Chasing

without waiting for Search timeout.

======================================================================
60. PLAYMODE — SEARCH FAILURE
======================================================================

Player breaks LOS and stays hidden.

Zombie:

→ reaches remembered area
→ performs finite local search
→ eventually Idle.

No infinite pursuit.

======================================================================
61. PLAYMODE — PATH AROUND OBSTACLE
======================================================================

Player visible behind navigable obstruction configuration.

Zombie finds legal route around obstacle.

Does not:

walk through wall
teleport
stop permanently without diagnostic.

======================================================================
62. PLAYMODE — UNREACHABLE TARGET
======================================================================

Create deterministic unreachable condition.

Zombie must:

- remain stable
- not spam exceptions
- not teleport
- use documented fallback behavior

======================================================================
63. PLAYMODE — PAUSE
======================================================================

During chase:

pause.

Verify:

- movement stops
- state timers remain coherent
- no hidden transition

Resume.

Behavior continues correctly.

======================================================================
64. PLAYMODE — SESSION TEARDOWN
======================================================================

BeginSession
→ zombie sees Player
→ ReturnToMenu.

Verify:

target reference cleared.

No MissingReferenceException.

======================================================================
65. PLAYMODE — NEW PLAYER
======================================================================

BeginSession
→ zombie acquires Player A
→ menu
→ BeginSession
→ Player B.

Verify zombie target is Player B.

Never Player A.

======================================================================
66. PLAYMODE — NO ATTACK
======================================================================

P2 must explicitly prove:

Zombie can reach engagement distance
but does NOT damage Player.

There is no PlayerHealth yet.

No hidden collision damage.

======================================================================
67. REPEATED SESSION TEST
======================================================================

Run at least:

10 session cycles

for AI lifecycle acceptance if technically practical.

Validate:

- no duplicated zombie controller subscriptions
- no stale target
- no growing callbacks
- no accumulating coroutines
- no growing Console errors

If existing architecture intentionally recreates the zombie per session, validate that
lifecycle instead.

======================================================================
68. VISUAL ACCEPTANCE
======================================================================

Use Unity MCP / Editor.

Capture real production zombie behavior.

Required acceptance sequences:

A.
Idle

B.
Player enters view

C.
Zombie turns / acquires

D.
Chase

E.
Obstacle path

F.
Player breaks LOS behind corner

G.
Zombie reaches LastKnownPosition

H.
Search

I.
Reacquisition

J.
Search failure → Idle

K.
Pause/resume

L.
Session restart

Use actual production Hotstrike model.

No capsules as final evidence.

======================================================================
69. VISUAL QUALITY
======================================================================

Inspect:

- foot sliding
- character hovering
- path jitter
- turning jitter
- stop/start jitter
- wall clipping
- agent rotation snapping
- animation snapping
- Idle/Walk transition
- zombie/pivot mismatch

Fix visually obvious production defects.

======================================================================
70. NAVIGATION READABILITY
======================================================================

When following a path around an obstacle:

the zombie body should visually face the direction of movement.

Avoid:

walking sideways

or:

feet forward while body rotates toward Player through wall.

While target is visible and movement direction differs from direct target vector:

prioritize believable locomotion direction.

======================================================================
71. TEST SCENE VS NORMAL GAMEPLAY
======================================================================

ZombieAcceptance is deterministic test infrastructure.

But P2 is not complete until ONE production zombie is integrated into the approved
normal gameplay route.

Do not only demonstrate AI in a lab arena.

Do not populate the world with many zombies.

One normal-route zombie is sufficient.

======================================================================
72. NORMAL GAMEPLAY REGRESSION
======================================================================

Verify:

Player movement remains functional.

Mouse look remains functional.

Crouch remains functional.

Rifle remains functional.

Reload remains functional.

ADS remains functional.

Pause/menu remains functional.

The zombie must not break existing systems.

======================================================================
73. BASELINE REGRESSION
======================================================================

At the end run complete:

EditMode suite

PlayMode suite

Expected minimum baseline before P2:

38 / 38 EditMode
23 / 23 PlayMode

Counts should increase due to new legitimate tests.

Require ALL current tests to pass.

ABORTED is not PASS.

======================================================================
74. PERFORMANCE PROFILING
======================================================================

Measure at least:

1 zombie

10 zombies

Record:

AI CPU
perception CPU
navigation/path cost
Animator CPU
GC allocation
frame-time observations

If safe:

perform 25 zombie smoke.

Do not claim shipping density certification.

======================================================================
75. HOT PATH REQUIREMENTS
======================================================================

Aim for:

0 B recurring managed allocation

during warmed steady-state AI updates where realistically achievable.

If non-zero:

identify exact source.

Do not hide GC allocation.

======================================================================
76. PATH REQUEST FREQUENCY
======================================================================

Measure how often path destinations are refreshed.

Do not accidentally request paths at render frame rate.

Evidence should include approximate:

destination refresh frequency

under normal chase.

======================================================================
77. LOGGING
======================================================================

No per-frame production logs.

Debug state logs should be gated.

Console must not flood during:

unreachable path
player lost
pause
session teardown.

======================================================================
78. FAILURE SAFETY
======================================================================

Missing required reference should produce:

clear actionable error

rather than:

NullReferenceException deep in Update.

Use validation where appropriate.

======================================================================
79. PREFAB VALIDATION
======================================================================

Ensure production zombie prefab contains required P2 references.

Potential:

Animator
NavMeshAgent
Controller
Perception
Navigation
Definition
Perception origin

No Missing Script.

No unresolved serialized reference.

======================================================================
80. DATA-DRIVEN TUNING
======================================================================

Expose only meaningful tuning.

Do not expose every internal variable.

Group tuning logically:

Perception
Memory
Navigation
Search
Presentation

Add tooltips/ranges where useful.

======================================================================
81. INITIAL SHAMBLER TUNING PHILOSOPHY
======================================================================

Do not optimize for difficulty yet.

Initial values should support:

clear observation of behavior.

Examples only, NOT commands:

Sight:
moderate

Walk:
near measured 0.92 m/s

Search:
long enough to feel persistent
short enough to escape

Turn speed:
slow/moderate

Detection:
not instantaneous at extreme range

Use actual playtesting.

======================================================================
82. DO NOT USE RANDOMNESS TO HIDE BAD AI
======================================================================

Do not make search "interesting" by uncontrolled Random.insideUnitSphere every frame.

If randomness is used:

- bounded
- seeded where testing needs determinism
- generated only at specific decision points

AI behavior must remain debuggable.

======================================================================
83. DETERMINISM IN TESTS
======================================================================

Tests must not occasionally fail because of random search choice.

Inject or seed deterministic search decisions for tests.

Production can use variation where appropriate later.

======================================================================
84. DOCUMENT ACTUAL IMPLEMENTATION
======================================================================

Update:

ZOMBIE_BEHAVIOR_SPEC.md

with actual implemented P2 transitions.

Update:

ZOMBIE_ARCHITECTURE.md

with actual source files/components.

Update:

ZOMBIE_LOCOMOTION_DECISION.md

with:

Agent settings
speed
angular speed
acceleration
stopping policy
animation speed matching

Update:

S004_ACCEPTANCE.md

mark ONLY actually proven P2 items.

Update:

S004_RISKS.md

Update:

S004_IMPLEMENTATION_BACKLOG.md

======================================================================
85. CREATE P2 REPORT
======================================================================

Create:

Docs/Implementation/S004/S004_P2_IMPLEMENTATION_REPORT.md

Required sections:

1. P2 ENTRY BASELINE

2. AI ARCHITECTURE

3. STATE MACHINE

4. PERCEPTION MODEL

5. LOS / OCCLUSION

6. TARGET MEMORY

7. LAST KNOWN POSITION

8. SEARCH BEHAVIOR

9. NAVIGATION

10. STUCK / INVALID PATH POLICY

11. SESSION LIFECYCLE

12. ANIMATION PRESENTATION

13. NORMAL GAMEPLAY INTEGRATION

14. TEST RESULTS

15. VISUAL ACCEPTANCE

16. PERFORMANCE

17. KNOWN LIMITATIONS

18. P3 ENTRY CRITERIA

19. FINAL STATUS

======================================================================
86. EVIDENCE PACKAGE
======================================================================

Create:

Docs/Implementation/S004/Evidence/<P2-run-id>/

Include:

compile results
EditMode XML
PlayMode XML
scene information
NavMesh configuration
AI tuning snapshot
perception diagrams/screenshots
state debug screenshots
chase evidence
corner-loss evidence
search evidence
reacquisition evidence
pause evidence
restart evidence
performance measurements
Console summary

======================================================================
87. P2 DEFINITION OF DONE
======================================================================

P2 PASS requires:

ARCHITECTURE
[ ] clear authoritative AI owner
[ ] no God MonoBehaviour
[ ] tuning data separated from mutable state

STATE
[ ] Idle
[ ] Chasing
[ ] Searching
[ ] explicit legal transitions

PERCEPTION
[ ] sight range
[ ] FOV
[ ] LOS
[ ] occlusion
[ ] stable acquisition
[ ] stable loss behavior

MEMORY
[ ] LastKnownPosition
[ ] finite memory
[ ] no hidden-player tracking
[ ] critical memory test passes

SEARCH
[ ] reaches remembered location
[ ] performs local search
[ ] finite timeout
[ ] reacquisition works

NAVIGATION
[ ] NavMesh configured
[ ] path around obstacle
[ ] no wall traversal
[ ] stopping stable
[ ] invalid path safe
[ ] partial path safe
[ ] off-NavMesh safe
[ ] stuck policy exists

PRESENTATION
[ ] real production zombie
[ ] Idle works
[ ] Walk works
[ ] no severe foot sliding
[ ] no severe turn jitter
[ ] no root motion authority conflict

LIFECYCLE
[ ] pause safe
[ ] menu safe
[ ] restart safe
[ ] stale Player reference impossible in tested flow

QA
[ ] full EditMode PASS
[ ] full PlayMode PASS
[ ] no aborted suite
[ ] deterministic AI tests
[ ] normal gameplay regression PASS

PERFORMANCE
[ ] 1 zombie profiled
[ ] 10 zombie profiled
[ ] hot path inspected for allocations
[ ] path refresh frequency measured

EVIDENCE
[ ] MCP visual acceptance
[ ] screenshots/results stored
[ ] P2 report complete

SCOPE
[ ] no PlayerHealth
[ ] no zombie damage
[ ] no ZombieHealth
[ ] no hitboxes
[ ] no death gameplay
[ ] no hearing
[ ] no director

======================================================================
88. P3 ENTRY CRITERIA
======================================================================

P3 may start only when:

P2 = PASS

and:

Zombie can reliably:

detect
chase
lose
remember
search
reacquire
stop in engagement range

without cheating.

P3 will then implement:

PlayerHealth
AttackWindup
AttackCommit
AttackRecovery
single validated damage transaction
attack animation synchronization

Do not implement these early.

======================================================================
89. STATUS RULE
======================================================================

Use only:

PASS
PARTIAL
BLOCKED

Do not call P2 PASS if:

- player is tracked through walls
- Search is fake
- tests abort
- NavMesh throws errors
- zombie jitters severely
- normal gameplay is broken
- lifecycle leaks references
- evidence is missing

======================================================================
90. FIRST ACTION NOW
======================================================================

FIRST:

1. Read current S004 docs.
2. Inspect the completed B0B zombie prefab.
3. Inspect player/session ownership.
4. Verify current baseline tests.
5. Design the smallest production component architecture matching existing docs.
6. Implement the AI in gates.

DO NOT begin by writing a giant ZombieAI.cs.

IMPLEMENT IN THIS ORDER:

GATE 1
Runtime architecture + tuning

GATE 2
NavMesh acceptance arena

GATE 3
Idle + navigation plumbing

GATE 4
Visual perception

GATE 5
Stable acquisition

GATE 6
Chase

GATE 7
LastKnownPosition

GATE 8
Search

GATE 9
Reacquisition

GATE 10
Pause/session lifecycle

GATE 11
Animation presentation

GATE 12
Automated tests

GATE 13
Normal gameplay integration

GATE 14
Performance profiling

GATE 15
Regression

GATE 16
Evidence + documentation

Compile and run focused tests after every meaningful gate.

Do not defer validation until the end.

START S004-P2 NOW.
