using Main.StatsAndManipulation;
using UnityEngine;

namespace Main.Items.Weapons.Melee {
    [CreateAssetMenu(fileName = "Swing Stats", menuName = "Stats/Swing Stats", order = 0)]
    public class SwingStats : ScriptableObject{
        [Tooltip("Modifies the total range of the swing (high value means high maximum distance form which enemies can be hit)")]
        public float hitDistanceRangeMultiplier = 1;
        [Tooltip("Modifies the breadth  of the swing (how wide it reaches)")]
        public float motionRangeMultiplier = 1;
        [Tooltip("Value that is added to the attack animation speed modifier for this swing (use to make attacks faster or slower)")]
        public float attackSpeedMod = 0;
        [Tooltip("Added modification to the charge animation speed modifier for this swing (use to make charging faster or slower)")]
        public float chargeSpeedMod = 0;
        [Tooltip("Value that is added or subtracted from the original damage dealt when executing this swing (if base damage is 100 and this value is 8, final damage would be 92)")]
        public float damageMod = 0;
        [Tooltip("Value added to the enemy knockback when hit by this attack")]
        public float knockbackMod = 0;
        [Tooltip("Controls the motion of the swing over time along the axis of the swing")]
        public AnimationCurve motionOverTime;
        [Tooltip("Controls the range of the swing over time")]
        public AnimationCurve hitDistanceOverTime;
        [Tooltip("Determines if a swing can damage multiple enemies")]
        public bool canDamageMultipleEnemies = true;
    }
}