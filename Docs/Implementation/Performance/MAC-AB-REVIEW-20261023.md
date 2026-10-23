# Mac A/B — first comparison, camera mismatch

Owner B run: 20261023T0818436045980Z-445349c333d44e869fdc1653313e1f38. Source CSV/JSON/TXT archived with hashes in Evidence. No render settings or source modified.

| Metric | A | B vegetation off (grass retained) |
|---|---:|---:|
| Average FPS | 5.466 | 122.080 |
| P50 ms (nearest rank) | 182.257 | 4.549 |
| P95 ms | 367.610 | 13.262 |
| P99 ms | 367.854 | 13.388 |
| Triangle counter | 5,152,360 | 257,397 |
| SetPass | 72 | 42 |
| GPU samples | 0 | 3,254 |
| Frames >100ms | 130/164 | 0/3,664 |

**Not a controlled single-variable comparison yet:** A camera angles (0,65,0), B (12.480,90.170,approximately 0). Position identical (24,9.81199,82). Resolution 1649x860, High/PC/Metal, generation/digest, AA, FOV, VSync/cap match. Both completed without focus loss/configurationChanged; that flag does not check camera movement between runs. No percentage gain or permanent optimization acceptance is claimed.

Vegetation rendering is a strong leading hypothesis given the large observed change and falling render counters. Camera-angle confounding and one sample per condition prevent final attribution. Geometry versus alpha overdraw versus shadow versus submission cost remains unresolved. A GPU values remain unavailable even though B exposes positive GPU samples; do not infer A GPU time from B. B GPU median: 10.514708337723278 ms over valid samples. No GC collection increments in either capture. Account warning is not a proven cause.

Next: repeat A (already restored) with Game view focused, Fn+F3 reset, then Fn+F9 without mouse movement. After completion choose B, refocus Game view, Fn+F3 again and Fn+F9 without moving mouse. Keep viewport layout/dimensions unchanged. Send both completion paths; then proceed to C tree-shadow isolation at the same pose if comparison holds. No code/quality change until this gate.
