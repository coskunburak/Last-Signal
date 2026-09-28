using FPS.Scripts.Characters;
using Main.StatsAndManipulation;
using UnityEngine;
using UnityEngine.Serialization;

namespace FPS.Scripts.Items.Weapons{
    public class OnHitWeaponKickback : MonoBehaviour{
        [FormerlySerializedAs("recoilSharpness")]
        [Header("Weapon Kickback when not Aiming")]
        [Tooltip("This will affect how fast the recoil moves the weapon, the bigger the value, the fastest")]
        public float kickbackSharpness = 50f;
        [FormerlySerializedAs("maxRecoilDistance")] [Tooltip("Maximum distance the recoil can affect the weapon")]
        public float maxKickbackDistance = 0.5f;
        [FormerlySerializedAs("recoilRestitutionSharpness")] [Tooltip("How fast the weapon goes back to it's original position after the recoil is finished")]
        public float kickbackRestitutionSharpness = 10f;
        [FormerlySerializedAs("recoilForce")] [Tooltip("Amount of kickback on each shot when the weapon is fired")]
        public float kickbackForce = 1.0f;
        
        public Vector3 mWeaponKickbackLocalPosition { private set; get; }
        Vector3 mAccumulatedKickback;
        
        private void Awake(){
            Health health = GetComponent<Health>();
            // "Kickback" is accumulated when player is hit by an enemy (whether they parry or take the damage)
            health.onHitWithDamage += AddHitKickback;
        }

        private void AddHitKickback(DamageInfo damageInfo){
            if (!damageInfo.createsPlayerWeaponKickbackAnimation) return;
            mAccumulatedKickback += Vector3.back * kickbackForce;
            mAccumulatedKickback = Vector3.ClampMagnitude(mAccumulatedKickback, maxKickbackDistance);
        }

        private void Update(){
            if (mWeaponKickbackLocalPosition.z >= mAccumulatedKickback.z * 0.99f)
            {
                mWeaponKickbackLocalPosition = Vector3.Lerp(mWeaponKickbackLocalPosition, mAccumulatedKickback, kickbackSharpness * Time.deltaTime);
            }
            // otherwise, move weapon position to make it recover towards its resting pose
            else
            {
                mWeaponKickbackLocalPosition = Vector3.Lerp(mWeaponKickbackLocalPosition, Vector3.zero, kickbackRestitutionSharpness * Time.deltaTime);
                mAccumulatedKickback = mWeaponKickbackLocalPosition;
            }
        } 
    }
}