using System.Collections.Generic;
using LastSignal.Vehicles;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class VehicleExitTests
    {
        readonly List<GameObject> owned = new List<GameObject>();
        readonly Vector3 origin = new Vector3(7000, 0, 7000);
        Vector3[] candidates, doors;
        [SetUp] public void Setup()
        {
            Box(origin + Vector3.down * .1f, new Vector3(12, .2f, 12));
            candidates = new[] { origin + Vector3.left * 2, origin + Vector3.right * 2 };
            doors = new[] { origin + new Vector3(-1, 1, 0), origin + new Vector3(1, 1, 0) };
            Physics.SyncTransforms();
        }
        void Box(Vector3 position, Vector3 size)
        {
            var go = new GameObject("VEH-001 exit fixture"); owned.Add(go); go.transform.position = position;
            go.AddComponent<BoxCollider>().size = size;
        }
        bool Find(System.Func<Vector3, bool> ready, out Vector3 feet)
            => VehicleExitSolver.TryFind(doors, candidates, 1.8f, .3f, 45, 1, ready, out feet);
        [TearDown] public void Cleanup()
        { foreach (var go in owned) Object.DestroyImmediate(go); owned.Clear(); Physics.SyncTransforms(); }
        [Test] public void VEH_INT_003_BlockedLeftSelectsSafeRight()
        {
            Box(candidates[0] + Vector3.up, new Vector3(1, 2, 1)); Physics.SyncTransforms();
            Assert.IsTrue(Find(_ => true, out var feet)); Assert.AreEqual(candidates[1].x, feet.x, .001f);
        }
        [Test] public void VEH_INT_004_AllBlockedRejects()
        {
            foreach (var p in candidates) Box(p + Vector3.up, new Vector3(1, 2, 1)); Physics.SyncTransforms();
            Assert.IsFalse(Find(_ => true, out _));
        }
        [Test] public void EmptyCapsuleBeyondWallCannotTeleportThroughIt()
        {
            Box(origin + new Vector3(-1.5f, 1, 0), new Vector3(.1f, 2, 2)); Physics.SyncTransforms();
            Assert.IsTrue(Find(_ => true, out var feet)); Assert.AreEqual(candidates[1].x, feet.x, .001f);
        }
        [Test] public void UnreadyCellOrMissingPolicyRejects()
        { Assert.IsFalse(Find(_ => false, out _)); Assert.IsFalse(Find(null, out _)); }
        [Test] public void MissingGroundRejects()
        { foreach (var go in owned) go.SetActive(false); Physics.SyncTransforms(); Assert.IsFalse(Find(_ => true, out _)); }
    }
}
