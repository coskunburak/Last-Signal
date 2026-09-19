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
        public void Configure(SessionFlow flow, Text promptText, Text statusText, Text crosshairText,
            GameObject menuPanel, Text title, Button start, Button resume, Button menu)
        {
            session = flow; prompt = promptText; status = statusText; crosshair = crosshairText;
            panel = menuPanel; panelTitle = title; startButton = start; resumeButton = resume; menuButton = menu;
        }
        public void SetAmmoDisplay(Text ammo) => ammoDisplay = ammo;
        void OnEnable()
        {
            if (!session) return;
            startButton.onClick.AddListener(session.BeginSession);
            resumeButton.onClick.AddListener(session.Resume);
            menuButton.onClick.AddListener(session.ReturnToMenu);
        }
        void OnDisable()
        {
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
                combat = observedPlayer ? observedPlayer.GetComponent<PlayerCombatController>() : null;
            }
            bool menu = session.InMenu, paused = session.Paused;
            panel.SetActive(menu || paused || session.PlayerDead);
            panelTitle.text = menu ? "LAST SIGNAL" : session.PlayerDead ? "ÖLDÜN" : "DURAKLATILDI";
            startButton.gameObject.SetActive(menu);
            resumeButton.gameObject.SetActive(!menu && !session.PlayerDead);
            menuButton.gameObject.SetActive(!menu);
            crosshair.enabled = !menu && !paused && !session.PlayerDead;
            prompt.text = menu || paused || session.PlayerDead || !interaction ? "" : interaction.Prompt;
            status.text = stance && stance.StandBlocked ? "Baş üstünde engel var. Açık alanda C ile tekrar dene." :
                "WASD  Hareket    MOUSE  Bakış    SHIFT  Koş    C  Çömel    E  Kullan    R  Şarjör    SAĞ FARE  Nişan    SOL FARE  Ateş    ESC  Menü";

            // Ammo display.
            if (ammoDisplay)
            {
                if (combat && combat.ActiveWeapon && combat.ActiveWeapon.RuntimeState != null)
                {
                    var rs = combat.ActiveWeapon.RuntimeState;
                    float hp = health ? health.CurrentHealth : -1, maxHp = health ? health.MaxHealth : -1;
                    if (displayedMagazine != rs.CurrentMagazine || displayedReserve != rs.ReserveAmmo || displayedHealth != hp || displayedMaxHealth != maxHp)
                    {
                        displayedMagazine = rs.CurrentMagazine; displayedReserve = rs.ReserveAmmo;
                        displayedHealth = hp; displayedMaxHealth = maxHp;
                        ammoDisplay.text = (health ? "CAN " + hp.ToString("0") + " / " + maxHp.ToString("0") + "    |    " : "") + rs.CurrentMagazine + " / " + rs.ReserveAmmo;
                    }
                    ammoDisplay.enabled = !menu && !paused;
                }
                else
                {
                    ammoDisplay.text = "";
                    ammoDisplay.enabled = false;
                }
            }
        }
    }
}

