# Diesel rig audit

Source inspection / asset authoring only; visual acceptance NOT_RUN.

Selected source: `Assets/ThirdParty/Diesel.fbx` (user-provided, initially untracked). A second import exists at `Assets/ThirdParty/S14 ASSETS/Diesel.fbx`; retained untouched. Both imported scenes contained alternative geometry, lights/backdrop, and two Mixamo skeletons; neither had an assigned Humanoid Animator in the initial inspection.

Integration selects `Armature` (import scale 100) and only its bound skin parts: `retopo_body.002`, `jacket.003`, `shirt.003`, `pants.003`, `boots.003`, `hair.003`, `eyes.003`, `beard.003`, `mid_eyebrow.002`. The source mesh dimensions were approximately 1.8 m for the selected body. Import scaling stays within the skeleton; the player/body root is unit scale and at the capsule foot origin. Runtime weapons are positioned in world metres, not scaled as children of imported 100x bones.

| Humanoid mapping | Source |
|---|---|
| Hips | mixamorig:Hips |
| Spine / Chest / UpperChest | Spine / Spine1 / Spine2 |
| Neck / Head | Neck / Head |
| Left/Right shoulder | LeftShoulder / RightShoulder |
| Upper/lower arms, hands | Left/Right Arm, ForeArm, Hand |
| Fingers | Left/Right Hand Thumb/Index/Middle/Ring/Pinky 1–3 |
| Upper/lower legs, feet, toes | Left/Right UpLeg, Leg, Foot, ToeBase |

Each retained skin has 65 source bone bindings. Authoring uses `HumanTrait.BoneName` for humanoid names, default joint limits, 0.02 arm/leg stretch, and checks AvatarBuilder output before saving. `Diesel_Humanoid.asset` is project-owned; no vendor reimport configuration changes. Required bones are checked by authored EditMode tests, not yet run.

Art: project-owned URP/Lit Skin, Jacket, Shirt, Pants, Boots, Eyes and Hair materials. Forest/wood/neutral tones follow the approved S013 palette; skin/hair have separate colors. Roughness is represented by low smoothness (0.16; eyes 0.30), metallic 0. No added high-resolution textures. Diesel had no usable texture assignments; detailed garment/face texturing is not claimed. Existing FPS textures and normal maps remain on tinted material copies.

Authoring bounds account for the root-bone scale; AlwaysAnimate keeps the local shadow body/IK evaluated. No final statement about shoulder deformation, bind-pose accuracy, ground contact, silhouette or lighting can be made without the user's acceptance pass.
