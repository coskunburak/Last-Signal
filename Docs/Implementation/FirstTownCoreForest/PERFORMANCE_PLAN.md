# Performance setup and owner measurement

**NOT_MEASURED.** No FPS improvement, 60 FPS guarantee, memory reduction or Windows retail result is claimed. Proposed first target: 1080p/60 FPS on the owner's declared PC hardware.

Implementation budgets are authoring ceilings, not measured costs: <=340 tree roots, <=520 small bush roots, <=85 rock roots, <=16 bark debris roots; <=12,000 generated terrain detail units. Candidate loops are bounded. Terrain detail rendering distance45m, one four-layer splat group, immutable shared mesh/material resources. No per-tree Update/MonoBehaviour controller, no per-frame scene search or material clone. Tree wind is one shader-global bridge. Source mesh topology is retained; canopy LOD is dithered culling, not geometric reduction.

Pine/Birch cost 1,554/1,192 triangles each. Bush54, Rock658, Grass4. Those are source metadata, not rendered-triangle measurements. Grass copies use Terrain instancing; ordinary renderer materials are eligible for instancing/SRP Batcher, but these are different submission paths and no simultaneous benefit on the same draw is asserted. Wind objects are not statically batched because displacement uses object-space geometry. Shadows use the current pipeline's 50m main-shadow distance; ground cover/bushes cast no shadows. Wind/depth/shadow vertices use the same deformation. Alpha overdraw remains a visual/performance risk.

Capture before/after at identical resolution, quality, route, FOV, time/weather and hardware. Record separate device/OS results. Warm up first; take >=60s per case:

1. Town center looking toward forest.
2. Forest edge walk/run/crouch.
3. Dense west/north grove and clearing.
4. Western vehicle loop, including entry/exit and reverse turn.
5. Golden-hour canopy shadows.
6. Night and flashlight sweep.
7. Heavy rain, wet forest and atmosphere probe refresh.
8. Existing zombie pursuing from town into woodland and retreating back.

Collect CPU/GPU ms, frame p50/p95/p99, draw calls/batches/SetPass, rendered triangles, per-frame GC, total/texture memory, renderer/shadow-caster counts, alpha overdraw, scene load time and navigation bake time. Include worst frames and probe/shadow spikes; average FPS alone is insufficient.

Owner tools: Unity Profiler, Frame Debugger and Rendering statistics. For shipping comparisons use a user-built player. Mac editor figures are not Windows retail figures. Existing atmosphere light/rain/probe budgets still apply; isolate them when diagnosing a forest cost increase.

Review LOD fade/popping at 20/50/100/200m. If full geometry is costly at distance, benchmark a controlled reduced-geometry/impostor derivative before accepting it; no automated decimation is shipped here. If alpha dominates, reduce grass/bush density or distance before adding a new asset family. If shader variants/instancing regress, compare stock Lit on a review copy and inspect actual draw submission. Keep the same acceptance camera/lighting while changing one factor.
