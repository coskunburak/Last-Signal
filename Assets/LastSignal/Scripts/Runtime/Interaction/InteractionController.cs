using UnityEngine;

namespace LastSignal
{
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform origin;
        [SerializeField, Min(.1f)] float rangeMeters = 2.2f;
        [SerializeField] LayerMask visibilityMask = ~(1 << 2);
        // A small cone retains an already visible target when the exact ray slips
        // past its edge. It never discovers a new target or bypasses a blocker.
        const float RetainAimDot = .997f;
        readonly RaycastHit[] hitBuffer = new RaycastHit[16];
        IInteractable retainedTarget;
        Collider retainedCollider;
        public event System.Action InteractionSucceeded;
        public event System.Action<IInteractable> InteractionPresented;
        public string Prompt { get; private set; } = "";
        public void Configure(PlayerInputReader reader, Transform view) { input = reader; origin = view; }
        void Awake()
        {
            if (!input || !origin) { Debug.LogError("Interaction requires input and view origin.", this); enabled = false; }
        }
        void OnEnable() { if (input) input.InteractRequested += OnInteract; }
        void OnDisable()
        { if (input) input.InteractRequested -= OnInteract; Prompt = ""; retainedTarget = null; retainedCollider = null; }
        void Update()
        {
            IInteractable target = Resolve();
            Prompt = target == null ? "" : target.Prompt;
        }
        public IInteractable Resolve()
        {
            if (!isActiveAndEnabled || !input || !input.GameplayActive || !origin) return ClearTarget();
            if (FirstHit(origin.position, origin.forward, rangeMeters, out var hit))
            {
                // The first collider owns occlusion, even if it is an ordinary wall.
                var target = ValidTarget(hit.collider);
                if (target == null) return ClearTarget();
                retainedTarget = target; retainedCollider = hit.collider;
                return target;
            }
            if (CanRetain()) return retainedTarget;
            return ClearTarget();
        }
        IInteractable ClearTarget() { retainedTarget = null; retainedCollider = null; return null; }
        static IInteractable ValidTarget(Collider collider)
        {
            if (!collider || !collider.enabled || !collider.gameObject.activeInHierarchy) return null;
            var target = collider.GetComponentInParent<IInteractable>();
            return target is Behaviour b && b.isActiveAndEnabled && target.Available ? target : null;
        }
        bool FirstHit(Vector3 start, Vector3 direction, float distance, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(start, direction, hitBuffer, distance,
                visibilityMask, QueryTriggerInteraction.Ignore);
            // A saturated buffer cannot establish the true first blocker. Fail closed.
            if (count >= hitBuffer.Length) return true;
            if (count == 0) return false;
            int winner = 0;
            for (int i = 1; i < count; i++)
            {
                float delta = hitBuffer[i].distance - hitBuffer[winner].distance;
                if (delta < -.0001f || (Mathf.Abs(delta) <= .0001f &&
                    EntityId.ToULong(hitBuffer[i].collider.GetEntityId()) <
                    EntityId.ToULong(hitBuffer[winner].collider.GetEntityId()))) winner = i;
            }
            nearest = hitBuffer[winner];
            return true;
        }
        bool CanRetain()
        {
            if (!(retainedTarget is Behaviour behaviour) || !behaviour || !behaviour.isActiveAndEnabled ||
                !retainedCollider || !retainedCollider.enabled || !retainedCollider.gameObject.activeInHierarchy ||
                !retainedTarget.Available) return false;
            Vector3 delta = retainedCollider.bounds.ClosestPoint(origin.position) - origin.position;
            float distance = delta.magnitude;
            if (distance <= .001f || distance > rangeMeters || Vector3.Dot(delta / distance, origin.forward) < RetainAimDot)
                return false;
            // Check the candidate's actual line of sight. A wall in this path clears it.
            return FirstHit(origin.position, delta / distance, distance + .05f, out var hit) &&
                hit.collider && ValidTarget(hit.collider) == retainedTarget;
        }
        public bool TryInteract()
        {
            var target = Resolve(); // Revalidate at commit; never trust last frame's preview.
            bool succeeded = target != null && target.TryInteract();
            if (succeeded)
            {
                InteractionSucceeded?.Invoke();
                InteractionPresented?.Invoke(target);
            }
            return succeeded;
        }
        void OnInteract() => TryInteract();
    }
}
