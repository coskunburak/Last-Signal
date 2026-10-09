using UnityEngine;
using UnityEngine.UI;
using LastSignal.Inventory.Data;

namespace LastSignal.Inventory.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        public GameObject Root => panel;
        [SerializeField] InventorySlotUI[] slotUIs;
        
        [SerializeField] int selectedSlot = -1;
        [SerializeField] Button dropButton;
        [SerializeField] Button splitButton;
        [SerializeField] Text feedbackText;

        PlayerInventory inventory;
        SessionFlow session;
        PlayerInputReader input;
        ItemDefinition selectedItem;
        bool splitPending;
        long selectedRevision;
        Slider splitAmount;
        Text splitAmountText;
        Button useButton, unequipButton;
        public int SplitAmount => splitAmount ? Mathf.RoundToInt(splitAmount.value) : 0;
        public int SelectedSlot => selectedSlot;
        public bool SplitPending => splitPending;
        public string FeedbackMessage => feedbackText ? feedbackText.text : "";
        public bool IsOpen => panel && panel.activeSelf;
        public void ConfigureView(GameObject root, InventorySlotUI[] slots, Button drop)
        { panel = root; slotUIs = slots; dropButton = drop; }

        void Awake()
        {
            if (panel) panel.SetActive(false);
            EnsureActionControls();
            if (dropButton)
            {
                dropButton.onClick.AddListener(OnDropClicked);
                dropButton.interactable = false;
            }
            if (splitButton)
            {
                splitButton.onClick.AddListener(BeginSplitHalf);
                splitButton.interactable = false;
            }
            if (useButton) useButton.onClick.AddListener(UseSelected);
            if (unequipButton) unequipButton.onClick.AddListener(UnequipBackpack);
        }

        void EnsureActionControls()
        {
            if (!panel || !dropButton) return;
            var parent = dropButton.transform.parent;
            var dropRect = dropButton.GetComponent<RectTransform>();
            var font = dropButton.GetComponentInChildren<Text>()?.font;
            if (!font) font = ProductionUiFont.Resolve();
            if (!splitButton)
            {
                var rect = new GameObject("Split selected stack", typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = dropRect.anchorMin;
                rect.pivot = dropRect.pivot; rect.sizeDelta = new Vector2(230, 46);
                rect.anchoredPosition = new Vector2(0, dropRect.anchoredPosition.y + 60);
                var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.2f, .25f, .32f);
                splitButton = rect.gameObject.AddComponent<Button>(); splitButton.targetGraphic = image;
                CreateText(rect, "Yarısını böl → boş yuva", font);
            }
            if (!splitAmount)
            {
                var go = new GameObject("Split amount", typeof(RectTransform), typeof(Slider));
                var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
                rect.anchorMin = rect.anchorMax = dropRect.anchorMin; rect.sizeDelta = new Vector2(230, 30);
                rect.anchoredPosition = new Vector2(300, dropRect.anchoredPosition.y + 60);
                splitAmount = go.GetComponent<Slider>(); splitAmount.wholeNumbers = true; splitAmount.minValue = 1; splitAmount.maxValue = 2;
                var track = SessionUi.Panel("Track", rect, Vector2.zero, new Vector2(230, 6));
                var handle = SessionUi.Panel("Handle", rect, Vector2.zero, new Vector2(18, 28));
                splitAmount.handleRect = (RectTransform)handle.transform; splitAmount.targetGraphic = handle.GetComponent<Image>();
                splitAmountText = SessionUi.Label(parent, "", new Vector2(300, dropRect.anchoredPosition.y + 95), new Vector2(280, 30), 18);
                splitAmountText.rectTransform.anchorMin = splitAmountText.rectTransform.anchorMax = dropRect.anchorMin;
                splitAmount.onValueChanged.AddListener(AmountChanged);
                go.SetActive(false); splitAmountText.gameObject.SetActive(false);
            }
            if (!feedbackText)
            {
                var rect = new GameObject("Inventory feedback", typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = dropRect.anchorMin;
                rect.pivot = dropRect.pivot; rect.sizeDelta = new Vector2(410, 250);
                rect.anchoredPosition = new Vector2(735, 0);
                rect.gameObject.AddComponent<Image>().color = new Color(.04f, .05f, .07f, .98f);
                feedbackText = CreateText(rect, "", font);
                feedbackText.rectTransform.offsetMin = new Vector2(12, 12);
                feedbackText.rectTransform.offsetMax = new Vector2(-12, -12);
            }
            if (!useButton) useButton = ActionButton(parent, dropRect, font, "Kullan / Tak", new Vector2(-300, 60));
            if (!unequipButton) unequipButton = ActionButton(parent, dropRect, font, "Çantayı çıkar", new Vector2(0, 115));
        }
        static Button ActionButton(Transform parent, RectTransform drop, Font font, string title, Vector2 offset)
        {
            var rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = drop.anchorMin;
            rect.pivot = drop.pivot; rect.sizeDelta = new Vector2(230, 44);
            rect.anchoredPosition = new Vector2(offset.x, drop.anchoredPosition.y + offset.y);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.2f, .25f, .32f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            CreateText(rect, title, font); return button;
        }
        void EnsureSlots()
        {
            if (!inventory || slotUIs == null || slotUIs.Length == 0 || !slotUIs[0]) return;
            int oldCount = slotUIs.Length;
            if (oldCount < inventory.Capacity)
            {
                System.Array.Resize(ref slotUIs, inventory.Capacity);
                for (int i = oldCount; i < slotUIs.Length; i++)
                    slotUIs[i] = Instantiate(slotUIs[0], slotUIs[0].transform.parent);
            }
            for (int i = 0; i < slotUIs.Length; i++)
            {
                var view = slotUIs[i]; if (!view) continue;
                view.gameObject.SetActive(i < inventory.Capacity); view.Configure(i, this); view.FitCompactLayout();
                var rect = (RectTransform)view.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(130, 50);
                rect.anchoredPosition = new Vector2(-414 + (i % 7) * 138, 180 - (i / 7) * 60);
            }
        }
        static Text CreateText(RectTransform parent, string value, Font font)
        {
            var rect = new GameObject("Label", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter; label.color = Color.white;
            label.raycastTarget = false; label.text = value; return label;
        }

        public void Bind(PlayerInventory inv, SessionFlow sess, PlayerInputReader inp)
        {
            Unbind();
            inventory = inv;
            session = sess;
            input = inp;

            if (inventory)
            {
                inventory.InventoryChanged += Refresh;
                if (slotUIs != null)
                {
                    for (int i = 0; i < slotUIs.Length; i++)
                    {
                        if (slotUIs[i]) slotUIs[i].Configure(i, this);
                    }
                }
            }

            if (input)
            {
                input.InventoryRequested += ToggleUI;
            }
        }

        public void Unbind()
        {
            if (IsOpen) Close();
            if (inventory) inventory.InventoryChanged -= Refresh;
            if (input) input.InventoryRequested -= ToggleUI;
            if (panel) panel.SetActive(false);
            ClearSelection();
            inventory = null;
            session = null;
            input = null;
        }

        public void Close()
        {
            if (session) session.CloseInventory(this);
            else HideFromSession();
        }

        internal void HideFromSession()
        {
            if (panel) panel.SetActive(false);
            ClearSelection();
            if (dropButton) dropButton.interactable = false;
            if (splitButton) splitButton.interactable = false;
        }

        internal void ShowFromSession()
        { ClearSelection(); if (feedbackText) feedbackText.text = "Bir eşya seç; kullan, tak, taşı, böl veya bırak."; panel.SetActive(true); Refresh(); }

        public bool Open() => isActiveAndEnabled && inventory && panel && session && session.OpenInventory(this);

        void OnDisable() { if (IsOpen) Close(); }

        void OnDestroy()
        {
            Unbind();
            if (dropButton) dropButton.onClick.RemoveListener(OnDropClicked);
            if (splitButton) splitButton.onClick.RemoveListener(BeginSplitHalf);
            if (splitAmount) splitAmount.onValueChanged.RemoveListener(AmountChanged);
            if (useButton) useButton.onClick.RemoveListener(UseSelected);
            if (unequipButton) unequipButton.onClick.RemoveListener(UnequipBackpack);
        }

        void ToggleUI()
        {
            if (IsOpen) Close(); else Open();
        }

        void Refresh()
        {
            if (!inventory || !panel || !panel.activeSelf) return;
            EnsureSlots();
            if (selectedSlot >= 0 && (selectedSlot >= inventory.Capacity ||
                inventory.Revision != selectedRevision || inventory.GetSlot(selectedSlot).IsEmpty || inventory.GetSlot(selectedSlot).Item != selectedItem))
            {
                ClearSelection();
                SetFeedback("Seçili eşya değişti. Yeniden seç.");
            }

            if (slotUIs != null)
            {
                for (int i = 0; i < slotUIs.Length; i++)
                {
                    if (slotUIs[i]) slotUIs[i].Refresh(inventory.GetSlot(i), i == selectedSlot);
                }
            }
            
            if (dropButton)
            {
                bool hasSelection = selectedSlot >= 0 && selectedSlot < inventory.Capacity;
                dropButton.interactable = hasSelection && !inventory.GetSlot(selectedSlot).IsEmpty;
            }
            if (splitButton) splitButton.interactable = selectedSlot >= 0 && inventory.GetSlot(selectedSlot).Quantity > 1;
            var survival = inventory.GetComponent<PlayerSurvival>();
            if (useButton) useButton.interactable = survival && selectedItem && selectedItem.Use != ItemUse.None && !splitPending;
            if (unequipButton) unequipButton.interactable = survival && survival.Backpack;
        }
        public void UseSelected()
        {
            if (!IsOpen || !inventory || selectedSlot < 0 || splitPending) return;
            var survival = inventory.GetComponent<PlayerSurvival>();
            if (!survival) { SetFeedback("Bu oturumda eşya kullanımı yapılandırılmamış."); return; }
            bool applied = survival.Use(selectedSlot, selectedRevision);
            if (applied) ClearSelection();
            SetFeedback(survival.Feedback); Refresh();
        }
        public void UnequipBackpack()
        {
            if (!IsOpen || !inventory) return;
            var survival = inventory.GetComponent<PlayerSurvival>();
            if (!survival) return;
            survival.UnequipBackpack(); SetFeedback(survival.Feedback); Refresh();
        }

        void ClearSelection()
        { selectedSlot = -1; selectedItem = null; splitPending = false; if (splitAmount) splitAmount.gameObject.SetActive(false); if (splitAmountText) splitAmountText.gameObject.SetActive(false); }
        void AmountChanged(float value) { if (splitAmountText) splitAmountText.text = "Bölünecek miktar: " + Mathf.RoundToInt(value); }
        void SetFeedback(string message) { if (feedbackText) feedbackText.text = message; }

        public void BeginSplitHalf()
        {
            if (!IsOpen || !inventory || selectedSlot < 0) return;
            var slot = inventory.GetSlot(selectedSlot);
            if (slot.IsEmpty || slot.Item != selectedItem || slot.Quantity < 2)
            { SetFeedback("Bölmek için en az iki eşyalı bir yığın seç."); return; }
            splitPending = true;
            if (splitAmount)
            {
                splitAmount.maxValue = slot.Quantity - 1; splitAmount.value = Mathf.Max(1, slot.Quantity / 2);
                splitAmount.gameObject.SetActive(true); splitAmountText.gameObject.SetActive(true); AmountChanged(splitAmount.value);
            }
            SetFeedback("Miktarı ayarla ve boş hedef yuvayı seç. Tekrar aynı yuvaya basarak iptal et.");
        }

        public void OnSlotClicked(int index)
        {
            if (!IsOpen || !inventory || index < 0 || index >= inventory.Capacity) return;

            if (selectedSlot == -1)
            {
                var slot = inventory.GetSlot(index);
                if (slot.IsEmpty) { SetFeedback("Boş yuvada eşya yok."); return; }
                selectedSlot = index;
                selectedItem = slot.Item;
                selectedRevision = inventory.Revision;
                var survival = inventory.GetComponent<PlayerSurvival>();
                SetFeedback(slot.Item.DisplayName + " ×" + slot.Quantity + " | " + slot.Item.MassGrams + " g/adet | Yığın " + slot.Item.MaxStack + "\n" + slot.Item.Description + "\n" + (survival ?
                    "  Su " + survival.State.Hydration.ToString("0") + " | Yemek " + survival.State.Nutrition.ToString("0") +
                    " | Kanama " + survival.State.Bleeding + " | Yuva " + inventory.Capacity +
                    "\nToplam kütle: " + (inventory.TotalMassGrams + (survival.Backpack ? survival.Backpack.MassGrams : 0)) + " g (takılı çanta dahil). Kapasite yuva ile sınırlıdır." : ""));
            }
            else if (selectedSlot != index)
            {
                var source = inventory.GetSlot(selectedSlot);
                if (inventory.Revision != selectedRevision || source.IsEmpty || source.Item != selectedItem)
                { ClearSelection(); SetFeedback("Seçili eşya değişti. Yeniden seç."); Refresh(); return; }
                bool wasSplit = splitPending;
                bool succeeded = splitPending
                    ? inventory.TrySplit(selectedSlot, index, splitAmount ? SplitAmount : source.Quantity / 2)
                    : inventory.TryMove(selectedSlot, index);
                if (succeeded)
                { ClearSelection(); SetFeedback(wasSplit ? "Yığın bölündü." : "Eşya taşındı."); }
                else SetFeedback(splitPending ? "Bölme reddedildi: boş hedef yuva gerekli." : "Taşıma reddedildi: hedef dolu veya işlem meşgul.");
            }
            else
            {
                ClearSelection(); SetFeedback("Seçim iptal edildi.");
            }
            Refresh();
        }

        void OnDropClicked()
        {
            if (!IsOpen || !inventory) return;
            if (selectedSlot >= 0 && selectedSlot < inventory.Capacity)
            {
                var slot = inventory.GetSlot(selectedSlot);
                if (inventory.Revision != selectedRevision || slot.IsEmpty || slot.Item != selectedItem) { ClearSelection(); SetFeedback("Seçili eşya değişti."); Refresh(); return; }
                bool dropped = inventory.TryDrop(selectedSlot, slot.Quantity);
                if (dropped) { ClearSelection(); SetFeedback("Yığın yere bırakıldı."); }
                else SetFeedback("Bırakma reddedildi: bu eşyanın dünya nesnesi yok veya işlem meşgul.");
                Refresh();
            }
        }
    }
}
