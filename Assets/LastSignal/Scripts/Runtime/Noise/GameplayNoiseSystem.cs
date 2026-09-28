using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LastSignal.Noise
{
    /// <summary>Session-owned synchronous stream. Ordered receipt high-water mark gives permanent
    /// idempotence in O(1) space, including events evicted from the diagnostic history.
    /// Delayed/out-of-order delivery and reentrant emission are deliberately rejected.</summary>
    public sealed class GameplayNoiseSystem
    {
        public const int MaxListeners = 4096, HistoryCapacity = 64, CellSize = 16;
        static long nextEpoch; // Process-local namespace only; no Unity objects or static subscriptions.
        sealed class Entry
        {
            public IGameplayNoiseListener Listener;
            public Vector3 Position;
            public Vector2Int Cell;
            public bool Registered;
        }
        readonly Dictionary<ulong, Entry> listeners = new Dictionary<ulong, Entry>();
        readonly Dictionary<Vector2Int, List<Entry>> buckets = new Dictionary<Vector2Int, List<Entry>>();
        readonly Entry[] candidates = new Entry[MaxListeners];
        readonly GameplayNoiseTrace[] history = new GameplayNoiseTrace[HistoryCapacity];
        readonly Func<double> time;
        readonly Func<bool> allowed;
        readonly IGameplayNoisePressureSink pressure;
        readonly Action<Exception> reportFault;
        ulong highWater;
        int historyHead, historyCount;
        bool dispatching;
        public ulong Epoch { get; }
        public bool Active { get; private set; } = true;
        public int ListenerCount => listeners.Count;
        public int BucketCount => buckets.Count;
        public int RecentCount => historyCount;
        public ulong AcceptedCount => highWater;
        public bool CanEmit => Active && allowed();
        public GameplayNoiseTrace LastTrace => historyCount == 0 ? default : history[(historyHead + HistoryCapacity - 1) % HistoryCapacity];
        public GameplayNoiseTrace Recent(int index)
        {
            if (index < 0 || index >= historyCount) throw new ArgumentOutOfRangeException(nameof(index));
            return history[(historyHead - historyCount + index + HistoryCapacity) % HistoryCapacity];
        }
        public GameplayNoiseSystem(Func<double> simulationTime, Func<bool> canEmit,
            IGameplayNoisePressureSink pressureSink = null, Action<Exception> fault = null)
        {
            time = simulationTime ?? throw new ArgumentNullException(nameof(simulationTime));
            allowed = canEmit ?? throw new ArgumentNullException(nameof(canEmit));
            pressure = pressureSink; reportFault = fault;
            long epoch = Interlocked.Increment(ref nextEpoch);
            if (epoch <= 0) throw new InvalidOperationException("Noise epoch exhausted.");
            Epoch = (ulong)epoch;
        }
        public static bool ValidPosition(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z) &&
            Mathf.Abs(p.x) <= 1000000 && Mathf.Abs(p.y) <= 1000000 && Mathf.Abs(p.z) <= 1000000;
        static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.z / CellSize));
        public bool Register(IGameplayNoiseListener listener, Vector3 position)
        {
            if (!Active || listener == null || listener.ListenerId == 0 || !ValidPosition(position) ||
                listeners.Count == MaxListeners || listeners.ContainsKey(listener.ListenerId)) return false;
            var entry = new Entry { Listener = listener, Position = position, Cell = Cell(position), Registered = true };
            listeners.Add(listener.ListenerId, entry); AddBucket(entry); return true;
        }
        void AddBucket(Entry entry)
        {
            if (!buckets.TryGetValue(entry.Cell, out var bucket)) { bucket = new List<Entry>(); buckets.Add(entry.Cell, bucket); }
            bucket.Add(entry);
        }
        void RemoveBucket(Entry entry)
        {
            var bucket = buckets[entry.Cell]; bucket.Remove(entry);
            if (bucket.Count == 0) buckets.Remove(entry.Cell);
        }
        public bool UpdatePosition(IGameplayNoiseListener listener, Vector3 position)
        {
            if (listener == null || !ValidPosition(position) || !listeners.TryGetValue(listener.ListenerId, out var e) ||
                !ReferenceEquals(e.Listener, listener)) return false;
            var cell = Cell(position);
            if (cell != e.Cell) { RemoveBucket(e); e.Cell = cell; AddBucket(e); }
            e.Position = position; return true;
        }
        public void Unregister(IGameplayNoiseListener listener)
        {
            if (listener == null || !listeners.TryGetValue(listener.ListenerId, out var e) || !ReferenceEquals(e.Listener, listener)) return;
            e.Registered = false; RemoveBucket(e); listeners.Remove(listener.ListenerId);
        }
        public bool TryEmit(in GameplayNoiseRequest request, out GameplayNoiseEvent committed)
        {
            committed = default;
            if (highWater == ulong.MaxValue) return false;
            var noise = new GameplayNoiseEvent(new GameplayNoiseId(Epoch, highWater + 1), request, time());
            if (!TryReceive(noise)) return false;
            committed = noise; return true;
        }
        public bool TryReceive(in GameplayNoiseEvent noise)
        {
            double now = time();
            double ttl = noise.ExpiresAt - noise.SimulationTime;
            if (!CanEmit || dispatching || noise.EventId.Epoch != Epoch || noise.EventId.Sequence <= highWater ||
                noise.SourceId == 0 || !ValidPosition(noise.Position) ||
                noise.Category < GameplayNoiseCategory.Footstep || noise.Category > GameplayNoiseCategory.Generator ||
                !new GameplayNoiseProfile(noise.BaseRadiusMeters, noise.Intensity, (float)ttl).Valid ||
                !double.IsFinite(now) || !double.IsFinite(noise.SimulationTime) || noise.SimulationTime < 0 || noise.SimulationTime > now ||
                !double.IsFinite(noise.ExpiresAt) || noise.ExpiresAt <= now || ttl > 30) return false;
            highWater = noise.EventId.Sequence; dispatching = true;
            int count = 0, visited = 0, delivered = 0;
            bool forwarded = false;
            try
            {
                var extent = new Vector3(noise.BaseRadiusMeters, 0, noise.BaseRadiusMeters);
                var min = Cell(noise.Position - extent); var max = Cell(noise.Position + extent);
                for (int x = min.x; x <= max.x; x++) for (int z = min.y; z <= max.y; z++)
                {
                    visited++;
                    if (!buckets.TryGetValue(new Vector2Int(x, z), out var bucket)) continue;
                    foreach (var entry in bucket) candidates[count++] = entry;
                }
                // Pressure is a separate consumer and cannot depend on a physical listener's acceptance.
                forwarded = pressure != null && pressure.Forward(noise);
                float radiusSquared = noise.BaseRadiusMeters * noise.BaseRadiusMeters;
                for (int i = 0; i < count && Active; i++)
                {
                    var entry = candidates[i];
                    if (!entry.Registered || (entry.Position - noise.Position).sqrMagnitude > radiusSquared) continue;
                    try { entry.Listener.ReceiveNoise(noise); delivered++; }
                    catch (Exception exception) { if (reportFault == null) throw; reportFault(exception); }
                }
                if (Active)
                {
                    history[historyHead] = new GameplayNoiseTrace(noise, count, delivered, visited, forwarded);
                    historyHead = (historyHead + 1) % HistoryCapacity; historyCount = Math.Min(historyCount + 1, HistoryCapacity);
                }
                return true;
            }
            finally { Array.Clear(candidates, 0, count); dispatching = false; }
        }
        public void End()
        {
            Active = false;
            foreach (var e in listeners.Values) e.Registered = false;
            listeners.Clear(); buckets.Clear(); Array.Clear(history, 0, history.Length);
            historyHead = historyCount = 0; highWater = 0;
        }
    }
}
