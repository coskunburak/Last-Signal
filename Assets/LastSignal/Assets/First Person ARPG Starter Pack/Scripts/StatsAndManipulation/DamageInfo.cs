using System;
using UnityEngine;

namespace Main.StatsAndManipulation{
    [Serializable]
    public struct DamageInfo : ICloneable{
        public float damageAmount;
        [Tooltip("Can damage blockers (i.e. parrying) effect this damage")]
        public bool isBlockable;
        [Tooltip("Should the weapon be flung back when this damage is applied to the player")]
        public bool createsPlayerWeaponKickbackAnimation;
        [Tooltip("Applies force on damage dealt if the character has an attached knockback component")]
        public float targetKnockbackOnHitForce;
        [Tooltip("Potentially useful for area damage")]
        public bool isExplosionDamage;
        public GameObject damageSource{ get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}