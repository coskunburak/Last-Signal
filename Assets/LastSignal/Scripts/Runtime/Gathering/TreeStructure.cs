using System;
using LastSignal.Persistence;
using UnityEngine;

namespace LastSignal.Gathering
{
    [Serializable] public sealed class TreeCutSnapshot
    {
        public int version = 1, side;
        public float depth;
        // V2: quantized authored geometry, configurable penetration, condition-scaled cutting effort.
        // V1 fields/defaults remain readable for previously saved physical checkpoints.
        public int cutUnits;
        public float cutStep;
        public int fractureGeneration;
        public long fractureRevision;
        public Vector3 localImpact, localDirection;
        public bool fractured, resting, impactRecorded, hasPose;
        public Vector3 position, velocity, angularVelocity;
        public Quaternion rotation;
    }

    /// <summary>One localized chord cut. Geometry and support use the same removed circular segment.
    /// Failure is a separate durable transition requested only by the physical joint adapter.</summary>
    public static class TreeStructure
    {
        public const int Sides = 8, Levels = 4, ProgressiveLevels = 16;
        public static TreeCutSnapshot CreateCut(ResourceNodeDefinition d) => new TreeCutSnapshot { version=2, cutStep=d.PhysicalPenetration };
        public static float ProgressiveDepth(int units) => Mathf.Min(.975f,Mathf.Clamp(units,0,ProgressiveLevels)/(float)ProgressiveLevels);
        public static int CuttingUnits(float step,int condition) => Mathf.Clamp(Mathf.CeilToInt(step*(.5f+.5f*Mathf.Clamp01(condition/100f))*ProgressiveLevels),1,ProgressiveLevels);
        public static Vector3 Direction(int side) => new Vector3(Mathf.Cos(side*Mathf.PI/4),0,Mathf.Sin(side*Mathf.PI/4));
        public static int Side(Vector3 point) => (Mathf.RoundToInt(Mathf.Atan2(point.z,point.x)*4/Mathf.PI)+8)%8;
        public static float Depth(long hits,int damage,int health) => Mathf.Min(.975f,hits*(float)damage/health);
        public static float RemainingArea(float depth)
        {
            float x=1-2*Mathf.Clamp01(depth);
            return 1-(Mathf.Acos(x)-x*Mathf.Sqrt(Mathf.Max(0,1-x*x)))/Mathf.PI;
        }
        public static float MomentCapacity(float radius,float effectiveStrength,float depth)
            => effectiveStrength*Mathf.PI*radius*radius*radius*.25f*Mathf.Pow(RemainingArea(depth),1.5f);
        // Centroid of the remaining circular segment, opposite the notch. Moving both joint anchors
        // to this support point changes the gravity lever arm without moving the tree transform.
        public static float SupportOffset(float radius,float depth)
        {
            float x=1-2*Mathf.Clamp(depth,0,.975f), area=RemainingArea(depth);
            return radius*(2f/3f)*Mathf.Pow(Mathf.Max(0,1-x*x),1.5f)/(Mathf.PI*Mathf.Max(area,.000001f));
        }
        public static bool Finite(Vector3 v) => float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
        public static bool Empty(TreeCutSnapshot t) => t==null || ((t.version==0 || t.version==1) && t.side==0 && t.depth==0 && t.fractureGeneration==0 && t.fractureRevision==0 &&
            t.localImpact==Vector3.zero && t.localDirection==Vector3.zero && !t.fractured && !t.resting && !t.impactRecorded && !t.hasPose &&
            t.cutUnits==0 && t.cutStep==0 && t.position==Vector3.zero && t.velocity==Vector3.zero && t.angularVelocity==Vector3.zero &&
            t.rotation.x==0 && t.rotation.y==0 && t.rotation.z==0 && t.rotation.w==0);
        public static bool Valid(ResourceNodeState n)
        {
            var t=n.tree;
            if((t.version!=1 && t.version!=2) || t.side<0 || t.side>=Sides || !float.IsFinite(t.depth)) return false;
            if(t.version==1 && (t.cutUnits!=0 || t.cutStep!=0 || Mathf.Abs(t.depth-Depth(n.revision,n.damage,n.maximumHealth))>.0001f)) return false;
            if(t.version==2 && (!float.IsFinite(t.cutStep) || t.cutStep<=0 || t.cutStep>1 || t.cutUnits<0 || t.cutUnits>ProgressiveLevels ||
                t.cutUnits<Math.Min(n.revision,ProgressiveLevels) || t.cutUnits>Math.Min(n.revision*CuttingUnits(t.cutStep,100),ProgressiveLevels) ||
                Mathf.Abs(t.depth-ProgressiveDepth(t.cutUnits))>.0001f)) return false;
            if(
                !Finite(t.localImpact)||!Finite(t.localDirection)||!Finite(t.position)||!Finite(t.velocity)||!Finite(t.angularVelocity)||
                t.velocity.sqrMagnitude>10000 || t.angularVelocity.sqrMagnitude>10000 ||
                (t.resting && !t.fractured) || (t.impactRecorded && !t.fractured) || (t.fractured && (n.revision==0 || !t.hasPose || t.fractureGeneration!=1 || t.fractureRevision!=n.revision)) ||
                (!t.fractured && (t.fractureGeneration!=0 || t.fractureRevision!=0))) return false;
            if(t.fractured && n.reward!=RewardLocation.Pending && !t.resting) return false;
            if(n.revision>0 && (Mathf.Abs(t.localDirection.sqrMagnitude-1)>.01f || Side(t.localImpact)!=t.side)) return false;
            if(!float.IsFinite(t.rotation.x)||!float.IsFinite(t.rotation.y)||!float.IsFinite(t.rotation.z)||!float.IsFinite(t.rotation.w) ||
                (t.resting && (t.velocity.sqrMagnitude>.000001f || t.angularVelocity.sqrMagnitude>.000001f))) return false;
            if(t.hasPose && !SaveValidation.Pose(new TransformSnapshot {x=t.position.x,y=t.position.y,z=t.position.z,
                qx=t.rotation.x,qy=t.rotation.y,qz=t.rotation.z,qw=t.rotation.w})) return false;
            return n.health==(t.fractured?0:Math.Max(1,n.maximumHealth-(int)n.revision*n.damage));
        }
        public static HarvestReason Cut(GatheringSnapshot current,string id,long revision,string session,long swing,
            string tool,Vector3 localImpact,Vector3 localDirection,out GatheringSnapshot candidate)
        {
            candidate=null;
            if(!GatheringState.Valid(current)||!GatheringState.Id(session)||swing<=0||current.sequence==long.MaxValue||
                !Finite(localImpact)||!Finite(localDirection)||localDirection.sqrMagnitude<.5f) return HarvestReason.Invalid;
            if(tool!=MeleeWeaponDefinition.AxeId) return HarvestReason.WrongTool;
            if(current.actorSession==session && swing<=current.lastSwing) return HarvestReason.Duplicate;
            var n=GatheringState.Find(current,id);
            if(n?.tree==null) return HarvestReason.Invalid;
            if(n.revision!=revision) return HarvestReason.StaleRevision;
            if(n.tree.fractured) return HarvestReason.Depleted;
            if(current.axeCondition<1) return HarvestReason.BrokenTool;
            if(n.tree.version==2 && n.tree.cutUnits==ProgressiveLevels) return HarvestReason.AwaitingFall;
            int side=Side(localImpact);
            if(n.revision>0 && side!=n.tree.side) return HarvestReason.Blocked; // Continue the established localized notch.
            candidate=GatheringState.Copy(current); n=GatheringState.Find(candidate,id);
            candidate.sequence++; candidate.actorSession=session; candidate.lastSwing=swing; candidate.axeCondition--;
            n.revision++; n.health=Math.Max(1,n.maximumHealth-(int)n.revision*n.damage); n.stage=ResourceStage.Damaged;
            n.tree.side=side;
            if(n.tree.version==2)
            {
                n.tree.cutUnits=Math.Min(ProgressiveLevels,n.tree.cutUnits+CuttingUnits(n.tree.cutStep,current.axeCondition));
                n.tree.depth=ProgressiveDepth(n.tree.cutUnits);
            }
            else n.tree.depth=Depth(n.revision,n.damage,n.maximumHealth);
            // Keep the first accepted cut location/direction stable across later contacts.
            if(n.revision==1) { n.tree.localImpact=localImpact; n.tree.localDirection=localDirection.normalized; }
            return HarvestReason.Accepted;
        }
        public static HarvestReason Fracture(GatheringSnapshot current,string id,long expectedRevision,out GatheringSnapshot candidate)
        {
            candidate=null;
            if(!GatheringState.Valid(current)||current.sequence==long.MaxValue) return HarvestReason.Invalid;
            var n=GatheringState.Find(current,id);
            if(n?.tree==null || n.revision==0 || !n.tree.hasPose) return HarvestReason.Invalid;
            if(n.revision!=expectedRevision) return HarvestReason.StaleRevision;
            if(n.tree.fractured) return HarvestReason.Duplicate;
            candidate=GatheringState.Copy(current); candidate.sequence++; n=GatheringState.Find(candidate,id);
            n.tree.fractured=true; n.tree.fractureGeneration=1; n.tree.fractureRevision=n.revision; n.health=0; n.stage=ResourceStage.FelledPresentation;
            n.remaining=0; n.yieldSequence=1; n.reward=RewardLocation.Pending;
            n.pickup=new WorldItemSnapshot { id=n.id+":yield:1",definitionId=n.outputId,origin=WorldItemOrigin.Harvest,
                quantity=n.budget,disposition=EntityDisposition.Present };
            return HarvestReason.Accepted;
        }
    }
}
