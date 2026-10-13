# Melee correction
The light weapon in this project is Machete, not a short knife; its existing 75 cm mesh remains. WoodenAxe is about 63 cm. No substitution or external package was needed.

Crowbar bounds fitting was removed from the final S021 authoring result. Mesh origin no longer determines grip: each imported mesh has a physical handle contact. Both world and FPS use the same profile and source geometry at metre scale. Finger closure differs (.80 machete, .65 axe), reflecting the measured handle cross-sections. Detailed thumb contact still needs visual acceptance.

The FPS Generic arm Animator is retained with an owned neutral root clip. FirstPersonWeaponGrip owns the actual arm/wrist motion; MeleeWeaponPresenter keeps audio/events but does not launch the old crowbar Swing when ProfileArmTargets is active. The original crowbar is unchanged.

Windup/strike/recovery positions and rotations are distinct per weapon. Axe anticipation is higher, farther back and larger; machete motion is quicker and shallower. Phase boundaries derive from MeleeAttackState.PresentationProgress and the unchanged definition durations. The arm solver moves upper arm/elbow/wrist together; solved palm contact positions the visual. Cancel recovery eases back in FPS; weapon disable resets immediately. World action layers retain torso/locomotion, with final arm contact owned by CharacterWeaponRig.

Neither animation callbacks nor grip components dispatch damage, spend stamina or change active-window timings. Contact clamps at extreme poses preserve attachment but may shorten motion; unacceptable neutral residuals must be recalibrated, not accepted just because contact error is low.
