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

        void Awake() => actor = GetComponentInParent<VehicleActor>();
        public string Prompt => service == Service.Fuel ? "Refuel pickup (one fuel can)" : "Open pickup cargo";
        public bool Available => actor && actor.CanService();
        public bool TryInteract()
        {
            if (!Available || (actor.Owner.Player.transform.position - transform.position).sqrMagnitude > 9) return false;
            if (service == Service.Fuel) return actor.TryRefuel();
            open = true; actor.Owner.OpenCargo(this); actor.Present("trunk"); return true;
        }
        void Update()
        {
            if (open && (!actor || actor.Owner.InMenu || !actor.Owner.Paused || actor.Owner.PlayerDead || actor.Occupied)) Close();
        }
        public bool IsOpen => open;
        public Service Kind => service;
        public VehicleActor Actor => actor;
        public void Close()
        {
            open = false;
            if (actor && actor.Owner) actor.Owner.CloseCargo(this);
        }
        void OnDisable() { if (open) Close(); }
    }
}
