using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Camera recoil controller. Applies pitch/yaw camera kick on each shot
    /// and smoothly recovers. Separate from visual viewmodel recoil.
    /// </summary>
    public sealed class WeaponRecoilController : MonoBehaviour
    {
        [SerializeField] WeaponController weapon;
        [SerializeField] FirstPersonLook look;

        float recoilPitch; // Accumulated recoil to recover from.
        float recoilYaw;

        public void Configure(WeaponController ctrl, FirstPersonLook fpLook)
        {
            weapon = ctrl;
            look = fpLook;
        }

        void OnEnable()
        {
            if (weapon) weapon.ShotFired += OnShotFired;
        }

        void OnDisable()
        {
            if (weapon) weapon.ShotFired -= OnShotFired;
            recoilPitch = 0;
            recoilYaw = 0;
        }

        void Update()
        {
            if (!look || !weapon) return;
            float dt = Time.deltaTime;
            float recovery = weapon.Definition.RecoilRecoverySpeed * dt;

            // Recover recoil smoothly.
            if (Mathf.Abs(recoilPitch) > .001f || Mathf.Abs(recoilYaw) > .001f)
            {
                float pitchRecover = Mathf.MoveTowards(recoilPitch, 0, recovery);
                float yawRecover = Mathf.MoveTowards(recoilYaw, 0, recovery);
                float dPitch = recoilPitch - pitchRecover;
                float dYaw = recoilYaw - yawRecover;
                look.ApplyRecoil(dPitch, -dYaw);
                recoilPitch = pitchRecover;
                recoilYaw = yawRecover;
            }
        }

        void OnShotFired(WeaponFireResolver.ShotResult result)
        {
            if (!weapon || !look) return;
            var def = weapon.Definition;
            float aimMul = weapon.RuntimeState != null
                ? Mathf.Lerp(1f, def.AdsRecoilMultiplier, weapon.RuntimeState.AimAmount)
                : 1f;

            float vertical = def.RecoilVerticalDegrees * aimMul;
            float horizontal = Random.Range(-def.RecoilHorizontalRange, def.RecoilHorizontalRange) * aimMul;

            // Apply immediate kick.
                look.ApplyRecoil(-vertical, horizontal);
            recoilPitch += vertical;
            recoilYaw += horizontal;
        }
    }
}
