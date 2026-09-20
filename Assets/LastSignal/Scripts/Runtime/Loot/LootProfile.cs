using System;
using System.Collections.Generic;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using UnityEngine;

namespace LastSignal.Loot
{
    [Serializable]
    public struct LootEntry
    {
        [SerializeField] ItemDefinition item;
        [SerializeField, Min(1)] int weight;
        [SerializeField, Min(1)] int minQuantity;
        [SerializeField, Min(1)] int maxQuantity;
        public ItemDefinition Item => item;
        public int Weight => weight;
        public int MinQuantity => minQuantity;
        public int MaxQuantity => maxQuantity;
        public LootEntry(ItemDefinition item, int weight, int min, int max)
        { this.item = item; this.weight = weight; minQuantity = min; maxQuantity = max; }
    }

    public enum LootOutcome { Spawned, Empty, Invalid, Blocked }
    public readonly struct LootSelection
    {
        public readonly ItemDefinition Item;
        public readonly int Quantity;
        public readonly LootOutcome Outcome;
        public LootSelection(LootOutcome outcome, ItemDefinition item = null, int quantity = 0)
        { Outcome = outcome; Item = item; Quantity = quantity; }
    }

    [CreateAssetMenu(menuName = "Last Signal/Loot/Profile")]
    public sealed class LootProfile : ScriptableObject
    {
        [Tooltip("0 = never empty, 10000 = always empty. Roll before weighted selection.")]
        [SerializeField, Range(0, 10000)] int emptyBasisPoints = 3500;
        [SerializeField] LootEntry[] entries = Array.Empty<LootEntry>();
        public int EmptyBasisPoints => emptyBasisPoints;
        public int EntryCount => entries == null ? 0 : entries.Length;
        public LootEntry GetEntry(int index) => entries[index];

        public bool Validate(out string error)
        {
            error = null;
            if (emptyBasisPoints < 0 || emptyBasisPoints > 10000) error = "Empty chance must be 0..10000 basis points.";
            else if (entries == null || entries.Length == 0) error = "Profile requires at least one entry (even when always empty).";
            else
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var e in entries)
                {
                    if (!e.Item || !e.Item.Id.IsValid || e.Item.MaxStack < 1) { error = "Entry requires valid item identity and stack size."; break; }
                    if (!e.Item.WorldPrefab || !e.Item.WorldPrefab.activeSelf || !e.Item.WorldPrefab.GetComponent<WorldItem>()) { error = "Item requires an active S005 WorldItem prefab."; break; }
                    if (!ids.Add(e.Item.Id.Value)) { error = "Duplicate item identity in profile."; break; }
                    if (e.Weight <= 0) { error = "Weights must be positive integers."; break; }
                    if (e.MinQuantity < 1 || e.MaxQuantity < e.MinQuantity) { error = "Quantity must satisfy 1 <= min <= max."; break; }
                }
            }
            return error == null;
        }

        public LootSelection Select(int seed, string pointId)
        {
            if (!Validate(out _)) return new LootSelection(LootOutcome.Invalid);
            var random = new LootRandom(seed, pointId);
            if (random.Below(10000) < (ulong)emptyBasisPoints) return new LootSelection(LootOutcome.Empty);
            // Maximum array length and int weights fit safely within ulong.
            ulong total = 0;
            foreach (var e in entries) total += (ulong)e.Weight;
            ulong ticket = random.Below(total);
            foreach (var e in entries)
            {
                if (ticket < (ulong)e.Weight)
                {
                    int quantity = e.MinQuantity + (int)random.Below((ulong)((long)e.MaxQuantity - e.MinQuantity + 1));
                    return new LootSelection(LootOutcome.Spawned, e.Item, quantity);
                }
                ticket -= (ulong)e.Weight;
            }
            return new LootSelection(LootOutcome.Invalid);
        }
    }
}
