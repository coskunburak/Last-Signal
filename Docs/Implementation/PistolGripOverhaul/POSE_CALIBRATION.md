# Editor workflow

Use Edit mode. `Last Signal > Character > Author Pistol Anatomical Grip` updates only the pistol profile, two production pistol prefabs and two per-rig finger assets. The broader existing weapon authoring calls this final pistol pass so rebuilding S021 does not regress calibration. Existing asset GUIDs are preserved.

`Preview Pistol Grip Authoring` renders an isolated 75° editor pose; `Preview Pistol Finger Contact` renders a close side view. Outputs are `/private/tmp/lastsignal-pistol-editor-preview.png` and `/private/tmp/lastsignal-pistol-contact-preview.png`. These are authored static poses, not gameplay screenshots. The preview does not start Play or run acceptance tests.

Saved copies from this authoring pass: [low-ready](Authoring/PistolLowReady.png) and [finger contact](Authoring/PistolFingerContact.png). These show the static asset candidate only; no gameplay or user acceptance is implied.

Select FirstPersonWeaponGrip to inspect the wrist, elbow, primary contact, rear sight, muzzle, finger segments and weapon bounds with Scene handles. The inspector links the persistent profile. Runtime numbers expose palm separation, reach residual and wrist angle; test code must use final LateUpdate samples, not transient values between Update restoration and the final pose pass.

Authoring directions live in PistolGripAuthoring; persisted bind-relative offsets live in PistolFingers_FPS and PistolFingers_Diesel. Changes to the model or skeleton require recalibration rather than blindly copying quaternions between rigs. Prefabs store neutral fingers; the final runtime pass owns their articulation.

Finger-core checks use a conservative internal volume in the measured handle (not a complete skin collision solver). Bone paths outside that core can still have flesh/glove penetration. Review the rendered surfaces, thumb, trigger guard and sleeve seam before approval. Reach and viewport thresholds are unchanged.
