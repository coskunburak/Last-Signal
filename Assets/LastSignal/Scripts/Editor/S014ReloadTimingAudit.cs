using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSignal.Editor
{
    /// <summary>Measures imported production poses in an isolated prefab scene; never saves assets.</summary>
    public static class S014ReloadTimingAudit
    {
        const string Prefab = "Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab";
        const string Bone = "LVA4_Armature/root/wpn_body";
        [Serializable] sealed class Report
        {
            public string status = "MEASURED_NOT_VISUALLY_ACCEPTED";
            public string unityVersion, prefab = Prefab;
            public string limitation = "Raw authored clip samples. Animator transition delay, frame scheduling, hand contact and gameplay appearance require runtime video; this is not D135 PASS.";
            public Measurement[] reloads;
        }
        [Serializable] sealed class Measurement
        {
            public string state, clip;
            public float clipSeconds, stateSpeed, gameplaySeconds, commitSeconds, clipTimeAtCommit;
            public float commitPositionErrorMetres, commitAngleErrorDegrees, maximumSeparationMetres;
            public float finalPositionErrorMetres, finalAngleErrorDegrees;
            public int visibleMagazineRenderers;
        }

        [MenuItem("Last Signal/Validation/S014/Audit reload magazine timing")]
        public static void RunMenu()
        {
            string directory = Path.GetFullPath("Docs/Implementation/S014/Evidence/" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmss-fffffffZ") + "-reload-audit");
            Directory.CreateDirectory(directory);
            Measure(directory);
        }

        public static void RunBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-s014Evidence");
            if (index < 0 || index + 1 >= args.Length || !Path.IsPathRooted(args[index + 1]))
                throw new ArgumentException("-s014Evidence requires an absolute, new evidence directory.");
            Measure(args[index + 1]);
        }

        static void Measure(string directory)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Run outside Play Mode.");
            if (!Directory.Exists(directory) || File.Exists(Path.Combine(directory, "reload-audit.json")))
                throw new IOException("Evidence directory must exist and must not contain a previous report.");
            GameObject instance = null;
            try
            {
                instance = PrefabUtility.LoadPrefabContents(Prefab);
                var weapon = instance.GetComponent<WeaponController>();
                var animator = instance.GetComponentInChildren<Animator>(true);
                if (!weapon || !weapon.Definition || !animator)
                    throw new InvalidOperationException("Missing production weapon definition or Animator.");
                var controller = animator.runtimeAnimatorController as AnimatorController;
                if (!controller) throw new InvalidOperationException("Expected production AnimatorController.");
                var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToArray();
                AnimationClip idle = states.Single(s => s.name == "Ready").motion as AnimationClip;
                var body = animator.transform.Find(Bone);
                var magazine = body ? body.Find("mag/MRPoly_Magazine") : null;
                if (!idle || !body || !magazine) throw new InvalidOperationException("Production idle/body/magazine binding missing.");
                animator.enabled = false;
                var report = new Report { unityVersion = Application.unityVersion };
                var measurements = new List<Measurement>();
                foreach (bool empty in new[] { false, true })
                {
                    var state = states.Single(s => s.name == (empty ? "EmptyReload" : "Reload"));
                    var clip = state.motion as AnimationClip;
                    if (!clip || state.speed <= 0 || state.speedParameterActive || state.timeParameterActive)
                        throw new InvalidOperationException("Audit needs a direct clip with constant positive playback speed.");
                    idle.SampleAnimation(animator.gameObject, 0);
                    Vector3 seatedPosition = body.InverseTransformPoint(magazine.position);
                    Quaternion seatedRotation = Quaternion.Inverse(body.rotation) * magazine.rotation;
                    float duration = empty ? weapon.Definition.EmptyReloadSeconds : weapon.Definition.TacticalReloadSeconds;
                    float fraction = empty ? weapon.Definition.EmptyReloadCommitNormalized : weapon.Definition.ReloadCommitNormalized;
                    var m = new Measurement {
                        state = state.name, clip = clip.name, clipSeconds = clip.length, stateSpeed = state.speed,
                        gameplaySeconds = duration, commitSeconds = duration * fraction,
                        clipTimeAtCommit = Mathf.Clamp(duration * fraction * state.speed, 0, clip.length),
                        visibleMagazineRenderers = magazine.GetComponentsInChildren<Renderer>(true).Count(r => r.enabled && r.gameObject.activeInHierarchy)
                    };
                    var csv = new StringBuilder("clip_seconds,ideal_gameplay_seconds,position_error_metres,angle_error_degrees,commit_sample\n");
                    var times = new SortedSet<float> { 0, clip.length, m.clipTimeAtCommit };
                    int samples = Mathf.CeilToInt(clip.length * Mathf.Max(24, clip.frameRate));
                    for (int i = 1; i < samples; i++) times.Add(clip.length * i / samples);
                    foreach (float time in times)
                    {
                        clip.SampleAnimation(animator.gameObject, time);
                        float distance = Vector3.Distance(magazine.position, body.TransformPoint(seatedPosition));
                        float angle = Quaternion.Angle(magazine.rotation, body.rotation * seatedRotation);
                        bool commit = time == m.clipTimeAtCommit;
                        csv.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R},{3:R},{4}",
                            time, time / state.speed, distance, angle, commit));
                        m.maximumSeparationMetres = Mathf.Max(m.maximumSeparationMetres, distance);
                        if (commit) { m.commitPositionErrorMetres = distance; m.commitAngleErrorDegrees = angle; }
                        if (time == clip.length) { m.finalPositionErrorMetres = distance; m.finalAngleErrorDegrees = angle; }
                    }
                    WriteNew(Path.Combine(directory, state.name + ".csv"), csv.ToString());
                    measurements.Add(m);
                }
                report.reloads = measurements.ToArray();
                WriteNew(Path.Combine(directory, "reload-audit.json"), JsonUtility.ToJson(report, true));
                Debug.Log("S014 reload pose audit: " + directory + " (measurement only; visual acceptance pending)");
            }
            finally { if (instance) PrefabUtility.UnloadPrefabContents(instance); }
        }

        static void WriteNew(string path, string contents)
        {
            using (var writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write)))
                writer.Write(contents);
        }
    }
}
