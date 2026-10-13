# FPS and Diesel hand ownership

IMPLEMENTED_NOT_VERIFIED; anatomical/visual acceptance is NOT_RUN.

The existing VAL Generic FPS arms and `PistolGrip.controller` are retained. FPS right chain is upperarm.R → forearm.R → hand.R. Fifteen right finger joints are taken from the already-authored FirstPersonWeaponGrip mapping, including thumb_meta.R. Diesel uses its Humanoid bone mappings, not the VAL mapping. Neither skeleton nor skin weights were replaced.

| Model | Primary palm contact (normalized model metres) | Finger width/depth authoring inputs | Sight distance |
|---|---|---|---|
| DXT | (.013,.025,-.009) | .026 / .038 | .340 m |
| Glock17 | (.014,.013,-.006) | .029 / .042 | .345 m |
| Custom9mm | (.015,.022,-.004) | .030 / .045 | .350 m |

Each model has independently generated FPS and Diesel bind-relative quaternion arrays, separate index trigger poses and recorded endpoints. Directions use the model's grip proportions, then solve against each skeleton's actual phalanx axes/lengths. Diesel thumb proportions receive a separate direction. These are initial authored calibrations; finger penetration/gaps and wrist anatomy require the user's visual review, not merely valid quaternion arrays.

Ownership remains the existing ordered pipeline: restore previous offsets → Animator → profile arm target solve → attach weapon to the resolved right palm → finger pose → mechanical motion. The visual is never parented under the hand while the hand simultaneously follows that child. Camera-relative shoulder anchors and bounded reach/elbow/twist solving remain active; forearms are not hidden to conceal contact errors. Tests inspect reach residual as well as palm contact.

Left arm stays independent at idle/ADS/fire and reaches the actual moving Magazine/ReloadReference only during reload. Cancel, switch, disable, death, seating and teardown follow existing pose restoration and visibility handling. FPS uses layer 29; world mesh renderers use layer 30. CharacterBodyVisibility still owns camera-specific visibility.

FPS and Diesel use separate finger assets and separate camera-space versus shoulder-space pose fields. They share identity, handedness and source geometry, not camera offsets. World mechanics bind the same accepted-shot owner without producing audio or damage.

The earlier VEIN screenshots were not attached in this chat; no direct screenshot match is claimed. User acceptance should include front/side hand views as well as FPS.
