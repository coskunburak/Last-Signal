# Current local root causes — 2026-10-11
Implementation candidate, NOT visually accepted. This record extends WeaponGripCorrection; it does not repeat its historical audit.

| Category | Current local finding | Implemented response |
|---|---|---|
| FPS rig | Pistol AuthoredFingerCurl preserved a rifle-derived closed pose; actual mesh has complete 15-joint finger bindings | Calibrate from real skin bind matrices; persist neutral hand descendants and explicit finger-rest rotations; apply weapon-specific finger/trigger/thumb articulation |
| Mesh/socket | Prior physical contacts/native metre scales remain useful; no new mesh defect established | Retain calibrated contacts; add cosmetic silhouette reference for framing checks |
| Animation ownership | Owned pistol idle still contained hand/weapon curves; repeat finger Apply could replace restoration snapshot | Strip those curves only in owned clip; idempotent finger restoration; validate disjoint left/right bone chains |
| Framing | Single pistol hip target did not express low-ready and raised-ready; blade/axe idle shared almost identical composition | Independent low/raised ready driven by accepted shot events; different light/heavy stance; geometric viewport test targets |
| Melee motion | SmoothStep ended active swing at zero speed; no distinct follow-through | Accelerating active segment, velocity-continuous positional follow-through, eased recovery tied to unchanged combat timing |
| World body | Typed one-hand solve already present; shot-ready and cancellation recovery differed from FPS | Shared ready blend/attack sampling, matching finger profile, independent left hand except reload |

Editor source inspection: S013Cabin open and stopped; no live FirstPersonWeaponGrip instances existed. Did not start Play to manufacture a runtime audit. Inspected production prefab hierarchies and imported skin bindings instead. Pistol Arms/gloves/Gorka: 75 bones/bind matrices, all 15 right-finger references present. Melee armsmesh: 49 bones/binds, all 15 present. Existing Generic rigs are sufficient; Diesel remains Humanoid.

Production Player: default 75° FOV, settings range 60–100°, near clip .01 m. Scene evidence cameras (60/65°) are not the spawned Player gameplay camera and were not used for calibration.

At initial implementation, both pasted attachments contained text only and the referenced screenshots were unavailable. On 2026-10-12 the user supplied three VEIN target images; their observable composition/contact details and limitations are recorded in REFERENCE_ACCEPTANCE.md. Current Last Signal defect/gameplay captures are still unavailable, so no comparison of the implementation against those references or reference-quality claim is made. Confirmed inherited rifle lens/trigger defects were removed in the previous correction and remain disabled. Their exact relationship to the unavailable black-protrusion screenshot remains unconfirmed.
