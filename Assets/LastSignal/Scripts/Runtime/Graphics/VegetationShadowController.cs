using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.RenderingQuality
{
    /// <summary>Explicit tree group registration. All LODs; no name heuristics, mesh or physics mutations.</summary>
    [DisallowMultipleComponent]
    public sealed class VegetationShadowController : MonoBehaviour
    {
        struct Entry { public Renderer renderer; public ShadowCastingMode original; }
        readonly List<Entry> entries = new List<Entry>();
        GraphicsSettingsManager manager;
        bool casting = true;
        public int RendererCount => entries.Count;
        public void Refresh()
        {
            Restore(); entries.Clear();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r is MeshRenderer || r is SkinnedMeshRenderer) entries.Add(new Entry { renderer = r, original = r.shadowCastingMode });
            manager = GraphicsSettingsManager.Ensure(); manager.Register(this);
        }
        void OnEnable() { if (Application.isPlaying) Refresh(); }
        void OnDisable() { if (manager) manager.Unregister(this); Restore(); entries.Clear(); }
        void OnTransformChildrenChanged() { if (Application.isPlaying && isActiveAndEnabled) Refresh(); }
        public void Apply(GraphicsSettingsState state, Vector3? observer, GraphicsQualityProfiles profiles, bool force)
        {
            bool next = state.treeShadows == ShadowQuality.High;
            if (state.treeShadows == ShadowQuality.Low || state.treeShadows == ShadowQuality.Medium)
            {
                float distance = state.treeShadows == ShadowQuality.Low ? profiles.lowTreeDistance : profiles.mediumTreeDistance;
                // Whole mixed trees are the safe fallback; no unverified trunk/leaf classification.
                if (!force && casting) distance += profiles.distanceHysteresis;
                next = observer.HasValue && (transform.position - observer.Value).sqrMagnitude <= distance * distance;
            }
            if (!force && next == casting) return;
            casting = next;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                if (!e.renderer) { entries.RemoveAt(i); continue; }
                e.renderer.shadowCastingMode = next ? e.original : ShadowCastingMode.Off;
            }
        }
        public void Restore()
        {
            foreach (var e in entries) if (e.renderer) e.renderer.shadowCastingMode = e.original;
        }
        public static VegetationShadowController Attach(GameObject root)
        {
            var existing = root.GetComponent<VegetationShadowController>();
            return existing ? existing : root.AddComponent<VegetationShadowController>();
        }
    }
}
