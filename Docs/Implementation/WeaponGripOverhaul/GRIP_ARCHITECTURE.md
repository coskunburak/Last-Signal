# Anatomical grip architecture
The existing CharacterWeaponPoseProfile remains the only per-weapon pose source. Extended data: low/raised-ready composition and hold time, per-joint finger/thumb/trigger flexion, follow-through target/fraction. Shared SampleAttack handles authoritative phase sampling and cancelled-action recovery in both representations.

Generic FPS calibration now starts from the actual skinned-mesh bind relationship `parentBind * inverse(childBind)`. Authoring persists neutral finger/metacarpal positions and rotations without editing vendor assets. Runtime temporarily applies the serialized neutral joint rotations during calibration, then restores the original transforms. Finger flex axes therefore come from the uncurled anatomy rather than the rifle grip. Thumb opposition is bounded to 30°; finger/trigger flexion uses per-profile angles.

Independent profile/root target → calibrated wrist → two-bone arm solve → visual attached to resolved palm. Camera-space shoulder anchors remain the established presentation origin. Arm segment lengths are not scaled. Reach residual remains separate from contact error; new wrist-bend diagnostics catch implausible neutral alignment. A clamped weapon still requires visual and reach-budget approval.

ValidAuthoring rejects missing/mixed hand chains, foreign/duplicate finger references, invalid rest quaternions and missing socket contracts. Repeated finger sampling restores the original snapshot before reapplying, so disable/next-frame restoration remains idempotent. Invalid initialization no longer resets a magazine to an uninitialized rest position.

Legacy rifle/crowbar paths remain unchanged. No second Animator, weapon-selection registry, camera, hit authority or save data was introduced. See previous WeaponGripCorrection/GRIP_ARCHITECTURE.md for the preserved socket compatibility contract.
