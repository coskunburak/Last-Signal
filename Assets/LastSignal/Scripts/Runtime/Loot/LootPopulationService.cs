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
        GameObject runtimeRoot;
        bool populated;
        public int Seed { get; private set; }
        public bool Populated => populated;
        public IReadOnlyList<LootPopulationResult> Results => results.AsReadOnly();
        public int GeneratedCount { get; private set; }
        public double SpawnMilliseconds { get; private set; }
        static readonly ProfilerMarker PopulateMarker = new ProfilerMarker("LastSignal.Loot.Populate");
        static readonly ProfilerMarker SpawnMarker = new ProfilerMarker("LastSignal.Loot.Instantiate");
        const int WorldMask = ~((1 << 2) | (1 << 8));

        public void Begin(GameObject player, int? seed = null)
        {
            if (populated) return;
            using (PopulateMarker.Auto())
            {
                populated = true;
                Seed = seed ?? (overrideSeed ? explicitSeed : BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
                results.Clear(); preexisting.Clear(); GeneratedCount = 0; SpawnMilliseconds = 0;
                var points = new List<LootSpawnPoint>();
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    points.AddRange(root.GetComponentsInChildren<LootSpawnPoint>(false));
                    foreach (var item in root.GetComponentsInChildren<WorldItem>(true)) preexisting.Add(item);
                }
                points.Sort((a,b) => StringComparer.Ordinal.Compare(a.StableId,b.StableId));
                var counts = new Dictionary<string,int>(StringComparer.Ordinal);
                foreach (var p in points) { string id=p.StableId ?? ""; counts.TryGetValue(id,out int n); counts[id]=n+1; }
                runtimeRoot = new GameObject("Session Loot");
                SceneManager.MoveGameObjectToScene(runtimeRoot, gameObject.scene);
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
                        go.GetComponent<WorldItem>().Configure(selection.Item,selection.Quantity);
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
                foreach (var root in gameObject.scene.GetRootGameObjects())
                    foreach (var item in root.GetComponentsInChildren<WorldItem>(true))
                        if (item && !preexisting.Contains(item)) { item.gameObject.SetActive(false); Destroy(item.gameObject); }
            if (runtimeRoot) { runtimeRoot.SetActive(false); Destroy(runtimeRoot); }
            runtimeRoot=null; preexisting.Clear(); results.Clear(); GeneratedCount=0; populated=false;
        }
        void OnDestroy() => End();
    }
}
