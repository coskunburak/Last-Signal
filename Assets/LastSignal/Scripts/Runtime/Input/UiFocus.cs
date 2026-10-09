using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>Non-color-only focus outline and scroll reveal; never selects or invokes an item.</summary>
    public sealed class UiFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        Outline outline;
        public void OnSelect(BaseEventData data)
        {
            var graphic = GetComponent<Selectable>()?.targetGraphic;
            if (graphic)
            {
                if (!outline) outline = graphic.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white; outline.effectDistance = new Vector2(3, -3); outline.enabled = true;
            }
            var scroll = GetComponentInParent<ScrollRect>();
            if (!scroll || !scroll.content || !scroll.viewport) return;
            Canvas.ForceUpdateCanvases();
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, (RectTransform)transform);
            var view = scroll.viewport.rect;
            float offset = bounds.max.y > view.yMax ? view.yMax - bounds.max.y :
                bounds.min.y < view.yMin ? view.yMin - bounds.min.y : 0;
            scroll.content.anchoredPosition += new Vector2(0, offset);
        }
        public void OnDeselect(BaseEventData data) { if (outline) outline.enabled = false; }
        void OnDisable() { if (outline) outline.enabled = false; }
    }
}
