> 2026-10-13 visual rejection update: arm/jacket clearance and axe framing changed after the earlier PASS run. Current revision is NOT_RUN; production visual acceptance remains pending. See [anatomy correction](ANATOMY_CORRECTION_20261013.md).

# User visual acceptance — PENDING

Run functional gates first. Start the production scene with the production Player. Turn off Gizmos for screenshots; use them separately to diagnose contacts. Record FOV/aspect and selected weapon. Baseline 75 degrees / 16:9; also inspect 60 and 100 degrees, 4:3 and 21:9. Do not judge anatomy solely from passing numerical checks.

## Axe

- Head visible upper-right, shaft diagonal, centre/crosshair unobstructed enough to navigate.
- Right hand at the higher shaft contact, left at the lower contact; neither overlaps the other. Inspect both thumbs, knuckles and distal curls for empty grip or mesh penetration.
- Wrists flow into forearms; elbows bend consistently without inversion, body intersection or stretched shoulders. Check arm mesh ends and camera near-plane clipping.
- Inspect ready, walking forward/back/sideways, direction reversal, sprint entry/exit, crouch, slide and stop; yaw and pitch ±80 degrees. Both hands stay on the shaft.
- Attack: anticipation, both-arm acceleration, active contact, follow-through, recovery; no static wrist while shaft swings. Return to exactly the same ready pose. Interrupt during windup and recovery and repeat several cycles.
- Compare idle directly with the user's VEIN screenshot once available. The reference was not present in this task's attachments; screenshot matching remains unaccepted.

## One-hand blade (current Machete asset)

Right palm holds the handle, never the blade. Thumb opposes curled fingers. Blade extends naturally; wrist is not folded sideways. Left hand remains independent in idle, light strike, recovery and movement. No two-hand axe constraint survives switching. The source is a machete, not a newly supplied short knife.

## Diesel / transitions

Existing inspection controls: 6 toggles character inspection; 7 cycles views. Inspect front, side, rear and three-quarter while standing, moving and attacking. Check both axe palms; check independent knife/machete left arm; no duplicate weapons or floating FPS arms.

Repeat Axe → Blade → Axe → each pistol → Axe → Blade → Rifle, rapid switching, switching during recovery/crouch, pause during attack and resume, inventory interaction, treatment, accepted damage/death, vehicle entry/exit, component disable/re-enable and session restart. Check no stale constraints/finger poses. Outgoing weapons currently disappear at the existing switch boundary; animated holstering is not implemented.

## Evidence after functional gates

Provide axe FPS idle, mid-swing clip/screenshot and recovery; blade FPS idle and attack; Diesel axe front/side and blade front/side. Include the VEIN reference for direct comparison. Report any hand separation, clipped arm, wrist roll or finger penetration even if the 20 mm test passes.
