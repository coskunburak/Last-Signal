# First zombie decision — S004-B0B

**Selected production model: Hotstrike Studio — Free Stylized Dark Fantasy Zombie (Unity SK variant). B0B technical PASS; P2 technical entry READY. P2 implementation NOT_STARTED.**

- Source: `Assets/LastSignal/Assets/SZombie/SZombie_Variant_1/Unity/SK_SZombie_Variant_1.fbx`. Selected over general/Cascadeur/static alternatives because it supplies the correct upright meter-scale Unity skin and stable Humanoid retarget.
- Rig: its own valid `SK_SZombie_Variant_1Avatar`; 52 mapped humanoid bones, full fingers. Source skeleton preserved; +Z visual facing and ground pivot require no adapter rotation.
- Material: project-owned `M_Zombie_Shambler_URP`, supplied A palette, correct normal and linear packed smoothness/AO/metallic. No pink shader.
- Animation pack: Kevin Iglesias, Zombie Monster Animations FREE; exact five-role mapping in [matrix](ZOMBIE_ANIMATION_MATRIX.md). Project-only Death floor correction preserves source muscle/rotation curves.
- Prefab: `Assets/LastSignal/Enemies/Zombie/Prefabs/LS_Zombie_Shambler.prefab`, nested FBX, one Animator under its skeleton owner. Head/Chest references are added bone children; FeetReference remains at actor ground origin.
- License: public publisher/EULA sources recorded; **LICENSE_EVIDENCE_PENDING** for local acquisition/entitlement. This is technical acceptance under the user-supplied asset scope, not commercial release clearance.
- Readiness: measured scale, Humanoid, URP, five animation roles, visual evidence and full 38/38 EditMode + 23/23 PlayMode pass. No AI/health/navigation/combat damage added.

One slow Shambler remains the first archetype. The stylized pale-green skin/dark cloth is accepted as the supplied asset direction; the expressive death motion is retained. No texture redesign. Later agent-speed/foot-contact tuning, slope-specific death handling, population LOD/profile and commercial entitlement evidence remain explicit follow-ups.

[B0B report](S004_B0B_ZOMBIE_ASSET_INTEGRATION_REPORT.md) · [evidence](Evidence/20260918-B0B/README.md)
