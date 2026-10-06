# D132 — wide FOV crowbar shoulder exposure

Scene: Assets/LastSignal/Scenes/Production/S013Cabin.unity, Unity 6000.5.0f1.
Temporary Play Mode; actual Player camera, Camera.Render 1280x720; HUD excluded.

Rifle hip and crowbar idle captured at vertical FOV 60/75/100. At 100 the crowbar arm mesh exposes large shoulder surfaces at both lower corners. Existing 75-degree framing does not show those surfaces. Rifle hip inspection did not identify the same defect.

Temporary visual-pivot offsets of -8/-12/-16 cm on local Z were captured and restored. -12 cm removes the exposed shoulders in the inspected idle view. Production MeleeStanceViewPresenter now blends this visual-only offset between FOV 75 and 100, clamping outside that range. The Animator remains owner of its local authored pose; meleeOrigin is outside the modified visual branch.

Source refresh completed; runtime FOV 100 reported StancePivot local position (0,0,-0.12). crowbar-100-runtime-fixed.png was inspected: exposed shoulder surfaces are absent. The final capture is a fresh Play Mode session with slightly different look framing; use temporary candidate captures for same-frame comparison. Source backups are included.

The existing stance PlayMode test now checks FOV 60/100/75, preserving authored swing rotation and gameplay origin and restoring standard-FOV pose. Updated test NOT_RUN; user focused-melee command required, expected 4/4. No full suite or build executed.

Limitations: idle screenshots only; continuous swing/equip, crouch/slide, other aspect ratios, wall clipping, shared hand identity and human art acceptance remain open. This is not final D132/D140 PASS. Temporary Play Mode ended; Editor left open; scene/prefab/vendor assets not saved.
