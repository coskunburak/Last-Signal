using UnityEngine;
namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Combat/Melee Weapon")]
    public sealed class MeleeWeaponDefinition : ScriptableObject
    {
        public const string CrowbarId = "weapon.crowbar";
        public const string BladeId = "weapon.blade";
        public const string AxeId = "weapon.axe";
        public string WeaponId = CrowbarId;
        [Min(.01f)] public float WindupSeconds = .22f, ActiveSeconds = .12f, RecoverySeconds = .46f;
        [Min(.01f)] public float Range = 1.8f, Radius = .3f, Damage = 35;
        [Min(.01f)] public float StaminaCost = PlayerStamina.MeleeAttackCost;
        public bool Heavy;
        public bool Valid => !string.IsNullOrWhiteSpace(WeaponId) && Positive(WindupSeconds) && Positive(ActiveSeconds) &&
            Positive(RecoverySeconds) && Positive(Range) && Positive(Radius) && Positive(Damage) && Positive(StaminaCost);
        static bool Positive(float v) => float.IsFinite(v) && v > 0;
    }
}
