using UnityEngine;

namespace LastSignal
{
    public enum SessionScreen { Gameplay, MainMenu, Pause, Inventory, Storage, Journal, Death, Loading, Settings, ShelterProduction, VehicleCargo }

    [DisallowMultipleComponent]
    public sealed class SessionFlow : MonoBehaviour
    {
        [SerializeField] GameObject playerPrefab;
        [SerializeField] bool enableSurvival;
        public bool SurvivalEnabled => enableSurvival;
        public SessionInput Controls { get; private set; }
        public SessionUi UserInterface { get; private set; }
        public bool SettingsOpen { get; private set; }
        public Vehicles.VehicleServicePoint CargoPoint { get; private set; }
        public bool ProductionOpen => GetComponent<Shelter.ShelterSite>()?.IsOpen ?? false;
        public bool CargoOpen => CargoPoint && CargoPoint.IsOpen;
        public void OpenCargo(Vehicles.VehicleServicePoint point) { CargoPoint = point; Pause(); }
        public void CloseCargo(Vehicles.VehicleServicePoint point) { if (CargoPoint != point) return; CargoPoint = null; Resume(); }
        void Awake()
        {
            Controls = gameObject.AddComponent<SessionInput>();
        }
        public void OpenSettings()
        {
            if (Screen != SessionScreen.MainMenu && Screen != SessionScreen.Pause) return;
            SettingsOpen = true;
        }
        public void CloseSettings()
        { Controls?.CancelRebind(); SettingsOpen = false; }
        public void OpenJournal() => GetComponent<Objectives.RelayMission>()?.ToggleJournal();
        [SerializeField] Noise.GameplayNoiseTuning noiseTuning = new Noise.GameplayNoiseTuning();
        public Noise.GameplayNoiseSystem Noise { get; private set; }
        public Noise.GameplayNoiseTuning NoiseTuning => noiseTuning;
        public double NoiseSimulationTime => NoiseTime();
        double NoiseTime() { var clock = GetComponent<WorldTime.WorldClock>(); return clock && clock.Simulation != null ? clock.Simulation.Seconds : Time.timeAsDouble; }
        bool NoiseAllowed() { var clock = GetComponent<WorldTime.WorldClock>(); return isActiveAndEnabled && Player && !Paused && !Restoring && !PlayerDead && Time.timeScale > 0; }
        [SerializeField] Transform spawnPoint;
        [SerializeField] DoorInteractable[] doors;
        [SerializeField] ZombieEncounter zombieEncounter;
        public void ConfigureEncounter(ZombieEncounter encounter) => zombieEncounter = encounter;
        public GameObject Player { get; private set; }
        public bool Paused { get; private set; }
        public long Generation { get; private set; }
        public bool Restoring { get; private set; }
        public bool InMenu => !Player;
        PlayerInputReader input;
        PlayerHealth health;
        LastSignal.Inventory.PlayerInventory inventory;
        LastSignal.Inventory.UI.InventoryUI inventoryView;
        public LastSignal.Inventory.UI.InventoryUI InventoryView => inventoryView;
        public bool InventoryOpen => inventoryView && inventoryView.IsOpen;
        public SessionScreen Screen => SettingsOpen ? SessionScreen.Settings : Restoring ? SessionScreen.Loading : InMenu ? SessionScreen.MainMenu :
            PlayerDead ? SessionScreen.Death : InventoryOpen ? SessionScreen.Inventory :
            JournalOpen ? SessionScreen.Journal : ProductionOpen ? SessionScreen.ShelterProduction :
            CargoOpen ? SessionScreen.VehicleCargo : PreparationOpen ? SessionScreen.Storage :
            Paused ? SessionScreen.Pause : SessionScreen.Gameplay;
        public bool SessionMenuVisible => Screen == SessionScreen.MainMenu || Screen == SessionScreen.Pause || Screen == SessionScreen.Death;
        LastSignal.Loot.LootPopulationService loot;
        Shelter.ShelterLoop shelter;
        public bool PreparationOpen => shelter && shelter.Preparing;
        public bool JournalOpen => GetComponent<Objectives.RelayMission>()?.JournalOpen ?? false;
        public bool PlayerDead => health && !health.IsAlive;
        public void Configure(GameObject prefab, Transform spawn, DoorInteractable[] sceneDoors)
        { playerPrefab = prefab; spawnPoint = spawn; doors = sceneDoors; }
        void Start()
        {
            if (playerPrefab)
            {
                Controls.Initialize(this, playerPrefab.GetComponent<PlayerInputReader>()?.SourceAsset);
                if (Controls.Actions)
                {
                    UserInterface = gameObject.AddComponent<SessionUi>();
                    UserInterface.Initialize(this);
                    gameObject.AddComponent<FirstUseGuide>().Initialize(this);
                }
            }
            // Existing editor acceptance fixtures explicitly exercise spawned sessions.
            // Standalone production starts at the same main menu used after quit/load.
            if (Application.isEditor || gameObject.scene.name != "S013Cabin") BeginSession();
            else ReturnToMenu();
        }
        public void BeginSession() => BeginSession(false);
        internal void BeginRestoreSession() => BeginSession(true);
        internal void CompleteRestore() { Restoring = false; SetPaused(false); }
        void BeginSession(bool restoring)
        {
            if (Player) return;
            SettingsOpen = false;
            Generation++; Restoring = restoring;
            if (noiseTuning == null || !noiseTuning.Valid) throw new System.InvalidOperationException("Invalid gameplay noise tuning on SessionFlow.");
            if (!playerPrefab || !spawnPoint) { Debug.LogError("Session requires player prefab and spawn point.", this); return; }
            foreach (var door in doors) if (door) door.ResetDoor();
            Player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            input = Player.GetComponent<PlayerInputReader>();
            input.ExternalUiOwner = Controls && Controls.Actions;
            input.JournalRequested += OpenJournal;
            health = Player.GetComponent<PlayerHealth>();
            var preferences = GetComponent<Audio.ProductionAudio>()?.Preferences;
            var look = Player.GetComponent<FirstPersonLook>();
            if (preferences != null && look)
            {
                look.ApplyPreferences(preferences);
            }
            
            // S005: Initialize Inventory
            inventory = Player.AddComponent<LastSignal.Inventory.PlayerInventory>();
            var cam = Player.GetComponentInChildren<Camera>();
            inventory.ConfigureDrop(cam ? cam.transform : Player.transform);

            // Reuse the authored view; legacy validation scenes get a typed fallback.
            var ui = FindObjectOfType<LastSignal.Inventory.UI.InventoryUI>(true);
            if (!ui) ui = LastSignal.Inventory.UI.InventoryViewFactory.Create(inventory.Capacity);
            inventoryView = ui;
            if (ui) ui.Bind(inventory, this, input);

            if (health) health.Died += OnPlayerDied;
            input.PauseRequested += TogglePause;
            input.FocusLost += Pause;
            var population = GetComponent<LastSignal.AI.WorldPopulationManager>();
            Noise?.End();
            Noise = new Noise.GameplayNoiseSystem(NoiseTime, NoiseAllowed, population ? new Noise.WorldPressureNoiseAdapter(population, this) : null, Debug.LogException);
            Player.AddComponent<Noise.GameplayNoiseContext>().Bind(this);
            if (zombieEncounter) zombieEncounter.Begin(Player);
            loot = GetComponent<LastSignal.Loot.LootPopulationService>();
            if (loot && !restoring) loot.Begin(Player);
            shelter = GetComponent<Shelter.ShelterLoop>();
            if (shelter) shelter.Begin(this);
            var worldClock = GetComponent<WorldTime.WorldClock>();
            if (worldClock) worldClock.Begin();
            var catalog = GetComponent<Persistence.SaveSession>()?.Catalog;
            if (enableSurvival)
            {
                if (!catalog || !catalog.GetItem(new Inventory.Data.StableItemId(PlayerSurvival.BackpackId)))
                    throw new System.InvalidOperationException("Survival session requires the authored field-pack catalog entry.");
                var survival = Player.AddComponent<PlayerSurvival>();
                survival.Initialize(this, inventory);
                // One authored starter pack in a NEW session; hydration never grants another.
                if (!restoring) inventory.TryAdd(catalog.GetItem(new Inventory.Data.StableItemId(PlayerSurvival.BackpackId)), 1);
            }
            var cells = GetComponent<WorldCells.WorldCellManager>();
            if (cells) cells.Begin(restoring);
            if (population) population.BeginSession();
            Player.GetComponent<PlayerCombatController>()?.BindNoise(Noise, noiseTuning, 1);
            Player.GetComponent<FirstPersonMotor>()?.BindNoise(Noise, noiseTuning, 1);
            GetComponent<Shelter.ShelterSite>()?.Begin();
            GetComponent<Objectives.RelayMission>()?.Begin();
            GetComponent<Vehicles.VehicleWorld>()?.Begin(this);
            GetComponent<Audio.ProductionAudio>()?.BeginSession();
            SetPaused(restoring);
            Debug.Log("S001 session started: one player, local input.");
        }
        public bool OpenInventory(LastSignal.Inventory.UI.InventoryUI view)
        {
            if (!view || view != inventoryView || Screen != SessionScreen.Gameplay) return false;
            SetPaused(true);
            view.ShowFromSession();
            return true;
        }
        public void CloseInventory(LastSignal.Inventory.UI.InventoryUI view)
        {
            if (!view || view != inventoryView || !view.IsOpen) return;
            view.HideFromSession();
            if (Player && !PlayerDead && !Restoring && !PreparationOpen && !JournalOpen) SetPaused(false);
        }
        public void TogglePause() { if (SettingsOpen) { CloseSettings(); return; } if (Restoring || PlayerDead) return; if (InventoryOpen) CloseInventory(inventoryView); else if (JournalOpen) GetComponent<Objectives.RelayMission>().CloseJournal(); else if (ProductionOpen) GetComponent<Shelter.ShelterSite>().Close(); else if (CargoOpen) CargoPoint.Close(); else if (PreparationOpen) shelter.ClosePreparation(); else if (Player) SetPaused(!Paused); }
        public void Pause() { if (Player) SetPaused(true); }
        public void Resume() { if (SettingsOpen) { CloseSettings(); return; } if (Restoring || PlayerDead) return; if (InventoryOpen) CloseInventory(inventoryView); else if (JournalOpen) GetComponent<Objectives.RelayMission>().CloseJournal(); else if (ProductionOpen) GetComponent<Shelter.ShelterSite>().Close(); else if (CargoOpen) CargoPoint.Close(); else if (PreparationOpen) shelter.ClosePreparation(); else if (Player) SetPaused(false); }
        void SetPaused(bool value)
        {
            Paused = value;
            if (value || PlayerDead) GetComponent<Vehicles.VehicleWorld>()?.Suspend();
            if (value && Player) Player.GetComponent<FirstPersonMotor>()?.ResetNoiseCadence();
            if (zombieEncounter) zombieEncounter.SetPaused(value);
            Time.timeScale = value ? 0 : 1;
            if (input) input.SetGameplay(!value && !PlayerDead);
            if (value || PlayerDead) CancelPlayerCombat();
            Cursor.lockState = value || PlayerDead ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value || PlayerDead;
        }
        void CancelPlayerCombat()
        {
            if (!Player) return;
            var combat = Player.GetComponent<PlayerCombatController>();
            if (combat) combat.CancelGameplayActions(PlayerDead);
        }
        void OnPlayerDied()
        {
            CloseSettings();
            if (CargoOpen) CargoPoint.Close();
            if (ProductionOpen) GetComponent<Shelter.ShelterSite>().Close();
            if (inventoryView) inventoryView.HideFromSession();
            if (input) input.SetGameplay(false);
            GetComponent<Vehicles.VehicleWorld>()?.Suspend();
            CancelPlayerCombat();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void ReturnToMenu()
        {
            CloseSettings();
            CargoPoint = null;
            if (inventoryView) inventoryView.HideFromSession();
            GetComponent<Audio.ProductionAudio>()?.EndSession();
            Generation++; Restoring = false;
            Noise?.End(); Noise = null;
            GetComponent<Objectives.RelayMission>()?.End();
            GetComponent<Shelter.ShelterSite>()?.End();
            var cells = GetComponent<WorldCells.WorldCellManager>();
            if (cells) cells.End();
            var worldClock = GetComponent<WorldTime.WorldClock>();
            if (worldClock) worldClock.End();
            var pop = GetComponent<LastSignal.AI.WorldPopulationManager>();
            if (pop) pop.ClearSession();
            if (shelter) shelter.End();
            if (loot) loot.End();
            if (health) health.Died -= OnPlayerDied;
            health = null;
            if (zombieEncounter) zombieEncounter.End();
            if (input)
            {
                input.JournalRequested -= OpenJournal;
                input.PauseRequested -= TogglePause;
                input.FocusLost -= Pause;
                input.SetGameplay(false);
            }
            var ui = inventoryView;
            if (ui) ui.Unbind();
            inventoryView = null;
            if (Player) { Player.SetActive(false); Destroy(Player); }
            GetComponent<Vehicles.VehicleWorld>()?.End();
            Player = null;
            input = null;
            inventory = null;
            Paused = false;
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        void OnDisable() { Noise?.End(); }
        void OnDestroy()
        {
            ReturnToMenu();
            Time.timeScale = 1;
        }
    }
}
