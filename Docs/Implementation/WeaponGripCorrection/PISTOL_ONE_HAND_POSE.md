# One-handed pistol
PistolPose explicitly selects OneHandedPistol. Normal world support-hand weight is zero; locomotion owns the free left arm. FPS solves a lowered independent left wrist. Idle/hip/ADS/fire never target LeftGrip or a stock.

The actual MRPoly pistol has a separate weapon visual root outside the animated rifle wpn_body. The existing VAL Generic Animator remains, with an owned copy of its idle clip and no rifle reload/fire/sprint transitions or animation events. The pistol WeaponAnimationPresenter is disabled because profile/arm presentation replaces those incompatible action curves. Existing audio, recoil gameplay controller, VFX and combat controller remain.

WeaponViewPresenter interpolates to the measured rear-sight frame; bounded cosmetic root kick feeds the arm solver so hands follow the gun. The final solve anchors shoulder positions in camera coordinates, preventing the open shoulder ends from travelling with ADS. Existing cameras/FOV/near clip are unchanged.

Reload assistance is sin² of the authoritative reload progress; it moves the actual pistol magazine along its insertion axis and reaches its child ReloadPalm. Cancel/switch resets the magazine and assistance immediately. This is a procedural magazine-handling approximation, not a new authored slide-rack animation. Slide lock/rack finger choreography remains an art/visual acceptance limitation.

Disabled inherited renderers: rifle body, rifle magazine, rifle Trigger, ScopeLens, unused VAL_Model and Environment. Arms, glove and sleeve renderers remain enabled. ScopeOpticPresenter is disabled only on the pistol. Muzzle flash follows the corrected muzzle; casing ejection uses the pistol frame. Gameplay colliders were not changed.
