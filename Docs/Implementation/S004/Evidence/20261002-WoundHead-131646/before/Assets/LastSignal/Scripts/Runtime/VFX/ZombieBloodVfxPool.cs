using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal
{
    // Scene-local fixed arrays. All allocations and particle component discovery happen during warmup.
    public sealed class ZombieBloodVfxPool : MonoBehaviour
    {
        sealed class Effect
        {
            public GameObject root;
            public ParticleSystem[] particles;
        }
        sealed class Slot
        {
            public Effect burst, spurt, drip;
            public ZombieBloodVfxPresenter owner;
            public Transform anchor;
            public ZombieBloodVfxProfile.Region region;
            public ZombieBodyPart part;
            public float start;
            public bool dripping;
        }
        sealed class Mark
        {
            public GameObject root;
            public Renderer renderer;
            public Collider supportSurface;
            public readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
            public float start, size;
            public bool pool;
        }
        sealed class Corpse
        {
            public ZombieController actor;
            public string cellId;
            public float expires;
        }
        Corpse[] corpses;
        int nextCorpse;
        public int RetainedCorpses { get { int n = 0; if (corpses != null) foreach (var c in corpses) if (c.actor) n++; return n; } }
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static ZombieBloodVfxPool instance;
        ZombieBloodVfxProfile profile;
        Slot[] slots;
        Mark[] marks;
        readonly System.Collections.Generic.List<Material> surfaceMaterials = new System.Collections.Generic.List<Material>(16);
        int nextEffect, nextMark;
        public int ActiveEffects { get { int n = 0; if (slots != null) foreach (var s in slots) if (s.owner) n++; return n; } }
        public int ActiveMarks { get { int n = 0; if (marks != null) foreach (var m in marks) if (m.root.activeSelf) n++; return n; } }
        public int TotalRequests { get; private set; }

        public static void ReleaseCell(string cellId)
        {
            try
            {
                if (instance && instance.corpses != null)
                    foreach (var c in instance.corpses) if (c.cellId == cellId) ReleaseCorpse(c);
            }
            catch (System.Exception e) { Debug.LogException(e); }
        }
        public static void ResetSession()
        {
            try { if (instance) instance.ClearAll(); }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        public static ZombieBloodVfxPool Warm(ZombieBloodVfxProfile config)
        {
            if (!config || !config.IsValid) return null;
            if (instance) return instance.profile == config ? instance : null;
            var root = new GameObject("Zombie Blood VFX Pool");
            instance = root.AddComponent<ZombieBloodVfxPool>();
            try { instance.Initialize(config); }
            catch (System.Exception e) { Debug.LogException(e); Destroy(root); instance = null; }
            return instance;
        }
        void Initialize(ZombieBloodVfxProfile config)
        {
            profile = config;
            corpses = new Corpse[profile.effectCapacity];
            for (int i = 0; i < corpses.Length; i++) corpses[i] = new Corpse();
            slots = new Slot[profile.effectCapacity];
            for (int i = 0; i < slots.Length; i++) slots[i] = new Slot
            { burst = Create(profile.burstPrefab), spurt = Create(profile.spurtPrefab), drip = Create(profile.dripPrefab) };
            marks = new Mark[profile.markCapacity];
            for (int i = 0; i < marks.Length; i++)
            {
                var root = Instantiate(profile.surfacePrefab, transform);
                root.SetActive(false);
                marks[i] = new Mark { root = root, renderer = root.GetComponentInChildren<Renderer>(true) };
                if (!marks[i].renderer || !marks[i].renderer.sharedMaterial) throw new System.InvalidOperationException("Blood surface renderer/material missing");
            }
            ZombieGorePreference.Changed += GoreChanged;
            SceneManager.activeSceneChanged += SceneChanged;
        }
        Effect Create(GameObject source)
        {
            var root = Instantiate(source, transform);
            var effect = new Effect { root = root, particles = root.GetComponentsInChildren<ParticleSystem>(true) };
            if (effect.particles.Length == 0) throw new System.InvalidOperationException("Blood particle prefab is empty");
            Stop(effect);
            return effect;
        }
        static void Stop(Effect effect)
        {
            foreach (var p in effect.particles) p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.root.SetActive(false);
            effect.root.transform.localScale = Vector3.one;
        }
        static void Begin(Effect effect, Vector3 position, Quaternion rotation, float scale)
        {
            Stop(effect);
            effect.root.transform.SetPositionAndRotation(position, rotation);
            effect.root.transform.localScale = Vector3.one * scale;
            effect.root.SetActive(true);
            foreach (var p in effect.particles) p.Play(false);
        }
        static void Rate(Effect effect, float rate)
        {
            foreach (var p in effect.particles) { var emission = p.emission; emission.rateOverTime = rate; }
        }
        void Reset(Slot s)
        {
            Stop(s.burst); Stop(s.spurt); Stop(s.drip);
            s.owner = null; s.anchor = null; s.region = null; s.start = 0; s.dripping = false;
        }
        public bool RetainCorpse(ZombieBloodVfxPresenter owner, ZombieController actor, string cellId)
        {
            // Only an already bleeding, terminal actor can enter this presentation-only bounded lease.
            float expires = 0;
            foreach (var s in slots)
                if (s.owner == owner) expires = Mathf.Max(expires, s.start + s.region.spurtDuration + s.region.dripDuration + 1.5f);
            if (expires <= Time.time || !actor || !actor.IsDead) return false;
            foreach (var c in corpses) if (c.actor == actor) return true;
            var corpse = corpses[nextCorpse]; nextCorpse = (nextCorpse + 1) % corpses.Length;
            ReleaseCorpse(corpse); corpse.actor = actor; corpse.cellId = cellId; corpse.expires = expires;
            return true;
        }
        static void ReleaseCorpse(Corpse corpse)
        {
            var actor = corpse.actor; corpse.actor = null; corpse.cellId = null; corpse.expires = 0;
            if (!actor) return;
            try { actor.Shutdown(); }
            finally { if (actor) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); } }
        }
        public void ReleaseOwner(ZombieBloodVfxPresenter owner)
        {
            if (slots != null) foreach (var s in slots) if (s.owner == owner) Reset(s);
        }
        public void Play(ZombieBloodVfxPresenter owner, Transform anchor, ZombieSeverPresentation request)
        {
            if (!ZombieGorePreference.GraphicGoreEnabled || !owner || !anchor) return;
            var region = profile.ForPart(request.Part);
            if (region == null) return;
            if (request.Part == ZombieBodyPart.LeftArm || request.Part == ZombieBodyPart.RightArm)
            {
                var hand = request.Part == ZombieBodyPart.LeftArm ? ZombieBodyPart.LeftHand : ZombieBodyPart.RightHand;
                foreach (var old in slots) if (old.owner == owner && old.part == hand) Reset(old);
            }
            var s = slots[nextEffect]; nextEffect = (nextEffect + 1) % slots.Length;
            Reset(s); s.owner = owner; s.anchor = anchor; s.region = region; s.part = request.Part; s.start = Time.time;
            TotalRequests++;
            foreach (var particle in s.burst.particles)
            {
                var emission = particle.emission;
                if (!emission.enabled) continue;
                emission.SetBurst(0, new ParticleSystem.Burst(0, (short)region.burstCount));
                var shape = particle.shape; shape.angle = region.burstConeAngle;
            }
            Begin(s.burst, anchor.position, anchor.rotation, region.scale);
            Begin(s.spurt, anchor.position, anchor.rotation, region.scale);
            Rate(s.spurt, region.spurtRate);
            PlaceMarks(anchor.position, anchor.forward, region);
        }
        void LateUpdate()
        {
            if (slots == null) return;
            float now = Time.time;
            foreach (var s in slots)
            {
                if (!s.owner) { if (s.anchor || s.region != null) Reset(s); continue; }
                if (!s.anchor || !s.owner.isActiveAndEnabled) { Reset(s); continue; }
                float age = now - s.start;
                var r = s.region;
                if (age >= r.spurtDuration + r.dripDuration + 1.5f) { Reset(s); continue; }
                if (age < r.spurtDuration)
                {
                    s.spurt.root.transform.SetPositionAndRotation(s.anchor.position, s.anchor.rotation);
                    float pulse = .15f + .85f * Mathf.Pow(Mathf.Max(0, Mathf.Sin(age * 22 + Mathf.PI * .5f)), 2);
                    Rate(s.spurt, r.spurtRate * Mathf.Clamp01(profile.pressure.Evaluate(age / r.spurtDuration)) * pulse);
                }
                else
                {
                    Rate(s.spurt, 0);
                    if (!s.dripping && r.dripDuration > 0)
                    { Begin(s.drip, s.anchor.position, Quaternion.LookRotation(Vector3.down), r.scale); s.dripping = true; }
                    if (s.dripping)
                    {
                        s.drip.root.transform.position = s.anchor.position;
                        Rate(s.drip, age < r.spurtDuration + r.dripDuration ? r.dripRate : 0);
                    }
                }
            }
            foreach (var corpse in corpses)
                if (corpse.actor && now >= corpse.expires) ReleaseCorpse(corpse);
            foreach (var m in marks)
            {
                if (!m.root.activeSelf) continue;
                float age = now - m.start;
                if (age >= profile.markLifetime || !m.supportSurface || !m.supportSurface.enabled || !m.supportSurface.gameObject.activeInHierarchy)
                { m.root.SetActive(false); m.supportSurface = null; m.properties.Clear(); continue; }
                float growth = m.pool ? Mathf.Lerp(.2f, 1, Mathf.Clamp01(age / profile.poolGrowthDuration)) : 1;
                m.root.transform.localScale = Vector3.one * (m.size * growth);
                float fade = Mathf.Clamp01((profile.markLifetime - age) / 3);
                var color = Color.Lerp(new Color(.23f, .027f, .023f), new Color(.105f, .016f, .014f), age / profile.markLifetime);
                color.a = .8f * fade;
                m.properties.SetColor(ColorId, color);
                m.properties.SetFloat(SmoothnessId, Mathf.Lerp(.48f, .2f, age / profile.markLifetime));
                m.renderer.SetPropertyBlock(m.properties);
            }
        }
        void PlaceMarks(Vector3 origin, Vector3 forward, ZombieBloodVfxProfile.Region region)
        {
            Vector3 previous = new Vector3(float.PositiveInfinity, 0, 0);
            // One downward pool candidate, then a small fixed fan. Never particle collision callbacks.
            for (int i = 0; i < region.surfaceMarks; i++)
            {
                Vector3 direction = i == 0 ? Vector3.down : (forward + Vector3.down * (.3f + .35f * i)).normalized;
                if (!Physics.Raycast(origin, direction, out var hit, profile.probeDistance, profile.surfaceLayers, QueryTriggerInteraction.Ignore)) continue;
                if (hit.rigidbody || hit.collider.GetComponentInParent<ZombieHealth>() || hit.collider.GetComponentInParent<CharacterController>()) continue;
                var target = hit.collider.GetComponent<Renderer>();
                // Conservative: non-terrain colliders without an explicit opaque renderer are not eligible.
                if (!target && !(hit.collider is TerrainCollider)) continue;
                if (target)
                {
                    surfaceMaterials.Clear(); target.GetSharedMaterials(surfaceMaterials);
                    bool opaque = surfaceMaterials.Count > 0;
                    foreach (var material in surfaceMaterials) if (!material || material.renderQueue >= 3000) opaque = false;
                    if (!opaque) continue;
                }
                if ((hit.point - previous).sqrMagnitude < .04f) continue;
                float size = region.markSize * Random.Range(.85f, 1.15f) * (i == 0 ? 1.7f : 1);
                Quaternion rotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0, 0, Random.Range(0, 360));
                // Reject marks straddling edges; four extra short probes, all bounded.
                bool supported = true;
                for (int c = 0; c < 4; c++)
                {
                    var corner = hit.point + rotation * new Vector3((c % 2 == 0 ? -1 : 1) * size * .5f, (c < 2 ? -1 : 1) * size * .5f, 0);
                    if (!Physics.Raycast(corner + hit.normal * .04f, -hit.normal, out var edge, .08f, profile.surfaceLayers, QueryTriggerInteraction.Ignore) ||
                        edge.collider != hit.collider || Vector3.Dot(edge.normal, hit.normal) < .95f) { supported = false; break; }
                }
                if (!supported) continue;
                var m = marks[nextMark]; nextMark = (nextMark + 1) % marks.Length;
                m.root.SetActive(false); m.properties.Clear(); m.renderer.SetPropertyBlock(null);
                m.root.transform.SetPositionAndRotation(hit.point + hit.normal * .008f, rotation);
                m.supportSurface = hit.collider; m.start = Time.time; m.size = size; m.pool = i == 0 && hit.normal.y > .7f;
                m.root.transform.localScale = Vector3.one * size * (m.pool ? .2f : 1);
                m.root.SetActive(true); previous = hit.point;
            }
        }
        void GoreChanged(bool enabled) { if (!enabled) ClearAll(); }
        void SceneChanged(Scene previous, Scene current) { ClearAll(); }
        void ClearAll()
        {
            if (corpses != null) foreach (var c in corpses) if (c != null) ReleaseCorpse(c);
            if (slots != null) foreach (var s in slots) if (s != null) Reset(s);
            if (marks != null) foreach (var m in marks) if (m != null) { m.root.SetActive(false); m.supportSurface = null; m.properties.Clear(); if (m.renderer) m.renderer.SetPropertyBlock(null); }
        }
        void OnDisable() { ClearAll(); }
        void OnDestroy()
        {
            ClearAll();
            ZombieGorePreference.Changed -= GoreChanged;
            SceneManager.activeSceneChanged -= SceneChanged;
            if (instance == this) instance = null;
        }
    }
}
