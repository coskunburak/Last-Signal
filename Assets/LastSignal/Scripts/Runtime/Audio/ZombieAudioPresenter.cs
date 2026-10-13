using UnityEngine;
namespace LastSignal.Audio
{
    [DisallowMultipleComponent, RequireComponent(typeof(ZombieController))]
    public sealed class ZombieAudioPresenter : MonoBehaviour
    {
        ZombieController zombie;
        ProductionAudio director;
        GameObject boundTarget;
        Vector3 lastPosition;
        float distance, nextIdle, nextHeartbeat, nextApproach;
        void Awake() => zombie=GetComponent<ZombieController>();
        void OnEnable() { zombie.Runtime.Changed+=Changed; zombie.ArchetypeChanged+=ArchetypeChanged; lastPosition=transform.position; nextIdle=Time.time+8+(GetEntityId().GetHashCode()&7); }
        void OnDisable() { zombie.Runtime.Changed-=Changed; zombie.ArchetypeChanged-=ArchetypeChanged; if(director) director.StopOwner(transform); director=null; boundTarget=null; distance=0; }
        void Bind()
        {
            if (zombie.IsDead && director) return; // Retain the terminal cue until owner/session teardown.
            if(boundTarget==zombie.Target && director) return;
            if (director) director.StopOwner(transform);
            boundTarget=zombie.Target;
            director=boundTarget?boundTarget.GetComponent<Noise.GameplayNoiseContext>()?.Session?.GetComponent<ProductionAudio>():null;
        }
        AudioCue Telegraph => zombie.Definition.ArchetypeId == ZombieArchetypes.Lurker ? AudioCue.LurkerTelegraph :
            zombie.Definition.ArchetypeId == ZombieArchetypes.Runner ? AudioCue.RunnerTelegraph : AudioCue.ZombieTelegraph;
        void ArchetypeChanged(ZombieArchetypePhase phase)
        {
            Bind(); if (!director || zombie.Paused || zombie.IsDead) return;
            if (phase == ZombieArchetypePhase.Preparation)
            { director.StopOwner(transform); director.Play(zombie.Definition.ArchetypeId == ZombieArchetypes.Runner ? AudioCue.RunnerPreparation : AudioCue.LurkerTelegraph, transform); }
            else if (phase == ZombieArchetypePhase.Suspicious) director.Play(AudioCue.LurkerSuspicion, transform);
            else if (phase == ZombieArchetypePhase.Recovery && zombie.Definition.ArchetypeId == ZombieArchetypes.Runner)
                director.Play(AudioCue.RunnerRecovery, transform);
        }
        void Changed(ZombieState from,ZombieState to)
        {
            Bind(); if(!director || zombie.Paused) return;
            if(to==ZombieState.Chasing && from!=ZombieState.Recovering && from!=ZombieState.AttackWindup &&
                zombie.Definition.ArchetypeId != ZombieArchetypes.Lurker && zombie.Archetype.Phase != ZombieArchetypePhase.Preparation)
            { director.StopOwner(transform); nextApproach=Time.time+4; director.Play(AudioCue.ZombieAlert,transform); director.Threat(transform); }
            else if(to==ZombieState.AttackWindup) { director.StopOwner(transform); director.Play(Telegraph,transform); director.Threat(transform); }
            else if(to==ZombieState.AttackCommit) director.Play(AudioCue.ZombieAttack,transform);
            else if(to==ZombieState.HitReact) director.Play(AudioCue.ZombieHurt,transform);
            else if(to==ZombieState.Dead) { director.StopOwner(transform); director.Play(AudioCue.ZombieDeath,transform); }
        }
        void LateUpdate()
        {
            Bind(); Vector3 delta=transform.position-lastPosition; lastPosition=transform.position; delta.y=0;
            if(!director || !director.Active || zombie.Paused || zombie.IsDead) { distance=0; return; }
            float now=Time.time;
            if(now>=nextHeartbeat)
            {
                nextHeartbeat=now+1;
                if(zombie.Runtime.Visible && (zombie.Runtime.State==ZombieState.Chasing || zombie.Attacking)) director.Threat(transform);
            }
            if(zombie.Runtime.State==ZombieState.Idle && now>=nextIdle)
            { nextIdle=now+10+(GetEntityId().GetHashCode()&7); director.Play(zombie.Definition.ArchetypeId == ZombieArchetypes.Lurker ? AudioCue.LurkerSuspicion : AudioCue.ZombieIdle,transform); }
            if(zombie.Runtime.State==ZombieState.Chasing && !zombie.Archetype.Hold && now>=nextApproach)
            { nextApproach=now+6; director.Play(AudioCue.ZombieApproach,transform); }
            float moved=delta.magnitude;
            if(moved>1.5f || zombie.Attacking) { distance=0; return; }
            if(moved<.001f) return;
            distance+=moved;
            if(distance>=1.2f) { distance%=1.2f; director.Step(transform,.65f,true); }
        }
    }
}
