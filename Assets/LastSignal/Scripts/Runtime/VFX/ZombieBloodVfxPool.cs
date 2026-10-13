using UnityEngine;
using Unity.Profiling;
using UnityEngine.SceneManagement;

namespace LastSignal
{
    // Scene-local fixed arrays. All allocations and particle component discovery happen during warmup.
    public sealed class ZombieBloodVfxPool : MonoBehaviour
    {
        static readonly ProfilerMarker HitMarker = new ProfilerMarker("LastSignal.Combat.BloodHit");
        sealed class HitSlot
        {
            public Effect effect, bleed, drip;
            public ZombieBloodVfxPresenter owner;
            public ZombieWoundSurfaceResolver.Contact contact;
            public uint generation;
            public float expires, started, bleedSeconds, dripSeconds;
            public bool dripping;
        }
        HitSlot[] hits;
        int nextHit;
        public int TotalHitRequests { get; private set; }
        public Vector3 LastHitOrigin { get; private set; }
        public bool TryGetBleedOrigin(ZombieBloodVfxPresenter owner, out Vector3 point)
        {
            if (hits != null) foreach (var h in hits) if (h.owner == owner && h.bleed.root.activeSelf)
            { point = h.bleed.root.transform.position; return true; }
            point = default; return false;
        }
        public int ActiveBleeds { get { int n = 0; if (hits != null) foreach (var h in hits) if (h.owner && h.contact.Valid && Time.time < h.started + h.bleedSeconds + h.dripSeconds) n++; return n; } }
        public int ActiveHits { get { int n = 0; if (hits != null) foreach (var h in hits) if (h.owner) n++; return n; } }
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
            public ZombieBloodVfxPresenter owner;
            public Vector3 localPoint;
            public Quaternion localRotation;
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
            try { if (instance) instance.ClearAll(); ZombieLocalizedWounds.ClearSession(); ZombieDetachedPartPool.HideAll(); }
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
            hits = new HitSlot[Mathf.Clamp(profile.hitCapacity, 1, 16)];
            for (int i = 0; i < hits.Length; i++) hits[i] = new HitSlot { effect = Create(profile.burstPrefab), bleed = Create(profile.spurtPrefab), drip = Create(profile.dripPrefab) };
            corpses = new Corpse[profile.woundProfile ? profile.woundProfile.corpseCapacity : 8];
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
            foreach (var p in effect.particles)
            {
                var emission = p.emission;
                if (emission.enabled) emission.rateOverTime = rate;
            }
        }
        void Reset(Slot s)
        {
            Stop(s.burst); Stop(s.spurt); Stop(s.drip);
            s.owner = null; s.anchor = null; s.region = null; s.start = 0; s.dripping = false;
        }
        public bool RetainCorpse(ZombieBloodVfxPresenter owner, ZombieController actor, string cellId)
        {
            if (!actor || !actor.IsDead || !owner) return false;
            foreach (var c in corpses) if (c.actor == actor) { if (!string.IsNullOrEmpty(cellId)) c.cellId = cellId; return true; }
            float expires = Time.time + (profile.woundProfile ? profile.woundProfile.corpseSeconds : 30);
            var corpse = corpses[nextCorpse]; nextCorpse = (nextCorpse + 1) % corpses.Length;
            ReleaseCorpse(corpse); corpse.actor = actor; corpse.cellId = cellId; corpse.expires = expires;
            actor.GetComponent<ZombieCorpseImpactReceiver>()?.Retain(expires);
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
            // Actor reset/reuse invalidates its old lease without destroying the new lifetime.
            if (corpses != null) foreach (var c in corpses) if (c.actor && c.actor.gameObject == owner.gameObject)
            { c.actor = null; c.cellId = null; c.expires = 0; }
            if (slots != null) foreach (var s in slots) if (s.owner == owner) Reset(s);
            if (hits != null) foreach (var h in hits) if (h.owner == owner) ResetHit(h);
            if (marks != null) foreach (var m in marks) if (m.owner == owner) { m.root.SetActive(false); m.owner = null; m.supportSurface = null; }
        }
        public void PlayHit(ZombieBloodVfxPresenter owner, DamageReceipt receipt, ZombieWoundSurfaceResolver.Contact contact = default)
        {
            if (!owner || !receipt.PresentationAccepted || !ZombieGorePreference.GraphicGoreEnabled) return;
            var camera = Camera.main;
            if (camera && (receipt.Hit.HitPoint - camera.transform.position).sqrMagnitude > profile.hitDistance * profile.hitDistance) return;
            using (HitMarker.Auto())
            {
                var h = hits[nextHit]; nextHit = (nextHit + 1) % hits.Length;
                ResetHit(h); h.owner = owner; h.started = Time.time; h.contact = contact;
                h.generation = owner.GetComponent<ZombieLocalizedWounds>()?.Generation ?? 0;
                h.bleedSeconds = (profile.woundProfile ? profile.woundProfile.bleedSeconds : .8f) * (receipt.CorpseImpact ? .45f : 1);
                h.dripSeconds = profile.woundProfile ? profile.woundProfile.dripSeconds : 1.2f;
                h.expires = Time.time + h.bleedSeconds + h.dripSeconds + 1.5f; TotalHitRequests++;
                bool critical = receipt.Hit.Region == DamageRegion.Head;
                float severity = receipt.Terminal || critical || receipt.Severity != ZombieImpactSeverity.Light ? 1.45f : 1;
                foreach (var p in h.effect.particles)
                {
                    var emission = p.emission;
                    if (!emission.enabled) continue;
                    emission.rateOverTime = 0; emission.burstCount = 1;
                    emission.SetBurst(0, new ParticleSystem.Burst(0, (short)Mathf.Clamp(Mathf.RoundToInt(profile.hitParticles * severity), 1, 12)));
                    var main = p.main; main.startSpeed = 1.8f * severity; main.startLifetime = .3f;
                    var shape = p.shape; shape.angle = critical ? 24 : 38;
                }
                Vector3 origin = contact.Valid ? contact.position : receipt.Hit.HitPoint;
                Vector3 outward = contact.Valid ? contact.normal : receipt.Hit.HitNormal;
                Vector3 direction = (outward * .75f - receipt.Hit.Direction.normalized * .25f).normalized;
                if (direction.sqrMagnitude < .001f) direction = Vector3.up;
                LastHitOrigin = origin;
                Begin(h.effect, origin + direction * .004f, Quaternion.LookRotation(direction), profile.hitScale * severity * CombatPresentationPreference.BloodIntensity);
                if (contact.Valid && !CombatPresentationPreference.ReducedEffects)
                {
                    foreach (var p in h.bleed.particles)
                    {
                        if (!p.emission.enabled) continue;
                        var main = p.main; main.startSpeed = .35f; main.startLifetime = .25f;
                        var shape = p.shape; shape.angle = 16;
                        var emission = p.emission; emission.burstCount = 0;
                    }
                    Rate(h.bleed, 8); Begin(h.bleed, origin, Quaternion.LookRotation(direction), profile.hitScale * CombatPresentationPreference.BloodIntensity);
                }
                else { h.bleedSeconds = h.dripSeconds = 0; }
                if (!CombatPresentationPreference.ReducedEffects) PlaceMarks(origin + direction * .01f, direction, profile.hand, owner);
            }
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
                var main = particle.main; main.startSpeed = region.burstSpeed;
            }
            Begin(s.burst, anchor.position, anchor.rotation, region.scale * CombatPresentationPreference.BloodIntensity);
            foreach (var particle in s.spurt.particles)
            {
                if (!particle.emission.enabled) continue;
                var main = particle.main; main.startSpeed = region.spurtSpeed;
                var shape = particle.shape; shape.angle = region.spurtConeAngle;
            }
            Rate(s.spurt, region.spurtRate);
            Begin(s.spurt, anchor.position, anchor.rotation, region.scale * CombatPresentationPreference.BloodIntensity);
            PlaceMarks(anchor.position, anchor.forward, region, owner);
        }
        void LateUpdate()
        {
            if (slots == null) return;
            float now = Time.time;
            if (hits != null) foreach (var h in hits)
            {
                if (h.expires <= 0) continue;
                var wounds = h.owner ? h.owner.GetComponent<ZombieLocalizedWounds>() : null;
                if (!h.owner || !h.owner.isActiveAndEnabled || now >= h.expires || wounds && wounds.Generation != h.generation)
                { ResetHit(h); continue; }
                float age = now - h.started;
                if (h.contact.Evaluate(out var point, out var normal) && !CombatPresentationPreference.ReducedEffects)
                {
                    h.bleed.root.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
                    Rate(h.bleed, age < h.bleedSeconds ? 8 * (1 - age / h.bleedSeconds) : 0);
                    if (age >= h.bleedSeconds && !h.dripping && h.dripSeconds > 0)
                    {
                        foreach (var p in h.drip.particles) { if (!p.emission.enabled) continue; var e = p.emission; e.burstCount = 0; var m = p.main; m.startLifetime = .45f; m.startSpeed = .2f; }
                        Rate(h.drip, 3); Begin(h.drip, point, Quaternion.LookRotation(Vector3.down), profile.hitScale * CombatPresentationPreference.BloodIntensity); h.dripping = true;
                        PlaceMarks(point + normal * .015f, Vector3.down, profile.hand, h.owner);
                    }
                    h.drip.root.transform.position = point;
                    Rate(h.drip, age < h.bleedSeconds + h.dripSeconds ? 3 : 0);
                }
                else { Rate(h.bleed, 0); Rate(h.drip, 0); }
            }
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
                    float pressure = profile.PressureAt(r, age);
                    float pulse = ZombieBloodVfxProfile.PulseAt(r, age);
                    Rate(s.spurt, r.spurtRate * pressure * pulse);
                    foreach (var particle in s.spurt.particles)
                    {
                        if (!particle.emission.enabled) continue;
                        var main = particle.main;
                        main.startSpeed = r.spurtSpeed * Mathf.Lerp(r.terminalSpeedFraction, 1, pressure) *
                            Mathf.Lerp(1, Mathf.Lerp(.65f, 1, pulse), 1 - r.terminalSpeedFraction);
                    }
                }
                else
                {
                    Rate(s.spurt, 0);
                    if (!s.dripping && r.dripDuration > 0)
                    { Rate(s.drip, r.dripRate); Begin(s.drip, s.anchor.position, Quaternion.LookRotation(Vector3.down), r.scale * CombatPresentationPreference.BloodIntensity); s.dripping = true; }
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
                m.root.transform.SetPositionAndRotation(m.supportSurface.transform.TransformPoint(m.localPoint),
                    m.supportSurface.transform.rotation * m.localRotation);
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
        void PlaceMarks(Vector3 origin, Vector3 forward, ZombieBloodVfxProfile.Region region, ZombieBloodVfxPresenter owner)
        {
            Vector3 previous = new Vector3(float.PositiveInfinity, 0, 0);
            // One downward pool candidate, then a small fixed fan. Never particle collision callbacks.
            for (int i = 0; i < region.surfaceMarks; i++)
            {
                Vector3 direction = i == 0 ? Vector3.down : (forward + Vector3.down * (.3f + .35f * i)).normalized;
                if (!Physics.Raycast(origin, direction, out var hit, profile.probeDistance, (profile.surfaceLayers.value | (1 << 8) | (1 << 2)), QueryTriggerInteraction.Ignore)) continue;
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
                m.supportSurface = hit.collider; m.owner = owner;
                m.localPoint = hit.collider.transform.InverseTransformPoint(hit.point + hit.normal * .008f);
                m.localRotation = Quaternion.Inverse(hit.collider.transform.rotation) * rotation;
                m.start = Time.time; m.size = size; m.pool = i == 0 && hit.normal.y > .7f;
                m.root.transform.localScale = Vector3.one * size * (m.pool ? .2f : 1);
                m.root.SetActive(true); previous = hit.point;
            }
        }
        void GoreChanged(bool enabled) { if (!enabled) ClearAll(false); }
        void SceneChanged(Scene previous, Scene current) { ClearAll(); }
        void ResetHit(HitSlot h)
        {
            Stop(h.effect); Stop(h.bleed); Stop(h.drip); h.owner = null; h.contact = default; h.expires = 0; h.dripping = false;
        }
        void ClearAll(bool releaseCorpses = true)
        {
            if (hits != null) foreach (var h in hits) if (h != null) ResetHit(h);
            if (releaseCorpses && corpses != null) foreach (var c in corpses) if (c != null) ReleaseCorpse(c);
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
