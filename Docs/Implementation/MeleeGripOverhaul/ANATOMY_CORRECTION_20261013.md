# Arm / jacket correction following visual rejection

Status: IMPLEMENTED, UNITY VERIFICATION NOT_RUN, VISUAL ACCEPTANCE PENDING.
The earlier eight PASS gates describe the previous revision only. They do not validate these changes or establish production visual acceptance.

## Observed in the user’s running scene

Read-only inspection: both world palms were within approximately 0.015 mm of their sockets, while the left elbow and wrist were inside the jacket. Actor-space left elbow was (-0.1303, 1.3607, 0.0953), wrist (0.0609, 1.2397, 0.1190). Weighted jacket vertices extended to approximately z=0.27–0.28. Bone mapping and parentage were consistent; this is evidence of a pose-placement/IK problem, not proof of defective skin weights.
The FPS left palm viewport was (0.560, -0.476, 0.218), wholly below the frame despite exact socket contact. FOV was 75, aspect 1.773.

## Changes

A body-frame ellipsoid approximates the Diesel jacket plus sleeve clearance. Arm solutions search the nearest clear elbow swivel around the fixed shoulder-to-wrist axis, preserving both segment lengths and the wrist target. Unsatisfiable geometry remains visible in penetration diagnostics rather than stretching bones or reporting success. This applies to firearm and procedural melee arms. Rifle stock anchoring is retained.
Axe shared-root placement projects both wrist targets forward of the envelope as well as within reach; its existing 10 cm adjustment bound is retained. World axe ready pose moves toward the center and forward. FPS ready pose is raised and tilted separately to expose the support hand. Authoring defaults and the production profile agree.
Inspector diagnostics now display jacket penetration. Tests now require torso clearance in melee swing/stance and assault stance/look cases and both FPS palms in frame in axe ready poses at the existing FOV/aspect matrix. Pure geometry tests independently check bone lengths and sampled segment distances. No finger mesh, vendor skin weights, weapon damage, attack timing, or rifle stock sockets were changed.

## Verification and limits

No Unity compilation gate, EditMode/PlayMode tests, build, or automated visual acceptance was run: user retains gate ownership. Changes were reviewed as text only. This envelope is an approximation, not a skinned-mesh collision solver. New authored offsets and the torso search must pass user gates and rendered visual review before production sign-off. Contact-only PASS is insufficient.
Required visual review: axe and assault front, rear, both sides and three-quarter views; ready, aim, crouch, movement, slide, attack/recovery and reload; FPS at 60/75/100 FOV and 4:3/16:9/21:9. Check jacket/sleeve penetration, elbow silhouette and continuity, wrist roll, thumb/finger wrap, both axe hands visible at ready, and stock seating. Include blade and pistol regression. Finger-wrap appearance has not yet been visually accepted by this change.

Use the existing eight-gate weapon-grip-verify.py sequence. melee-grip-edit includes CharacterArmClearanceTests; melee-grip-play includes the new assault case. Stop at the first failure and inspect its evidence. Do not lower clearance assertions to reproduce the old contact-only acceptance.

## Follow-up: 11:41 PlayMode failure

User-run compile 113945Z and EditMode 114018Z passed. PlayMode 114105Z failed all four cases: axe initial equip right penetration approximately 0.180 m; assault approximately 0.040 m. Remaining gates did not execute. These results are preserved as evidence, not marked passed.

Read-only inspection of the user's equipped live axe found exact contacts but approximately 14/19 mm right/left penetration. This does not explain away the larger initial-equip test failure: the original failure log lacked pose coordinates. New failure snapshots now include both arm chains, requested targets, torso frame/radii, reach and contact distances, sampled in LateUpdate even when no FPS probe is assigned. Assault assertions also use those final-pose snapshots rather than querying restored bones in the coroutine's Update phase.

The previous wrist-only projection missed chords passing through the torso. Clearance now measures the closest point over the entire arm segments analytically, and axe root placement minimizes both arms' penetration and reach residual within the existing 10 cm budget. The authored world axe position is raised 10 cm and advanced 3 cm. Added regression covers an intersection between the old sample points; the 3 mm PlayMode threshold is unchanged.

Live assault inspection independently confirmed the right requested wrist inside the envelope: torso-local (0.1387, 0.0788, 0.1064), penetration approximately 53 mm, exact grip contact. The stock seed was (0.1765, 0.1591, -0.0141), behind the anterior jacket envelope. The stock seed now projects to that envelope before weapon attachment; the rifle/default torso stance is 55 degrees to retain support-hand reach with the anterior pocket. Small existing torso reach assistance remains bounded. The weapon still attaches exactly to StockTarget; the target now represents an anterior pocket instead of an internal clavicle offset.

No automated Unity verification was run by the agent. Read-only live measurements and geometric authoring calculations are diagnostic evidence only. All changes in this follow-up require the user to rerun gates; no production or visual PASS is claimed.

## Follow-up: 12:03 PlayMode failure

User compile 120204Z and EditMode 120236Z passed. PlayMode 120321Z failed four cases; no later gates ran. The new snapshots identify axe failures at initial equip with torso Euler approximately (280, 69, 312), while its authored placement and search axes were actor-relative. Both contacts remained exact but the shared translation saturated at 0.10 m. This is an incompatible coordinate-frame relationship, not an acceptable contact PASS. The cause of that initial animated torso orientation itself has not been established; subsequent snapshots also record actor rotation, grounded state and Animator state.

Two-hand world melee placement, elbow hints and translation search now use the anatomical torso frame. View pitch affects world weapon orientation at 0.35 weight and does not orbit its position about the shoulder; authoritative melee aim/damage and the FPS pose are unchanged by this world-only pitch policy. FPS ready height is separately raised 15 mm for lower-hand margin at the narrowest existing FOV case; rendered acceptance is pending.

Assault progressed beyond the previous right-arm failure but its support forearm penetrated approximately 18 mm in the downward-look case despite zero reach residual. The shared-arm clearance cost now also chooses a bounded hip-only rotation around the fixed stock target (maximum 12 degrees, fading to zero as aimWeight reaches one). Both hand targets and rotations move with that same rigid weapon rotation. The existing ADS forward-vector regression remains; an explicit full-ADS zero-correction assertion and hip stock/contact/angle assertions are added. No assertion thresholds were loosened.

Validation: source review and whitespace checks only. Unity verification remains user-operated and NOT_RUN for this revision. Initial-animation provenance, dynamic continuity, performance and rendered appearance are not established by these edits. Do not mark production acceptance complete without passing the gates and visual review.
