using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Inventory.UI
{
    public class InventorySlotUI : MonoBehaviour
    {
        [SerializeField] Image iconImage;
        [SerializeField] Text quantityText; // Or TMPro if project uses it, falling back to standard UI text if we don't know. Wait, unity uses uGUI usually.
        // Actually, if we use standard `UnityEngine.UI.Text`, it's safer. Or `TMPro.TextMeshProUGUI`?
        // Let's use `UnityEngine.UI.Text` to be completely safe against missing references, but TMPro is very common. We can just use gameobject activation.

        public int Index { get; private set; }
        InventoryUI owner;

        Button btn;

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

        public void Refresh(InventorySlot slot)
        {
            if (slot.IsEmpty)
            {
                if (iconImage) iconImage.enabled = false;
                if (quantityText) quantityText.text = "";
            }
            else
            {
                if (iconImage)
                {
                    iconImage.sprite = slot.Item.Icon;
                    iconImage.enabled = slot.Item.Icon != null;
                }
                if (quantityText) quantityText.text = slot.Item.DisplayName + " x" + slot.Quantity;
            }
        }

        void OnClick()
        {
            owner?.OnSlotClicked(Index);
        }
    }
}
