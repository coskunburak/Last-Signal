using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LastSignal.Editor.Rendering
{
    /// <summary>
    /// Owner visual review window for P0 HDRP material inspection.
    /// Opens existing repaired prefabs in isolation without scene mutation or asset rewriting.
    /// Provides a controlled, reversible 3-point studio lighting & HDRP exposure rig to
    /// unambiguously separate material PBR properties from Scene View lighting/exposure artifacts.
    /// </summary>
    public sealed class HdrpMaterialReviewWindow : EditorWindow
    {
        private const string RigGameObjectName = "__HDRP_ReviewStudioRig";

        // Active production review subjects (Space Crew excluded -> moved to deferred)
        private static readonly string[] ActiveLabels =
        {
            "Medicine — Complete Pack",
            "Medicine — PharmaSyringe2",
            "Custom9mm — Pistol & Magazine",
            "DXT — Pistol",
            "Shirtless Zombie — Visual",
            "Props — Water Flask (PolygonLite)"
        };

        private static readonly string[] ActivePaths =
        {
            "Assets/LastSignal/Art/FirstTown/Prefabs/Medicine.prefab",
            "Assets/LastSignal/Art/FirstTown/Prefabs/Medicine.prefab",
            "Assets/LastSignal/Art/TriplePistols/Custom9mm/Visual.prefab",
            "Assets/LastSignal/Art/TriplePistols/DXT/Visual.prefab",
            "Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Shirtless_Visual.prefab",
            "Assets/LastSignal/Prefabs/Items/drink.water.prefab"
        };

        // Inactive / deferred subjects
        private static readonly string DeferredLabel = "Space Crew — Character & Helmet (INACTIVE_DEFERRED)";
        private static readonly string DeferredPath = "Assets/LastSignal/Prefabs/Combat/WorldBody_tt3d.prefab";

        private Vector2 _scroll;
        private int _selectedIndex = -1;
        private bool _isDeferredSelected = false;
        private GameObject _inspectedTarget;
        private bool _showDeferred = false;

        // Controlled lighting settings (100% non-destructive, HideAndDontSave)
        private bool _enableStudioRig = true;
        private float _ev100 = 10.0f; // Default balanced studio exposure
        private float _keyLux = 10000f;
        private float _fillLux = 3000f;
        private float _rimLux = 2000f;

        private GameObject _currentRig;
        private Volume _currentVolume;
        private Exposure _volumeExposure;
        private Light _keyLight;
        private Light _fillLight;
        private Light _rimLight;

        [MenuItem("Last Signal/Rendering/HDRP Integrity/5. Owner Visual Review")]
        public static void Open()
        {
            var window = GetWindow<HdrpMaterialReviewWindow>("P0 Visual Review");
            window.minSize = new Vector2(500, 640);
            window.Show();
        }

        private void OnEnable()
        {
            FindExistingRig();
        }

        private void OnDisable()
        {
            DestroyRig();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("P0 HDRP Visual Review & Capture Handoff", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Owner visual inspection mode. Repaired prefabs open in isolation.\n" +
                "No materials, transforms, scenes, importers or gameplay settings are modified.\n" +
                "Exit Play Mode before inspecting subjects.",
                MessageType.Info);

            // =========================================================================
            // CONTROLLED LIGHTING & EXPOSURE RIG (Separates Material vs Lighting vs EV)
            // =========================================================================
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Controlled Studio Lighting & Exposure Rig", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                _enableStudioRig = EditorGUILayout.ToggleLeft(
                    "Enable Studio Lighting (3-Point Lights + HDRP Sky/Exposure)",
                    _enableStudioRig, EditorStyles.boldLabel);

                if (_enableStudioRig)
                {
                    EditorGUILayout.HelpBox(
                        "Why models look dark in Scene View without this:\n" +
                        "1. Metallic PBR (guns/metal) has ZERO diffuse albedo. Without environment reflections (Sky/HDRI), metal reflects pitch black!\n" +
                        "2. Single Scene View headlight has no fill light, causing harsh unlit backsides.\n" +
                        "3. This rig injects temporary (HideAndDontSave) Key+Fill+Rim lights and a Sky/Exposure Volume.",
                        MessageType.None);

                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Lighting Presets:", EditorStyles.miniBoldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Studio EV 0 (1:1 Pre-Exposure)"))
                        {
                            _ev100 = -0.263f; // RenderPipelineAdapter.HdrpUnitExposureEv100
                            _keyLux = 314f;
                            _fillLux = 150f;
                            _rimLux = 100f;
                            UpdateRigProperties();
                        }
                        if (GUILayout.Button("Studio EV 10 (Neutral Balanced)"))
                        {
                            _ev100 = 10.0f;
                            _keyLux = 10000f;
                            _fillLux = 3500f;
                            _rimLux = 2000f;
                            UpdateRigProperties();
                        }
                        if (GUILayout.Button("Daylight EV 13 (FirstTown Outdoor)"))
                        {
                            _ev100 = 13.0f;
                            _keyLux = 80000f;
                            _fillLux = 15000f;
                            _rimLux = 10000f;
                            UpdateRigProperties();
                        }
                    }

                    EditorGUILayout.Space(4);
                    _ev100 = EditorGUILayout.Slider("Exposure (EV100)", _ev100, -2.0f, 16.0f);
                    _keyLux = EditorGUILayout.Slider("Key Light (Lux)", _keyLux, 100f, 100000f);
                    _fillLux = EditorGUILayout.Slider("Fill Light (Lux)", _fillLux, 50f, 30000f);

                    if (GUILayout.Button("Revert to Default Scene View Headlight (Destroy Rig)"))
                    {
                        _enableStudioRig = false;
                        DestroyRig();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Studio rig disabled. Using default Unity Scene View lighting.\n" +
                        "(Remember: turn Scene Lighting OFF in Scene View controls if using camera headlight).",
                        MessageType.None);
                    DestroyRig();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (_enableStudioRig) EnsureOrUpdateRig();
                    else DestroyRig();
                }
            }

            // =========================================================================
            // ACTIVE PRODUCTION SUBJECTS
            // =========================================================================
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Active Review Subjects", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                for (int i = 0; i < ActiveLabels.Length; i++)
                {
                    bool isCurrent = (_selectedIndex == i && !_isDeferredSelected);
                    GUI.backgroundColor = isCurrent ? new Color(0.6f, 0.85f, 1f) : Color.white;
                    if (GUILayout.Button(ActiveLabels[i], GUILayout.Height(28)))
                    {
                        _selectedIndex = i;
                        _isDeferredSelected = false;
                        OpenSubject(ActivePaths[i], i == 1 ? "PharmaSyringe2" : null);
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            // =========================================================================
            // INACTIVE / DEFERRED ASSETS
            // =========================================================================
            EditorGUILayout.Space(4);
            _showDeferred = EditorGUILayout.Foldout(_showDeferred, "Inactive / Deferred Assets (Non-Gameplay)");
            if (_showDeferred)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        "Space Crew (WorldBody_tt3d) is NOT used by FirstTownCore or Player.prefab (Arberry is active).\n" +
                        "Classified as INACTIVE_DEFERRED. Deprioritized from current visual QA.",
                        EditorStyles.wordWrappedMiniLabel);

                    GUI.backgroundColor = _isDeferredSelected ? new Color(0.9f, 0.9f, 0.7f) : Color.white;
                    if (GUILayout.Button(DeferredLabel, GUILayout.Height(24)))
                    {
                        _selectedIndex = -1;
                        _isDeferredSelected = true;
                        OpenSubject(DeferredPath, null);
                    }
                    GUI.backgroundColor = Color.white;
                }
            }

            // =========================================================================
            // ACTIVE INSPECTION DETAILS & PBR DIAGNOSTIC READOUT
            // =========================================================================
            if (_inspectedTarget != null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Active Review Target & Diagnostic Readout", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Target Object:", _inspectedTarget.name, EditorStyles.boldLabel);
                    string activePath = _isDeferredSelected ? DeferredPath :
                        (_selectedIndex >= 0 && _selectedIndex < ActivePaths.Length ? ActivePaths[_selectedIndex] : "");
                    if (!string.IsNullOrEmpty(activePath))
                    {
                        EditorGUILayout.LabelField("Source Prefab:", activePath);
                    }

                    if (_selectedIndex == 1 && !_isDeferredSelected)
                    {
                        EditorGUILayout.HelpBox(
                            "PharmaSyringe2 Topology Note:\n" +
                            "Watertight 2-manifold (298 verts, 0 boundary edges, 0 non-manifold edges).\n" +
                            "Polygon 378 is a 5-gon with projected overlapping edges in 2D.\n" +
                            "Verify visually: check for surface holes, dark shading seams, or distorted normals.",
                            MessageType.Warning);
                    }

                    var renderers = _inspectedTarget.GetComponentsInChildren<Renderer>(true);
                    EditorGUILayout.LabelField($"Renderers Found ({renderers.Length}):", EditorStyles.miniBoldLabel);

                    foreach (var rend in renderers)
                    {
                        string rendName = rend.name;
                        var mats = rend.sharedMaterials;
                        for (int s = 0; s < mats.Length; s++)
                        {
                            var mat = mats[s];
                            if (mat == null)
                            {
                                EditorGUILayout.LabelField($"  [{rendName}] Slot {s}: <MISSING MATERIAL>", EditorStyles.boldLabel);
                            }
                            else
                            {
                                string shaderName = mat.shader != null ? mat.shader.name : "<null shader>";
                                bool isHdrp = shaderName.StartsWith("HDRP/", StringComparison.Ordinal) ||
                                              shaderName.StartsWith("LastSignal/", StringComparison.Ordinal);
                                string statusTag = isHdrp ? "[HDRP OK]" : "[NON-HDRP]";

                                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                                float smoothness = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0f;
                                float aoMin = mat.HasProperty("_AORemapMin") ? mat.GetFloat("_AORemapMin") : 1f;

                                EditorGUILayout.LabelField($"  [{rendName}] Slot {s}: {mat.name} -> {shaderName} {statusTag}");
                                EditorGUILayout.LabelField($"       ↳ PBR: Metallic={metallic:F2}, Smoothness={smoothness:F2}, AORemapMin={aoMin:F2}", EditorStyles.miniLabel);

                                if (metallic > 0.5f && !_enableStudioRig)
                                {
                                    EditorGUILayout.HelpBox(
                                        $"Slot {s} ({mat.name}) is HIGHLY METALLIC ({metallic:F2}). " +
                                        "Without Studio Lighting / Sky reflections, it will appear dark/black. Enable Studio Lighting above.",
                                        MessageType.Warning);
                                }
                            }
                        }
                    }

                    EditorGUILayout.Space(4);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Refocus Scene View", GUILayout.Height(24)))
                        {
                            FocusTarget(_inspectedTarget);
                        }
                        if (GUILayout.Button("Ping in Project", GUILayout.Height(24)))
                        {
                            if (!string.IsNullOrEmpty(activePath))
                            {
                                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(activePath);
                                if (asset != null) EditorGUIUtility.PingObject(asset);
                            }
                        }
                    }
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("QA Verification Protocol", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Capture set:\n" +
                "• Medicine complete overview + PharmaSyringe2 close-up\n" +
                "• Custom9mm pistol & magazine + DXT pistol\n" +
                "• Shirtless zombie body & clothes\n" +
                "• PolygonLite item representative (drink.water)\n" +
                "Compare Studio Rig ON vs OFF to clearly distinguish material albedo from lighting/exposure attenuation.\n" +
                "Status remains VISUAL_NOT_APPROVED until owner reviews and accepts screenshot evidence.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndScrollView();
        }

        private void OpenSubject(string path, string childTargetName)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before inspecting review subjects.");
            }

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefabAsset)
            {
                throw new InvalidOperationException("Missing review prefab: " + path);
            }

            bool opened = PrefabStageUtility.OpenPrefab(path);
            if (!opened)
            {
                AssetDatabase.OpenAsset(prefabAsset);
            }

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject root = (stage != null && stage.prefabContentsRoot != null) ? stage.prefabContentsRoot : prefabAsset;

            GameObject target = root;
            if (!string.IsNullOrEmpty(childTargetName))
            {
                var child = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == childTargetName);
                if (child == null)
                {
                    throw new InvalidOperationException($"Expected child '{childTargetName}' was not found inside {path}");
                }
                target = child.gameObject;
            }

            _inspectedTarget = target;
            Selection.activeGameObject = target;
            EditorGUIUtility.PingObject(target);
            FocusTarget(target);

            if (_enableStudioRig)
            {
                EnsureOrUpdateRig();
            }
        }

        private static void FocusTarget(GameObject target)
        {
            if (target == null) return;
            Selection.activeGameObject = target;
            var view = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
            if (view != null)
            {
                view.Show();
                view.Focus();
                view.FrameSelected();
            }
        }

        // =========================================================================
        // REVERSIBLE STUDIO RIG IMPLEMENTATION (HideAndDontSave)
        // =========================================================================
        private void EnsureOrUpdateRig()
        {
            FindExistingRig();

            if (_currentRig == null)
            {
                _currentRig = new GameObject(RigGameObjectName);
                _currentRig.hideFlags = HideFlags.HideAndDontSave;

                // 1. Key Light (Sun/Warm Key from Front-Right)
                var keyGo = new GameObject("InspectionKeyLight");
                keyGo.hideFlags = HideFlags.HideAndDontSave;
                keyGo.transform.SetParent(_currentRig.transform);
                keyGo.transform.rotation = Quaternion.Euler(45f, 35f, 0f);
                _keyLight = keyGo.AddComponent<Light>();
                _keyLight.type = LightType.Directional;
                _keyLight.color = new Color(1.0f, 0.98f, 0.95f);
                _keyLight.shadows = LightShadows.Soft;
                var keyHd = keyGo.AddComponent<HDAdditionalLightData>();
                keyHd.affectsVolumetric = false;

                // 2. Fill Light (Cool Diffuse Fill from Opposite Angle)
                var fillGo = new GameObject("InspectionFillLight");
                fillGo.hideFlags = HideFlags.HideAndDontSave;
                fillGo.transform.SetParent(_currentRig.transform);
                fillGo.transform.rotation = Quaternion.Euler(30f, -145f, 0f);
                _fillLight = fillGo.AddComponent<Light>();
                _fillLight.type = LightType.Directional;
                _fillLight.color = new Color(0.85f, 0.92f, 1.0f);
                _fillLight.shadows = LightShadows.None;
                var fillHd = fillGo.AddComponent<HDAdditionalLightData>();
                fillHd.affectsVolumetric = false;

                // 3. Rim Light (Back Light for Silhouette Separation)
                var rimGo = new GameObject("InspectionRimLight");
                rimGo.hideFlags = HideFlags.HideAndDontSave;
                rimGo.transform.SetParent(_currentRig.transform);
                rimGo.transform.rotation = Quaternion.Euler(-20f, 180f, 0f);
                _rimLight = rimGo.AddComponent<Light>();
                _rimLight.type = LightType.Directional;
                _rimLight.color = Color.white;
                _rimLight.shadows = LightShadows.None;
                var rimHd = rimGo.AddComponent<HDAdditionalLightData>();
                rimHd.affectsVolumetric = false;

                // 4. HDRP Global Volume for Controlled Exposure & Sky Reflections
                var volGo = new GameObject("InspectionVolume");
                volGo.hideFlags = HideFlags.HideAndDontSave;
                volGo.transform.SetParent(_currentRig.transform);
                _currentVolume = volGo.AddComponent<Volume>();
                _currentVolume.isGlobal = true;

                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.hideFlags = HideFlags.HideAndDontSave;

                // Add Exposure
                _volumeExposure = profile.Add<Exposure>(true);
                _volumeExposure.mode.Override(ExposureMode.Fixed);

                // Add Visual Environment & Physically Based Sky for Metallic Reflections
                var visualEnv = profile.Add<VisualEnvironment>(true);
                visualEnv.skyType.Override((int)SkyType.PhysicallyBased);
                visualEnv.skyAmbientMode.Override(SkyAmbientMode.Dynamic);

                var pbrSky = profile.Add<PhysicallyBasedSky>(true);
                pbrSky.type.Override(PhysicallyBasedSkyModel.EarthAdvanced);
                pbrSky.skyIntensityMode.Override(SkyIntensityMode.Multiplier);
                pbrSky.multiplier.Override(1.0f);

                // Add Tonemapping
                var tonemapping = profile.Add<Tonemapping>(true);
                tonemapping.mode.Override(TonemappingMode.ACES);

                _currentVolume.sharedProfile = profile;
            }

            UpdateRigProperties();
        }

        private void UpdateRigProperties()
        {
            if (_keyLight != null) _keyLight.intensity = _keyLux * (1.0f / Mathf.PI);
            if (_fillLight != null) _fillLight.intensity = _fillLux * (1.0f / Mathf.PI);
            if (_rimLight != null) _rimLight.intensity = _rimLux * (1.0f / Mathf.PI);

            if (_volumeExposure != null)
            {
                _volumeExposure.fixedExposure.Override(_ev100);
            }

            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.Repaint();
            }
        }

        private void FindExistingRig()
        {
            if (_currentRig == null)
            {
                _currentRig = GameObject.Find(RigGameObjectName);
            }
        }

        private void DestroyRig()
        {
            FindExistingRig();
            if (_currentRig != null)
            {
                DestroyImmediate(_currentRig);
                _currentRig = null;
                _currentVolume = null;
                _volumeExposure = null;
                _keyLight = null;
                _fillLight = null;
                _rimLight = null;

                var view = SceneView.lastActiveSceneView;
                if (view != null)
                {
                    view.Repaint();
                }
            }
        }
    }
}
