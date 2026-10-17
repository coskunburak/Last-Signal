using System;
using UnityEngine;
namespace LastSignal.Forest
{
    /// <summary>Artist-editable inputs. No runtime scatter, spawning or world ownership.</summary>
    [CreateAssetMenu(menuName="Last Signal/FirstTown Forest Layout")]
    public sealed class ForestLayout : ScriptableObject
    {
        public int seed=261016;
        [Range(0,500)] public int treeLimit=340;
        [Range(0,1000)] public int bushLimit=520;
        [Range(0,200)] public int rockLimit=85;
        [Range(0,20000)] public int grassBudget=12000;
        [Range(4,14)] public float treeSpacing=7.5f;
        public Bounds tile=new Bounds(Vector3.zero,new Vector3(500,20,500));
        public Bounds[] artistExclusions=Array.Empty<Bounds>();
        public Clearing[] clearings={new Clearing(-222,-80,12),new Clearing(-213,180,13),new Clearing(-40,223,10)};
        public Trail[] trails={
            new Trail("West forest vehicle loop",7,true,new[]{new Vector2(-180,-140),new Vector2(-204,-116),new Vector2(-212,-68),new Vector2(-194,-18),new Vector2(-211,31),new Vector2(-197,80)}),
            new Trail("Ranger woodland foot loop",2.4f,false,new[]{new Vector2(-197,136),new Vector2(-211,153),new Vector2(-213,180),new Vector2(-187,211),new Vector2(-158,216),new Vector2(-151,173),new Vector2(-160,117),new Vector2(-190,80)}),
            new Trail("North woodland to farm",3,false,new[]{new Vector2(-75,170),new Vector2(-72,191),new Vector2(-47,214),new Vector2(0,224),new Vector2(65,219),new Vector2(120,210),new Vector2(120,200)})};
        [Serializable] public struct Clearing
        {
            public Vector2 center; public float radius;
            public Clearing(float x,float z,float r){center=new Vector2(x,z);radius=r;}
        }
        [Serializable] public sealed class Trail
        {
            public string name; public float width; public bool vehicle; public Vector2[] points;
            public Trail(string n,float w,bool v,Vector2[] p){name=n;width=w;vehicle=v;points=p;}
        }
        public static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b)
        {var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude)));}
        public float TrailDistance(Vector2 p)
        {
            float result=float.PositiveInfinity;
            foreach(var trail in trails)for(int i=1;i<trail.points.Length;i++)result=Mathf.Min(result,SegmentDistance(p,trail.points[i-1],trail.points[i])-trail.width*.5f);
            return result;
        }
        public float ClearingDistance(Vector2 p)
        {float d=float.PositiveInfinity;foreach(var c in clearings)d=Mathf.Min(d,Vector2.Distance(p,c.center)-c.radius);return d;}
        public static float RiverX(float z)=>-112+7*Mathf.Sin(z*.018f);
        public float Density(Vector2 p)
        {
            float west=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-142,-222,p.x));
            float north=Mathf.SmoothStep(0,1,Mathf.InverseLerp(200,236,p.y));
            float mask=Mathf.Max(west,north*.8f);
            // Farmland remains sparse; the river and existing roads split connected woodland zones.
            if(p.x>120)mask*=.18f;
            float clusters=.25f+.75f*Mathf.PerlinNoise((p.x+seed%731)*.022f,(p.y+seed%397)*.022f);
            return mask*clusters*Mathf.SmoothStep(0,1,Mathf.Clamp01(ClearingDistance(p)/9));
        }
    }
}
