# Authored pose matrix
Values are spatial profile data, not pixel offsets. Contact and source scale precede framing.

| Weapon | Exploration/idle | Raised / action | Finger flexion (proximal, middle, distal) |
|---|---|---|---|
| Pistol | Root (.14,-.19,.28), Euler (18,0,-6): lower-right/downward ready | Accepted shot raises toward (.12,-.13,.30), Euler (3,0,-3); holds 1.1 s then returns. ADS remains actual rear-sight aligned | (65,85,55), thumb (22,38,28), trigger (16,38,22), bounded shot squeeze |
| Machete | Primary target (.14,-.22,.30), Euler (48,18,-28): forward-inclined lighter silhouette | Small windup, fast accelerating slash and short follow-through | (58,75,45), thumb (25,40,28) |
| Axe | Primary target (.16,-.20,.32), Euler (32,-8,-18): shaft toward upper-right, hand below | Higher/backward windup, stronger downward/cross-body stroke and visible follow-through | (62,80,48), thumb (25,40,28) |

Pistol coordinates describe the viewmodel root; melee coordinates describe its primary contact within the neutral visual frame. Their anatomical wrist offsets are calibrated independently.

Existing actual source geometry is retained: Tactical Pistol ≈24 cm, Machete ≈75 cm, WoodenAxe ≈63 cm. The light blade is a machete, not a short knife; no falsely labelled replacement was introduced. Measured handle contacts and axes remain documented in WeaponGripCorrection/WEAPON_SOCKET_MATRIX.md.

SilhouetteReference is a cosmetic mesh-local reference at the pistol slide, axe head or machete blade. It does not steer aiming, damage or the hand solve. FOV/aspect tests check normalized viewport bounds, not fixed screen pixels. Global gameplay FOV, near clip and camera ownership remain untouched.
