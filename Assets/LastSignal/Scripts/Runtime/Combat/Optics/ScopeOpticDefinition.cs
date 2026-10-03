using UnityEngine;

namespace LastSignal
{
    [CreateAssetMenu(menuName = "Last Signal/Combat/Scope Optic")]
    public sealed class ScopeOpticDefinition : ScriptableObject
    {
        [SerializeField, Range(1f, 8f)] float magnification = 3f;
        [SerializeField, Range(128, 2048)] int textureSize = 512;
        [SerializeField, Min(.01f)] float fadeSeconds = .1f;
        [SerializeField, Min(.001f)] float eyeBoxRadius = .009f;
        [SerializeField, Min(.001f)] float minimumEyeDistance = .12f;
        [SerializeField, Min(.01f)] float maximumEyeDistance = .4f;
        [SerializeField, ColorUsage(false, true)] Color reticleColor = new Color(2f, .12f, .03f, 1f);
        [SerializeField, Range(.001f, .03f)] float dotRadius = .006f;
        [SerializeField, Range(0, .3f)] float ringRadius = .075f;
        [SerializeField, Range(.001f, .02f)] float ringWidth = .004f;
        [SerializeField, Range(8, 31)] int viewmodelLayer = 29;
        [SerializeField] int rendererIndex = -1;

        public float Magnification => Mathf.Clamp(magnification, 1, 8);
        public int TextureSize => Mathf.Clamp(Mathf.ClosestPowerOfTwo(textureSize), 128, 2048);
        public float FadeSeconds => Mathf.Max(.01f, fadeSeconds);
        public float EyeBoxRadius => Mathf.Max(.001f, eyeBoxRadius);
        public float MinimumEyeDistance => Mathf.Max(.001f, minimumEyeDistance);
        public float MaximumEyeDistance => Mathf.Max(MinimumEyeDistance + .01f, maximumEyeDistance);
        public Color ReticleColor => reticleColor;
        public float DotRadius => Mathf.Clamp(dotRadius, .001f, .03f);
        public float RingRadius => Mathf.Clamp(ringRadius, 0, .3f);
        public float RingWidth => Mathf.Clamp(ringWidth, .001f, .02f);
        public int ViewmodelLayer => Mathf.Clamp(viewmodelLayer, 8, 31);
        public int RendererIndex => rendererIndex;

        // Merceğin ekrandaki açısal boyutunu hesaba katar; main FOV / power kullanılmaz.
        public static float CalculateVerticalFov(float halfHeight, float eyeDistance, float power)
        {
            return Mathf.Clamp(2f * Mathf.Atan(Mathf.Max(.0001f, halfHeight) /
                (Mathf.Max(.001f, eyeDistance) * Mathf.Max(1f, power))) * Mathf.Rad2Deg, .1f, 120f);
        }
    }
}
