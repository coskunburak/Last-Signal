using System;
using UnityEngine;

namespace LastSignal
{
    public readonly struct ZombieSeverPresentation
    {
        public readonly ZombieBodyPart Part;
        public readonly Vector3 Position, Impulse;
        public ZombieSeverPresentation(ZombieBodyPart part, Vector3 position, Vector3 impulse)
        { Part = part; Position = position; Impulse = impulse; }
    }

    [DisallowMultipleComponent, RequireComponent(typeof(ZombieDismemberment))]
    public sealed class ZombieBloodVfxPresenter : MonoBehaviour
    {
        [Serializable] public struct AnchorBinding { public ZombieBodyPart part; public Transform anchor; }
        [SerializeField] ZombieBloodVfxProfile profile;
        [SerializeField] AnchorBinding[] anchors;
        ZombieDismemberment sever;
        ZombieHealth health;
        ZombieLocalizedWounds wounds;
        ZombieBloodVfxPool pool;
        ZombieCorpseImpactReceiver corpse;
        bool retained;
        readonly ulong[] presentedShots = new ulong[32]; readonly int[] presentedSubHits = new int[32]; int nextPresentation;
        public ZombieBloodVfxProfile Profile => profile;
        public void Configure(ZombieBloodVfxProfile value, AnchorBinding[] bindings) { Clear(); profile = value; anchors = bindings; if (Application.isPlaying && isActiveAndEnabled) pool = ZombieBloodVfxPool.Warm(profile); }
        public Transform AnchorFor(ZombieBodyPart part)
        {
            if (anchors != null) for (int i = 0; i < anchors.Length; i++)
                if (anchors[i].part == part) return anchors[i].anchor;
            return null;
        }
        public bool RetainDeathPresentation(ZombieController actor, string cellId)
        {
            if (!pool && Application.isPlaying) pool = ZombieBloodVfxPool.Warm(profile);
            retained = pool && actor && actor.IsDead && pool.RetainCorpse(this, actor, cellId);
            return retained;
        }
        void Awake()
        {
            sever = GetComponent<ZombieDismemberment>(); health = GetComponent<ZombieHealth>();
            if (profile && profile.woundMaterial)
            {
                wounds = GetComponent<ZombieLocalizedWounds>();
                if (!wounds) wounds = gameObject.AddComponent<ZombieLocalizedWounds>();
                wounds.Initialize(profile.woundMaterial, profile.hitDistance, profile.woundProfile);
            }
            corpse = GetComponent<ZombieCorpseImpactReceiver>();
            if (!corpse) corpse = gameObject.AddComponent<ZombieCorpseImpactReceiver>();
            corpse.Initialize(health, wounds, this);
        }
        void LateUpdate()
        {
            // Encounter actors use the same bounded lease as population actors.
            if (!retained && health && !health.IsAlive) RetainDeathPresentation(GetComponent<ZombieController>(), null);
        }
        void OnEnable()
        {
            sever.SeverPresented += OnSever;
            if (health) { health.DamageCommitted += OnHit; health.StateRestored += OnHealthRestored; }
            sever.PresentationReset += Clear;
            if (Application.isPlaying && profile && profile.IsValid) pool = ZombieBloodVfxPool.Warm(profile);
        }
        void OnDisable() { if (health) { health.DamageCommitted -= OnHit; health.StateRestored -= OnHealthRestored; } sever.SeverPresented -= OnSever; sever.PresentationReset -= Clear; Clear(); }
        void OnHealthRestored()
        {
            retained = false; if (corpse) corpse.Clear(); if (pool) pool.ReleaseOwner(this);
            // Anatomy may already have hydrated persistent wounds; do not erase them or replay effects.
        }
        void Clear() { Array.Clear(presentedShots, 0, presentedShots.Length); Array.Clear(presentedSubHits, 0, presentedSubHits.Length); nextPresentation = 0; retained = false; if (corpse) corpse.Clear(); if (pool) pool.ReleaseOwner(this); if (wounds) wounds.Clear(); }
        void OnHit(DamageReceipt receipt) => PresentImpact(receipt);
        public void PresentImpact(DamageReceipt receipt)
        {
            if (receipt.Target != health || receipt.Generation != health.LifetimeGeneration || !receipt.PresentationAccepted || receipt.Hit.Category != DamageCategory.Bullet || !ZombieGorePreference.GraphicGoreEnabled) return;
            if (!pool && Application.isPlaying) pool = ZombieBloodVfxPool.Warm(profile);
            if (receipt.Hit.ShotId != 0)
            {
                for (int i = 0; i < presentedShots.Length; i++) if (presentedShots[i] == receipt.Hit.ShotId && presentedSubHits[i] == receipt.Hit.SubHitId) return;
                presentedShots[nextPresentation] = receipt.Hit.ShotId; presentedSubHits[nextPresentation] = receipt.Hit.SubHitId;
                nextPresentation = (nextPresentation + 1) % presentedShots.Length;
            }
            // An approved sever already owns the larger effect. Gated cuts still get a localized hit.
            bool visibleSever = receipt.Severed && sever.IsVisualSeverApproved(receipt.Hit.BodyPart);
            if (visibleSever) return;
            ZombieWoundSurfaceResolver.Contact contact = default;
            // Surface failures never suppress the immediate accepted-hit blood burst.
            try { if (wounds) wounds.Present(receipt, out contact); }
            catch (Exception e) { Debug.LogException(e, this); }
            if (pool) pool.PlayHit(this, receipt, contact);
        }
        void OnSever(ZombieSeverPresentation request)
        {
            if (!ZombieGorePreference.GraphicGoreEnabled) return;
            if (!pool && Application.isPlaying) pool = ZombieBloodVfxPool.Warm(profile);
            if (!pool) return;
            var anchor = AnchorFor(request.Part);
            if (anchor) pool.Play(this, anchor, request);
        }
    }
}
