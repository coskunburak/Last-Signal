# Pistol anatomy correction — 2026-10-12

Implementation candidate, not user-verified. Scope of this revision: pistol only. Earlier weapon-grip gates do not validate this revision.

The supplied Last Signal screenshot shows poor wrapping and hand/weapon continuity. Source and edit-mode prefab inspection established the following specific causes:

* Generic flexion angles were applied to fingers whose neutral phalanges already had distinct directions. The resulting middle/ring distal segments curled back toward the grip interior and upward rather than continuing around its far side. In pistol model metres, the former middle-finger endpoint was approximately (0.0558, 0.0775, -0.0495); the former thumb endpoint reached x=-0.0147, far beyond the opposite grip surface (approximately x=0.039–0.061).
* A zero palm/socket separation only proved frame attachment. It did not prove finger surface contact. Existing checks could therefore accept visibly poor anatomy.
* The shared pistol palm location did not account for the distinct VAL and Diesel hand proportions. Separate right-hand calibrations are now persisted.
* The former reload used a whole-duration sine wave. Its maximum magazine extraction was unrelated to the authoritative insertion/commit time.

The pistol already used a one-handed branch, not rifle stock/two-hand placement. Disabled rifle visuals and neutral animation ownership from the earlier correction are retained. There is one Generic FPS Animator; Diesel is a separate Humanoid rig. These were not newly discovered defects or newly replaced systems.

Baseline: branch `s012-integrated-graybox-slice`, HEAD `c41dcf1a0e0942377556227e938ef847391623bb`. Git status was inspected and recorded in `/private/tmp/pistol-overhaul-status-before.txt` (16,174 existing entries, including vendor deletions). Unrelated changes were preserved; no reset, commit or push.

No Play session, compilation gate, test suite, build or benchmark was run. Isolated editor asset previews were used during authoring; they are not gameplay acceptance.
