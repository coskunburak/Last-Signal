using UnityEngine;

namespace LastSignal
{
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform origin;
        [SerializeField, Min(.1f)] float rangeMeters = 2.2f;
        [SerializeField] LayerMask visibilityMask = ~(1 << 2);
        public string Prompt { get; private set; } = "";
        public void Configure(PlayerInputReader reader, Transform view) { input = reader; origin = view; }
        void Awake()
        {
            if (!input || !origin) { Debug.LogError("Interaction requires input and view origin.", this); enabled = false; }
        }
        void OnEnable() { if (input) input.InteractRequested += OnInteract; }
        void OnDisable() { if (input) input.InteractRequested -= OnInteract; Prompt = ""; }
        void Update()
        {
            IInteractable target = Resolve();
            Prompt = target == null ? "" : target.Prompt;
        }
        public IInteractable Resolve()
        {
            if (!isActiveAndEnabled || !input.GameplayActive) return null;
            if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, rangeMeters,
                    visibilityMask, QueryTriggerInteraction.Ignore)) return null;
            // First blocking collider wins, including ordinary walls. Never ray through them.
            var target = hit.collider.GetComponentInParent<IInteractable>();
            return target is Behaviour b && b.isActiveAndEnabled && target.Available ? target : null;
        }
        public bool TryInteract()
        {
            var target = Resolve(); // Revalidate at commit; never trust last frame's preview.
            return target != null && target.TryInteract();
        }
        void OnInteract() => TryInteract();
    }
}
