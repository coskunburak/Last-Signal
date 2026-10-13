# FPS animation integration
| State | Actual implementation | Limit |
|---|---|---|
| Pistol low/raised ready | Procedural shared profile blend; accepted shot events raise; no new input or aiming mechanic | Art acceptance pending |
| Pistol ADS | Existing sight-space WeaponViewPresenter alignment composed after ready pose | Actual sight picture needs screenshots |
| Pistol fire | Existing cosmetic kick/recovery, final arm solve and bounded trigger squeeze | No bespoke recoil clip |
| Pistol reload | Existing right-hand ownership, actual magazine displacement and temporary left-arm assist | Partial procedural reload; no fully authored slide-rack choreography |
| Melee equip | Existing bounded procedural raise | No bespoke draw clip |
| Melee idle/movement | Existing Generic rig and neutral owned Animator; stance layer plus profile arm target | Walk style/weight still needs visual approval |
| Melee windup/active/follow-through/recovery | Per-weapon procedural phase motion, using unchanged definition timing; positional velocity continuous into follow-through | Angular interpolation is continuous but not a motion-captured action |
| Cancellation | Shared exponential return in FPS and world; disable resets | Immediate combat unequip remains immediate; no delayed cosmetic stow |

The PistolGripIdle project-owned clip retains compatible base/auxiliary animation; inherited hand, handIK and wpn_body curves are removed. The actual source FBX and rifle controller/clips are untouched. No dummy set of new attack clips was generated. Existing neutral melee clips remain.

Update restores prior overrides. Animator evaluates. WeaponViewPresenter resolves low/raised/ADS root. FirstPersonWeaponGrip runs final arms at order 350; tests probe at 10000. Pause/modal freezes cosmetic time; cached phases are reapplied from restored bones without accumulating transforms. Treatment/death/seating release the FPS arm pass and restore magazine state. Existing combat/body visibility controls renderer ownership.
