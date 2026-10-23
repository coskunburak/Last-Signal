using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.RenderingQuality
{
    [DisallowMultipleComponent]
    public sealed class GraphicsSettingsManager : MonoBehaviour
    {
        public static GraphicsSettingsManager Instance { get; private set; }
        public static GraphicsSettingsManager Ensure()
        {
            if (Instance) return Instance;
            return new GameObject("Graphics settings").AddComponent<GraphicsSettingsManager>();
        }
        readonly HashSet<VegetationShadowController> trees = new HashSet<VegetationShadowController>();
        readonly List<Camera> observers = new List<Camera>();
        GraphicsSettingsState active;
        public GraphicsSettingsState State => active.Copy();
        public GraphicsQualityProfiles Profiles { get; private set; }
        public HdrpGraphicsAdapter Adapter { get; private set; }
        public event Action Changed;
        public bool Suspended { get; private set; }
        public bool ChangesLocked { get; set; }
        public bool DiagnosticActive { get; set; }
        public string Message { get; private set; } = "";
        float originalLod, nextDistanceUpdate;
        public int RegisteredTreeGroups => trees.Count;
        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            Profiles = GraphicsQualityProfiles.Load(); active = GraphicsSettingsPersistence.Load(Profiles);
            originalLod = QualitySettings.lodBias; Adapter = new HdrpGraphicsAdapter(transform);
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) RegisterScene(SceneManager.GetSceneAt(i));
            ApplyInternal();
        }
        void OnSceneLoaded(Scene scene, LoadSceneMode mode) { RegisterScene(scene); ApplyInternal(); }
        void OnSceneUnloaded(Scene scene)
        {
            trees.RemoveWhere(t => !t); observers.RemoveAll(c => !c); ApplyInternal();
        }
        void RegisterScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects()) RegisterRoot(root);
        }
        // Existing scene/session spawn paths call this once. New spawned lights/cameras must register here too.
        public void RegisterRoot(GameObject root)
        {
            if (!root || Adapter == null) return;
            foreach (var c in root.GetComponentsInChildren<Camera>(true))
            { Adapter.Register(c); if (!observers.Contains(c)) observers.Add(c); }
            foreach (var light in root.GetComponentsInChildren<Light>(true)) Adapter.Register(light);
        }
        Vector3? Observer()
        {
            Camera fallback = null;
            foreach (var camera in observers)
                if (camera && camera.isActiveAndEnabled && camera.cameraType == CameraType.Game)
                { if (camera.CompareTag("MainCamera")) return camera.transform.position; if (!fallback) fallback = camera; }
            return fallback ? fallback.transform.position : (Vector3?)null;
        }
        public void Register(VegetationShadowController group)
        {
            if (!group) return; trees.Add(group);
            if (!Suspended && !DiagnosticActive) group.Apply(active, Observer(), Profiles, true);
        }
        public void Unregister(VegetationShadowController group) => trees.Remove(group);
        public bool Apply(GraphicsSettingsState requested, bool save = true)
        {
            if (ChangesLocked || DiagnosticActive) { Message = "Kayıt veya tanılama bitmeden ayar değiştirilemez."; return false; }
            if (requested == null || !requested.IsValid) { Message = "Geçersiz grafik ayarı."; return false; }
            if (!Adapter.Supported) { Message = "HDRP etkin değil."; return false; }
            active = requested.Copy();
            if (active.preset != GraphicsPreset.Custom && !active.SameValues(Profiles.Get(active.preset))) active.preset = GraphicsPreset.Custom;
            if ((!Adapter.FogSupported && active.volumetricFog) || (!Adapter.AoSupported && active.ambientOcclusion))
            {
                active.volumetricFog &= Adapter.FogSupported; active.ambientOcclusion &= Adapter.AoSupported; active.preset = GraphicsPreset.Custom;
                Message = "Pipeline tarafından desteklenmeyen özellikler kapalı tutuldu.";
            }
            else Message = "Uygulandı.";
            Suspended = false; ApplyInternal();
            if (save) GraphicsSettingsPersistence.Save(active);
            Changed?.Invoke(); return true;
        }
        public bool SetPreset(GraphicsPreset preset) => Apply(Profiles.Get(preset));
        public bool ResetDefaults() => SetPreset(GraphicsPreset.Low);
        public void Reapply() { if (!Suspended) ApplyInternal(); }
        void ApplyInternal()
        {
            if (Suspended || Adapter == null || !Adapter.Supported) return;
            if ((!Adapter.FogSupported && active.volumetricFog) || (!Adapter.AoSupported && active.ambientOcclusion))
            {
                active.volumetricFog &= Adapter.FogSupported; active.ambientOcclusion &= Adapter.AoSupported;
                active.preset = GraphicsPreset.Custom;
                Message = "Pipeline tarafından desteklenmeyen özellikler kapalı tutuldu.";
            }
            QualitySettings.lodBias = active.lodBias; Adapter.Apply(active, Profiles);
            var observer = Observer();
            if (!DiagnosticActive) foreach (var group in trees) if (group) group.Apply(active, observer, Profiles, true);
        }
        public void RestoreAuthored()
        {
            if (ChangesLocked || DiagnosticActive) { Message = "Kayıt veya tanılama sırasında geri yükleme kilitli."; return; }
            Suspended = true; Adapter.Restore(); QualitySettings.lodBias = originalLod;
            foreach (var group in trees) if (group) group.Restore();
            Message = "Sahnenin özgün ayarları geçici olarak geri yüklendi. Kaydedilmiş tercih değiştirilmedi."; Changed?.Invoke();
        }
        void Update()
        {
            if (Suspended || DiagnosticActive || Time.unscaledTime < nextDistanceUpdate) return;
            nextDistanceUpdate = Time.unscaledTime + .25f;
            if (active.treeShadows != ShadowQuality.Low && active.treeShadows != ShadowQuality.Medium) return;
            var position = Observer();
            // Cached registrations only. No scene queries or renderer allocations here.
            foreach (var group in trees) if (group) group.Apply(active, position, Profiles, false);
        }
        void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Adapter?.Dispose(); QualitySettings.lodBias = originalLod;
            foreach (var group in trees) if (group) group.Restore();
            Instance = null;
        }
    }
}
