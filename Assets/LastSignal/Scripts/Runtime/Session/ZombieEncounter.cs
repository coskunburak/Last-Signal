using UnityEngine;

namespace LastSignal
{
    public sealed class ZombieEncounter : MonoBehaviour
    {
        [SerializeField] ZombieController prefab;
        [SerializeField] Transform spawn;
        public ZombieController Actor { get; private set; }
        public uint Generation { get; private set; }
        public void Configure(ZombieController source, Transform spawnPoint) { prefab = source; spawn = spawnPoint; }
        public void Begin(GameObject player)
        {
            End(); Generation++;
            if (!prefab || !spawn) { Debug.LogError("Zombie encounter requires production prefab and NavMesh spawn.",this); return; }
            Actor = Instantiate(prefab,spawn.position,spawn.rotation);
            if (!Actor.Initialize() || !Actor.Bind(player)) End();
        }
        public void SetPaused(bool paused) { if(Actor)Actor.SetPaused(paused); }
        public void End()
        {
            if (!Actor) return;
            Actor.Shutdown(); Actor.gameObject.SetActive(false); Destroy(Actor.gameObject); Actor = null;
        }
        void OnDisable() => End();
    }
}
