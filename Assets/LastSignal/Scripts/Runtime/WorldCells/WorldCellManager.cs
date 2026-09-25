using System;
using System.Collections;
using System.Collections.Generic;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using UnityEngine;

namespace LastSignal.WorldCells
{
    [DisallowMultipleComponent, RequireComponent(typeof(SessionFlow), typeof(WorldClock))]
    public sealed class WorldCellManager : MonoBehaviour
    {
        [SerializeField] WorldCellContent[] definitions;
        [SerializeField] ItemCatalog catalog;
        sealed class Entry
        {
            public WorldCellContent Definition, Runtime;
            public CellSnapshot Delta;
            public readonly CellLifecycle Life = new CellLifecycle();
            public CellToken Token;
            public double Latency, Elapsed;
        }
        readonly Dictionary<string, Entry> cells = new Dictionary<string, Entry>(StringComparer.Ordinal);
        SessionFlow flow;
        WorldClock clock;
        bool active;
        public event Action<string> CellReady;
        public event Action<string> CellLeaving;
        public event Action<string> CellEntered;
        public IEnumerable<string> DefinedCellIds => cells.Keys;
        public bool ContainsCell(string id) => id != null && cells.ContainsKey(id);
        public string CurrentCell { get; private set; } = "resident";
        public string Failure { get; private set; }
        public int ReadyListenerCount => CellReady?.GetInvocationList().Length ?? 0;
        public bool Stable
        {
            get
            {
                if (!active || OwnershipTransaction.Active) return false;
                foreach (var cell in cells.Values) if (cell.Life.State != CellState.Ready && cell.Life.State != CellState.Unloaded) return false;
                return true;
            }
        }
        public void Configure(WorldCellContent[] content, ItemCatalog items) { definitions = content; catalog = items; }
        public void Begin(bool restoring)
        {
            End(); flow = GetComponent<SessionFlow>(); clock = GetComponent<WorldClock>(); active = true; CurrentCell = "resident"; Failure = null;
            foreach (var definition in definitions)
            {
                if (!definition || !catalog) throw new InvalidOperationException("Missing cell definition/catalog.");
                cells.Add(definition.coordinate.Id, new Entry { Definition = definition,
                    Delta = new CellSnapshot { id = definition.coordinate.Id, lastProcessed = clock.Simulation.Seconds } });
            }
        }
        public CellState State(string id) => cells.TryGetValue(id, out var cell) ? cell.Life.State : CellState.Failed;
        public WorldCellContent Content(string id) => cells.TryGetValue(id, out var cell) ? cell.Runtime : null;
        public double LoadMilliseconds(string id) => cells[id].Latency;
        public double LastElapsed(string id) => cells[id].Elapsed;
        public bool Request(string id)
        {
            if (!active || !cells.TryGetValue(id, out var cell)) { Failure = "Unknown cell ID."; return false; }
            if (cell.Life.State == CellState.Ready || cell.Life.State == CellState.Requested || cell.Life.State == CellState.Loading || cell.Life.State == CellState.Restoring) return true;
            if (cell.Life.State != CellState.Unloaded && cell.Life.State != CellState.Failed) return false;
            cell.Token = cell.Life.Request(flow.Generation); StartCoroutine(Load(cell, cell.Token)); return true;
        }
        bool Current(Entry cell, CellToken token) => active && flow && flow.Generation == token.Session && cell.Life.Current(token);
        IEnumerator Load(Entry cell, CellToken token)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            yield return null; // Requested remains observable; cancellation precedes any creation.
            if (!Current(cell, token)) yield break;
            cell.Life.Move(token, CellState.Loading);
            WorldCellContent content = null;
            try
            {
                content = Instantiate(cell.Definition); cell.Runtime = content;
                content.gameObject.SetActive(true);
                content.InstallNavigation();
                foreach (var portal in content.GetComponentsInChildren<CellPortal>()) portal.Bind(this);
            }
            catch (Exception error) { Fail(cell, token, error); yield break; }
            yield return null; // Unity Awake/OnEnable and native navigation registration precede restore.
            if (!Current(cell, token)) yield break;
            cell.Life.Move(token, CellState.Restoring);
            try { content.Hydrate(cell.Delta, catalog, flow.Player, GetComponent<LastSignal.Loot.LootPopulationService>().Seed); }
            catch (Exception error) { Fail(cell, token, error); yield break; }
            yield return null;
            if (!Current(cell, token)) yield break;
            try
            {
                Physics.SyncTransforms();
                if (!content.CollisionReady() || !content.NavigationReady) throw new InvalidOperationException("Cell collision/navigation prerequisites failed.");
                var elapsed = new ElapsedWorldState { lastProcessed = cell.Delta.lastProcessed };
                cell.Elapsed = elapsed.TakeElapsed(clock.Simulation.Seconds);
                // Current cell entities have no offscreen timed behavior. Timestamp consumption is atomic with their unchanged state.
                cell.Delta.lastProcessed = elapsed.lastProcessed;
                if (!cell.Life.Complete(token, true, true, true, true, true, true)) throw new InvalidOperationException("Readiness rejected.");
                cell.Latency = watch.Elapsed.TotalMilliseconds;
            }
            catch (Exception error) { Fail(cell, token, error); yield break; }
            CellReady?.Invoke(cell.Definition.coordinate.Id);
        }
        void Fail(Entry cell, CellToken token, Exception error)
        {
            if (!Current(cell, token)) return;
            Release(cell); cell.Life.Fail(token, error.Message); Failure = error.Message;
            Debug.LogWarning("Cell load failed safely: " + error.Message, this);
        }
        public bool Cancel(string id)
        {
            if (!active || !cells.TryGetValue(id, out var cell) || CurrentCell == id) return false;
            if (cell.Life.State == CellState.Ready) return Unload(id);
            cell.Life.Invalidate(flow.Generation); Release(cell); return true;
        }
        public bool Unload(string id)
        {
            if (!active || OwnershipTransaction.Active || CurrentCell == id || !cells.TryGetValue(id, out var cell) || cell.Life.State != CellState.Ready) return false;
            try
            {
                OwnershipTransaction.Enter();
                CellLeaving?.Invoke(id);
                var snapshot = Capture(cell);
                cell.Life.Move(cell.Token, CellState.Unloading);
                cell.Delta = snapshot;
                Release(cell); // disable immediately; deferred Destroy cannot remain an authority.
                cell.Life.Move(cell.Token, CellState.Unloaded);
                return true;
            }
            catch (Exception error) { Failure = error.Message; return false; }
            finally { OwnershipTransaction.Exit(); }
        }
        static void Release(Entry cell)
        {
            if (cell.Runtime) { cell.Runtime.Release(); cell.Runtime.gameObject.SetActive(false); Destroy(cell.Runtime.gameObject); cell.Runtime = null; }
        }
        CellSnapshot Capture(Entry cell) => new CellSnapshot { id = cell.Definition.coordinate.Id, visited = true,
            lastProcessed = clock.Simulation.Seconds, world = cell.Runtime.Capture() };
        public CellWorldSnapshot Capture()
        {
            if (!Stable) throw new InvalidOperationException("Cell transition in progress.");
            var snapshots = new List<CellSnapshot>();
            foreach (var cell in cells.Values) snapshots.Add(cell.Life.State == CellState.Ready ? Capture(cell) : cell.Delta);
            snapshots.Sort((a,b) => StringComparer.Ordinal.Compare(a.id,b.id));
            // Roundtrip ensures callers never retain mutable unloaded authority.
            return JsonUtility.FromJson<CellWorldSnapshot>(JsonUtility.ToJson(new CellWorldSnapshot { playerCell = CurrentCell, cells = snapshots.ToArray() }));
        }
        public bool ValidateTopology(CellWorldSnapshot snapshot)
        {
            if (snapshot == null || snapshot.cells == null || snapshot.cells.Length != definitions.Length) return false;
            var ids = new HashSet<string>(); foreach (var definition in definitions) ids.Add(definition.coordinate.Id);
            bool destination = snapshot.playerCell == "resident";
            foreach (var cell in snapshot.cells)
            { if (cell == null || !ids.Remove(cell.id)) return false; if (cell.id == snapshot.playerCell && cell.visited) destination = true; }
            return destination;
        }
        public IEnumerator Restore(CellWorldSnapshot snapshot, double savedTime)
        {
            if (!ValidateTopology(snapshot)) { Failure = "Cell topology changed."; yield break; }
            foreach (var state in snapshot.cells)
                cells[state.id].Delta = JsonUtility.FromJson<CellSnapshot>(JsonUtility.ToJson(state));
            // WorldClock must be restored before consuming elapsed timestamps, while player input remains gated.
            if (snapshot.playerCell != "resident")
            {
                Request(snapshot.playerCell);
                while (State(snapshot.playerCell) != CellState.Ready && State(snapshot.playerCell) != CellState.Failed)
                { if (!active) yield break; yield return null; }
                if (State(snapshot.playerCell) != CellState.Ready) yield break;
            }
            CurrentCell = snapshot.playerCell;
        }
        public bool TryEnter(string id)
        {
            if (!active || flow.Restoring || flow.Paused || flow.PlayerDead || OwnershipTransaction.Active || clock.Sleeping || State(id) != CellState.Ready) return false;
            var destination = cells[id].Runtime.entry.position;
            if (!Place(destination)) return false;
            var old = CurrentCell; CurrentCell = id;
            if (old != "resident" && old != id) Unload(old);
            CellEntered?.Invoke(CurrentCell);
            clock.RefreshExposure(); return true;
        }
        public bool ReturnToResident(Vector3 position)
        {
            if (!active || flow.Restoring || flow.Paused || flow.PlayerDead || clock.Sleeping || !Place(position)) return false;
            var old = CurrentCell; CurrentCell = "resident";
            if (old != "resident") Unload(old);
            CellEntered?.Invoke(CurrentCell);
            clock.RefreshExposure(); return true;
        }
        bool Place(Vector3 position)
        {
            if (!Physics.Raycast(position + Vector3.up, Vector3.down, 2, ~((1<<8)|(1<<2)), QueryTriggerInteraction.Ignore)) return false;
            var capsule = flow.Player.GetComponent<CharacterController>();
            var center = position + capsule.center; float offset = Mathf.Max(0,capsule.height*.5f-capsule.radius);
            if (Physics.CheckCapsule(center+Vector3.up*offset,center-Vector3.up*offset,capsule.radius*.95f,~((1<<8)|(1<<2)),QueryTriggerInteraction.Ignore)) return false;
            capsule.enabled = false; flow.Player.transform.position = position; capsule.enabled = true;
            Physics.SyncTransforms(); return true;
        }
        public void AdoptDrop(WorldItem item)
        {
            var id = CellCoordinate.FromWorld(item.transform.position).Id;
            if (cells.TryGetValue(id, out var cell))
            {
                if (cell.Life.State != CellState.Ready) throw new InvalidOperationException("Drop destination is not ready.");
                item.transform.SetParent(cell.Runtime.transform, true);
            }
        }
        public void End()
        {
            active = false;
            foreach (var cell in cells.Values) { cell.Life.Invalidate(flow ? flow.Generation : 0); Release(cell); }
            cells.Clear(); StopAllCoroutines(); CurrentCell = "resident";
        }
        void OnDisable() => End();
    }
}
