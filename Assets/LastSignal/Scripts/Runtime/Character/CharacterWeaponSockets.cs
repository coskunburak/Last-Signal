using UnityEngine;
namespace LastSignal
{
    /// <summary>Cosmetic transforms only. No projectile, ammo or collision authority.</summary>
    public sealed class CharacterWeaponSockets : MonoBehaviour
    {
        // Grip transforms describe palm contact: +Z points along the fingers, +Y into the grip.
        public Transform RightGrip, LeftGrip, AimReference, Muzzle, ReloadReference, Magazine, StockContact;
        public bool HasFirearmGrips => RightGrip && LeftGrip && AimReference && Muzzle && ReloadReference && StockContact;
    }
}
