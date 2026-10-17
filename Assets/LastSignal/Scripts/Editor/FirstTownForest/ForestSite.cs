using System;
using System.Collections.Generic;
using System.Linq;
using LastSignal.Forest;
using LastSignal.WorldTime;
using LastSignal.Vehicles;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace LastSignal.Editor.FirstTownForest
{
    /// <summary>Scene-derived exclusion data. No physics simulation or prefab-name guesses for gameplay clearance.</summary>
    internal sealed class ForestSite
    {
        internal readonly List<Bounds> solids=new List<Bounds>(),roads=new List<Bounds>(),protectedPads=new List<Bounds>();
        readonly Dictionary<Vector2Int,List<Bounds>> cells=new Dictionary<Vector2Int,List<Bounds>>();
        internal readonly Terrain terrain;
        internal readonly ForestLayout layout;
        internal ForestSite(Scene scene,ForestAuthoringState state)
        {
            terrain=state.terrain;layout=state.layout;
            var components=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            bool Owned(Transform t)=> (state.generated && t.IsChildOf(state.generated.transform)) || (state.legacyForest && t.IsChildOf(state.legacyForest.transform));
            foreach(var t in components)
            {
                if(Owned(t) || !t.gameObject.activeInHierarchy)continue;
                var c=t.GetComponent<Collider>();
                if(c && c.enabled && !c.isTrigger && !(c is TerrainCollider))
                {
                    if(t.name=="Roadbed" || t.name=="Clear junction" || t.name=="Continuous deck")roads.Add(c.bounds);
                    else solids.Add(c.bounds);
                }
                if(t.GetComponent<RainRoof>())
                    foreach(var roof in t.GetComponentsInChildren<Collider>().Where(x=>x.enabled))protectedPads.Add(roof.bounds);
                if(t.name.StartsWith("Route -") || t.name=="PlayerSpawn" || t.GetComponent<ZombieEncounter>())
                    protectedPads.Add(new Bounds(t.position,new Vector3(6,8,6)));
                var vehicle=t.GetComponent<VehicleWorld>();
                if(vehicle){var so=new SerializedObject(vehicle);protectedPads.Add(new Bounds(so.FindProperty("spawnPosition").vector3Value,new Vector3(18,10,18)));}
            }
            protectedPads.AddRange(layout.artistExclusions);
            if(state.heroTrees)foreach(var r in state.heroTrees.GetComponentsInChildren<Renderer>())solids.Add(r.bounds);
            foreach(var b in roads)Index(b,3);foreach(var b in solids)Index(b,2);foreach(var b in protectedPads)Index(b,3);
        }
        internal static float Distance(Vector2 p,Bounds b)
        {return new Vector2(Mathf.Max(b.min.x-p.x,0,p.x-b.max.x),Mathf.Max(b.min.z-p.y,0,p.y-b.max.z)).magnitude;}
        void Index(Bounds b,float padding)
        {
            b.Expand(new Vector3(padding*2,0,padding*2));
            for(int x=Mathf.FloorToInt((b.min.x-16)/16);x<=Mathf.FloorToInt((b.max.x+16)/16);x++)
                for(int z=Mathf.FloorToInt((b.min.z-16)/16);z<=Mathf.FloorToInt((b.max.z+16)/16);z++)
                {var key=new Vector2Int(x,z);if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<Bounds>();list.Add(b);}
        }
        internal float ExistingClearance(Vector2 p)
        {
            float d=16;
            if(cells.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x/16),Mathf.FloorToInt(p.y/16)),out var list))
                foreach(var b in list)d=Mathf.Min(d,Distance(p,b));
            return d;
        }
        internal bool Allowed(Vector2 p,float radius)
        {
            if(Mathf.Abs(p.x)>239 || Mathf.Abs(p.y)>239 || Mathf.Abs(p.x-ForestLayout.RiverX(p.y))<18+radius)return false;
            return ExistingClearance(p)>radius && layout.TrailDistance(p)>radius+1.5f && layout.ClearingDistance(p)>radius;
        }
        internal float Height(Vector2 p)=>terrain.terrainData.GetInterpolatedHeight((p.x-terrain.transform.position.x)/500,(p.y-terrain.transform.position.z)/500)+terrain.transform.position.y;
        internal float Slope(Vector2 p)=>terrain.terrainData.GetSteepness((p.x-terrain.transform.position.x)/500,(p.y-terrain.transform.position.z)/500);
        internal float HeightInfluence(Vector2 p)
        {
            float protectedDistance=ExistingClearance(p);
            float river=Mathf.Abs(p.x-ForestLayout.RiverX(p.y));
            return Mathf.SmoothStep(0,1,Mathf.Clamp01(protectedDistance/12))*Mathf.SmoothStep(0,1,Mathf.Clamp01((river-18)/12));
        }
    }
}
