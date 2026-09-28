using UnityEngine;
namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Combat/Melee Weapon")]
    public sealed class MeleeWeaponDefinition : ScriptableObject
    {
        public const string CrowbarId = "weapon.crowbar";
        [Min(.01f)] public float WindupSeconds = .22f, ActiveSeconds = .12f, RecoverySeconds = .46f;
        [Min(.01f)] public float Range = 1.8f, Radius = .3f, Damage = 35;
        public bool Valid => Positive(WindupSeconds) && Positive(ActiveSeconds) && Positive(RecoverySeconds) && Positive(Range) && Positive(Radius) && Positive(Damage);
        static bool Positive(float v) => float.IsFinite(v) && v > 0;
    }
}
