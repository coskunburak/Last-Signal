# Scoped mask candidate preparation

> Current instruction (2026-10-22): Unity runs are owner-operated. See [CURRENT_HANDOFF.md](CURRENT_HANDOFF.md) for the next exact commands and latest reviewed results. Earlier next-step and authorization statements below are historical.


Owner shader gate: **18/18 checked, zero errors**. Three localized-wound loop-unrolling warnings remain; no shader modification is justified by these warnings alone. Receipts: Evidence/shader-gate-review.json and the original owner log/report. This does not certify all shader variants or a Windows build.

Implemented `HdrpProductionMaskCandidates.Prepare` and registered `prepare-mask-candidates` in the existing manual runner. This is the next bounded Unity authoring gate; the agent has not executed it or compiled the new C#.

```bash
cd '/Users/burakcoskun/Last Signal'
python3 Tools/HDRPProductionOptimization/manual_checks.py prepare-mask-candidates
```

Save and close Unity first. This command compiles the added utility and stages masks; it is not a test-suite run. Do not repeat the six previously passing audit tests without affected changes.

## Scope and safeguards

- Require the exact owner audit hash, 43 provenance-resolved project-owned materials, 40 distinct source textures, and all 170 current file checksums. Fail before mutation if input identity, current HDRP shader, material local ID, source GUID, assigned mask, detail map, UV mode or basic importer contract differs.
- Copy each PNG/TIFF/TGA source into a unique new project-owned generation and change only that copy to linear/uncompressed/readable input. Unity's importer supplies decoding. No new third-party decoder and no vendor import changes.
- Preserve full source width/height up to 8192 per axis. Fail rather than silently resize larger sources. Pack RGBA8: raw source R, constant G=1, constant B=1, raw source A. Source smoothness stays in the plan for later material remapping; it is not baked twice. Raw source data interpretation deliberately corrects the sRGB data-map issue, so later visual review is required rather than assuming old sampling was correct.
- Share one packed candidate per source GUID. Verify every PNG pixel against the staged source's R/A values and constant G/B; retain source sampling/wrap settings in the output importer. Source-to-RGBA8 precision is an explicit mask-format choice, not preservation of higher-bit source precision.
- Remove only the newly-created decode copy and its meta through AssetDatabase after its output passes verification. Never delete original assets. A failure retains partial candidate output for diagnosis; no automatic broad cleanup or retry overwrite.
- Recheck all original guards after authoring. Do not assign/save any existing material, prefab, scene, pipeline asset or vendor importer. This stage cannot fix the visible materials by itself and must not be reported as final migration.

Outputs: `Assets/LastSignal/Art/HdrpMaterialRecovery/ProductionCandidates/G<timestamp-id>/` and `Evidence/mask-candidates-<timestamp-id>.json`. Expected status: **CANDIDATES_VERIFIED_NOT_APPLIED**, 40 outputs covering 43 materials. Return that receipt plus the runner's timestamped process.json and unity.log. On failure, stop and return the receipt/log before retrying.

After a successful candidate receipt, inspect source/candidate channels and material state, then implement bounded assignment with current-file backups and scalar remaps using the existing mask utility. Follow with production coverage and owner visual checks. Resolve the two embedded weapon normal maps and source-authoring dependencies separately. Do not remove URP or serialized components before parity verification.

No additional source files were modified by this authoring operation yet: only its C# tool/meta and runner registration were implemented. The 108,402-file checkpoint, Git/LFS, vendor assets, ForestPrototype120 and S004 remain preserved.
