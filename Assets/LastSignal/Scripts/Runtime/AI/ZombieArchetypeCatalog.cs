using UnityEngine;
namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Zombie Archetype Catalog")]
    public sealed class ZombieArchetypeCatalog : ScriptableObject
    {
        [SerializeField] ZombieDefinition shambler, lurker, runner;
        [SerializeField, Range(0, 10000)] int shamblerWeight = 70, lurkerWeight = 20, runnerWeight = 10;
        public bool IsValid => shambler && lurker && runner && shambler.ArchetypeId == ZombieArchetypes.Shambler &&
            lurker.ArchetypeId == ZombieArchetypes.Lurker && runner.ArchetypeId == ZombieArchetypes.Runner &&
            shambler.IsValid(out _) && lurker.IsValid(out _) && runner.IsValid(out _) &&
            shamblerWeight >= 0 && lurkerWeight >= 0 && runnerWeight >= 0 && shamblerWeight <= 10000 &&
            lurkerWeight <= 10000 && runnerWeight <= 10000 && shamblerWeight + lurkerWeight + runnerWeight > 0;
        public string Select(string identity) => ZombieArchetypes.Select(identity, shamblerWeight, lurkerWeight, runnerWeight);
        public ZombieDefinition Resolve(string id) => ZombieArchetypes.Legacy(id) switch {
            ZombieArchetypes.Shambler => shambler, ZombieArchetypes.Lurker => lurker,
            ZombieArchetypes.Runner => runner, _ => null };
    }
}
