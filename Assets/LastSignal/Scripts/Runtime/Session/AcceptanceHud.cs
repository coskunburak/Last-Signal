using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    public sealed class AcceptanceHud : MonoBehaviour
    {
        [SerializeField] SessionFlow session;
        [SerializeField] Text prompt, status, crosshair;
        [SerializeField] GameObject panel;
        [SerializeField] Text panelTitle;
        [SerializeField] Button startButton, resumeButton, menuButton;
        [SerializeField] Text ammoDisplay;
        int displayedMagazine = -1, displayedReserve = -1;
        float displayedHealth = -1, displayedMaxHealth = -1;
        GameObject observedPlayer;
        InteractionController interaction;
        PlayerStance stance;
        PlayerHealth health;
        PlayerCombatController combat;
        WeaponController ammoWeapon;
        PlayerHealth ammoHealth;
        public void Configure(SessionFlow flow, Text promptText, Text statusText, Text crosshairText,
            GameObject menuPanel, Text title, Button start, Button resume, Button menu)
        {
            session = flow; prompt = promptText; status = statusText; crosshair = crosshairText;
            panel = menuPanel; panelTitle = title; startButton = start; resumeButton = resume; menuButton = menu;
        }
        public void SetAmmoDisplay(Text ammo) { ammoDisplay = ammo; RefreshAmmo(); }
        void OnEnable()
        {
            if (!session) return;
            startButton.onClick.AddListener(session.BeginSession);
            resumeButton.onClick.AddListener(session.Resume);
            menuButton.onClick.AddListener(session.ReturnToMenu);
            BindAmmoPlayer();
        }
        void OnDisable()
        {
            UnbindAmmoPlayer();
            if (!session) return;
            startButton.onClick.RemoveListener(session.BeginSession);
            resumeButton.onClick.RemoveListener(session.Resume);
            menuButton.onClick.RemoveListener(session.ReturnToMenu);
        }
        void Update()
        {
            if (observedPlayer != session.Player)
            {
                observedPlayer = session.Player; displayedMagazine = displayedReserve = -1;
                interaction = observedPlayer ? observedPlayer.GetComponent<InteractionController>() : null;
                stance = observedPlayer ? observedPlayer.GetComponent<PlayerStance>() : null;
                health = observedPlayer ? observedPlayer.GetComponent<PlayerHealth>() : null;
                BindAmmoPlayer();
            }
            bool menu = session.InMenu, paused = session.Paused;
            panel.SetActive(menu || (paused && !session.PreparationOpen) || session.PlayerDead);
            panelTitle.text = menu ? "LAST SIGNAL" : session.PlayerDead ? "ÖLDÜN" : "DURAKLATILDI";
            startButton.gameObject.SetActive(menu);
            resumeButton.gameObject.SetActive(!menu && !session.PlayerDead);
            menuButton.gameObject.SetActive(!menu);
            crosshair.enabled = !menu && !paused && !session.PlayerDead;
            prompt.text = menu || paused || session.PlayerDead || !interaction ? "" : interaction.Prompt;
            status.text = stance && stance.StandBlocked ? "Baş üstünde engel var. Açık alanda C ile tekrar dene." :
                "WASD  Hareket    MOUSE  Bakış    SHIFT  Koş    C  Çömel    E  Kullan    R  Şarjör    SAĞ FARE  Nişan    SOL FARE  Ateş    ESC  Menü";

            if (ammoDisplay) ammoDisplay.enabled = ammoWeapon && !menu && !paused && !session.PlayerDead;
        }

        void BindAmmoPlayer()
        {
            UnbindAmmoPlayer();
            var player = session ? session.Player : null;
            combat = player ? player.GetComponent<PlayerCombatController>() : null;
            ammoHealth = player ? player.GetComponent<PlayerHealth>() : null;
            if (combat) combat.WeaponChanged += BindAmmoWeapon;
            if (ammoHealth) ammoHealth.HealthChanged += RefreshAmmo;
            BindAmmoWeapon();
        }
        void UnbindAmmoPlayer()
        {
            if (combat) combat.WeaponChanged -= BindAmmoWeapon;
            if (ammoHealth) ammoHealth.HealthChanged -= RefreshAmmo;
            if (ammoWeapon) ammoWeapon.AmmoChanged -= RefreshAmmo;
            combat = null; ammoHealth = null; ammoWeapon = null;
        }
        void BindAmmoWeapon()
        {
            if (ammoWeapon) ammoWeapon.AmmoChanged -= RefreshAmmo;
            ammoWeapon = combat ? combat.ActiveWeapon : null;
            if (ammoWeapon) ammoWeapon.AmmoChanged += RefreshAmmo;
            displayedMagazine = displayedReserve = -1;
            RefreshAmmo();
        }
        void RefreshAmmo()
        {
            if (!ammoDisplay) return;
            if (!ammoWeapon || !ammoWeapon.isActiveAndEnabled || ammoWeapon.RuntimeState == null)
            { ammoDisplay.text = ""; ammoDisplay.enabled = false; return; }
            var state = ammoWeapon.RuntimeState;
            int magazine = state.CurrentMagazine, reserve = state.ReserveAmmo;
            float hp = ammoHealth ? ammoHealth.CurrentHealth : -1, maxHp = ammoHealth ? ammoHealth.MaxHealth : -1;
            if (displayedMagazine == magazine && displayedReserve == reserve && displayedHealth == hp && displayedMaxHealth == maxHp) return;
            displayedMagazine = magazine; displayedReserve = reserve; displayedHealth = hp; displayedMaxHealth = maxHp;
            ammoDisplay.text = (ammoHealth ? "CAN " + hp.ToString("0") + " / " + maxHp.ToString("0") + "    |    " : "") + magazine + " / " + reserve;
        }
    }
}
