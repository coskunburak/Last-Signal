# S008 verification plan
All gates pending except fresh compilation/EditMode. Do not interpret this checklist as results.
1. Complete entry PlayMode; failures/aborts/skips block feature changes.
2. Domain: same container rules, exact/partial/full/no-source/multistack/bidirectional transfers, invalid inputs, no negative quantities, observers see conserved committed totals, duplicate/reentrant transfer rejection, randomized bounded conservation sequence. Test actual asset ammo MaxStack 60.
3. Ammo: stash 40, magazine 12, withdraw 20, reload 18, fire 4, deposit reserve 2 => stash 22, reserve 0, magazine 26; only actual shot/reload APIs.
4. Lifecycle: initial shelter, one leave/return per request, duplicate rejection, stash preserved, sequence increments, no BeginSession on return, no loot End/Begin, no replacement Player. Reach authored interactions with production InteractionController.
5. UI: actual buttons one/stack selected transfers, event-driven refresh; 10 open/close cycles and single click exactly one transfer; input/cursor restoration; no competing inventory or menu panel. Death, pause, focus loss and menu teardown, Session B clean.
6. World: consume deterministic point in Expedition 1, retain a partial stack and dropped item, return/store/prepare, Expedition 2 verifies identities/remainders and no respawn. Actual scavenge/combat/return both expeditions; no fixture injection claimed as normal route.
7. Soak: 10 within-session expeditions with bounded object/listener counts and item conservation including magazine/fired rounds.
8. Profile: warmed domain transfer allocation vs UI refresh, idle open panel, actual leave/return duration and allocation. Report sampling scope and do not substitute static no-Update inspection for measured zero.
9. Full EditMode/PlayMode, expected zero failure/aborted/unexpected skipped. Classify Console warnings.
10. macOS Development build using shelter scene, actual standalone input Expedition 1/return/prep/Expedition 2 and Player.log. Windows commercial target remains unverified unless executed on Windows.
11. Store actual evidence and final gate matrix, required specs/authoring/report, final honest verdict. Checkpoint after every major gate.
