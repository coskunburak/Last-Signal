# Arberry rig and assembly

Status: own Humanoid asset authored; functional/deformation verification NOT_RUN.

Unity's supported ModelImporter workflow generated an Avatar with `isValid=true` and `isHuman=true`. These properties were observed during authoring; they do not prove successful retargeting or acceptable joint deformation. No Diesel Avatar was assigned.

The complete mapping is in [rig-authoring.txt](rig-authoring.txt). Core mapping: pelvis→Hips, spine_01→Spine, spine_03→Chest, spine_05→UpperChest, neck_02→Neck, head→Head; clavicle/upperarm/lowerarm/hand and thigh/calf/foot/ball use matching l/r sides. All 30 finger joints map by actual names; metacarpals, twist/corrective/IK/accessory bones are retained. Intermediate spine and neck bones remain in the hierarchy. Optimize Game Objects is disabled. Import cameras/lights/animations are disabled; the source had no embedded clips.

`Arberry_WorldBody.prefab` preserves one skeleton, all 11 modular parts, shared bone references and original mesh/blendshape assets. Unit root scale, +Z forward, layer30, one Animator, applyRootMotion=false, AlwaysAnimate. Conservative skin bounds are authored in pelvis-local coordinates for crouch/weapon/seated poses. Shadow casting remains On; owner-camera hiding is delegated to the existing per-camera visibility component.

Baked rest metadata gives shoe minimum Y=-.0142 m and hair maximum Y=1.8792 m. The character is about 1.89 m including soles/hair; the existing 1.8 m gameplay capsule is unchanged. The 1.4 cm rest sole underflow and animated ground contact require visual acceptance; no arbitrary gameplay-capsule scaling was used. Exact animated foot contact is not established by rest bounds.

Right arm chain length measured .5502 m; shoulders are around ±.19 m, Y1.44. Profile targets use those anatomical dimensions. Full skin results are in skin-authoring-metadata.txt. No extreme-pose, T-pose deformation, corrective-bone behavior or cloth clipping PASS is claimed. Corrective bones have no new driver system; Humanoid retarget quality on this UE-style skeleton must be reviewed.

Source warning: one self-intersecting Jacket polygon is discarded by Unity. Production visual approval is pending this check.
