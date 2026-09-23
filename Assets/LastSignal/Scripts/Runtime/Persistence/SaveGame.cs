using System;

namespace LastSignal.Persistence
{
    // Detached data only. No Unity object references or default-valued required sections:
    // missing sections must remain detectable when parsing old/corrupt JSON.
    [Serializable] public sealed class SaveGame
    {
        public LastSignal.WorldTime.WorldTimeSnapshot worldTime;
        public SaveHeader header;
        public PlayerSnapshot player;
        public ContainerSnapshot inventory;
        public WeaponSnapshot weapon;
        public ShelterSnapshot shelter;
        public WorldSnapshot world;
    }
    [Serializable] public sealed class SaveHeader
    {
        public int schemaVersion;
        public string contentVersion, buildId, worldId, timestampUtc;
        public int seed;
        public long generation;
    }
    [Serializable] public sealed class TransformSnapshot
    {
        public float x, y, z, qx, qy, qz, qw;
    }
    [Serializable] public sealed class PlayerSnapshot
    {
        public string id;
        public TransformSnapshot transform;
        public float health, pitch;
        public bool crouching;
    }
    [Serializable] public sealed class SlotSnapshot
    {
        public string definitionId;
        public int quantity;
    }
    [Serializable] public sealed class ContainerSnapshot
    {
        public string id;
        public int capacity;
        public SlotSnapshot[] slots;
    }
    [Serializable] public sealed class WeaponSnapshot
    {
        public string definitionId;
        public int magazine;
    }
    [Serializable] public sealed class ShelterSnapshot
    {
        public ContainerSnapshot storage;
        public bool onExpedition;
        public int expeditionIndex;
    }
    [Serializable] public sealed class DoorSnapshot
    {
        public string id;
        public bool open;
    }
    public enum OpportunityOutcome { Unknown = 0, Generated = 1, Empty = 2, Blocked = 3 }
    public enum WorldItemOrigin { Unknown = 0, Loot = 1, Drop = 2, Authored = 3 }
    public enum EntityDisposition { Unknown = 0, Present = 1, Consumed = 2 }
    [Serializable] public sealed class LootOpportunitySnapshot
    {
        public string id;
        public OpportunityOutcome outcome;
        public string entityId;
    }
    [Serializable] public sealed class WorldItemSnapshot
    {
        public string id, definitionId;
        public WorldItemOrigin origin;
        public EntityDisposition disposition;
        public int quantity;
        public TransformSnapshot transform;
    }
    [Serializable] public sealed class EnemySnapshot
    {
        public string id;
        public float health;
        public TransformSnapshot transform;
    }
    [Serializable] public sealed class WorldSnapshot
    {
        public DoorSnapshot[] doors;
        public LootOpportunitySnapshot[] opportunities;
        public WorldItemSnapshot[] items;
        public EnemySnapshot[] enemies;
    }
    public enum SaveError
    {
        None, MissingFile, InvalidJson, InvalidData, UnsupportedSchema, IncompatibleContent,
        WrongWorld, UnknownDefinition, DuplicateIdentity, ChecksumMismatch, TooLarge,
        IoFailure, Busy, StaleSession, StaleGeneration
    }
    public readonly struct SaveResult
    {
        public readonly SaveError Error;
        public readonly string Message;
        public bool Success => Error == SaveError.None;
        public SaveResult(SaveError error, string message) { Error = error; Message = message; }
        public static SaveResult Ok => new SaveResult(SaveError.None, "Success");
    }
}
