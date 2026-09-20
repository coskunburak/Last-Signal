using UnityEngine;

namespace LastSignal.Shelter
{
    public enum ShelterAction { Prepare, Leave, Return }
    [DisallowMultipleComponent]
    public sealed class ShelterPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] ShelterLoop loop;
        [SerializeField] ShelterAction action;
        [SerializeField, Range(1,4)] float useRange = 2.5f;
        public ShelterLoop Loop => loop;
        public ShelterAction Action => action;
        public float UseRange => useRange;
        public bool Available => isActiveAndEnabled && loop && loop.CanUse(this);
        public string Prompt => action == ShelterAction.Prepare ? "E — Prepare / Shelter storage" : action == ShelterAction.Leave ? "E — Leave shelter" : "E — Return to shelter";
        public bool TryInteract() => Available && loop.TryUse(this);
        void OnDrawGizmosSelected() { Gizmos.color = action == ShelterAction.Return ? Color.green : Color.yellow; Gizmos.DrawWireSphere(transform.position,useRange); }
    }
}
