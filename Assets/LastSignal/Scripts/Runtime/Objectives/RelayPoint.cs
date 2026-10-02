using UnityEngine;
namespace LastSignal.Objectives
{
    public enum RelayPointKind { Radio, Relay }
    public sealed class RelayPoint : MonoBehaviour, IInteractable
    {
        public string stableId;
        public RelayMission mission;
        public RelayPointKind kind;
        public bool Available => mission && mission.Progress != null;
        public string Prompt => kind == RelayPointKind.Radio ? "Read / listen to cabin radio" : mission.Pending != 0 ? "Cancel relay repair" : "Repair relay (fuse + wrench)";
        public bool TryInteract()
        {
            if (!Available) return false;
            if (kind == RelayPointKind.Radio) return mission.Discover();
            if (mission.Pending != 0) { mission.CancelRepair(); return true; }
            return mission.BeginRepair() != 0;
        }
    }
}
