# PERF-S001 — Shadow implementation

## HDRP global control

Local HDRP package source verified under `Library/PackageCache/com.unity.render-pipelines.high-definition@66cdf30f7d1a/`:

- `Runtime/RenderPipeline/Camera/HDAdditionalCameraData.cs`: customRenderingSettings, ref renderingPathCustomFrameSettings, override mask, volumeLayerMask and volumeAnchorOverride.
- `Runtime/RenderPipeline/Settings/FrameSettings.cs`: ShadowMaps, ScreenSpaceShadows, ContactShadows, Volumetrics and SSAO fields/mask.
- `Runtime/Lighting/Light/HDAdditionalLightData.cs`: SetShadowResolution, SetShadowResolutionLevel and SetShadowResolutionOverride.
- `Runtime/Lighting/Shadow/HDShadowManager.cs`: per-light-type maximum resolutions.
- Core package `Runtime/Volume/VolumeManager.cs`: isolated CreateStack/Update/DestroyStack diagnostic evaluation.

Global Off overrides camera ShadowMaps, ScreenSpaceShadows and ContactShadows to false. Global On restores the authored screen-space-shadow choice; shadow-map resolution tiers are 512/1024/2048, capped by active HDRP asset limits. Original light resolution override/level and camera fields are captured and restored. Light enabled/intensity and Light.shadows stay under scene/atmosphere authority (FirstTown budgets lamp shadows dynamically). Baked lighting and SSAO are distinct from real-time shadows; Global Off does not erase them. No legacy QualitySettings.shadows control or HDRP asset mutation/reconstruction.

Only registered Game cameras with HDAdditionalCameraData receive frame overrides. Dynamic cameras must register explicitly. Ray tracing, cached/on-demand shadow refresh and custom render pipelines are not implemented paths. Current pipeline has ray tracing and screen-space GI disabled.

## Volume distance

Runtime-only profile contains just HDShadowSettings.maxShadowDistance override. For each registered camera mask, manager creates one global runtime Volume on an included layer, priority float.MaxValue, weight 1. Other artistic parameters keep original local/global Volume blending. User distance intentionally takes precedence over local artistic shadow-distance overrides. Empty masks emit a diagnostic; distance cannot affect such a camera. Do not create competing volumes at the same maximum priority.

Disable/restore releases influence; manager disposal destroys owned Volume objects, profile and components. No shared source profile mutation. `TryReadEffectiveDistance` evaluates the camera mask and anchor through a separate VolumeStack without replacing the live camera stack. Owner report command and PlayMode test check composition against a competing scene Volume. This is **not yet a rendered-frame validation**; camera frame sanitization, platform, light range/cascades and shader availability still constrain actual shadows.

## Tree policy

Each explicit tree root caches its MeshRenderer/SkinnedMeshRenderer descendants, including inactive LODs, once at registration. No names, material names or mesh naming heuristics in runtime classification. Off changes only shadowCastingMode. High restores the exact original mode (including Off, TwoSided, ShadowsOnly). Low enables complete trees within 20 m; Medium within 40 m. Exit radius adds 3 m hysteresis; refresh every 0.25 seconds uses squared distance. Switching profiles forces immediate evaluation.

Mixed trunk/foliage assets use whole-tree distance gating. No unsupported trunk-only claim. These levels have distinct behavior even though a tree inside 20 m may look identical across enabled levels. High remains bounded by global distance and global shadows. Enabling a previously inactive cached renderer or switching mesh in-place retains its policy. Newly created deeper descendants must call Refresh; dynamically spawned independent trees call Attach. Disabling/unloading restores original modes and unregisters.

ForestPrototype120 explicitly serializes canopy, hero and distant roots; child LOD trees register separately, leaving the distant ground apron alone. Active families: European Forests Spruce 1/4/6/8/9, Birch 2, Poly Haven fir. Production FirstTownCore serializes ForestEdge, MixedWoodland, DeepForest roots; generator writes equivalent refs for new generations. Older validation copies without these references are not silently classified. Interactive ResourceNodeView registers its root; current TreePhysicsBody changes existing meshes and Rigidbody/joints, so the shadow controller does not touch its simulation or reward state.

Terrain detail grass is retained; audited ForestPrototype120 has zero Terrain tree instances. Terrain treeInstances, billboard-only/indirect/custom GPU vegetation require another adapter before claiming support. Renderer-based instancing retains its renderer shadow flags. Ordinary LODGroups are covered; asset-specific crossfade/impostor visuals await owner inspection. Multi-camera tree distance selection and terrain grass density remain future work.

## Evidence limits

Existing owner captures:

| Capture | Avg FPS | P50 ms | P95 ms | P99 ms |
|---|---:|---:|---:|---:|
| A original forest, 08:15:25 | 5.466 | 182.257 | 367.610 | 367.854 |
| B vegetation renderers off, grass retained, 08:18:43 | 122.080 | 4.549 | 13.262 | 13.388 |

Evidence under `Evidence/20261023T0815252187600Z-cf61c2da6bfe4055a9210ebd3db2125e/` and `Evidence/20261023T0818436045980Z-445349c333d44e869fdc1653313e1f38/`. Camera orientations differ; B disables full vegetation geometry. These values do **not** quantify tree-shadow-only gains. New Off/Low/Medium/High, Windows, standalone and shadow-pass GPU cost: **NOT_MEASURED**. Existing native audits count 5792 MeshRenderers, 2300 LODGroups and 382 colliders in the forest; no geometry was removed by this sprint.
