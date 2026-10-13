using UnityEngine;
namespace LastSignal
{
    public sealed partial class ZombieDefinition
    {
        [Header("S022 — simulation seconds; legacy Shambler keeps 100 HP")]
        [SerializeField] string archetypeId = ZombieArchetypes.Shambler;
        [SerializeField] float archetypeHealth = 100;
        [SerializeField] float preparationSeconds = .65f, burstSeconds = 1.6f;
        [SerializeField] float recoverySeconds = 2.4f, burstCooldown = 5;
        [SerializeField] float burstSpeed = 4.6f, burstTurnSpeed = 65;
        [SerializeField] float ambushRange = 6;
        public string ArchetypeId => string.IsNullOrEmpty(archetypeId) ? ZombieArchetypes.Shambler : archetypeId;
        public float ArchetypeHealth => archetypeHealth;
        public float PreparationSeconds => preparationSeconds;
        public float BurstSeconds => burstSeconds;
        public float RecoverySeconds => recoverySeconds;
        public float BurstCooldown => burstCooldown;
        public float BurstSpeed => burstSpeed;
        public float BurstTurnSpeed => burstTurnSpeed;
        public float AmbushRange => ambushRange;
        public bool IsArchetypeValid(out string reason)
        {
            if (!ZombieArchetypes.IsKnown(ArchetypeId) || !Positive(archetypeHealth) || !ZombieArchetypes.ValidSaved(ArchetypeId, archetypeHealth) ||
                !Positive(preparationSeconds) || preparationSeconds < .5f || !Positive(burstSeconds) || burstSeconds > 3 ||
                !Positive(recoverySeconds) || recoverySeconds < 1 || !Positive(burstCooldown) || burstCooldown < recoverySeconds ||
                !Positive(burstSpeed) || burstSpeed > 6 || burstSpeed <= runSpeed || !Positive(burstTurnSpeed) || burstTurnSpeed > 120 || !Positive(ambushRange))
            { reason = "Invalid S022 identity, health or bounded warning/burst/recovery profile."; return false; }
            reason = null; return true;
        }
    }
    public static class ZombieArchetypes
    {
        public const string Shambler = "zombie.shambler", Lurker = "zombie.lurker", Runner = "zombie.runner";
        public static string Legacy(string id) => string.IsNullOrEmpty(id) ? Shambler : id;
        public static bool IsKnown(string id) => id == Shambler || id == Lurker || id == Runner;
        public static bool ValidSaved(string id, float health) => IsKnown(Legacy(id)) && float.IsFinite(health) &&
            health >= 0 && health <= (Legacy(id) == Runner ? 60 : Legacy(id) == Lurker ? 75 : 100);
        // Stable FNV-1a; independent of runtime string hashing and global Unity RNG.
        public static uint Hash(string identity)
        { uint h = 2166136261; foreach (char c in identity) { h ^= c; h = unchecked(h * 16777619); } return h; }
        public static string Select(string identity, int shamblerWeight = 70, int lurkerWeight = 20, int runnerWeight = 10)
        {
            if (identity == null || shamblerWeight < 0 || lurkerWeight < 0 || runnerWeight < 0 ||
                shamblerWeight > 10000 || lurkerWeight > 10000 || runnerWeight > 10000 || shamblerWeight + lurkerWeight + runnerWeight == 0)
                throw new System.ArgumentException("Invalid deterministic population weights/identity.");
            uint roll = Hash(identity) % (uint)(shamblerWeight + lurkerWeight + runnerWeight);
            return roll < shamblerWeight ? Shambler : roll < shamblerWeight + lurkerWeight ? Lurker : Runner;
        }
        public static ZombieDefinition Load(string id)
        {
            var catalog = Resources.Load<ZombieArchetypeCatalog>("S022/ZombieCatalog");
            return catalog ? catalog.Resolve(id) : null;
        }
    }
}
