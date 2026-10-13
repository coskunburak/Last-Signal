using System.Collections.Generic;
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
        [SerializeField] WeaponController secondaryWeapon;

        public enum CombatSlot { Firearm, Melee }
        [SerializeField] MeleeWeaponController startingMelee;
        [SerializeField] MeleeWeaponController fastMelee;
        [SerializeField] MeleeWeaponController heavyMelee;
        readonly List<WeaponController> firearms = new List<WeaponController>(2);
        readonly List<MeleeWeaponController> meleeWeapons = new List<MeleeWeaponController>(3);
        MeleeWeaponController melee;
        public CombatSlot SelectedSlot { get; private set; }
        public MeleeWeaponController Melee => melee;
        public WeaponController Firearm => activeWeapon;
        public WeaponController Rifle => FindFirearm("weapon.rifle");
        public IReadOnlyList<WeaponController> Firearms => firearms;
        public IReadOnlyList<MeleeWeaponController> MeleeWeapons => meleeWeapons;
        WeaponController activeWeapon;
#if UNITY_EDITOR
        bool evidenceAimHeld;
        public void SetEvidenceAim(bool held) => evidenceAimHeld = held;
#endif
        FirstPersonMotor motor;

        void Awake() => motor = GetComponent<FirstPersonMotor>();

        public WeaponController ActiveWeapon => SelectedSlot == CombatSlot.Firearm ? activeWeapon : null;
        public event System.Action WeaponChanged;
        public void NotifyPresentationChanged() => WeaponChanged?.Invoke();

        void Start() => EnsureStartingEquipment();

        internal void EnsureStartingEquipment()
        {
            if (!activeWeapon && startingWeapon)
                EquipWeapon(Instantiate(startingWeapon, weaponParent));
            if (secondaryWeapon && !FindFirearm(secondaryWeapon.Definition.WeaponId))
            {
                var extra = Instantiate(secondaryWeapon, weaponParent);
                ConfigureFirearm(extra);
                extra.gameObject.SetActive(false);
                firearms.Add(extra);
            }
            AddMelee(startingMelee);
            AddMelee(fastMelee);
            AddMelee(heavyMelee);
            if (!melee && meleeWeapons.Count > 0) melee = meleeWeapons[0];
        }

        void AddMelee(MeleeWeaponController prefab)
        {
            if (!prefab || FindMelee(prefab.Definition.WeaponId)) return;
            var instance = Instantiate(prefab, weaponParent);
            instance.Initialize(gameObject, look ? look.View.transform : transform);
            var stance = instance.GetComponent<MeleeStanceViewPresenter>();
            if (stance) stance.Configure(GetComponent<PlayerStance>(), motor);
            if (noise != null) instance.BindNoise(noise, noiseTuning, noiseSource);
            instance.gameObject.SetActive(false);
            meleeWeapons.Add(instance);
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
            if (input)
            {
                input.FirePressed -= OnFirePressed;
                input.FireReleased -= OnFireReleased;
                input.ReloadRequested -= OnReload;
                input.MeleeSlotRequested -= SelectMelee;
                input.FirearmSlotRequested -= SelectFirearm;
            }
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

            ConfigureFirearm(activeWeapon);
            if (!firearms.Contains(activeWeapon)) firearms.Add(activeWeapon);
            activeWeapon.RequestEquip();
            WeaponChanged?.Invoke();
        }

        void ConfigureFirearm(WeaponController weapon)
        {

            var cam = look ? look.View : Camera.main;
            if (weaponParent) weapon.transform.SetParent(weaponParent, false);
            var view = weapon.GetComponent<WeaponViewPresenter>();
            if (view) view.Configure(weapon, input,
                weapon.transform.Find("ViewmodelRoot"), GetComponent<PlayerStance>(), motor);
            var recoil = weapon.GetComponent<WeaponRecoilController>();
            if (recoil) recoil.Configure(weapon, look);
            weapon.Initialize(cam ? cam.transform : transform, gameObject);
            weapon.ShotFired -= OnWeaponShotFired;
            weapon.ShotFired += OnWeaponShotFired;
        }

        public WeaponController FindFirearm(string id)
        {
            foreach (var weapon in firearms)
                if (weapon && weapon.Definition && weapon.Definition.WeaponId == id) return weapon;
            return null;
        }
        public MeleeWeaponController FindMelee(string id)
        {
            foreach (var weapon in meleeWeapons)
                if (weapon && weapon.Definition && weapon.Definition.WeaponId == id) return weapon;
            return null;
        }
        public bool SelectFirearm(string id)
        {
            var next = FindFirearm(id);
            if (!next) return false;
            if (activeWeapon == next) return SelectSlot(CombatSlot.Firearm);
            GetComponent<PlayerSurvival>()?.CancelTreatment("Ekipman değişti; bandaj tüketilmedi.");
            if (activeWeapon) { activeWeapon.OnFireReleased(); activeWeapon.RequestUnequip(); activeWeapon.gameObject.SetActive(false); }
            if (melee) { melee.Cancel(); melee.gameObject.SetActive(false); }
            activeWeapon = next;
            SelectedSlot = CombatSlot.Firearm;
            if (!vehicleHolstered) { next.gameObject.SetActive(true); next.RequestEquip(); }
            if (look) { look.SetFOVOverride(-1); look.SetSensitivityMultiplier(1); }
            WeaponChanged?.Invoke();
            return true;
        }
        public bool SelectMeleeWeapon(string id)
        {
            var next = FindMelee(id);
            if (!next) return false;
            if (melee == next) return SelectSlot(CombatSlot.Melee);
            if (melee) { melee.Cancel(); melee.gameObject.SetActive(false); }
            if (activeWeapon) { activeWeapon.OnFireReleased(); activeWeapon.RequestUnequip(); activeWeapon.gameObject.SetActive(false); }
            melee = next;
            SelectedSlot = CombatSlot.Melee;
            if (!vehicleHolstered) melee.gameObject.SetActive(true);
            if (look) { look.SetFOVOverride(-1); look.SetSensitivityMultiplier(1); }
            WeaponChanged?.Invoke();
            return true;
        }

        Noise.GameplayNoiseSystem noise;
        Noise.GameplayNoiseTuning noiseTuning;
        ulong noiseSource, lastShot;
        public void BindNoise(Noise.GameplayNoiseSystem authority, Noise.GameplayNoiseTuning tuning, ulong source)
        { noise = authority; noiseTuning = tuning; noiseSource = source; lastShot = 0;
            foreach (var weapon in meleeWeapons) if (weapon) weapon.BindNoise(authority, tuning, source); }
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
            firearms.Remove(activeWeapon);
            activeWeapon = null;
            WeaponChanged?.Invoke();
            if (look) look.SetFOVOverride(-1);
            if (look) look.SetSensitivityMultiplier(1);
        }

        bool vehicleHolstered;
        public void SetVehicleHolstered(bool value)
        {
            vehicleHolstered = value; CancelGameplayActions(false);
            foreach (var weapon in meleeWeapons) if (weapon) weapon.gameObject.SetActive(!value && SelectedSlot == CombatSlot.Melee && weapon == melee);
            foreach (var weapon in firearms)
            {
                if (!weapon) continue;
                weapon.gameObject.SetActive(!value && SelectedSlot == CombatSlot.Firearm && weapon == activeWeapon);
                if (!value && SelectedSlot == CombatSlot.Firearm && weapon == activeWeapon) weapon.RequestEquip();
            }
            if (look) { look.SetFOVOverride(-1); look.SetSensitivityMultiplier(1); }
        }
        public void CancelGameplayActions(bool terminal)
        {
#if UNITY_EDITOR
            evidenceAimHeld = false;
#endif
            foreach (var weapon in meleeWeapons) if (weapon) { weapon.Cancel(); if (terminal) weapon.gameObject.SetActive(false); }
            foreach (var weapon in firearms) if (weapon) { weapon.OnFireReleased(); if (terminal) weapon.gameObject.SetActive(false); }
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

        void SelectMelee()
        {
            if (SelectedSlot == CombatSlot.Melee && meleeWeapons.Count > 1)
            {
                int next = (meleeWeapons.IndexOf(melee) + 1) % meleeWeapons.Count;
                SelectMeleeWeapon(meleeWeapons[next].Definition.WeaponId);
            }
            else SelectSlot(CombatSlot.Melee);
        }
        void SelectFirearm()
        {
            if (SelectedSlot == CombatSlot.Firearm && firearms.Count > 1)
            {
                if (activeWeapon && activeWeapon.PistolVariants && activeWeapon.PistolVariants.SelectNext())
                { WeaponChanged?.Invoke(); return; }
                int next = (firearms.IndexOf(activeWeapon) + 1) % firearms.Count;
                var target = firearms[next];
                if (activeWeapon && activeWeapon.PistolVariants && activeWeapon.PistolVariants.QueueExit(target.Definition.WeaponId)) return;
                if (target.PistolVariants) target.PistolVariants.SelectFirst();
                SelectFirearm(target.Definition.WeaponId);
            }
            else SelectSlot(CombatSlot.Firearm);
        }
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
            if (activeWeapon) { activeWeapon.OnFireReleased(); activeWeapon.RequestUnequip(); activeWeapon.gameObject.SetActive(false); }
            SelectedSlot = slot;
            if (!vehicleHolstered && slot == CombatSlot.Melee) melee.gameObject.SetActive(true);
            else if (!vehicleHolstered && activeWeapon) { activeWeapon.gameObject.SetActive(true); activeWeapon.RequestEquip(); }
            if (look) { look.SetFOVOverride(-1); look.SetSensitivityMultiplier(1); }
            WeaponChanged?.Invoke();
            return true;
        }
        void OnFirePressed() { if (SelectedSlot == CombatSlot.Melee) { if (melee) melee.TryAttack(); } else if (activeWeapon) activeWeapon.OnFirePressed(); }
        void OnFireReleased() { if (activeWeapon) activeWeapon.OnFireReleased(); }
        void OnReload() { if (SelectedSlot == CombatSlot.Firearm && activeWeapon) activeWeapon.OnReloadRequested(); }
    }
}
