using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Editor.WorldReconstruction
{
    internal sealed class WorldTerrainSampling
    {
        internal readonly float[,] core, west;
        internal readonly bool[,] protectedCore;
        internal readonly float[,] original;
        internal float minimum = float.MaxValue, maximum = float.MinValue, origin, range;
        internal int rendererMasks, protectedSamples;

        internal WorldTerrainSampling(WorldReconstructionProfile p, WorldPlanSurvey survey, Scene scene, Action<int,int> progress = null)
        {
            // G3 keeps existing decoration anchored. G5 can replace these conservative footprint masks
            // with explicitly owned grounding moves; G3 must never leave existing trees/rocks floating.
            float margin = 2 * p.core.Width / (p.heightmapResolution - 1);
            foreach (var renderer in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                var b = renderer.bounds;
                float x0 = Mathf.Max(b.min.x, p.core.minX), x1 = Mathf.Min(b.max.x, p.core.maxX);
                float z0 = Mathf.Max(b.min.z, p.core.minZ), z1 = Mathf.Min(b.max.z, p.core.maxZ);
                if (x0 > x1 || z0 > z1) continue;
                survey.protections.Add(new Bounds(new Vector3((x0+x1)*.5f,0,(z0+z1)*.5f), new Vector3(x1-x0,20,z1-z0)), margin);
                rendererMasks++;
            }
            int n = p.heightmapResolution;
            core = new float[n,n]; west = new float[n,n]; protectedCore = new bool[n,n]; original = survey.originalHeights;
            var evaluator = new NaturalTerrainPlan(p, original, survey.protections);
            for (int z=0; z<n; z++)
            {
                if(z%8==0)progress?.Invoke(z,n);
                for (int x=0; x<n; x++)
                {
                var c = Point(p.core,x,z,n); var w = Point(p.extension,x,z,n);
                core[z,x] = evaluator.Height(c); west[z,x] = evaluator.Height(w);
                minimum = Mathf.Min(minimum,core[z,x],west[z,x]); maximum = Mathf.Max(maximum,core[z,x],west[z,x]);
                if (evaluator.Influence(c) == 0) { protectedCore[z,x] = true; protectedSamples++; }
                }
            }
            origin = Mathf.Floor((minimum-p.verticalPaddingMeters)/8)*8;
            float top = Mathf.Ceil((maximum+p.verticalPaddingMeters)/8)*8; range = top-origin;
            // The approved resident volume is not enlarged silently by a terrain generation.
            if (origin < WorldStageAuthoring.ResidentBounds.min.y || top + 8 > WorldStageAuthoring.ResidentBounds.max.y)
                throw new InvalidOperationException("Full sampled vertical interval exceeds approved resident bounds + vehicle clearance.");
            for (int z=0; z<n; z++)
            {
                if (Mathf.Abs(core[z,0]-west[z,n-1]) > .00001f) throw new InvalidOperationException("Analytical seam mismatch.");
                for (int x=0; x<n; x++)
                {
                    core[z,x] = NaturalTerrainPlan.ProjectWorldHeight(core[z,x],origin,range);
                    west[z,x] = NaturalTerrainPlan.ProjectWorldHeight(west[z,x],origin,range);
                }
            }
        }
        internal static Vector2 Point(WorldRectangle rect,int x,int z,int n) =>
            new Vector2(Mathf.Lerp(rect.minX,rect.maxX,x/(float)(n-1)),Mathf.Lerp(rect.minZ,rect.maxZ,z/(float)(n-1)));

        internal float[,,] WestSurface(TerrainData baseline, WorldReconstructionProfile p, string[] layerSourceNames)
        {
            int n = baseline.alphamapResolution, layers = baseline.terrainLayers.Length;
            // G2 copies are named by GUID. Semantic names must come from its source-to-copy manifest.
            var names = layerSourceNames.Select(name => name.ToLowerInvariant()).ToArray();
            if(names.Length!=layers)throw new InvalidOperationException("Terrain layer manifest count mismatch.");
            int woodland = Array.FindIndex(names,s=>s.Contains("woodland")), soil = Array.FindIndex(names,s=>s.Contains("soil")), rock = Array.FindIndex(names,s=>s.Contains("rock"));
            if (woodland<0 || soil<0 || rock<0) throw new InvalidOperationException("Expected isolated ForestPrototype woodland/soil/rock terrain layers.");
            var edge = baseline.GetAlphamaps(0,0,1,n); var result = new float[n,n,layers];
            float spacing = p.extension.Width/(p.heightmapResolution-1);
            for (int z=0; z<n; z++) for (int x=0; x<n; x++)
            {
                var point = Point(p.extension,x,z,n);
                int hx = Mathf.RoundToInt(x*(west.GetLength(1)-1f)/(n-1)), hz = Mathf.RoundToInt(z*(west.GetLength(0)-1f)/(n-1));
                int x0=Mathf.Max(0,hx-1), x1=Mathf.Min(west.GetLength(1)-1,hx+1), z0=Mathf.Max(0,hz-1), z1=Mathf.Min(west.GetLength(0)-1,hz+1);
                float dx=(west[hz,x1]-west[hz,x0])*range/((x1-x0)*spacing), dz=(west[z1,hx]-west[z0,hx])*range/((z1-z0)*spacing);
                float slope=Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;
                float r=Mathf.SmoothStep(0,1,Mathf.InverseLerp(28,48,slope));
                float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01((p.core.minX-point.x)/p.seamBlendMeters));
                for (int layer=0;layer<layers;layer++)
                {
                    float weight=layer==rock?r:layer==woodland?(1-r)*.8f:layer==soil?(1-r)*.2f:0;
                    result[z,x,layer]=Mathf.Lerp(edge[z,0,layer],weight,blend);
                }
            }
            return result;
        }
    }
}
