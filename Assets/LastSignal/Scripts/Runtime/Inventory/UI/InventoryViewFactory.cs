using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Inventory.UI
{
    // Only legacy scenes without an authored InventoryUI use this view.
    // No gameplay state or private-field reflection belongs in UI construction.
    public static class InventoryViewFactory
    {
        public static InventoryUI Create(int capacity)
        {
            var root = new GameObject("Inventory", typeof(RectTransform));
            root.SetActive(false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>();
            var view = root.AddComponent<InventoryUI>();
            var panel = Rect("Panel", root.transform, Vector2.zero, new Vector2(1040, 650));
            panel.gameObject.AddComponent<Image>().color = new Color(.04f, .05f, .07f, .98f);
            Label(panel, "ENVANTER — OYUN DURAKLATILDI", new Vector2(0, 280), new Vector2(960, 44));
            var slots = new InventorySlotUI[capacity];
            for (int i = 0; i < capacity; i++)
            {
                var button = Button(panel, "Boş", new Vector2(-375 + (i % 4) * 250, 215 - (i / 4) * 65), new Vector2(240, 58));
                slots[i] = button.gameObject.AddComponent<InventorySlotUI>();
                slots[i].ConfigureLabel(button.GetComponentInChildren<Text>());
            }
            var drop = Button(panel, "Seçili yığını yere bırak", new Vector2(-200, -265), new Vector2(380, 48));
            var close = Button(panel, "Geri / ESC", new Vector2(260, -265), new Vector2(250, 48));
            view.ConfigureView(panel.gameObject, slots, drop);
            close.onClick.AddListener(view.Close);
            root.SetActive(true);
            return view;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        static Text Label(Transform parent, string text, Vector2 position, Vector2 size)
        {
            var label = Rect("Label", parent, position, size).gameObject.AddComponent<Text>();
            label.font = ProductionUiFont.Resolve();
            label.fontSize = 22; label.text = text; label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            return label;
        }
        static Button Button(Transform parent, string title, Vector2 position, Vector2 size)
        {
            var rect = Rect(title, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.16f, .19f, .24f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Label(rect, title, Vector2.zero, size - new Vector2(12, 4));
            return button;
        }
    }
}
