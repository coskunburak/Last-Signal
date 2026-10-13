# Resource budget — measurement pending

| Resource | Configured bound / policy |
|---|---|
| Wounds | 8 normal, 4 reduced effects per actor; merge within 2.5 cm; deterministic replacement |
| Surface cache | 15 production meshes, max 16,000 vertices per supported mesh; arrays acquired once per actor |
| Mutable meshes | Head and Ribcage only: at most 2 per actor; source data retained for reset |
| Overlay materials | One shared existing material; reusable MaterialPropertyBlocks; no per-shot material clones |
| Overlay renderers | 15 precreated disabled renderers per actor; only injured visible surfaces draw within 35 m |
| Immediate hit slots | 12 (profile hard limit 16); each owns burst, bleed and drip systems |
| Ordinary blood | 5 baseline burst particles; .8 s diminishing bleed; 1.2 s drip; 1.5 s tail |
| Corpse blood | .45 × living bleed duration; no lifetime extension |
| Existing sever effects | 16 sets; existing region tuning retained |
| Environmental marks | Existing 32 shared slots, 25 s lifetime; bounded probes, no droplet collision callbacks |
| Corpses | 8 active, 30 simulation seconds; fixed 16-entry receiver ceiling |
| Deformation | At most 3 mm indentation; impact-time writes only; sparse regions use shading |

CPU sampling is event-time linear in candidate mesh vertices/triangles. Corpse camera/muzzle queries may sample several aligned corpses; bounds and nearest-distance checks prune candidates. A sustained automatic weapon or overlapping corpse stack must be profiled. Readable mesh assets plus actor-owned CPU arrays consume extra memory; no measured memory/FPS/zero-allocation result is claimed. Initial surface arrays, overlay objects, first deformation meshes and first wound entries allocate; restore/capture also allocates bounded snapshot data. No RenderTextures are used.

Profiler markers: `LastSignal.Wounds.SurfaceResolve`, `LastSignal.Wounds.Update`, `LastSignal.Wounds.Deform`, existing `LastSignal.Combat.BloodHit` and zombie damage/AI markers. Inspect CPU time and GC alongside GPU transparent overdraw, draw calls, skinning and Memory Profiler snapshots.

During the user's Play session, `Last Signal > Zombie > Log Wound Resource Snapshot` logs active actors/wounds/overlays/runtime meshes, runtime mesh bytes, resolution misses, particles, hits, bleeds, marks and corpses. Save the `LS_WOUND_DIAGNOSTICS` lines with profiler captures. Use 1/10/20/30 live zombies, repeated firing, corpse stacking and repeated cell/session changes. Budget values are starting configurations, not measured production acceptance.
