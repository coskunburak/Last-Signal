using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    public sealed class AcceptanceHud : MonoBehaviour
    {
        [SerializeField] SessionFlow session;
        [SerializeField] Text prompt, status, crosshair;
        [SerializeField] GameObject panel;
        public GameObject MenuRoot => panel;
        public Button StartButton => startButton;
        int promptRevision = -1;
        string controlsText = "", previousPrompt = "", composedPrompt = "";
        [SerializeField] Text panelTitle;
        [SerializeField] Button startButton, resumeButton, menuButton;
        [SerializeField] Text ammoDisplay;
        int displayedMagazine = -1, displayedReserve = -1;
        float displayedHealth = -1, displayedMaxHealth = -1;
        PlayerStamina stamina;
        int shownStamina=-1;
        Text staminaDisplay;
        Text survivalDisplay;
        PlayerSurvival survival;
        int shownWater = -1, shownFood = -1, shownBleeding = -1, shownTreatment = -1;
        string shownSurvivalFeedback;
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
        void Start() { if (session && prompt) gameObject.AddComponent<InputPromptGlyph>().Initialize(session, prompt); }
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
                stamina = observedPlayer ? observedPlayer.GetComponent<PlayerStamina>() : null;
                survival = observedPlayer ? observedPlayer.GetComponent<PlayerSurvival>() : null;
                shownWater = shownFood = shownBleeding = shownTreatment = -1; shownSurvivalFeedback = null;
                shownStamina=-1;
                BindAmmoPlayer();
            }
            if (!staminaDisplay && ammoDisplay)
            {
                staminaDisplay=Instantiate(ammoDisplay,ammoDisplay.transform.parent);
                staminaDisplay.name="StaminaDisplay";
                staminaDisplay.rectTransform.anchoredPosition += new Vector2(0,32);
                staminaDisplay.text="";
            }
            bool menu = session.InMenu, paused = session.Paused;
            if (!survivalDisplay && ammoDisplay)
            {
                survivalDisplay = Instantiate(ammoDisplay, ammoDisplay.transform.parent);
                survivalDisplay.name = "SurvivalDisplay";
                survivalDisplay.rectTransform.anchoredPosition += new Vector2(0, 85);
                survivalDisplay.rectTransform.sizeDelta = new Vector2(900, 72);
                survivalDisplay.fontSize = 18; survivalDisplay.text = "";
            }
            if (survivalDisplay)
            {
                survivalDisplay.enabled = survival && !menu && !paused && !session.PlayerDead;
                if (survival)
                {
                    int water = Mathf.CeilToInt((float)survival.State.Hydration), food = Mathf.CeilToInt((float)survival.State.Nutrition);
                    int bleeding = survival.State.Bleeding, treating = survival.ApplyingTreatment ? Mathf.CeilToInt(survival.TreatmentRemaining * 10) : -1;
                    if (water != shownWater || food != shownFood || bleeding != shownBleeding || treating != shownTreatment || survival.Feedback != shownSurvivalFeedback)
                    {
                        shownWater = water; shownFood = food; shownBleeding = bleeding; shownTreatment = treating; shownSurvivalFeedback = survival.Feedback;
                        survivalDisplay.text = "SU " + water + " / 100    YEMEK " + food + " / 100    " +
                            (bleeding > 0 ? "KANAMA " + bleeding + " — envanterden bandaj kullan" : "KANAMA YOK") + "\n" +
                            (treating >= 0 ? "Bandaj: " + (treating / 10f).ToString("0.0") + " sn — hareketsiz kal" : survival.Feedback);
                        survivalDisplay.color = bleeding > 0 || water <= 10 || food == 0 ? new Color(1, .5f, .3f) : Color.white;
                    }
                }
            }
            if(staminaDisplay)
            {
                staminaDisplay.enabled=stamina && !menu && !paused && !session.PlayerDead;
                int value=stamina?Mathf.CeilToInt(stamina.CurrentStamina):-1;
                if(value!=shownStamina) { shownStamina=value; staminaDisplay.text="STAMINA  " + value + " / 100"; }
                staminaDisplay.color=stamina && stamina.Exhausted?new Color(1,.5f,.3f):Color.white;
            }
            panel.SetActive(session.SessionMenuVisible);
            panelTitle.text = menu ? "LAST SIGNAL" : session.PlayerDead ? "ÖLDÜN" : "DURAKLATILDI";
            if (session.PlayerDead && health && health.LastDamage.Category == DamageCategory.Survival && survival)
                panelTitle.text = "ÖLDÜN — " + (survival.State.Bleeding > 0 ? "KANAMA" : survival.State.Hydration <= 0 ? "SUSUZLUK" : "AÇLIK");
            startButton.gameObject.SetActive(menu);
            resumeButton.gameObject.SetActive(!menu && !session.PlayerDead);
            menuButton.gameObject.SetActive(!menu);
            crosshair.enabled = !menu && !paused && !session.PlayerDead;
            var controls = session.Controls;
            if (controls && controls.Actions && promptRevision != controls.PresentationRevision)
            {
                promptRevision = controls.PresentationRevision;
                previousPrompt = null;
                controlsText = controls.BindingText("Player/Move") + " Hareket   " + controls.BindingText("Player/Look") + " Bakış   " +
                    controls.BindingText("Player/Interact") + " Kullan   " + controls.BindingText("Player/Inventory") + " Envanter   " +
                    controls.BindingText("Player/Journal") + " Günlük   " + controls.BindingText("Player/Pause") + " Menü";
            }
            string currentPrompt = interaction ? interaction.Prompt : "";
            if (currentPrompt != previousPrompt)
            {
                previousPrompt = currentPrompt;
                composedPrompt = string.IsNullOrEmpty(currentPrompt) ? "" :
                    (controls && controls.Actions ? controls.BindingText("Player/Interact") + " — " : "") + currentPrompt;
            }
            prompt.text = menu || paused || session.PlayerDead ? "" : composedPrompt;
            status.text = stance && stance.StandBlocked ? "Baş üstünde engel var. Açık alanda tekrar çömelme tuşuna bas." : controlsText;

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
