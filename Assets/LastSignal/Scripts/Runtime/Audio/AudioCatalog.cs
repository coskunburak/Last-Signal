using System;
using UnityEngine;
using UnityEngine.Audio;

namespace LastSignal.Audio
{
    public enum AudioCue { StepDefault, StepWood, StepDirt, StepGrass, StepGravel, StepMetal, StepStone,
        ZombieIdle, ZombieAlert, ZombieApproach, ZombieTelegraph, ZombieAttack, ZombieHurt, ZombieDeath,
        Fire, DryFire, ReloadStart, MagazineOut, MagazineIn, Bolt, Equip, MeleeSwing, MeleeImpact, Confirm,
        Wind, Rain, HeavyRain, Night, Interior, Exploration, Threat, Relief }
    public enum AudioBus { Master, Effects, Ambience, Voice, Music }
    public enum AudioSurfaceKind { Default, Wood, Dirt, Grass, Gravel, Metal, Stone }
    [Serializable] public sealed class AudioCueEntry
    {
        public AudioCue cue;
        public AudioClip[] clips = Array.Empty<AudioClip>();
        public AudioBus bus = AudioBus.Effects;
        [Range(0,1)] public float gain = .4f;
        [Range(0,256)] public int priority = 128;
        [Min(1)] public float range = 20;
        public bool spatial = true, loop;
        [Range(0,.15f)] public float pitchVariation = .04f;
    }
    [CreateAssetMenu(menuName="Last Signal/Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        public AudioMixer mixer;
        public AudioMixerGroup effects, ambience, voice, music;
        public AudioCueEntry[] cues = Array.Empty<AudioCueEntry>();
        public AudioCueEntry Get(AudioCue cue)
        { for (int i=0;i<cues.Length;i++) if(cues[i]!=null && cues[i].cue==cue) return cues[i]; return null; }
        public AudioMixerGroup Group(AudioBus bus) => bus == AudioBus.Music ? music : bus == AudioBus.Voice ? voice : bus == AudioBus.Ambience ? ambience : effects;
        public static int Variation(int count, int previous, int random)
        {
            if(count<=0) return -1;
            if(count==1) return 0;
            int choices = previous>=0 && previous<count ? count-1 : count;
            int chosen=(int)((uint)random % (uint)choices);
            return choices<count && chosen>=previous ? chosen+1 : chosen;
        }
    }
    /// <summary>Presentation time only; neither this model nor mixer settings can emit gameplay noise.</summary>
    public sealed class AudioTension
    {
        public AudioCue State { get; private set; } = AudioCue.Exploration;
        float expires, reliefUntil;
        public void Signal(float now) { expires=now+4; State=AudioCue.Threat; }
        public void Tick(float now)
        {
            if(State==AudioCue.Threat && now>=expires) { State=AudioCue.Relief; reliefUntil=now+8; }
            else if(State==AudioCue.Relief && now>=reliefUntil) State=AudioCue.Exploration;
        }
        public void Reset() { State=AudioCue.Exploration; expires=reliefUntil=0; }
    }
}
