using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor
{
    // Owner-operated preparation only. No asset, profile or prefab is saved or assigned.
    public static class FpsPistolFingerCandidates
    {
        const string Prefab = ArberryCharacterAuthoring.Root + "/Prefabs/FPS_DXT.prefab";
        [Serializable] sealed class Guard { public string path, sha256; }
        [Serializable] sealed class Report
        {
            public string status = "PREPARING", error, prefab = Prefab, profile, sourcePose;
            public string scope = "DXT FPS only; candidate is unassigned; runtime verification NOT_RUN";
            public Guard[] guards;
            public Vector3[] before, after;
            public float[] planeTiltDegrees = new float[5];
            public Quaternion[] gripRelative, triggerRelative;
        }

        public static void PrepareDxt() => Prepare("DXT", PistolVisualVariant.DxtId);
        public static void PrepareCustom9mm() => Prepare("Custom9mm", PistolVisualVariant.CustomId);

        static void Prepare(string key, string variantId)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Prepare outside Play mode.");
            string output = "Docs/Implementation/HDRPProductionOptimization/Evidence/fps-fingers-" +
                DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N") + ".json";
            var report = new Report { scope = key + " FPS only; candidate is unassigned; runtime verification NOT_RUN" };
            GameObject root = null;
            PistolFingerPose candidate = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(Prefab);
                var grip = root.GetComponent<FirstPersonWeaponGrip>();
                var extraPaths = new List<string>();
                if (variantId != PistolVisualVariant.DxtId)
                {
                    // Reproduce the runtime variant swap on the same production FPS_DXT skeleton.
                    // Standalone legacy FPS prefabs are not the rig used by the failing test.
                    var presenter = root.GetComponent<PistolVariantPresenter>();
                    if (!grip || !presenter || !presenter.ValidCatalog)
                        throw new InvalidOperationException("Missing production pistol variant catalog.");
                    var variant = presenter.Variants.Single(v => v.Id == variantId);
                    var profile = grip.ResolveProfile(variant.Pose);
                    if (!profile || AssetDatabase.GetAssetPath(profile) != ArberryCharacterAuthoring.Root + "/Profiles/" + key + ".asset")
                        throw new InvalidOperationException("Unexpected resolved variant profile.");
                    var visual = Object.Instantiate(variant.VisualPrefab, grip.PoseFrame);
                    var sockets = visual.GetComponent<CharacterWeaponSockets>();
                    CharacterWeaponSockets.Attach(visual.transform, sockets.PrimaryGrip, grip.PoseFrame.position,
                        grip.PoseFrame.rotation * Quaternion.Euler(0, 0, 90));
                    grip.Visual.gameObject.SetActive(false);
                    grip.Profile = profile; grip.Visual = visual.transform; grip.Sockets = sockets;
                    extraPaths.AddRange(new[] { AssetDatabase.GetAssetPath(variant), AssetDatabase.GetAssetPath(variant.Pose),
                        AssetDatabase.GetAssetPath(variant.VisualPrefab), "Assets/LastSignal/Scripts/Runtime/Combat/PistolVariantPresenter.cs" });
                }
                if (!grip || !grip.ValidAuthoring || !grip.Profile.FpsTwoHandedPistol ||
                    !grip.Profile.FpsWeaponOwnsContacts || grip.Sockets.PrimaryGrip.parent != grip.Visual)
                    throw new InvalidOperationException("Unsupported FPS contact contract; no candidate written.");
                var frame = grip.Visual.Find("Frame");
                if (!frame || !frame.GetComponent<MeshFilter>() || !frame.GetComponent<MeshFilter>().sharedMesh)
                    throw new InvalidOperationException("Missing actual " + key + " frame mesh.");
                report.profile = AssetDatabase.GetAssetPath(grip.Profile);
                report.sourcePose = AssetDatabase.GetAssetPath(grip.Profile.FpsPistolFingers);
                var paths = new[] { Prefab, report.profile, report.sourcePose,
                    AssetDatabase.GetAssetPath(frame.GetComponent<MeshFilter>().sharedMesh),
                    "Assets/LastSignal/Scripts/Runtime/Character/FirstPersonWeaponGrip.cs",
                    "Assets/LastSignal/Scripts/Runtime/Character/CharacterHandPose.cs",
                    "Assets/LastSignal/Scripts/Editor/Character/ArberryPresentationAuthoring.cs",
                    "Assets/LastSignal/Scripts/Editor/Character/FpsPistolFingerCandidates.cs" };
                report.guards = paths.Concat(extraPaths).SelectMany(p => new[] { p, p + ".meta" }).Distinct()
                    .Select(p => new Guard { path = p, sha256 = Hash(p) }).ToArray();
                var fingers = grip.RightFingers;
                var rest = grip.RightFingerRest;
                for (int i = 0; i < 15; i++) fingers[i].localRotation = rest[i];
                var palm = new CharacterHandPose(grip.RightHand, fingers, false, grip.RightPalmDepth, false);
                if (!palm.Valid) throw new InvalidOperationException("Invalid FPS palm calibration.");
                var tips = new Vector3[15];
                for (int i = 0; i < 15; i++)
                    tips[i] = fingers[i].childCount > 0
                        ? fingers[i].InverseTransformPoint(fingers[i].GetChild(0).position)
                        : Quaternion.Inverse(rest[i]) * (fingers[i].localPosition * .75f);
                var socket = grip.Sockets.PrimaryGrip;
                palm.WristPose(socket.position + socket.rotation * grip.Profile.FpsPrimaryPalmOffset,
                    socket.rotation, out var wristPosition, out var wristRotation);
                // Temporary preview skeleton only: the production arm solver and its reach limits stay untouched.
                grip.RightHand.SetPositionAndRotation(wristPosition, wristRotation);
                candidate = Object.Instantiate(grip.Profile.FpsPistolFingers);
                for (int i = 0; i < 15; i++) fingers[i].localRotation = rest[i] * candidate.GripRelative[i];
                Vector3[] Endpoints() => Enumerable.Range(0, 15).Select(i =>
                    grip.Visual.InverseTransformPoint(fingers[i].TransformPoint(tips[i]))).ToArray();
                report.before = Endpoints();
                ArberryPresentationAuthoring.FitFingerRoutes(fingers, tips, rest, grip.Visual.gameObject, candidate,
                    allowPlaneTilt: true, selectedPlaneTilts: report.planeTiltDegrees);
                report.after = Endpoints();
                // Exact, unquantized mesh check supplements the fitter's quantized search.
                ValidateGeometry(grip.Visual, frame, fingers, tips);
                if (!candidate.Valid) throw new InvalidOperationException("Invalid candidate rotations.");
                for (int i = 0; i < 6; i++)
                    if (candidate.GripRelative[i] != grip.Profile.FpsPistolFingers.GripRelative[i] ||
                        candidate.TriggerRelative[i] != grip.Profile.FpsPistolFingers.TriggerRelative[i])
                        throw new InvalidOperationException("Thumb/index pose changed unexpectedly.");
                foreach (var guard in report.guards)
                    if (Hash(guard.path) != guard.sha256) throw new InvalidOperationException("Source changed: " + guard.path);
                report.gripRelative = candidate.GripRelative;
                report.triggerRelative = candidate.TriggerRelative;
                report.status = "CANDIDATE_ONLY_REVIEW_REQUIRED";
            }
            catch (Exception e) { report.status = "FAILED_NO_ASSIGNMENT"; report.error = e.ToString(); throw; }
            finally
            {
                if (candidate) Object.DestroyImmediate(candidate);
                if (root) PrefabUtility.UnloadPrefabContents(root);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(report, true) + "\n");
                Debug.Log("FPS finger candidate report: " + output + " — " + report.status);
            }
        }

        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        static void ValidateGeometry(Transform visual, Transform frame, Transform[] fingers, Vector3[] tips)
        {
            var mesh = frame.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices.Select(v => visual.InverseTransformPoint(frame.TransformPoint(v))).ToArray();
            var triangles = mesh.triangles;
            List<float> Section(Vector3 p)
            {
                var xs = new List<float>();
                for (int k = 0; k < triangles.Length; k += 3)
                {
                    var a = vertices[triangles[k]]; var b = vertices[triangles[k + 1]]; var c = vertices[triangles[k + 2]];
                    float by = b.y-a.y, bz = b.z-a.z, cy = c.y-a.y, cz = c.z-a.z, det = by*cz-bz*cy;
                    if (Mathf.Abs(det) < 1e-10f) continue;
                    float py = p.y-a.y, pz = p.z-a.z, u = (py*cz-pz*cy)/det, v = (by*pz-bz*py)/det;
                    if (u >= 0 && v >= 0 && u+v <= 1) xs.Add(a.x+u*(b.x-a.x)+v*(c.x-a.x));
                }
                xs.Sort(); return xs;
            }
            for (int i = 6; i < 15; i++)
            {
                var a = visual.InverseTransformPoint(fingers[i].position);
                var b = visual.InverseTransformPoint(fingers[i].TransformPoint(tips[i]));
                for (int sample = 1; sample <= 20; sample++)
                {
                    var p = Vector3.Lerp(a, b, sample/20f); var xs = Section(p);
                    if (xs.Count >= 2 && xs.Count % 2 == 0 && xs.Count(x => x < p.x) % 2 != 0)
                        throw new InvalidOperationException("Candidate penetrates frame at finger " + i);
                }
                if (i % 3 != 2) continue;
                var section = Section(b);
                if (section.Count < 2 || section.Count % 2 != 0 || section.Last()-section[0] <= .001f ||
                    b.x < section[0]-.014f || b.x > section[0]-.002f)
                    throw new InvalidOperationException("Candidate has no bounded distal contact at finger " + i);
            }
        }
    }
}
