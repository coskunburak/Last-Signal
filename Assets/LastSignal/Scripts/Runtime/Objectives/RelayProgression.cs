using System;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;

namespace LastSignal.Objectives
{
    public enum ObjectiveStage { Locked, Available, Active, RequirementsMet, Completed }
    public enum RelayBeat { Radio, Fuse, Tools, Repair, Listen }

    [Serializable] public sealed class RelaySnapshot
    {
        public int version, definitionRevision;
        public string definitionId, repairReceipt, rewardReceipt;
        public bool radio, acquired, tools, listened;
        public int phase, rewardCount, completionCount;
        public double completedAt;
    }

    /// <summary>Single session authority. Persistent facts are monotonic; held resources are checked at commit.
    /// The reward is Contact intel, stored with its receipt, not an inventory spawn callback.</summary>
    public sealed class RelayProgression
    {
        public const string DefinitionId = "objective.relay-contact";
        public const string FuseId = "quest.relay-fuse";
        public const string RepairReceipt = "relay.repair.v1";
        public const string RewardReceipt = "reward.contact-intel.v1";
        public static readonly string[] NodeIds = { "relay.radio", "relay.fuse", "relay.tools", "relay.repair", "relay.listen" };
        RelaySnapshot state = Initial();
        public bool Radio => state.radio;
        public bool Acquired => state.acquired;
        public bool Repaired => state.repairReceipt == RepairReceipt;
        public bool Listened => state.listened;
        public int Phase => state.phase;
        public int RewardCount => state.rewardCount;
        public event Action Changed;
        static RelaySnapshot Initial() => new RelaySnapshot { version = 1, definitionRevision = 1, definitionId = DefinitionId };
        public RelaySnapshot Capture() => new RelaySnapshot { version=state.version, definitionRevision=state.definitionRevision, definitionId=state.definitionId,
            radio=state.radio, acquired=state.acquired, tools=state.tools, listened=state.listened, phase=state.phase,
            repairReceipt=state.repairReceipt, rewardReceipt=state.rewardReceipt, rewardCount=state.rewardCount,
            completionCount=state.completionCount, completedAt=state.completedAt };
        public static bool Empty(RelaySnapshot s) => s == null || (s.version == 0 && s.definitionRevision == 0 && string.IsNullOrEmpty(s.definitionId) &&
            string.IsNullOrEmpty(s.repairReceipt) && string.IsNullOrEmpty(s.rewardReceipt) && !s.radio && !s.acquired && !s.tools && !s.listened &&
            s.phase == 0 && s.rewardCount == 0 && s.completionCount == 0 && s.completedAt == 0);
        public static bool Valid(RelaySnapshot s, double now)
        {
            if (s == null) return true; // Explicit old-save default; malformed non-null sections fail.
            if (s.version != 1 || s.definitionRevision != 1 || s.definitionId != DefinitionId || !double.IsFinite(s.completedAt) || s.completedAt < 0 || s.completedAt > now) return false;
            bool done = s.repairReceipt == RepairReceipt;
            if (done) return s.radio && s.acquired && s.tools && s.rewardReceipt == RewardReceipt && s.rewardCount == 1 && s.completionCount == 1 && s.phase == 1;
            return string.IsNullOrEmpty(s.repairReceipt) && string.IsNullOrEmpty(s.rewardReceipt) && s.rewardCount == 0 && s.completionCount == 0 && s.phase == 0 && s.completedAt == 0 && !s.listened;
        }
        public void Restore(RelaySnapshot snapshot, double now)
        {
            if (!Valid(snapshot, now)) throw new ArgumentException("Invalid relay progression.");
            state = snapshot ?? Initial(); state = Capture(); // Detached; restore emits no gameplay completion.
        }
        public void DiscoverRadio() { if (state.radio) return; state.radio = true; Publish(); }
        public void ObserveInventory(InventoryContainer inventory, ItemDefinition fuse, ItemDefinition tool)
        {
            bool acquired = inventory.GetTotalQuantity(fuse) > 0, tools = inventory.GetTotalQuantity(tool) > 0;
            if ((!acquired || state.acquired) && (!tools || state.tools)) return;
            state.acquired |= acquired; state.tools |= tools; Publish();
        }
        public ObjectiveStage Stage(RelayBeat beat)
        {
            switch (beat)
            {
                case RelayBeat.Radio: return state.radio ? ObjectiveStage.Completed : ObjectiveStage.Active;
                case RelayBeat.Fuse: return !state.radio ? ObjectiveStage.Locked : state.acquired ? ObjectiveStage.Completed : ObjectiveStage.Active;
                case RelayBeat.Tools: return !state.radio || !state.acquired ? ObjectiveStage.Locked : state.tools ? ObjectiveStage.Completed : ObjectiveStage.Active;
                case RelayBeat.Repair: return Repaired ? ObjectiveStage.Completed : !state.radio ? ObjectiveStage.Locked : state.acquired && state.tools ? ObjectiveStage.RequirementsMet : ObjectiveStage.Active;
                case RelayBeat.Listen: return state.listened ? ObjectiveStage.Completed : Repaired ? ObjectiveStage.Available : ObjectiveStage.Locked;
                default: throw new ArgumentOutOfRangeException(nameof(beat));
            }
        }
        public bool Repair(InventoryContainer inventory, ItemDefinition fuse, ItemDefinition tool, double now)
        {
            if (Repaired || !state.radio || !double.IsFinite(now) || now < 0 || inventory.GetTotalQuantity(tool) < 1) return false;
            bool committed = inventory.Exchange(fuse, 1, null, 0, () => {
                state.acquired = state.tools = true;
                state.repairReceipt = RepairReceipt; state.rewardReceipt = RewardReceipt;
                state.phase = state.rewardCount = state.completionCount = 1; state.completedAt = now;
            });
            if (committed) Publish();
            return committed;
        }
        public void Listen() { if (!Repaired || state.listened) return; state.listened = true; Publish(); }
        void Publish()
        {
            if (Changed == null) return;
            foreach (Action listener in Changed.GetInvocationList()) try { listener(); } catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }
    }
}
