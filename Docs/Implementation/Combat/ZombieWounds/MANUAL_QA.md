# Manual acceptance — NOT_RUN

Use the real `Assets/LastSignal/Scenes/Validation/ZombieAcceptance.unity` scene and its production rifle/zombie, then repeat lifecycle/save scenarios in `Assets/LastSignal/Scenes/Production/RelayExpedition.unity`. Open and enter Play manually. No new demo scene was created.

The existing manual combat route, where its HUD is present, supports F4 canopy, F5 inspection flashlight, F6 replace with 1/10/20/30 zombies, F7 gore, F8 daylight/night, F9 reduced effects, F11 reserve ammo (R reload). F10 previews **unapproved** cut art on disposable actors only; it does not approve or change production cut gates. Do save/load acceptance in a fresh normal encounter: F6 intentionally ends the authored encounter and is a combat inspection route, not a persistence fixture.

## Living body

- One chest shot: entry wound and blood must appear at the visible contact, not the collider centre.
- Two chest points separated by at least 8 cm: two distinct wounds. Five close shots: one progressively larger/darker local wound; immediate feedback for every shot.
- Back, shoulder, head/face/neck where geometry exists, abdomen, pelvis, each upper/lower arm, hand, thigh and lower leg. Rear hits must not open a front-chest patch. Trouser impacts must affect the visible clothing.
- Walk, rotate, attack and hit-react while watching the wound; it must follow the surface without drifting, floating, square borders or detaching.
- Fatal shot: immediate wound/blood and the existing death presentation. Approved-cut preview: one detached part only, no duplicate attached geometry, no stranded wound or emitter. Keep production art gates unchanged.
- Inspect small head/ribcage dents from a grazing view. Other wounds use simulated depth; do not accept them as physical holes.

## Corpses and obstruction

- Kill an actor, then shoot chest, head and leg while it falls and after it settles. Different points remain distinct; close shots merge.
- Compare health transactions and death callbacks before/after corpse fire. They must not increase; health stays zero, AI stays dead, no extra loot/reward/population death.
- Put a wall in front of the corpse, then test a corpse in front of a living actor. Only the nearest unobstructed target gets feedback. Repeat with the muzzle against/through a wall and with camera/muzzle parallax.
- Continue firing until the original 30-second lease expires. Shots must not extend it. Create more than eight corpses; oldest bodies should leave while death records remain intact. No invisible collision barrier remains.
- Gore OFF must suppress graphic effects while retaining the same corpse obstruction and health behavior. ON must not replay old blood.

## Save, cell and session

- Wound a living actor, save, unload/reload the cell and load the save. Supported wounds restore in place, dry, without bursts; anatomical state remains correct.
- Kill an authored encounter, let its corpse disappear, then save. Reload: same encounter identity remains dead, no new kill event/reward, no historical blood burst. Save again after the restored corpse expires.
- Kill a population actor: its dead ledger increments once; firing into the retained body changes no population count. Cell unload removes its body and effects.
- Disable/reuse an actor, transition scene, return to menu and restart. Old wound meshes, overlays, bleeding, marks and receiver references must not survive the previous lifetime.
- Normal/reduced effects: maximum 8/4 wounds. Switching quality must not change HP, damage, ammo or death. Gore OFF→ON must not replay cleared wounds or bursts.

## Lighting and performance

Inspect daylight, night and flashlight at front/side/rear views and close/medium range. Reject floating decals, clipping, excessive gloss, incorrect backface placement, pink shaders or fabric that looks like exposed bare skin. Alpha overlays and sparse topology require this visual check.

Run 1/10/20/30-zombie scenarios, repeated automatic fire, concurrent bleeding, eight corpses and repeated pool reuse. Capture CPU/GPU frame time, skinning, transparent overdraw, draw calls, GC and memory. Invoke `Last Signal > Zombie > Log Wound Resource Snapshot` while playing to capture bounded resource counts and resolution misses. These are manual measurements, not pre-existing evidence.

Return day/night/flashlight screenshots, a short movement/attack/fatal/corpse-fire video, gore/quality settings, zombie count, `LS_WOUND_DIAGNOSTICS` lines and profiler captures. Automated assertions cannot establish subjective visual acceptance.

## Written automated coverage (NOT_RUN)

| Coverage | Fixtures / cases |
|---|---|
| Receipt acceptance/rejection, target/generation, dedup, sub-hits, coordinate validation, fatal single death, region/multipliers | New `ZombieWoundTests`; existing `CombatFeedbackTests`, `ZombieDamageTests` |
| Profile budgets, production references, all 15 readable skin bindings, missing renderer, barycentric/backface mapping, authoring repeat GUIDs | New `ZombieWoundTests`; existing `ZombieBloodVfxTests` |
| Head/body/arm/hand/leg/clothing placement, separate/merged wounds, rear exclusion, movement/rotation/attack attachment, local dent bounds | New `ZombieWoundPlayTests` |
| Ordinary/fatal blood, actual emitter origin and following, lifetime, normal/low budgets, gore/reuse/session cleanup | New `ZombieWoundPlayTests`; existing `ZombieBloodVfxPlayTests`, `CombatFeedbackPlayTests` |
| Production ammo, obstruction, attack cancellation, hit reaction and death; existing anatomy and detached-part compatibility | Existing `CombatFeedbackPlayTests`, `ZombieDamagePlayTests`, `ZombieDismembermentPlayTests` |
| Corpse head/chest/leg impact, wall/mask exclusion, dedup, no health/death mutation, no lease extension, cleanup/expiry | New `ZombieWoundPlayTests`; actual scene death coverage remains in `ZombieDamagePlayTests` |
| Old data and wound validation, wound JSON/anatomy restoration without burst replay, terminal record after actor removal | New `ZombieWoundTests`, `ZombieWoundPlayTests`; existing `SaveFoundationTests` |
| Real scene kill → body cleanup → save → completed-encounter reload → no new Died event → save again | Added `PersistenceIntegrationTests.ZombieCorpseRemovalSaveAndCompletedEncounterReload` |
| Population ledgers, save/streaming lifecycle and session generations | Existing `WorldPopulationManagerTests`, `WorldPopulationPlayTests`; full `WorldCellIntegrationTests` in regression |

The isolated wound fixture manually sets terminal controller state for mesh/receipt checks and does not pretend to validate navigation. Real-scene tests cover controller and persistence integration. The older performance fixture was updated to expect finite corpse retention and skip dereferencing evicted bodies; its allocation assertions were retained. It is excluded from focused Play and reserved for user-run regression. There is no new loot-award subsystem: corpse and restoration tests assert no health/death notification, the integration boundary through which a new kill could be awarded. Visual appearance, detached cut art and 1/10/20/30-zombie performance remain manual acceptance items.
