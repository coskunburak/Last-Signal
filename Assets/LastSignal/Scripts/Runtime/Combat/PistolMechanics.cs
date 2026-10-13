using UnityEngine;
namespace LastSignal
{
    /// <summary>Rigid source parts. Only accepted shots and the authoritative reload clock drive motion.</summary>
    [DefaultExecutionOrder(400)]
    public sealed class PistolMechanics : MonoBehaviour
    {
        public Transform Slide, Trigger, Hammer, EjectionPort;
        public Vector3 SlideTravel = new Vector3(0, 0, -.025f);
        public float TriggerDegrees = -12, HammerDegrees = 18;
        WeaponController weapon;
        Vector3 slideRest;
        Quaternion triggerRest, hammerRest;
        float shotSeconds;
        bool captured;
        void Awake() => Capture();
        void Capture()
        {
            if (captured) return; captured = true;
            if (Slide) slideRest = Slide.localPosition;
            if (Trigger) triggerRest = Trigger.localRotation;
            if (Hammer) hammerRest = Hammer.localRotation;
        }
        public void Bind(WeaponController owner)
        {
            Unbind(); Capture(); weapon = owner;
            if (isActiveAndEnabled && weapon) weapon.ShotFired += Shot;
        }
        void OnEnable() { if (weapon) weapon.ShotFired += Shot; }
        void Unbind() { if (weapon) weapon.ShotFired -= Shot; }
        void OnDisable() { Unbind(); ResetParts(); }
        void Shot(WeaponFireResolver.ShotResult shot) => shotSeconds = .09f;
        void ResetParts()
        {
            shotSeconds = 0;
            if (!captured) return;
            if (Slide) Slide.localPosition = slideRest;
            if (Trigger) Trigger.localRotation = triggerRest;
            if (Hammer) Hammer.localRotation = hammerRest;
        }
        void LateUpdate()
        {
            if (!weapon || weapon.RuntimeState == null || !weapon.isActiveAndEnabled)
            { ResetParts(); return; }
            var state = weapon.RuntimeState;
            if (state.State == WeaponState.Holstered || state.State == WeaponState.Unequipping) { ResetParts(); return; }
            if (weapon.IsSimulationActive) shotSeconds = Mathf.Max(0, shotSeconds - Time.deltaTime);
            float pull = Mathf.Sin(Mathf.Clamp01(shotSeconds / .09f) * Mathf.PI);
            // No inferred last-round hold-open: current combat has a round pool, not a chamber/slide state.
            if (state.State == WeaponState.Reloading && state.IsEmptyReload && state.ReloadCommitted)
            {
                float t = state.StateTimer / state.Definition.EmptyReloadSeconds;
                pull = Mathf.Max(pull, Mathf.Sin(Mathf.InverseLerp(.80f, .96f, t) * Mathf.PI));
            }
            if (Slide) Slide.localPosition = slideRest + SlideTravel * pull;
            if (Trigger) Trigger.localRotation = triggerRest * Quaternion.Euler(TriggerDegrees * pull, 0, 0);
            if (Hammer) Hammer.localRotation = hammerRest * Quaternion.Euler(HammerDegrees * pull, 0, 0);
        }
    }
}
