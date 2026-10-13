# Socket matrix
Positions below are in the native imported mesh frame, metres. All S021 models have uniform scale 1 in FPS and world prefabs. Contacts sit on the right palmar handle surface; +Y points into the grip and +Z along fingers.

| Weapon | PrimaryGrip xyz | Palm forward / normal | Optional references |
|---|---|---|---|
| Tactical Pistol | .061, .047, -.058 | +Z / -X | Sight (.05016,.15706,-.078); muzzle (.05016,.131,.14312); actual Magazine/ReloadPalm; no LeftGrip or Stock |
| Machete (requested light blade) | .017, -.004, -.12 | -Y / -X | No firearm references |
| Wooden Axe | .011, .002, -.12 | -Y / -X | No firearm references |
| Assault Rifle | Existing authored contacts unchanged | Existing calibrated palms | Existing stock/support/reload/muzzle/sight |

Machete handle at z≈-.12: half-width .0173 m; axe at z≈-.12: half-width .0105 m, y range -.0157 to .0196. Both shafts extend toward +Z. Axe head is concentrated near z=.27–.41, projecting toward -Y; its exact centre of mass is not available (no density data). The rest frame maps shaft-up, keeping the head above the grip instead of pointing back at the camera.

Sight height derives from actual mesh top vertices rather than whole-weapon bounds. Iron-sight picture/front-rear alignment still needs Game-view approval. Socket validation rejects missing primary, invalid scale, nonfinite data and nonuniform contact scale. Disabled inherited socket objects may remain for compatibility; active references point to the new semantic contacts.
