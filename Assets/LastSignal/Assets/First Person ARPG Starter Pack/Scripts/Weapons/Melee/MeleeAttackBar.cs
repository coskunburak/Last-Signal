using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace FPS.Scripts.UI{
    [RequireComponent(typeof(CanvasGroup))]
    public class MeleeAttackBar : MonoBehaviour {
        public CanvasGroup canvasGroup;
        [FormerlySerializedAs("canvasFillImage")] public Image currentStrikeChargeBar;
        public Image perfectStrikeChargeBar;

        public float fadeSpeed = 0.1f;

        private bool incrementedChargedThisTurn = false;

        private float maxBarPosition;

        public void UpdateAttackBar(float chargePercent, float perfectStrikeOffset, float perfectStrikeSize){
            // determine where the perfect strike bar should be placed
            var rectTransform = perfectStrikeChargeBar.rectTransform;
            Vector2 perfectStrikeTranslationRect = rectTransform.anchoredPosition;
            perfectStrikeTranslationRect = new Vector2(maxBarPosition * perfectStrikeOffset, perfectStrikeTranslationRect.y);
            rectTransform.anchoredPosition = perfectStrikeTranslationRect;

            // make sure the despite the offset, the perfect strike indicator it does not exceed the actual total bar size
            float perfectStrikeFillAmount = Mathf.Clamp(perfectStrikeSize, 0.0f, 1.0f - perfectStrikeOffset);
            perfectStrikeChargeBar.fillAmount = perfectStrikeFillAmount;
            
            currentStrikeChargeBar.fillAmount = chargePercent;
            // set flag, which is used to determine whether the bar should be faded in or out
            incrementedChargedThisTurn = true;
        }

        private void Awake(){
            canvasGroup.alpha = 0.0f;
            currentStrikeChargeBar.fillAmount = 0;
            maxBarPosition = perfectStrikeChargeBar.rectTransform.rect.width;
        }

        private void LateUpdate(){
            // Fade the bar in and out
            if (incrementedChargedThisTurn)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1.0f, fadeSpeed);
            }
            else
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0.0f, fadeSpeed);
            }

            incrementedChargedThisTurn = false;
        }
    }
}