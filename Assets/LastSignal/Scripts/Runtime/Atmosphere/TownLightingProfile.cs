using System;
using UnityEngine;
namespace LastSignal.Atmosphere
{
    /// <summary>Visual calibration sampled from WorldClock by TownAtmosphere; never owns time or weather.</summary>
    [CreateAssetMenu(menuName="Last Signal/Town lighting calibration")]
    public sealed class TownLightingProfile : ScriptableObject
    {
        // Existing profiles keep the previous presentation until explicitly opted in.
        public float sunIntensity = 1.65f;
        public Color dayAmbient = new Color(.34f,.39f,.44f);
        public Color nightAmbient = new Color(.055f,.073f,.105f);
        public Color daySkyFill = new Color(.40f,.48f,.58f);
        public Color nightSkyFill = new Color(.10f,.13f,.21f);
        public float skyFillIntensity = .3f;

        public bool usePhaseLighting;
        [Serializable] public struct Phase
        {
            public Color keyColor, ambient, skyFill, fog;
            public float keyIntensity, moonIntensity, fillStrength, fogDensity, reflection;
            public float exposure, contrast, bloom, shadowStrength, skyDay, skyGold, twilight, exterior;
            public static Phase operator +(Phase a,Phase b)=>new Phase {
                keyColor=a.keyColor+b.keyColor,ambient=a.ambient+b.ambient,skyFill=a.skyFill+b.skyFill,fog=a.fog+b.fog,
                keyIntensity=a.keyIntensity+b.keyIntensity,moonIntensity=a.moonIntensity+b.moonIntensity,fillStrength=a.fillStrength+b.fillStrength,
                fogDensity=a.fogDensity+b.fogDensity,reflection=a.reflection+b.reflection,exposure=a.exposure+b.exposure,
                contrast=a.contrast+b.contrast,bloom=a.bloom+b.bloom,shadowStrength=a.shadowStrength+b.shadowStrength,
                skyDay=a.skyDay+b.skyDay,skyGold=a.skyGold+b.skyGold,twilight=a.twilight+b.twilight,exterior=a.exterior+b.exterior };
            public static Phase operator *(Phase a,float w)=>new Phase {
                keyColor=a.keyColor*w,ambient=a.ambient*w,skyFill=a.skyFill*w,fog=a.fog*w,keyIntensity=a.keyIntensity*w,
                moonIntensity=a.moonIntensity*w,fillStrength=a.fillStrength*w,fogDensity=a.fogDensity*w,reflection=a.reflection*w,
                exposure=a.exposure*w,contrast=a.contrast*w,bloom=a.bloom*w,shadowStrength=a.shadowStrength*w,
                skyDay=a.skyDay*w,skyGold=a.skyGold*w,twilight=a.twilight*w,exterior=a.exterior*w };
        }
        public Phase daylight, sunset, dusk, night;
        public Vector4 skyExposure = new Vector4(.72f,.68f,.58f,.16f);
        public Color twilightTint = new Color(.12f,.20f,.32f);
        [Range(0,1)] public float cloudKeyTransmission=.38f, rainKeyTransmission=.55f, rainAmbientTransmission=.85f;
        public float rainFogAddition=.0023f, cloudFogAddition=.00045f;
        [Range(0,1)] public float opaqueWetCeiling=.58f, puddleWetCeiling=.80f, maximumWetDarkening=.23f;
        public Material roadMaterial;
        [Range(0,1)] public float roadWetSmoothness=.46f;
        public float fixtureEmission=1.5f;
        [Serializable] public struct Practical
        {
            public string fixtureName;
            public Color color;
            public float gain, priority;
            public bool shadows;
            // Day, sunset, blue hour, night. Independent of the light-budget culling decision.
            public Vector4 levels;
        }
        public Practical[] practicals=Array.Empty<Practical>();

        public static float Altitude(float day)=>Mathf.Sin((Mathf.Repeat(day,1)-.25f)*Mathf.PI*2);
        static float Blend(float a,float b,float value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));
        public Vector4 Weights(float day)
        {
            float a=Altitude(day);
            if(a>=.12f){float t=Blend(.12f,.55f,a);return new Vector4(t,1-t,0,0);}
            if(a>=-.12f){float t=Blend(-.12f,.12f,a);return new Vector4(0,t,1-t,0);}
            float n=Blend(-.24f,-.12f,a);return new Vector4(0,0,n,1-n);
        }
        public Phase Evaluate(float day,float clouds,float rain)
        {
            var w=Weights(day);var p=daylight*w.x+sunset*w.y+dusk*w.z+night*w.w;
            clouds=Mathf.Clamp01(clouds);rain=Mathf.Clamp01(rain);
            // Never place a direct solar key below the horizon. The blue-hour fill is indirect.
            p.keyIntensity*=Blend(0,.12f,Altitude(day))*Mathf.Lerp(1,cloudKeyTransmission,clouds)*Mathf.Lerp(1,rainKeyTransmission,rain);
            p.moonIntensity*=Mathf.Lerp(1,.55f,clouds);
            p.ambient*=Mathf.Lerp(1,rainAmbientTransmission,rain);p.skyFill*=Mathf.Lerp(1,.82f,rain);
            p.fogDensity+=clouds*cloudFogAddition+rain*rainFogAddition;
            p.reflection*=Mathf.Lerp(1,.82f,rain);
            return p;
        }
        public bool TryPractical(string name,out Practical result)
        {
            foreach(var p in practicals)if(string.Equals(p.fixtureName,name,StringComparison.Ordinal)){result=p;return true;}
            result=default;return false;
        }
    }
}
