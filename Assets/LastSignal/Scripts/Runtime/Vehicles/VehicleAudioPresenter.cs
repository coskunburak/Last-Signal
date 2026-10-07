using UnityEngine;
using Unity.Profiling;

namespace LastSignal.Vehicles
{
    /// <summary>Presentation only. Muting this component cannot change hearing or pressure.</summary>
    [RequireComponent(typeof(VehicleActor))]
    public sealed class VehicleAudioPresenter : MonoBehaviour
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Vehicle.Audio");
        [SerializeField] AudioClip door, starter, start, idle, stop, horn, impact, trunk;
        [SerializeField] AudioClip loadedEngine, rolling;
        [SerializeField] Light[] headlights = new Light[0];
        VehicleActor actor;
        AudioSource loop, events, loadedLoop, rollingLoop;
        IVehiclePresentationState physics;
        IVehiclePhysicsPort physicsPort;
        readonly AudioSource[] bodyThumps = new AudioSource[3];
        int nextBodyVoice;
        float loadBlend;
        string hudText = "";
        float nextHudRefresh;
        void Awake()
        {
            actor = GetComponent<VehicleActor>(); loop = gameObject.AddComponent<AudioSource>(); events = gameObject.AddComponent<AudioSource>();
            loadedLoop = gameObject.AddComponent<AudioSource>(); rollingLoop = gameObject.AddComponent<AudioSource>();
            for (int i = 0; i < bodyThumps.Length; i++)
            {
                var emitter = new GameObject("Body traversal audio " + i); emitter.transform.SetParent(transform, false);
                var source = bodyThumps[i] = emitter.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = 1;
                source.minDistance = 3; source.maxDistance = 25;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.outputAudioMixerGroup = events.outputAudioMixerGroup;
                emitter.AddComponent<AudioLowPassFilter>().cutoffFrequency = 550;
            }
            physics = GetComponent<IVehiclePresentationState>();
            physicsPort = GetComponent<IVehiclePhysicsPort>();
            foreach (var source in new[] { loop, events, loadedLoop, rollingLoop })
            { source.playOnAwake = false; source.spatialBlend = 1; source.minDistance = 3; source.maxDistance = 60; source.rolloffMode = AudioRolloffMode.Linear; }
            loop.loop = true; loop.clip = idle;
            loadedLoop.loop = rollingLoop.loop = true;
            loadedLoop.clip = loadedEngine; rollingLoop.clip = rolling;
        }
        void OnEnable() { if (!actor) actor = GetComponent<VehicleActor>(); actor.Presented += Play; actor.ZombieImpactCommitted += PlayInfected; actor.BodyFeelPresented += PlayBody; }
        void OnDisable() { actor.Presented -= Play; actor.ZombieImpactCommitted -= PlayInfected; actor.BodyFeelPresented -= PlayBody; StopBodyAudio(); if (loop) loop.Stop(); if (events) events.Stop(); if (loadedLoop) loadedLoop.Stop(); if (rollingLoop) rollingLoop.Stop(); }
        void PlayBody(VehicleBodyCue cue, float strength)
        {
            if (!impact) return;
            var bodyThump = bodyThumps[nextBodyVoice];
            nextBodyVoice = (nextBodyVoice + 1) % bodyThumps.Length;
            bodyThump.Stop(); // Bounded voice count; changing pitch never alters another stage.
            bodyThump.pitch = cue == VehicleBodyCue.Impact ? .8f : cue == VehicleBodyCue.FrontAxle ? .65f : .7f;
            bodyThump.PlayOneShot(impact, strength * (cue == VehicleBodyCue.Impact ? .3f : .45f));
        }
        void StopBodyAudio() { foreach (var source in bodyThumps) if (source) source.Stop(); }
        void PlayInfected(VehicleZombieImpactReceipt receipt)
        { if (impact) events.PlayOneShot(impact, Mathf.Lerp(.25f, 1, receipt.Severity)); }
        void Play(string cue)
        {
            AudioClip clip = cue == "door" ? door : cue == "starter" ? starter : cue == "start" ? start : cue == "stop" ? stop :
                cue == "horn" ? horn : cue == "impact" ? impact : cue == "trunk" ? trunk : null;
            if (clip) events.PlayOneShot(clip);
        }
        void Update()
        {
            using (Marker.Auto())
            {
                if (!actor.Ready) StopBodyAudio();
                bool running = actor.Ready && actor.Resources != null && actor.Resources.EngineRunning;
                float wheelSpeed = physics != null ? physics.WheelSpeed01 : 0;
                float target = running && physics != null ? Mathf.Clamp01(wheelSpeed * .7f + physics.DriveLoad * .3f) : 0;
                loadBlend = Mathf.Lerp(loadBlend, target, 1 - Mathf.Exp(-5 * Time.deltaTime));
                loop.volume = Mathf.Lerp(.65f, .25f, loadBlend); loop.pitch = Mathf.Lerp(.95f, 1.12f, loadBlend);
                loadedLoop.volume = .7f * loadBlend; loadedLoop.pitch = Mathf.Lerp(.8f, 1.35f, wheelSpeed);
                rollingLoop.volume = .25f * wheelSpeed; rollingLoop.pitch = Mathf.Lerp(.85f, 1.15f, wheelSpeed);
                SetLoop(loop, running); SetLoop(loadedLoop, running);
                SetLoop(rollingLoop, actor.Ready && physicsPort != null && physicsPort.IsGrounded && wheelSpeed > .02f);
                foreach (var light in headlights) if (light) light.enabled = actor.LightsOn;
                if (actor.Occupied && actor.Ready && Time.unscaledTime >= nextHudRefresh)
                {
                    nextHudRefresh = Time.unscaledTime + .25f;
                    hudText = $"Pickup | Fuel {actor.Resources.FuelLiters:F1} L | Condition {actor.Resources.Condition:P0}\n" +
                        "W / RT throttle · S / LT brake · R / West reverse · Space / South handbrake\n" +
                        "I / North ignition · E / East exit · H / LS horn · L / D-pad up lights";
                }
            }
        }
        static void SetLoop(AudioSource source, bool play)
        { if (play && source.clip && !source.isPlaying) source.Play(); else if (!play && source.isPlaying) source.Stop(); }
        void OnGUI()
        {
            if (!actor.Occupied || !actor.Ready) return;
            GUI.Box(new Rect(20, Screen.height - 104, 590, 84), hudText);
        }
    }
}
