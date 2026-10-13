# Root cause analysis — 2026-10-11
Status: IMPLEMENTED / USER VERIFICATION PENDING. Local branch: s012-integrated-graybox-slice. Unity 6000.5.0f1; connected Editor scene S013Cabin, stopped. Pre-existing dirty work preserved.

| Path | Verified local defect | Correction |
|---|---|---|
| S021CombatAuthoring.FirearmPrefab → VAL Generic FPS rig | Bounds-centred pistol under animated rifle wpn_body; rifle trigger and ScopeLens remained enabled; rifle scope presenter and two-hand rifle action clips inherited | Independent owned visual root, measured pistol contacts, neutral existing Generic Animator, final right-arm solve; inherited rifle-only renderers/scope disabled |
| S021CombatAuthoring.MeleePrefab → crowbar Generic arms | Machete/axe scaled and centred on old crowbar bounds; both source shafts run along +Z, not the crowbar axis | Native metre scale; contacts measured on actual handles; distinct profile arm targets |
| CharacterWeaponRig | All firearms required left hand and stock; melee directly offset from wrist, no palm alignment or arm solve | Typed grip modes; one-hand branch with primary palm contact and bounded two-bone solve |
| World melee scale | WorldScale applied only to firearms, while S021 melee profiles inherited .85 | Explicit S021 WorldScale=1 and uniform native geometry scale in both views |

Mesh inspection used imported mesh vertices and actual prefab bone/renderer hierarchies through Editor authoring MCP. Tactical Pistol: length .2405 m; machete: .7548 m; axe: .6303 m. FPS arms are two different Generic skeletons; Diesel is Humanoid. No vendor source was edited.

The attachment contained the request text only: the three referenced screenshots were not present. Therefore the retained rifle lens/trigger are confirmed defects and plausible dark-protrusion sources, but correspondence to the screenshot cannot be conclusively established. Actual Game-view confirmation remains required.

No verification gates were run. An incidental authoring import reported CS0136 for a duplicated local `step`; it was renamed before asset authoring proceeded. This is not compilation PASS evidence.
