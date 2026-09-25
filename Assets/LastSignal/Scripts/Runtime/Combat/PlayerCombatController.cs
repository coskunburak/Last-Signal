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

        WeaponController activeWeapon;
        bool aimInputHeld;

        public WeaponController ActiveWeapon => activeWeapon;
        public event System.Action WeaponChanged;

        void Start()
        {
            if (!activeWeapon && startingWeapon)
                EquipWeapon(Instantiate(startingWeapon, weaponParent));
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
            input.AimPressed += OnAimPressed;
            input.AimReleased += OnAimReleased;
            input.ReloadRequested += OnReload;
        }

        void OnDisable()
        {
            if (!input) return;
            input.FirePressed -= OnFirePressed;
            input.FireReleased -= OnFireReleased;
            input.AimPressed -= OnAimPressed;
            input.AimReleased -= OnAimReleased;
            input.ReloadRequested -= OnReload;
            aimInputHeld = false;
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
            if (view) view.Configure(activeWeapon, input, activeWeapon.transform.Find("ViewmodelRoot"));
            var recoil = activeWeapon.GetComponent<WeaponRecoilController>();
            if (recoil) recoil.Configure(activeWeapon, look);
            activeWeapon.Initialize(cam ? cam.transform : transform, gameObject);
            activeWeapon.ShotFired += OnWeaponShotFired;
            activeWeapon.RequestEquip();
            WeaponChanged?.Invoke();
        }

        void OnWeaponShotFired(WeaponFireResolver.ShotResult obj)
        {
            var pop = Object.FindAnyObjectByType<LastSignal.AI.WorldPopulationManager>();
            if (pop)
            {
                string cellId = LastSignal.WorldCells.CellCoordinate.FromWorld(transform.position).Id;
                pop.ReportNoise(System.Guid.NewGuid().ToString(), cellId, 1.0f);
            }
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

        public void CancelGameplayActions(bool terminal)
        {
            aimInputHeld = false;
            if (!activeWeapon) return;
            activeWeapon.OnFireReleased();
            if (terminal) UnequipWeapon();
        }

        void Update()
        {
            if (!activeWeapon || activeWeapon.RuntimeState == null) return;
            // Update aim amount (smooth interpolation).
            var state = activeWeapon.RuntimeState;
            float adsSpeed = activeWeapon.Definition.AdsTransitionSeconds;
            float targetAim = aimInputHeld && input.GameplayActive ? 1f : 0f;
            if (adsSpeed > 0)
                state.AimAmount = Mathf.MoveTowards(state.AimAmount, targetAim, Time.deltaTime / adsSpeed);
            else
                state.AimAmount = targetAim;
            if (look) look.SetFOVOverride(Mathf.Lerp(look.BaseFOV, activeWeapon.Definition.AdsFovDegrees, state.AimAmount));
            if (look) look.SetSensitivityMultiplier(Mathf.Lerp(1, activeWeapon.Definition.AdsSensitivityMultiplier, state.AimAmount));
        }

        void OnFirePressed() { if (activeWeapon) activeWeapon.OnFirePressed(); }
        void OnFireReleased() { if (activeWeapon) activeWeapon.OnFireReleased(); }
        void OnAimPressed() => aimInputHeld = true;
        void OnAimReleased() => aimInputHeld = false;
        void OnReload() { if (activeWeapon) activeWeapon.OnReloadRequested(); }
    }
}
