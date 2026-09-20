using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Inventory.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] InventorySlotUI[] slotUIs;
        
        // Minimal interaction: Select a slot, then click a "Drop" button
        [SerializeField] int selectedSlot = -1;
        [SerializeField] Button dropButton;

        PlayerInventory inventory;
        SessionFlow session;
        PlayerInputReader input;

        void Awake()
        {
            if (panel) panel.SetActive(false);
            if (dropButton)
            {
                dropButton.onClick.AddListener(OnDropClicked);
                dropButton.interactable = false;
            }
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
            if (inventory) inventory.InventoryChanged -= Refresh;
            if (input) input.InventoryRequested -= ToggleUI;
            if (panel) panel.SetActive(false);
            selectedSlot = -1;
            inventory = null;
            session = null;
            input = null;
        }

        public void Close()
        {
            if (panel) panel.SetActive(false);
            selectedSlot = -1;
            if (session && !session.InMenu) session.Resume();
        }

        void OnDestroy()
        {
            Unbind();
        }

        void ToggleUI()
        {
            if (!inventory || !panel) return;

            bool isOpening = !panel.activeSelf;
            panel.SetActive(isOpening);
            selectedSlot = -1;
            if (dropButton) dropButton.interactable = false;

            if (isOpening)
            {
                Refresh();
                if (session) session.Pause(); // Pause uses Time.timeScale=0 and unlocks cursor. Let's rely on that since it handles cursor.
            }
            else
            {
                if (session) session.Resume();
            }
        }

        void Refresh()
        {
            if (!inventory || !panel || !panel.activeSelf) return;

            if (slotUIs != null)
            {
                for (int i = 0; i < slotUIs.Length; i++)
                {
                    if (slotUIs[i]) slotUIs[i].Refresh(inventory.GetSlot(i));
                }
            }
            
            if (dropButton)
            {
                bool hasSelection = selectedSlot >= 0 && selectedSlot < inventory.Capacity;
                dropButton.interactable = hasSelection && !inventory.GetSlot(selectedSlot).IsEmpty;
            }
        }

        public void OnSlotClicked(int index)
        {
            if (!inventory) return;

            if (selectedSlot == -1)
            {
                selectedSlot = index;
            }
            else if (selectedSlot != index)
            {
                // Try move/merge/swap
                inventory.TryMove(selectedSlot, index);
                selectedSlot = -1;
            }
            else
            {
                // Clicked same slot, deselect
                selectedSlot = -1;
            }
            Refresh();
        }

        void OnDropClicked()
        {
            if (!inventory) return;
            if (selectedSlot >= 0 && selectedSlot < inventory.Capacity)
            {
                // Drop whole stack for now
                int amount = inventory.GetSlot(selectedSlot).Quantity;
                inventory.TryDrop(selectedSlot, amount);
                selectedSlot = -1;
                Refresh();
            }
        }
    }
}
