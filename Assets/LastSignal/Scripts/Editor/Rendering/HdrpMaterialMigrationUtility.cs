using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LastSignal.Editor.Rendering
{
    /// <summary>
    /// Audits project and vendor materials; only project-owned standalone materials may be migrated.
    /// Preserves all material asset GUIDs, texture assignments, surface modes, alpha clipping, and wet-surface compatibility.
    /// </summary>
    public static class HdrpMaterialMigrationUtility
    {
        public const string RollbackManifestPath =
            HdrpPipelineSetupUtility.HdrpSettingsFolder + "/MaterialRollbackManifest.json";

        private static readonly string[] SearchFolders =
        {
            "Assets/LastSignal",
            "Assets/ThirdParty",
            "Assets/RainSnowCloudEffect"
        };

        [Serializable]
        public sealed class MaterialAuditRecord
        {
            public string assetPath;
            public string guid;
            public string materialName;
            public string currentShaderName;
            public string targetHdrpShaderName;
            public string migrationAction;
            public bool isOpaque;
            public bool isAlphaClipped;
        }

        [Serializable]
        public sealed class MaterialAuditReport
        {
            public string status;
            public string timestampUtc;
            public int totalMaterialsScanned;
            public int urpLitCount;
            public int urpUnlitCount;
            public int urpParticleCount;
            public int customDualPipelineCount;
            public int alreadyHdrpCount;
            public int otherShaderCount;
            public List<MaterialAuditRecord> records = new List<MaterialAuditRecord>();
        }

        [Serializable]
        public sealed class MaterialRollbackEntry
        {
            public string assetPath;
            public string materialGuid;
            public string originalShaderName;
            public string[] originalKeywords;
            public float smoothnessTextureChannel;
            public float glossMapScale;
            public float workflowMode;
            public Color baseColor;
            public string baseMapGuid;
            public Vector2 baseMapScale;
            public Vector2 baseMapOffset;
            public string bumpMapGuid;
            public float bumpScale;
            public string metallicMapGuid;
            public float metallic;
            public float smoothness;
            public string occlusionMapGuid;
            public float occlusionStrength;
            public string emissionMapGuid;
            public Color emissionColor;
            public bool emissionEnabled;
            public float surfaceType;
            public bool alphaClip;
            public float cutoff;
            public float cull;
            public int customRenderQueue;
        }

        [Serializable]
        public sealed class MaterialRollbackManifest
        {
            public string recordedUtc;
            public int count;
            public List<MaterialRollbackEntry> entries = new List<MaterialRollbackEntry>();
        }

        [Serializable]
        public sealed class MaterialMigrationReport
        {
            public string status;
            public string timestampUtc;
            public int totalScanned;
            public int migratedToHdrpLit;
            public int migratedToHdrpUnlit;
            public int migratedToDualParticleUnlit;
            public int migratedToHdrpTerrainLit;
            public int dualPipelineRetained;
            public int alreadyHdrpValidated;
            public int skippedOther;
            public int skippedProtectedSource;
            public int errorCount;
            public List<string> errors = new List<string>();
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/4a. Audit All Materials")]
        public static void AuditMaterials()
        {
            EnsureDirectoryExists(HdrpPipelineSetupUtility.EvidenceFolder);

            var report = new MaterialAuditReport
            {
                status = "PASS",
                timestampUtc = DateTime.UtcNow.ToString("O")
            };

            string[] guids = FindAllMaterialGuids();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    continue;
                }

                report.totalMaterialsScanned++;
                string shaderName = mat.shader != null ? mat.shader.name : "Hidden/InternalErrorShader";
                ClassifyMaterial(mat, path, guid, shaderName, report);
            }

            string outPath = Path.Combine(HdrpPipelineSetupUtility.EvidenceFolder, "material_migration_audit.json");
            File.WriteAllText(outPath, JsonUtility.ToJson(report, true));
            Debug.Log(
                $"[HdrpMaterialMigrationUtility] Audited {report.totalMaterialsScanned} materials: " +
                $"URP Lit={report.urpLitCount}, URP Unlit={report.urpUnlitCount}, Particles={report.urpParticleCount}, " +
                $"Custom Dual={report.customDualPipelineCount}, Already HDRP={report.alreadyHdrpCount}.");
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/4b. Migrate All Materials to HDRP")]
        public static void MigrateAllMaterialsToHdrp()
        {
            // The legacy manifest does not capture complete vendor or embedded material state.
            // Never replay the broad migration over the established production migration.
            if (File.Exists(RollbackManifestPath))
                throw new InvalidOperationException("Existing migration detected. Use repair-material-integrity for the approved P0 scope; preserve the historical manifest.");
            EnsureDirectoryExists(HdrpPipelineSetupUtility.HdrpSettingsFolder);
            EnsureDirectoryExists(HdrpPipelineSetupUtility.EvidenceFolder);

            var hdrpLit = Shader.Find("HDRP/Lit");
            var hdrpUnlit = Shader.Find("HDRP/Unlit");
            var hdrpTerrainLit = Shader.Find("HDRP/TerrainLit");
            var dualParticleUnlit = Shader.Find("LastSignal/VFX/Particles/Unlit");

            if (hdrpLit == null || hdrpUnlit == null)
            {
                throw new InvalidOperationException("HDRP/Lit or HDRP/Unlit shader not found. Ensure HDRP 17.5.0 is imported.");
            }

            var existingManifest = LoadRollbackManifest();
            var rollbackByPath = new Dictionary<string, MaterialRollbackEntry>(StringComparer.Ordinal);
            if (existingManifest != null)
            {
                foreach (var entry in existingManifest.entries)
                {
                    if (!string.IsNullOrEmpty(entry.assetPath))
                    {
                        rollbackByPath[entry.assetPath] = entry;
                    }
                }
            }

            var migrationReport = new MaterialMigrationReport
            {
                timestampUtc = DateTime.UtcNow.ToString("O")
            };

            string[] guids = FindAllMaterialGuids();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    continue;
                }

                migrationReport.totalScanned++;
                if (!path.StartsWith("Assets/LastSignal/", StringComparison.Ordinal) || !path.EndsWith(".mat", StringComparison.Ordinal))
                {
                    migrationReport.skippedProtectedSource++;
                    continue;
                }
                string shaderName = mat.shader != null ? mat.shader.name : string.Empty;

                try
                {
                    if (IsCustomDualPipelineShader(shaderName))
                    {
                        migrationReport.dualPipelineRetained++;
                        continue;
                    }

                    if (shaderName == "HDRP/Lit" || shaderName == "HDRP/Unlit" || shaderName == "HDRP/TerrainLit")
                    {
                        HDMaterial.ValidateMaterial(mat);
                        EditorUtility.SetDirty(mat);
                        migrationReport.alreadyHdrpValidated++;
                        continue;
                    }

                    if (IsUrpOrStandardLitShader(shaderName))
                    {
                        if (!rollbackByPath.ContainsKey(path))
                        {
                            rollbackByPath[path] = CaptureRollbackEntry(mat, path, guid, shaderName);
                        }

                        ConvertMaterialToHdrpLit(mat, hdrpLit);
                        migrationReport.migratedToHdrpLit++;
                    }
                    else if (IsUrpOrLegacyUnlitShader(shaderName))
                    {
                        if (!rollbackByPath.ContainsKey(path))
                        {
                            rollbackByPath[path] = CaptureRollbackEntry(mat, path, guid, shaderName);
                        }

                        ConvertMaterialToHdrpUnlit(mat, hdrpUnlit);
                        migrationReport.migratedToHdrpUnlit++;
                    }
                    else if (IsParticleShader(shaderName) && dualParticleUnlit != null)
                    {
                        if (!rollbackByPath.ContainsKey(path))
                        {
                            rollbackByPath[path] = CaptureRollbackEntry(mat, path, guid, shaderName);
                        }

                        ConvertMaterialToDualParticle(mat, dualParticleUnlit);
                        migrationReport.migratedToDualParticleUnlit++;
                    }
                    else if (IsUrpTerrainShader(shaderName) && hdrpTerrainLit != null)
                    {
                        if (!rollbackByPath.ContainsKey(path))
                        {
                            rollbackByPath[path] = CaptureRollbackEntry(mat, path, guid, shaderName);
                        }

                        mat.shader = hdrpTerrainLit;
                        HDMaterial.ValidateMaterial(mat);
                        EditorUtility.SetDirty(mat);
                        migrationReport.migratedToHdrpTerrainLit++;
                    }
                    else
                    {
                        migrationReport.skippedOther++;
                    }
                }
                catch (Exception ex)
                {
                    migrationReport.errorCount++;
                    migrationReport.errors.Add($"{path} ({shaderName}): {ex.Message}");
                }
            }

            var newManifest = new MaterialRollbackManifest
            {
                recordedUtc = DateTime.UtcNow.ToString("O"),
                count = rollbackByPath.Count,
                entries = new List<MaterialRollbackEntry>(rollbackByPath.Values)
            };
            File.WriteAllText(RollbackManifestPath, JsonUtility.ToJson(newManifest, true));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            migrationReport.status = migrationReport.errorCount == 0 ? "PASS" : "FAIL";
            string reportPath = Path.Combine(HdrpPipelineSetupUtility.EvidenceFolder, "material_migration_execution.json");
            File.WriteAllText(reportPath, JsonUtility.ToJson(migrationReport, true));

            if (migrationReport.errorCount > 0)
            {
                throw new InvalidOperationException(
                    $"Material migration encountered {migrationReport.errorCount} errors. See {reportPath}.");
            }

            Debug.Log(
                $"[HdrpMaterialMigrationUtility] Migrated materials to HDRP: " +
                $"HDRP/Lit={migrationReport.migratedToHdrpLit}, HDRP/Unlit={migrationReport.migratedToHdrpUnlit}, " +
                $"Particles={migrationReport.migratedToDualParticleUnlit}, DualRetained={migrationReport.dualPipelineRetained}.");
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/Rollback: Restore Materials to URP")]
        public static void RollbackMaterialsToUrp()
        {
            var manifest = LoadRollbackManifest();
            if (manifest == null || manifest.entries.Count == 0)
            {
                Debug.LogWarning("[HdrpMaterialMigrationUtility] No MaterialRollbackManifest.json found to roll back.");
                return;
            }

            int restored = 0;
            foreach (var entry in manifest.entries)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(entry.assetPath);
                var shader = Shader.Find(entry.originalShaderName);
                if (mat == null || shader == null)
                {
                    continue;
                }

                mat.shader = shader;
                var baseMap = LoadTextureByGuid(entry.baseMapGuid);
                var bumpMap = LoadTextureByGuid(entry.bumpMapGuid);
                var metallicMap = LoadTextureByGuid(entry.metallicMapGuid);
                var occlusionMap = LoadTextureByGuid(entry.occlusionMapGuid);
                var emissionMap = LoadTextureByGuid(entry.emissionMapGuid);

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", entry.baseColor);
                }
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", entry.baseColor);
                }
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", baseMap);
                    mat.SetTextureScale("_BaseMap", entry.baseMapScale);
                    mat.SetTextureOffset("_BaseMap", entry.baseMapOffset);
                }
                if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", baseMap);
                    mat.SetTextureScale("_MainTex", entry.baseMapScale);
                    mat.SetTextureOffset("_MainTex", entry.baseMapOffset);
                }
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", bumpMap);
                }
                if (mat.HasProperty("_BumpScale"))
                {
                    mat.SetFloat("_BumpScale", entry.bumpScale);
                }
                if (mat.HasProperty("_MetallicGlossMap"))
                {
                    mat.SetTexture("_MetallicGlossMap", metallicMap);
                }
                if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", entry.metallic);
                }
                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", entry.smoothness);
                }
                if (mat.HasProperty("_OcclusionMap"))
                {
                    mat.SetTexture("_OcclusionMap", occlusionMap);
                }
                if (mat.HasProperty("_OcclusionStrength"))
                {
                    mat.SetFloat("_OcclusionStrength", entry.occlusionStrength);
                }
                if (mat.HasProperty("_EmissionMap"))
                {
                    mat.SetTexture("_EmissionMap", emissionMap);
                }
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", entry.emissionColor);
                }
                if (entry.emissionEnabled)
                {
                    mat.EnableKeyword("_EMISSION");
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                }
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", entry.surfaceType);
                }
                if (mat.HasProperty("_AlphaClip"))
                {
                    mat.SetFloat("_AlphaClip", entry.alphaClip ? 1.0f : 0.0f);
                }
                if (mat.HasProperty("_Cutoff"))
                {
                    mat.SetFloat("_Cutoff", entry.cutoff);
                }
                if (mat.HasProperty("_Cull"))
                {
                    mat.SetFloat("_Cull", entry.cull);
                }

                mat.renderQueue = entry.customRenderQueue;
                EditorUtility.SetDirty(mat);
                restored++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HdrpMaterialMigrationUtility] Rolled back {restored} materials to URP.");
        }

        // Authoring copies preserve source identity and use the same channel-aware conversion as migration.
        internal static Material CreateHdrpLitCopy(Material source, string path)
        {
            if (!source || !source.shader || !path.StartsWith("Assets/LastSignal/", StringComparison.Ordinal) ||
                !path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase) || File.Exists(path))
                throw new InvalidOperationException("Expected a new project-owned material path: " + path);
            HdrpMaterialMaskUtility.Capture(source); // Fail before creating an unsupported workflow copy.
            var copy = new Material(source) { name = source.name, enableInstancing = true };
            AssetDatabase.CreateAsset(copy, path);
            try
            {
                ConvertMaterialToHdrpLit(copy, Shader.Find("HDRP/Lit"));
                AssetDatabase.SaveAssetIfDirty(copy);
                return copy;
            }
            catch
            {
                // Only this newly created copy belongs to this operation; source assets are never edited.
                AssetDatabase.DeleteAsset(path);
                throw;
            }
        }

        private static void ConvertMaterialToHdrpLit(Material mat, Shader hdrpLit)
        {
            Texture baseMap = GetFirstTexture(mat, "_BaseMap", "_BaseColorMap", "_MainTex");
            Vector2 baseScale = GetFirstTextureScale(mat, "_BaseMap", "_BaseColorMap", "_MainTex");
            Vector2 baseOffset = GetFirstTextureOffset(mat, "_BaseMap", "_BaseColorMap", "_MainTex");
            Color baseColor = GetFirstColor(mat, Color.white, "_BaseColor", "_Color");

            Texture bumpMap = GetFirstTexture(mat, "_BumpMap", "_NormalMap");
            float bumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1.0f;

            var maskInputs = HdrpMaterialMaskUtility.Capture(mat);
            string materialGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(mat));
            if (string.IsNullOrEmpty(materialGuid))
                throw new InvalidOperationException("Conversion requires a persistent project-owned material.");
            Texture packedMask = HdrpMaterialMaskUtility.BuildForConversion(maskInputs,
                "Assets/LastSignal/Art/HdrpMaterialRecovery/Masks/" + materialGuid + ".png");

            Texture emissionMap = GetFirstTexture(mat, "_EmissionMap", "_EmissiveColorMap");
            Color emissionColor = GetFirstColor(mat, Color.black, "_EmissionColor", "_EmissiveColor");
            bool emissionEnabled = mat.IsKeywordEnabled("_EMISSION");

            bool isTransparent = (mat.HasProperty("_Surface") && mat.GetFloat("_Surface") > 0.5f)
                                 || mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                                 || string.Equals(mat.GetTag("RenderType", false), "Transparent", StringComparison.OrdinalIgnoreCase);

            bool isAlphaClip = (mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") > 0.5f)
                               || mat.IsKeywordEnabled("_ALPHATEST_ON")
                               || string.Equals(mat.GetTag("RenderType", false), "TransparentCutout", StringComparison.OrdinalIgnoreCase);

            float cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;
            float cull = mat.HasProperty("_Cull") ? mat.GetFloat("_Cull") : 2.0f;
            bool doubleSided = Mathf.RoundToInt(cull) == 0;

            mat.shader = hdrpLit;

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", baseColor);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", baseColor);
            }
            if (mat.HasProperty("_BaseColorMap"))
            {
                mat.SetTexture("_BaseColorMap", baseMap);
                mat.SetTextureScale("_BaseColorMap", baseScale);
                mat.SetTextureOffset("_BaseColorMap", baseOffset);
            }
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", baseMap);
                mat.SetTextureScale("_MainTex", baseScale);
                mat.SetTextureOffset("_MainTex", baseOffset);
            }

            if (mat.HasProperty("_NormalMap"))
            {
                mat.SetTexture("_NormalMap", bumpMap);
                mat.SetTextureScale("_NormalMap", baseScale);
                mat.SetTextureOffset("_NormalMap", baseOffset);
            }
            if (mat.HasProperty("_NormalScale"))
            {
                mat.SetFloat("_NormalScale", bumpScale);
            }

            HdrpMaterialMaskUtility.Apply(mat, packedMask, maskInputs.metallic != null,
                maskInputs.metallic != null || maskInputs.albedoAlpha != null,
                maskInputs.metallicScalar, maskInputs.smoothness, maskInputs.aoStrength,
                maskInputs.occlusion != null, maskInputs.scale, maskInputs.offset);

            if (mat.HasProperty("_EmissiveColorMap"))
            {
                mat.SetTexture("_EmissiveColorMap", emissionMap);
            }
            if (mat.HasProperty("_EmissiveColor"))
            {
                mat.SetColor("_EmissiveColor", emissionEnabled ? emissionColor : Color.black);
            }
            if (mat.HasProperty("_UseEmissiveIntensity"))
            {
                mat.SetFloat("_UseEmissiveIntensity", 0f);
            }

            if (mat.HasProperty("_SurfaceType"))
            {
                mat.SetFloat("_SurfaceType", isTransparent ? 1.0f : 0.0f);
            }
            if (mat.HasProperty("_BlendMode") && isTransparent)
            {
                mat.SetFloat("_BlendMode", 0.0f);
            }
            if (mat.HasProperty("_AlphaCutoffEnable"))
            {
                mat.SetFloat("_AlphaCutoffEnable", isAlphaClip ? 1.0f : 0.0f);
            }
            if (mat.HasProperty("_AlphaCutoff"))
            {
                mat.SetFloat("_AlphaCutoff", cutoff);
            }
            if (mat.HasProperty("_DoubleSidedEnable"))
            {
                mat.SetFloat("_DoubleSidedEnable", doubleSided ? 1.0f : 0.0f);
            }

            HDMaterial.ValidateMaterial(mat);
            EditorUtility.SetDirty(mat);
        }

        private static void ConvertMaterialToHdrpUnlit(Material mat, Shader hdrpUnlit)
        {
            Texture baseMap = GetFirstTexture(mat, "_BaseMap", "_UnlitColorMap", "_MainTex");
            Vector2 baseScale = GetFirstTextureScale(mat, "_BaseMap", "_UnlitColorMap", "_MainTex");
            Vector2 baseOffset = GetFirstTextureOffset(mat, "_BaseMap", "_UnlitColorMap", "_MainTex");
            Color baseColor = GetFirstColor(mat, Color.white, "_BaseColor", "_UnlitColor", "_Color");

            bool isTransparent = (mat.HasProperty("_Surface") && mat.GetFloat("_Surface") > 0.5f)
                                 || mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                                 || string.Equals(mat.GetTag("RenderType", false), "Transparent", StringComparison.OrdinalIgnoreCase);

            bool isAlphaClip = (mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") > 0.5f)
                               || mat.IsKeywordEnabled("_ALPHATEST_ON");
            float cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f;

            mat.shader = hdrpUnlit;

            if (mat.HasProperty("_UnlitColor"))
            {
                mat.SetColor("_UnlitColor", baseColor);
            }
            if (mat.HasProperty("_UnlitColorMap"))
            {
                mat.SetTexture("_UnlitColorMap", baseMap);
                mat.SetTextureScale("_UnlitColorMap", baseScale);
                mat.SetTextureOffset("_UnlitColorMap", baseOffset);
            }
            if (mat.HasProperty("_SurfaceType"))
            {
                mat.SetFloat("_SurfaceType", isTransparent ? 1.0f : 0.0f);
            }
            if (mat.HasProperty("_AlphaCutoffEnable"))
            {
                mat.SetFloat("_AlphaCutoffEnable", isAlphaClip ? 1.0f : 0.0f);
            }
            if (mat.HasProperty("_AlphaCutoff"))
            {
                mat.SetFloat("_AlphaCutoff", cutoff);
            }

            HDMaterial.ValidateMaterial(mat);
            EditorUtility.SetDirty(mat);
        }

        private static void ConvertMaterialToDualParticle(Material mat, Shader dualParticleShader)
        {
            Texture baseMap = GetFirstTexture(mat, "_BaseMap", "_MainTex");
            Vector2 baseScale = GetFirstTextureScale(mat, "_BaseMap", "_MainTex");
            Vector2 baseOffset = GetFirstTextureOffset(mat, "_BaseMap", "_MainTex");
            Color baseColor = GetFirstColor(mat, Color.white, "_BaseColor", "_Color");
            int queue = mat.renderQueue;

            mat.shader = dualParticleShader;
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", baseMap);
                mat.SetTextureScale("_BaseMap", baseScale);
                mat.SetTextureOffset("_BaseMap", baseOffset);
            }
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", baseColor);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", baseColor);
            }

            mat.renderQueue = queue;
            EditorUtility.SetDirty(mat);
        }

        private static MaterialRollbackEntry CaptureRollbackEntry(
            Material mat,
            string path,
            string guid,
            string shaderName)
        {
            Texture baseMap = GetFirstTexture(mat, "_BaseMap", "_MainTex");
            Texture bumpMap = GetFirstTexture(mat, "_BumpMap");
            Texture metallicMap = GetFirstTexture(mat, "_MetallicGlossMap");
            Texture occlusionMap = GetFirstTexture(mat, "_OcclusionMap");
            Texture emissionMap = GetFirstTexture(mat, "_EmissionMap");

            return new MaterialRollbackEntry
            {
                assetPath = path,
                materialGuid = guid,
                originalShaderName = shaderName,
                originalKeywords = mat.shaderKeywords,
                smoothnessTextureChannel = mat.HasProperty("_SmoothnessTextureChannel") ? mat.GetFloat("_SmoothnessTextureChannel") : 0f,
                glossMapScale = mat.HasProperty("_GlossMapScale") ? mat.GetFloat("_GlossMapScale") : 1f,
                workflowMode = mat.HasProperty("_WorkflowMode") ? mat.GetFloat("_WorkflowMode") : 1f,
                baseColor = GetFirstColor(mat, Color.white, "_BaseColor", "_Color"),
                baseMapGuid = GetAssetGuid(baseMap),
                baseMapScale = GetFirstTextureScale(mat, "_BaseMap", "_MainTex"),
                baseMapOffset = GetFirstTextureOffset(mat, "_BaseMap", "_MainTex"),
                bumpMapGuid = GetAssetGuid(bumpMap),
                bumpScale = mat.HasProperty("_BumpScale") ? mat.GetFloat("_BumpScale") : 1.0f,
                metallicMapGuid = GetAssetGuid(metallicMap),
                metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0.0f,
                smoothness = mat.HasProperty("_Smoothness")
                    ? mat.GetFloat("_Smoothness")
                    : (mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f),
                occlusionMapGuid = GetAssetGuid(occlusionMap),
                occlusionStrength = mat.HasProperty("_OcclusionStrength") ? mat.GetFloat("_OcclusionStrength") : 1.0f,
                emissionMapGuid = GetAssetGuid(emissionMap),
                emissionColor = GetFirstColor(mat, Color.black, "_EmissionColor"),
                emissionEnabled = mat.IsKeywordEnabled("_EMISSION"),
                surfaceType = mat.HasProperty("_Surface") ? mat.GetFloat("_Surface") : 0.0f,
                alphaClip = (mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") > 0.5f) || mat.IsKeywordEnabled("_ALPHATEST_ON"),
                cutoff = mat.HasProperty("_Cutoff") ? mat.GetFloat("_Cutoff") : 0.5f,
                cull = mat.HasProperty("_Cull") ? mat.GetFloat("_Cull") : 2.0f,
                customRenderQueue = mat.renderQueue
            };
        }

        private static void ClassifyMaterial(
            Material mat,
            string path,
            string guid,
            string shaderName,
            MaterialAuditReport report)
        {
            var record = new MaterialAuditRecord
            {
                assetPath = path,
                guid = guid,
                materialName = mat.name,
                currentShaderName = shaderName,
                isOpaque = !(mat.HasProperty("_Surface") && mat.GetFloat("_Surface") > 0.5f),
                isAlphaClipped = (mat.HasProperty("_AlphaClip") && mat.GetFloat("_AlphaClip") > 0.5f) || mat.IsKeywordEnabled("_ALPHATEST_ON")
            };

            if (IsCustomDualPipelineShader(shaderName))
            {
                record.targetHdrpShaderName = shaderName;
                record.migrationAction = "RetainDualSubShader";
                report.customDualPipelineCount++;
            }
            else if (shaderName.StartsWith("HDRP/", StringComparison.Ordinal))
            {
                record.targetHdrpShaderName = shaderName;
                record.migrationAction = "ValidateExistingHdrp";
                report.alreadyHdrpCount++;
            }
            else if (IsUrpOrStandardLitShader(shaderName))
            {
                record.targetHdrpShaderName = "HDRP/Lit";
                record.migrationAction = "ConvertToHdrpLit";
                report.urpLitCount++;
            }
            else if (IsUrpOrLegacyUnlitShader(shaderName))
            {
                record.targetHdrpShaderName = "HDRP/Unlit";
                record.migrationAction = "ConvertToHdrpUnlit";
                report.urpUnlitCount++;
            }
            else if (IsParticleShader(shaderName))
            {
                record.targetHdrpShaderName = "LastSignal/VFX/Particles/Unlit";
                record.migrationAction = "ConvertToDualParticleUnlit";
                report.urpParticleCount++;
            }
            else
            {
                record.targetHdrpShaderName = shaderName;
                record.migrationAction = "InspectOrSkip";
                report.otherShaderCount++;
            }

            report.records.Add(record);
        }

        private static bool IsCustomDualPipelineShader(string shaderName)
        {
            return shaderName.StartsWith("LastSignal/", StringComparison.Ordinal)
                   || shaderName.StartsWith("Hidden/LastSignal/", StringComparison.Ordinal);
        }

        private static bool IsUrpOrStandardLitShader(string shaderName)
        {
            return shaderName == "Universal Render Pipeline/Lit"
                   || shaderName == "Universal Render Pipeline/Simple Lit"
                   || shaderName == "Universal Render Pipeline/Baked Lit"
                   || shaderName == "Universal Render Pipeline/Complex Lit"
                   || shaderName == "Standard"
                   || shaderName == "Standard (Specular setup)";
        }

        private static bool IsUrpOrLegacyUnlitShader(string shaderName)
        {
            return shaderName == "Universal Render Pipeline/Unlit"
                   || shaderName == "Unlit/Color"
                   || shaderName == "Unlit/Texture"
                   || shaderName == "Unlit/Transparent"
                   || shaderName == "Unlit/Transparent Cutout";
        }

        private static bool IsParticleShader(string shaderName)
        {
            return shaderName == "Universal Render Pipeline/Particles/Unlit"
                   || shaderName == "Universal Render Pipeline/Particles/Simple Lit"
                   || shaderName == "Universal Render Pipeline/Particles/Lit"
                   || shaderName == "Particles/Standard Unlit"
                   || shaderName == "Particles/Standard Surface";
        }

        private static bool IsUrpTerrainShader(string shaderName)
        {
            return shaderName == "Universal Render Pipeline/Terrain/Lit"
                   || shaderName == "Nature/Terrain/Standard";
        }

        private static string[] FindAllMaterialGuids()
        {
            var validFolders = new List<string>();
            foreach (string folder in SearchFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    validFolders.Add(folder);
                }
            }

            return AssetDatabase.FindAssets("t:Material", validFolders.ToArray());
        }

        private static Texture GetFirstTexture(Material mat, params string[] propNames)
        {
            foreach (string prop in propNames)
            {
                if (mat.HasProperty(prop))
                {
                    var tex = mat.GetTexture(prop);
                    if (tex != null)
                    {
                        return tex;
                    }
                }
            }

            return null;
        }

        private static Vector2 GetFirstTextureScale(Material mat, params string[] propNames)
        {
            foreach (string prop in propNames)
            {
                if (mat.HasProperty(prop))
                {
                    return mat.GetTextureScale(prop);
                }
            }

            return Vector2.one;
        }

        private static Vector2 GetFirstTextureOffset(Material mat, params string[] propNames)
        {
            foreach (string prop in propNames)
            {
                if (mat.HasProperty(prop))
                {
                    return mat.GetTextureOffset(prop);
                }
            }

            return Vector2.zero;
        }

        private static Color GetFirstColor(Material mat, Color fallback, params string[] propNames)
        {
            foreach (string prop in propNames)
            {
                if (mat.HasProperty(prop))
                {
                    return mat.GetColor(prop);
                }
            }

            return fallback;
        }

        private static string GetAssetGuid(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            string path = AssetDatabase.GetAssetPath(obj);
            return !string.IsNullOrEmpty(path) ? AssetDatabase.AssetPathToGUID(path) : string.Empty;
        }

        private static Texture LoadTextureByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            return !string.IsNullOrEmpty(path) ? AssetDatabase.LoadAssetAtPath<Texture>(path) : null;
        }

        private static MaterialRollbackManifest LoadRollbackManifest()
        {
            if (!File.Exists(RollbackManifestPath))
            {
                return null;
            }

            return JsonUtility.FromJson<MaterialRollbackManifest>(File.ReadAllText(RollbackManifestPath));
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
