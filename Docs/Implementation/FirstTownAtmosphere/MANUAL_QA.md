# Owner-run verification

All acceptance statuses start **NOT_RUN**. This document does not authorize the agent to run tests or PlayMode. `Install` and `Apply` have already been executed to author the delivered scene. Do not run FirstTown `Create` again.

## Commands

With Unity open: open `FirstTownCore`, wait for imports, inspect Console. Use **Last Signal > First Town > Atmosphere > Verify authored references (user run)**. This checks scene wiring, sky texture references/shader errors and persistent volume components; it cannot establish visual quality.

For batch commands, save and close this Unity project first. From Terminal:

```sh
cd '/Users/burakcoskun/Last Signal'
python3 Tools/first-town-atmosphere.py compile
python3 Tools/first-town-atmosphere.py references
python3 Tools/first-town-atmosphere.py editmode
python3 Tools/first-town-atmosphere.py playmode
```

Run each command individually and inspect its reported Evidence directory before moving to the next. `compile` launches Unity batch import/compilation only. `references` invokes the explicit reference checker. `editmode` filters to WorldTimeTests and FirstTownPersistenceTests; `playmode` filters to WorldTimeIntegrationTests and FirstTownPlayTests. Test XML is authoritative for counts/results; process exit alone is not a test PASS. These existing tests are regression coverage, not a new automated weather visual suite.

Evidence appears under `Docs/Implementation/FirstTownAtmosphere/Evidence/<timestamp>-<action>/` with exact command, Unity log, process exit and test XML when applicable. For an editor-open workflow use Test Runner with those class filters. No test command invokes a build or benchmark. Existing broader gameplay checks and user-owned navigation bake remain in `Docs/Implementation/FirstTown/MANUAL_QA.md`.

## Time and weather sampling

Use the delivered scene and its isolated FirstTown save slot. Preserve any valued save before doing a fresh-world review; do not delete saves just to inspect lighting. A restored save overrides the inspector's starting-time setting.

For fresh-world samples, outside PlayMode select **Session > WorldClock > Settings** and temporarily change `startingSeconds` to the value below. Start a fresh session manually, capture the player view, exit PlayMode, then undo the setting edit. Do not save temporary QA settings into the production scene. Restore original 28,800 seconds / 60 world-seconds per real second after sampling.

| Sample | startingSeconds | Inspect |
|---|---:|---|
| Pre-dawn | 18,000 (05:00) | Silhouettes and street pools without washed-out night |
| Dawn | 23,400 (06:30) | Warm horizon, long shadows, dawn haze and readable entrances |
| Morning | 28,800 (08:00) | Clear texture colors, sky transitions, no double sun |
| Day | 43,200 (12:00) | Bright readable roads/loot, texture detail retained by ACES |
| Afternoon | 54,000 (15:00) | Stable ambient and shadow direction |
| Golden hour | 63,000 (17:30) | Sun/horizon alignment, warm highlights without orange clipping |
| Dusk | 68,400 (19:00) | Smooth night blend and streetlight ramp |
| Night | 82,800 (23:00) | Zombies/doors/road edge readable at practical encounter distances |
| Midnight wrap | 86,340 (23:59) | No sky-cloud jump or lamp flash across day rollover |

At default time scale, one world hour takes 60 real seconds. Weather intervals are 2,700–7,200 world seconds (45–120 real seconds); rain alternates through the existing simulation. A full day takes 24 real minutes. Wait through the natural transitions rather than editing persisted weather JSON. Overcast can swell while the HUD correctly still says CLEAR.

For each weather transition:

- Record CLEAR → cloud build-up → RAIN → clearing at one fixed viewpoint. No abrupt sky/light/audio jump, stale splash pile or full-screen wall of drops.
- Observe wetting over roughly 15 real seconds and drying over 40 real seconds at default scale. Check asphalt, masonry, roof metal, timber, container and vehicle separately. Timber/masonry should remain rougher than metal/asphalt.
- Inspect all 12 selected road patches. They disappear dry, stay flush with roads, and do not float, z-fight or cover lane markings indiscriminately.
- Enter/leave roof cover repeatedly. HUD UNDER ROOF remains gameplay-owned; nearby rain clears without eliminating visible outdoor rain. No drops or splashes through ceilings; no indoor floor wetness.
- Compare normal rain/heavy rain ambience outside, under canopy and inside a house. No duplicated RainMaker sound, sudden loop restart or inexplicable thunder.
- Pause/resume, die/restart, return to menu/new session and save/load during rain. No leaked materials, leftover rain, stale lighting or accumulating AudioSources. Loading restores time/weather without altering player wetness rules.
- Sleep through dawn/weather changes using the existing bed eligibility flow. Sky/weather should reflect the resulting authoritative time; no hidden extra clock.

