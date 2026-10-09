using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Optional icon beside binding text. Unknown families/overrides keep their readable text.</summary>
    public sealed class InputPromptGlyph : MonoBehaviour
    {
        SessionFlow flow;
        Text prompt;
        RawImage icon;
        InputGlyphCatalog catalog;
        int revision = -1;
        public void Initialize(SessionFlow owner, Text label)
        {
            flow = owner; prompt = label;
            catalog = Resources.Load<InputGlyphCatalog>("InputGlyphCatalog");
            var go = new GameObject("Interaction glyph", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(label.transform, false);
            icon = go.GetComponent<RawImage>(); icon.raycastTarget = false;
            var rect = icon.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.sizeDelta = new Vector2(40, 40); rect.anchoredPosition = new Vector2(-28, 0);
        }
        void LateUpdate()
        {
            if (!flow || !icon) return;
            var input = flow.Controls;
            if (!input || !input.Actions) { icon.enabled = false; return; }
            if (revision != input.PresentationRevision)
            {
                revision = input.PresentationRevision;
                var action = input.Actions.FindAction("Player/Interact", true);
                int index = input.BindingIndex(action, input.Family != InputDeviceFamily.KeyboardMouse);
                icon.texture = catalog && index >= 0 ? catalog.Resolve(input.Family, action.bindings[index].effectivePath) : null;
            }
            icon.enabled = icon.texture && flow.Screen == SessionScreen.Gameplay && !string.IsNullOrWhiteSpace(prompt.text);
        }
        void OnDestroy() { if (icon) Destroy(icon.gameObject); }
    }
}
