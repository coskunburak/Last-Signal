using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.WorldTime;

namespace LastSignal.Shelter
{
    /// <summary>One session-owned workstation. Inputs live in the persisted job escrow;
    /// completion converts that escrow to one pending result. No growing receipt history.</summary>
    public sealed class ShelterProduction : IWorldTimeParticipant
    {
        readonly ShelterRecipe recipe;
        readonly ItemDefinition material, fuel;
        readonly int moduleCost, upgradeCost;
        readonly double fuelUnitSeconds;
        ShelterProductionSnapshot state;
        bool busy;
        public ShelterProductionSnapshot Capture()
        {
            if (busy || Persistence.OwnershipTransaction.Active) throw new InvalidOperationException("Production transaction in progress.");
            return Copy(state);
        }
        public bool Claimed => state.claimed;
        public bool Upgraded => state.upgraded;
        public bool Installed(ShelterModule module) => module == ShelterModule.Bed ? state.bed : module == ShelterModule.Storage ? state.storage : module == ShelterModule.Workbench && state.workbench;
        public CraftStatus? Status => state.job?.status;
        public double Progress => state.job == null ? 0 : state.job.progress / state.job.duration;
        public double FuelSeconds => state.fuelSeconds;
        public bool GeneratorEnabled => state.generatorEnabled;
        public bool Powered => GeneratorEnabled && FuelSeconds > 0;
        public bool Busy => busy;
        public event Action<double> Generated;
        public ShelterProduction(string id, ShelterRecipe recipe, ItemDefinition material, ItemDefinition fuel, int moduleCost, int upgradeCost, double fuelUnitSeconds, double now)
        {
            if (string.IsNullOrWhiteSpace(id) || !recipe || !recipe.Valid || !material || !fuel || moduleCost < 1 || upgradeCost < 1 ||
                !WorldTimeSettings.Finite(fuelUnitSeconds) || fuelUnitSeconds < 1 || !WorldTimeSettings.ValidTime(now)) throw new ArgumentException("Invalid shelter production definitions.");
            this.recipe = recipe; this.material = material; this.fuel = fuel; this.moduleCost = moduleCost; this.upgradeCost = upgradeCost; this.fuelUnitSeconds = fuelUnitSeconds;
            state = new ShelterProductionSnapshot { version = 1, shelterId = id, lastProcessed = now };
        }
        public bool Claim()
        {
            if (busy || state.claimed) return false;
            state.claimed = true; return true;
        }
        public bool Install(ShelterModule module, InventoryContainer source)
        {
            if (busy || !Claimed || !Enum.IsDefined(typeof(ShelterModule), module) || Installed(module) || source == null) return false;
            return Mutate(source, material, moduleCost, null, 0, () => {
                if (module == ShelterModule.Bed) state.bed = true;
                else if (module == ShelterModule.Storage) state.storage = true;
                else state.workbench = true;
            });
        }
        public bool Upgrade(InventoryContainer source)
        {
            if (busy || !state.workbench || state.upgraded || state.job != null || source == null) return false;
            return Mutate(source, material, upgradeCost, null, 0, () => state.upgraded = true);
        }
        public bool Refuel(InventoryContainer source)
        {
            if (busy || !Claimed || source == null || state.fuelSeconds + fuelUnitSeconds > 86400) return false;
            return Mutate(source, fuel, 1, null, 0, () => state.fuelSeconds += fuelUnitSeconds);
        }
        public bool ToggleGenerator()
        { if (busy || !Claimed) return false; state.generatorEnabled = !state.generatorEnabled; return true; }
        public bool Start(InventoryContainer source)
        {
            if (busy || !state.workbench || state.job != null || !recipe.Valid || source == null || state.sequence == long.MaxValue ||
                (recipe.tool && source.GetTotalQuantity(recipe.tool) < 1)) return false;
            var job = new CraftSnapshot { id = state.shelterId + ":job:" + (state.sequence + 1), recipeId = recipe.recipeId, revision = recipe.revision,
                inputId = recipe.input.Id.Value, outputId = recipe.output.Id.Value, inputQuantity = recipe.inputQuantity, outputQuantity = recipe.outputQuantity,
                duration = recipe.durationWorldSeconds * (state.upgraded ? .5 : 1), status = CraftStatus.Running };
            return Mutate(source, recipe.input, recipe.inputQuantity, null, 0, () => { state.sequence++; state.job = job; });
        }
        public bool Cancel()
        {
            if (busy || state.job == null || state.job.status != CraftStatus.Running) return false;
            state.job.status = CraftStatus.CancelledWaitingRefund; return true;
        }
        public bool Collect(InventoryContainer destination)
        {
            if (busy || state.job == null || state.job.status == CraftStatus.Running || destination == null) return false;
            bool refund = state.job.status == CraftStatus.CancelledWaitingRefund;
            return Mutate(destination, null, 0, refund ? recipe.input : recipe.output, refund ? state.job.inputQuantity : state.job.outputQuantity, () => state.job = null);
        }
        bool Mutate(InventoryContainer container, ItemDefinition remove, int count, ItemDefinition add, int added, Action commit)
        {
            busy = true;
            try { return container.Exchange(remove, count, add, added, commit); }
            finally { busy = false; }
        }
        public double NextBoundary(double now)
        {
            double remaining = Powered ? state.fuelSeconds : double.PositiveInfinity;
            if (state.job != null && state.job.status == CraftStatus.Running && (!recipe.requiresPower || Powered))
                remaining = Math.Min(remaining, state.job.duration - state.job.progress);
            return double.IsPositiveInfinity(remaining) ? remaining : now + Math.Max(.000001, remaining);
        }
        public void ApplyElapsed(double from, double to)
        {
            if (!WorldTimeSettings.ValidTime(to) || from != state.lastProcessed || to < from) throw new InvalidOperationException("Noncontiguous production clock.");
            double dt = to - from, powered = state.generatorEnabled ? Math.Min(dt, state.fuelSeconds) : 0;
            state.fuelSeconds = Math.Max(0, state.fuelSeconds - powered);
            var job = state.job;
            if (job != null && job.status == CraftStatus.Running)
            {
                job.progress = Math.Min(job.duration, job.progress + (recipe.requiresPower ? powered : dt));
                if (job.progress >= job.duration) job.status = CraftStatus.CompletedWaitingOutput;
            }
            state.lastProcessed = to;
            if (powered > 0) Generated?.Invoke(powered);
        }
        public AdvanceReason Inspect(double now) => AdvanceReason.Completed;
        public bool CanRestore(ShelterProductionSnapshot value, double now)
        {
            if (!ValidSnapshot(value, now) || value.shelterId != state.shelterId) return false;
            var j = value.job;
            return j == null || (j.recipeId == recipe.recipeId && j.revision == recipe.revision && j.inputId == recipe.input.Id.Value &&
                j.outputId == recipe.output.Id.Value && j.inputQuantity == recipe.inputQuantity && j.outputQuantity == recipe.outputQuantity &&
                j.duration == recipe.durationWorldSeconds * (value.upgraded ? .5 : 1));
        }
        public void Restore(ShelterProductionSnapshot value, double now)
        { if (busy || !CanRestore(value, now)) throw new ArgumentException("Incompatible production snapshot."); state = Copy(value); }
        public static bool ValidSnapshot(ShelterProductionSnapshot s, double now)
        {
            if (s == null || s.version != 1 || string.IsNullOrWhiteSpace(s.shelterId) || s.shelterId.Length > 128 ||
                !WorldTimeSettings.ValidTime(s.lastProcessed) || s.lastProcessed != now || !WorldTimeSettings.Finite(s.fuelSeconds) || s.fuelSeconds < 0 || s.fuelSeconds > 86400 || s.sequence < 0 ||
                (!s.claimed && (s.bed || s.storage || s.workbench || s.upgraded || s.generatorEnabled || s.fuelSeconds != 0 || s.sequence != 0 || s.job != null)) || (s.upgraded && !s.workbench)) return false;
            var j = s.job;
            return j == null || (s.workbench && s.sequence > 0 && j.id == s.shelterId + ":job:" + s.sequence && !string.IsNullOrWhiteSpace(j.recipeId) &&
                !string.IsNullOrWhiteSpace(j.inputId) && !string.IsNullOrWhiteSpace(j.outputId) && j.inputId != j.outputId && j.revision > 0 && j.inputQuantity > 0 && j.outputQuantity > 0 &&
                WorldTimeSettings.Finite(j.duration) && j.duration > 0 && j.duration <= 86400 && WorldTimeSettings.Finite(j.progress) && j.progress >= 0 && j.progress <= j.duration &&
                Enum.IsDefined(typeof(CraftStatus), j.status) && (j.status == CraftStatus.CompletedWaitingOutput ? j.progress == j.duration : j.progress < j.duration));
        }
        static ShelterProductionSnapshot Copy(ShelterProductionSnapshot s)
        {
            var c = new ShelterProductionSnapshot { version = s.version, shelterId = s.shelterId, claimed = s.claimed, bed = s.bed, storage = s.storage, workbench = s.workbench,
                upgraded = s.upgraded, generatorEnabled = s.generatorEnabled, fuelSeconds = s.fuelSeconds, lastProcessed = s.lastProcessed, sequence = s.sequence };
            var j = s.job;
            if (j != null) c.job = new CraftSnapshot { id = j.id, recipeId = j.recipeId, revision = j.revision, inputId = j.inputId, outputId = j.outputId,
                inputQuantity = j.inputQuantity, outputQuantity = j.outputQuantity, duration = j.duration, progress = j.progress, status = j.status };
            return c;
        }
    }
}
