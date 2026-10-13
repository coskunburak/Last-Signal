using UnityEngine;

namespace LastSignal
{
    public enum WeaponCategory { Unspecified, Rifle, Pistol, Shotgun }
    public enum FireMode { SemiAutomatic, Automatic }

    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Last Signal/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] string weaponId = "weapon.rifle";
        [SerializeField] string weaponName = "Unnamed";
        [SerializeField] WeaponCategory category = WeaponCategory.Rifle;

        [Header("Ammunition")]
        [SerializeField, Min(1)] int magazineCapacity = 30;
        [SerializeField] LastSignal.Inventory.Data.ItemDefinition ammunition;
        [SerializeField, Min(0)] int startingMagazine = 30;

        [Header("Fire")]
        [SerializeField] FireMode fireMode = FireMode.SemiAutomatic;
        [SerializeField, Min(1)] float roundsPerMinute = 600;
        [SerializeField, Min(0)] float muzzleObstructionRange = .5f;
        [SerializeField, Min(0)] float maxRange = 200f;
        [SerializeField, Min(0)] float baseDamage = 25f;

        [Header("Reload")]
        [SerializeField, Min(0)] float tacticalReloadSeconds = 1.8f;
        [SerializeField, Min(0)] float emptyReloadSeconds = 2.4f;
        [SerializeField, Range(0, 1)] float reloadCommitNormalized = .55f;
        [SerializeField, Range(0, 1)] float emptyReloadCommitNormalized = .55f;

        [Header("Equip / Unequip")]
        [SerializeField, Min(0)] float equipSeconds = .6f;
        [SerializeField, Min(0)] float unequipSeconds = .4f;

        [Header("ADS")]
        [SerializeField, Range(20, 90)] float adsFovDegrees = 55f;
        [SerializeField, Min(0)] float adsTransitionSeconds = .2f;
        [SerializeField, Range(0, 1)] float adsSensitivityMultiplier = .6f;

        [Header("Recoil")]
        [SerializeField, Min(0)] float recoilVerticalDegrees = 1.2f;
        [SerializeField] float recoilHorizontalRange = .4f;
        [SerializeField, Min(0)] float recoilRecoverySpeed = 8f;
        [SerializeField, Range(0, 1)] float adsRecoilMultiplier = .65f;
        [SerializeField, Min(0)] float visualRecoilKick = .015f;

        // Public read-only accessors — gameplay code queries these, never modifies them.
        public string WeaponName => weaponName;
        public string WeaponId => weaponId;
        public WeaponCategory Category => category;
        public int MagazineCapacity => magazineCapacity;
        public LastSignal.Inventory.Data.ItemDefinition Ammunition => ammunition;
        public int StartingMagazine => startingMagazine;
        public bool HasValidAmmunitionConfiguration => !string.IsNullOrWhiteSpace(weaponId) && ammunition && ammunition.Id.IsValid &&
            magazineCapacity > 0 && startingMagazine >= 0 && startingMagazine <= magazineCapacity &&
            ValidDuration(tacticalReloadSeconds) && ValidDuration(emptyReloadSeconds) &&
            ValidFraction(reloadCommitNormalized) && ValidFraction(emptyReloadCommitNormalized);
        public bool HasValidCombatConfiguration => HasValidAmmunitionConfiguration &&
            category != WeaponCategory.Unspecified && System.Enum.IsDefined(typeof(WeaponCategory), category) &&
            System.Enum.IsDefined(typeof(FireMode), fireMode) && float.IsFinite(roundsPerMinute) && roundsPerMinute > 0 &&
            float.IsFinite(maxRange) && maxRange > 0 && float.IsFinite(baseDamage) && baseDamage > 0 &&
            float.IsFinite(muzzleObstructionRange) && muzzleObstructionRange >= 0 &&
            float.IsFinite(equipSeconds) && equipSeconds >= 0 && float.IsFinite(unequipSeconds) && unequipSeconds >= 0 &&
            float.IsFinite(adsFovDegrees) && adsFovDegrees >= 20 && adsFovDegrees <= 90 &&
            float.IsFinite(adsTransitionSeconds) && adsTransitionSeconds >= 0 &&
            ValidFraction(adsSensitivityMultiplier) && float.IsFinite(recoilVerticalDegrees) && recoilVerticalDegrees >= 0 &&
            float.IsFinite(recoilHorizontalRange) && recoilHorizontalRange >= 0 &&
            float.IsFinite(recoilRecoverySpeed) && recoilRecoverySpeed >= 0 &&
            ValidFraction(adsRecoilMultiplier) && float.IsFinite(visualRecoilKick) && visualRecoilKick >= 0;
        static bool ValidDuration(float value) => value > 0 && !float.IsInfinity(value) && !float.IsNaN(value);
        static bool ValidFraction(float value) => value >= 0 && value <= 1 && !float.IsNaN(value);
        public FireMode FireMode => fireMode;
        public float RoundsPerMinute => roundsPerMinute;
        public float FireCooldownSeconds => 60f / Mathf.Max(1, roundsPerMinute);
        public float MuzzleObstructionRange => muzzleObstructionRange;
        public float MaxRange => maxRange;
        public float BaseDamage => baseDamage;
        public float TacticalReloadSeconds => tacticalReloadSeconds;
        public float EmptyReloadSeconds => emptyReloadSeconds;
        public float ReloadCommitNormalized => reloadCommitNormalized;
        public float EmptyReloadCommitNormalized => emptyReloadCommitNormalized;
        public float EquipSeconds => equipSeconds;
        public float UnequipSeconds => unequipSeconds;
        public float AdsFovDegrees => adsFovDegrees;
        public float AdsTransitionSeconds => adsTransitionSeconds;
        public float AdsSensitivityMultiplier => adsSensitivityMultiplier;
        public float RecoilVerticalDegrees => recoilVerticalDegrees;
        public float RecoilHorizontalRange => recoilHorizontalRange;
        public float RecoilRecoverySpeed => recoilRecoverySpeed;
        public float AdsRecoilMultiplier => adsRecoilMultiplier;
        public float VisualRecoilKick => visualRecoilKick;
    }
}
