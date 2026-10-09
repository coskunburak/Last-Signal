using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Bridges PlayerInputReader combat events to the active WeaponController.
    /// Lives on the Player prefab. Manages weapon equip/unequip lifecycle.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] FirstPersonLook look;
        [SerializeField] Transform weaponParent; // Child of camera, holds weapon viewmodel
        [SerializeField] WeaponController startingWeapon;

        public enum CombatSlot { Firearm, Melee }
        [SerializeField] MeleeWeaponController startingMelee;
        MeleeWeaponController melee;
        public CombatSlot SelectedSlot { get; private set; }
        public MeleeWeaponController Melee => melee;
        public WeaponController Firearm => activeWeapon;
        WeaponController activeWeapon;
#if UNITY_EDITOR
        bool evidenceAimHeld;
        public void SetEvidenceAim(bool held) => evidenceAimHeld = held;
#endif
        FirstPersonMotor motor;

        void Awake() => motor = GetComponent<FirstPersonMotor>();

        public WeaponController ActiveWeapon => SelectedSlot == CombatSlot.Firearm ? activeWeapon : null;
        public event System.Action WeaponChanged;

        void Start()
        {
            if (!activeWeapon && startingWeapon)
                EquipWeapon(Instantiate(startingWeapon, weaponParent));
            if (startingMelee)
            {
                melee = Instantiate(startingMelee, weaponParent);
                melee.Initialize(gameObject, look ? look.View.transform : transform);
                var meleeStance = melee.GetComponent<MeleeStanceViewPresenter>();
                if (meleeStance)
                    meleeStance.Configure(GetComponent<PlayerStance>(), motor);
                if (noise != null) melee.BindNoise(noise, noiseTuning, noiseSource);
                melee.gameObject.SetActive(false);
            }
        }

        public void Configure(PlayerInputReader reader, FirstPersonLook fpLook, Transform wpnParent)
        {
            input = reader;
            look = fpLook;
            weaponParent = wpnParent;
        }

        void OnEnable()
        {
            if (!input) return;
            input.FirePressed += OnFirePressed;
            input.FireReleased += OnFireReleased;
            input.ReloadRequested += OnReload;
            input.MeleeSlotRequested += SelectMelee;
            input.FirearmSlotRequested += SelectFirearm;
        }

        void OnDisable()
        {
            if (!input) return;
            input.FirePressed -= OnFirePressed;
            input.FireReleased -= OnFireReleased;
            input.ReloadRequested -= OnReload;
            input.MeleeSlotRequested -= SelectMelee;
            input.FirearmSlotRequested -= SelectFirearm;
            CancelGameplayActions(false);
#if UNITY_EDITOR
            evidenceAimHeld = false;
#endif
        }

        /// <summary>
        /// Equip a weapon. Initializes and parents the weapon viewmodel.
        /// </summary>
        public void EquipWeapon(WeaponController weapon)
        {
            if (activeWeapon) UnequipWeapon();
            activeWeapon = weapon;
            if (!activeWeapon) return;

            var cam = look ? look.View : Camera.main;
            if (weaponParent) activeWeapon.transform.SetParent(weaponParent, false);
            var view = activeWeapon.GetComponent<WeaponViewPresenter>();
            if (view) view.Configure(activeWeapon, input,
                activeWeapon.transform.Find("ViewmodelRoot"), GetComponent<PlayerStance>(), motor);
            var recoil = activeWeapon.GetComponent<WeaponRecoilController>();
            if (recoil) recoil.Configure(activeWeapon, look);
            activeWeapon.Initialize(cam ? cam.transform : transform, gameObject);
            activeWeapon.ShotFired += OnWeaponShotFired;
            activeWeapon.RequestEquip();
            WeaponChanged?.Invoke();
        }

        Noise.GameplayNoiseSystem noise;
        Noise.GameplayNoiseTuning noiseTuning;
        ulong noiseSource, lastShot;
        public void BindNoise(Noise.GameplayNoiseSystem authority, Noise.GameplayNoiseTuning tuning, ulong source)
        { noise = authority; noiseTuning = tuning; noiseSource = source; lastShot = 0; if (melee) melee.BindNoise(authority, tuning, source); }
        void OnWeaponShotFired(WeaponFireResolver.ShotResult shot)
        {
            if (noise == null || shot.ShotId <= lastShot || !activeWeapon) return;
            lastShot = shot.ShotId;
            noise.TryEmit(new Noise.GameplayNoiseRequest(noiseSource, shot.SourcePosition,
                Noise.GameplayNoiseCategory.Gunshot, noiseTuning.Gunshot, shot.ShotId), out _);
        }

        public void UnequipWeapon()
        {
            if (!activeWeapon) return;
            activeWeapon.ShotFired -= OnWeaponShotFired;
            activeWeapon.RequestUnequip();
            activeWeapon.gameObject.SetActive(false);
            Destroy(activeWeapon.gameObject);
            activeWeapon = null;
            WeaponChanged?.Invoke();
            if (look) look.SetFOVOverride(-1);
            if (look) look.SetSensitivityMultiplier(1);
        }

        bool vehicleHolstered;
        public void SetVehicleHolstered(bool value)
        {
            vehicleHolstered = value; CancelGameplayActions(false);
            if (melee) melee.gameObject.SetActive(!value && SelectedSlot == CombatSlot.Melee);
            if (activeWeapon)
            {
                activeWeapon.gameObject.SetActive(!value && SelectedSlot == CombatSlot.Firearm);
                if (!value && SelectedSlot == CombatSlot.Firearm) activeWeapon.RequestEquip();
            }
            if (look) { look.SetFOVOverride(-1); look.SetSensitivityMultiplier(1); }
        }
        public void CancelGameplayActions(bool terminal)
        {
#if UNITY_EDITOR
            evidenceAimHeld = false;
#endif
            if (melee) { melee.Cancel(); if (terminal) melee.gameObject.SetActive(false); }
            if (!activeWeapon) return;
            activeWeapon.OnFireReleased();
            if (terminal) UnequipWeapon();
        }

        void Update()
        {
            if (vehicleHolstered) return;
            if (!input || !input.GameplayActive) { if (melee) melee.Cancel(); }
            if (SelectedSlot != CombatSlot.Firearm || !activeWeapon || activeWeapon.RuntimeState == null) return;
            // Update aim amount (smooth interpolation).
            var state = activeWeapon.RuntimeState;
            float adsSpeed = activeWeapon.Definition.AdsTransitionSeconds;
            bool aimHeld = input.AimHeld;
#if UNITY_EDITOR
            aimHeld |= evidenceAimHeld;
#endif
            float targetAim = aimHeld && input.GameplayActive &&
                !(motor && motor.IsSliding) ? 1f : 0f;
            if (adsSpeed > 0)
                state.AimAmount = Mathf.MoveTowards(state.AimAmount, targetAim, Time.deltaTime / adsSpeed);
            else
                state.AimAmount = targetAim;
            if (look) look.SetFOVOverride(Mathf.Lerp(look.BaseFOV, activeWeapon.Definition.AdsFovDegrees, state.AimAmount));
            if (look) look.SetSensitivityMultiplier(Mathf.Lerp(1, activeWeapon.Definition.AdsSensitivityMultiplier, state.AimAmount));
        }

        void SelectMelee() => SelectSlot(CombatSlot.Melee);
        void SelectFirearm() => SelectSlot(CombatSlot.Firearm);
        public bool SelectSlot(CombatSlot slot)
        {
            if (slot != CombatSlot.Firearm && slot != CombatSlot.Melee) return false;
            if (slot == CombatSlot.Melee && !melee) return false;
            if (slot == SelectedSlot) return true;
            GetComponent<PlayerSurvival>()?.CancelTreatment("Ekipman değişti; bandaj tüketilmedi.");
#if UNITY_EDITOR
            evidenceAimHeld = false;
#endif
            if (melee) { melee.Cancel(); melee.gameObject.SetActive(false); }
            if (activeWeapon) { activeWeapon.OnFireReleased(); activeWeapon.gameObject.SetActive(false); }
            SelectedSlot = slot;
            if (slot == CombatSlot.Melee) melee.gameObject.SetActive(true);
            else if (activeWeapon) { activeWeapon.gameObject.SetActive(true); activeWeapon.RequestEquip(); }
            if (look) { look.SetFOVOverride(-1); look.SetSensitivityMultiplier(1); }
            WeaponChanged?.Invoke();
            return true;
        }
        void OnFirePressed() { if (SelectedSlot == CombatSlot.Melee) { if (melee) melee.TryAttack(); } else if (activeWeapon) activeWeapon.OnFirePressed(); }
        void OnFireReleased() { if (activeWeapon) activeWeapon.OnFireReleased(); }
        void OnReload() { if (SelectedSlot == CombatSlot.Firearm && activeWeapon) activeWeapon.OnReloadRequested(); }
    }
}
