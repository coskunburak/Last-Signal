# Fill-in evidence receipts — blank by design

All blanks explicitly mean **NOT_RUN / NOT_VERIFIED**. Never autofill `PASS` without user evidence.

## A. Vendor acquisition and source mapping

```yaml
asset_id: <unique>
publisher: <name>
listing_url: <url>
channel: <assetstore|polyhaven|fab|itch|other>
listed_free_at: <utc-date>
license_url_and_edition: <url/version>
account_entitlement_proof_private_reference: <receipt-id or PENDING>
original_archive_sha256: <hash or PENDING>
local_source_path: <Assets/ThirdParty/...>
project_owned_variant_path: <Assets/LastSignal/Art/Realism/...>
unity_editor_version: <version>
pipeline_tested: <URP17.5|HDRP17.x|neither>
mesh_lod0_lod1_lod2_triangles: <values or NOT_MEASURED>
shadow_alpha_wind_verified: <PASS|FAIL|NOT_RUN>
trunk_cutting_compatibility: <PASS|FAIL|NOT_RUN>
license_release_gate: <APPROVED|BLOCKED|PENDING>
quality_gate: <ACCEPT|ADAPT|REJECT|DEFER>
```

## B. Visual acceptance

```yaml
run_id: <timestamp>
scene_path: <...>
scene_and_terrain_sha: <hashes>
git_branch_commit_dirty: <...>
weather_and_time: <...>
resolution_fov_quality: <...>
baseline_image: <...>
candidate_image: <...>
case_ids: [V01, V02, V03]
owner_visual_result: <PASS|FAIL|NOT_RUN>
notes: <...>
```

## C. Performance

```yaml
run_id: <timestamp>
platform: <mac|windows>
player_or_editor: <...>
cpu_gpu_ram_vram_driver: <...>
git_commit_rp_unity_scene: <...>
resolution_settings_render_scale: <...>
route_seed_time_weather: <...>
frame_cpu_p50_p95_p99_ms: <...>
frame_gpu_p50_p95_p99_ms: <...>
frame_total_p50_p95_p99_ms: <...>
gpu_memory_peak_mb: <...>
renderers_triangles_batches_overdraw: <...>
physics_dynamic_trees_and_peak_ms: <...>
per_frame_gc_allocations: <...>
result: <PASS|FAIL|NOT_MEASURED>
```

## D. Production sprint

```yaml
sprint_id: <LS-AR-xx>
owner: <name>
source_ref: <branch sha>
protected_source_artifacts_sha: <...>
changed_file_paths: <...>
new_vendor_package_license_ids: <...>
focused_test_commands_owner_operated: <...>
actual_test_xml_or_logs: <...>
visual_owner_receipt: <...>
performance_owner_receipt: <...>
blocked_open_issues: <...>
rollback_checkpoint: <...>
closure: <PASS|PARTIAL|BLOCKED|NOT_RUN>
```
