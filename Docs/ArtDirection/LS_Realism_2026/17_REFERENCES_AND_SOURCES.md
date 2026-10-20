# Evidence and external reference register

**Research date:** 2026-10-19. Sources are URLs, not downloaded asset bundles or guarantees of real-time price/license at future date. Files on local GitHub `main` baseline are private/user repository sources, not public web references. Store listings and Unity documentation can change; validate at acquisition/implementation.

## Unity official

- Pipeline choice and limitations: https://docs.unity.com/en-us/engine/6000.5/manual/render-pipelines/choose-a-render-pipeline/pipeline
- URP/HDRP feature comparison: https://docs.unity.com/en-us/engine/6000.5/manual/render-pipelines/choose-a-render-pipeline/feature-comparison
- Metal graphics compatibility: https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/cross-platform-features/graphics-apis/metal/requirements-and-compatibility
- Pipeline migration warning (HDRP → URP, analogous manual-conversion risk; **not** a complete URP → HDRP guide): https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/migrating-from-hdrp/workflow
- Unity Asset Store EULA: https://unity.com/legal/as-terms

## Candidate source pages

- https://assetstore.unity.com/packages/3d/vegetation/trees/european-forests-realistic-trees-229716 — listing confirmed FREE, historical URP/HDRP compatibility (2020.3.15), Standard EULA; 6000.5 compatibility untested.
- https://assetstore.unity.com/packages/3d/vegetation/flora-form-266407 — listing confirmed FREE; historic 2021.3.31 URP yes/HDRP no; 6000.5 untested.
- https://polyhaven.com/a/fir_tree_01 — 3D tree source, CC0 on Poly Haven; game-ready retopology/LOD unverified.
- https://polyhaven.com/a/forrest_ground_01 — CC0 PBR forest ground.
- https://polyhaven.com/a/forrest_ground_03 — CC0 PBR forest ground variant.
- https://polyhaven.com/a/asphalt_02 — CC0 PBR asphalt (recheck current download availability).
- https://polyhaven.com/a/asphalt_01 — CC0 PBR asphalt alternative.
- https://polyhaven.com/a/corrugated_iron — CC0 material (verify at actual acquisition).
- https://polyhaven.com/a/wooden_planks — CC0 material (verify at actual acquisition).
- https://polyhaven.com/license — licensing terms / CC0 policy.

## Internal repository primary baseline

- `Docs/Implementation/S013/S013_ART_BIBLE.md` — prior art standard.
- `Docs/Last_Signal_Docs_v0.2/sources/19_Art_Audio_Asset_Pipeline.md` — import/animation pipeline.
- `Docs/Last_Signal_Docs_v0.2/sources/20_Performance_Build_Operations.md` — profiling and FPS hypothesis.
- `Docs/Implementation/FirstTownCoreForest/{HANDOFF,DESIGN_AND_ASSETS,MANUAL_QA,PERFORMANCE_PLAN}.md` — known forest status and URP shader ownership.
- `Docs/Implementation/FirstTownAtmosphere/{HANDOFF,MANUAL_QA}.md` — sky, wind, rain, time, weather state.
- `Docs/Implementation/FirstTownVisualSlice/REFINEMENT_05.md` — stage hash preservation.
- `Docs/BLD_Production_Knowledge_Base_v2_2026-10-17/00_GOVERNANCE/05_ASSET_ART_RULES.md` — proposed production art governance.
- `Packages/manifest.json` — com.unity.render-pipelines.universal 17.5.0.

## Copyright and comparison ethics

Reference screenshots must be stored only under rights-appropriate workflow; avoid republishing copyrighted external game screenshots bundled into commercial project as assets. Directly copying game shader code, art assets, map layouts or design-specific logo/landmarks is prohibited. Creative aspiration does not grant redistribution license.
