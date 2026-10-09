using System;

namespace LastSignal
{
    // Slice survival resources. World seconds only; wall clock never participates.
    [Serializable] public sealed class SurvivalSnapshot
    {
        public int version;
        public double hydration, nutrition;
        public int bleeding;
        public long woundRevision;
        public string backpackId;
        public int baseCapacity;
    }

    public sealed class SurvivalState
    {
        public const double HydrationDrain = 100d / 43200, NutritionDrain = 100d / 86400;
        public double Hydration { get; private set; } = 100;
        public double Nutrition { get; private set; } = 100;
        public int Bleeding { get; private set; }
        public long WoundRevision { get; private set; }
        public float RecoveryMultiplier => (Hydration <= 10 ? .5f : 1) * (Nutrition <= 0 ? .5f : 1) * (Bleeding > 0 ? .75f : 1);
        public double DamagePerWorldSecond => (Hydration <= 0 ? 6d / 3600 : 0) +
            (Nutrition <= 0 ? 2d / 3600 : 0) + Bleeding * 12d / 3600;
        public double NextBoundarySeconds
        {
            get
            {
                double next = 60;
                if (Hydration > 0) next = Math.Min(next, Hydration / HydrationDrain);
                if (Nutrition > 0) next = Math.Min(next, Nutrition / NutritionDrain);
                return Math.Max(1e-6, next);
            }
        }
        public void AddWound()
        {
            Bleeding = Math.Min(3, Bleeding + 1);
            WoundRevision++;
        }
        public bool CanTreat(long revision) => Bleeding > 0 && revision == WoundRevision;
        public void Treat(int reduction = 3)
        { if (reduction < 1 || reduction > 3) throw new ArgumentOutOfRangeException(nameof(reduction)); Bleeding = Math.Max(0, Bleeding - reduction); WoundRevision++; }
        public void Drink(double amount)
        { RequireAmount(amount); Hydration = Math.Min(100, Hydration + amount); }
        public void Eat(double amount)
        { RequireAmount(amount); Nutrition = Math.Min(100, Nutrition + amount); }
        static void RequireAmount(double amount)
        { if (!double.IsFinite(amount) || amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount)); }
        public double Advance(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            // Exact threshold integration is independent of caller chunk size.
            double dry = Math.Max(0, seconds - Hydration / HydrationDrain);
            double hungry = Math.Max(0, seconds - Nutrition / NutritionDrain);
            double damage = dry * 6d / 3600 + hungry * 2d / 3600 + Bleeding * seconds * 12d / 3600;
            Hydration = Math.Max(0, Hydration - seconds * HydrationDrain);
            Nutrition = Math.Max(0, Nutrition - seconds * NutritionDrain);
            return damage;
        }
        public static bool Valid(SurvivalSnapshot state) => state != null && state.version == 1 &&
            double.IsFinite(state.hydration) && state.hydration >= 0 && state.hydration <= 100 &&
            double.IsFinite(state.nutrition) && state.nutrition >= 0 && state.nutrition <= 100 &&
            state.bleeding >= 0 && state.bleeding <= 3 && state.woundRevision >= state.bleeding && state.woundRevision < long.MaxValue - 1 &&
            state.baseCapacity >= 1 && state.baseCapacity <= 248;
        public SurvivalSnapshot Capture(int baseCapacity, string backpackId) => new SurvivalSnapshot {
            version = 1, hydration = Hydration, nutrition = Nutrition, bleeding = Bleeding,
            woundRevision = WoundRevision, baseCapacity = baseCapacity, backpackId = backpackId };
        public void Restore(SurvivalSnapshot state)
        {
            if (!Valid(state)) throw new ArgumentException("Invalid survival state.");
            Hydration = state.hydration; Nutrition = state.nutrition;
            Bleeding = state.bleeding; WoundRevision = state.woundRevision;
        }
    }
}
