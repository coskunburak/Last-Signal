using UnityEngine;
namespace LastSignal.Noise
{
    /// <summary>Explicit session dependency carried by its player for existing actor Bind(player) seams.</summary>
    [DisallowMultipleComponent]
    public sealed class GameplayNoiseContext : MonoBehaviour
    {
        public SessionFlow Session { get; private set; }
        public void Bind(SessionFlow session) => Session = session;
        public GameplayNoiseSystem System => Session ? Session.Noise : null;
        public double SimulationTime => Session ? Session.NoiseSimulationTime : double.NaN;
    }
}
