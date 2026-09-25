using System;
using System.Collections.Generic;
using LastSignal.WorldCells;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.AI
{
    [Serializable] public sealed class CellPressureState
    {
        public string CellId;
        public float Pressure;
        public double LastUpdateTime;
        public List<string> Receipts = new List<string>();
        public void ApplyDecay(double now, float rate)
        {
            if (now <= LastUpdateTime) return;
            Pressure = (float)Math.Max(0, Pressure - (now - LastUpdateTime) * rate);
            LastUpdateTime = now;
        }
    }
    [Serializable] public sealed class MigrationGroup
    {
        public string GroupId, SourceCellId, TargetCellId;
        public double DepartureTime, ArrivalTime;
        public int Size;
    }
    [Serializable] public sealed class PopulationLedger
    {
        public string CellId;
        public int Logical, Dead;
        [NonSerialized] public int Physical;
    }
    [DisallowMultipleComponent, RequireComponent(typeof(WorldCellManager), typeof(WorldClock))]
    public sealed class WorldPopulationManager : MonoBehaviour, IWorldTimeParticipant
    {
        public float DecayRatePerSecond = .001f;
        public float MaxPressure = 1;
        public float NoiseIntensityMultiplier = .2f;
        public double TravelSecondsPerCell = 120;
        public int MaxReceipts = 10;
        [SerializeField] ZombieController zombiePrefab;
        [SerializeField] bool materializationEnabled;
        [SerializeField, Range(0, 100)] int initialPopulationPerCell = 15;
        readonly Dictionary<string, CellPressureState> pressures = new Dictionary<string, CellPressureState>(StringComparer.Ordinal);
        readonly Dictionary<string, PopulationLedger> ledgers = new Dictionary<string, PopulationLedger>(StringComparer.Ordinal);
        readonly List<MigrationGroup> migrations = new List<MigrationGroup>();
        sealed class Physical
        {
            public string Id, Cell;
            public ZombieHealth Health;
            public Action Died;
        }
        readonly Dictionary<ZombieController, Physical> physical = new Dictionary<ZombieController, Physical>();
        readonly List<PopulationActorSnapshot> dormant = new List<PopulationActorSnapshot>();
        readonly List<ZombieController> removal = new List<ZombieController>();
        readonly Plane[] planes = new Plane[6];
        WorldCellManager cells;
        WorldClock clock;
        SessionFlow flow;
        WorldSimulation simulation;
        long generation, lastNoiseSequence;
        double nextSearch;
        bool active;
        public event Action<string> PressureChanged;
        public int PhysicalCount => physical.Count;
        public int MigrationCount => migrations.Count;
        public long LastNoiseSequence => lastNoiseSequence;
        public IReadOnlyList<MigrationGroup> Migrations => migrations;
        public string MaterializationState { get; private set; } = "Idle";
        public bool MaterializationEnabled { get => materializationEnabled; set => materializationEnabled = value; }
        void Awake()
        {
            cells = GetComponent<WorldCellManager>(); clock = GetComponent<WorldClock>(); flow = GetComponent<SessionFlow>();
            cells.CellReady += OnCellReady; cells.CellLeaving += DematerializeCell; cells.CellEntered += OnCellReady;
        }
        public void BeginSession()
        {
            ClearSession();
            if (!float.IsFinite(DecayRatePerSecond) || DecayRatePerSecond < 0 || MaxPressure != 1 ||
                !float.IsFinite(NoiseIntensityMultiplier) || NoiseIntensityMultiplier <= 0 ||
                !WorldTimeSettings.ValidTime(TravelSecondsPerCell) || TravelSecondsPerCell <= 0 || MaxReceipts < 1 || MaxReceipts > 64)
                throw new InvalidOperationException("Invalid population authoring.");
            active = true; generation = flow.Generation;
            foreach (var id in cells.DefinedCellIds)
            {
                ledgers.Add(id, new PopulationLedger { CellId = id, Logical = initialPopulationPerCell });
                pressures.Add(id, new CellPressureState { CellId = id, LastUpdateTime = clock.Simulation.Seconds });
            }
            BindClock();
        }
        public void BindClock()
        {
            simulation?.Unregister(this); simulation = active ? clock.Simulation : null; simulation?.Register(this);
        }
        bool Current => active && flow && flow.Generation == generation && clock.Simulation != null;
        public void ReportShot(long sessionGeneration)
        {
            if (!Current || sessionGeneration != generation || flow.Paused || flow.Restoring || flow.PlayerDead || lastNoiseSequence == long.MaxValue) return;
            ReportNoise("noise:" + (lastNoiseSequence + 1), cells.CurrentCell, 1);
        }
        // Single-player synchronous receipt stream. The persisted high-water mark rejects old
        // events even after the bounded diagnostic receipt window is compacted.
        public void ReportNoise(string receiptId, string cellId, float intensity)
        {
            if (!Current || flow.Restoring || !ledgers.ContainsKey(cellId ?? "") || !float.IsFinite(intensity) || intensity <= 0 ||
                receiptId == null || !receiptId.StartsWith("noise:", StringComparison.Ordinal) ||
                !long.TryParse(receiptId.Substring(6), out var sequence) || sequence <= lastNoiseSequence) return;
            lastNoiseSequence = sequence;
            var state = GetPressure(cellId);
            state.Pressure = Mathf.Clamp(state.Pressure + intensity * NoiseIntensityMultiplier, 0, MaxPressure);
            state.Receipts.Add(receiptId);
            if (state.Receipts.Count > MaxReceipts) state.Receipts.RemoveAt(0);
            EvaluateMigration(cellId); PressureChanged?.Invoke(cellId);
        }
        public CellPressureState GetPressure(string id)
        {
            if (!pressures.TryGetValue(id, out var state)) throw new ArgumentException("Unknown population cell: " + id);
            state.ApplyDecay(clock.Simulation.Seconds, DecayRatePerSecond); return state;
        }
        public PopulationLedger GetLedger(string id) => ledgers.TryGetValue(id, out var ledger) ? ledger : throw new ArgumentException("Unknown population cell: " + id);
        public int TotalConservation(string id)
        {
            var l = GetLedger(id); int total = l.Logical + l.Physical + l.Dead;
            foreach (var m in migrations) if (m.SourceCellId == id) total += m.Size;
            return total; // Per-cell accounting is not invariant after arrivals; use the global sum.
        }
        public int TotalAccounted
        {
            get { int total = 0; foreach (var l in ledgers.Values) total += l.Logical + l.Physical + l.Dead;
                foreach (var m in migrations) total += m.Size; return total; }
        }
        void EvaluateMigration(string target)
        {
            if (pressures[target].Pressure < .3f) return;
            var coordinate = Parse(target);
            // Fixed ordering makes the one adjacent source per accepted event explainable.
            foreach (var id in new[] { new CellCoordinate(coordinate.x + 1, coordinate.z).Id,
                new CellCoordinate(coordinate.x - 1, coordinate.z).Id, new CellCoordinate(coordinate.x, coordinate.z + 1).Id,
                new CellCoordinate(coordinate.x, coordinate.z - 1).Id })
            {
                if (!ledgers.TryGetValue(id, out var source)) continue;
                int available = source.Logical;
                foreach (var unit in dormant) if (unit.cellId == id) available--; // Retain wounded identities at their owner.
                if (available <= 0) continue;
                int size = Math.Min(3, available); double now = clock.Simulation.Seconds;
                if (!WorldTimeSettings.ValidTime(now + TravelSecondsPerCell)) return;
                source.Logical -= size;
                migrations.Add(new MigrationGroup { GroupId = Guid.NewGuid().ToString("N"), SourceCellId = id,
                    TargetCellId = target, DepartureTime = now, ArrivalTime = now + TravelSecondsPerCell, Size = size });
                return;
            }
        }
        static CellCoordinate Parse(string id)
        { var parts = id.Split(':'); return new CellCoordinate(int.Parse(parts[1]), int.Parse(parts[2])); }
        public double NextBoundary(double now)
        {
            double next = double.PositiveInfinity;
            foreach (var m in migrations) if (m.ArrivalTime > now) next = Math.Min(next, m.ArrivalTime);
            return next;
        }
        public void ApplyElapsed(double from, double to)
        {
            if (!Current) return;
            foreach (var p in pressures.Values) p.ApplyDecay(to, DecayRatePerSecond);
            for (int i = migrations.Count - 1; i >= 0; --i)
            {
                var m = migrations[i]; if (m.ArrivalTime > to) continue;
                GetLedger(m.TargetCellId).Logical += m.Size; migrations.RemoveAt(i); nextSearch = 0;
            }
        }
        public AdvanceReason Inspect(double now)
        {
            if (!Current) return AdvanceReason.Completed;
            ApplyElapsed(now, now);
            // An arrival at the occupied cell is a sleep boundary even if visibility defers its actor.
            return clock.Sleeping && ledgers.TryGetValue(cells.CurrentCell, out var l) && l.Logical + l.Physical > 0
                ? AdvanceReason.ThreatNearby : AdvanceReason.Completed;
        }
        void Update()
        {
            if (!Current || flow.Restoring || flow.Paused || flow.PlayerDead || clock.Sleeping) return;
            if (clock.Simulation.Seconds < nextSearch) return;
            nextSearch = clock.Simulation.Seconds + 1; // Bounded retry for camera movement/blocked candidates; no global scan.
            TryMaterialize();
        }
        public bool TryMaterialize()
        {
            MaterializationState = "Deferred";
            if (!Current || !materializationEnabled || flow.Restoring || flow.Paused || flow.PlayerDead || !zombiePrefab ||
                cells.State(cells.CurrentCell) != CellState.Ready) return false;
            var content = cells.Content(cells.CurrentCell);
            if (!content || !content.NavigationReady || !content.CollisionReady()) return false;
            string owner = cells.CurrentCell; var ledger = GetLedger(owner);
            if (ledger.Logical <= 0 || !flow.Player) return false;
            var camera = flow.Player.GetComponentInChildren<Camera>(); if (!camera) return false;
            var agent = zombiePrefab.GetComponent<NavMeshAgent>();
            GeometryUtility.CalculateFrustumPlanes(camera, planes);
            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 20;
                var candidate = flow.Player.transform.position + new Vector3(offset.x, 0, offset.y);
                if (!NavMesh.SamplePosition(candidate, out var hit, 2, new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask })) continue;
                if (CellCoordinate.FromWorld(hit.position).Id != owner || Vector3.Distance(hit.position, flow.Player.transform.position) <= 10) continue;
                if (!content.ground.Raycast(new Ray(hit.position + Vector3.up, Vector3.down), out var ground, 2) || Mathf.Abs(ground.point.y - hit.position.y) > .2f) continue;
                var center = hit.position + Vector3.up * agent.height * .5f;
                if (GeometryUtility.TestPlanesAABB(planes, new Bounds(center, new Vector3(agent.radius * 2, agent.height, agent.radius * 2)))) continue;
                if (Physics.CheckCapsule(hit.position + Vector3.up * (agent.radius + .1f),
                    hit.position + Vector3.up * (agent.height - agent.radius), agent.radius, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
                var actor = Instantiate(zombiePrefab, hit.position, Quaternion.identity);
                if (!actor.Initialize() || !actor.Bind(flow.Player)) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); continue; }
                PopulationActorSnapshot saved = null;
                for (int d = 0; d < dormant.Count; d++) if (dormant[d].cellId == owner) { saved = dormant[d]; dormant.RemoveAt(d); break; }
                var health = actor.GetComponent<ZombieHealth>(); if (saved != null) health.RestoreHealth(saved.health);
                var record = new Physical { Id = saved?.id ?? Guid.NewGuid().ToString("N"), Cell = owner, Health = health };
                long token = generation;
                record.Died = () => OnZombieDied(actor, token);
                physical.Add(actor, record); health.Died += record.Died;
                ledger.Logical--; ledger.Physical++; MaterializationState = "Materialized " + owner; return true;
            }
            return false;
        }
        void OnZombieDied(ZombieController actor, long token)
        {
            if (!Current || token != generation || !physical.TryGetValue(actor, out var record)) return;
            record.Health.Died -= record.Died; physical.Remove(actor);
            var ledger = GetLedger(record.Cell); ledger.Physical--; ledger.Dead++;
            // Terminal authority is committed before disabling the actor; no corpse can regain population authority.
            actor.Shutdown(); actor.gameObject.SetActive(false); Destroy(actor.gameObject);
        }
        void OnCellReady(string id) { nextSearch = 0; }
        public void DematerializeCell(string id)
        {
            removal.Clear(); foreach (var pair in physical) if (pair.Value.Cell == id) removal.Add(pair.Key);
            foreach (var actor in removal)
            {
                var record = physical[actor]; record.Health.Died -= record.Died;
                dormant.Add(new PopulationActorSnapshot { id = record.Id, cellId = record.Cell, health = record.Health.CurrentHealth });
                var ledger = GetLedger(record.Cell); ledger.Physical--; ledger.Logical++;
                physical.Remove(actor); actor.Shutdown(); actor.gameObject.SetActive(false); Destroy(actor.gameObject);
            }
            removal.Clear();
        }
        public PopulationSnapshot GetSaveSnapshot()
        {
            var p = new List<CellPressureSnapshot>();
            foreach (var state in pressures.Values)
            {
                state.ApplyDecay(clock.Simulation.Seconds, DecayRatePerSecond);
                var receipts = new List<NoiseReceiptSnapshot>(); foreach (var id in state.Receipts) receipts.Add(new NoiseReceiptSnapshot { id = id });
                p.Add(new CellPressureSnapshot { cellId = state.CellId, pressure = state.Pressure, lastUpdateTime = state.LastUpdateTime, receipts = receipts.ToArray() });
            }
            var l = new List<PopulationLedgerSnapshot>(); foreach (var state in ledgers.Values)
                l.Add(new PopulationLedgerSnapshot { cellId = state.CellId, logical = state.Logical + state.Physical, dead = state.Dead });
            var m = new List<MigrationGroupSnapshot>(); foreach (var state in migrations)
                m.Add(new MigrationGroupSnapshot { groupId = state.GroupId, sourceCellId = state.SourceCellId, targetCellId = state.TargetCellId,
                    departureTime = state.DepartureTime, arrivalTime = state.ArrivalTime, size = state.Size });
            var units = new List<PopulationActorSnapshot>(); foreach (var unit in dormant)
                units.Add(new PopulationActorSnapshot { id = unit.id, cellId = unit.cellId, health = unit.health });
            foreach (var pair in physical) units.Add(new PopulationActorSnapshot { id = pair.Value.Id, cellId = pair.Value.Cell, health = pair.Value.Health.CurrentHealth });
            return new PopulationSnapshot { pressures = p.ToArray(), ledgers = l.ToArray(), migrations = m.ToArray(), actors = units.ToArray(), lastNoiseSequence = lastNoiseSequence };
        }
        public void SyncFromSave(PopulationSnapshot snap)
        {
            if (snap == null) return; // Supported legacy cells start with the explicitly authored population.
            var ids = new HashSet<string>(ledgers.Keys, StringComparer.Ordinal);
            if (!SaveValidation.ValidPopulation(snap, ids, clock.Simulation.Seconds)) throw new InvalidOperationException("Invalid population snapshot.");
            // Validate detached data before replacing any runtime authority.
            ClearActors(); pressures.Clear(); ledgers.Clear(); migrations.Clear(); dormant.Clear();
            foreach (var p in snap.pressures) pressures.Add(p.cellId, new CellPressureState { CellId = p.cellId, Pressure = p.pressure, LastUpdateTime = p.lastUpdateTime,
                Receipts = new List<string>(Array.ConvertAll(p.receipts, r => r.id)) });
            foreach (var l in snap.ledgers) ledgers.Add(l.cellId, new PopulationLedger { CellId = l.cellId, Logical = l.logical, Dead = l.dead });
            foreach (var m in snap.migrations) migrations.Add(new MigrationGroup { GroupId = m.groupId, SourceCellId = m.sourceCellId, TargetCellId = m.targetCellId,
                DepartureTime = m.departureTime, ArrivalTime = m.arrivalTime, Size = m.size });
            foreach (var unit in snap.actors) dormant.Add(new PopulationActorSnapshot { id = unit.id, cellId = unit.cellId, health = unit.health });
            lastNoiseSequence = snap.lastNoiseSequence; nextSearch = 0;
        }
        void ClearActors()
        {
            foreach (var pair in physical)
            { if (pair.Value.Health) pair.Value.Health.Died -= pair.Value.Died;
                if (pair.Key) { pair.Key.Shutdown(); pair.Key.gameObject.SetActive(false); Destroy(pair.Key.gameObject); } }
            physical.Clear();
        }
        public void ClearSession()
        {
            active = false; simulation?.Unregister(this); simulation = null;
            ClearActors(); dormant.Clear(); pressures.Clear(); ledgers.Clear(); migrations.Clear(); lastNoiseSequence = 0; nextSearch = 0;
        }
        void OnDestroy()
        { ClearSession(); if (cells) { cells.CellReady -= OnCellReady; cells.CellLeaving -= DematerializeCell; cells.CellEntered -= OnCellReady; } }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] bool showDebug;
        void OnGUI()
        {
            if (!showDebug || !Current) return;
            GUILayout.BeginArea(new Rect(10, 220, 650, 500), GUI.skin.box);
            GUILayout.Label($"Population: {TotalAccounted}, receipts through {lastNoiseSequence}; {MaterializationState}");
            foreach (var p in pressures.Values) { var l = ledgers[p.CellId];
                GUILayout.Label($"{p.CellId}: P={p.Pressure:F3} at {p.LastUpdateTime:F1}; L={l.Logical} P={l.Physical} D={l.Dead}; {string.Join(",", p.Receipts)}"); }
            foreach (var m in migrations) GUILayout.Label($"{m.SourceCellId} → {m.TargetCellId}: {m.Size}, departure {m.DepartureTime:F1}, ETA {m.ArrivalTime:F1}");
            GUILayout.EndArea();
        }
#endif
    }
}
