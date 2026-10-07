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
        public static readonly string[] Parameters = { "MasterVolume", "EffectsVolume", "AmbienceVolume", "VoiceVolume", "MusicVolume" };
        readonly string storagePrefix;
        public AudioPreferences(string storagePrefix=Prefix) { this.storagePrefix=storagePrefix; }
        readonly float[] levels = { .8f, .8f, .65f, .9f, .5f };
        public bool Captions { get; set; } = true;
        public float this[AudioBus bus] { get => levels[(int)bus]; set => levels[(int)bus]=Sanitize(value); }
        static float Sanitize(float v) => float.IsFinite(v) ? Mathf.Clamp01(v) : 0;
        public static float ToDecibels(float v) => v>0 && float.IsFinite(v) ? Mathf.Max(MuteFloorDb,20*Mathf.Log10(Mathf.Clamp01(v))) : MuteFloorDb;
        public void Load()
        {
            for(int i=0;i<levels.Length;i++) levels[i]=Sanitize(PlayerPrefs.GetFloat(storagePrefix+Parameters[i],levels[i]));
            Captions=PlayerPrefs.GetInt(storagePrefix+"Captions",1)!=0;
        }
        public void Save()
        {
            for(int i=0;i<levels.Length;i++) PlayerPrefs.SetFloat(storagePrefix+Parameters[i],levels[i]);
            PlayerPrefs.SetInt(storagePrefix+"Captions",Captions?1:0); PlayerPrefs.Save();
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
