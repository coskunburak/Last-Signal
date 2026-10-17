using UnityEngine;
namespace LastSignal
{
    /// <summary>Cosmetic transforms only. No projectile, ammo or collision authority.</summary>
    public sealed class CharacterWeaponSockets : MonoBehaviour
    {
        // Grip transforms describe palm contact: +Z points along the fingers, +Y into the grip.
        public Transform RightGrip, LeftGrip, AimReference, Muzzle, ReloadReference, Magazine, StockContact;
        // Existing RightGrip/LeftGrip serialization remains intact.
        public Transform PalmContact, WeaponVisualRoot, SilhouetteReference;

        [Tooltip("TPS support-palm contact. Parent directly under the rigid weapon root.")]
        public Transform PistolSupport;

        public bool HasWorldPistolSupport =>
            ValidContact(PistolSupport) &&
            PistolSupport.parent == transform &&
            PistolSupport != PrimaryGrip &&
            (!Magazine ||
                (PistolSupport != Magazine && !PistolSupport.IsChildOf(Magazine)));
        public Transform PrimaryGrip => PalmContact ? PalmContact : RightGrip;
        public Transform SecondaryGrip => LeftGrip;
        public bool ValidFor(CharacterWeaponPoseProfile profile)
        {
            if (!profile || !profile.ValidGripData) return false;
            if (profile.TwoHandedMelee) return ValidContact(PrimaryGrip) && ValidContact(SecondaryGrip) &&
                PrimaryGrip != SecondaryGrip && Vector3.Distance(PrimaryGrip.position, SecondaryGrip.position) > .08f;
            if (!profile.SingleHanded) return profile.Firearm ? HasFirearmGrips : true;
            if (!ValidContact(PrimaryGrip) || profile.PrimaryHand != WeaponPrimaryHand.Right) return false;
            return !profile.Pistol || (ValidContact(AimReference) && ValidContact(Muzzle));
        }
        bool ValidContact(Transform contact)
        {
            if (!contact || !contact.IsChildOf(transform)) return false;
            var p = contact.position; var s = contact.lossyScale; var q = contact.rotation;
            return float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z) &&
                float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) && float.IsFinite(q.w) &&
                float.IsFinite(s.x) && float.IsFinite(s.y) && float.IsFinite(s.z) && s.x > 0 && s.y > 0 && s.z > 0 &&
                Mathf.Abs(s.x - s.y) < .001f && Mathf.Abs(s.x - s.z) < .001f;
        }
        public static void Attach(Transform visual, Transform contact, Vector3 position, Quaternion rotation)
        {
            // One direction only: a resolved palm owns the visual. Never solve back from this result.
            visual.rotation = rotation * Quaternion.Inverse(Quaternion.Inverse(visual.rotation) * contact.rotation);
            visual.position += position - contact.position;
        }
        public bool HasFirearmGrips => RightGrip && LeftGrip && AimReference && Muzzle && ReloadReference && StockContact;
    }
}
