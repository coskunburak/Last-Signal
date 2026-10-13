using UnityEngine;
namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Combat/Pistol Visual Variant")]
    public sealed class PistolVisualVariant : ScriptableObject
    {
        public const string DxtId = "pistol.dxt", GlockId = "pistol.glock17", CustomId = "pistol.custom9mm";
        public string Id, DisplayName, SourcePath;
        public GameObject VisualPrefab;
        public CharacterWeaponPoseProfile Pose;
        public static bool KnownOrLegacy(string id) => string.IsNullOrEmpty(id) || id == DxtId || id == GlockId || id == CustomId;
        public bool Valid => !string.IsNullOrEmpty(Id) && KnownOrLegacy(Id) && !string.IsNullOrWhiteSpace(DisplayName) &&
            VisualPrefab && Pose && Pose.WorldPrefab == VisualPrefab && Pose.Pistol && Pose.ValidGripData && Pose.Firearm && Pose.Firearm.WeaponId == "weapon.pistol" &&
            Pose.FpsPistolFingers && Pose.FpsPistolFingers.Valid && Pose.WorldPistolFingers && Pose.WorldPistolFingers.Valid &&
            VisualPrefab.GetComponent<CharacterWeaponSockets>() is CharacterWeaponSockets sockets && sockets.ValidFor(Pose) && sockets.Magazine;
    }
}
