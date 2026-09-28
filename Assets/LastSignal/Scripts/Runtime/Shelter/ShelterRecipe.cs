using System;
using System.Collections.Generic;
using LastSignal.Inventory.Data;
using UnityEngine;

namespace LastSignal.Shelter
{
    [CreateAssetMenu(menuName = "Last Signal/Shelter/Recipe")]
    public sealed class ShelterRecipe : ScriptableObject
    {
        public string recipeId;
        public int revision = 1;
        public ItemDefinition input, output, tool;
        public int inputQuantity = 2, outputQuantity = 10;
        public double durationWorldSeconds = 600;
        public ShelterModule station = ShelterModule.Workbench;
        public bool requiresPower = true;
        public bool Valid => !string.IsNullOrWhiteSpace(recipeId) && recipeId.Length <= 128 && revision > 0 &&
            input && output && input.Id.IsValid && output.Id.IsValid && input.Id != output.Id &&
            inputQuantity > 0 && inputQuantity <= 256L * input.MaxStack && outputQuantity > 0 && outputQuantity <= 256L * output.MaxStack &&
            (!tool || tool.Id.IsValid) && station == ShelterModule.Workbench &&
            WorldTime.WorldTimeSettings.Finite(durationWorldSeconds) && durationWorldSeconds >= 1 && durationWorldSeconds <= 86400;
        public static bool ValidateCatalog(ShelterRecipe[] recipes)
        {
            if (recipes == null || recipes.Length == 0 || recipes.Length > 64) return false;
            var ids = new HashSet<string>();
            foreach (var r in recipes) if (!r || !r.Valid || !ids.Add(r.recipeId)) return false;
            // Conservatively disallow resource conversion cycles in this small production catalog.
            foreach (var r in recipes)
                if (Reaches(recipes, r.output.Id, r.input.Id, new HashSet<StableItemId>())) return false;
            return true;
        }
        static bool Reaches(ShelterRecipe[] recipes, StableItemId from, StableItemId target, HashSet<StableItemId> seen)
        {
            if (from == target) return true;
            if (!seen.Add(from)) return false;
            foreach (var r in recipes) if (r.input.Id == from && Reaches(recipes, r.output.Id, target, seen)) return true;
            return false;
        }
    }
    public enum ShelterModule { Bed, Storage, Workbench }
    public enum CraftStatus { Running, CompletedWaitingOutput, CancelledWaitingRefund }
    [Serializable] public sealed class CraftSnapshot
    {
        public string id, recipeId, inputId, outputId;
        public int revision, inputQuantity, outputQuantity;
        public double progress, duration;
        public CraftStatus status;
    }
    [Serializable] public sealed class ShelterProductionSnapshot
    {
        public int version;
        public string shelterId;
        public bool claimed, bed, storage, workbench, upgraded, generatorEnabled;
        public double fuelSeconds, lastProcessed;
        public long sequence;
        public CraftSnapshot job;
    }
}
