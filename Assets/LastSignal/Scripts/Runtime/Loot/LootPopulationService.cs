using System;
using System.Collections.Generic;
using LastSignal.Inventory;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Loot
{
    public readonly struct LootPopulationResult
    {
        public readonly string PointId;
        public readonly LootSelection Selection;
        public readonly string Diagnostic;
        public LootPopulationResult(string id, LootSelection selection, string diagnostic = null)
        { PointId = id; Selection = selection; Diagnostic = diagnostic; }
    }

    [DisallowMultipleComponent]
    public sealed class LootPopulationService : MonoBehaviour
    {
        [SerializeField] bool overrideSeed;
        [SerializeField] int explicitSeed = 12345;
        readonly List<LootPopulationResult> results = new List<LootPopulationResult>();
        readonly HashSet<WorldItem> preexisting = new HashSet<WorldItem>();
        readonly Dictionary<string,bool> consumedLoot = new Dictionary<string,bool>(StringComparer.Ordinal);
        internal bool TryGetConsumed(string id, out bool consumed) => consumedLoot.TryGetValue(id,out consumed);
        void RecordQuantity(string id,int quantity) { consumedLoot[id] = quantity <= 0; }
        GameObject runtimeRoot;
        Transform contentScope;
        bool populated;
        public int Seed { get; private set; }
        public bool Populated => populated;
        public IReadOnlyList<LootPopulationResult> Results => results.AsReadOnly();
        public int GeneratedCount { get; private set; }
        public double SpawnMilliseconds { get; private set; }
        static readonly ProfilerMarker PopulateMarker = new ProfilerMarker("LastSignal.Loot.Populate");
        static readonly ProfilerMarker SpawnMarker = new ProfilerMarker("LastSignal.Loot.Instantiate");
        const int WorldMask = ~((1 << 2) | (1 << 8));

        public void Begin(GameObject player, int? seed = null, Transform scope = null)
        {
            if (populated) return;
            contentScope = scope;
            using (PopulateMarker.Auto())
            {
                populated = true;
                Seed = seed ?? (overrideSeed ? explicitSeed : BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
                results.Clear(); preexisting.Clear(); consumedLoot.Clear(); GeneratedCount = 0; SpawnMilliseconds = 0;
                var points = new List<LootSpawnPoint>();
                foreach (var root in OwnedRoots())
                {
                    points.AddRange(root.GetComponentsInChildren<LootSpawnPoint>(false));
                    foreach (var item in root.GetComponentsInChildren<WorldItem>(true)) preexisting.Add(item);
                }
                points.Sort((a,b) => StringComparer.Ordinal.Compare(a.StableId,b.StableId));
                var counts = new Dictionary<string,int>(StringComparer.Ordinal);
                foreach (var p in points) { string id=p.StableId ?? ""; counts.TryGetValue(id,out int n); counts[id]=n+1; }
                runtimeRoot = new GameObject("Session Loot");
                SceneManager.MoveGameObjectToScene(runtimeRoot, gameObject.scene);
                if (contentScope) runtimeRoot.transform.SetParent(contentScope, true);
                Physics.SyncTransforms();
                foreach (var point in points)
                {
                    if (!point.isActiveAndEnabled) continue;
                    string error = null;
                    if (counts[point.StableId ?? ""] > 1) error = "Duplicate point ID; all matching points rejected.";
                    else point.Validate(out error);
                    if (error != null) { Record(point,new LootSelection(LootOutcome.Invalid),error); continue; }
                    var selection = point.Profile.Select(Seed,point.StableId);
                    if (selection.Outcome != LootOutcome.Spawned) { Record(point,selection); continue; }
                    if (!PlacementValid(point,selection.Item.WorldPrefab,player,out error))
                    { Record(point,new LootSelection(LootOutcome.Blocked),error); continue; }
                    var start = System.Diagnostics.Stopwatch.GetTimestamp();
                    using (SpawnMarker.Auto())
                    {
                        var go = Instantiate(selection.Item.WorldPrefab,point.transform.position,point.transform.rotation,runtimeRoot.transform);
                        var worldItem = go.GetComponent<WorldItem>();
                        worldItem.Configure(selection.Item,selection.Quantity);
                        worldItem.AssignPersistentIdentity("loot:" + point.StableId, LastSignal.Persistence.WorldItemOrigin.Loot);
                        consumedLoot.Add(worldItem.PersistentId,false); worldItem.BindPersistence(RecordQuantity);
                    }
                    SpawnMilliseconds += (System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
                    GeneratedCount++;
                    Physics.SyncTransforms(); // Each following point must see already committed colliders.
                    Record(point,selection);
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"S006 loot seed={Seed} resolved={results.Count} generated={GeneratedCount}",this);
#endif
            }
        }
        void Record(LootSpawnPoint point,LootSelection selection,string error=null)
        {
            results.Add(new LootPopulationResult(point.StableId,selection,error));
            if (error != null) Debug.LogWarning($"S006 loot '{point.StableId}': {error}",point);
        }

        public static bool PlacementValid(LootSpawnPoint point,GameObject prefab,GameObject player,out string error)
        {
            error = null;
            var box = prefab.GetComponent<BoxCollider>();
            // S005 seed wrappers use one passive, centered root box. Reject unsupported footprints explicitly.
            if (!box || !box.enabled || box.isTrigger || box.center != Vector3.zero || prefab.transform.localScale != Vector3.one ||
                prefab.GetComponentsInChildren<Collider>(true).Length != 1 || prefab.GetComponentInChildren<Rigidbody>() ||
                prefab.GetComponentInChildren<UnityEngine.AI.NavMeshObstacle>())
            { error="Prefab needs one centered passive root BoxCollider and unit scale.";return false; }
            Vector3 half = box.size*.5f;
            Vector3 clear = point.Clearance;
            if (half.x<=0 || half.y<=0 || half.z<=0 || half.x>clear.x || half.y>clear.y || half.z>clear.z || Mathf.Abs(half.y-clear.y)>.002f)
            {error="Prefab footprint does not fit point clearance/support height.";return false;}
            Vector3 center=point.transform.position;
            if (!Physics.Raycast(center,Vector3.down,out var hit,clear.y+.025f,WorldMask,QueryTriggerInteraction.Ignore) ||
                Mathf.Abs(hit.distance-clear.y)>.02f || hit.normal.y<.95f || hit.collider.GetComponentInParent<WorldItem>())
            {error="Missing flat support at authored height (2 cm tolerance).";return false;}
            if (Physics.CheckBox(center+Vector3.up*.006f,new Vector3(clear.x,clear.y-.005f,clear.z),point.transform.rotation,WorldMask,QueryTriggerInteraction.Ignore))
            {error="Clearance overlaps solid geometry or existing loot.";return false;}
            if (player)
            {
                var capsule=player.GetComponent<CharacterController>();
                if (capsule && capsule.bounds.Intersects(new Bounds(center,clear*2+Vector3.one*.2f)))
                {error="Point overlaps player spawn clearance.";return false;}
            }
            return true;
        }
        public void End()
        {
            if (!populated) return;
            // One teardown scan owns newly created world items (including S005 drops), excluding authored entry objects.
            if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
                foreach (var root in OwnedRoots())
                    foreach (var item in root.GetComponentsInChildren<WorldItem>(true))
                        if (item && !preexisting.Contains(item)) { item.gameObject.SetActive(false); Destroy(item.gameObject); }
            if (runtimeRoot) { runtimeRoot.SetActive(false); Destroy(runtimeRoot); }
            runtimeRoot=null; preexisting.Clear(); results.Clear(); consumedLoot.Clear(); GeneratedCount=0; populated=false;
        }
        internal void Restore(LastSignal.Persistence.SaveGame save, LastSignal.Inventory.Data.ItemCatalog catalog, Transform scope = null)
        {
            if (populated) throw new InvalidOperationException("Restore must precede loot population.");
            contentScope = scope;
            runtimeRoot = new GameObject("Session Loot"); runtimeRoot.SetActive(false);
            SceneManager.MoveGameObjectToScene(runtimeRoot, gameObject.scene);
                if (contentScope) runtimeRoot.transform.SetParent(contentScope, true);
            Seed = save.header.seed; results.Clear(); preexisting.Clear(); consumedLoot.Clear(); GeneratedCount = 0;
            var byId = new Dictionary<string, LastSignal.Persistence.WorldItemSnapshot>(StringComparer.Ordinal);
            foreach (var item in save.world.items)
            {
                byId.Add(item.id, item);
                if(item.origin==LastSignal.Persistence.WorldItemOrigin.Loot)
                    consumedLoot.Add(item.id,item.disposition==LastSignal.Persistence.EntityDisposition.Consumed);
            }
            // Mark ownership before fallible instantiation so End() can clean an interrupted hydrate.
            populated = true;
            foreach (var opportunity in save.world.opportunities)
            {
                if (opportunity.outcome == LastSignal.Persistence.OpportunityOutcome.Generated)
                {
                    var item = byId[opportunity.entityId];
                    var definition = catalog.GetItem(new LastSignal.Inventory.Data.StableItemId(item.definitionId));
                    results.Add(new LootPopulationResult(opportunity.id, new LootSelection(LootOutcome.Spawned, definition, item.quantity)));
                    GeneratedCount++;
                }
                else results.Add(new LootPopulationResult(opportunity.id, new LootSelection(
                    opportunity.outcome == LastSignal.Persistence.OpportunityOutcome.Empty ? LootOutcome.Empty : LootOutcome.Blocked)));
            }
            foreach (var item in save.world.items)
            {
                if (item.disposition == LastSignal.Persistence.EntityDisposition.Consumed) continue;
                var definition = catalog.GetItem(new LastSignal.Inventory.Data.StableItemId(item.definitionId));
                var pose = item.transform;
                var go = Instantiate(definition.WorldPrefab, new Vector3(pose.x,pose.y,pose.z), new Quaternion(pose.qx,pose.qy,pose.qz,pose.qw), runtimeRoot.transform);
                var worldItem = go.GetComponent<WorldItem>(); worldItem.Configure(definition,item.quantity);
                worldItem.AssignPersistentIdentity(item.id,item.origin);
                if(item.origin==LastSignal.Persistence.WorldItemOrigin.Loot) worldItem.BindPersistence(RecordQuantity);
            }
            runtimeRoot.SetActive(true);
        }
        IEnumerable<GameObject> OwnedRoots()
        {
            if (contentScope) { yield return contentScope.gameObject; yield break; }
            foreach (var root in gameObject.scene.GetRootGameObjects())
                if (!root.GetComponent<LastSignal.WorldCells.WorldCellContent>()) yield return root;
        }
        void OnDestroy() => End();
    }
}
