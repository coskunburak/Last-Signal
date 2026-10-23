using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Editor
{
    // Applies exactly the reviewed candidate, never whichever report happens to be newest.
    public static class FpsPistolFingerAssignment
    {
        const string Evidence = "Docs/Implementation/HDRPProductionOptimization/Evidence/";
        const string CandidatePath = Evidence + "fps-fingers-20261021T2154441670090Z-a4f975158f90492aa54de2b7feb0b7da.json";
        const string CandidateHash = "24c2138c3a02f98d77ce6ffaea6bc0ec0d51bedb4f5c7eda98c9505bc8e99e0e";
        [Serializable] sealed class Guard { public string path, sha256; }
        [Serializable] sealed class Candidate
        {
            public string status, profile, sourcePose;
            public Guard[] guards;
            public Vector3[] after;
            public Quaternion[] gripRelative, triggerRelative;
        }
        [Serializable] sealed class Receipt
        {
            public string status = "STARTED", error, candidate, candidateSha256;
            public string profile, pose, profileSha256, poseSha256, rollback;
            public string runtimeVerification = "NOT_RUN";
        }
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        public static void ApplyReviewedDxt() => Apply("DXT", CandidatePath, CandidateHash, 16);
        public static void ApplyReviewedCustom9mm() => Apply("Custom9mm",
            Evidence + "fps-fingers-20261021T2200010984120Z-b130ca0474c44c7d84ef78cbc6c35a27.json",
            "e13abc2ff5cb968bb329df14fc9afbee2c1a33d7c8a4f236703f809e50985cac", 24);

        static void Apply(string key, string candidatePath, string candidateHash, int expectedGuards)
        {
            string ProfilePath = ArberryCharacterAuthoring.Root + "/Profiles/" + key + ".asset";
            string PosePath = ArberryCharacterAuthoring.Root + "/Profiles/" + key + "_FpsFingers.asset";
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply outside Play mode.");
            if (Hash(candidatePath) != candidateHash) throw new InvalidOperationException("Reviewed candidate changed.");
            var data = JsonUtility.FromJson<Candidate>(File.ReadAllText(candidatePath));
            if (data.status != "CANDIDATE_ONLY_REVIEW_REQUIRED" || data.profile != ProfilePath ||
                data.guards == null || data.guards.Length != expectedGuards || data.after == null || data.after.Length != 15)
                throw new InvalidOperationException("Incomplete or unexpected reviewed report.");
            foreach (var guard in data.guards)
                if (Hash(guard.path) != guard.sha256) throw new InvalidOperationException("Source changed; prepare/review again: " + guard.path);
            if (File.Exists(PosePath) || File.Exists(PosePath + ".meta") || AssetDatabase.LoadMainAssetAtPath(PosePath))
                throw new InvalidOperationException("Destination already exists; refusing overwrite. Review the previous assignment.");
            var profile = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(ProfilePath);
            var source = AssetDatabase.LoadAssetAtPath<PistolFingerPose>(data.sourcePose);
            if (!profile || !source || profile.FpsPistolFingers != source)
                throw new InvalidOperationException("Source FPS reference mismatch.");
            var world = profile.WorldPistolFingers;
            var pose = Object.Instantiate(source);
            pose.name = key + "_FpsFingers";
            pose.Rig = source.Rig + " / FPS primary contact";
            pose.GripRelative = data.gripRelative;
            pose.TriggerRelative = data.triggerRelative;
            pose.AuthoredEndpoints = data.after;
            if (!pose.Valid) { Object.DestroyImmediate(pose); throw new InvalidOperationException("Invalid candidate rotations."); }
            for (int i = 0; i < 15; i++)
            {
                bool unchanged = i < 6 && (pose.GripRelative[i] != source.GripRelative[i] || pose.TriggerRelative[i] != source.TriggerRelative[i]);
                bool unstable = i >= 6 && pose.GripRelative[i] != pose.TriggerRelative[i];
                if (unchanged || unstable) { Object.DestroyImmediate(pose); throw new InvalidOperationException("Unexpected finger changes."); }
            }
            string output = Evidence + "fps-fingers-assignment-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ") + "-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(output);
            var receipt = new Receipt { candidate = candidatePath, candidateSha256 = candidateHash, profile = ProfilePath, pose = PosePath };
            bool saved = false, profileTouched = false;
            byte[] original = File.ReadAllBytes(ProfilePath);
            try
            {
                File.WriteAllBytes(output + "/" + key + ".asset.backup", original);
                File.Copy(ProfilePath + ".meta", output + "/" + key + ".asset.meta.backup");
                File.Copy(candidatePath, output + "/candidate.json");
                AssetDatabase.CreateAsset(pose, PosePath);
                saved = AssetDatabase.Contains(pose);
                if (!saved) throw new InvalidOperationException("Candidate asset creation failed.");
                AssetDatabase.SaveAssetIfDirty(pose);
                profileTouched = true;
                profile.FpsPistolFingers = pose;
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                if (profile.WorldPistolFingers != world) throw new InvalidOperationException("World pose reference changed.");
                const string fpsReference = @"(?m)^  FpsPistolFingers: \{[^\r\n]*\}";
                string beforeText = Encoding.UTF8.GetString(original), afterText = File.ReadAllText(ProfilePath);
                if (Regex.Matches(beforeText, fpsReference).Count != 1 || Regex.Matches(afterText, fpsReference).Count != 1 ||
                    Regex.Replace(beforeText, fpsReference, "FPS_REFERENCE") != Regex.Replace(afterText, fpsReference, "FPS_REFERENCE"))
                    throw new InvalidOperationException("Profile serialization changed fields beyond the FPS reference.");
                // Only the profile bytes and the new asset may change; all other guarded inputs stay identical.
                foreach (var guard in data.guards.Where(g => g.path != ProfilePath))
                    if (Hash(guard.path) != guard.sha256) throw new InvalidOperationException("Protected input changed: " + guard.path);
                receipt.profileSha256 = Hash(ProfilePath);
                receipt.poseSha256 = Hash(PosePath);
                receipt.status = "APPLIED_RUNTIME_VERIFICATION_REQUIRED";
            }
            catch (Exception e)
            {
                receipt.status = "FAILED"; receipt.error = e.ToString();
                try
                {
                    if (profileTouched)
                    {
                        profile.FpsPistolFingers = source;
                        File.WriteAllBytes(ProfilePath, original);
                        AssetDatabase.ImportAsset(ProfilePath, ImportAssetOptions.ForceUpdate);
                    }
                    if (saved && AssetDatabase.LoadAssetAtPath<PistolFingerPose>(PosePath) == pose)
                        if (!AssetDatabase.DeleteAsset(PosePath)) throw new IOException("Could not remove the newly created pose.");
                    receipt.rollback = "Original profile restored; no existing asset deleted.";
                }
                catch (Exception rollbackError) { receipt.rollback = "REVIEW_REQUIRED: " + rollbackError; }
                throw;
            }
            finally
            {
                if (pose && !AssetDatabase.Contains(pose)) Object.DestroyImmediate(pose);
                File.WriteAllText(output + "/assignment.json", JsonUtility.ToJson(receipt, true) + "\n");
                Debug.Log("FPS finger assignment: " + output + " — " + receipt.status);
            }
        }
    }
}
