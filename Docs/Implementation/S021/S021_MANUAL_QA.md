# S021 human gameplay acceptance — NOT_RUN / USER OWNED

Run in `Assets/LastSignal/Scenes/Production/S013Cabin.unity` after the development build gate. Record build/evidence directory, seed, frame-rate cap, difficulty, health/stamina/ammo start and whether gore is enabled. Capture short FPS and third-person clips, a screenshot for alignment issues, and the relevant save/evidence file. No row is marked PASS before user observation.

| Check | Setup and action | Expected | Failure indicator | Evidence |
|---|---|---|---|---|
| Equip/unequip | Start a fresh session; select melee/firearm repeatedly | Rifle↔pistol and crowbar→machete→axe cycle; exactly one viewmodel visible | Ghost/duplicate weapon, stuck hands | FPS and third-person clip |
| Rifle firing | Ready rifle, fire one tap and a held burst | One round per accepted shot; established automatic behavior | Duplicate/missing shot | Ammo HUD + target clip |
| Pistol firing | Select pistol and tap fire twice | Semiauto shot per press; only 9 mm magazine decreases | Rifle ammo consumed, automatic burst | HUD + target clip |
| Fast melee | Fresh stamina; attack once with machete | Short windup/recovery, one hit per swing | Long crowbar cadence, multi-hit | 30/120 FPS clip and hit log |
| Crowbar | Select crowbar and strike same target | Existing medium response retained | Damage/animation regression | Before/after clip |
| Heavy melee | Full then low stamina; swing axe | Slow committed attack, higher cost, exhausted rejection | Fast/no-cost swing or hit on rejection | Stamina/HUD clip |
| Head/torso/limbs | Hit each visible zone on stationary zombie | Region multiplier, wound location and reaction agree | Head multiplier on torso, wrong wound | Zone screenshots + receipt log |
| Close range | Fight at arm length and next to a wall | Obstruction and reach rules hold | Through-wall hit, self-hit | Side/FPS clip |
| Low ammo | Empty each magazine, use a few reserve rounds | Dry cue, no ghost shot, partial reload only transfers available identity | Negative/created ammo | Before/after inventory |
| Reload interruptions | Switch before and after commit; repeat on death and menu/inventory open | Before commit conserves reserve; after commit transfer persists once; UI pause freezes clock; no late callback | Double load, stuck reload, ghost cue | Ammo timeline + log |
| ADS and FOV | Aim then switch, pause, vehicle enter, die | FOV/sensitivity restore; no invisible active gun | Stuck zoom or firing | FPS capture + camera values |
| FPS idle/ADS/fire | For rifle and pistol, hold idle/aim/fire | Clear silhouettes, grip and recoil fit weapon | Floating hand, rifle-style pistol hold | Close FPS video |
| FPS reload | Tactical/empty reload both guns | Magazine/slide motion matches committed ammo and sound | Pistol plays visibly rifle-specific sequence | Slow-motion FPS video |
| FPS melee motion | Swing each melee from idle/walk/sprint | Anticipation/recovery match speed and weight | Axe appears light; clipping | FPS video |
| Third-person locomotion | Observe another camera in idle/walk/run/crouch/slide | Active weapon follows body and stays readable | Missing model, floating wrist, torso penetration | Side/front/back video |
| Third-person fire/reload | Observe both firearms in third person | Hands/fingers/shoulders and muzzle aligned; no transition pop | Rifle pose on pistol, detached magazine | Side/front slow-motion video |
| Switch transition | Alternate all weapons rapidly, including held trigger | One active state, no extra audio/noise/projectile | Ghost shots, duplicate effects | FPS clip + ammo/noise log |
| Audio identity | Fire each gun; strike each melee; dry-fire | Distinct transient/mechanics/contact, zombie response, plausible spatial level | Rifle transient on pistol, clipping, missing impact | Headphone recording |
| Scarcity/viability | Replay B01–B10 with fixed seed | Melee and firearm choices each have useful niches | Universal dominant loadout or unusable role | Filled balance sheet |
| Noise/pressure | Fire at same range with each gun then use melee | Gunshot and impact events are accepted once; observe AI pressure | Gore toggle changes hearing, duplicate noise | Event count + encounter trace |
| Save/restart | Save pistol/axe selection, partial ammo, reduced condition; load twice | Same weapon/slot, magazines, reserve and condition each time | Ammo growth, missing ownership, wrong selection | Two save snapshots + HUD |

For each row record **PASS/FAIL**, actual observation, evidence path and any reproducible steps. Material, grip, animation, recoil feel and audio quality are human judgments; structural tests cannot substitute for them. The pistol visual reload, new melee weight, third-person hand placement, sound provenance and balance are known acceptance dependencies.