## Traversal and visual route

Use player first/third person and normal gameplay camera; reference cameras alone do not establish acceptance.

1. Safehouse cabin (-180,-170) → gate (-180,-141) → west bridge → checkpoint (0,-140).
2. Gas approach (51,-108) → forecourt (72,-83) → service room (70,-55).
3. Main street → shops (-25,22..58) → diner (28,27) → town hall (30,66).
4. Industrial gate (128,-93) → loading (165,-66) → dispatch (149,-109).
5. Clinic (-55,-42) → school (-45,139) → residential junction (125,115) and a warm annex.
6. Motel court (80,-185) → room → highway; then forest ranger (-197,130) and farm (180,205).

Coordinates above are X/Z. At every route stop, compare clear day, rainy day and rainy night. Check the entrance, retreat direction, one loot interaction and nearby zombie silhouette. At 10/25/50/100m, confirm fog preserves useful depth and route information. Drive through checkpoint, bridge and gas approach in rain/night. Verify light poles/fixtures add no collision or path obstruction.

Interior-specific: compare doorway thresholds, ceiling spill, wall leakage, floor wetness, loot highlights and whether local light budgeting switches conspicuously between neighboring rooms. Check camera rotation against skylight/specular highlights and moving vehicle reflections. Look for purple shaders and incorrectly oriented panoramas at all headings.

## Performance capture (owner only)

Use the same device, resolution, quality level, route and camera for each sample. Record CPU/GPU frame ms, p95/p99 frame time, draw calls, SetPass, transparent overdraw, particle count, shadow cost, probe spikes, memory and GC allocations. Take at least 60 seconds each: clear day main street, rainy day gas, rainy night industrial, rainy-night vehicle drive, and interior traversal. Capture after warm-up and once around a probe refresh.

Expected authored caps: 2,154 live rain/impact particles; 12 enabled local lights; 2 local shadow-casting lights within 30m; one 128px probe updated at most once per 30 active real seconds outdoors. Sun/sky materials and scene startup material assignment also need observation. Impact rays are at most 20/s plus 40/s middle tile-height rays. Particle collision adds its own engine cost and is not included in those ray counts.

Inspect static batching/SRP behavior after the material swap. Budget compliance alone is not an FPS PASS. If probe spikes dominate, temporarily disable `exteriorReflection` on a local review copy and compare; if transparencies dominate, compare lower middle emitter rates/caps. Apply shipping changes only after evidence review.

## Acceptance matrix

| Requirement | Implementation | Acceptance |
|---|---|---|
| One authoritative time/weather/save path | IMPLEMENTED; no new persisted state | NOT_RUN |
| Dawn/morning/day/afternoon/golden/night sky | IMPLEMENTED | NOT_RUN |
| Clear/overcast/rain transition coherence | IMPLEMENTED | NOT_RUN |
| Roof-aware near/mid rain and impacts | IMPLEMENTED | NOT_RUN |
| Per-family wetness, road puddles, drying | IMPLEMENTED; selective coverage | NOT_RUN |
| All ten district lighting roles | IMPLEMENTED | NOT_RUN |
| ACES/post/readability and reflection cost | IMPLEMENTED | NOT_RUN |
| Existing audio loop integration | IMPLEMENTED | NOT_RUN |
| Save/load/sleep/return-to-menu and gameplay | Integration authored | NOT_RUN |
| Advanced volumetric/local fog and interior probes | PARTIAL; see handoff limits | NOT_RUN |
| Compilation/reference/test commands | Prepared, not run by agent | NOT_RUN |
| Visual target / target-device performance | Requires owner acceptance | NOT_RUN |

Return the evidence directory, screenshots/video from the failing sample, time/weather HUD, district/camera and hardware/resolution. Mark PASS only after you have run and accepted that row.
