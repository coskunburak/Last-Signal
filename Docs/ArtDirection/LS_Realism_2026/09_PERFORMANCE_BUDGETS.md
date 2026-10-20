# Realistic environment performance plan — evidence-based budgets

**Target hypothesis:** 1080p / 60fps Windows PC build = 16.67ms frame budget. `LS-DOC-20` originally specifies p95<=16.67ms, p99<=25ms as preliminary until a minimum GPU/CPU/VRAM is chosen. Those remain tentative, not a claim achieved. **No benchmark has been run by this documentation task.** Mac M3 Pro is development validation; never extrapolate PC release throughput from Editor frame rate.

## Must capture

Build commit, dirty state, OS, CPU, GPU and GPU driver, RAM/VRAM, Unity/editor, URP vs HDRP, quality preset, resolution, upscaling/render scale, FOV, sky/weather/time, draw distance, content seed and entity counts, scene version. Repeat a >=60s traversal after warmup, five runs if practical; record CPU/GPU frame p50/p95/p99, maximum stalls, frame timing spikes, draw/batches/SetPass, triangles, GPU overdraw, texture/total memory, shader compile stutter, GC alloc and count, physics fixed-step load and lights/shadow casters.

## Per-tier hypotheses (NOT signed budgets)

| Feature | Low | Medium | High | Gameplay invariant |
|---|---|---|---|---|
| Foliage draw distance | closer with fewer decorative blades | moderate | farther & denser | functional cuttable trees unchanged unless LOD-correct |
| Shadow cascade/range | trimmed | balanced | higher | silhouettes/threat clarity |
| Ground clutter | broad strokes | layered | local fine litter | pickup locations and collision constant |
| Volumetrics | reduced/off | conservative | higher quality if measured | no loss of gameplay signaling |
| Texture residency | aggressive compression and mip streaming | balanced | more hero resolution | readable interactions |
| Tree LOD | geometric LOD + impostor | geometric LOD + impostor | longer LOD0 near camera | persistent resource identity stable |
| Wind | bounded shader animation | bounded | bounded/high fidelity | no gameplay time authority |
| Post | restrained | restrained | restrained | visibility/accessibility |

## Risk-specific profiling

- **Trees:** alpha-overdraw often dominates despite low triangle count; view along forest edge at noon and through backlit needles at sunset. Count true geometry LOD savings; fading full mesh until cull is **not** real LOD.
- **Shadows:** canopy double-sided/depth passes may dominate GPU; optimize shadow caster LOD and range separately from camera LOD.
- **Terrain:** multiple terrain splats/normal maps/height blends, detail instancing and texture copies accumulate sampling and memory costs.
- **Rain:** in previous atmosphere implementation bounded 2,154 particles and 41 candidate lights are authored, with active lights budgeted; validate with dense foliage/vehicle/zombies rather than alone.
- **Physics:** large falling trees are dynamic, not statically batched; remove expensive mesh colliders and bound joints/fragments; profile peak contact cost while zombies and vehicle active.
- **Content streaming:** don't accidentally duplicate texture sets across cells; retain manual save and scene unload/readmission test.

## Gates and adaptive optimization order

1. Same-content benchmark and spike source identification (Profiler/Frame Debugger/GPU capture).
2. Alpha overdraw and shadow caster inspection; improve leaf card density/material alpha cutoff and impostors.
3. Texture mipmaps, atlas and compression; trim instances/mesh LODs before changing gameplay distance.
4. Terrain/detail clustering and meaningful occlusion; avoid creating per-object script updates.
5. Lighting/rain/volumetric quality tiers; maintain readability.
6. Compare URP/HDRP only after both reach reasonable parity; measure any conversion cost.

A quality setting may lower decorative density but must not change loot, tree damage authority, zombie sensing/collision, road physics or important world geometry. If target GPU cannot pass with acceptable art, narrow scene density/target feature set and get explicit owner signoff — no fake PASS.
