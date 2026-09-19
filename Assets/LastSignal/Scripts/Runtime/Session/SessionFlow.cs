using UnityEngine;

namespace LastSignal
{
    public sealed class SessionFlow : MonoBehaviour
    {
        [SerializeField] GameObject playerPrefab;
        [SerializeField] Transform spawnPoint;
        [SerializeField] DoorInteractable[] doors;
        [SerializeField] ZombieEncounter zombieEncounter;
        public void ConfigureEncounter(ZombieEncounter encounter) => zombieEncounter = encounter;
        public GameObject Player { get; private set; }
        public bool Paused { get; private set; }
        public bool InMenu => !Player;
        PlayerInputReader input;
        PlayerHealth health;
        public bool PlayerDead => health && !health.IsAlive;
        public void Configure(GameObject prefab, Transform spawn, DoorInteractable[] sceneDoors)
        { playerPrefab = prefab; spawnPoint = spawn; doors = sceneDoors; }
        void Start() => BeginSession();
        public void BeginSession()
        {
            if (Player) return;
            if (!playerPrefab || !spawnPoint) { Debug.LogError("Session requires player prefab and spawn point.", this); return; }
            foreach (var door in doors) if (door) door.ResetDoor();
            Player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            input = Player.GetComponent<PlayerInputReader>();
            health = Player.GetComponent<PlayerHealth>();
            if (health) health.Died += OnPlayerDied;
            input.PauseRequested += TogglePause;
            input.FocusLost += Pause;
            if (zombieEncounter) zombieEncounter.Begin(Player);
            SetPaused(false);
            Debug.Log("S001 session started: one player, local input.");
        }
        public void TogglePause() { if (Player) SetPaused(!Paused); }
        public void Pause() { if (Player) SetPaused(true); }
        public void Resume() { if (Player) SetPaused(false); }
        void SetPaused(bool value)
        {
            Paused = value;
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
            if (input) input.SetGameplay(false);
            CancelPlayerCombat();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void ReturnToMenu()
        {
            if (health) health.Died -= OnPlayerDied;
            health = null;
            if (zombieEncounter) zombieEncounter.End();
            if (input)
            {
                input.PauseRequested -= TogglePause;
                input.FocusLost -= Pause;
                input.SetGameplay(false);
            }
            if (Player) { Player.SetActive(false); Destroy(Player); }
            Player = null;
            input = null;
            Paused = false;
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        void OnDestroy()
        {
            ReturnToMenu();
            Time.timeScale = 1;
        }
    }
}
