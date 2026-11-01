using System;
using System.Collections.Generic;
using LastSignal.Forest;
using UnityEngine;

namespace LastSignal.Editor.WorldReconstruction
{
    // Pure world-height evaluator. No TerrainData writes, clamp-to-01, meshes or scene operations.
    public sealed class NaturalTerrainPlan
    {
        readonly WorldReconstructionProfile p;
        readonly float[,] source;
        readonly ProtectionIndex protections;
        public NaturalTerrainPlan(WorldReconstructionProfile profile, float[,] sourceHeights, ProtectionIndex protection)
        {
            p = profile; p.Validate(); source = sourceHeights; protections = protection ?? throw new ArgumentNullException(nameof(protection));
            if (source.GetLength(0) != p.heightmapResolution || source.GetLength(1) != p.heightmapResolution) throw new ArgumentException("Source sampling changed.");
        }
        public float Original(Vector2 point)
        {
            float x = Mathf.Clamp01((point.x-p.core.minX)/p.core.Width)*(source.GetLength(1)-1);
            float z = Mathf.Clamp01((point.y-p.core.minZ)/p.core.Depth)*(source.GetLength(0)-1);
            int ix = Mathf.Min((int)x, source.GetLength(1)-2), iz = Mathf.Min((int)z, source.GetLength(0)-2);
            return p.sourceOrigin.y + p.sourceSize.y * Mathf.Lerp(Mathf.Lerp(source[iz,ix],source[iz,ix+1],x-ix),
                Mathf.Lerp(source[iz+1,ix],source[iz+1,ix+1],x-ix),z-iz);
        }
        float Shape(Vector2 point)
        {
            float sum = 0;
            foreach (var f in p.forms)
            {
                var d = point-f.center; float a = f.yaw*Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
                float x = (c*d.x+s*d.y)/f.radius.x, z = (-s*d.x+c*d.y)/f.radius.y;
                sum += f.height*Mathf.Exp(-2*(x*x+z*z));
            }
            // A small, low-frequency deterministic perturbation, subordinate to the named macroforms.
            float fade = Mathf.SmoothStep(0,1,Mathf.Clamp01((p.core.minX+95-point.x)/95));
            sum += fade*p.mesoscaleMeters*(Mathf.PerlinNoise((point.x+(p.seed%997))* .018f,(point.y+(p.seed%613))*.018f)-.5f);
            return sum;
        }
        float Drain(Vector2 point, float height)
        {
            float distance = float.PositiveInfinity, bed = height;
            for (int i=1;i<p.drainage.Length;i++)
            {
                var a=p.drainage[i-1];var b=p.drainage[i];var start=new Vector2(a.x,a.z);var d=new Vector2(b.x-a.x,b.z-a.z);
                float t=Mathf.Clamp01(Vector2.Dot(point-start,d)/Mathf.Max(.0001f,d.sqrMagnitude));float dist=Vector2.Distance(point,start+d*t);
                if(dist<distance){distance=dist;bed=Mathf.Lerp(a.y,b.y,t);}
            }
            float cut=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/p.drainageHalfWidth));
            return Mathf.Lerp(height,Mathf.Min(height,bed),cut);
        }
        public float Influence(Vector2 point)
        {
            float distance=protections.Distance(point);
            foreach(var f in p.foundations) distance=Mathf.Min(distance,f.area.Distance(point));
            foreach(var r in p.routes) if(r.preserveExisting)
                for(int i=1;i<r.points.Length;i++)distance=Mathf.Min(distance,Mathf.Max(0,ForestLayout.SegmentDistance(point,r.points[i-1],r.points[i])-r.width*.5f-r.shoulder));
            // Existing creek and both bridge approaches stay at surveyed world heights through G1.
            if(p.core.Contains(point))distance=Mathf.Min(distance,Mathf.Max(0,Mathf.Abs(point.x-ForestLayout.RiverX(point.y))-18));
            return Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/p.protectionBlendMeters));
        }
        public float CoreHeight(Vector2 point)
        {
            float old=Original(point);return Mathf.Lerp(old,Drain(point,old+Shape(point)),Influence(point));
        }
        public float Height(Vector2 point)
        {
            if(p.core.Contains(point))return CoreHeight(point);
            if(!p.extension.Contains(point))throw new ArgumentOutOfRangeException(nameof(point),"Outside the planned two tiles.");
            var edge=new Vector2(p.core.minX,point.y);float seam=CoreHeight(edge),d=p.core.minX-point.x;
            float cell=p.core.Width/(p.heightmapResolution-1);
            float gradient=(CoreHeight(edge+Vector2.right*cell)-seam)/cell;
            float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(d/p.seamBlendMeters));
            // At d=0 both tiles call the same world-height function. Preserve the inward edge tangent too.
            float continuation=seam-gradient*d*Mathf.Exp(-d/p.seamBlendMeters);
            return Mathf.Lerp(continuation,Drain(point,continuation+Shape(point)-Shape(edge)),blend);
        }
        public static float ProjectWorldHeight(float worldHeight,float newOriginY,float newRange)
        {
            if(!WorldReconstructionProfile.Finite(worldHeight)||!WorldReconstructionProfile.Finite(newOriginY)||!WorldReconstructionProfile.Finite(newRange)||newRange<=0)
                throw new ArgumentException("Invalid vertical projection.");
            float value=(worldHeight-newOriginY)/newRange;
            if(value<0||value>1)throw new ArgumentOutOfRangeException(nameof(worldHeight),"Height interval would clip terrain. Expand the derivative interval.");
            return value;
        }
    }
    // Same spatial-bin concept as ForestSite; bounds are supplied by the read-only scene audit.
    public sealed class ProtectionIndex
    {
        const float Cell=16;
        readonly Dictionary<Vector2Int,List<Bounds>> bins=new Dictionary<Vector2Int,List<Bounds>>();
        readonly float blend;
        public int Count {get;private set;}
        public ProtectionIndex(float blendMeters){blend=blendMeters;}
        public void Add(Bounds bounds,float margin=0)
        {
            bounds.Expand(new Vector3(margin*2,0,margin*2));Count++;
            for(int x=Mathf.FloorToInt((bounds.min.x-blend)/Cell);x<=Mathf.FloorToInt((bounds.max.x+blend)/Cell);x++)
                for(int z=Mathf.FloorToInt((bounds.min.z-blend)/Cell);z<=Mathf.FloorToInt((bounds.max.z+blend)/Cell);z++)
                {var key=new Vector2Int(x,z);if(!bins.TryGetValue(key,out var list))bins[key]=list=new List<Bounds>();list.Add(bounds);}
        }
        public float Distance(Vector2 p)
        {
            float d=blend;if(bins.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x/Cell),Mathf.FloorToInt(p.y/Cell)),out var list))
                foreach(var b in list)d=Mathf.Min(d,new Vector2(Mathf.Max(b.min.x-p.x,0,p.x-b.max.x),Mathf.Max(b.min.z-p.y,0,p.y-b.max.z)).magnitude);
            return d;
        }
    }
}
