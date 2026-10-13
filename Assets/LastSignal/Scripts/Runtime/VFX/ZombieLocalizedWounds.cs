using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal
{
    [DisallowMultipleComponent]
    public sealed class ZombieLocalizedWounds : MonoBehaviour
    {
        public const int MaximumWounds = 8;
        static readonly HashSet<ZombieLocalizedWounds> active = new HashSet<ZombieLocalizedWounds>();
        public static void ClearSession() { foreach (var value in active) if (value) value.Clear(); }
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Wounds.Update");
        static readonly ProfilerMarker DeformMarker = new ProfilerMarker("LastSignal.Wounds.Deform");
        static readonly int Centers = Shader.PropertyToID("_WoundCenters"), Normals = Shader.PropertyToID("_WoundNormals"), Details = Shader.PropertyToID("_WoundDetails");
        sealed class Wound
        {
            public ZombieWoundSurfaceResolver.Contact contact;
            public float radius, severity, seed, created;
        }
        sealed class Visual
        {
            public ZombieWoundSurfaceResolver.Surface surface;
            public SkinnedMeshRenderer overlay;
            public Mesh instance;
            public readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        }
        readonly Wound[] wounds = new Wound[MaximumWounds];
        readonly Vector4[] centers = new Vector4[MaximumWounds], normals = new Vector4[MaximumWounds], details = new Vector4[MaximumWounds];
        readonly List<Visual> visuals = new List<Visual>(16);
        readonly ulong[] seen = new ulong[32]; readonly int[] seenSub = new int[32];
        int nextSeen, next; uint generation = 1;
        ZombieHealth health;
        ZombieWoundProfile profile;
        Camera observer;
        float maximumDistance = 35;
        public ZombieWoundSurfaceResolver Resolver { get; private set; }
        public int WoundCount { get; private set; }
        public int MergedWounds { get; private set; }
        public int DeformedVertices { get; private set; }
        public uint Generation => generation;
        public int RuntimeMeshCount { get { int n = 0; foreach (var v in visuals) if (v.instance) n++; return n; } }
        public int ActiveOverlayCount { get { int n = 0; foreach (var v in visuals) if (v.overlay && v.overlay.enabled) n++; return n; } }
        public int Budget => profile ? (CombatPresentationPreference.ReducedEffects ? profile.lowCapacity : profile.capacity) : (CombatPresentationPreference.ReducedEffects ? 4 : 8);
        public void Initialize(Material material, float distance = 35, ZombieWoundProfile tuning = null)
        {
            if (Resolver != null) return;
            health = GetComponent<ZombieHealth>(); profile = tuning && tuning.IsValid ? tuning : null;
            maximumDistance = Mathf.Max(1, distance); observer = Camera.main;
            Resolver = new ZombieWoundSurfaceResolver(gameObject);
            if (!material) return;
            foreach (var surface in Resolver.Surfaces)
            {
                var source = surface.renderer;
                var root = new GameObject("Localized wound surface"); root.transform.SetParent(source.transform, false);
                var overlay = root.AddComponent<SkinnedMeshRenderer>(); overlay.sharedMesh = source.sharedMesh;
                overlay.bones = source.bones; overlay.rootBone = source.rootBone; overlay.quality = source.quality;
                overlay.localBounds = source.localBounds; overlay.updateWhenOffscreen = false;
                overlay.shadowCastingMode = ShadowCastingMode.Off; overlay.receiveShadows = true;
                var materials = new Material[source.sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                overlay.sharedMaterials = materials; overlay.enabled = false;
                visuals.Add(new Visual { surface = surface, overlay = overlay });
            }
        }
        public void Present(DamageReceipt receipt) => Present(receipt, out _);
        public bool Present(DamageReceipt receipt, out ZombieWoundSurfaceResolver.Contact contact)
        {
            contact = default;
            if (!receipt.PresentationAccepted || receipt.Target != health || receipt.Generation != health.LifetimeGeneration ||
                !ZombieGorePreference.GraphicGoreEnabled || Resolver == null || visuals.Count == 0) return false;
            var region = receipt.Hit.HitCollider ? receipt.Hit.HitCollider.GetComponent<ZombieHitRegion>() : null;
            if (!region || region.Owner != health) return false;
            if (receipt.Hit.ShotId != 0)
            {
                for (int i = 0; i < seen.Length; i++) if (seen[i] == receipt.Hit.ShotId && seenSub[i] == receipt.Hit.SubHitId) return false;
                seen[nextSeen] = receipt.Hit.ShotId; seenSub[nextSeen] = receipt.Hit.SubHitId; nextSeen = (nextSeen + 1) % seen.Length;
            }
            contact = receipt.SurfaceContact;
            if (!contact.Valid && !Resolver.Resolve(receipt.Hit, profile ? profile.surfaceSearchDistance : .35f, out contact)) return false;
            if (!Resolver.Surfaces.Contains(contact.surface)) return false;
            TrimBudget();
            float severity = Mathf.Clamp01(receipt.Hit.Amount / 60f);
            float radius = (profile ? profile.radius : .035f) * (receipt.Hit.Region == DamageRegion.Head ? .8f : 1) * (1 + severity * .35f);
            float merge = Mathf.Min(profile ? profile.mergeDistance : .025f, radius * .8f);
            for (int i = 0; i < WoundCount; i++)
            {
                var w = wounds[i];
                if (w.contact.surface != contact.surface || Vector3.Dot(w.contact.restNormal, contact.restNormal) < .5f ||
                    (w.contact.restPoint - contact.restPoint).sqrMagnitude > merge * merge) continue;
                w.severity = Mathf.Min(1, w.severity + .18f + severity * .25f);
                w.radius = Mathf.Min(radius * 1.7f, Mathf.Max(radius, w.radius + .004f)); w.created = Time.time;
                MergedWounds++; RebuildDeformation(contact.surface); UpdateVisuals(); return true;
            }
            int index = WoundCount < Budget ? WoundCount++ : next++ % Budget;
            var old = wounds[index]?.contact.surface;
            var entry = wounds[index] ?? new Wound();
            entry.contact = contact; entry.radius = radius; entry.severity = severity;
            entry.seed = (receipt.Hit.ShotId % 997) * .618f; entry.created = Time.time; wounds[index] = entry;
            if (old != null && old != contact.surface) RebuildDeformation(old);
            RebuildDeformation(contact.surface); UpdateVisuals(); return true;
        }
        void TrimBudget()
        {
            if (WoundCount <= Budget) return;
            for (int i = Budget; i < WoundCount; i++) wounds[i] = null;
            WoundCount = Budget; next = 0;
            foreach (var v in visuals) if (v.instance) RebuildDeformation(v.surface);
        }
        public bool TryGetContact(int index, out ZombieWoundSurfaceResolver.Contact contact)
        { contact = index >= 0 && index < WoundCount ? wounds[index].contact : default; return contact.Valid; }
        public float SeverityAt(int index) => index >= 0 && index < WoundCount ? wounds[index].severity : 0;
        void RebuildDeformation(ZombieWoundSurfaceResolver.Surface s)
        {
            using (DeformMarker.Auto())
            {
                // Sparse clothing meshes cannot form credible craters. Keep those as torn-fabric shading.
                if (s.clothing || (s.part != ZombieBodyPart.Head && s.name != "Ribcage")) return;
                Visual visual = null; foreach (var candidate in visuals) if (candidate.surface == s) { visual = candidate; break; }
                if (visual == null) return;
                bool hasWound = false;
                for (int j = 0; j < WoundCount; j++) if (wounds[j].contact.surface == s) { hasWound = true; break; }
                if (!hasWound && !visual.instance) return;
                if (!visual.instance)
                {
                    visual.instance = Instantiate(s.original); visual.instance.name = s.original.name + " Instance wounds";
                    visual.instance.MarkDynamic(); s.renderer.sharedMesh = visual.instance; visual.overlay.sharedMesh = visual.instance;
                }
                float limit = profile ? profile.maximumIndent : .003f;
                DeformedVertices = 0;
                for (int i = 0; i < s.rest.Length; i++)
                {
                    float depth = 0;
                    for (int j = 0; j < WoundCount; j++)
                    {
                        var w = wounds[j]; if (w.contact.surface != s || Vector3.Dot(s.normals[i], w.contact.restNormal) < .5f) continue;
                        float d = Vector3.Distance(s.rest[i], w.contact.restPoint) / w.radius;
                        if (d < 1) depth = Mathf.Max(depth, limit * (.3f + .7f * w.severity) * Mathf.Pow(1 - d * d, 2));
                    }
                    s.deformed[i] = s.rest[i] - s.normals[i] * depth;
                    if (depth > 0) DeformedVertices++;
                }
                visual.instance.vertices = s.deformed; visual.instance.RecalculateNormals(); s.Invalidate();
            }
        }
        void LateUpdate() { TrimBudget(); UpdateVisuals(); }
        void UpdateVisuals()
        {
            if (Resolver == null) return;
            using (Marker.Auto())
            {
                if (!observer) observer = Camera.main;
                bool show = ZombieGorePreference.GraphicGoreEnabled && (!observer || (transform.position - observer.transform.position).sqrMagnitude <= maximumDistance * maximumDistance);
                foreach (var visual in visuals)
                {
                    bool active = false; visual.surface.Invalidate();
                    for (int i = 0; i < MaximumWounds; i++)
                    {
                        centers[i] = normals[i] = details[i] = Vector4.zero;
                        var w = wounds[i]; if (w == null || w.contact.surface != visual.surface || !w.contact.Valid) continue;
                        Vector3 p = w.contact.restPoint, n = w.contact.restNormal;
                        centers[i] = new Vector4(p.x, p.y, p.z, w.radius);
                        normals[i] = new Vector4(n.x, n.y, n.z, w.seed);
                        details[i] = new Vector4(w.severity, Mathf.Clamp01((Time.time - w.created) / (profile ? profile.drySeconds : 35)), visual.surface.clothing ? 1 : 0, 0); active = true;
                    }
                    visual.overlay.enabled = active && show;
                    if (!visual.overlay.enabled) continue;
                    visual.block.SetVectorArray(Centers, centers); visual.block.SetVectorArray(Normals, normals); visual.block.SetVectorArray(Details, details);
                    visual.overlay.SetPropertyBlock(visual.block);
                }
            }
        }
        public Persistence.ZombieWoundSnapshot CaptureState()
        {
            var data = new Persistence.ZombieWoundSnapshot { version = 1, entries = new Persistence.ZombieWoundEntry[WoundCount] };
            for (int i = 0; i < WoundCount; i++)
            {
                var w = wounds[i]; data.entries[i] = new Persistence.ZombieWoundEntry { surface = w.contact.surface.name,
                    triangle = w.contact.triangle, barycentric = w.contact.barycentric, radius = w.radius, severity = w.severity, seed = w.seed };
            }
            return data;
        }
        public void RestoreState(Persistence.ZombieWoundSnapshot data)
        {
            Clear();
            if (!ZombieGorePreference.GraphicGoreEnabled || Resolver == null || !Persistence.ZombieWoundSnapshot.Valid(data) || data?.entries == null) return;
            foreach (var entry in data.entries)
            {
                if (WoundCount >= Budget) break;
                var s = Resolver.Surfaces.Find(x => x.name == entry.surface);
                if (s == null || !s.Visible || entry.triangle >= s.triangles.Length / 3) continue;
                int t = entry.triangle * 3; var b = entry.barycentric;
                var c = new ZombieWoundSurfaceResolver.Contact { surface = s, triangle = entry.triangle, barycentric = b,
                    restPoint = s.rest[s.triangles[t]] * b.x + s.rest[s.triangles[t + 1]] * b.y + s.rest[s.triangles[t + 2]] * b.z,
                    restNormal = (s.normals[s.triangles[t]] * b.x + s.normals[s.triangles[t + 1]] * b.y + s.normals[s.triangles[t + 2]] * b.z).normalized };
                wounds[WoundCount++] = new Wound { contact = c, radius = entry.radius, severity = entry.severity, seed = entry.seed, created = Time.time - 120 };
            }
            foreach (var v in visuals) RebuildDeformation(v.surface);
            UpdateVisuals(); // No burst, bleed, health event or corpse restoration.
        }
        public void Clear()
        {
            WoundCount = next = nextSeen = MergedWounds = DeformedVertices = 0; generation++;
            Array.Clear(wounds, 0, wounds.Length); Array.Clear(seen, 0, seen.Length); Array.Clear(seenSub, 0, seenSub.Length);
            foreach (var v in visuals)
            {
                if (v.overlay) { v.overlay.enabled = false; v.overlay.sharedMesh = v.surface.original; }
                if (v.surface.renderer) v.surface.renderer.sharedMesh = v.surface.original;
                Array.Copy(v.surface.rest, v.surface.deformed, v.surface.rest.Length);
                if (v.instance) { if (Application.isPlaying) Destroy(v.instance); else DestroyImmediate(v.instance); v.instance = null; }
                v.surface.Invalidate();
            }
        }
        void PreferenceChanged(bool value) { if (!value) Clear(); }
        void OnEnable() { active.Add(this); ZombieGorePreference.Changed += PreferenceChanged; }
        void OnDisable() { active.Remove(this); ZombieGorePreference.Changed -= PreferenceChanged; Clear(); }
    }
}
