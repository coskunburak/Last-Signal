# S021 animation and audio mapping

| Action | Rifle/crowbar | Pistol | Machete/axe | Acceptance |
|---|---|---|---|---|
| Idle, equip, unequip, locomotion, crouch/slide | Existing VAL/FPS and Diesel routes | Inherited viewmodel controller plus new world pose | Existing crowbar controller plus new world hand pose | Visual QA pending for every new weapon |
| Fire / swing / recovery | Existing weapon authority and presentation | Pistol-specific gunshot clip, inherited viewmodel state machine | Definition-timed swing and distinct foley/impact fallback | Gameplay timing authored; clip quality pending |
| ADS / recoil | Existing rifle pose and recoil | Pistol pose offset and recoil data | Not applicable | Pistol grip, wrists and FOV pending |
| Reload / empty reload | Existing rifle route | Transaction-timed pistol foley; inherited rifle animation clips are temporary and may not match pistol geometry | Not applicable | **PARTIAL:** pistol visual reload needs dedicated compatible animation |
| Dry fire / interruption / death | Existing event/cancel route | Shared dry-fire fallback, cancelled on disable/switch; zero condition rejects | Swing cancellation on disable | Visual/audio QA pending |
| World body | Rifle and crowbar pose profiles | `PistolPose.asset`, pistol model wrapper | `MachetePose.asset`, `WoodenAxePose.asset` | Hand, finger, shoulder and clipping review pending |

Pistol sound is taken from the local handgun pack before the general S015 firearm cue to avoid a rifle transient; accepted impact sounds still route through S015. Melee impact/noise is emitted only after a committed damage receipt for receipt-aware targets. None of the authored pose offsets, imported audio or inherited animations has been given a human quality PASS.

The machete and axe presenter scales the inherited swing clip to each definition's full attack duration; the existing crowbar timing is preserved. This keeps visual cadence near the authoritative windup/active/recovery timeline, but the strike pose still requires frame-by-frame human review.
