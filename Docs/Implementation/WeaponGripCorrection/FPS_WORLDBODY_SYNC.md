# FPS / world-body synchronization
Combat selection still resolves the existing definition references in CharacterWeaponRig.Profiles. Each S021 FPS prefab holds the same CharacterWeaponPoseProfile used by Diesel. No parallel selection database.

FPS rigs: pistol VAL Generic skeleton; melee existing FPS arms Generic skeleton. World: existing Diesel Humanoid/locomotion controller. They do not share bone identities. Calibration derives wrist-to-palm transforms independently.

FPS: WeaponViewPresenter (hip/ADS/sway/recoil) → existing stance/action parents → FirstPersonWeaponGrip final arms/contact (order 350). World: Animator/action layers → CharacterWeaponRig final contact (order 300). Probes sample order 10000. Visibility remains owned by CharacterBodyVisibility and the existing layer 29/2 versus 30 camera policy.

World death/treatment/seat releases the weapon pose; vehicle hand IK remains the owner when seated. FPS equipment visibility remains combat-owned; death/treatment stops its final arm override. Disable restores local positions/rotations and magazine; new sessions have no static grip state. Existing camera inspection/front/side/rear controls remain unchanged.

A future networking synchronization system is outside scope; this is local FPS/world presentation consistency. Renderer pass behaviour, seating and world-body deformation require user regression/visual checks.
