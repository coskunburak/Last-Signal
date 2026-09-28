using UnityEngine;
using UnityEngine.UI;

namespace FPS.Scripts.Characters.UI
{
    public abstract class StatsBar : MonoBehaviour
    {
        public Image fillImage;
        public Image previewFillImage;

        public float criticalStatPercent = 0f;
        public float timeTillBarCatchesUp = 0.4f;
        
        public GuiAlphaFlicker lowStatsFlickerIndicator;

        protected float timeStatWasLastUpdated;
        protected float maxBarPosition;
        protected float lastStatFillPercent;
        
        private void Start()
        {
            maxBarPosition = previewFillImage.rectTransform.rect.width;

            fillImage.fillAmount = 1f;
        
            lastStatFillPercent = 1f;
            previewFillImage.fillAmount = 0f;
            
            InitialiseStatBar();
        }

        private void Update()
        {
            // check if last stat fill percent (the white bar) is supposed to be catching up to to the real value
            if (timeTillBarCatchesUp + timeStatWasLastUpdated < Time.time)
            {
                previewFillImage.fillAmount = 0;
                // if it should, update it!
                lastStatFillPercent = GetStatFillPercent();
            }

            if (lowStatsFlickerIndicator)
            {
                // show the indicator (if applicable) that shows the player that this stat is critically low
                lowStatsFlickerIndicator.shouldShow = GetStatFillPercent() < criticalStatPercent;
            }
            
            UpdateStatBar();
        }

        protected virtual void UpdateStatBar()
        {
            
        }

        protected abstract void InitialiseStatBar();
        protected abstract float GetStatFillPercent();

        private float CalculateStatPercentDifference()
        {
            return lastStatFillPercent - GetStatFillPercent();
        }
        
        protected void OnStatValueDeducted()
        {
            // Note: white bar preview only implemented for when stat is deducted


            fillImage.fillAmount = GetStatFillPercent();
            timeStatWasLastUpdated = Time.time;
            float fillDifference = CalculateStatPercentDifference();
            if (fillDifference > 0f)
            {
                // calculate where the upcoming white bar should be positioned
                float previewImageX = (maxBarPosition * GetStatFillPercent());
                var staminaPreviewImageRectTransform = previewFillImage.rectTransform;
                staminaPreviewImageRectTransform.anchoredPosition = new Vector2(previewImageX,
                    staminaPreviewImageRectTransform.anchoredPosition.y);
                // make the size of the white bar equal to the difference in drained stat
                previewFillImage.fillAmount = fillDifference;
            }
            lastStatFillPercent = GetStatFillPercent();
        }

        protected void OnStatValueIncremented()
        {
            previewFillImage.fillAmount = 0;
            lastStatFillPercent = GetStatFillPercent();
            fillImage.fillAmount = GetStatFillPercent();
        }
    }
}