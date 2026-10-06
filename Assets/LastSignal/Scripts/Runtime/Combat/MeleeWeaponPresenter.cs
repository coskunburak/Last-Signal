using UnityEngine;
namespace LastSignal
{
    /// <summary>Read-only animation/audio adapter. It never dispatches damage.</summary>
    public sealed class MeleeWeaponPresenter : MonoBehaviour
    {
        [SerializeField] MeleeWeaponController controller;
        [SerializeField] Animator animator;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip swing, impact;
        MeleeState previous;
        void OnEnable()
        {
            if(controller) { controller.AttackCommitted += OnSwing; controller.ImpactCommitted += OnImpact; }
            previous=MeleeState.Ready;
            if(animator) animator.Play("Equip",0,0);
        }
        void OnDisable()
        {
            if(controller) { controller.AttackCommitted -= OnSwing; controller.ImpactCommitted -= OnImpact; }
            if(audioSource) audioSource.Stop();
        }
        void OnSwing()
        {
            // Record the accepted attack synchronously. It can be cancelled before
            // our next Update; polling alone would miss Windup and leave Swing playing.
            previous = MeleeState.Windup;
            if(animator) animator.Play("Swing",0,0);
            if(audioSource && swing) audioSource.PlayOneShot(swing,.45f);
        }
        void OnImpact() { if(audioSource && impact) audioSource.PlayOneShot(impact,.6f); }
        void Update()
        {
            if(!controller || controller.Simulation==null) return;
            var state=controller.Simulation.State;
            if(state==MeleeState.Ready && previous!=state && animator) animator.Play("Idle",0,0);
            previous=state;
        }
    }
}
