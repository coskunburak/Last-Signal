using System;
using UnityEngine;
namespace LastSignal
{
    [DisallowMultipleComponent]
    public sealed class MeleeWeaponController : MonoBehaviour
    {
        [SerializeField] MeleeWeaponDefinition definition;
        [SerializeField] Transform meleeOrigin;
        PlayerInputReader input; PlayerHealth health; PlayerStamina stamina;
        Transform eye; GameObject player;
        readonly MeleeAttackResolver resolver = new MeleeAttackResolver();
        public MeleeAttackState Simulation { get; private set; }
        public MeleeWeaponDefinition Definition => definition;
        public string LastRejectionReason { get; private set; } = "None";
        public string LastMeleeResult => resolver.LastResult;
        public IDamageable LastMeleeTarget => resolver.LastTarget;
        Noise.GameplayNoiseSystem noise;
        Noise.GameplayNoiseTuning noiseTuning;
        ulong noiseSource;
        public void BindNoise(Noise.GameplayNoiseSystem authority, Noise.GameplayNoiseTuning tuning, ulong source)
        { noise = authority; noiseTuning = tuning; noiseSource = source; }
        public event Action AttackCommitted;
        public event Action ImpactCommitted;
        public bool Allowed => isActiveAndEnabled && player && player.activeInHierarchy && Time.timeScale > 0 &&
            (!input || input.GameplayActive) && (!health || health.IsAlive);
        public void Initialize(GameObject owner, Transform camera)
        {
            player=owner; eye=camera;
            stamina=owner.GetComponent<PlayerStamina>(); input=owner.GetComponent<PlayerInputReader>(); health=owner.GetComponent<PlayerHealth>();
            Simulation=new MeleeAttackState(definition);
            Simulation.ActiveWindow += Resolve;
        }
        public bool TryAttack()
        {
            if(!Allowed || !meleeOrigin || Simulation==null) { LastRejectionReason="Lifecycle"; return false; }
            if(Simulation.State!=MeleeState.Ready) { LastRejectionReason="Recovery"; return false; }
            if(!Simulation.TryBegin(stamina)) { LastRejectionReason="StaminaOrDefinition"; return false; }
            LastRejectionReason="None"; AttackCommitted?.Invoke(); return true;
        }
        void Update()
        {
            if(!Allowed) { Cancel(); return; }
            Simulation?.Tick(Time.deltaTime);
        }
        void Resolve()
        {
            if (Allowed && resolver.Resolve(eye, meleeOrigin, definition, player, Simulation.SwingId))
            {
                noise?.TryEmit(new Noise.GameplayNoiseRequest(noiseSource, resolver.LastImpactPosition,
                    Noise.GameplayNoiseCategory.MeleeImpact, noiseTuning.Melee, Simulation.SwingId), out _);
                ImpactCommitted?.Invoke();
            }
        }
        public void Cancel() => Simulation?.Cancel();
        void OnDisable() => Cancel();
        public bool HasValidAuthoring => definition && definition.Valid && meleeOrigin && GetComponentInChildren<Renderer>(true);
    }
}
