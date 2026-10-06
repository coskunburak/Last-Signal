# S014 Editor visual review

S013Cabin, live production player camera, 1280x720. Rifle hip FOV75 and ADS FOV55; crowbar idle FOV75. Camera.Render captures omit screen-space HUD. No standalone or human approval claimed.

Confirmed source conflict: Crowbar_Swing root rotation (windup -30/-22 degrees) and equip translation were overwritten by MeleeStanceViewPresenter LateUpdate. Fixed with runtime visual-only StancePivot above Animator root; original melee origin remains outside visual branch. No damage/timing/ammo changes.

Fixed windup capture is an explicitly sampled/frozen Animator pose, not a continuous combat route or proof of hit-frame alignment. It was sampled after entering Swing with Update(0), followed by Update(.22). Transform recorded (330,0,338). The camera framing differs from initial idle capture; these are not a pixel-matched before/after comparison.

Images reviewed: rifle-hip.png, rifle-ads.png, crowbar-idle.png, crowbar-windup-fixed.png. Rifle/crowbar hand and sleeve identity still visibly differs. Full FOV/aspect/pitch/wall tests and visual acceptance remain outstanding. Scene not saved; Play Mode stopped.

Runtime fix compiled and live preview executed. Added regression test NOT_RUN; focused-play now expects six tests.
