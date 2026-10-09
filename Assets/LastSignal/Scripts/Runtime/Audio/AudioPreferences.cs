using System;
using UnityEngine;
using UnityEngine.Audio;
namespace LastSignal.Audio
{
    // Same device-preference store as ZombieGorePreference. World saves do not own device volume.
    public sealed class AudioPreferences
    {
        public const float MuteFloorDb = -80;
        public const string Prefix = "LastSignal.Audio.";
        public const int ProfileVersion = 2;
        public const float DefaultFovDegrees = 75;
        public const float DefaultMouseSensitivity = .12f;
        public const float DefaultUiScale = 1;
        public const float DefaultGamepadYaw = 120;
        public const float DefaultGamepadPitch = 90;
        public const float DefaultGamepadDeadzone = .2f;
        public static readonly string[] Parameters = { "MasterVolume", "EffectsVolume", "AmbienceVolume", "VoiceVolume", "MusicVolume" };
        readonly string storagePrefix;
        public AudioPreferences(string storagePrefix=Prefix) { this.storagePrefix=storagePrefix; }
        readonly float[] levels = { .8f, .8f, .65f, .9f, .5f };
        public bool Captions { get; set; } = true;
        public string BindingOverrides { get; set; } = "";
        public int TutorialCompleted { get; set; }
        public int TutorialSkipped { get; set; }
        float fovDegrees = DefaultFovDegrees, mouseSensitivity = DefaultMouseSensitivity, uiScale = DefaultUiScale;
        float gamepadYaw = DefaultGamepadYaw, gamepadPitch = DefaultGamepadPitch, gamepadDeadzone = DefaultGamepadDeadzone;
        public float GamepadYaw { get => gamepadYaw; set => gamepadYaw = ValidRange(value, 30, 300, DefaultGamepadYaw); }
        public float GamepadPitch { get => gamepadPitch; set => gamepadPitch = ValidRange(value, 30, 300, DefaultGamepadPitch); }
        public float GamepadDeadzone { get => gamepadDeadzone; set => gamepadDeadzone = ValidRange(value, .15f, .4f, DefaultGamepadDeadzone); }
        public bool GamepadInvertX { get; set; }
        public bool GamepadInvertY { get; set; }
        public float FovDegrees { get => fovDegrees; set => fovDegrees = ValidRange(value, 60, 100, DefaultFovDegrees); }
        public float MouseSensitivity { get => mouseSensitivity; set => mouseSensitivity = ValidRange(value, .02f, .5f, DefaultMouseSensitivity); }
        public float UiScale { get => uiScale; set => uiScale = ValidRange(value, .8f, 1.3f, DefaultUiScale); }
        public float this[AudioBus bus] { get => levels[(int)bus]; set => levels[(int)bus]=Sanitize(value); }
        static float Sanitize(float v) => float.IsFinite(v) ? Mathf.Clamp01(v) : 0;
        static float ValidRange(float v, float min, float max, float fallback) => float.IsFinite(v) ? Mathf.Clamp(v, min, max) : fallback;
        public static float ToDecibels(float v) => v>0 && float.IsFinite(v) ? Mathf.Max(MuteFloorDb,20*Mathf.Log10(Mathf.Clamp01(v))) : MuteFloorDb;
        public void Load()
        {
            for(int i=0;i<levels.Length;i++) levels[i]=Sanitize(PlayerPrefs.GetFloat(storagePrefix+Parameters[i],levels[i]));
            BindingOverrides=PlayerPrefs.GetString(storagePrefix+"BindingOverrides", "");
            TutorialCompleted=PlayerPrefs.GetInt(storagePrefix+"TutorialCompleted",0) & 7;
            TutorialSkipped=PlayerPrefs.GetInt(storagePrefix+"TutorialSkipped",0) & 7;
            Captions=PlayerPrefs.GetInt(storagePrefix+"Captions",1)!=0;
            // An existing S015 audio profile has no version key. Keep its audio
            // values and initialize the new controls to safe defaults.
            int version = PlayerPrefs.GetInt(storagePrefix+"ProfileVersion",0);
            if (version >= 1 && version <= ProfileVersion)
            {
                FovDegrees=PlayerPrefs.GetFloat(storagePrefix+"FovDegrees",DefaultFovDegrees);
                MouseSensitivity=PlayerPrefs.GetFloat(storagePrefix+"MouseSensitivity",DefaultMouseSensitivity);
                UiScale=PlayerPrefs.GetFloat(storagePrefix+"UiScale",DefaultUiScale);
            }
            else { FovDegrees=DefaultFovDegrees; MouseSensitivity=DefaultMouseSensitivity; UiScale=DefaultUiScale; }
            GamepadYaw = version == ProfileVersion ? PlayerPrefs.GetFloat(storagePrefix+"GamepadYaw", DefaultGamepadYaw) : DefaultGamepadYaw;
            GamepadPitch = version == ProfileVersion ? PlayerPrefs.GetFloat(storagePrefix+"GamepadPitch", DefaultGamepadPitch) : DefaultGamepadPitch;
            GamepadDeadzone = version == ProfileVersion ? PlayerPrefs.GetFloat(storagePrefix+"GamepadDeadzone", DefaultGamepadDeadzone) : DefaultGamepadDeadzone;
            GamepadInvertX = version == ProfileVersion && PlayerPrefs.GetInt(storagePrefix+"GamepadInvertX", 0) != 0;
            GamepadInvertY = version == ProfileVersion && PlayerPrefs.GetInt(storagePrefix+"GamepadInvertY", 0) != 0;
        }
        public void Save()
        {
            for(int i=0;i<levels.Length;i++) PlayerPrefs.SetFloat(storagePrefix+Parameters[i],levels[i]);
            PlayerPrefs.SetString(storagePrefix+"BindingOverrides",BindingOverrides ?? "");
            PlayerPrefs.SetInt(storagePrefix+"TutorialCompleted",TutorialCompleted & 7);
            PlayerPrefs.SetInt(storagePrefix+"TutorialSkipped",TutorialSkipped & 7);
            PlayerPrefs.SetInt(storagePrefix+"Captions",Captions?1:0);
            PlayerPrefs.SetFloat(storagePrefix+"FovDegrees",FovDegrees);
            PlayerPrefs.SetFloat(storagePrefix+"MouseSensitivity",MouseSensitivity);
            PlayerPrefs.SetFloat(storagePrefix+"UiScale",UiScale);
            PlayerPrefs.SetFloat(storagePrefix+"GamepadYaw",GamepadYaw);
            PlayerPrefs.SetFloat(storagePrefix+"GamepadPitch",GamepadPitch);
            PlayerPrefs.SetFloat(storagePrefix+"GamepadDeadzone",GamepadDeadzone);
            PlayerPrefs.SetInt(storagePrefix+"GamepadInvertX",GamepadInvertX ? 1 : 0);
            PlayerPrefs.SetInt(storagePrefix+"GamepadInvertY",GamepadInvertY ? 1 : 0);
            PlayerPrefs.SetInt(storagePrefix+"ProfileVersion",ProfileVersion);
            PlayerPrefs.Save();
        }
        public bool Apply(AudioMixer mixer)
        {
            if(!mixer) return false;
            bool ok=true;
            for(int i=0;i<levels.Length;i++) ok &= mixer.SetFloat(Parameters[i],ToDecibels(levels[i]));
            return ok;
        }
    }
}
