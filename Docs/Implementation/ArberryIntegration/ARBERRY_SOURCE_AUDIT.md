# Arberry source audit — 2026-10-13

Status: local metadata inspection and asset authoring performed; verification gates NOT_RUN.

## Provenance and preservation

Publisher: Arberry. Exact listing: https://www.fab.com/listings/11d20d01-b764-4936-8163-cb20d05c369e (Survival Character). The publisher's announcement links this listing: https://forums.unrealengine.com/t/arberry-survival-character-free/2662385 . Public listing reports “Allows usage with AI: No”. No source images, meshes, texture pixels, FBX contents, or embeddings were sent to a generative/vision service. Unity decoded and transformed files locally; model context received only identifiers, aggregate diagnostics and hierarchy metadata.

User-acquired license type, acquisition date and receipt/library record: **UNKNOWN; requested from user, not supplied at handoff**. Public listing displays no license terms in the fetched text. Free availability is not proof of acquired rights. Distribution/production legal acceptance is BLOCKED until the actual license record is supplied. No permission to redistribute raw assets is asserted. Local authoring proceeds under the user's explicit instructions for their imported copy.

Authoritative model resolved using AssetDatabase: `Assets/ThirdParty/Characters/Survival Char/survival_character.fbx`. Texture root: `Assets/ThirdParty/Characters/Survival Char/Textures 2/Textures/`.

Original binary hashes are recorded in [source-preservation.json](source-preservation.json). No original model or texture binary was rewritten. Only the FBX importer metadata was changed for its own Humanoid import; source texture importers and 12 original material remaps remain intact. ThirdParty is ignored by Git: the importer metadata change will not appear in ordinary git diff; retain it in the licensed local asset distribution workflow. Existing untracked `Assets/LastSignal/Art/Arberry/Materials` was preserved. Repeating the explicit Import own Humanoid menu reconstructs the importer settings on an authorized fresh import.

## Findings

See [source-metadata.md](source-metadata.md) for all actual texture paths/dimensions, original remaps, source mesh counts and hierarchy; [source-uv-metadata.txt](source-uv-metadata.txt) for submesh tile coverage; [skin-authoring-metadata.txt](skin-authoring-metadata.txt) for influences/rest bounds.

Initial importer: Generic / NoAvatar, global scale 1, optimize off, no clips. Twelve material names, 11 renderers, 13 material slots, shared pelvis root and 160 bone bindings per skin. No duplicate outfit family exists in this imported model. The selected assembly retains Backpack, Body3, Eye, Eyebrows, Eyeleash, Gloves, Hair, Jacket, Jeans, Mouth1 and Shoes. Body3 contains Head/Body2/Arms submeshes, not three separate full bodies.

Total: **120,917 triangles**. Hair alone: 38,428 triangles; backpack: 22,175. There are 51 body, 9 eye, 13 eyebrow, 14 eyelash and 6 mouth blendshapes. They remain intact; no facial animation system was introduced. All inspected skin vertices are weighted, have normalized weight sums within .02, and valid indices; maximum imported influences is four. Root/object scale is 1. Source forward is consistent with +Z feet/face placement; animated orientation remains pending review.

The exact import warning is: `A polygon of Mesh 'Jacket' in Assets/ThirdParty/Characters/Survival Char/survival_character.fbx is self-intersecting and has been discarded.` This persists from the original source; no speculative topology repair was performed. Human review must determine whether it creates a visible hole.

## Texture coverage and limitations

Base, normal and ORM maps exist for jacket, jeans, gloves, shoes, backpack and body tiles 1001–1004. Eye includes base, normal, AO, gloss, specular and parallax. Hair/brows include diffuse/base, normal, alpha/opacity, AO and specialized direction/root/depth/ID maps. HairCap is a separate supplied family; no corresponding selected renderer was found. There is one supplied outfit texture family in this import, not a verified complete marketplace library.

UV metadata establishes Head→1001, Body2→1002, Arms→1003. Tile1004 is unused by the selected body. Repeat addressing samples each assigned tile without modifying UVs. The mouth normal is actually named `Teet_Normal.png`; it is present. No required base/normal file was missing for a selected material. Unknown: whether the original package has additional tint masks/material graphs outside this imported folder; documentation cannot certify package completeness from this subset.

Cavity/SSS, parallax, strand flow/depth/ID/root controls are preserved but not reproduced by stock URP/Lit. Normal green-channel convention and hair back-face acceptability need visual confirmation. No visual fidelity comparison to official renders was performed.
