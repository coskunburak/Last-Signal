using UnityEngine;
using LastSignal.Persistence;

namespace LastSignal
{
    public sealed class ZombieEncounter : MonoBehaviour
    {
        [SerializeField] ZombieController prefab;
        [SerializeField] Transform spawn;
        public ZombieController Actor { get; private set; }
        EnemySnapshot terminalState;
        ZombieHealth health;
        public EnemySnapshot CaptureEnemy()
        {
            if (terminalState != null) return terminalState;
            if (!Actor) return null;
            return new EnemySnapshot { id = GetComponent<PersistentEntityId>()?.Id,
                archetypeId = Actor.Definition.ArchetypeId, health = Actor.GetComponent<ZombieHealth>().CurrentHealth, transform = SaveSession.Pose(Actor.transform),
                anatomy = Actor.GetComponent<ZombieDismemberment>()?.CaptureState() };
        }
        void RememberDeath() { terminalState = CaptureEnemy(); }
        void RememberRestoredState() { terminalState = null; if (health && !health.IsAlive) RememberDeath(); }
        public uint Generation { get; private set; }
        public void Configure(ZombieController source, Transform spawnPoint) { prefab = source; spawn = spawnPoint; }
        public void Begin(GameObject player)
        {
            End(); Generation++;
            if (!prefab || !spawn) { Debug.LogError("Zombie encounter requires production prefab and NavMesh spawn.",this); return; }
            Actor = Instantiate(prefab,spawn.position,spawn.rotation);
            if (!Actor.Initialize() || !Actor.Bind(player)) { End(); return; }
            health = Actor.GetComponent<ZombieHealth>(); health.Died += RememberDeath; health.StateRestored += RememberRestoredState;
        }
        public void SetPaused(bool paused) { if(Actor)Actor.SetPaused(paused); }
        public void End()
        {
            if (health) { health.Died -= RememberDeath; health.StateRestored -= RememberRestoredState; } health = null; terminalState = null;
            if (!Actor) return;
            Actor.Shutdown(); Actor.gameObject.SetActive(false); Destroy(Actor.gameObject); Actor = null;
        }
        void OnDisable() => End();
    }
}
