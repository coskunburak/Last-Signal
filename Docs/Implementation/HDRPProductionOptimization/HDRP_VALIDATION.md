# HDRP validation

> Current instruction (2026-10-22): Unity runs are owner-operated. See [CURRENT_HANDOFF.md](CURRENT_HANDOFF.md) for the next exact commands and latest reviewed results. Earlier next-step and authorization statements below are historical.


Latest gate: owner shader validation **18/18 PASS, zero errors**. The previous shader command below is completed history. Current next command: `python3 Tools/HDRPProductionOptimization/manual_checks.py prepare-mask-candidates`. See [MASK_CANDIDATES_HANDOFF.md](MASK_CANDIDATES_HANDOFF.md) for exact scope and safeguards.

Latest owner evidence: compilation PASS; focused audit tests **6/6 PASS**, zero skipped; production audit collected successfully but found **45 active materials needing repair** and **46 URP component instances**. See [AUDIT_RESULTS_REVIEW.md](AUDIT_RESULTS_REVIEW.md). The earlier “run now” commands below are completed history; next gate is `python3 Tools/HDRPProductionOptimization/manual_checks.py validate-shaders`. Do not rerun passing suites without affected changes.

Overall: PARTIAL / owner-run gate pending. Static configuration is verified; compilation, rendered appearance, gameplay and Windows build are NOT VERIFIED for this task.

- Unity ProjectVersion: 6000.5.0f1. Manifest and lock: HDRP/Core/URP 17.5.0. Installed HDRP package.json declares version 17.5.0 and Unity 6000.5. No version upgrade.
- Graphics default and active PC quality index 1 point to `LastSignal_HDRP_High` (GUID `15722f09b35214c7cac8dee7a2a4c2e3`). Mobile quality points to `LastSignal_HDRP_Medium` (GUID `6c097e46d54844257ad24baa94055d84`), so both serialized quality overrides are HDRP despite their legacy names.
- HDRP global settings map points to `Assets/HDRPDefaultResources/HDRenderPipelineGlobalSettings.asset`; a URP global-settings map entry remains. Existing setup utility's own GlobalSettingsPath constant is not the actual registered global-settings location.
- The default volume asset and scene-local forest Morning/Overcast profiles exist. Their effective appearance needs the owner-run audit/visual review. Do not infer active volumetric cost from asset support flags alone.
- Required package-owner search: no transitive URP owner in the current lock, but project assemblies, shaders, authoring tools and serialized components still depend on it.

[Unity HDRP 17.5 system requirements](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.5/manual/System-Requirements.html) supports Metal on macOS and DX11/DX12 on Windows; the installed package is the source of truth for this exact project's API contracts. No Windows build or package resolution was run.

## Corrected audit contract

Installed `Runtime/Material/Lit/LitAPI.cs:29–40` sets `_NORMALMAP_TANGENT_SPACE` according to coordinate space, independently of map presence. `_NORMALMAP` depends on the selected normal source, or a tangent-space detail map. The old audit's 100 missing-texture flags on the space keyword were not valid conversion evidence. The fix checks sampling and space independently and recognizes object-space/detail-only cases. Six user-run EditMode tests cover valid and invalid cases. No material was changed to silence a warning.

Old coverage evidence (2026-10-21 06:29 UTC) is historical: 129 ACTIVE_NEEDS_REPAIR, 1 ACTIVE_UNSUPPORTED (built-in GUI/Text Shader), 7 SOURCE_UNVERIFIED, 3 missing slots on disabled blood particle root renderers. These require reclassification/context, not blanket deletion or automatic acceptance. Remaining sRGB mask maps require project-owned, correctly packed linear derivatives; simply toggling a vendor importer or renaming the shader is not acceptable. Reinspect base/normal/mask R-metallic G-AO B-detail A-smoothness, alpha, double-sided, emission, transparency and wind.

## Gate evidence requested

The production audit uses existing coverage traversal (prefab overrides/model subassets/terrain data) plus FirstTownCore, ForestPrototype120 and enabled build scenes. It reports pipeline configuration, shader support/errors, missing scripts/slots/references and serialized URP components. Completion means report collected, not visual approval. Native UI shaders, intentional disabled roots and runtime material assignments need explicit review.

Inspect FirstTownCore and ForestPrototype120 in Game view: foliage/bark/terrain wind and shadows; wetness, sky/fog/time/weather; FPS/TPS Arberry/VAL, cloth/skin and grip/ADS; glass, scopes, bullets, wounds/blood, particles and rain. Compare material channels against sources and previous owner captures. No pink surface or missing prefab/script caused by migration is acceptable.

No shader name substitution, global material conversion, forest rebuild, scene save or package removal was executed in this phase.
