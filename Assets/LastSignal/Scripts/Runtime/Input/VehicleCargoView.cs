using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>uGUI replacement for the existing cargo view, using VehicleActor.TransferCargo only.</summary>
    public sealed class VehicleCargoView : MonoBehaviour
    {
        SessionFlow flow;
        Vehicles.VehicleActor actor;
        PlayerInventory inventory;
        GameObject root;
        Transform carriedPane, cargoPane;
        Button[] carriedRows, cargoRows;
        Text message;
        Button one, stack;
        long playerRevision = -1, cargoRevision = -1;
        int selected = -1;
        bool deposit;
        ItemDefinition selectedItem;
        public GameObject Root => root;
        public void Initialize(SessionFlow owner, Transform parent)
        {
            flow = owner;
            root = SessionUi.Panel("Araç bagajı", parent, Vector2.zero, new Vector2(1460, 800));
            SessionUi.Label(root.transform, "TAŞINAN                                 ARAÇ BAGAJI", new Vector2(0, 350), new Vector2(1300, 50), 26);
            carriedPane = Pane(new Vector2(-350, 10)); cargoPane = Pane(new Vector2(350, 10));
            one = SessionUi.Button("Birini aktar", root.transform, new Vector2(-440, -325), () => Transfer(false));
            stack = SessionUi.Button("Sığanı aktar", root.transform, new Vector2(-130, -325), () => Transfer(true));
            SessionUi.Button("Geri", root.transform, new Vector2(500, -325), () => flow.CargoPoint?.Close());
            message = SessionUi.Label(root.transform, "", new Vector2(0, -385), new Vector2(1350, 36), 20);
            root.SetActive(false);
        }
        Transform Pane(Vector2 position)
        {
            var panel = SessionUi.Panel("Pane", root.transform, position, new Vector2(650, 600));
            var scroll = panel.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 35;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = SessionUi.Panel("Viewport", panel.transform, Vector2.zero, new Vector2(650, 600)).GetComponent<RectTransform>();
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Rows", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero; scroll.viewport = viewport; scroll.content = content; return content;
        }
        Button[] Rows(Transform parent, int count, bool carried)
        {
            var rows = new Button[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                rows[i] = SessionUi.Button("Yuva", parent, new Vector2(0, -30 - i * 56), () => Select(carried, index));
                var rect = (RectTransform)rows[i].transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(600, 48);
            }
            ((RectTransform)parent).sizeDelta = new Vector2(0, count * 56 + 10); return rows;
        }
        void Bind(Vehicles.VehicleActor next)
        {
            actor = next; inventory = actor ? flow.Player.GetComponent<PlayerInventory>() : null;
            if (carriedRows != null) foreach (var row in carriedRows) if (row) Destroy(row.gameObject);
            if (cargoRows != null) foreach (var row in cargoRows) if (row) Destroy(row.gameObject);
            carriedRows = inventory ? Rows(carriedPane, inventory.Capacity, true) : null;
            cargoRows = actor ? Rows(cargoPane, actor.Resources.CargoCapacity, false) : null;
            playerRevision = cargoRevision = -1; selected = -1;
        }
        void Update()
        {
            if (!root || !flow) return;
            bool visible = flow.Screen == SessionScreen.VehicleCargo;
            if (root.activeSelf != visible) { root.SetActive(visible); selected = -1; playerRevision = cargoRevision = -1; }
            if (!visible) return;
            var next = flow.CargoPoint.Actor;
            if (next != actor || !inventory) Bind(next);
            if (actor && (playerRevision != inventory.Revision || cargoRevision != actor.Resources.CargoRevision))
            { selected = -1; Refresh(); }
        }
        void Select(bool carried, int index)
        {
            if (!root.activeInHierarchy || !actor || !inventory) return;
            if (playerRevision != inventory.Revision || cargoRevision != actor.Resources.CargoRevision)
            { selected = -1; Refresh(); message.text = "Eşyalar değişti. Yeniden seç."; return; }
            deposit = carried; selected = index;
            var slot = carried ? inventory.GetSlot(index) : actor.Resources.GetCargoSlot(index);
            selectedItem = slot.Item;
            Refresh();
        }
        void Transfer(bool all)
        {
            if (!root.activeInHierarchy || !actor || selected < 0) return;
            if (playerRevision != inventory.Revision || cargoRevision != actor.Resources.CargoRevision)
            { selected = -1; Refresh(); message.text = "Eşyalar değişti. Yeniden seç."; return; }
            var slot = deposit ? inventory.GetSlot(selected) : actor.Resources.GetCargoSlot(selected);
            if (slot.IsEmpty || slot.Item != selectedItem) { selected = -1; Refresh(); return; }
            var result = actor.TransferCargo(inventory, slot.Item, all ? slot.Quantity : 1, deposit);
            if (result.Moved > 0) selected = -1;
            Refresh(); message.text = Shelter.ShelterStorageUI.DescribeTransfer(deposit, slot.Item.DisplayName, result);
        }
        void Refresh()
        {
            if (!actor || !inventory) return;
            playerRevision = inventory.Revision; cargoRevision = actor.Resources.CargoRevision;
            for (int i = 0; i < carriedRows.Length; i++) Present(carriedRows[i], inventory.GetSlot(i), deposit && selected == i);
            for (int i = 0; i < cargoRows.Length; i++) Present(cargoRows[i], actor.Resources.GetCargoSlot(i), !deposit && selected == i);
            one.interactable = stack.interactable = selected >= 0 && selectedItem;
            message.text = selected >= 0 ? "Seçili türü bir adet veya sığan miktar kadar aktar." : "Bir yığın seç; aktarım miktarını seç.";
        }
        static void Present(Button row, InventorySlot slot, bool selected)
        {
            row.interactable = !slot.IsEmpty;
            row.GetComponentInChildren<Text>().text = slot.IsEmpty ? "Boş" : (selected ? "▶ " : "") + slot.Item.DisplayName + " ×" + slot.Quantity;
        }
        void OnDestroy() { if (root) Destroy(root); }
    }
}
