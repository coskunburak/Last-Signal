using UnityEngine;
namespace LastSignal.WorldTime
{
    [DisallowMultipleComponent] public sealed class RestPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] WorldClock clock;
        [SerializeField, Min(.1f)] float useRange = 2.5f;
        [SerializeField, Min(1)] double sleepSeconds = 21600;
        public WorldClock Clock => clock;
        public float UseRange => useRange;
        public double SleepSeconds => sleepSeconds;
        public void Configure(WorldClock owner) => clock = owner;
        public bool Available => isActiveAndEnabled && clock && clock.Flow && clock.Flow.Player && !clock.Flow.Paused && !clock.Sleeping;
        public string Prompt => "E — Sleep 6 hours / recover under shelter";
        public bool TryInteract() => Available && clock.RequestSleep(this, sleepSeconds).Accepted;
    }
}
