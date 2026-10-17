using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Forest;
using LastSignal.Loot;
using LastSignal.Vehicles;
using LastSignal.VisualSlice;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.FirstTownVisualSlice
{
    internal sealed class SliceSite
    {
        internal readonly FirstTownSliceState state;
        internal readonly List<Bounds> solids = new List<Bounds>(), roads = new List<Bounds>(), pads = new List<Bounds>();
        internal readonly List<Vector2> treeCenters = new List<Vector2>();
        internal readonly Transform town, compound, station, service, cabin;
        internal readonly ForestAuthoringState forest;
        internal readonly List<string> skipped = new List<string>();
        internal SliceSite(Scene scene, FirstTownSliceState s)
        {
            state=s;
            var ts=SliceAuthoring.All<Transform>(scene);
            town=ts.Single(t=>t.name=="FirstTownCore - 500m resident world");
            compound=town.Find("02 Safehouse Compound"); station=town.Find("04 Gas Station");
            service=station?station.Find("service-shop"):null;
            cabin=scene.GetRootGameObjects().Single(g=>g.name=="S013 Production Cabin").transform;
            forest=SliceAuthoring.All<ForestAuthoringState>(scene).Single();
            if(!compound || !station || !service || Vector3.Distance(compound.position,new Vector3(-180,0,-170))>.1f || Vector3.Distance(station.position,new Vector3(65,0,-82))>.1f)
                throw new InvalidOperationException("Endpoint topology changed; the authored composition must be re-fitted.");
            // Renderer bounds of roofs cover open interiors, which thin wall colliders alone cannot protect.
            foreach(var roof in SliceAuthoring.All<LastSignal.WorldTime.RainRoof>(scene))
                foreach(var c in roof.GetComponentsInChildren<Collider>()) if(c.enabled)pads.Add(c.bounds);
            foreach(var c in SliceAuthoring.All<Collider>(scene))
            {
                if(!c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger || c is TerrainCollider || IsGeneratedForest(c.transform))continue;
                var b=c.bounds;if(!Intersects(b))continue;
                if(c.name=="Roadbed" || c.name=="Clear junction" || c.name=="Continuous deck" || c.name=="Drive-through apron" || c.name=="Vehicle parking")roads.Add(b);
                else if(b.size.y>.06f)solids.Add(b);
            }
            foreach(var t in ts)
            {
                if(!t.gameObject.activeInHierarchy || t.IsChildOf(s.transform))continue;
                if(t.name.StartsWith("Route -",StringComparison.Ordinal) || t.name=="PlayerSpawn" || t.GetComponent<ZombieEncounter>())
                    pads.Add(new Bounds(t.position,new Vector3(6,10,6)));
                if(t.GetComponent<LootSpawnPoint>())pads.Add(new Bounds(t.position,new Vector3(3,10,3)));
                if(t.GetComponent<LastSignal.DoorInteractable>())pads.Add(new Bounds(t.position,new Vector3(5,10,6)));
                var vehicle=t.GetComponent<VehicleWorld>();
                if(vehicle)pads.Add(new Bounds(new SerializedObject(vehicle).FindProperty("spawnPosition").vector3Value,new Vector3(18,10,18)));
            }
            pads.AddRange(s.profile.artistExclusions);
            foreach(var r in s.artistContent.GetComponentsInChildren<Renderer>())pads.Add(r.bounds);
            foreach(var r in forest.heroTrees.GetComponentsInChildren<Renderer>())pads.Add(r.bounds);
        }
        bool IsGeneratedForest(Transform t) => (forest.generated && t.IsChildOf(forest.generated.transform)) || (forest.legacyForest && t.IsChildOf(forest.legacyForest.transform));
        internal bool Intersects(Bounds b)
        {
            var p=new Vector2(b.center.x,b.center.z);
            return state.profile.EdgeDistance(p)<Mathf.Max(b.extents.x,b.extents.z)+2;
        }
        internal static float Distance(Vector2 p, Bounds b)
        { float x=Mathf.Max(0,Mathf.Abs(p.x-b.center.x)-b.extents.x),z=Mathf.Max(0,Mathf.Abs(p.y-b.center.z)-b.extents.z); return Mathf.Sqrt(x*x+z*z); }
        internal float RoadDistance(Vector2 p) {float d=10000;foreach(var b in roads)d=Mathf.Min(d,Distance(p,b));return d;}
        internal float SolidDistance(Vector2 p) {float d=10000;foreach(var b in solids)d=Mathf.Min(d,Distance(p,b));foreach(var b in pads)d=Mathf.Min(d,Distance(p,b));return d;}
        internal float TrailDistance(Vector2 p)
        {return Mathf.Min(FirstTownSliceProfile.Distance(p,state.profile.footTrail)-state.profile.trailWidth*.5f,forest.layout.TrailDistance(p));}
        internal bool Allowed(Vector2 p,float radius)
        {
            return state.profile.Contains(p,radius+1) && RoadDistance(p)>radius+1.2f && SolidDistance(p)>radius+.6f && TrailDistance(p)>radius+.6f &&
                Mathf.Abs(p.x-ForestLayout.RiverX(p.y))>18+radius && Slope(p)<25;
        }
        internal float Height(Vector2 p) => state.terrain.SampleHeight(new Vector3(p.x,0,p.y))+state.terrain.transform.position.y;
        internal float Slope(Vector2 p)
        {var t=state.terrain;var o=t.transform.position;return t.terrainData.GetSteepness((p.x-o.x)/t.terrainData.size.x,(p.y-o.z)/t.terrainData.size.z);}
        internal void SuppressOldVegetation()
        {
            var hidden=new HashSet<GameObject>();
            foreach(var source in new[]{forest.generated,forest.legacyForest})
            {
                if(!source || !source.activeInHierarchy)continue;
                foreach(var r in source.GetComponentsInChildren<MeshRenderer>())
                {
                    var g=PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
                    if(!g || !g.transform.IsChildOf(source.transform))continue;
                    var p=new Vector2(g.transform.position.x,g.transform.position.z);
                    if(state.profile.Contains(p,2))hidden.Add(g);
                    else if(g.GetComponentInChildren<CapsuleCollider>())treeCenters.Add(p);
                }
            }
            state.hiddenVegetation=hidden.OrderBy(g=>g.transform.position.x).ThenBy(g=>g.transform.position.z).ToArray();
            foreach(var g in state.hiddenVegetation)g.SetActive(false);
        }
    }
}
