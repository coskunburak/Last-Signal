#if UNITY_EDITOR
using System.Collections;
using LastSignal.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace LastSignal.Tests
{
    public sealed class VehicleZombieDominanceTests
    {
        SessionFlow flow;
        VehicleActor car;
        ZombieController zombie;
        [UnitySetUp] public IEnumerator Setup()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/LastSignal/Scenes/Production/S013Cabin.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            flow = Object.FindAnyObjectByType<SessionFlow>(); flow.Resume();
            car = flow.GetComponent<VehicleWorld>().Actor;
            zombie = Object.FindAnyObjectByType<ZombieEncounter>().Actor;
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
        }
        [Test] public void ProductionMassAndLocomotionAuthorityAreExplicit()
        {
            Assert.AreEqual(1800, car.GetComponent<Rigidbody>().mass);
            Assert.IsFalse(car.GetComponent<Rigidbody>().isKinematic);
            Assert.IsNull(zombie.GetComponent<Rigidbody>());
            Assert.IsNull(zombie.GetComponent<CharacterController>());
            Assert.IsTrue(zombie.GetComponent<NavMeshAgent>().updatePosition);
            Assert.IsFalse(zombie.GetComponentInChildren<Animator>().applyRootMotion);
        }
        [UnityTest] public IEnumerator ZombieCannotPushStationaryPickup()
        {
            var agent = zombie.GetComponent<NavMeshAgent>();
            Assert.IsTrue(agent.Warp(car.transform.TransformPoint(new Vector3(1.5f, 0, 0))));
            Vector3 before = car.transform.position;
            // Exercise the real NavMesh transform authority against the parked production chassis.
            for (int i = 0; i < 150; i++)
            {
                var direction = Vector3.ProjectOnPlane(car.transform.position - zombie.transform.position, Vector3.up).normalized;
                agent.Move(direction * .03f);
                yield return new WaitForFixedUpdate();
            }
            float displacement = Vector3.ProjectOnPlane(car.transform.position - before, Vector3.up).magnitude;
            TestContext.WriteLine($"PARKED displacement_m={displacement}");
            Assert.Less(displacement, .15f, "One walking infected must not shove a parked pickup.");
            Assert.AreEqual(0, car.ZombieImpactCount);
        }
        [UnityTest] public IEnumerator LethalDamageDuringVehicleKnockdownNeverRestoresAI()
        {
            var health = zombie.GetComponent<ZombieHealth>();
            health.TakeDamage(new DamageInfo { Amount = 40, Category = DamageCategory.VehicleImpact,
                Direction = car.transform.forward, BodyPart = ZombieBodyPart.Torso });
            Assert.IsTrue(zombie.VehicleReactionActive);
            Assert.IsFalse(zombie.GetComponent<CapsuleCollider>().enabled);
            Assert.IsFalse(zombie.Attacking);
            Assert.IsTrue(zombie.GetComponent<NavMeshAgent>().isStopped);
            float duration = zombie.ReactionRemaining;
            health.TakeDamage(new DamageInfo { Amount = 1000 });
            yield return new WaitForSeconds(duration + .2f);
            Assert.IsTrue(zombie.IsDead); Assert.IsFalse(zombie.VehicleReactionActive);
            Assert.IsFalse(zombie.GetComponent<NavMeshAgent>().enabled);
            Assert.IsFalse(zombie.GetComponent<ZombieNoiseListener>().Registered);
            Assert.IsFalse(zombie.Attacking);
            foreach (var c in zombie.GetComponentsInChildren<Collider>()) Assert.IsFalse(c.enabled);
        }
        [UnityTest] public IEnumerator ProductionSingleInfectedRetainsMomentumAndCorpseCannotCommitAgain()
        {
            Assert.IsTrue(zombie.GetComponent<NavMeshAgent>().Warp(car.transform.TransformPoint(new Vector3(0, 0, 4))));
            var health = zombie.GetComponent<ZombieHealth>(); int deaths = 0; health.Died += () => deaths++;
            var body = car.GetComponent<Rigidbody>(); Vector3 forward = car.transform.forward;
            body.linearVelocity = forward * 12; Physics.SyncTransforms();
            for (int i = 0; i < 100 && car.ZombieImpactCount == 0; i++) yield return new WaitForFixedUpdate();
            double conditionAfterHit = car.Resources.Condition;
            var noiseAfterHit = flow.Noise.LastTrace.Event;
            float after = Vector3.Dot(body.linearVelocity, forward);
            TestContext.WriteLine($"PRODUCTION momentum_before=12 after={after} ratio={after / 12}");
            Assert.AreEqual(1, car.ZombieImpactCount); Assert.IsTrue(zombie.IsDead);
            Assert.Greater(after, 12 * .7f);
            for (int i = 0; i < 70; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, deaths); Assert.AreEqual(1, health.DamageTransactions);
            Assert.AreEqual(1, car.ZombieImpactCount); Assert.AreEqual(0, car.ImpactContactCount);
            Assert.Greater(Vector3.Dot(car.transform.up, Vector3.up), .9f);
            Assert.AreEqual(1, car.BodyImpactCueCount);
            Assert.AreEqual(1, car.FrontAxleCueCount, "Front axle must traverse the real corpse footprint.");
            Assert.AreEqual(1, car.RearAxleCueCount, "Rear axle must traverse the real corpse footprint.");
            Assert.AreEqual(0, car.PendingBodyPasses);
            Assert.AreEqual(conditionAfterHit, car.Resources.Condition, "Feel cannot charge condition again.");
            Assert.AreEqual(noiseAfterHit, flow.Noise.LastTrace.Event, "Feel cannot emit gameplay noise.");
            Assert.IsTrue(zombie.GetComponent<ZombieAnimationPresenter>().CorpseSettled);
            TestContext.WriteLine($"BODY_FEEL impact={car.BodyImpactCueCount} front={car.FrontAxleCueCount} rear={car.RearAxleCueCount} pending={car.PendingBodyPasses}");
        }
        [UnityTest] public IEnumerator PerformanceCourseGeneratesThreeRealImpacts()
        {
            zombie.SetPaused(true);
            var playerCapsule = flow.Player.GetComponent<CharacterController>();
            playerCapsule.enabled = false;
            flow.Player.transform.position = car.transform.TransformPoint(new Vector3(-2, 0, .15f)) + Vector3.up * .1f;
            playerCapsule.enabled = true; Physics.SyncTransforms();
            Assert.IsTrue(car.TryEnter()); Assert.IsTrue(car.TryIgnition());
            car.SetDevelopmentControl(new VehicleControlIntent(1, 0, 0, false));
            yield return new WaitForSeconds(5);
            Assert.Greater(car.GetComponent<Rigidbody>().linearVelocity.magnitude, 9, "Warmup must reach a meaningful road speed.");
            var fixture = VehicleZombieInspection.Create(flow);
            Assert.AreEqual(5, fixture.ActiveCount);
            int before = car.ZombieImpactCount;
            yield return new WaitForSeconds(VehiclePerformanceCapture.ActiveForwardSeconds);
            car.SetDevelopmentControl(new VehicleControlIntent(0, 1, 0, false));
            yield return new WaitForSeconds(20 - VehiclePerformanceCapture.ActiveForwardSeconds);
            TestContext.WriteLine($"COURSE impacts={car.ZombieImpactCount-before} living_ai={fixture.ActiveCount} contacts={car.ImpactContactCount} {car.DescribeImpactContacts()}");
            Assert.GreaterOrEqual(car.ZombieImpactCount - before, 3);
            Assert.Greater(fixture.ActiveCount, 0);
            Assert.AreEqual(0, car.ImpactContactCount);
            Assert.AreEqual(car.ZombieImpactCount, car.BodyImpactCueCount);
            Assert.Greater(car.FrontAxleCueCount, 0); Assert.Greater(car.RearAxleCueCount, 0);
            Assert.AreEqual(0, car.PendingBodyPasses);
            TestContext.WriteLine($"COURSE_FEEL impact={car.BodyImpactCueCount} front={car.FrontAxleCueCount} rear={car.RearAxleCueCount} pending={car.PendingBodyPasses}");
            Object.Destroy(fixture.gameObject);
        }
        IEnumerator HitForFeel(float speed = 12)
        {
            Assert.IsTrue(zombie.GetComponent<NavMeshAgent>().Warp(car.transform.TransformPoint(new Vector3(0, 0, 4))));
            car.GetComponent<Rigidbody>().linearVelocity = car.transform.forward * speed;
            Physics.SyncTransforms();
            for (int i = 0; i < 100 && car.ZombieImpactCount == 0; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(1, car.ZombieImpactCount);
            Assert.AreEqual(1, car.BodyImpactCueCount);
        }
        [UnityTest] public IEnumerator BodyFeelCancelsOnTargetDespawn()
        {
            yield return HitForFeel();
            int front = car.FrontAxleCueCount, rear = car.RearAxleCueCount;
            zombie.gameObject.SetActive(false);
            yield return new WaitForSeconds(.8f);
            Assert.AreEqual(0, car.PendingBodyPasses);
            Assert.AreEqual(front, car.FrontAxleCueCount); Assert.AreEqual(rear, car.RearAxleCueCount);
        }
        [UnityTest] public IEnumerator BodyFeelSuspendClearsTransientStateWithoutLateCues()
        {
            yield return HitForFeel();
            car.Suspend();
            Assert.AreEqual(0, car.PendingBodyPasses);
            Assert.AreEqual(Vector3.zero, car.BodyFeelPosition);
            Assert.AreEqual(Vector3.zero, car.BodyFeelAngles);
            int front = car.FrontAxleCueCount, rear = car.RearAxleCueCount;
            yield return new WaitForSeconds(.8f);
            Assert.AreEqual(front, car.FrontAxleCueCount); Assert.AreEqual(rear, car.RearAxleCueCount);
        }
        [UnityTest] public IEnumerator BodyFeelDoesNotScheduleTraversalWhenStoppedBeforeAxles()
        {
            yield return HitForFeel();
            car.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            car.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            int front = car.FrontAxleCueCount, rear = car.RearAxleCueCount;
            yield return new WaitForSeconds(4.2f);
            Assert.AreEqual(front, car.FrontAxleCueCount); Assert.AreEqual(rear, car.RearAxleCueCount);
            Assert.AreEqual(0, car.PendingBodyPasses);
        }
        [UnityTest] public IEnumerator BodyFeelNonLethalImpactStillRecoversWithoutDuplicateConsequences()
        {
            yield return HitForFeel(6.5f);
            var health = zombie.GetComponent<ZombieHealth>();
            Assert.IsTrue(health.IsAlive); Assert.IsTrue(zombie.VehicleReactionActive);
            double condition = car.Resources.Condition;
            var noise = flow.Noise.LastTrace.Event;
            yield return new WaitForSeconds(4.5f);
            Assert.IsTrue(health.IsAlive); Assert.IsFalse(zombie.VehicleReactionActive);
            Assert.IsTrue(zombie.GetComponent<CapsuleCollider>().enabled);
            Assert.AreEqual(1, health.DamageTransactions);
            Assert.AreEqual(condition, car.Resources.Condition);
            Assert.AreEqual(noise, flow.Noise.LastTrace.Event);
            Assert.AreEqual(0, car.PendingBodyPasses);
        }
        [UnityTest] public IEnumerator BodyFeelCockpitOffsetIsBoundedAndExitRestoresCamera()
        {
            var look = flow.Player.GetComponent<FirstPersonLook>();
            Vector3 onFoot = look.View.transform.localPosition;
            var capsule = flow.Player.GetComponent<CharacterController>();
            capsule.enabled = false;
            flow.Player.transform.position = car.transform.TransformPoint(new Vector3(-2, 0, .15f)) + Vector3.up * .1f;
            capsule.enabled = true; Physics.SyncTransforms(); Assert.IsTrue(car.TryEnter());
            Vector3 seated = look.View.transform.localPosition;
            yield return HitForFeel();
            // The next Update removes the previous offset before LateUpdate reapplies it.
            // Resume at the following fixed step, after that LateUpdate and before Update.
            yield return null; yield return new WaitForFixedUpdate();
            Assert.Greater((look.View.transform.localPosition - seated).magnitude, .001f,
                $"bodyFeel={car.BodyFeelPosition}, occupied={car.Occupied}, driving={flow.Player.GetComponent<PlayerInputReader>().DrivingActive}");
            Assert.LessOrEqual((look.View.transform.localPosition - seated).magnitude, .0401f);
            car.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            car.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            car.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            // Exit after LateUpdate intentionally exercises offset removal outside actor.Update.
            Assert.IsTrue(car.TryExit());
            yield return null; yield return null;
            Assert.Less((look.View.transform.localPosition - onFoot).magnitude, .0001f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (car) car.SetDevelopmentControl(null);
            if (flow) flow.ReturnToMenu(); Time.timeScale = 1; yield return null;
        }
    }
}
#endif
