using System;
using System.Collections.Generic;
using LastSignal.WorldCells;
using LastSignal.Persistence;
using LastSignal.WorldTime;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.AI
{
    [Serializable]
    public sealed class CellPressureState
    {
        public string CellId;
        public float Pressure;
        public double LastUpdateTime;
        public List<string> Receipts = new List<string>();
        
        public void ApplyDecay(double now, float decayRatePerSecond)
        {
            if (now <= LastUpdateTime) return;
            float elapsed = (float)(now - LastUpdateTime);
            Pressure = Mathf.Max(0f, Pressure - elapsed * decayRatePerSecond);
            LastUpdateTime = now;
        }
    }

    [Serializable]
    public sealed class MigrationGroup
    {
        public string GroupId;
        public string SourceCellId;
        public string TargetCellId;
        public double DepartureTime;
        public double ArrivalTime;
        public int Size;
    }

    [Serializable]
    public sealed class PopulationLedger
    {
        public string CellId;
        public int Logical;
        public int Dead;
        [NonSerialized] public int Physical;
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorldCellManager), typeof(WorldClock))]
    public sealed class WorldPopulationManager : MonoBehaviour
    {
        public float DecayRatePerSecond = 0.001f;
        public float MaxPressure = 1.0f;
        public float NoiseIntensityMultiplier = 0.2f;
        public double TravelSecondsPerCell = 120.0; // 2 world minutes
        public int MaxReceipts = 10;
        
        [SerializeField] ZombieController zombiePrefab;
        
        readonly Dictionary<string, CellPressureState> pressures = new Dictionary<string, CellPressureState>(StringComparer.Ordinal);
        readonly Dictionary<string, PopulationLedger> ledgers = new Dictionary<string, PopulationLedger>(StringComparer.Ordinal);
        readonly List<MigrationGroup> migrations = new List<MigrationGroup>();
        readonly HashSet<ZombieController> activePhysical = new HashSet<ZombieController>();
        
        WorldCellManager cells;
        WorldClock clock;
        
        public event Action<string> PressureChanged;
        
        void Awake()
        {
            cells = GetComponent<WorldCellManager>();
            clock = GetComponent<WorldClock>();
            cells.CellReady += OnCellReady;
        }

        public void ReportNoise(string receiptId, string cellId, float intensity)
        {
            if (string.IsNullOrEmpty(receiptId) || string.IsNullOrEmpty(cellId)) return;
            var state = GetPressure(cellId);
            if (state.Receipts.Contains(receiptId)) return; // Idempotent
            
            state.ApplyDecay(clock?.Simulation?.Seconds ?? 0, DecayRatePerSecond);
            state.Pressure = Mathf.Min(MaxPressure, state.Pressure + intensity * NoiseIntensityMultiplier);
            state.Receipts.Add(receiptId);
            if (state.Receipts.Count > MaxReceipts) state.Receipts.RemoveAt(0);
            
            PressureChanged?.Invoke(cellId);
            EvaluateMigration(cellId);
        }

        public CellPressureState GetPressure(string cellId)
        {
            if (!pressures.TryGetValue(cellId, out var state))
            {
                state = new CellPressureState { CellId = cellId, LastUpdateTime = clock?.Simulation?.Seconds ?? 0 };
                pressures[cellId] = state;
            }
            else
            {
                state.ApplyDecay(clock?.Simulation?.Seconds ?? 0, DecayRatePerSecond);
            }
            return state;
        }

        public PopulationLedger GetLedger(string cellId)
        {
            if (!ledgers.TryGetValue(cellId, out var ledger))
            {
                // In a real project, initial population might come from authored data.
                // We'll give 15 default logical zombies to any requested ledger for this test.
                ledger = new PopulationLedger { CellId = cellId, Logical = 15, Dead = 0, Physical = 0 };
                ledgers[cellId] = ledger;
            }
            return ledger;
        }
        
        public int TotalConservation(string cellId)
        {
            var l = GetLedger(cellId);
            int total = l.Logical + l.Physical + l.Dead;
            foreach (var m in migrations)
            {
                if (m.SourceCellId == cellId) total += m.Size;
            }
            return total;
        }

        void EvaluateMigration(string targetCellId)
        {
            var p = GetPressure(targetCellId);
            if (p.Pressure < 0.3f) return;
            
            // Find adjacent cell to pull from
            string[] adjacent = GetAdjacent(targetCellId);
            foreach (var adj in adjacent)
            {
                var ledger = GetLedger(adj);
                if (ledger.Logical > 0)
                {
                    int migrateCount = Mathf.Min(3, ledger.Logical);
                    ledger.Logical -= migrateCount;
                    
                    migrations.Add(new MigrationGroup
                    {
                        GroupId = Guid.NewGuid().ToString(),
                        SourceCellId = adj,
                        TargetCellId = targetCellId,
                        DepartureTime = clock?.Simulation?.Seconds ?? 0,
                        ArrivalTime = (clock?.Simulation?.Seconds ?? 0) + TravelSecondsPerCell,
                        Size = migrateCount
                    });
                    // Only pull from one cell per event
                    break;
                }
            }
        }
        
        string[] GetAdjacent(string cellId)
        {
            string[] parts = cellId.Replace("cell:", "").Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int z)) return new string[0];
            return new string[]
            {
                $"cell:{x+1}:{z}",
                $"cell:{x-1}:{z}",
                $"cell:{x}:{z+1}",
                $"cell:{x}:{z-1}"
            };
        }

        void Update()
        {
            if (!clock || clock.Simulation == null) return;
            double now = clock.Simulation.Seconds;
            
            // Process migrations
            for (int i = migrations.Count - 1; i >= 0; i--)
            {
                var m = migrations[i];
                if (now >= m.ArrivalTime)
                {
                    // Arrived
                    var targetLedger = GetLedger(m.TargetCellId);
                    targetLedger.Logical += m.Size;
                    migrations.RemoveAt(i);
                }
            }
            
            TryMaterialize();
        }

        void TryMaterialize()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("WorldTimeAcceptance")) return;
            if (!cells || string.IsNullOrEmpty(cells.CurrentCell) || cells.CurrentCell == "resident" || !zombiePrefab) return;
            var currentContent = cells.Content(cells.CurrentCell);
            if (!currentContent || !currentContent.NavigationReady || !currentContent.CollisionReady()) return;
            
            var ledger = GetLedger(cells.CurrentCell);
            if (ledger.Logical <= 0) return;
            
            var flow = GetComponent<SessionFlow>();
            if (!flow || !flow.Player) return;
            var playerCamera = flow.Player.GetComponentInChildren<Camera>();
            if (!playerCamera) return;
            
            // Try to find a visibility-safe spawn point on NavMesh
            for (int i = 0; i < 5; i++)
            {
                Vector2 rand = UnityEngine.Random.insideUnitCircle * 20f;
                Vector3 candidate = flow.Player.transform.position + new Vector3(rand.x, 0, rand.y);
                if (NavMesh.SamplePosition(candidate, out var hit, 5f, NavMesh.AllAreas))
                {
                    Vector3 viewPos = playerCamera.WorldToViewportPoint(hit.position);
                    bool inView = viewPos.x >= -0.2f && viewPos.x <= 1.2f && viewPos.y >= -0.2f && viewPos.y <= 1.2f && viewPos.z > 0;
                    
                    if (!inView && Vector3.Distance(hit.position, flow.Player.transform.position) > 10f)
                    {
                        // Spawn
                        ledger.Logical--;
                        ledger.Physical++;
                        
                        var z = Instantiate(zombiePrefab, hit.position, Quaternion.identity);
                        if (z.Initialize())
                        {
                            z.Bind(flow.Player);
                            z.GetComponent<ZombieHealth>().Died += () => OnZombieDied(z, cells.CurrentCell);
                            activePhysical.Add(z);
                        }
                        else
                        {
                            Destroy(z.gameObject);
                            ledger.Logical++;
                            ledger.Physical--;
                        }
                        return; // Only spawn one per frame
                    }
                }
            }
        }

        void OnZombieDied(ZombieController z, string cellId)
        {
            if (activePhysical.Remove(z))
            {
                var ledger = GetLedger(cellId);
                ledger.Physical--;
                ledger.Dead++;
            }
        }

        void OnCellReady(string cellId)
        {
            // Optional: immediately try to evaluate if we should dematerialize old cell zombies?
            // "When leaving simulation range: physical -> logical".
            // If cell changes, we should dematerialize zombies not in the current cell.
            DematerializeFarZombies();
        }

        void DematerializeFarZombies()
        {
            string cur = cells.CurrentCell;
            List<ZombieController> toRemove = new List<ZombieController>();
            foreach (var z in activePhysical)
            {
                // Simple distance check or check bounds
                var c = CellCoordinate.FromWorld(z.transform.position);
                if (c.Id != cur)
                {
                    toRemove.Add(z);
                }
            }
            foreach (var z in toRemove)
            {
                activePhysical.Remove(z);
                var ledger = GetLedger(CellCoordinate.FromWorld(z.transform.position).Id);
                ledger.Physical--;
                ledger.Logical++;
                z.Shutdown();
                Destroy(z.gameObject);
            }
        }
        
        public void SyncFromSave(PopulationSnapshot snap)
        {
            pressures.Clear();
            ledgers.Clear();
            migrations.Clear();
            
            if (snap == null) return;
            
            if (snap.pressures != null)
                foreach (var p in snap.pressures)
                    pressures[p.cellId] = new CellPressureState { CellId = p.cellId, Pressure = p.pressure, LastUpdateTime = p.lastUpdateTime, Receipts = new List<string>(Array.ConvertAll(p.receipts ?? new NoiseReceiptSnapshot[0], r => r.id)) };
                    
            if (snap.ledgers != null)
                foreach (var l in snap.ledgers)
                    ledgers[l.cellId] = new PopulationLedger { CellId = l.cellId, Logical = l.logical, Dead = l.dead, Physical = 0 };
                    
            if (snap.migrations != null)
                foreach (var m in snap.migrations)
                    migrations.Add(new MigrationGroup { GroupId = m.groupId, SourceCellId = m.sourceCellId, TargetCellId = m.targetCellId, DepartureTime = m.departureTime, ArrivalTime = m.arrivalTime, Size = m.size });
        }
        
        public PopulationSnapshot GetSaveSnapshot()
        {
            // Before save, dematerialize all to ensure state is clean or just save physical as logical?
            // "When leaving simulation range: physical authority -> logical authority". 
            // In a save, we just save them. If we save physical, we should convert them to logical for the snapshot, or persist physical coords.
            // Requirement D083 says: "Persist only authoritative S009 state... group state... death/consumed".
            // Let's fold physical into logical for the snapshot so they resume as logical on load.
            var popSnap = new PopulationSnapshot();
            
            var pList = new List<CellPressureSnapshot>();
            foreach (var kvp in pressures)
            {
                var rList = new List<NoiseReceiptSnapshot>();
                foreach (var r in kvp.Value.Receipts) rList.Add(new NoiseReceiptSnapshot { id = r });
                pList.Add(new CellPressureSnapshot { cellId = kvp.Key, pressure = kvp.Value.Pressure, lastUpdateTime = kvp.Value.LastUpdateTime, receipts = rList.ToArray() });
            }
            popSnap.pressures = pList.ToArray();
            
            var lList = new List<PopulationLedgerSnapshot>();
            foreach (var kvp in ledgers)
            {
                lList.Add(new PopulationLedgerSnapshot { cellId = kvp.Key, logical = kvp.Value.Logical + kvp.Value.Physical, dead = kvp.Value.Dead });
            }
            popSnap.ledgers = lList.ToArray();
            
            var mList = new List<MigrationGroupSnapshot>();
            foreach (var m in migrations)
            {
                mList.Add(new MigrationGroupSnapshot { groupId = m.GroupId, sourceCellId = m.SourceCellId, targetCellId = m.TargetCellId, departureTime = m.DepartureTime, arrivalTime = m.ArrivalTime, size = m.Size });
            }
            popSnap.migrations = mList.ToArray();
            
            return popSnap;
        }

        public void ClearSession()
        {
            foreach (var z in activePhysical)
            {
                z.Shutdown();
                Destroy(z.gameObject);
            }
            activePhysical.Clear();
            pressures.Clear();
            ledgers.Clear();
            migrations.Clear();
        }
        
        void OnDestroy()
        {
            if (cells) cells.CellReady -= OnCellReady;
        }
    }
}
