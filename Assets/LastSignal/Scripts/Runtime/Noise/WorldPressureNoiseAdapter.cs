using LastSignal.AI;
using LastSignal.WorldCells;

namespace LastSignal.Noise
{
    /// <summary>Gunshots and generator pulses affect regional pressure in authored ledger cells. Legacy save receipts remain noise:N;
    /// LastEventId retains the canonical correlation without changing the population save schema.</summary>
    public sealed class WorldPressureNoiseAdapter : IGameplayNoisePressureSink
    {
        readonly WorldPopulationManager population;
        readonly SessionFlow flow;
        readonly long generation;
        GameplayNoiseId lastReceived;
        public GameplayNoiseId LastEventId { get; private set; }
        public long LastPressureReceipt { get; private set; }
        public WorldPressureNoiseAdapter(WorldPopulationManager manager, SessionFlow owner)
        { population = manager; flow = owner; generation = owner.Generation; }
        public bool Forward(in GameplayNoiseEvent noise)
        {
            if (!population || !flow || flow.Generation != generation || flow.InMenu || flow.Paused || flow.Restoring || flow.PlayerDead ||
                (noise.Category != GameplayNoiseCategory.Gunshot && noise.Category != GameplayNoiseCategory.Generator) || noise.Intensity <= 0 ||
                (noise.EventId.Epoch == lastReceived.Epoch && noise.EventId.Sequence <= lastReceived.Sequence) ||
                population.LastNoiseSequence == long.MaxValue) return false;
            var id = CellCoordinate.FromWorld(noise.Position).Id;
            // Unknown world cells have no pressure ledger; local distribution still remains valid.
            var cells = flow.GetComponent<WorldCellManager>();
            bool known = false;
            foreach (var defined in cells.DefinedCellIds) if (defined == id) { known = true; break; }
            if (!known) return false;
            lastReceived = noise.EventId;
            long receipt = population.LastNoiseSequence + 1;
            population.ReportNoise("noise:" + receipt, id, noise.Intensity);
            if (population.LastNoiseSequence != receipt) return false;
            LastEventId = noise.EventId; LastPressureReceipt = receipt; return true;
        }
    }
}
