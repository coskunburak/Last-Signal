using System;
using LastSignal.Persistence;
using UnityEngine;

namespace LastSignal
{
    // Presentation preference only. Logical sever, health, attack legality and hearing never read it.
    public static class ZombieGorePreference
    {
        const string Key = "LastSignal.ReducedZombieGore";
        public static bool GraphicGoreEnabled => PlayerPrefs.GetInt(Key, 0) == 0;
        public static event Action<bool> Changed;
        public static void SetGraphicGoreEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(Key, enabled ? 0 : 1);
            if (!enabled) ZombieDetachedPartPool.HideAll();
            Changed?.Invoke(enabled);
        }
    }

    [DisallowMultipleComponent, RequireComponent(typeof(ZombieHealth))]
    public sealed class ZombieDismemberment : MonoBehaviour
    {
        [Serializable]
        public sealed class PartBinding
        {
            public ZombieBodyPart part;
            [Min(.01f)] public float severThreshold = 100;
            public bool fatalOnSever;
            public SkinnedMeshRenderer[] attachedRenderers;
            public Collider[] hitColliders;
            public Transform detachAnchor;
            public Vector3 detachedColliderCenter;
            public Vector3 detachedColliderSize = new Vector3(.25f, .4f, .25f);
            [Min(.1f)] public float impulse = 2;
        }

        [SerializeField] PartBinding[] parts;
        [SerializeField, Min(.01f)] float torsoDamageThreshold = 40;
        [SerializeField] GameObject torsoWoundVisual;
        readonly float[] regionalDamage = new float[9];
        public event Action<ZombieSeverPresentation> SeverPresented;
        public event Action PresentationReset;
        int severedMask;
        float torsoDamage;
        public int SeveredPartsMask => severedMask;
        public bool TorsoDamaged => torsoDamage >= torsoDamageThreshold;
        public bool CanUseRightArmAttack => !IsSevered(ZombieBodyPart.RightArm) && !IsSevered(ZombieBodyPart.RightHand);
        public ZombieAnatomySnapshot CaptureState() => new ZombieAnatomySnapshot
        {
            severedMask = severedMask, torsoDamage = torsoDamage,
            regionalDamage = (float[])regionalDamage.Clone()
        };

        public void Configure(PartBinding[] bindings, float torsoThreshold, GameObject woundVisual = null)
        { parts = bindings; torsoDamageThreshold = torsoThreshold; torsoWoundVisual = woundVisual; ResetState(); }

        void OnEnable()
        {
            ZombieGorePreference.Changed += OnGoreChanged;
            ResetState();
        }
        void OnDisable() => ZombieGorePreference.Changed -= OnGoreChanged;
        void OnGoreChanged(bool enabled)
        {
            if (torsoWoundVisual) torsoWoundVisual.SetActive(enabled && TorsoDamaged);
        }

        public void ResetState()
        {
            try { PresentationReset?.Invoke(); } catch (Exception e) { Debug.LogException(e, this); }
            severedMask = 0; torsoDamage = 0;
            Array.Clear(regionalDamage, 0, regionalDamage.Length);
            if (parts != null)
                foreach (var binding in parts)
                {
                    if (binding == null) continue;
                    SetVisible(binding, true);
                }
            if (torsoWoundVisual) torsoWoundVisual.SetActive(false);
        }

        public bool IsSevered(ZombieBodyPart part) => part != ZombieBodyPart.Unspecified &&
            (severedMask & (1 << (int)part)) != 0;

        // Called once by the existing ZombieHealth transaction, before its health commit.
        // Returns only the fatal consequence; ZombieHealth remains the death authority.
        internal bool ResolveCommittedHit(DamageInfo hit)
        {
            if (hit.BodyPart == ZombieBodyPart.Torso)
            {
                torsoDamage = Mathf.Min(torsoDamageThreshold, torsoDamage + hit.Amount);
                if (TorsoDamaged && torsoWoundVisual && ZombieGorePreference.GraphicGoreEnabled)
                    torsoWoundVisual.SetActive(true);
                return false;
            }
            var binding = Find(hit.BodyPart);
            if (binding == null || IsSevered(hit.BodyPart) ||
                !float.IsFinite(binding.severThreshold) || binding.severThreshold <= 0) return false;
            int index = (int)hit.BodyPart;
            regionalDamage[index] = Mathf.Min(binding.severThreshold, regionalDamage[index] + hit.Amount);
            if (regionalDamage[index] < binding.severThreshold) return false;
            severedMask |= 1 << index;
            if (ZombieGorePreference.GraphicGoreEnabled)
                ZombieDetachedPartPool.Spawn(binding.attachedRenderers, binding.detachAnchor,
                    binding.detachedColliderCenter, binding.detachedColliderSize,
                    hit.Direction, binding.impulse);
            SetVisible(binding, false);
            if (hit.BodyPart == ZombieBodyPart.LeftArm) MarkDependentHand(ZombieBodyPart.LeftHand);
            if (hit.BodyPart == ZombieBodyPart.RightArm) MarkDependentHand(ZombieBodyPart.RightHand);
            // Presentation failure cannot interrupt the health transaction. RestoreState never emits this event.
            if (ZombieGorePreference.GraphicGoreEnabled)
                try { SeverPresented?.Invoke(new ZombieSeverPresentation(hit.BodyPart,
                    binding.detachAnchor ? binding.detachAnchor.position : hit.HitPoint, hit.Direction)); }
                catch (Exception e) { Debug.LogException(e, this); }
            return binding.fatalOnSever;
        }

        void MarkDependentHand(ZombieBodyPart hand)
        {
            severedMask |= 1 << (int)hand;
            var binding = Find(hand);
            if (binding != null) SetVisible(binding, false);
        }

        public void RestoreState(ZombieAnatomySnapshot state)
        {
            ResetState();
            if (state == null) return;
            if (!SaveValidation.ValidZombieAnatomy(state))
                throw new ArgumentException("Invalid zombie anatomy snapshot.", nameof(state));
            torsoDamage = state.torsoDamage;
            Array.Copy(state.regionalDamage, regionalDamage, regionalDamage.Length);
            if (TorsoDamaged && torsoWoundVisual && ZombieGorePreference.GraphicGoreEnabled)
                torsoWoundVisual.SetActive(true);
            if (parts == null) return;
            foreach (var binding in parts)
            {
                if (binding == null || (state.severedMask & (1 << (int)binding.part)) == 0) continue;
                severedMask |= 1 << (int)binding.part;
                SetVisible(binding, false);
            }
        }

        PartBinding Find(ZombieBodyPart id)
        {
            if (parts == null || id == ZombieBodyPart.Unspecified) return null;
            foreach (var binding in parts)
                if (binding != null && binding.part == id) return binding;
            return null;
        }

        static void SetVisible(PartBinding binding, bool visible)
        {
            if (binding.attachedRenderers != null)
                foreach (var renderer in binding.attachedRenderers) if (renderer) renderer.enabled = visible;
            if (binding.hitColliders != null)
                foreach (var collider in binding.hitColliders) if (collider) collider.enabled = visible;
        }

#if UNITY_EDITOR
        [ContextMenu("Validate Dismemberment Bindings")]
        void ValidateBindings()
        {
            int mask = 0;
            if (parts == null || parts.Length == 0) { Debug.LogError("Missing sever bindings", this); return; }
            foreach (var binding in parts)
            {
                if (binding == null || binding.part == ZombieBodyPart.Unspecified ||
                    (mask & (1 << (int)binding.part)) != 0 || binding.attachedRenderers == null ||
                    binding.attachedRenderers.Length == 0 || binding.hitColliders == null ||
                    binding.hitColliders.Length == 0 || !binding.detachAnchor ||
                    !float.IsFinite(binding.severThreshold) || binding.severThreshold <= 0)
                { Debug.LogError("Invalid or duplicate sever binding", this); return; }
                mask |= 1 << (int)binding.part;
            }
            Debug.Log("Zombie sever bindings structurally valid. Visual cut surfaces require manual acceptance.", this);
        }
#endif
    }
}
