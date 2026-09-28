using UnityEngine;
namespace LastSignal.Noise
{
    /// <summary>Explicitly bound QA/R03 seam example. Never changes AI state.</summary>
    public sealed class NoiseAcceptanceProbe : MonoBehaviour, IGameplayNoiseListener
    {
        GameplayNoiseSystem system;
        public ulong ListenerId { get; private set; }
        public int ReceivedCount { get; private set; }
        public GameplayNoiseEvent LastEvent { get; private set; }
        public void Bind(GameplayNoiseSystem authority, ulong id)
        {
            system?.Unregister(this); system = authority; ListenerId = id; ReceivedCount = 0; LastEvent = default;
            if (isActiveAndEnabled && !system.Register(this, transform.position)) Debug.LogError("Noise probe registration rejected.", this);
        }
        void OnEnable() { if (system != null && system.Active) system.Register(this, transform.position); }
        void LateUpdate() { system?.UpdatePosition(this, transform.position); }
        void OnDisable() => system?.Unregister(this);
        public void ReceiveNoise(in GameplayNoiseEvent noise) { LastEvent = noise; ReceivedCount++; }
    }
}
