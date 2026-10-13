using LastSignal.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class CharacterWeaponPoseTests
    {
        [TestCase(-25f)] [TestCase(0f)] [TestCase(25f)]
        public void RifleTorsoTargetsSameHeadingAcrossLocomotionClipYaw(float clipYaw)
        {
            var actor = Quaternion.Euler(0, 73, 0);
            var frame = actor * Quaternion.Euler(0, clipYaw, 0);
            float correction = CharacterWeaponRig.CalculateTorsoYawCorrection(frame, actor, 30, 1);
            var posed = Quaternion.AngleAxis(correction, actor * Vector3.up) * frame;
            Assert.That(Quaternion.Angle(posed, actor * Quaternion.Euler(0, 30, 0)), Is.LessThan(.05f));
            Assert.That(CharacterWeaponRig.CalculateTorsoYawCorrection(frame, actor, 30, 0), Is.Zero);
            Assert.That(CharacterWeaponRig.CalculateTorsoYawCorrection(frame, actor, 30, .5f), Is.EqualTo(correction * .5f).Within(.001f));
        }
        [Test] public void RifleTorsoHeadingCorrectionRemainsAnatomicallyBounded()
        {
            float correction = CharacterWeaponRig.CalculateTorsoYawCorrection(Quaternion.Euler(0, 180, 0), Quaternion.identity, 30, 1);
            Assert.That(Mathf.Abs(correction), Is.LessThanOrEqualTo(60));
            Assert.That(CharacterWeaponRig.CalculateTorsoYawCorrection(Quaternion.Euler(90, 0, 0), Quaternion.identity, 30, 1), Is.Zero);
        }
        [TestCase(.19f, .23f, .3f)]
        [TestCase(.19f, .23f, 1f)]
        [TestCase(.19f, .23f, 0f)]
        [TestCase(.4f, .01f, .2f)]
        public void SolverPreservesBothBoneLengthsAndReportsUnreachableContact(float a, float b, float distance)
        {
            Vector3 origin = new Vector3(5, 2, -3), target = origin + Vector3.forward * distance;
            var solved = CharacterArmIK.Solve(origin, target, a, b, origin + Vector3.down,
                Vector3.down, Vector3.zero, .97f, Mathf.PI);
            Assert.That(solved.Valid, Is.True);
            Assert.That(Vector3.Distance(origin, solved.Elbow), Is.EqualTo(a).Within(.0001f));
            Assert.That(Vector3.Distance(solved.Elbow, solved.Wrist), Is.EqualTo(b).Within(.0001f));
            Assert.That(solved.Residual, Is.EqualTo(Vector3.Distance(target, solved.Wrist)).Within(.00001f));
            Assert.That(float.IsFinite(solved.Elbow.x) && float.IsFinite(solved.Elbow.y) && float.IsFinite(solved.Elbow.z), Is.True);
            if (distance > a + b) Assert.That(solved.Residual, Is.GreaterThan(0));
        }
        [Test] public void CollinearHintRetainsPlaneAndOppositeHintTurnsContinuously()
        {
            var previous = Vector3.down;
            for (int i = 0; i < 20; i++)
            {
                Vector3 hint = Vector3.forward + Vector3.up * (i % 2 == 0 ? .00001f : -.00001f);
                var solved = CharacterArmIK.Solve(Vector3.zero, Vector3.forward * .3f, .19f, .23f,
                    hint, Vector3.down, previous, .97f, .05f);
                Assert.That(Vector3.Angle(previous, solved.Bend), Is.LessThan(.01f));
                previous = solved.Bend;
            }
            var turn = CharacterArmIK.Solve(Vector3.zero, Vector3.forward * .3f, .19f, .23f,
                Vector3.up, Vector3.down, previous, .97f, .05f);
            Assert.That(Vector3.Angle(previous, turn.Bend), Is.InRange(2.8f, 2.9f));
            var paused = CharacterArmIK.Solve(Vector3.zero, Vector3.forward * .3f, .19f, .23f,
                Vector3.up, Vector3.down, previous, .97f, 0);
            Assert.That(Vector3.Angle(previous, paused.Bend), Is.LessThan(.01f));
        }
        [Test] public void BendRateIsIndependentOfStepPartition()
        {
            Vector3 slow = Vector3.down, fast = Vector3.down;
            for (int i = 0; i < 10; i++) slow = Turn(slow, .1f);
            for (int i = 0; i < 100; i++) fast = Turn(fast, .01f);
            Assert.That(Vector3.Angle(slow, fast), Is.LessThan(.05f));
        }
        static Vector3 Turn(Vector3 previous, float dt) => CharacterArmIK.Solve(Vector3.zero,
            Vector3.forward * .3f, .19f, .23f, Vector3.right, Vector3.down, previous, .97f, dt).Bend;

        [Test] public void ForearmSharesBoundedWristRollWithoutChangingReachDirection()
        {
            Vector3 axis = new Vector3(.2f, -.4f, .6f).normalized;
            var desired = Quaternion.AngleAxis(110, axis);
            var twist = CharacterArmIK.ForearmTwist(Quaternion.identity, desired, axis, 60);
            Assert.That(Quaternion.Angle(Quaternion.identity, twist), Is.EqualTo(60).Within(.05f));
            Assert.That(Vector3.Distance(twist * axis, axis), Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(twist, desired), Is.LessThan(Quaternion.Angle(Quaternion.identity, desired)));
            var swing = CharacterArmIK.ForearmTwist(Quaternion.identity, Quaternion.AngleAxis(180, Vector3.up), Vector3.forward, 60);
            Assert.That(Quaternion.Angle(Quaternion.identity, swing), Is.LessThan(.01f));
        }
        [Test] public void ShoulderAssistanceIsBoundedAndOnlyActsNearReachLimit()
        {
            Vector3 shoulder = Vector3.zero, upper = Vector3.right * .16f;
            var near = CharacterArmIK.ReachAssistance(shoulder, upper, upper + Vector3.forward * .2f, .42f, 8, 1);
            Assert.That(Quaternion.Angle(Quaternion.identity, near), Is.LessThan(.01f));
            var far = CharacterArmIK.ReachAssistance(shoulder, upper, Vector3.forward, .42f, 8, 1);
            Assert.That(Quaternion.Angle(Quaternion.identity, far), Is.EqualTo(8).Within(.01f));
            var zero = CharacterArmIK.ReachAssistance(shoulder, upper, Vector3.forward, .42f, 8, 0);
            Assert.That(Quaternion.Angle(Quaternion.identity, zero), Is.LessThan(.01f));
        }
        [Test] public void InvalidSegmentAndNonFiniteTargetNeverProduceValidPose()
        {
            Assert.That(CharacterArmIK.Solve(Vector3.zero, Vector3.one, 0, .2f, Vector3.down,
                Vector3.down, Vector3.zero, .97f, 1).Valid, Is.False);
            Assert.That(CharacterArmIK.Solve(Vector3.zero, new Vector3(float.NaN, 0, 0), .2f, .2f,
                Vector3.down, Vector3.down, Vector3.zero, .97f, 1).Valid, Is.False);
        }
        [TestCase(.5f)] [TestCase(1f)] [TestCase(2f)]
        public void StockAnchoredTorsoAssistanceRecoversReachWithoutChangingAimOrBoneLengths(float scale)
        {
            Vector3 upper = new Vector3(-.2f, 1.4f, 0) * scale;
            Vector3 stock = upper + Vector3.right * (.25f * scale);
            Vector3 wristOffset = Vector3.forward * (.4f * scale);
            float reach = .44f * scale;
            float yaw = CharacterArmIK.StockReachYaw(stock - upper, wristOffset, Vector3.up, reach, 12);
            Assert.That(yaw, Is.InRange(0.1f, 12));
            Vector3 pivot = Vector3.up * scale;
            var turn = Quaternion.AngleAxis(yaw, Vector3.up);
            Vector3 movedStock = pivot + turn * (stock - pivot);
            Vector3 movedUpper = pivot + turn * (upper - pivot);
            Vector3 target = movedStock + wristOffset;
            Assert.That(Vector3.Distance(movedUpper, target), Is.EqualTo(reach).Within(.0001f * scale));
            Assert.That(Vector3.Distance(movedUpper, movedStock), Is.EqualTo(Vector3.Distance(upper, stock)).Within(.0001f));
            var solved = CharacterArmIK.Solve(movedUpper, target, .21f * scale, .25f * scale,
                movedUpper + Vector3.down, Vector3.down, Vector3.zero, .97f, Mathf.PI);
            Assert.That(solved.Residual, Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(movedUpper, solved.Elbow), Is.EqualTo(.21f * scale).Within(.0001f));
            Assert.That(Vector3.Distance(solved.Elbow, solved.Wrist), Is.EqualTo(.25f * scale).Within(.0001f));
            Assert.That(CharacterArmIK.StockReachYaw(stock - upper, wristOffset, Vector3.up, scale, 12), Is.Zero);
            Assert.That(CharacterArmIK.StockReachYaw(stock - upper, wristOffset, Vector3.up, reach, 0), Is.Zero);
            float bounded = CharacterArmIK.StockReachYaw(stock - upper, wristOffset, Vector3.up, .2f * scale, 12);
            Assert.That(bounded, Is.EqualTo(12).Within(.001f));
        }
        [TestCase(.5f)] [TestCase(1f)] [TestCase(2f)]
        public void DieselPalmCalibrationRoundTripsWithImportedAxesAndActorScale(float scale)
        {
            var body = PrefabUtility.LoadPrefabContents(DieselCharacterAuthoring.BodyPath);
            try
            {
                body.transform.localScale = Vector3.one * scale;
                var animator = body.GetComponent<Animator>();
                foreach (bool left in new[] { false, true })
                {
                    var palm = new CharacterHandPose(animator, left);
                    Assert.That(palm.Valid, Is.True);
                    var hand = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                    Vector3 contact = new Vector3(.1f, 1.3f, .4f) * scale;
                    Quaternion rotation = Quaternion.Euler(22, 71, left ? -90 : 90);
                    palm.WristPose(contact, rotation, out var position, out var wristRotation);
                    hand.SetPositionAndRotation(position, wristRotation);
                    Assert.That(Vector3.Distance(palm.PalmPosition, contact), Is.LessThan(.0001f));
                    Assert.That(Quaternion.Angle(palm.PalmRotation, rotation), Is.LessThan(.05f));
                    var before = animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal).localRotation;
                    for (int i = 0; i < 50; i++) { palm.Apply(1, !left, .5f); palm.Restore(); }
                    Assert.That(Quaternion.Angle(before, animator.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal).localRotation), Is.LessThan(.01f));
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(body); }
        }
        [Test] public void AuthoredContactsLieOnMeshSurfacesAndRegenerationReusesSockets()
        {
            var profile = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(DieselCharacterAuthoring.Root + "/RiflePose.asset");
            var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(profile.WorldPrefab));
            try
            {
                var sockets = root.GetComponent<CharacterWeaponSockets>();
                var right = sockets.RightGrip; var left = sockets.LeftGrip;
                var rightPosition = right.localPosition; var leftPosition = left.localPosition;
                var stockPosition = sockets.StockContact.localPosition;
                int count = root.GetComponentsInChildren<Transform>(true).Length;
                DieselCharacterAuthoring.ConfigureRifleSockets(sockets);
                DieselCharacterAuthoring.ConfigureRifleSockets(sockets);
                Assert.That(sockets.RightGrip, Is.SameAs(right)); Assert.That(sockets.LeftGrip, Is.SameAs(left));
                Assert.That(root.GetComponentsInChildren<Transform>(true), Has.Length.EqualTo(count));
                Assert.That(right.localPosition, Is.EqualTo(rightPosition)); Assert.That(left.localPosition, Is.EqualTo(leftPosition));
                Assert.That(sockets.StockContact.localPosition, Is.EqualTo(stockPosition));
                var mesh = root.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(SurfaceDistance(mesh, rightPosition, false), Is.LessThan(.001f), "Right contact must touch the actual pistol grip surface.");
                Assert.That(SurfaceDistance(mesh, leftPosition, true), Is.LessThan(.001f), "Left contact must touch the actual foregrip surface.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static float SurfaceDistance(Mesh mesh, Vector3 point, bool left)
        {
            // Intersect triangles with the X ray through the authored contact, independent of socket constants.
            var vertices = mesh.vertices; var indices = mesh.triangles;
            float surface = left ? float.PositiveInfinity : float.NegativeInfinity;
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = vertices[indices[i]]; var b = vertices[indices[i + 1]]; var c = vertices[indices[i + 2]];
                float d = (b.z - c.z) * (a.y - c.y) + (c.y - b.y) * (a.z - c.z);
                if (Mathf.Abs(d) < 1e-9f) continue;
                float u = ((b.z - c.z) * (point.y - c.y) + (c.y - b.y) * (point.z - c.z)) / d;
                float v = ((c.z - a.z) * (point.y - c.y) + (a.y - c.y) * (point.z - c.z)) / d;
                float w = 1 - u - v;
                if (u < 0 || v < 0 || w < 0) continue;
                float x = u * a.x + v * b.x + w * c.x;
                surface = left ? Mathf.Min(surface, x) : Mathf.Max(surface, x);
            }
            return Mathf.Abs(point.x - surface);
        }
        [Test] public void ProfileSerializationPreservesReferencesAndLegacyValues()
        {
            var authored = AssetDatabase.LoadAssetAtPath<CharacterWeaponPoseProfile>(DieselCharacterAuthoring.Root + "/RiflePose.asset");
            var copy = ScriptableObject.CreateInstance<CharacterWeaponPoseProfile>();
            var defaults = ScriptableObject.CreateInstance<CharacterWeaponPoseProfile>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(authored), copy);
                Assert.That(copy.Firearm, Is.SameAs(authored.Firearm)); Assert.That(copy.WorldPrefab, Is.SameAs(authored.WorldPrefab));
                Assert.That(copy.HipStockOffset, Is.EqualTo(authored.HipStockOffset));
                Assert.That(copy.AimStockOffset, Is.EqualTo(authored.AimStockOffset));
                Assert.That(copy.ShoulderReachDegrees, Is.EqualTo(authored.ShoulderReachDegrees));
                Assert.That(copy.TorsoReachYawDegrees, Is.EqualTo(authored.TorsoReachYawDegrees));
                Assert.That(copy.TorsoReachYawDegrees, Is.EqualTo(defaults.TorsoReachYawDegrees));
                Assert.That(copy.HipStockOffset, Is.EqualTo(defaults.HipStockOffset), "New authoring must use the calibrated profile defaults.");
                Assert.That(copy.AimStockOffset, Is.EqualTo(defaults.AimStockOffset));
                Assert.That(copy.TorsoYaw, Is.EqualTo(defaults.TorsoYaw));
                JsonUtility.FromJsonOverwrite("{\"TorsoYaw\":23,\"MaximumReach\":0.95}", defaults);
                Assert.That(defaults.TorsoYaw, Is.EqualTo(23)); Assert.That(defaults.MaximumReach, Is.EqualTo(.95f));
                Assert.That(defaults.ShoulderReachDegrees, Is.InRange(0, 12));
                Assert.That(defaults.ElbowTurnDegreesPerSecond, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(copy); Object.DestroyImmediate(defaults); }
        }
    }
}
