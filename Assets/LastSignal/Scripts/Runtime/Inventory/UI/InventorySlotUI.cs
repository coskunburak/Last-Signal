using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Inventory.UI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] Image iconImage;
        [SerializeField] Text quantityText;

        public int Index { get; private set; }
        InventoryUI owner;

        Button btn;
        public void ConfigureLabel(Text label) => quantityText = label;
        public void FitCompactLayout()
        {
            if (!quantityText) return;
            if (!iconImage)
            {
                var go = new GameObject("Item icon", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false); iconImage = go.GetComponent<Image>();
                iconImage.raycastTarget = false; iconImage.preserveAspect = true;
            }
            var iconRect = iconImage.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, .5f);
            iconRect.anchoredPosition = new Vector2(20, 0); iconRect.sizeDelta = new Vector2(32, 32);
            var rect = quantityText.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(40, 2); rect.offsetMax = new Vector2(-5, -2);
            quantityText.resizeTextForBestFit = true; quantityText.resizeTextMinSize = 12; quantityText.resizeTextMaxSize = 18;
            quantityText.horizontalOverflow = HorizontalWrapMode.Wrap;
            quantityText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        void Awake()
        {
            btn = GetComponent<Button>();
            if (btn) btn.onClick.AddListener(OnClick);
        }

        public void Configure(int index, InventoryUI uiOwner)
        {
            Index = index;
            owner = uiOwner;
        }

        public void Refresh(InventorySlot slot, bool selected = false)
        {
            if (slot.IsEmpty)
            {
                if (iconImage) iconImage.enabled = false;
                if (quantityText) quantityText.text = selected ? "▶ Boş" : "";
            }
            else
            {
                if (iconImage)
                {
                    iconImage.sprite = slot.Item.Icon;
                    iconImage.enabled = slot.Item.Icon != null;
                }
                if (quantityText) quantityText.text = (selected ? "▶ " : "") + slot.Item.DisplayName + " ×" + slot.Quantity;
            }
        }

        void OnClick()
        {
            owner?.OnSlotClicked(Index);
        }
    }
}
