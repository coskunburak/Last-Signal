using System;
using UnityEngine;

namespace LastSignal.VisualSlice
{
    /// <summary>Authored coordinates, never a runtime world generator or time authority.</summary>
    [CreateAssetMenu(menuName = "Last Signal/First Town visual slice")]
    public sealed class FirstTownSliceProfile : ScriptableObject
    {
        public int seed = 161026;
        public Bounds safehouse = new Bounds(new Vector3(-185, 0, -166), new Vector3(100, 40, 100));
        public Bounds station = new Bounds(new Vector3(66, 0, -85), new Vector3(108, 40, 108));
        public float corridorHalfWidth = 14;
        public Vector2[] route = { new Vector2(-180,-170), new Vector2(-180,-140), new Vector2(50,-140), new Vector2(50,-82) };
        public Vector2[] footTrail = { new Vector2(-180,-138), new Vector2(-195,-130), new Vector2(-213,-137), new Vector2(-222,-151), new Vector2(-225,-178) };
        public float trailWidth = 2.4f;
        public int treeBudget = 95, shrubBudget = 230, rockBudget = 60, grassBudget = 9000;
        public Bounds[] artistExclusions = Array.Empty<Bounds>();
        public View[] views = {
            new View("01 Safehouse exterior",new Vector3(-174,1.65f,-157),new Vector3(-181,2,-170)),
            new View("02 Forest trail",new Vector3(-216,1.65f,-140),new Vector3(-224,1.9f,-157)),
            new View("03 Roadside approach",new Vector3(-168,1.69f,-135),new Vector3(-116,2,-135)),
            new View("04 Gas station",new Vector3(50,1.69f,-116),new Vector3(73,2.6f,-80)),
            new View("05 Service frontage",new Vector3(65,1.69f,-70),new Vector3(70,2,-55)),
            new View("06 Aerial coverage",new Vector3(-63,270,-103),new Vector3(-63,0,-104),true,190)
        };
        [Serializable] public sealed class View
        {
            public string name; public Vector3 position, target; public float fieldOfView = 75;
            public bool orthographic; public float orthographicSize;
            public View(string n,Vector3 p,Vector3 t,bool o=false,float size=10) { name=n;position=p;target=t;orthographic=o;orthographicSize=size; }
        }
        public static float Distance(Vector2 p, Vector2 a, Vector2 b)
        { var d=b-a; return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude))); }
        public static float Distance(Vector2 p, Vector2[] line)
        { float d=float.PositiveInfinity; for(int i=1;i<line.Length;i++) d=Mathf.Min(d,Distance(p,line[i-1],line[i])); return d; }
        public static float EdgeDistance(Vector2 p, Bounds b)
        { return Mathf.Max(Mathf.Abs(p.x-b.center.x)-b.extents.x,Mathf.Abs(p.y-b.center.z)-b.extents.z); }
        public float EdgeDistance(Vector2 p) => Mathf.Min(EdgeDistance(p,safehouse),EdgeDistance(p,station),Distance(p,route)-corridorHalfWidth);
        public float Weight(Vector2 p) => 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-5,0,EdgeDistance(p)));
        public bool Contains(Vector2 p,float inset=0) => EdgeDistance(p)<-inset;
    }
}
