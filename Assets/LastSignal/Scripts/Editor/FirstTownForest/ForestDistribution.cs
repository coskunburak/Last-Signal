using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LastSignal.Forest;
using UnityEngine;
namespace LastSignal.Editor.FirstTownForest
{
    internal static class ForestDistribution
    {
        static Transform Group(string name,Transform parent){var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
        internal static string Scatter(ForestAuthoringState state,ForestSite site,ForestSources source)
        {
            var root=state.generated.transform;
            var edge=Group("ForestEdge",root);var mixed=Group("MixedWoodland",root);var deep=Group("DeepForest",root);
            var under=Group("GroundCover",root);var rocks=Group("RockClusters",root);var trails=Group("TrailVegetation",root);
            var clearings=Group("Encounter clearings - no automatic spawns",root);
            var rng=new System.Random(state.layout.seed);var used=new List<Vector2>();var digest=new StringBuilder();
            var bins=new Dictionary<Vector2Int,List<Vector2>>();float spacing=state.layout.treeSpacing;
            bool Spaced(Vector2 p)
            {
                var cell=new Vector2Int(Mathf.FloorToInt(p.x/spacing),Mathf.FloorToInt(p.y/spacing));
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)if(bins.TryGetValue(cell+new Vector2Int(x,y),out var points))foreach(var other in points)if((p-other).sqrMagnitude<spacing*spacing)return false;
                if(!bins.TryGetValue(cell,out var list))bins[cell]=list=new List<Vector2>();list.Add(p);return true;
            }
            void Record(string kind,Vector2 p,float height,float scale,float yaw)
            {digest.AppendFormat(CultureInfo.InvariantCulture,"{0}:{1:F4},{2:F4},{3:F4}:{4:F4}:{5:F4};",kind,p.x,height,p.y,scale,yaw);}
            for(int i=0;i<24000 && state.trees<state.layout.treeLimit;i++)
            {
                var p=new Vector2(-239+(float)rng.NextDouble()*478,-239+(float)rng.NextDouble()*478);float density=state.layout.Density(p);
                if(rng.NextDouble()>density || !site.Allowed(p,3.2f) || site.Slope(p)>26 || !Spaced(p))continue;
                float moisture=1-Mathf.Clamp01((Mathf.Abs(p.x-ForestLayout.RiverX(p.y))-18)/60);
                string kind=rng.NextDouble()<.20+moisture*.32-Mathf.Clamp01(site.Height(p)/5)*.1?"Birch":"Pine";
                float scale=kind=="Pine"?.62f+(float)rng.NextDouble()*.32f:.60f+(float)rng.NextDouble()*.32f;
                float yaw=(float)rng.NextDouble()*360,height=site.Height(p);
                var parent=density>.48f?deep:density>.22f?mixed:edge;
                source.Place(kind,parent,new Vector3(p.x,height,p.y),scale,yaw);used.Add(p);state.trees++;Record(kind,p,height,scale,yaw);
            }
            var decorations=new List<Vector2>();
            for(int i=0;i<12000 && state.bushes<state.layout.bushLimit && used.Count>0;i++)
            {
                var center=used[rng.Next(used.Count)];float a=(float)rng.NextDouble()*Mathf.PI*2,r=3+(float)rng.NextDouble()*6;
                var p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                if(!site.Allowed(p,.85f) || site.Slope(p)>28 || decorations.Exists(o=>(o-p).sqrMagnitude<2.5f))continue;
                float scale=.5f+(float)rng.NextDouble()*.38f,yaw=(float)rng.NextDouble()*360,h=site.Height(p);
                source.Place("Bush",state.layout.TrailDistance(p)<6?trails:under,new Vector3(p.x,h,p.y),scale,yaw);
                decorations.Add(p);state.bushes++;Record("Bush",p,h,scale,yaw);
            }
            for(int i=0;i<5000 && state.rocks<state.layout.rockLimit;i++)
            {
                var p=new Vector2(-237+(float)rng.NextDouble()*474,-237+(float)rng.NextDouble()*474);
                if(rng.NextDouble()>state.layout.Density(p)*.7f || !site.Allowed(p,2.5f) || site.Slope(p)>32 || decorations.Exists(o=>(o-p).sqrMagnitude<14))continue;
                float scale=.16f+(float)rng.NextDouble()*.22f,yaw=(float)rng.NextDouble()*360,h=site.Height(p)-.12f;
                source.Place("Rock",rocks,new Vector3(p.x,h,p.y),scale,yaw);decorations.Add(p);state.rocks++;Record("Rock",p,h,scale,yaw);
            }
            var debris=Group("Natural debris - bark submesh",root);int logs=0;
            for(int i=0;i<used.Count && logs<16;i+=3)
            {
                var p=used[i]+new Vector2(4,3);
                if(!site.Allowed(p,5) || site.Slope(p)>15)continue;
                float scale=.18f+(float)rng.NextDouble()*.08f,yaw=(float)rng.NextDouble()*360,h=site.Height(p)-.08f;
                source.Place("Log",debris,new Vector3(p.x,h,p.y),scale,yaw);logs++;Record("Log",p,h,scale,yaw);
            }
            foreach(var c in state.layout.clearings)
            {
                var g=Group("Clearing — retreat radius "+c.radius,clearings);g.position=new Vector3(c.center.x,site.Height(c.center),c.center.y);
            }
            foreach(var route in state.layout.trails)
            {
                var g=Group(route.name,root);
                for(int j=0;j<route.points.Length;j++)
                {var p=route.points[j];var marker=Group("Route - forest "+j,g);marker.position=new Vector3(p.x,site.Height(p)+.08f,p.y);}
            }
            Cameras(root,site);
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(digest.ToString()))).Replace("-","").ToLowerInvariant();
        }
        static void Cameras(Transform root,ForestSite site)
        {
            var group=Group("Manual review cameras - disabled",root);
            var positions=new[]{new Vector2(-180,80),new Vector2(-225,174),new Vector2(-200,-105),new Vector2(-212,-68),new Vector2(-158,173),new Vector2(-40,223)};
            var targets=new[]{new Vector2(-219,155),new Vector2(-211,196),new Vector2(-212,-68),new Vector2(-194,-18),new Vector2(-209,180),new Vector2(-65,220)};
            var names=new[]{"Town to Forest","Forest Interior","Trail","Forest Road","Rain Forest","Night Forest"};
            for(int i=0;i<names.Length;i++){var t=Group(names[i],group);t.position=new Vector3(positions[i].x,site.Height(positions[i])+1.75f,positions[i].y);t.LookAt(new Vector3(targets[i].x,site.Height(targets[i])+1.5f,targets[i].y));var c=t.gameObject.AddComponent<Camera>();c.fieldOfView=60;c.farClipPlane=800;c.enabled=false;}
        }
    }
}
