using System;
using System.Linq;
using LastSignal.Forest;
using Object=UnityEngine.Object;
using UnityEditor;
using UnityEngine;
namespace LastSignal.Editor.FirstTownForest
{
    internal static class ForestTerrain
    {
        static float Hill(Vector2 p,Vector2 center,float radius,float height)
        {float d=Vector2.Distance(p,center)/radius;return height*Mathf.Exp(-d*d*2);}
        internal static void Shape(ForestAuthoringState state,ForestSite site,ForestSources sources,string folder)
        {
            var data=Object.Instantiate(state.terrain.terrainData);data.name="Forest terrain — staged derivative";
            AssetDatabase.CreateAsset(data,folder+"/Terrain.asset");
            var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);int n=data.heightmapResolution;
            var baseline=state.baselineTerrain.GetHeights(0,0,n,n);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                var p=new Vector2(-250+x*500f/(n-1),-250+z*500f/(n-1));
                if(p.x>-140 && p.y<198)continue;
                // Named landforms. Noise is only small secondary breakup, never the topology authority.
                float shape=Hill(p,new Vector2(-224,-60),64,3.1f)+Hill(p,new Vector2(-222,174),45,4.2f)+Hill(p,new Vector2(-32,231),68,2.6f);
                shape+=.22f*(Mathf.PerlinNoise((p.x+321)*.045f,(p.y+487)*.045f)-.5f);
                float edge=Mathf.SmoothStep(0,1,Mathf.Clamp01((245-Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.y)))/14));
                float trail=Mathf.SmoothStep(0,1,Mathf.Clamp01(state.layout.TrailDistance(p)/10));
                float region=Mathf.Max(Mathf.SmoothStep(0,1,Mathf.InverseLerp(-140,-175,p.x)),Mathf.SmoothStep(0,1,Mathf.InverseLerp(198,220,p.y)));
                float influence=site.HeightInfluence(p)*region*edge;
                if(influence>0)heights[z,x]=Mathf.Clamp01(baseline[z,x]+shape*influence*trail/data.size.y);
            }
            data.SetHeights(0,0,heights);data.terrainLayers=sources.Layers();data.alphamapResolution=512;
            state.terrain.terrainData=data;state.terrain.GetComponent<TerrainCollider>().terrainData=data;
            var map=new float[512,512,4];
            for(int z=0;z<512;z++)for(int x=0;x<512;x++)
            {
                var p=new Vector2(-250+x*500f/511,-250+z*500f/511);float density=state.layout.Density(p),slope=site.Slope(p);
                float track=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((state.layout.TrailDistance(p)+.3f)/3));
                float river=1-Mathf.Clamp01((Mathf.Abs(p.x-ForestLayout.RiverX(p.y))-5)/11);
                float rock=Mathf.Clamp01((slope-22)/30)*.8f+river*.35f;
                float grass=(1-track)*(1-rock)*Mathf.Lerp(.10f,.72f,density);
                float road=track*(1-rock)*.9f;
                map[z,x,0]=grass;map[z,x,1]=Mathf.Max(0,1-grass-road-rock);map[z,x,2]=road;map[z,x,3]=rock;
            }
            data.SetAlphamaps(0,0,map);
            var material=new Material(Shader.Find("LastSignal/Forest/Terrain Lit") ?? throw new InvalidOperationException("Forest terrain shader unavailable.")){name="Forest terrain weather"};AssetDatabase.CreateAsset(material,folder+"/Terrain.mat");
            state.terrain.materialTemplate=material;state.terrain.drawInstanced=true;state.terrain.detailObjectDistance=45;state.terrain.detailObjectDensity=1;state.terrain.heightmapPixelError=5;
            EditorUtility.SetDirty(data);
        }
        internal static void Grass(ForestAuthoringState state,ForestSite site,ForestSources sources,string folder)
        {
            // Terrain instancing needs a single root mesh, not an LOD prefab hierarchy.
            var source=sources.prefabs["Grass"];var go=new GameObject("Grass detail");GameObject prefab;
            try {go.AddComponent<MeshFilter>().sharedMesh=source.GetComponentInChildren<MeshFilter>().sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterial=source.GetComponentInChildren<MeshRenderer>().sharedMaterial;prefab=PrefabUtility.SaveAsPrefabAsset(go,folder+"/GrassDetail.prefab");}
            finally{Object.DestroyImmediate(go);}
            var data=state.terrain.terrainData;
            if(data.detailPrototypes.Length>0 && data.detailScatterMode!=DetailScatterMode.InstanceCountMode)throw new InvalidOperationException("Existing detail scatter mode must be preserved; adapt grass authoring before converting it.");
            data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            if(data.detailPrototypes.Length>0 && data.detailWidth!=512)throw new InvalidOperationException("Existing artist detail resolution differs. Preserve it and adapt grass authoring first.");
            if(data.detailWidth!=512)data.SetDetailResolution(512,16);
            var prototypes=data.detailPrototypes.ToList();
            var prototype=new DetailPrototype {prototype=prefab,usePrototypeMesh=true,useInstancing=true,renderMode=DetailRenderMode.VertexLit,
                minWidth=.65f,maxWidth=1.1f,minHeight=.6f,maxHeight=1.05f,noiseSpread=.3f,healthyColor=Color.white,dryColor=Color.white};
            bool existing=state.grassPrototypeIndex>=0 && state.grassPrototypeIndex<prototypes.Count;
            var detail=existing?data.GetDetailLayer(0,0,512,512,state.grassPrototypeIndex):new int[512,512];
            for(int z=0;z<512;z++)for(int x=0;x<512;x++)if(-250+x*500f/512<-140 || -250+z*500f/512>198)detail[z,x]=0;
            if(existing)prototypes[state.grassPrototypeIndex]=prototype;else{state.grassPrototypeIndex=prototypes.Count;prototypes.Add(prototype);}
            data.detailPrototypes=prototypes.ToArray();var rng=new System.Random(state.layout.seed+814);int count=0;
            for(int i=0;i<state.layout.grassBudget*12 && count<state.layout.grassBudget;i++)
            {
                int x=rng.Next(512),z=rng.Next(512);var p=new Vector2(-250+(x+.5f)*500/512,-250+(z+.5f)*500/512);
                if(detail[z,x]>=2 || !site.Allowed(p,.15f) || site.Slope(p)>27 || rng.NextDouble()>state.layout.Density(p)*.85f)continue;
                detail[z,x]++;count++;
            }
            data.SetDetailLayer(0,0,state.grassPrototypeIndex,detail);state.grassInstances=count;EditorUtility.SetDirty(data);
        }
    }
}
