# Performance preparation

Status: BASELINE INSPECTED; no benchmark executed, no FPS improvement claimed. Phase 8 profile changes wait for the earlier migration/verification/removal gates, as requested.

`LastSignal_HDRP_High` exists and remains untouched. No `LastSignal_HDRP_Development` asset exists yet. Existing Medium is not being relabeled as a proven Mac development configuration.

High supports volumetrics/clouds, SSAO, SSR including transparents, subsurface scattering and 4096 shadow atlases; ray tracing and SSGI support are disabled. Support is not proof a feature is active in a frame. Forest authoring uses 80 m directional shadow distance, TAA, dynamic resolution off, 290 canopy trees, one 177k/71k-triangle fir hero, 1,500 fern cull LODs and 9,000 terrain details. These are concrete profiling candidates, not proven bottlenecks. The existing RenderPipelineAdapter.TryApplyPerformanceTier HDRP branch clones the current asset without applying the requested Low/Medium changes: this is a remaining rendering-settings task.

After acceptance, create a dedicated Development copy using Unity authoring APIs: retain the High asset for Windows; initially evaluate 2048 shadow atlases, lower volumetric/reflection quality, shorter shadow distance and conservative LOD/detail distance. Preserve wind, alpha clipping, terrain blending and gameplay. Ray tracing remains disabled on Mac. Mac and Windows both use Standalone quality defaults, so do not change that shared default under the assumption it is Mac-only; explicit editor profile selection or platform-aware build setup is required.

## Repeatable owner-operated comparison

1. Let imports/shader compilation finish. Record hardware/OS, editor/player, power mode, quality asset, build target, VSync and cap. Set Game view 1920×1080; close extra Scene view rendering.
2. Open ForestPrototype120, use the unchanged generation and preset. For F1/F2/F3/F5/F6 compare identical poses/time/weather. F7 Morning, F8 Overcast.
3. Press F9 once per sample: existing recorder warms up 180 frames, then records 30 seconds. Keep pose still. Repeat 3 times per profile; include a matching trail walk. Capture High before Development at the same configuration.
4. Keep frames.csv/summary.txt from the path printed by ForestPrototypeCapture under Application.persistentDataPath/ForestPrototype120. Collect average FPS, P50/P95/P99 frame time, main/render-thread time, GPU time if available, allocations, memory, draw calls and triangles. Blank GPU counters are unavailable, not zero.
5. Use CPU/GPU Profiler and Frame Debugger to attribute shadows, alpha overdraw, volumetrics, reflections, geometry/LOD and Editor overhead. Thread markers may include waits. Review both performance and screenshots before selecting lower costs.
6. Repeat on Windows at 1080p with the intended graphics API. Production High quality is a separate acceptance target.

The cache cleanup can temporarily worsen first-open/first-build latency. Steady-state disk usage and frame times remain unmeasured. No performance tests should be run automatically by these tools.
