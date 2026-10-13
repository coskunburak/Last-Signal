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

        [Header("Fallback clips for scenes without ProductionAudio")]
        [SerializeField] AudioClip fireClip;
        [SerializeField] AudioClip dryFireClip;
        [SerializeField] AudioClip reloadStartClip;
        [SerializeField] AudioClip reloadEndClip;
        [SerializeField] AudioClip magazineOutClip;
        [SerializeField] AudioClip magazineInClip;
        [SerializeField] AudioClip boltClip;
        [SerializeField] AudioClip equipClip;

        WeaponController subscribedWeapon;
        Audio.ProductionAudio director;
        bool detached, attached, bolted;
        float lastDryFire=-10;
        public int PresentedCueCount { get; private set; }

        public void Configure(WeaponController ctrl, AudioSource source)
        {
            Unsubscribe();
            weapon = ctrl;
            audioSource = source;
            if (isActiveAndEnabled) Subscribe();
        }

        void OnEnable() => Subscribe();
        void Subscribe()
        {
            Unsubscribe();
            if (!weapon) return;
            subscribedWeapon = weapon;
            weapon.ShotFired += OnShotFired;
            weapon.ShotRejected += OnShotRejected;
            weapon.StateTransitioned += OnStateTransitioned;
            weapon.ReloadCommitted += OnReloadCommitted;
        }

        void OnDisable()
        {
            Unsubscribe();
            if(audioSource) audioSource.Stop();
            if(director) director.StopOwner(transform);
        }
        void Unsubscribe()
        {
            if (!subscribedWeapon) return;
            subscribedWeapon.ShotFired -= OnShotFired;
            subscribedWeapon.ShotRejected -= OnShotRejected;
            subscribedWeapon.StateTransitioned -= OnStateTransitioned;
            subscribedWeapon.ReloadCommitted -= OnReloadCommitted;
            subscribedWeapon=null;
        }

        void Play(Audio.AudioCue cue, AudioClip fallback, float gain=1)
        {
            // A handgun must not inherit the global rifle transient or magazine foley.
            if (weapon && weapon.Definition && weapon.Definition.Category == WeaponCategory.Pistol && fallback && audioSource)
            { audioSource.PlayOneShot(fallback, gain); PresentedCueCount++; return; }
            if(!director) director=GetComponentInParent<Noise.GameplayNoiseContext>()?.Session?.GetComponent<Audio.ProductionAudio>();
            if(director) { if(director.Play(cue,transform,gain)) PresentedCueCount++; }
            else if(fallback && audioSource) { audioSource.PlayOneShot(fallback,gain); PresentedCueCount++; }
        }
        void OnShotFired(WeaponFireResolver.ShotResult result)
        {
            Play(Audio.AudioCue.Fire, fireClip);
            if (!director) director=GetComponentInParent<Noise.GameplayNoiseContext>()?.Session?.GetComponent<Audio.ProductionAudio>();
            if (!director || !result.Hit) return;
            bool organic = ImpactSurface.Organic(result.Surface);
            if (organic && !result.ImpactAccepted) return;
            var cue = organic ? result.Receipt.Severed && ZombieGorePreference.GraphicGoreEnabled ? Audio.AudioCue.ImpactSever :
                result.Surface == ImpactSurfaceKind.Bone ? Audio.AudioCue.ImpactBone : Audio.AudioCue.ImpactFlesh :
                result.Surface == ImpactSurfaceKind.Metal ? Audio.AudioCue.ImpactMetal :
                result.Surface == ImpactSurfaceKind.Stone ? Audio.AudioCue.ImpactStone :
                result.Surface == ImpactSurfaceKind.Wood ? Audio.AudioCue.ImpactWood :
                result.Surface == ImpactSurfaceKind.Soil ? Audio.AudioCue.ImpactSoil : Audio.AudioCue.ImpactDefault;
            if (director.PlayAt(cue, transform, result.HitPoint, result.Receipt.Terminal ? 1.1f : 1)) PresentedCueCount++;
        }
        void OnShotRejected()
        {
            if(Time.time-lastDryFire<.2f) return;
            lastDryFire=Time.time; Play(Audio.AudioCue.DryFire,dryFireClip);
        }
        void OnReloadCommitted() => OnMagazineAttach();
        public void OnMagazineDetach()
        {
            if(!Reloading || detached) return;
            detached=true; Play(Audio.AudioCue.MagazineOut,magazineOutClip);
        }
        public void OnMagazineAttach()
        {
            if(!Reloading || attached || !weapon.RuntimeState.ReloadCommitted) return;
            attached=true; Play(Audio.AudioCue.MagazineIn,magazineInClip);
        }
        public void OnBoltAction()
        {
            if(!Reloading || bolted || !weapon.RuntimeState.IsEmptyReload || !weapon.RuntimeState.ReloadCommitted) return;
            bolted=true; Play(Audio.AudioCue.Bolt,boltClip);
        }
        bool Reloading => weapon && weapon.IsSimulationActive && weapon.RuntimeState!=null && weapon.RuntimeState.State==WeaponState.Reloading;
        void Update()
        {
            if(!Reloading) return;
            var state=weapon.RuntimeState;
            float duration=state.IsEmptyReload?state.Definition.EmptyReloadSeconds:state.Definition.TacticalReloadSeconds;
            // VAL has no imported animation events. Sample the authoritative reload clock;
            // optional animation receivers share the same one-shot guards.
            if(state.StateTimer>=duration*.16f) OnMagazineDetach();
            if(state.StateTimer>=duration*.86f) OnBoltAction();
        }
        void OnStateTransitioned(WeaponState from, WeaponState to)
        {
            if(to==WeaponState.Reloading)
            {
                detached=attached=bolted=false;
                Play(Audio.AudioCue.ReloadStart,reloadStartClip);
            }
            else if(to==WeaponState.Equipping) Play(Audio.AudioCue.Equip,equipClip);
            else if(to==WeaponState.Holstered || to==WeaponState.Unequipping)
            {
                if(audioSource) audioSource.Stop();
                if(director) director.StopOwner(transform);
            }
        }
    }
}
