# Mac F3 A baseline — owner-run capture

Source run: 20261023T0815252187600Z-cf61c2da6bfe4055a9210ebd3db2125e. Exact CSV/JSON/TXT copied with hashes into Evidence under the same run ID. Original untouched.

- COMPLETED, 30.003 s, 164 frames; High / PC / Metal / M3 Pro / Editor.
- F3 camera stayed fixed; zero unfocused frames; configurationChanged=false.
- Actual resolution **1649×860**, scale 1, dynamic resolution disabled; this is **not 1920×1080 acceptance**.
- Average FPS 5.466; nearest-rank P50 182.257 ms; P95 367.610 ms; P99 367.854 ms.
- 130/164 frames >100 ms; 34/164 >250 ms. Severe slowdown verified in this capture, rather than an isolated screenshot.
- Main Thread median approximately 182.463 ms; includes waits. FTM CPU present, GPU samples zero. Render Thread / Draw Calls / Batches unavailable. CPU-bound versus GPU-bound remains unresolved.
- Rendering counters constant: triangles 5,152,360, vertices 6,611,616, SetPass 72. Editor counters may include other views and passes; not unique visible scene geometry.
- Total Used Memory 3,726,517,618–3,732,992,856 bytes. GC allocation median 11,464 bytes/frame, max 44,771. No recorded GC collection-count increments. GC collection stalls are not supported as explanation for this sample; allocation source still requires Timeline.
- Reported graphics memory 12,288 MB is driver-reported on unified memory hardware, not verified dedicated VRAM. RAM 18,432 MB. VSync=0, targetFrameRate=-1. Runtime qualityIndex=0, name PC; the runtime report is authoritative for this run rather than the earlier serialized default index.

## Next controlled measurement

Keep current 1649×860 resolution, view/layout, High, Morning and F3 pose for B and C so this A remains comparable. Wait for capture completion before changing experiments. B: Hide vegetation renderers; focus Game view, F9, stay still. B retains terrain grass and colliders. Then A Restore; C tree shadows off, F9; finally A Restore. No Play Mode exit between scenarios is required. If viewport size/layout changed, rerun A at the new fixed size before B. Later perform the standardized 1920×1080 repeated runs separately.

First request only B output to determine whether vegetation rendering explains most cost. No permanent density/LOD/HDRP/physics change justified yet. CPU Timeline will be required to distinguish active CPU work from present/GPU waits. Account API warning alone is not a proven frame-time cause.
