using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Three small context lessons, stored in the existing profile. Display alone never completes one.</summary>
    public sealed class FirstUseGuide : MonoBehaviour
    {
        public const int Movement = 1, Loot = 2, Recovery = 4;
        SessionFlow flow;
        Audio.AudioPreferences preferences;
        GameObject player, canvas;
        Inventory.PlayerInventory inventory;
        PlayerHealth health;
        PlayerInputReader input;
        Text text;
        Vector3 lastPosition;
        float moved, previousHealth;
        int previousQuantity;
        bool foundLoot, needsRecovery;
        float nextSample;
        public int CurrentLesson { get; private set; }
        public void Initialize(SessionFlow owner)
        {
            flow = owner; preferences = flow.Controls.Preferences;
            canvas = new GameObject("First-use guidance", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 30;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            text = SessionUi.Label(canvas.transform, "", new Vector2(0, 210), new Vector2(850, 110), 24);
            FindAnyObjectByType<Audio.AudioSettingsView>()?.RefreshUiScale();
        }
        public static bool Pending(Audio.AudioPreferences profile, int lesson) =>
            ((profile.TutorialCompleted | profile.TutorialSkipped) & lesson) == 0;
        void Complete(int lesson)
        {
            if ((preferences.TutorialCompleted & lesson) != 0) return;
            preferences.TutorialCompleted |= lesson; preferences.Save();
        }
        public void Skip()
        {
            if (CurrentLesson == 0) return;
            preferences.TutorialSkipped |= CurrentLesson; preferences.Save(); CurrentLesson = 0; text.text = "";
        }
        public void ResetGuide()
        {
            preferences.TutorialCompleted = preferences.TutorialSkipped = 0; preferences.Save();
            moved = 0; foundLoot = false; needsRecovery = health && health.CurrentHealth < health.MaxHealth;
            CurrentLesson = 0;
        }
        void Unbind()
        {
            if (inventory) inventory.InventoryChanged -= InventoryChanged;
            if (health) health.HealthChanged -= HealthChanged;
            if (input) input.SkipTutorialRequested -= Skip;
            inventory = null; health = null; input = null;
        }
        void Bind()
        {
            Unbind(); player = flow.Player;
            moved = 0; foundLoot = needsRecovery = false;
            if (!player) return;
            lastPosition = player.transform.position;
            inventory = player.GetComponent<Inventory.PlayerInventory>(); health = player.GetComponent<PlayerHealth>(); input = player.GetComponent<PlayerInputReader>();
            previousQuantity = Quantity();
            if (inventory) inventory.InventoryChanged += InventoryChanged;
            if (health) { previousHealth = health.CurrentHealth; health.HealthChanged += HealthChanged; needsRecovery = previousHealth < health.MaxHealth; }
            if (input) input.SkipTutorialRequested += Skip;
        }
        int Quantity()
        { int count = 0; if (inventory) for (int i = 0; i < inventory.Capacity; i++) count += inventory.GetSlot(i).Quantity; return count; }
        void InventoryChanged()
        { int quantity = Quantity(); if (!flow.Restoring && quantity > previousQuantity) foundLoot = true; previousQuantity = quantity; }
        void HealthChanged()
        {
            if (!health || flow.Restoring) return;
            if (health.CurrentHealth < previousHealth) needsRecovery = true;
            if (needsRecovery && health.CurrentHealth > previousHealth) Complete(Recovery);
            previousHealth = health.CurrentHealth;
        }
        void Update()
        {
            if (!flow || preferences == null) return;
            if (player != flow.Player) Bind();
            if (!player || flow.PlayerDead || flow.Restoring) { text.text = ""; CurrentLesson = 0; return; }
            var position = player.transform.position;
            if (!flow.Paused && input.GameplayActive)
            {
                float distance = Vector3.Distance(position, lastPosition);
                if (distance < 2) moved += distance;
                if (moved >= 2) Complete(Movement);
            }
            lastPosition = position;
            if (flow.InventoryOpen && flow.InventoryView && flow.InventoryView.SelectedSlot >= 0) Complete(Loot);
            if (Time.unscaledTime < nextSample) return;
            nextSample = Time.unscaledTime + .2f;
            if (flow.Paused) { text.text = ""; CurrentLesson = 0; return; }
            CurrentLesson = needsRecovery && Pending(preferences, Recovery) ? Recovery :
                Pending(preferences, Movement) ? Movement : foundLoot && Pending(preferences, Loot) ? Loot : 0;
            string lesson = CurrentLesson == Movement ? flow.Controls.BindingText("Player/Move") + " ile yürü; " + flow.Controls.BindingText("Player/Look") + " ile çevrene bak." :
                CurrentLesson == Loot ? "Eşya aldın. " + flow.Controls.BindingText("Player/Inventory") + " ile envanteri aç; bir yığını seçerek taşı veya miktarını böl." :
                CurrentLesson == Recovery ? (player.GetComponent<PlayerSurvival>()?.State.Bleeding > 0 ?
                    "Kanaman var. Envanterden bandaj seç ve Kullan'a bas; üç saniye hareketsiz kal. Kanama durmadan uyuyamazsın." :
                    "Yaralandın. Güvenli barınağa dön; dinlenme noktası iyileşmeni sağlar. Yakın tehdit dinlenmeyi engelleyebilir.") : "";
            text.text = CurrentLesson == 0 ? "" : lesson + "\n" + flow.Controls.BindingText("Player/SkipTutorial") + " — Bu ipucunu atla";
        }
        void OnDisable() => Unbind();
        void OnDestroy() { if (canvas) Destroy(canvas); }
    }
}
