using System;
using System.Collections.Generic;
using LastSignal.Persistence;
using UnityEngine;

namespace LastSignal.Gathering
{
    public enum ResourceStage { Standing, Damaged, Depleted, FelledPresentation }
    public enum RewardLocation { None, Pending, World, Consumed }
    public enum HarvestReason { Accepted, Invalid, WrongTool, BrokenTool, StaleRevision, Duplicate, Depleted, Busy, Blocked, PersistenceFailed, RecoveryRequired, PhysicsNotReady, AwaitingFall }
    [Serializable] public sealed class ResourceNodeState
    {
        public string id, definitionId, cellId, outputId;
        public TransformSnapshot anchor;
        public int definitionRevision, health, maximumHealth, damage, budget, remaining;
        public long revision, yieldSequence;
        public ResourceStage stage;
        public RewardLocation reward;
        public WorldItemSnapshot pickup;
        public int treeVersion; // 0 = legacy S003; 1 = optional S004 physical projection.
        public TreeCutSnapshot tree;
    }
    [Serializable] public sealed class GatheringSnapshot
    {
        public int version = 1, axeCondition = 100;
        public string manifestId;
        public int manifestRevision;
        public long sequence;
        public string actorSession;
        public long lastSwing;
        public ResourceNodeState[] nodes = Array.Empty<ResourceNodeState>();
    }
    /// <summary>Detached candidate transitions. Caller publishes the complete save BEFORE adopting a candidate.</summary>
    public static class GatheringState
    {
        public const int MaximumNodes = 64;
        public static bool Id(string s) => !string.IsNullOrWhiteSpace(s) && s.Length <= 128;
        public static GatheringSnapshot Copy(GatheringSnapshot s)
        { var copy=JsonUtility.FromJson<GatheringSnapshot>(JsonUtility.ToJson(s)); Normalize(ref copy,1); return copy; }
        public static ResourceNodeState Find(GatheringSnapshot s, string id)
        { if (s?.nodes != null) foreach (var n in s.nodes) if (n != null && n.id == id) return n; return null; }
        public static ResourceNodeState Create(string id, string cell, ResourceNodeDefinition d, TransformSnapshot anchor)
        {
            if (!Id(id) || id.Length > 100 || !Id(cell) || !d || !d.Valid || !SaveValidation.Pose(anchor)) throw new ArgumentException("Invalid resource authoring.");
            return new ResourceNodeState { id=id, cellId=cell, anchor=anchor, definitionId=d.definitionId, definitionRevision=d.revision,
                health=d.health, maximumHealth=d.health, damage=d.strikeDamage, budget=d.yield, remaining=d.yield, outputId=d.output.Id.Value, treeVersion=d.physicalTree?1:0, tree=d.physicalTree?TreeStructure.CreateCut(d):null };
        }
        public static HarvestReason Prepare(GatheringSnapshot current, string nodeId, long expectedRevision, string session,
            long swing, string tool, TransformSnapshot ground, out GatheringSnapshot candidate)
        {
            candidate = null;
            if (!Valid(current) || !Id(session) || swing <= 0 || current.sequence == long.MaxValue) return HarvestReason.Invalid;
            if (tool != MeleeWeaponDefinition.AxeId) return HarvestReason.WrongTool;
            if (current.actorSession == session && swing <= current.lastSwing) return HarvestReason.Duplicate;
            var before = Find(current, nodeId);
            if (before == null) return HarvestReason.Invalid;
            if (before.revision != expectedRevision) return HarvestReason.StaleRevision;
            if (before.tree != null) return HarvestReason.Invalid; // Physical trees use the localized cut transaction.
            if (before.health == 0) return HarvestReason.Depleted;
            if (current.axeCondition < 1) return HarvestReason.BrokenTool;
            if (ground != null && !SaveValidation.Pose(ground)) return HarvestReason.Invalid;
            candidate = Copy(current);
            var node = Find(candidate, nodeId);
            candidate.sequence++; candidate.actorSession=session; candidate.lastSwing=swing;
            candidate.axeCondition--; // An exactly-last condition point pays for this blow, then breaks.
            node.health = Math.Max(0, node.health-node.damage); node.revision++;
            node.stage = node.health == 0 ? ResourceStage.Depleted : ResourceStage.Damaged;
            if (node.health == 0)
            {
                node.yieldSequence = 1; node.remaining = 0;
                node.reward = ground == null ? RewardLocation.Pending : RewardLocation.World;
                node.pickup = new WorldItemSnapshot { id=node.id+":yield:1", definitionId=node.outputId,
                    origin=WorldItemOrigin.Harvest, disposition=EntityDisposition.Present, quantity=node.budget,
                    transform=ground };
            }
            return HarvestReason.Accepted;
        }
        public static bool Valid(GatheringSnapshot s)
        {
            if (s == null || s.version != 1 || !Id(s.manifestId) || s.manifestRevision<1 || s.axeCondition < 0 || s.axeCondition > 100 || s.sequence < 0 ||
                s.nodes == null || s.nodes.Length < 1 || s.nodes.Length > MaximumNodes || s.lastSwing < 0 ||
                (s.sequence == 0 ? s.lastSwing != 0 || !string.IsNullOrEmpty(s.actorSession) : !Id(s.actorSession) || s.lastSwing < 1)) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            long hits = 0;
            foreach (var n in s.nodes)
            {
                if (n == null || !SaveValidation.Pose(n.anchor) || !Id(n.id) || n.id.Length > 100 || !ids.Add(n.id) || !Id(n.cellId) || !Id(n.definitionId) ||
                    !Id(n.outputId) || n.definitionRevision < 1 || n.maximumHealth < 1 || n.maximumHealth > 10000 ||
                    n.damage < 1 || n.damage > n.maximumHealth || n.health < 0 || n.health > n.maximumHealth ||
                    n.budget < 1 || n.budget > 64 || n.revision < 0 || n.revision > 10000 ||
                    !Enum.IsDefined(typeof(ResourceStage), n.stage) || !Enum.IsDefined(typeof(RewardLocation), n.reward)) return false;
                if(n.tree==null ? n.treeVersion!=0 : n.treeVersion!=1) return false;
                hits += n.revision;
                if (n.tree != null)
                { if (!TreeStructure.Valid(n)) return false; }
                else if (n.health != Math.Max(0, n.maximumHealth - (long)n.revision * n.damage)) return false;
                if (n.health > 0)
                {
                    if (n.remaining != n.budget || n.yieldSequence != 0 || n.reward != RewardLocation.None || n.pickup != null ||
                        n.stage != (n.revision == 0 ? ResourceStage.Standing : ResourceStage.Damaged)) return false;
                }
                else
                {
                    var p = n.pickup;
                    if ((n.tree == null && n.revision != (n.maximumHealth+n.damage-1)/n.damage) || n.remaining != 0 || n.yieldSequence != 1 ||
                        (n.stage != ResourceStage.Depleted && n.stage != ResourceStage.FelledPresentation) ||
                        n.reward == RewardLocation.None || p == null || p.id != n.id+":yield:1" || !ids.Add(p.id) ||
                        p.definitionId != n.outputId || p.origin != WorldItemOrigin.Harvest || p.quantity < 0 || p.quantity > n.budget) return false;
                    if (n.reward == RewardLocation.Pending)
                    { if (p.quantity != n.budget || p.transform != null || p.disposition != EntityDisposition.Present) return false; }
                    else if (!SaveValidation.Pose(p.transform) ||
                        (n.reward == RewardLocation.World ? p.quantity < 1 || p.disposition != EntityDisposition.Present :
                         p.quantity != 0 || p.disposition != EntityDisposition.Consumed)) return false;
                }
            }
            return hits <= s.sequence && hits == 100-s.axeCondition;
        }
        // JsonUtility constructs empty objects for absent nullable fields; normalize exact empties only.
        public static void Normalize(ref GatheringSnapshot s, int marker)
        {
            if (s == null) return;
            // Unity 6 can serialize a null optional class using its field initializers,
            // while older versions materialize a zero-filled object. Accept only these
            // exact empty forms without an extension marker; never erase real progress.
            bool emptyDefaults=(s.version==0 && s.axeCondition==0) || (s.version==1 && s.axeCondition==100);
            if (marker == 0 && emptyDefaults && s.sequence == 0 && s.lastSwing == 0 &&
                string.IsNullOrEmpty(s.actorSession) && string.IsNullOrEmpty(s.manifestId) && s.manifestRevision==0 && (s.nodes == null || s.nodes.Length == 0)) { s=null; return; }
            if (s.nodes == null) return;
            foreach (var n in s.nodes)
            {
                if (n != null && n.treeVersion==0 && TreeStructure.Empty(n.tree)) n.tree=null;
                var p = n?.pickup;
                if (p == null) continue;
                if (EmptyPose(p.transform)) p.transform=null;
                if (string.IsNullOrEmpty(p.id) && string.IsNullOrEmpty(p.definitionId) && p.quantity == 0 &&
                    p.origin == 0 && p.disposition == 0 && p.transform == null) n.pickup=null;
            }
        }
        static bool EmptyPose(TransformSnapshot p) => p != null && p.x==0 && p.y==0 && p.z==0 && p.qx==0 && p.qy==0 && p.qz==0 && p.qw==0;
    }
}
