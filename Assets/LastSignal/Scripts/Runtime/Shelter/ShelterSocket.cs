using UnityEngine;
namespace LastSignal.Shelter
{
    [DisallowMultipleComponent]
    public sealed class ShelterSocket : MonoBehaviour, IInteractable
    {
        public ShelterSite site;
        public ShelterModule module;
        public string socketId;
        public Vector3 clearance = new Vector3(.3f, .3f, .3f);
        readonly Collider[] overlaps = new Collider[16];
        public bool Clear
        {
            get
            {
                if (!float.IsFinite(clearance.x) || !float.IsFinite(clearance.y) || !float.IsFinite(clearance.z) || clearance.x <= 0 || clearance.y <= 0 || clearance.z <= 0) return false;
                int count = Physics.OverlapBoxNonAlloc(transform.position, clearance, overlaps, transform.rotation, ~((1 << 8) | (1 << 2)), QueryTriggerInteraction.Ignore);
                if (count == overlaps.Length) return false;
                for (int i = 0; i < count; i++) if (overlaps[i].GetComponentInParent<ShelterSocket>() != this) return false;
                return true;
            }
        }
        public bool Available => isActiveAndEnabled && site && site.CanAccess(transform, false);
        public string Prompt => "" + module + " socket / shelter production";
        public bool TryInteract() => Available && site.Open(this);
    }
}
