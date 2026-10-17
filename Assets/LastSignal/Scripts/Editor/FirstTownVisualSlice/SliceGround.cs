using System;
using System.Linq;
using LastSignal.Forest;
using LastSignal.VisualSlice;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace LastSignal.Editor.FirstTownVisualSlice
{
    internal static class SliceGround
    {
        internal static void Build(SliceSite site,SliceAssets art)
        {
            var s=site.state;var t=s.terrain;var data=Object.Instantiate(s.baselineTerrain);data.name="FirstTown local relief and verges";
            AssetDatabase.CreateAsset(data,art.folder+"/Terrain.asset");
            var origin=t.transform.position;int n=data.heightmapResolution;
            var heights=data.GetHeights(0,0,n,n);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                var p=new Vector2(origin.x+x*data.size.x/(n-1),origin.z+z*data.size.z/(n-1));float w=s.profile.Weight(p);
                if(w<=0 || Mathf.Abs(p.x-ForestLayout.RiverX(p.y))<25)continue;
                float road=site.RoadDistance(p),solid=site.SolidDistance(p),trail=site.TrailDistance(p);
                float protect=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.2f,5,solid))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,3,road));
                float noTrail=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,3,trail));
                float noise=Mathf.PerlinNoise((p.x+412)*.045f,(p.y+319)*.045f);
                // Continuous 0.4m berms and shallow drainage swales. Retain existing landforms and collider heights on pads.
                float berm=.48f*Mathf.Exp(-Mathf.Pow((road-5.2f)/2.8f,2));
                float ditch=-.12f*Mathf.Exp(-Mathf.Pow((road-2.5f)/1.1f,2));
                float relief=(noise-.43f)*.4f+berm+ditch;
                heights[z,x]=Mathf.Clamp01(heights[z,x]+w*protect*noTrail*relief/data.size.y);
            }
            data.SetHeights(0,0,heights);
            // Keep all existing layers, resolution and pixels outside the footprint; no global re-splat.
            var layers=data.terrainLayers;
            int Find(string key)=>Array.FindIndex(layers,l=>l && l.name.ToLowerInvariant().Contains(key));
            int grass=Find("grass"),soil=Find("soil"),roadLayer=Find("road"),rock=Find("rock");
            if(new[]{grass,soil,roadLayer,rock}.Any(i=>i<0))throw new InvalidOperationException("Expected existing forest grass/soil/road/rock layers. No global terrain replacement is allowed.");
            int aw=data.alphamapWidth,ah=data.alphamapHeight;var map=data.GetAlphamaps(0,0,aw,ah);
            for(int z=0;z<ah;z++)for(int x=0;x<aw;x++)
            {
                var p=new Vector2(origin.x+x*data.size.x/(aw-1),origin.z+z*data.size.z/(ah-1));float w=s.profile.Weight(p);
                if(w<=0 || Mathf.Abs(p.x-ForestLayout.RiverX(p.y))<15)continue;
                float d=site.RoadDistance(p),trail=site.TrailDistance(p);
                float wear=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.15f,1.5f,trail));
                float shoulder=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,4.3f,d));
                float patch=Mathf.PerlinNoise((p.x+312)*.12f,(p.y+812)*.12f);
                float r=.05f*(1-wear)*(1-shoulder);
                float track=Mathf.Max(wear*.85f,shoulder*.72f);
                float g=(1-track-r)*Mathf.Lerp(.48f,.85f,patch);
                float earth=Mathf.Max(0,1-r-track-g);
                for(int l=0;l<layers.Length;l++)map[z,x,l]*=1-w;
                map[z,x,grass]+=g*w;map[z,x,soil]+=earth*w;map[z,x,roadLayer]+=track*w;map[z,x,rock]+=r*w;
            }
            data.SetAlphamaps(0,0,map);t.terrainData=data;t.GetComponent<TerrainCollider>().terrainData=data;
            // Existing weather-aware terrain shader and material remain authoritative.
            t.materialTemplate=s.baselineTerrainMaterial;EditorUtility.SetDirty(data);
        }
        internal static void Grass(SliceSite site,SliceAssets art)
        {
            var s=site.state;var data=s.terrain.terrainData;
            if(data.detailScatterMode!=DetailScatterMode.InstanceCountMode || data.detailWidth<1)
                throw new InvalidOperationException("Existing instance-count grass details required; refuse a whole-terrain conversion.");
            int n=data.detailWidth;var origin=s.terrain.transform.position;
            var prototypes=data.detailPrototypes;int slot=Array.FindIndex(prototypes,p=>p.prototype && p.prototype.name.Contains("Grass"));
            if(slot<0)throw new InvalidOperationException("Existing compatible Grass detail prototype missing.");
            // Clear only regenerated pixels from every pre-existing detail layer so old and new grass never stack.
            for(int l=0;l<prototypes.Length;l++)
            {
                var layer=data.GetDetailLayer(0,0,n,n,l);
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)if(s.profile.Contains(new Vector2(origin.x+(x+.5f)*data.size.x/n,origin.z+(z+.5f)*data.size.z/n)))layer[z,x]=0;
                data.SetDetailLayer(0,0,l,layer);
            }
            var values=data.GetDetailLayer(0,0,n,n,slot);var rng=new System.Random(s.profile.seed+307);int count=0;
            for(int i=0;i<s.profile.grassBudget*30 && count<s.profile.grassBudget;i++)
            {
                int x=rng.Next(n),z=rng.Next(n);var p=new Vector2(origin.x+(x+.5f)*data.size.x/n,origin.z+(z+.5f)*data.size.z/n);
                float cluster=Mathf.PerlinNoise((p.x+92)*.17f,(p.y+54)*.17f);
                if(values[z,x]>=3 || !site.Allowed(p,.1f) || rng.NextDouble()>cluster*.8f)continue;
                values[z,x]++;count++;
            }
            data.SetDetailLayer(0,0,slot,values);s.grass=count;EditorUtility.SetDirty(data);
        }
    }
}
