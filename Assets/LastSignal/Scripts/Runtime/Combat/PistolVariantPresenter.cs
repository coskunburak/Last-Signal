using UnityEngine;
namespace LastSignal
{
    /// <summary>Three cosmetic forms of the same S021 9 mm instance. Never initializes or copies ammo.</summary>
    [DefaultExecutionOrder(360), DisallowMultipleComponent]
    public sealed class PistolVariantPresenter : MonoBehaviour
    {
        public PistolVisualVariant[] Variants;
        public string InitialId = PistolVisualVariant.DxtId;
        public PistolVisualVariant Selected { get; private set; }
        public string SelectedId => Selected ? Selected.Id : InitialId;
        WeaponController weapon;
        FirstPersonWeaponGrip grip;
        WeaponViewPresenter view;
        WeaponVfxPresenter vfx;
        CharacterBodyVisibility visibility;
        PlayerCombatController combat;
        PlayerHealth health;
        string queuedVariant, queuedFirearm;
        void OnDisable() { queuedVariant = queuedFirearm = null; }
        void Update()
        {
            if (queuedVariant == null && queuedFirearm == null) return;
            if (health && !health.IsAlive) { queuedVariant = queuedFirearm = null; return; }
            if (!weapon || !weapon.IsSimulationActive || weapon.RuntimeState == null) return;
            var state = weapon.RuntimeState.State;
            if (state == WeaponState.Ready || state == WeaponState.Reloading) weapon.RequestUnequip();
            if (weapon.RuntimeState.State != WeaponState.Holstered) return;
            string variant = queuedVariant, firearm = queuedFirearm;
            queuedVariant = queuedFirearm = null;
            if (variant != null) { if (Select(variant)) weapon.RequestEquip(); }
            else if (combat) combat.SelectFirearm(firearm);
        }
        public bool QueueExit(string firearmId)
        {
            if (!combat || !combat.FindFirearm(firearmId)) return false;
            queuedVariant = null; queuedFirearm = firearmId;
            weapon.OnFireReleased(); weapon.RequestUnequip(); return true;
        }
        public bool ValidCatalog
        {
            get
            {
                if (Variants == null || Variants.Length != 3) return false;
                for (int i = 0; i < Variants.Length; i++)
                {
                    if (!Variants[i] || !Variants[i].Valid) return false;
                    for (int j = 0; j < i; j++) if (Variants[i].Id == Variants[j].Id) return false;
                }
                return true;
            }
        }
        void Awake()
        {
            weapon = GetComponent<WeaponController>(); grip = GetComponent<FirstPersonWeaponGrip>();
            view = GetComponent<WeaponViewPresenter>(); vfx = GetComponent<WeaponVfxPresenter>();
            combat = GetComponentInParent<PlayerCombatController>(); health = GetComponentInParent<PlayerHealth>();
            visibility = combat ? combat.GetComponentInChildren<CharacterBodyVisibility>(true) : null;
            if (!ValidCatalog || !weapon || !grip) { Debug.LogError("Invalid triple pistol catalog or rig.", this); enabled = false; return; }
            // The default visual is persistently authored; bind it without spawning a duplicate.
            foreach (var variant in Variants) if (variant.Id == InitialId)
            { Selected = variant; Bind(grip.Visual, grip.Sockets); return; }
            Debug.LogError("Unknown initial pistol variant.", this); enabled = false;
        }
        public bool Select(string id)
        {
            if (string.IsNullOrEmpty(id)) id = PistolVisualVariant.DxtId;
            if (!ValidCatalog || !weapon || !grip) return false;
            PistolVisualVariant next = null;
            foreach (var variant in Variants) if (variant.Id == id) { next = variant; break; }
            if (!next) return false;
            queuedVariant = queuedFirearm = null;
            if (Selected == next) return true;
            // Stop authoritative action first: cancellation retains any already committed rounds.
            weapon.RequestPresentationChange();
            var old = grip.Visual;
            var visual = Instantiate(next.VisualPrefab, grip.PoseFrame);
            SetLayer(visual.transform, 29);
            var sockets = visual.GetComponent<CharacterWeaponSockets>();
            CharacterWeaponSockets.Attach(visual.transform, sockets.PrimaryGrip, grip.PoseFrame.position,
                grip.PoseFrame.rotation * Quaternion.Euler(0, 0, 90));
            Selected = next;
            Bind(visual.transform, sockets); // Reparent the existing muzzle FX before removing the old visual.
            if (old) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
            if (visibility) visibility.RefreshRenderers();
            if (combat) combat.NotifyPresentationChanged();
            return true;
        }
        void Bind(Transform visual, CharacterWeaponSockets sockets)
        {
            grip.BindVisual(Selected.Pose, visual, sockets);
            weapon.BindPistolPresentation(this, Selected.Pose, sockets);
            if (view) view.SetGripProfile(grip.Profile);
            var mechanics = visual.GetComponent<PistolMechanics>();
            if (mechanics) mechanics.Bind(weapon);
            if (vfx) vfx.BindPistolSockets(sockets, mechanics ? mechanics.EjectionPort : null);
        }
        public bool SelectNext()
        {
            if (!ValidCatalog) return false;
            for (int i = 0; i < Variants.Length - 1; i++)
                if (Variants[i].Id == (queuedVariant ?? SelectedId))
                {
                    queuedVariant = Variants[i + 1].Id; queuedFirearm = null;
                    weapon.OnFireReleased(); weapon.RequestUnequip(); return true;
                }
            return false;
        }
        public void SelectFirst() { if (ValidCatalog) Select(Variants[0].Id); }
        static void SetLayer(Transform root, int layer)
        { root.gameObject.layer = layer; foreach (Transform child in root) SetLayer(child, layer); }
    }
}
