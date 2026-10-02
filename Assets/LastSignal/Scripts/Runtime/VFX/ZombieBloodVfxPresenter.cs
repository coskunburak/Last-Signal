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
        ZombieBloodVfxPool pool;
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
            return ZombieGorePreference.GraphicGoreEnabled && pool && actor && actor.IsDead && pool.RetainCorpse(this, actor, cellId);
        }
        void Awake() { sever = GetComponent<ZombieDismemberment>(); }
        void OnEnable()
        {
            sever.SeverPresented += OnSever;
            sever.PresentationReset += Clear;
            if (Application.isPlaying && profile && profile.IsValid) pool = ZombieBloodVfxPool.Warm(profile);
        }
        void OnDisable() { sever.SeverPresented -= OnSever; sever.PresentationReset -= Clear; Clear(); }
        void Clear() { if (pool) pool.ReleaseOwner(this); }
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
