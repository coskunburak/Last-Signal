using UnityEngine;

namespace LastSignal
{
    public static class CombatPresentationPreference
    {
        public static bool ReducedEffects => PlayerPrefs.GetInt("LastSignal.ReducedCombatEffects", 0) != 0;
        public static float BloodIntensity => Mathf.Clamp(PlayerPrefs.GetFloat("LastSignal.BloodIntensity", 1), .35f, 1);
        public static void SetReducedEffects(bool value) => PlayerPrefs.SetInt("LastSignal.ReducedCombatEffects", value ? 1 : 0);
        public static void SetBloodIntensity(float value) => PlayerPrefs.SetFloat("LastSignal.BloodIntensity", Mathf.Clamp(value, .35f, 1));
    }

    [CreateAssetMenu(menuName = "Last Signal/Combat Feedback")]
    public sealed class CombatFeedbackProfile : ScriptableObject
    {
        public Material tracerMaterial, impactMaterial, casingMaterial;
        [Range(1, 8)] public int tracerEvery = 3;
        [Range(.001f, .025f)] public float tracerWidth = .007f;
        [Range(100, 2000)] public float tracerSpeed = 800;
        [Range(.005f, .08f)] public float tracerLifetime = .045f;
        [Range(.1f, 4)] public float tracerBrightness = 1.15f;
        public Color tracerColor = new Color(.8f, .68f, .43f, .7f);
        [Range(1, 16)] public int tracerCapacity = 8;
        [Range(1, 64)] public int markCapacity = 24;
        [Range(1, 30)] public float markLifetime = 18;
        [Range(5, 100)] public float effectDistance = 45;
        [Range(.01f, .1f)] public float markSize = .04f;
        public bool reducedEffects;
        public bool IsValid => tracerMaterial && impactMaterial && tracerCapacity > 0 && tracerCapacity <= 16 &&
            markCapacity > 0 && markCapacity <= 64 && tracerEvery > 0 && tracerEvery <= 8 &&
            float.IsFinite(tracerSpeed) && tracerSpeed >= 100 && tracerSpeed <= 2000 && float.IsFinite(tracerLifetime) && tracerLifetime > 0 && tracerLifetime <= .08f &&
            float.IsFinite(tracerWidth) && tracerWidth > 0 && tracerWidth <= .025f &&
            float.IsFinite(markLifetime) && markLifetime > 0 && markLifetime <= 30 && float.IsFinite(effectDistance) && effectDistance > 0 && effectDistance <= 100 &&
            float.IsFinite(markSize) && markSize > 0 && markSize <= .1f &&
            float.IsFinite(tracerBrightness) && tracerBrightness >= .1f && tracerBrightness <= 4 &&
            float.IsFinite(tracerColor.r) && float.IsFinite(tracerColor.g) && float.IsFinite(tracerColor.b) && float.IsFinite(tracerColor.a);
    }
}
