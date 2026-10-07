using LastSignal.Inventory;
using Unity.Profiling;
using UnityEngine;

namespace LastSignal.Vehicles
{
    /// <summary>Last Signal gameplay composition. Physics is consumed through the port;
    /// inventory, noise and health keep their existing authorities.</summary>
    [DisallowMultipleComponent]
    public sealed partial class VehicleActor : MonoBehaviour, IInteractable
    {
        static readonly ProfilerMarker UpdateMarker = new ProfilerMarker("LastSignal.Vehicle.Update");
        static readonly ProfilerMarker NoiseMarker = new ProfilerMarker("LastSignal.Vehicle.Noise");
        [SerializeField] VehicleDefinition definition;
        [SerializeField] Transform seat, cameraAnchor;
        [SerializeField] Transform[] doorways, exits;
        [SerializeField] Collider cabinRoof;
        [SerializeField] LayerMask solidMask = ~((1 << 8) | (1 << 2));
        IVehiclePhysicsPort physics;
        Rigidbody body;
        SessionFlow flow;
        VehicleNoiseProducer noise;
        VehicleTuning tuning;
        readonly VehicleSeatAuthority ownership = new VehicleSeatAuthority();
        readonly VehicleImpactGate impacts = new VehicleImpactGate();
        ulong impactSequence;
        GameObject driver;
        PlayerInputReader input;
        CharacterController capsule;
        FirstPersonMotor motor;
        PlayerStance stance;
        Transform oldParent;
        Vector3 oldCameraPosition;
        float oldNearClip;
        Vector3[] doorwayPoints, exitPoints;
        public SessionFlow Owner => flow;
        public void Present(string cue) => Presented?.Invoke(cue);
        public VehicleResources Resources { get; private set; }
        public VehicleDefinition Definition => definition;
        public bool Occupied => ownership.State == VehicleSeatState.Occupied;
        public bool RainCovered => Occupied && cabinRoof && cabinRoof.enabled && cabinRoof.Raycast(new Ray(cameraAnchor.position, Vector3.up), out _, 3);
        public bool LightsOn { get; private set; }
        public bool Ready => flow && !flow.InMenu && !flow.Paused && !flow.Restoring && !flow.PlayerDead;
        public bool Available => Ready && !Occupied && physics != null && physics.SpeedMetersPerSecond <= tuning.exitSpeedMetersPerSecond;
        public string Prompt => "E — Enter pickup";
        public event System.Action<string> Presented;
        public void Bind(SessionFlow owner)
        {
            flow = owner; physics = GetComponent<IVehiclePhysicsPort>(); body = GetComponent<Rigidbody>();
            if (!definition || !definition.Valid || physics == null || !seat || !cameraAnchor || doorways == null || exits == null || doorways.Length != exits.Length || exits.Length == 0)
                throw new System.InvalidOperationException("Incomplete vehicle authoring.");
            tuning = definition.CreateTuning();
            Resources = new VehicleResources(definition.StableId, tuning, definition.FuelItem, 8, 1);
            noise = new VehicleNoiseProducer(flow.Noise, 1001, tuning);
            doorwayPoints = new Vector3[doorways.Length]; exitPoints = new Vector3[exits.Length];
            physics.ResetTransientInput();
        }
        public bool TryInteract() => TryEnter();
        public bool TryEnter()
        {
            if (!Available || !flow.Player || (flow.Player.transform.position - seat.position).sqrMagnitude > 16) return false;
            return AttachDriver(false);
        }
        bool AttachDriver(bool restoring)
        {
            if (!flow || !flow.Player || (!restoring && !Ready) || !ownership.BeginEnter("player.local", true)) return false;
            var player = flow.Player; var reader = player.GetComponent<PlayerInputReader>();
            if (!reader || !reader.SetDrivingContext(true, !restoring)) { ownership.Rollback(); return false; }
            driver = player; input = reader; capsule = player.GetComponent<CharacterController>();
            motor = player.GetComponent<FirstPersonMotor>(); stance = player.GetComponent<PlayerStance>();
            var combat = player.GetComponent<PlayerCombatController>();
            combat?.SetVehicleHolstered(true);
            motor.enabled = false; stance.enabled = false; capsule.enabled = false;
            player.GetComponent<FirstPersonLook>().ResetVehicleBodyFeel();
            var view = player.GetComponent<FirstPersonLook>().View.transform;
            oldParent = player.transform.parent; oldCameraPosition = view.localPosition;
            var camera = player.GetComponent<FirstPersonLook>().View;
            oldNearClip = camera.nearClipPlane; camera.nearClipPlane = .05f;
            player.transform.SetParent(seat, false); player.transform.localPosition = Vector3.zero; player.transform.localRotation = Quaternion.identity;
            view.position = cameraAnchor.position; player.GetComponent<FirstPersonLook>().RestorePitch(0);
            ownership.Commit(); physics.ResetTransientInput(); Presented?.Invoke("door"); return true;
        }
        public bool TryExit()
        {
            if (!Ready || !Occupied || !driver) return false;
            for (int i = 0; i < exits.Length; i++) { doorwayPoints[i] = doorways[i].position; exitPoints[i] = exits[i].position; }
            // Validate standing clearance even if the driver entered crouched.
            bool safe = VehicleExitSolver.TryFind(doorwayPoints, exitPoints, Mathf.Max(1.8f, capsule.height), capsule.radius,
                capsule.slopeLimit, solidMask, ResidentReady, out var feet);
            if (!ownership.BeginExit("player.local", physics.SpeedMetersPerSecond, tuning.exitSpeedMetersPerSecond, safe)) return false;
            DetachDriver(feet); ownership.Commit(); Presented?.Invoke("door"); return true;
        }
        bool ResidentReady(Vector3 point)
        {
            var cells = flow.GetComponent<WorldCells.WorldCellManager>();
            return Ready && (!cells || (cells.Stable && cells.CurrentCell == "resident"));
        }
        void DetachDriver(Vector3 feet)
        {
            physics.ResetTransientInput();
            driver.transform.SetParent(oldParent, true); driver.transform.position = feet;
            driver.transform.rotation = Quaternion.Euler(0, driver.transform.eulerAngles.y, 0);
            driver.GetComponent<FirstPersonLook>().ResetVehicleBodyFeel();
            driver.GetComponent<FirstPersonLook>().View.transform.localPosition = oldCameraPosition;
            driver.GetComponent<FirstPersonLook>().View.nearClipPlane = oldNearClip;
            capsule.enabled = true; motor.enabled = true; stance.enabled = true;
            var combat = driver.GetComponent<PlayerCombatController>();
            if (combat) combat.SetVehicleHolstered(false);
            input.SetDrivingContext(false, Ready);
            driver = null; input = null;
        }
        public void Suspend()
        { physics?.ResetTransientInput(); noise?.Reset(); ClearBodyFeel(); }
        public bool TryRefuel()
        {
            if (!CanService() || !Resources.TryRefuel(flow.Player.GetComponent<PlayerInventory>())) return false;
            Presented?.Invoke("refuel"); return true;
        }
        public bool CanService() => Ready && !Occupied && physics.SpeedMetersPerSecond <= tuning.exitSpeedMetersPerSecond &&
            (flow.Player.transform.position - transform.position).sqrMagnitude <= 25;
        public LastSignal.Inventory.TransferResult TransferCargo(PlayerInventory inventory, LastSignal.Inventory.Data.ItemDefinition item, int count, bool deposit)
        {
            if (!flow || flow.Restoring || flow.PlayerDead || !flow.Player || !flow.Paused || Occupied ||
                inventory != flow.Player.GetComponent<PlayerInventory>() || physics.SpeedMetersPerSecond > tuning.exitSpeedMetersPerSecond ||
                (flow.Player.transform.position - transform.position).sqrMagnitude > 25)
                return new LastSignal.Inventory.TransferResult(count, 0, LastSignal.Inventory.TransferReason.Busy);
            return deposit ? Resources.Deposit(inventory, item, count) : Resources.Withdraw(inventory, item, count);
        }
        public bool TryIgnition()
        {
            if (!Ready || !Occupied) return false;
            if (Resources.EngineRunning) { Resources.StopEngine(); physics.ResetTransientInput(); Presented?.Invoke("stop"); return true; }
            Presented?.Invoke("starter");
            if (!Resources.TryStartEngine(true, true)) return false;
            Presented?.Invoke("start"); return true;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        VehicleControlIntent? developmentControl;
        public void SetDevelopmentControl(VehicleControlIntent? value) => developmentControl = value;
#endif
        void Update()
        {
            using (UpdateMarker.Auto())
            {
                if (Resources == null) return;
                if (!Ready) { Suspend(); return; }
                var intent = input ? input.VehicleIntent : VehicleControlIntent.Parked;
                bool controlsActive = input && input.DrivingActive;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (developmentControl.HasValue) { intent = developmentControl.Value; controlsActive = true; }
#endif
                float load = Occupied ? Mathf.Max(intent.Throttle, intent.Reverse) : 0;
                Resources.Advance(Time.deltaTime, load);
                using (NoiseMarker.Auto()) noise.Advance(Time.deltaTime, Resources.EngineRunning, load, transform.position);
                if (!Occupied || !input || !controlsActive) { physics.ResetTransientInput(); return; }
                if (input.VehicleExitPressed && TryExit()) return;
                if (input.VehicleIgnitionPressed) TryIgnition();
                if (input.VehicleHornPressed && noise.Horn(transform.position)) Presented?.Invoke("horn");
                if (input.VehicleLightsPressed) LightsOn = !LightsOn;
                physics.SetControl(intent, Resources.EngineRunning);
            }
        }
        void OnDisable() {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            developmentControl = null;
#endif
            Suspend(); ClearImpactContacts(); VehicleInfectedContactResponse.Unregister(responseColliders, true); }
        void OnDestroy()
        {
            // Session teardown destroys the player separately; detach before destroying this parent.
            if (driver) driver.transform.SetParent(oldParent, true);
        }
    }
}
