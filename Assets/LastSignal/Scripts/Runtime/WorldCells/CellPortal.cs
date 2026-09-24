using UnityEngine;
namespace LastSignal.WorldCells
{
    /// <summary>Solid terminal: proximity prefetch, explicit interaction and destination-ready gate.</summary>
    public sealed class CellPortal : MonoBehaviour, IInteractable
    {
        public string destination;
        public Vector3 residentEntry;
        WorldCellManager manager;
        float nextCheck;
        public void Bind(WorldCellManager owner) => manager = owner;
        public bool Available => manager && isActiveAndEnabled;
        public string Prompt => destination == "resident" ? "E — Return to shelter approach" :
            manager && manager.State(destination) == CellState.Ready ? "E — Enter " + destination : "Loading " + destination + "…";
        void Start() { if (!manager) manager = FindAnyObjectByType<WorldCellManager>(); }
        void Update()
        {
            if (!manager || destination == "resident" || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .2f;
            var flow = manager.GetComponent<SessionFlow>();
            if (flow.Player && !flow.Restoring && (flow.Player.transform.position-transform.position).sqrMagnitude < 36) manager.Request(destination);
        }
        public bool TryInteract()
        {
            if (!Available) return false;
            var flow = manager.GetComponent<SessionFlow>();
            if (!flow.Player || (flow.Player.transform.position-transform.position).sqrMagnitude > 9) return false;
            if (destination == "resident") return manager.ReturnToResident(residentEntry);
            manager.Request(destination); return manager.TryEnter(destination);
        }
    }
}
