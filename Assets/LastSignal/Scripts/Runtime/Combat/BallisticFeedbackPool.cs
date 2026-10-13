using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal
{
    /// <summary>Weapon-owned fixed budget; all world transforms remain independent of viewmodel motion.</summary>
    public sealed class BallisticFeedbackPool : MonoBehaviour
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Combat.BallisticFeedback");
        sealed class Trail { public LineRenderer line; public Vector3 from, to; public float start, length; }
        sealed class Mark { public GameObject root; public MeshRenderer visual; public Collider support; public Vector3 localPoint, localNormal; public float expires; }
        Trail[] trails;
        Mark[] marks;
        ParticleSystem[] debris;
        CombatFeedbackProfile profile;
        Transform observer;
        bool wasReduced;
        int nextTrail, nextMark, nextDebris;
        public int PresentedShots { get; private set; }
        public int ActiveTracers { get { int n = 0; if (trails != null) foreach (var t in trails) if (t.line.enabled) n++; return n; } }
        public int ActiveMarks { get { int n = 0; if (marks != null) foreach (var m in marks) if (m.root.activeSelf) n++; return n; } }
        public void Initialize(CombatFeedbackProfile value, Transform view)
        {
            if (profile || !value || !value.IsValid) return;
            profile = value; observer = view;
            trails = new Trail[profile.tracerCapacity];
            for (int i = 0; i < trails.Length; i++)
            {
                var root = new GameObject("Ballistic streak"); root.transform.SetParent(transform, false);
                var line = root.AddComponent<LineRenderer>();
                line.sharedMaterial = profile.tracerMaterial; line.useWorldSpace = true; line.positionCount = 2;
                line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false; line.enabled = false;
                trails[i] = new Trail { line = line };
            }
            marks = new Mark[profile.markCapacity];
            for (int i = 0; i < marks.Length; i++)
            {
                var root = GameObject.CreatePrimitive(PrimitiveType.Quad); root.name = "Bullet entry";
                root.transform.SetParent(transform, false); root.layer = 2;
                var collider = root.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
                var renderer = root.GetComponent<MeshRenderer>(); renderer.sharedMaterial = profile.impactMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                root.SetActive(false); marks[i] = new Mark { root = root, visual = renderer };
            }
            debris = new ParticleSystem[8];
            for (int i = 0; i < debris.Length; i++)
            {
                var root = new GameObject("Surface debris"); root.transform.SetParent(transform, false);
                var ps = root.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.playOnAwake = false; main.loop = false; main.maxParticles = 12;
                main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = .22f; main.startSize = .012f; main.gravityModifier = .7f;
                var emission = ps.emission; emission.enabled = false;
                var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = profile.tracerMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off; debris[i] = ps;
            }
        }
        public void Present(WeaponFireResolver.ShotResult shot)
        {
            if (!profile) return;
            using (Marker.Auto())
            {
                PresentedShots++;
                if ((profile.reducedEffects || CombatPresentationPreference.ReducedEffects)) return;
                // A barrel clipped through a wall cannot produce a credible muzzle-to-wall streak.
                // Suppress that cosmetic instead of drawing through the obstruction.
                if (!shot.BarrelClipped && shot.ShotId % (ulong)profile.tracerEvery == 0 && shot.Distance > .2f)
                {
                    var t = trails[nextTrail]; nextTrail = (nextTrail + 1) % trails.Length;
                    t.from = shot.SourcePosition; t.to = shot.EndPoint; t.length = Vector3.Distance(t.from, t.to); t.start = Time.time;
                    t.line.startWidth = profile.tracerWidth; t.line.endWidth = profile.tracerWidth * .3f;
                    Color color = profile.tracerColor; color.r *= profile.tracerBrightness; color.g *= profile.tracerBrightness; color.b *= profile.tracerBrightness;
                    t.line.startColor = color; t.line.endColor = new Color(color.r, color.g, color.b, 0);
                    t.line.SetPosition(0, t.from); t.line.SetPosition(1, Vector3.MoveTowards(t.from, t.to, Mathf.Min(t.length, 1.5f)));
                    t.line.enabled = true;
                }
                if (!shot.Hit || ImpactSurface.Organic(shot.Surface) || !shot.Collider ||
                    (observer && (shot.HitPoint - observer.position).sqrMagnitude > profile.effectDistance * profile.effectDistance)) return;
                var ps = debris[nextDebris]; nextDebris = (nextDebris + 1) % debris.Length;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.Play(false);
                var emit = new ParticleSystem.EmitParams { position = shot.HitPoint + shot.HitNormal * .006f,
                    startColor = shot.Surface == ImpactSurfaceKind.Metal ? new Color(.9f,.65f,.3f) :
                    shot.Surface == ImpactSurfaceKind.Wood ? new Color(.3f,.21f,.12f) : new Color(.35f,.33f,.29f) };
                for (int i = 0; i < 5; i++)
                { emit.velocity = (shot.HitNormal + Random.insideUnitSphere * .6f) * Random.Range(.5f, 1.8f); ps.Emit(emit, 1); }
                if (!Eligible(shot.Collider)) return;
                float size = profile.markSize * (shot.Surface == ImpactSurfaceKind.Soil ? 1.5f : 1);
                var rotation = Quaternion.LookRotation(-shot.HitNormal);
                if (!Supported(shot.Collider, shot.HitPoint, shot.HitNormal, rotation, size)) return;
                var m = marks[nextMark]; nextMark = (nextMark + 1) % marks.Length;
                m.support = shot.Collider; m.localPoint = m.support.transform.InverseTransformPoint(shot.HitPoint);
                m.localNormal = m.support.transform.InverseTransformDirection(shot.HitNormal); m.expires = Time.time + profile.markLifetime;
                m.root.transform.SetPositionAndRotation(shot.HitPoint + shot.HitNormal * .0015f, rotation);
                m.root.transform.localScale = Vector3.one * size; m.visual.enabled = true; m.root.SetActive(true);
            }
        }
        static bool Eligible(Collider c) => !c.attachedRigidbody && c.GetComponentInParent<IDamageable>() == null &&
            !c.GetComponentInParent<CharacterController>() && (c is TerrainCollider || c.GetComponent<Renderer>());
        public static bool Supported(Collider c, Vector3 point, Vector3 normal, Quaternion rotation, float size)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = point + rotation * new Vector3((i % 2 == 0 ? -1 : 1) * size * .5f, (i < 2 ? -1 : 1) * size * .5f, 0);
                // Query the selected collider, never accept a different surface behind an edge.
                if (!c.Raycast(new Ray(corner + normal * .02f, -normal), out var hit, .04f) || Vector3.Dot(hit.normal, normal) < .97f) return false;
            }
            return true;
        }
        void Update()
        {
            if (!profile) return;
            bool reduced = profile.reducedEffects || CombatPresentationPreference.ReducedEffects;
            if (reduced && !wasReduced) Clear();
            wasReduced = reduced;
            if (reduced) return;
            foreach (var t in trails)
            {
                if (!t.line.enabled) continue;
                float age = Time.time - t.start;
                if (age > profile.tracerLifetime || (profile.reducedEffects || CombatPresentationPreference.ReducedEffects)) { t.line.enabled = false; continue; }
                float distance = Mathf.Min(t.length, age * profile.tracerSpeed + 1.5f);
                Vector3 direction = (t.to - t.from).normalized;
                t.line.SetPosition(0, t.from + direction * Mathf.Max(0, distance - 1.5f));
                t.line.SetPosition(1, t.from + direction * distance);
            }
            foreach (var m in marks)
            {
                if (!m.root.activeSelf) continue;
                if (!m.support || !m.support.enabled || !m.support.gameObject.activeInHierarchy || Time.time >= m.expires || (profile.reducedEffects || CombatPresentationPreference.ReducedEffects))
                { m.root.SetActive(false); m.support = null; continue; }
                m.visual.enabled = !observer || (m.root.transform.position - observer.position).sqrMagnitude <= profile.effectDistance * profile.effectDistance;
                Vector3 n = m.support.transform.TransformDirection(m.localNormal);
                m.root.transform.SetPositionAndRotation(m.support.transform.TransformPoint(m.localPoint) + n * .0015f, Quaternion.LookRotation(-n));
            }
        }
        public void Clear()
        {
            if (trails != null) foreach (var t in trails) t.line.enabled = false;
            if (marks != null) foreach (var m in marks) { m.root.SetActive(false); m.support = null; }
            if (debris != null) foreach (var p in debris) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void OnDisable() => Clear();
    }
}
