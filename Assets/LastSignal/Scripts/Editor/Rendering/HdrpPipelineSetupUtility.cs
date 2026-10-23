using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LastSignal.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.Rendering
{
    /// <summary>
    /// Controlled HDRP 17.5.0 pipeline asset creation, shader validation, isolated test scene construction,
    /// and reversible GraphicsSettings / QualitySettings switching.
    /// </summary>
    public static class HdrpPipelineSetupUtility
    {
        public const string HdrpSettingsFolder = "Assets/LastSignal/Settings/Rendering/HDRP";
        public const string GlobalSettingsPath = HdrpSettingsFolder + "/LastSignal_HDRPGlobalSettings.asset";
        public const string DefaultVolumeProfilePath = HdrpSettingsFolder + "/LastSignal_HDRP_DefaultVolumeProfile.asset";
        public const string MediumAssetPath = HdrpSettingsFolder + "/LastSignal_HDRP_Medium.asset";
        public const string HighAssetPath = HdrpSettingsFolder + "/LastSignal_HDRP_High.asset";
        public const string BenchmarkAssetPath = HdrpSettingsFolder + "/LastSignal_HDRP_Benchmark.asset";
        public const string RollbackStatePath = HdrpSettingsFolder + "/UrpPipelineRollbackState.json";
        public const string IsolatedSceneFolder = "Assets/LastSignal/Scenes/Validation";
        public const string IsolatedScenePath = IsolatedSceneFolder + "/HDRP_MigrationTest.unity";
        public const string EvidenceFolder = "Docs/Implementation/VisualRealismMigration/Evidence";

        private static readonly string[] PersistentVolumeProfilePaths =
        {
            "Assets/LastSignal/Art/FirstTownAtmosphere/TownVolume.asset",
            "Assets/LastSignal/Art/FirstTownAtmosphere/Generated/Volume.asset"
        };

        private static readonly string[] RequiredCustomShaders =
        {
            "LastSignal/Optics/ScopeLens",
            "LastSignal/Ballistic",
            "LastSignal/BloodSurface",
            "LastSignal/LocalizedWound",
            "LastSignal/FirstTown/AtmosphereSky",
            "LastSignal/Forest/Lit Wind",
            "LastSignal/Forest/Terrain Lit",
            "Hidden/LastSignal/Forest/Terrain Base",
            "Hidden/LastSignal/Forest/Terrain Basemap Gen",
            "Hidden/LastSignal/Forest/Terrain Add Pass",
            "LastSignal/HDRP/ScopeLens",
            "LastSignal/HDRP/Ballistic",
            "LastSignal/HDRP/BloodSurface",
            "LastSignal/HDRP/LocalizedWound",
            "LastSignal/HDRP/Forest/Lit Wind",
            "LastSignal/VFX/Particles/Unlit",
            "LastSignal/VFX/Blood/ParticleSplat",
            // Preserved vendor GUID now delegates to the native HDRP blood pass.
            "Vefects/SH_Vefects_URP_Free_Blood_01_Lit",
            "HDRP/Lit",
            "HDRP/Unlit",
            "HDRP/TerrainLit"
        };

        [Serializable]
        private sealed class HdrpSetupReport
        {
            public string status;
            public string timestampUtc;
            public string globalSettingsAssetPath;
            public string defaultVolumeProfilePath;
            public string mediumAssetPath;
            public string highAssetPath;
            public string benchmarkAssetPath;
            public List<string> updatedAtmosphereProfiles = new List<string>();
            public string activePipelineBeforeSwitch;
        }

        [Serializable]
        private sealed class ShaderCheckEntry
        {
            public string shaderName;
            public string assetPath;
            public bool found;
            public bool hasError;
            public int passCount;
            public int subshaderCount;
            public List<string> messages = new List<string>();
        }

        [Serializable]
        private sealed class ShaderValidationReport
        {
            public string status;
            public string timestampUtc;
            public int totalChecked;
            public int failedCount;
            public List<ShaderCheckEntry> shaders = new List<ShaderCheckEntry>();
        }

        [Serializable]
        private sealed class PipelineRollbackState
        {
            public string recordedUtc;
            public string defaultRenderPipelineGuid;
            public string defaultRenderPipelinePath;
            public List<string> qualityLevelNames = new List<string>();
            public List<string> qualityLevelPipelineGuids = new List<string>();
            public List<string> qualityLevelPipelinePaths = new List<string>();
        }

        [Serializable]
        private sealed class PipelineSwitchReport
        {
            public string status;
            public string timestampUtc;
            public string activePipelineKind;
            public string defaultRenderPipelinePath;
            public List<string> qualityLevelPipelines = new List<string>();
        }

        [Serializable]
        private sealed class IsolatedSceneReport
        {
            public string status;
            public string timestampUtc;
            public string scenePath;
            public List<string> specimenNames = new List<string>();
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/1. Setup HDRP Assets (No Pipeline Switch)")]
        public static void SetupHdrpAssets()
        {
            EnsureDirectoryExists(HdrpSettingsFolder);
            EnsureDirectoryExists(EvidenceFolder);

            var globalSettings = EnsureHdrpGlobalSettings();
            var defaultProfile = CreateOrUpdateVolumeProfile(DefaultVolumeProfilePath);

            var updatedProfiles = new List<string> { DefaultVolumeProfilePath };
            foreach (string profilePath in PersistentVolumeProfilePaths)
            {
                var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
                if (existing != null)
                {
                    PopulateAndPersistVolumeProfile(existing);
                    updatedProfiles.Add(profilePath);
                }
            }

            if (globalSettings != null)
            {
                AssignDefaultVolumeProfileOnGlobalSettings(globalSettings, defaultProfile);
            }

            var mediumAsset = CreateOrUpdateHdrpAsset(MediumAssetPath, "LastSignal_HDRP_Medium", 2048, false);
            var highAsset = CreateOrUpdateHdrpAsset(HighAssetPath, "LastSignal_HDRP_High", 4096, true);
            var benchmarkAsset = CreateOrUpdateHdrpAsset(BenchmarkAssetPath, "LastSignal_HDRP_Benchmark", 4096, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var report = new HdrpSetupReport
            {
                status = (mediumAsset != null && highAsset != null && benchmarkAsset != null) ? "PASS" : "FAIL",
                timestampUtc = DateTime.UtcNow.ToString("O"),
                globalSettingsAssetPath = globalSettings != null ? AssetDatabase.GetAssetPath(globalSettings) : string.Empty,
                defaultVolumeProfilePath = DefaultVolumeProfilePath,
                mediumAssetPath = MediumAssetPath,
                highAssetPath = HighAssetPath,
                benchmarkAssetPath = BenchmarkAssetPath,
                updatedAtmosphereProfiles = updatedProfiles,
                activePipelineBeforeSwitch = RenderPipelineAdapter.ActivePipeline.ToString()
            };

            File.WriteAllText(
                Path.Combine(EvidenceFolder, "hdrp_assets_setup.json"),
                JsonUtility.ToJson(report, true));

            if (report.status != "PASS")
            {
                throw new InvalidOperationException("Failed to create one or more HDRP pipeline configuration assets.");
            }

            Debug.Log($"[HdrpPipelineSetupUtility] HDRP assets created and configured. Active pipeline remains: {report.activePipelineBeforeSwitch}");
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/2. Validate Custom Shaders")]
        public static void ValidateCustomShaders()
        {
            EnsureDirectoryExists(EvidenceFolder);

            var report = new ShaderValidationReport
            {
                timestampUtc = DateTime.UtcNow.ToString("O"),
                totalChecked = RequiredCustomShaders.Length
            };

            foreach (string shaderName in RequiredCustomShaders)
            {
                var shader = Shader.Find(shaderName);
                var entry = new ShaderCheckEntry
                {
                    shaderName = shaderName,
                    found = shader != null
                };

                if (shader != null)
                {
                    entry.assetPath = AssetDatabase.GetAssetPath(shader);
                    if (!string.IsNullOrEmpty(entry.assetPath) && entry.assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                    {
                        AssetDatabase.ImportAsset(entry.assetPath, ImportAssetOptions.ForceUpdate);
                        shader = AssetDatabase.LoadAssetAtPath<Shader>(entry.assetPath) ?? Shader.Find(shaderName);
                    }
                    entry.hasError = ShaderUtil.ShaderHasError(shader);
                    entry.passCount = shader.passCount;
                    entry.subshaderCount = shader.subshaderCount;
                    if (entry.passCount == 0 || entry.subshaderCount == 0)
                    {
                        entry.hasError = true;
                        entry.messages.Add("Shader has no usable passes/subshaders.");
                    }

                    var messages = ShaderUtil.GetShaderMessages(shader);
                    if (messages != null)
                    {
                        foreach (var msg in messages)
                        {
                            if (msg.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                            {
                                entry.hasError = true;
                            }

                            entry.messages.Add($"{msg.severity}: {msg.message} ({msg.file}:{msg.line})");
                        }
                    }
                }
                else
                {
                    entry.hasError = true;
                    entry.messages.Add("Shader.Find returned null.");
                }

                if (!entry.found || entry.hasError)
                {
                    report.failedCount++;
                }

                report.shaders.Add(entry);
            }

            report.status = report.failedCount == 0 ? "PASS" : "FAIL";
            string reportPath = Path.Combine(EvidenceFolder, "hdrp_shader_validation.json");
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));

            if (report.failedCount > 0)
            {
                throw new InvalidOperationException(
                    $"HDRP custom shader validation failed ({report.failedCount}/{report.totalChecked}). Inspect {reportPath}.");
            }

            Debug.Log($"[HdrpPipelineSetupUtility] All {report.totalChecked} custom and HDRP shaders validated with 0 errors.");
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/3. Create Isolated Validation Scene")]
        public static void CreateIsolatedTestScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit PlayMode before creating the isolated HDRP validation scene.");
            }

            EnsureDirectoryExists(IsolatedSceneFolder);
            EnsureDirectoryExists(EvidenceFolder);

            var defaultProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DefaultVolumeProfilePath);
            if (defaultProfile == null)
            {
                SetupHdrpAssets();
                defaultProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DefaultVolumeProfilePath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var specimens = new List<string>();

            // 1. Main Camera + HDAdditionalCameraData
            var camGo = new GameObject("HDRP_MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 2.2f, -6.5f);
            camGo.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            camGo.AddComponent<AudioListener>();
            if (camGo.GetComponent<HDAdditionalCameraData>() == null)
            {
                camGo.AddComponent<HDAdditionalCameraData>();
            }
            specimens.Add(camGo.name);

            // 2. Directional Sun Light + HDAdditionalLightData
            var sunGo = new GameObject("HDRP_DirectionalSun");
            sunGo.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.95f, 0.86f);
            sun.intensity = 1.25f * Mathf.PI;
            sun.shadows = LightShadows.Soft;
            if (sunGo.GetComponent<HDAdditionalLightData>() == null)
            {
                sunGo.AddComponent<HDAdditionalLightData>();
            }
            specimens.Add(sunGo.name);

            // 3. Global Volume
            var volumeGo = new GameObject("HDRP_GlobalVolume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = defaultProfile;
            specimens.Add(volumeGo.name);

            // 4. Forest Terrain / Ground Plane Specimen
            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Specimen_Ground_ForestLit";
            groundGo.transform.position = Vector3.zero;
            groundGo.transform.localScale = new Vector3(4f, 1f, 4f);
            var forestLitShader = Shader.Find("LastSignal/Forest/Lit Wind") ?? RenderPipelineAdapter.FindStandardLitShader();
            var groundMat = CreateOrUpdateTestMaterial(
                HdrpSettingsFolder + "/Test_ForestGround.mat",
                forestLitShader,
                new Color(0.24f, 0.29f, 0.20f, 1f));
            groundGo.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            specimens.Add(groundGo.name);

            // 5. Custom Shader Specimens (Blood, Wound, Ballistic, ScopeLens)
            CreateShaderSpecimenSphere("Specimen_BloodSurface", new Vector3(-2.4f, 1.0f, 0f), "LastSignal/BloodSurface", specimens);
            CreateShaderSpecimenSphere("Specimen_LocalizedWound", new Vector3(-0.8f, 1.0f, 0f), "LastSignal/LocalizedWound", specimens);
            CreateShaderSpecimenSphere("Specimen_Ballistic", new Vector3(0.8f, 1.0f, 0f), "LastSignal/Ballistic", specimens);
            CreateShaderSpecimenSphere("Specimen_ScopeLens", new Vector3(2.4f, 1.0f, 0f), "LastSignal/Optics/ScopeLens", specimens);

            // 6. Scope secondary camera specimen configured via RenderPipelineAdapter
            var scopeCamGo = new GameObject("Specimen_ScopeCamera");
            scopeCamGo.transform.position = new Vector3(2.4f, 1.0f, -2.0f);
            var scopeCam = scopeCamGo.AddComponent<Camera>();
            scopeCam.enabled = false;
            RenderPipelineAdapter.ConfigureScopeCamera(scopeCamGo, 0);
            specimens.Add(scopeCamGo.name);

            EditorSceneManager.SaveScene(scene, IsolatedScenePath);
            AssetDatabase.SaveAssets();

            var report = new IsolatedSceneReport
            {
                status = "PASS",
                timestampUtc = DateTime.UtcNow.ToString("O"),
                scenePath = IsolatedScenePath,
                specimenNames = specimens
            };

            File.WriteAllText(
                Path.Combine(EvidenceFolder, "hdrp_isolated_scene_validation.json"),
                JsonUtility.ToJson(report, true));

            Debug.Log($"[HdrpPipelineSetupUtility] Created isolated HDRP test scene at {IsolatedScenePath} with {specimens.Count} specimens.");
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/Run Pre-Switch Preparation (M3+M4+M5+M7)")]
        public static void PrepareAllPreSwitch()
        {
            SetupHdrpAssets();
            ValidateCustomShaders();
            HdrpMaterialMigrationUtility.AuditMaterials();
            HdrpMaterialMigrationUtility.MigrateAllMaterialsToHdrp();
            CreateIsolatedTestScene();
            Debug.Log("[HdrpPipelineSetupUtility] Completed all pre-switch HDRP preparation steps (M3, M4, M5, M7).");
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/Run Full Migration & Activate HDRP (M3-M8)")]
        public static void MigrateAndActivateHdrp()
        {
            PrepareAllPreSwitch();
            ActivateHdrpPipeline();
            Debug.Log("[HdrpPipelineSetupUtility] Completed full HDRP migration and activated HDRP globally (M3-M8).");
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/5. Activate HDRP Pipeline Globally")]
        public static void ActivateHdrpPipeline()
        {
            EnsureDirectoryExists(HdrpSettingsFolder);
            EnsureDirectoryExists(EvidenceFolder);

            var mediumAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(MediumAssetPath);
            var highAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(HighAssetPath);
            var benchmarkAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(BenchmarkAssetPath);

            if (mediumAsset == null || highAsset == null || benchmarkAsset == null)
            {
                SetupHdrpAssets();
                mediumAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(MediumAssetPath);
                highAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(HighAssetPath);
                benchmarkAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(BenchmarkAssetPath);
            }

            if (mediumAsset == null || highAsset == null || benchmarkAsset == null)
            {
                throw new InvalidOperationException("HDRP pipeline assets are missing; cannot activate HDRP.");
            }

            SaveRollbackStateIfNeeded();

            GraphicsSettings.defaultRenderPipeline = highAsset;

            string[] qualityNames = QualitySettings.names;
            var assignedPaths = new List<string>();
            for (int i = 0; i < qualityNames.Length; i++)
            {
                string qName = qualityNames[i].ToLowerInvariant();
                RenderPipelineAsset targetAsset = highAsset;
                if (qName.Contains("low") || qName.Contains("medium") || qName.Contains("mobile") || qName.Contains("performant"))
                {
                    targetAsset = mediumAsset;
                }
                else if (qName.Contains("ultra") || qName.Contains("benchmark"))
                {
                    targetAsset = benchmarkAsset;
                }

                QualitySettings.renderPipeline = targetAsset;
                SetQualityLevelRenderPipelineAsset(i, targetAsset);
                assignedPaths.Add($"{qualityNames[i]} -> {AssetDatabase.GetAssetPath(targetAsset)}");
            }

            QualitySettings.renderPipeline = highAsset;
            EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var report = new PipelineSwitchReport
            {
                status = RenderPipelineAdapter.IsHDRP ? "PASS" : "FAIL",
                timestampUtc = DateTime.UtcNow.ToString("O"),
                activePipelineKind = RenderPipelineAdapter.ActivePipeline.ToString(),
                defaultRenderPipelinePath = AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline),
                qualityLevelPipelines = assignedPaths
            };

            File.WriteAllText(
                Path.Combine(EvidenceFolder, "hdrp_pipeline_activation.json"),
                JsonUtility.ToJson(report, true));

            if (!RenderPipelineAdapter.IsHDRP)
            {
                throw new InvalidOperationException("Failed to activate HDRP as active RenderPipelineAsset.");
            }

            Debug.Log($"[HdrpPipelineSetupUtility] Activated HDRP globally. Default asset: {report.defaultRenderPipelinePath}");
        }

        [MenuItem("Last Signal/Rendering/HDRP Migration/Rollback: Restore URP Pipeline Globally")]
        public static void RestoreUrpPipeline()
        {
            EnsureDirectoryExists(EvidenceFolder);

            RenderPipelineAsset fallbackUrp =
                AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");

            if (File.Exists(RollbackStatePath))
            {
                var state = JsonUtility.FromJson<PipelineRollbackState>(File.ReadAllText(RollbackStatePath));
                if (state != null)
                {
                    var defaultAsset = LoadAssetByGuidOrPath<RenderPipelineAsset>(
                        state.defaultRenderPipelineGuid,
                        state.defaultRenderPipelinePath) ?? fallbackUrp;

                    if (!defaultAsset || defaultAsset.GetType().FullName != "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset")
                        throw new InvalidOperationException("URP rollback assets are unavailable or retired. Restore the reviewed settings archive and package before rollback; HDRP settings were not changed.");
                    if (state.qualityLevelPipelinePaths == null || state.qualityLevelPipelineGuids == null ||
                        state.qualityLevelPipelinePaths.Count != QualitySettings.names.Length ||
                        state.qualityLevelPipelineGuids.Count != state.qualityLevelPipelinePaths.Count)
                        throw new InvalidOperationException("URP rollback quality-level inventory changed; HDRP settings were not changed.");
                    int count = Mathf.Min(QualitySettings.names.Length, state.qualityLevelPipelinePaths.Count);
                    var restoredQuality = new RenderPipelineAsset[count];
                    for (int i = 0; i < count; i++)
                    {
                        restoredQuality[i] = LoadAssetByGuidOrPath<RenderPipelineAsset>(
                            state.qualityLevelPipelineGuids[i],
                            state.qualityLevelPipelinePaths[i]);
                        if (!restoredQuality[i] || restoredQuality[i].GetType().FullName != "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset")
                            throw new InvalidOperationException("Missing URP rollback quality asset; HDRP settings were not changed.");
                    }
                    GraphicsSettings.defaultRenderPipeline = defaultAsset;
                    for (int i = 0; i < count; i++) SetQualityLevelRenderPipelineAsset(i, restoredQuality[i]);
                    QualitySettings.renderPipeline = defaultAsset;
                }
                else throw new InvalidOperationException("Invalid URP rollback state; HDRP settings were not changed.");
            }
            else if (fallbackUrp != null)
            {
                GraphicsSettings.defaultRenderPipeline = fallbackUrp;
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    SetQualityLevelRenderPipelineAsset(i, fallbackUrp);
                }
                QualitySettings.renderPipeline = fallbackUrp;
            }
            else throw new InvalidOperationException("No URP rollback assets; HDRP settings were not changed.");

            EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var assignedPaths = new List<string>();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                var qAsset = QualitySettings.GetRenderPipelineAssetAt(i);
                assignedPaths.Add($"{QualitySettings.names[i]} -> {(qAsset != null ? AssetDatabase.GetAssetPath(qAsset) : "null")}");
            }

            var report = new PipelineSwitchReport
            {
                status = RenderPipelineAdapter.IsURP ? "PASS" : "FAIL",
                timestampUtc = DateTime.UtcNow.ToString("O"),
                activePipelineKind = RenderPipelineAdapter.ActivePipeline.ToString(),
                defaultRenderPipelinePath = GraphicsSettings.defaultRenderPipeline != null
                    ? AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline)
                    : "null",
                qualityLevelPipelines = assignedPaths
            };

            File.WriteAllText(
                Path.Combine(EvidenceFolder, "urp_pipeline_restore.json"),
                JsonUtility.ToJson(report, true));

            Debug.Log($"[HdrpPipelineSetupUtility] Restored URP pipeline globally. Active pipeline: {report.activePipelineKind}");
        }

        private static void SaveRollbackStateIfNeeded()
        {
            var currentDefault = GraphicsSettings.defaultRenderPipeline;
            if (currentDefault is HDRenderPipelineAsset && File.Exists(RollbackStatePath))
            {
                return;
            }

            var state = new PipelineRollbackState
            {
                recordedUtc = DateTime.UtcNow.ToString("O"),
                defaultRenderPipelinePath = currentDefault != null ? AssetDatabase.GetAssetPath(currentDefault) : string.Empty,
                defaultRenderPipelineGuid = currentDefault != null
                    ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(currentDefault))
                    : string.Empty
            };

            string[] qualityNames = QualitySettings.names;
            for (int i = 0; i < qualityNames.Length; i++)
            {
                var qAsset = QualitySettings.GetRenderPipelineAssetAt(i);
                string path = qAsset != null ? AssetDatabase.GetAssetPath(qAsset) : string.Empty;
                string guid = !string.IsNullOrEmpty(path) ? AssetDatabase.AssetPathToGUID(path) : string.Empty;
                state.qualityLevelNames.Add(qualityNames[i]);
                state.qualityLevelPipelinePaths.Add(path);
                state.qualityLevelPipelineGuids.Add(guid);
            }

            File.WriteAllText(RollbackStatePath, JsonUtility.ToJson(state, true));
        }

        private static void SetQualityLevelRenderPipelineAsset(int index, RenderPipelineAsset asset)
        {
            var objs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (objs == null || objs.Length == 0 || objs[0] == null)
            {
                return;
            }

            using (var so = new SerializedObject(objs[0]))
            {
                var arrayProp = so.FindProperty("m_QualitySettings");
                if (arrayProp != null && arrayProp.isArray && index >= 0 && index < arrayProp.arraySize)
                {
                    var elem = arrayProp.GetArrayElementAtIndex(index);
                    var rpProp = elem != null ? elem.FindPropertyRelative("customRenderPipeline") : null;
                    if (rpProp != null)
                    {
                        rpProp.objectReferenceValue = asset;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            EditorUtility.SetDirty(objs[0]);
        }

        private static T LoadAssetByGuidOrPath<T>(string guid, string path) where T : UnityEngine.Object
        {
            if (!string.IsNullOrEmpty(guid))
            {
                string resolvedPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    var byGuid = AssetDatabase.LoadAssetAtPath<T>(resolvedPath);
                    if (byGuid != null)
                    {
                        return byGuid;
                    }
                }
            }

            if (!string.IsNullOrEmpty(path))
            {
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return null;
        }

        private static RenderPipelineGlobalSettings EnsureHdrpGlobalSettings()
        {
            var existing = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
            if (existing != null)
            {
                return existing;
            }

            var globalSettingsType = typeof(HDRenderPipeline).Assembly.GetType(
                "UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings");

            if (globalSettingsType != null)
            {
                var ensureMethod = globalSettingsType.GetMethod(
                    "Ensure",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(bool) },
                    null);

                if (ensureMethod != null)
                {
                    var ensured = ensureMethod.Invoke(null, new object[] { true }) as RenderPipelineGlobalSettings;
                    if (ensured != null)
                    {
                        return ensured;
                    }
                }

                var asset = AssetDatabase.LoadAssetAtPath(GlobalSettingsPath, globalSettingsType) as RenderPipelineGlobalSettings;
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance(globalSettingsType) as RenderPipelineGlobalSettings;
                    if (asset != null)
                    {
                        asset.name = "LastSignal_HDRPGlobalSettings";
                        AssetDatabase.CreateAsset(asset, GlobalSettingsPath);
                    }
                }

                if (asset != null)
                {
                    UnityEditor.Rendering.EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<HDRenderPipeline>(asset);
                    EditorUtility.SetDirty(asset);
                    return asset;
                }
            }

            return null;
        }

        private static void AssignDefaultVolumeProfileOnGlobalSettings(
            RenderPipelineGlobalSettings globalSettings,
            VolumeProfile defaultProfile)
        {
            if (globalSettings == null || defaultProfile == null)
            {
                return;
            }

            using (var so = new SerializedObject(globalSettings))
            {
                var prop = so.FindProperty("m_DefaultVolumeProfile");
                if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    prop.objectReferenceValue = defaultProfile;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(globalSettings);
        }

        private static VolumeProfile CreateOrUpdateVolumeProfile(string path)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(profile, path);
            }

            PopulateAndPersistVolumeProfile(profile);
            return profile;
        }

        private static void PopulateAndPersistVolumeProfile(VolumeProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            RenderPipelineAdapter.EnsureAtmosphereVolumeComponents(profile);
            foreach (var comp in profile.components)
            {
                if (comp == null)
                {
                    continue;
                }

                comp.name = comp.GetType().Name;
                if (!AssetDatabase.Contains(comp))
                {
                    AssetDatabase.AddObjectToAsset(comp, profile);
                }

                EditorUtility.SetDirty(comp);
            }

            EditorUtility.SetDirty(profile);
        }

        private static HDRenderPipelineAsset CreateOrUpdateHdrpAsset(
            string path,
            string assetName,
            int directionalShadowResolution,
            bool highQualityEffects)
        {
            var asset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                asset.name = assetName;
                AssetDatabase.CreateAsset(asset, path);
            }

            RenderPipelineSettings settings = asset.currentPlatformRenderPipelineSettings;
            settings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
            settings.supportedLitShaderMode = RenderPipelineSettings.SupportedLitShaderMode.Both;
            settings.supportShadowMask = true;
            settings.supportSSR = true;
            settings.supportSSRTransparent = highQualityEffects;
            settings.supportSSAO = true;
            settings.supportSubsurfaceScattering = true;
            settings.supportVolumetrics = true;
            settings.supportVolumetricClouds = true;
            settings.supportLightLayers = true;
            settings.supportDecals = true;
            settings.supportDecalLayers = true;
            settings.supportSurfaceGradient = true;
            settings.supportCustomPass = true;
            settings.supportMotionVectors = true;
            settings.lightProbeSystem = RenderPipelineSettings.LightProbeSystem.LegacyLightProbes;
            settings.lightLoopSettings.maxDirectionalLightsOnScreen = 4;
            settings.lightLoopSettings.maxPunctualLightsOnScreen = highQualityEffects ? 256 : 128;
            settings.lightLoopSettings.maxAreaLightsOnScreen = 32;
            settings.lightLoopSettings.maxCubeReflectionOnScreen = 32;
            settings.lightLoopSettings.maxDecalsOnScreen = 256;
            settings.hdShadowInitParams.maxShadowRequests = highQualityEffects ? 64 : 32;
            settings.hdShadowInitParams.directionalShadowsDepthBits = DepthBits.Depth32;
            settings.hdShadowInitParams.maxDirectionalShadowMapResolution = directionalShadowResolution;

            asset.currentPlatformRenderPipelineSettings = settings;

            using (var so = new SerializedObject(asset))
            {
                var srpBatcherProp = so.FindProperty("m_UseSRPBatcher");
                if (srpBatcherProp != null)
                {
                    srpBatcherProp.boolValue = true;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Material CreateOrUpdateTestMaterial(string path, Shader shader, Color baseColor)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", baseColor);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreateShaderSpecimenSphere(
            string name,
            Vector3 position,
            string shaderName,
            List<string> specimens)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.position = position;
            var shader = Shader.Find(shaderName) ?? RenderPipelineAdapter.FindStandardLitShader();
            var mat = CreateOrUpdateTestMaterial(
                $"{HdrpSettingsFolder}/Test_{name}.mat",
                shader,
                new Color(0.75f, 0.2f, 0.2f, 1f));
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            specimens.Add(go.name);
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
