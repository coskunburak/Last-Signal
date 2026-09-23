using UnityEngine;
using UnityEngine.UI;
namespace LastSignal.WorldTime
{
    [DisallowMultipleComponent] public sealed class WorldTimePresentation : MonoBehaviour
    {
        [SerializeField] Light sun;
        [SerializeField] Text status;
        [SerializeField] ParticleSystem rain;
        [SerializeField] AnimationCurve sunIntensity = new AnimationCurve(new Keyframe(0,.08f),new Keyframe(.25f,.15f),new Keyframe(.5f,1.2f),new Keyframe(.75f,.15f),new Keyframe(1,.08f));
        WorldClock clock;
        float refreshRemaining;
        Color originalAmbient;
        float originalAmbientIntensity, originalSunIntensity;
        Quaternion originalSunRotation;
        bool bound;
        string displayed;
        public Light Sun => sun;
        public ParticleSystem Rain => rain;
        public void Configure(Light light, Text label, ParticleSystem particles) { sun = light; status = label; rain = particles; }
        public void Bind(WorldClock source)
        {
            Unbind(); clock = source; bound = true;
            originalAmbient = RenderSettings.ambientLight; originalAmbientIntensity = RenderSettings.ambientIntensity;
            if (sun) { originalSunIntensity = sun.intensity; originalSunRotation = sun.transform.rotation; }
            Refresh();
        }
        public void Unbind()
        {
            if (bound)
            {
                RenderSettings.ambientLight = originalAmbient; RenderSettings.ambientIntensity = originalAmbientIntensity;
                if (sun) { sun.intensity = originalSunIntensity; sun.transform.rotation = originalSunRotation; }
            }
            if (rain) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (status) status.text = "";
            clock = null; displayed = null; bound = false;
        }
        void OnDisable() => Unbind();
        void LateUpdate()
        {
            if (!bound || !clock || clock.Simulation == null) return;
            PresentEnvironment();
            refreshRemaining -= Time.unscaledDeltaTime;
            if (refreshRemaining <= 0) { refreshRemaining = .25f; PresentText(); }
        }
        public void Refresh() { if (!clock || clock.Simulation == null) return; PresentEnvironment(); PresentText(); }
        void PresentEnvironment()
        {
            float day = (float)(clock.Simulation.TimeOfDay / 86400);
            float daylight = Mathf.Clamp01(Mathf.Sin((day - .25f) * Mathf.PI * 2));
            if (sun) { sun.transform.rotation = Quaternion.Euler(day * 360 - 90, -30, 0); sun.intensity = Mathf.Max(.06f, sunIntensity.Evaluate(day)); }
            RenderSettings.ambientLight = Color.Lerp(new Color(.15f,.19f,.28f),new Color(.65f,.70f,.78f),daylight);
            RenderSettings.ambientIntensity = Mathf.Lerp(.35f,1,daylight);
            if (rain && clock.Flow.Player)
            {
                rain.transform.position = clock.Flow.Player.transform.position + Vector3.up * 8;
                var emission = rain.emission; emission.rateOverTime = clock.ProtectedFromRain ? 0 : (float)clock.Simulation.RainPresentation * 700;
                if (clock.Simulation.RainPresentation > 0 && !rain.isPlaying) rain.Play();
                else if (clock.Simulation.RainPresentation <= 0 && rain.isPlaying) rain.Stop();
            }
        }
        void PresentText()
        {
            if (!status) return;
            var s = clock.Simulation; int minute = (int)(s.TimeOfDay / 60);
            string value = $"DAY {s.Day}   {minute/60:00}:{minute%60:00}   {(s.Raining ? "RAIN" : "CLEAR")}   {(clock.ProtectedFromRain ? "UNDER ROOF" : "EXPOSED")}\n" +
                $"Wet {(int)(s.Wetness*100)}% • Rest healing {(int)(s.RecoveryMultiplier*100)}% — Dry under shelter.\n{clock.Feedback}";
            if (value != displayed) { status.text = value; displayed = value; }
        }
    }
}
