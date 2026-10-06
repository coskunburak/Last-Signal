using LastSignal.Inventory;
using UnityEngine;

namespace LastSignal.Vehicles
{
    /// <summary>Uses the existing interaction ray. The view delegates all transfers to vehicle inventory authority.</summary>
    public sealed class VehicleServicePoint : MonoBehaviour, IInteractable
    {
        public enum Service { Fuel, Cargo }
        [SerializeField] Service service;
        VehicleActor actor;
        bool open;
        Vector2 scroll;
        void Awake() => actor = GetComponentInParent<VehicleActor>();
        public string Prompt => service == Service.Fuel ? "E — Refuel pickup (one fuel can)" : "E — Open pickup cargo";
        public bool Available => actor && actor.CanService();
        public bool TryInteract()
        {
            if (!Available || (actor.Owner.Player.transform.position - transform.position).sqrMagnitude > 9) return false;
            if (service == Service.Fuel) return actor.TryRefuel();
            open = true; actor.Owner.Pause(); actor.Present("trunk"); return true;
        }
        void Update()
        {
            if (open && (!actor || actor.Owner.InMenu || !actor.Owner.Paused || actor.Owner.PlayerDead || actor.Occupied)) open = false;
        }
        void OnGUI()
        {
            if (!open || !actor || !actor.Owner.Player) return;
            var inventory = actor.Owner.Player.GetComponent<PlayerInventory>();
            GUILayout.BeginArea(new Rect(30, 50, 520, 500), GUI.skin.box);
            GUILayout.Label("Pickup cargo — select a stack to transfer");
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Carried inventory");
            for (int i = 0; i < inventory.Capacity; i++)
            {
                var slot = inventory.GetSlot(i);
                if (!slot.IsEmpty && GUILayout.Button(slot.Item.DisplayName + " × " + slot.Quantity + " → cargo"))
                    actor.TransferCargo(inventory, slot.Item, slot.Quantity, true);
            }
            GUILayout.Label("Pickup cargo");
            for (int i = 0; i < actor.Resources.CargoCapacity; i++)
            {
                var slot = actor.Resources.GetCargoSlot(i);
                if (!slot.IsEmpty && GUILayout.Button(slot.Item.DisplayName + " × " + slot.Quantity + " → inventory"))
                    actor.TransferCargo(inventory, slot.Item, slot.Quantity, false);
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close")) { open = false; actor.Owner.Resume(); }
            GUILayout.EndArea();
        }
    }
}
