using UnityEngine;

namespace LastSignal
{
    public sealed class DoorInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] Transform hinge;
        [SerializeField] BoxCollider leaf;
        [SerializeField, Range(30, 150)] float openAngleDegrees = 95;
        [SerializeField, Min(.1f)] float durationSeconds = .65f;
        [SerializeField] LayerMask obstructionMask = ~0;
        readonly Collider[] overlaps = new Collider[32];
        Quaternion closedRotation;
        float angle, destination;
        public bool IsOpen { get; private set; }
        public bool Busy { get; private set; }
        public bool Obstructed { get; private set; }
        public int AcceptedRequests { get; private set; }
        public bool Available => isActiveAndEnabled && !Busy;
        public string Prompt => IsOpen ? "E — Kapat" : "E — Aç";
        public void Configure(Transform pivot, BoxCollider collider) { hinge = pivot; leaf = collider; }
        void Awake()
        {
            if (!hinge || !leaf) { Debug.LogError("Door requires hinge and leaf collider.", this); enabled = false; return; }
            closedRotation = hinge.localRotation;
        }
        public bool TryInteract()
        {
            if (!Available) return false;
            destination = IsOpen ? 0 : openAngleDegrees;
            Busy = true;
            Obstructed = false;
            AcceptedRequests++;
            return true;
        }
        void Update()
        {
            if (!Busy || Time.deltaTime <= 0) return;
            float remaining = openAngleDegrees / durationSeconds * Mathf.Min(Time.deltaTime, .1f);
            while (remaining > .001f && Busy)
            {
                float step = Mathf.Min(remaining, 2f);
                float next = Mathf.MoveTowards(angle, destination, step);
                Quaternion old = hinge.localRotation;
                hinge.localRotation = closedRotation * Quaternion.Euler(0, next, 0);
                int count = Physics.OverlapBoxNonAlloc(leaf.transform.TransformPoint(leaf.center),
                    Vector3.Scale(leaf.size, leaf.transform.lossyScale) * .5f + Vector3.one * .01f,
                    overlaps, leaf.transform.rotation, obstructionMask, QueryTriggerInteraction.Ignore);
                bool blocked = count == overlaps.Length;
                for (int i = 0; i < count; i++)
                    if (overlaps[i] && !overlaps[i].transform.IsChildOf(transform)) blocked = true;
                if (blocked)
                {
                    hinge.localRotation = old;
                    Obstructed = true;
                    // Remain busy and resume only when the obstruction clears. No capsule crushing.
                    break;
                }
                Obstructed = false;
                angle = next;
                remaining -= step;
                if (Mathf.Approximately(angle, destination)) { IsOpen = destination > 0; Busy = false; }
            }
        }
        public void ResetDoor()
        {
            angle = destination = 0;
            IsOpen = Busy = Obstructed = false;
            AcceptedRequests = 0;
            hinge.localRotation = closedRotation;
        }
    }
}
