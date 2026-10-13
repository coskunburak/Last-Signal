# User visual acceptance — NOT_RUN
Use S013Cabin with the actual Player prefab, current gameplay FOV and saved camera settings. Restart Play yourself after imports. Do not approve from socket distances alone. Record FOV, selected weapon, action, view and screenshots/video for failures. Referenced original screenshots were not available in the attachment.

| Weapon | View | States / observations |
|---|---|---|
| Pistol | FPS | Idle, hip, ADS, repeated fire: only right grip; clear sights, hand/gun recoil together, no black rifle lens/trigger geometry |
| Pistol | FPS | Tactical/empty reload, cancellation by switching, pause and resume: magazine follows real state, left assists transiently then lowers |
| Pistol | Third-person | Front, side, rear, three-quarter; hip/ADS/fire/movement; left never supports except reload |
| Machete | FPS | Equip, idle, attack, recovery, cancel; handle inside palm, blade forward/up as intended, no blade grip |
| Machete | Third-person | Idle/attack/walk/sprint; continuous hand contact and coherent elbow/wrist |
| Axe | FPS | Idle, anticipation, heavy swing, impact and recovery; shaft through grasp, head readable, no camera strike or head blocking crosshair |
| Axe | Third-person | Front/side/three-quarter idle/attack/movement; shoulder/elbow participation and stable contact |
| Rifle | Both | Stock/right/support contact, ADS, fire/reload, movement and switching regression |

For each weapon repeat crouch, sprint, slide/recovery, rapid turns/stops, maximum up/down pitch, repeated switching, disable/enable, death and new session. Inspect normal camera and Scene view with Gizmos off; use existing 6/7 inspection controls for front/side/three-quarter. Enter/exit vehicle and confirm seating exclusively owns hands.

FAIL: neutral contact or requested reach residual above ~2 cm, floating handle, fingers penetrating/gapping, hyperextended wrist, inverted elbow, shoulder seam/open arm end, view-obscuring weapon, ADS clipping, left-hand pistol support, stuck reload/attack or stale hidden weapon. Clamp at extremes must remain visually plausible. Art adjustments must update the shared profile/sockets, not global player/camera scale.

These observations are user-owned. Automated tests cannot award visual PASS.
