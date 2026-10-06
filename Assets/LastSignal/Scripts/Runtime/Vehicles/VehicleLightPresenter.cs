using UnityEngine;

namespace LastSignal.Vehicles
{
    [DisallowMultipleComponent, RequireComponent(typeof(VehicleActor))]
    public sealed class VehicleLightPresenter : MonoBehaviour
    {
        [SerializeField] Light[] brakeLights = new Light[0], reverseLights = new Light[0];
        [SerializeField] Renderer[] brakeLenses = new Renderer[0], reverseLenses = new Renderer[0];
        IVehiclePresentationState physics;
        VehicleActor actor;
        void Awake() { physics = GetComponent<IVehiclePresentationState>(); actor = GetComponent<VehicleActor>(); }
        void LateUpdate()
        {
            bool active = actor.Ready && actor.Occupied && physics != null;
            Set(brakeLights, brakeLenses, active && physics.BrakeApplied);
            Set(reverseLights, reverseLenses, active && actor.Resources.EngineRunning && physics.ReverseEngaged);
        }
        void OnDisable() { Set(brakeLights, brakeLenses, false); Set(reverseLights, reverseLenses, false); }
        static void Set(Light[] lights, Renderer[] lenses, bool value)
        {
            foreach (var light in lights) if (light) light.enabled = value;
            foreach (var lens in lenses) if (lens) lens.enabled = value;
        }
    }
}
