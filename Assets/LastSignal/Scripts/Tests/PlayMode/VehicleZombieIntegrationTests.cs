#if UNITY_EDITOR
using System.Collections;
using LastSignal.Vehicles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class VehicleZombieIntegrationTests
    {
        SessionFlow flow;
        VehicleActor car;
        GameObject target;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Validation/VehiclePlayableAcceptance.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); flow.Resume();
            car = flow.GetComponent<VehicleWorld>().Actor;
            Assert.IsNotNull(car);
            // Isolated lane: retain production colliders/health and actual Rigidbody callbacks.
            car.transform.SetPositionAndRotation(new Vector3(9000, 2, 9000), Quaternion.identity);
            var rb = car.GetComponent<Rigidbody>(); rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            foreach (var wheel in car.GetComponentsInChildren<WheelCollider>()) wheel.enabled = false;
            car.GetComponent<LastSignal.Vehicles.MotionCoreIntegration.MotionCoreVehiclePhysicsAdapter>().enabled = false;
            rb.position = new Vector3(9000, 2, 9000); rb.rotation = Quaternion.identity;
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
        }
        ZombieHealth Spawn(float z)
        {
            target = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab"), new Vector3(9000, 2, 9000 + z), Quaternion.identity);
            // This isolated physics test intentionally does not initialize AI off NavMesh.
            target.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            target.GetComponent<ZombieController>().enabled = false;
            target.GetComponent<ZombieNavigation>().enabled = false;
            Physics.SyncTransforms(); return target.GetComponent<ZombieHealth>();
        }
        IEnumerator Drive(float speed)
        {
            car.GetComponent<Rigidbody>().linearVelocity = Vector3.forward * speed;
            for (int i = 0; i < 70; i++) yield return new WaitForFixedUpdate();
        }
        [UnityTest] public IEnumerator ProductionBodyContactCommitsOnceAndUsesCanonicalDamageNoiseCost()
        {
            var health = Spawn(5); double condition = car.Resources.Condition;
            yield return Drive(8);
            Assert.AreEqual(1, health.DamageTransactions, "real prefab body must hit once");
            Assert.AreEqual(DamageCategory.VehicleImpact, health.LastDamage.Category);
            Assert.Greater(health.CurrentHealth, 0); Assert.Less(health.CurrentHealth, health.MaxHealth);
            Assert.AreEqual(1, car.ZombieImpactCount); Assert.Less(car.Resources.Condition, condition);
            Assert.Greater(car.LastZombieImpact.Direction.z, .9f);
            Assert.AreEqual(LastSignal.Noise.GameplayNoiseCategory.VehicleImpact, flow.Noise.LastTrace.Event.Category);
        }
        [UnityTest] public IEnumerator ReverseContactUsesRearAndCorrectDirection()
        {
            var health = Spawn(-5); yield return Drive(-8);
            Assert.AreEqual(1, health.DamageTransactions, $"closing={car.LastObservedClosingSpeed} pos={car.transform.position} v={car.GetComponent<Rigidbody>().linearVelocity} contacts={car.ImpactContactCount} suppressed={car.ImpactSuppressionCount}"); Assert.Less(car.LastZombieImpact.Direction.z, -.9f);
        }
        [UnityTest] public IEnumerator ParkingContactCannotKillOrSpendCondition()
        {
            var health = Spawn(3.4f); double condition = car.Resources.Condition;
            yield return Drive(1);
            Assert.AreEqual(0, health.DamageTransactions); Assert.AreEqual(condition, car.Resources.Condition);
        }
        [UnityTest] public IEnumerator HighSpeedDeathIsSingleAndDespawnCleansContactLedger()
        {
            var health = Spawn(5); int deaths = 0; health.Died += () => deaths++;
            yield return Drive(18);
            float retained = Vector3.Dot(car.GetComponent<Rigidbody>().linearVelocity, Vector3.forward);
            TestContext.WriteLine($"DOMINANCE before=18 after={retained} ratio={retained / 18f}");
            Assert.Greater(retained, 18f * .7f, "One ordinary infected must not remove most pickup momentum.");
            Assert.IsFalse(health.IsAlive, $"hits={health.DamageTransactions} hp={health.CurrentHealth} closing={car.LastZombieImpact.ClosingSpeed} velocity={car.GetComponent<Rigidbody>().linearVelocity} pos={car.transform.position}"); Assert.AreEqual(1, deaths); Assert.AreEqual(1, health.DamageTransactions);
            Object.Destroy(target); yield return null; yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, car.ImpactContactCount);
            Assert.IsTrue(float.IsFinite(car.GetComponent<Rigidbody>().linearVelocity.sqrMagnitude));
        }
        [UnityTest] public IEnumerator MultipleBodyCollidersAndContinuousContactCommitOnlyOnce()
        {
            var health = Spawn(5);
            for (int i = 0; i < 3; i++)
            {
                var child = new GameObject("Compound physical body"); child.layer = 2;
                child.transform.SetParent(target.transform, false); child.transform.localPosition = new Vector3((i - 1) * .1f, .9f, 0);
                var capsule = child.AddComponent<CapsuleCollider>(); capsule.height = 1.8f; capsule.radius = .32f;
            }
            Physics.SyncTransforms(); yield return Drive(8);
            Assert.AreEqual(1, health.DamageTransactions); Assert.AreEqual(1, car.ZombieImpactCount);
            Assert.Greater(car.ImpactSuppressionCount, 0);
            for (int i = 0; i < 70; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, health.DamageTransactions);
        }
        [UnityTest] public IEnumerator SeparationAllowsSecondImpactAndDeadContactHasNoCost()
        {
            var health = Spawn(5); yield return Drive(8);
            Assert.AreEqual(1, health.DamageTransactions);
            var rb = car.GetComponent<Rigidbody>(); rb.position = new Vector3(9000, 2, 9000); rb.linearVelocity = Vector3.zero;
            Physics.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return new WaitForSeconds(1.1f);
            yield return Drive(8); Assert.AreEqual(2, health.DamageTransactions);
            health.TakeDamage(new DamageInfo { Amount = 1000 }); double condition = car.Resources.Condition;
            int commits = car.ZombieImpactCount;
            rb.position = new Vector3(9000, 2, 9000); rb.linearVelocity = Vector3.zero; Physics.SyncTransforms();
            yield return new WaitForSeconds(1.1f); yield return Drive(18);
            Assert.AreEqual(commits, car.ZombieImpactCount); Assert.AreEqual(condition, car.Resources.Condition);
        }
        [UnityTest] public IEnumerator ActiveProductionEncounterKillPersistsWithConditionAndResidentNoise()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); flow.Resume(); car = flow.GetComponent<VehicleWorld>().Actor;
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            var encounter = Object.FindAnyObjectByType<ZombieEncounter>(); var zombie = encounter.Actor;
            Assert.IsNotNull(zombie); Assert.IsFalse(zombie.IsDead);
            var agent = zombie.GetComponent<UnityEngine.AI.NavMeshAgent>();
            Assert.IsTrue(agent.Warp(car.transform.TransformPoint(new Vector3(0,0,5))));
            var rb = car.GetComponent<Rigidbody>();
            rb.linearVelocity = car.transform.forward * 18; Physics.SyncTransforms();
            for (int i = 0; i < 45; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(zombie.IsDead, $"hits={car.ZombieImpactCount} closing={car.LastObservedClosingSpeed} hp={zombie.GetComponent<ZombieHealth>().CurrentHealth}");
            Assert.AreEqual(DamageCategory.VehicleImpact, zombie.GetComponent<ZombieHealth>().LastDamage.Category);
            Assert.IsTrue(flow.Noise.LastTrace.PressureForwarded);
            rb.linearVelocity = rb.angularVelocity = Vector3.zero;
            yield return new WaitForSeconds(.5f);
            double condition = car.Resources.Condition; Assert.Less(condition, 1);
            var save = flow.GetComponent<LastSignal.Persistence.SaveSession>(); flow.Pause();
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "veh-zmb-" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var result = save.Save(path); Assert.IsTrue(result.Success, result.Message);
                flow.ReturnToMenu(); yield return null; yield return save.Load(path);
                Assert.IsTrue(save.LastResult.Success, save.LastResult.Message);
                car = flow.GetComponent<VehicleWorld>().Actor;
                Assert.AreEqual(condition, car.Resources.Condition, 1e-12);
                Assert.IsTrue(encounter.Actor.IsDead); Assert.IsFalse(encounter.Actor.GetComponent<ZombieHealth>().IsAlive);
            }
            finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); if (System.IO.File.Exists(path + ".bak")) System.IO.File.Delete(path + ".bak"); }
        }
        [UnityTest] public IEnumerator SmallGroupUsesDistinctTargetIdentity()
        {
            var first = Spawn(5); var firstObject = target;
            var second = Spawn(5); target.transform.position += Vector3.right * .55f;
            var secondObject = target;
            var third = Spawn(5); target.transform.position -= Vector3.right * .55f;
            try
            {
                Physics.SyncTransforms(); yield return Drive(8);
                Assert.AreEqual(1, first.DamageTransactions); Assert.AreEqual(1, second.DamageTransactions);
                Assert.AreEqual(1, third.DamageTransactions); Assert.AreEqual(3, car.ZombieImpactCount);
                Assert.IsTrue(float.IsFinite(car.GetComponent<Rigidbody>().linearVelocity.sqrMagnitude));
            }
            finally { Object.Destroy(firstObject); Object.Destroy(secondObject); }
        }
        [UnityTest] public IEnumerator HundredRealContactsReleaseCacheAndListeners()
        {
            int listeners = flow.Noise.ListenerCount, peak = 0;
            long warmMemory = 0, midpointMemory = 0;
            var rb = car.GetComponent<Rigidbody>();
            for (int episode = 0; episode < 100; episode++)
            {
                rb.position = new Vector3(9000, 2, 9000); rb.linearVelocity = Vector3.zero;
                var health = Spawn(3); Physics.SyncTransforms(); yield return new WaitForFixedUpdate();
                rb.linearVelocity = Vector3.forward * 18;
                for (int step = 0; step < 15 && health.IsAlive; step++) yield return new WaitForFixedUpdate();
                Assert.AreEqual(1, health.DamageTransactions, "episode " + episode);
                Assert.IsFalse(health.IsAlive); peak = Mathf.Max(peak, car.ImpactContactCount);
                Object.Destroy(target); yield return null; yield return new WaitForFixedUpdate();
                Assert.AreEqual(0, car.ImpactContactCount);
                if (episode == 9) warmMemory = System.GC.GetTotalMemory(true);
                if (episode == 49) midpointMemory = System.GC.GetTotalMemory(true);
            }
            Assert.AreEqual(100, car.ZombieImpactCount); Assert.AreEqual(listeners, flow.Noise.ListenerCount);
            TestContext.WriteLine($"SOAK impacts={car.ZombieImpactCount}; cache_peak={peak}; cache_end={car.ImpactContactCount}; listeners={listeners}/{flow.Noise.ListenerCount}; managed_warm={warmMemory}; managed_mid={midpointMemory}; managed_end={System.GC.GetTotalMemory(true)}; scope=production-prefab isolated physics, AI disabled; allocations include fixture instantiate/destroy");
        }
        [UnityTest] public IEnumerator ActiveSurvivorReactsAndWitnessHearsThroughCanonicalNoise()
            => SurvivorAndWitnessHearing(true);
        [UnityTest] public IEnumerator ChassisOcclusionRejectsQuietImpactButPreservesDamageAndPressure()
            => SurvivorAndWitnessHearing(false);
        IEnumerator SurvivorAndWitnessHearing(bool openLane)
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); flow.Resume(); car = flow.GetComponent<VehicleWorld>().Actor;
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            var zombie = Object.FindAnyObjectByType<ZombieEncounter>().Actor;
            Assert.IsTrue(zombie.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(car.transform.TransformPoint(new Vector3(0,0,4))));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab");
            // The production AI moves toward the player during the approach. Keep the witness
            // ahead and to the side of the front impact, rather than behind the moving chassis.
            target = Object.Instantiate(prefab, car.transform.TransformPoint(openLane ? new Vector3(2,0,9) : new Vector3(2,0,-2)), Quaternion.identity);
            var witness = target.GetComponent<ZombieController>(); Assert.IsTrue(witness.Initialize()); Assert.IsTrue(witness.Bind(flow.Player));
            var listener = target.GetComponent<ZombieNoiseListener>(); int heard = listener.Heard;
            var rb = car.GetComponent<Rigidbody>(); rb.linearVelocity = car.transform.forward * 8; Physics.SyncTransforms();
            float timeout = Time.time + 2;
            while (car.ZombieImpactCount == 0 && Time.time < timeout) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, car.ZombieImpactCount); Assert.IsFalse(zombie.IsDead);
            Assert.AreEqual(ZombieState.HitReact, zombie.Runtime.State);
            var impactNoise = flow.Noise.LastTrace.Event;
            var hearingOrigin = witness.Perception.Origin;
            var hearingOffset = impactNoise.Position - hearingOrigin;
            bool hearingBlocked = Physics.Raycast(hearingOrigin, hearingOffset.normalized, out var hearingObstacle,
                hearingOffset.magnitude, witness.Definition.OcclusionMask, QueryTriggerInteraction.Ignore);
            float clearStrength = ZombieHearingEvaluator.Strength(impactNoise, hearingOrigin, witness.Definition, false);
            string hearingDiagnostic = $"impact closing={car.LastZombieImpact.ClosingSpeed} severity={car.LastZombieImpact.Severity}; " +
                $"noise={impactNoise.Category} radius={impactNoise.BaseRadiusMeters} intensity={impactNoise.Intensity}; " +
                $"distance={hearingOffset.magnitude} clear={clearStrength} blocked={hearingBlocked} obstacle={(hearingObstacle.collider ? hearingObstacle.collider.name : "none")}; " +
                $"obstacleDistance={hearingObstacle.distance} car={car.transform.position} forward={car.transform.forward}; occludedStrength={clearStrength * witness.Definition.OccludedTransmission} threshold={witness.Definition.HearingThreshold}; " +
                $"registered={listener.Registered} canHear={witness.CanHear} paused={witness.Paused} candidates={listener.Candidates} queries={listener.PhysicsQueries} heard={listener.Heard} accepted={listener.Accepted}; " +
                $"witness={hearingOrigin} source={impactNoise.Position}";
            TestContext.WriteLine(hearingDiagnostic);
            Assert.Greater(clearStrength, witness.Definition.HearingThreshold, hearingDiagnostic);
            if (openLane)
            {
                Assert.IsFalse(hearingBlocked, "Open hearing lane required by this fixture. " + hearingDiagnostic);
                Assert.Greater(listener.Heard, heard, hearingDiagnostic);
                Assert.AreEqual(LastSignal.Noise.GameplayNoiseCategory.VehicleImpact, listener.LastHeard.Event.Category);
            }
            else
            {
                Assert.IsTrue(hearingBlocked, hearingDiagnostic);
                Assert.AreEqual(car.gameObject, hearingObstacle.collider.attachedRigidbody.gameObject, hearingDiagnostic);
                Assert.Less(clearStrength * witness.Definition.OccludedTransmission, witness.Definition.HearingThreshold, hearingDiagnostic);
                Assert.Greater(listener.Candidates, 0, hearingDiagnostic);
                Assert.Greater(listener.PhysicsQueries, 0, hearingDiagnostic);
                Assert.AreEqual(heard, listener.Heard, hearingDiagnostic);
            }
            Assert.IsTrue(flow.Noise.LastTrace.PressureForwarded);
            Assert.Greater(Vector3.Dot(car.transform.up, Vector3.up), .9f);
            Assert.IsTrue(zombie.VehicleReactionActive);
            Assert.IsFalse(zombie.GetComponent<CapsuleCollider>().enabled);
            Assert.IsTrue(zombie.GetComponent<UnityEngine.AI.NavMeshAgent>().isStopped);
            yield return new WaitForSeconds(zombie.ReactionRemaining + .2f);
            Assert.IsFalse(zombie.VehicleReactionActive);
            Assert.IsTrue(zombie.GetComponent<CapsuleCollider>().enabled);
            Assert.IsTrue(zombie.GetComponent<ZombieNavigation>().Ready); Assert.AreNotEqual(ZombieState.HitReact, zombie.Runtime.State);
        }
        [UnityTest] public IEnumerator ServiceColliderDoesNotOwnDamage()
        {
            var health = Spawn(5);
            var service = new GameObject("Interaction-only physical test collider");
            service.transform.SetParent(car.transform, false); service.transform.localPosition = new Vector3(0, 1, 3);
            service.AddComponent<BoxCollider>().size = new Vector3(2, 2, 1);
            Physics.SyncTransforms(); yield return Drive(8);
            Assert.AreEqual(0, health.DamageTransactions); Assert.AreEqual(0, car.ZombieImpactCount);
        }
        [UnityTest] public IEnumerator DisabledActorAndPausedSessionCannotCommitDamage()
        {
            var health = Spawn(5); car.enabled = false; yield return Drive(8);
            Assert.AreEqual(0, health.DamageTransactions); Assert.AreEqual(0, car.ImpactContactCount);
            car.enabled = true; var rb = car.GetComponent<Rigidbody>();
            rb.position = new Vector3(9000,2,9000); rb.linearVelocity = Vector3.zero; Physics.SyncTransforms();
            yield return new WaitForFixedUpdate(); flow.Pause();
            // Allow controlled physics while keeping session authority paused.
            Time.timeScale = 1; yield return Drive(8);
            Assert.AreEqual(0, health.DamageTransactions); Assert.AreEqual(1, car.Resources.Condition);
            flow.Resume(); yield return new WaitForSeconds(.1f);
            Assert.AreEqual(0, health.DamageTransactions);
        }
        [Test] public void TrunkInteractionRayStillFindsServiceAfterContactFilter()
        {
            var trunk = car.transform.Find("TrunkAnchor"); Assert.IsNotNull(trunk);
            Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(trunk.position + Vector3.back, Vector3.forward, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore));
            Assert.AreEqual(trunk.GetComponent<Collider>(), hit.collider);
            Assert.IsNotNull(hit.collider.GetComponent<VehicleServicePoint>());
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (target) Object.Destroy(target); if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null; }
    }
}
#endif
