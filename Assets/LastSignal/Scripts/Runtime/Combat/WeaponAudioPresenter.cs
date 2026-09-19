using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Weapon audio presentation. Plays fire, dry-fire, reload sounds.
    /// Driven by WeaponController events. Does not create or destroy gameplay state.
    /// </summary>
    public sealed class WeaponAudioPresenter : MonoBehaviour
    {
        [SerializeField] WeaponController weapon;
        [SerializeField] AudioSource audioSource;

        [Header("Clips (placeholder — assign actual clips when available)")]
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip dryFireClip;
        [SerializeField] AudioClip reloadStartClip;
        [SerializeField] AudioClip reloadEndClip;
        [SerializeField] AudioClip magazineOutClip;
        [SerializeField] AudioClip magazineInClip;
        [SerializeField] AudioClip boltClip;
        [SerializeField] AudioClip equipClip;

        public void Configure(WeaponController ctrl, AudioSource source)
        {
            weapon = ctrl;
            audioSource = source;
        }

        void OnEnable()
        {
            if (!weapon) return;
            weapon.ShotFired += OnShotFired;
            weapon.ShotRejected += OnShotRejected;
            weapon.StateTransitioned += OnStateTransitioned;
            weapon.ReloadCommitted += OnReloadCommitted;
        }

        void OnDisable()
        {
            if (!weapon) return;
            weapon.ShotFired -= OnShotFired;
            weapon.ShotRejected -= OnShotRejected;
            weapon.StateTransitioned -= OnStateTransitioned;
            weapon.ReloadCommitted -= OnReloadCommitted;
        }

        void Play(AudioClip clip)
        {
            if (clip && audioSource) audioSource.PlayOneShot(clip);
        }

        void OnShotFired(WeaponFireResolver.ShotResult result) => Play(fireClip);
        void OnShotRejected() => Play(dryFireClip);
        void OnReloadCommitted() => Play(magazineInClip);

        void OnStateTransitioned(WeaponState from, WeaponState to)
        {
            switch (to)
            {
                case WeaponState.Equipping: Play(equipClip); break;
                case WeaponState.Reloading: Play(reloadStartClip); break;
                case WeaponState.Ready when from == WeaponState.Reloading: Play(reloadEndClip); break;
            }
        }
    }
}
