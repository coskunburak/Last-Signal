using LastSignal.Loot;
using UnityEngine;

namespace LastSignal.Authoring
{
    // Authoring references only. Existing session/cell systems remain the runtime owners.
    [DisallowMultipleComponent]
    public sealed class PoiAuthoringLayout : MonoBehaviour
    {
        public Transform entry, retreat, navigationGeometry;
        public LootSpawnPoint[] lootAnchors;
        public ZombieEncounter encounter;
        [TextArea] public string productionNotes;
    }
}
