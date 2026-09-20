using System;
using UnityEngine;

namespace LastSignal.Inventory.Data
{
    [Serializable]
    public struct StableItemId : IEquatable<StableItemId>
    {
        [SerializeField] string id;
        public string Value => id;
        
        public StableItemId(string id)
        {
            this.id = id;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(id);

        public bool Equals(StableItemId other) => string.Equals(id, other.id, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StableItemId other && Equals(other);
        public override int GetHashCode() => id != null ? StringComparer.Ordinal.GetHashCode(id) : 0;

        public static bool operator ==(StableItemId left, StableItemId right) => left.Equals(right);
        public static bool operator !=(StableItemId left, StableItemId right) => !left.Equals(right);
        public override string ToString() => id ?? "invalid";
    }
}
