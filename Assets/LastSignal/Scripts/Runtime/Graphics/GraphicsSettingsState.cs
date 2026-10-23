using System;
using UnityEngine;

namespace LastSignal.RenderingQuality
{
    public enum GraphicsPreset { Low, Medium, High, Ultra, Custom }
    public enum ShadowQuality { Off, Low, Medium, High }
    [Serializable] public sealed class GraphicsSettingsState
    {
        public GraphicsPreset preset;
        public ShadowQuality globalShadows, treeShadows;
        public float shadowDistance = 40, lodBias = 1;
        public bool volumetricFog, ambientOcclusion;
        public GraphicsSettingsState Copy() => (GraphicsSettingsState)MemberwiseClone();
        public bool IsValid => Enum.IsDefined(typeof(GraphicsPreset), preset) && Enum.IsDefined(typeof(ShadowQuality), globalShadows) &&
            Enum.IsDefined(typeof(ShadowQuality), treeShadows) && float.IsFinite(shadowDistance) && shadowDistance >= 10 && shadowDistance <= 160 &&
            float.IsFinite(lodBias) && lodBias >= .5f && lodBias <= 1.5f;
        public bool SameValues(GraphicsSettingsState b) => b != null && globalShadows == b.globalShadows && treeShadows == b.treeShadows &&
            shadowDistance == b.shadowDistance && lodBias == b.lodBias && volumetricFog == b.volumetricFog && ambientOcclusion == b.ambientOcclusion;
    }
    [Serializable] public sealed class GraphicsQualityProfiles
    {
        public GraphicsSettingsState[] profiles;
        public float lowTreeDistance = 20, mediumTreeDistance = 40, distanceHysteresis = 3;
        public int lowShadowResolution = 512, mediumShadowResolution = 1024, highShadowResolution = 2048;
        public GraphicsSettingsState Get(GraphicsPreset preset)
        {
            if (preset == GraphicsPreset.Custom || profiles == null) throw new ArgumentException("Named preset required.");
            foreach (var p in profiles) if (p != null && p.preset == preset && p.IsValid) return p.Copy();
            throw new InvalidOperationException("Missing graphics preset: " + preset);
        }
        public static GraphicsQualityProfiles Load()
        {
            var asset = Resources.Load<TextAsset>("Graphics/QualityProfiles");
            if (!asset) throw new InvalidOperationException("Missing Graphics/QualityProfiles resource.");
            var result = JsonUtility.FromJson<GraphicsQualityProfiles>(asset.text);
            foreach (GraphicsPreset p in new[] { GraphicsPreset.Low, GraphicsPreset.Medium, GraphicsPreset.High, GraphicsPreset.Ultra }) result.Get(p);
            if (!(float.IsFinite(result.mediumTreeDistance) && float.IsFinite(result.distanceHysteresis) && result.lowTreeDistance > 0 && result.mediumTreeDistance > result.lowTreeDistance && result.distanceHysteresis >= 0 && result.lowShadowResolution > 0 && result.mediumShadowResolution > result.lowShadowResolution && result.highShadowResolution > result.mediumShadowResolution))
                throw new InvalidOperationException("Invalid tree distance policy.");
            return result;
        }
    }
    public static class GraphicsSettingsPersistence
    {
        public const string Key = "LastSignal.Graphics.v1";
        [Serializable] sealed class Envelope { public int version; public GraphicsSettingsState state; }
        public static string Encode(GraphicsSettingsState state)
        {
            if (state == null || !state.IsValid) throw new ArgumentException("Invalid graphics state.");
            return JsonUtility.ToJson(new Envelope { version = 1, state = state.Copy() });
        }
        public static GraphicsSettingsState Decode(string json, GraphicsQualityProfiles profiles)
        {
            try
            {
                var e = JsonUtility.FromJson<Envelope>(json);
                if (e == null || e.version != 1 || e.state == null || !e.state.IsValid) return profiles.Get(GraphicsPreset.Low);
                if (e.state.preset != GraphicsPreset.Custom && !e.state.SameValues(profiles.Get(e.state.preset))) e.state.preset = GraphicsPreset.Custom;
                return e.state;
            }
            catch (Exception) { return profiles.Get(GraphicsPreset.Low); }
        }
        public static GraphicsSettingsState Load(GraphicsQualityProfiles profiles)
        {
            if (PlayerPrefs.HasKey(Key)) return Decode(PlayerPrefs.GetString(Key), profiles);
            // Migrate the owner's previous opt-in tree toggle without deleting the old key.
            if (PlayerPrefs.HasKey("LastSignal.Forest.TreeShadows"))
            {
                var state = profiles.Get(GraphicsPreset.High);
                state.treeShadows = PlayerPrefs.GetInt("LastSignal.Forest.TreeShadows") == 0 ? ShadowQuality.Off : ShadowQuality.High;
                state.preset = GraphicsPreset.Custom;
                return state;
            }
            return profiles.Get(GraphicsPreset.Low);
        }
        public static void Save(GraphicsSettingsState state) { PlayerPrefs.SetString(Key, Encode(state)); PlayerPrefs.Save(); }
    }
}
