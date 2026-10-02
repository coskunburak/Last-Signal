#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public class ZombieBloodVfxPlayTests
    {
        GameObject actor;
        ZombieDismemberment sever;
        ZombieBloodVfxPool pool;
        bool original;
        [SetUp] public void SetUp()
        {
            original = ZombieGorePreference.GraphicGoreEnabled;
            ZombieGorePreference.SetGraphicGoreEnabled(true);
            actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/LS_Zombie_Runtime.prefab"));
            sever = actor.GetComponent<ZombieDismemberment>();
            pool = Object.FindAnyObjectByType<ZombieBloodVfxPool>();
            Assert.That(pool, Is.Not.Null);
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(actor);
            if (pool) Object.DestroyImmediate(pool.gameObject);
            var detached = Object.FindAnyObjectByType<ZombieDetachedPartPool>();
            if (detached) Object.DestroyImmediate(detached.gameObject);
            ZombieGorePreference.SetGraphicGoreEnabled(original);
        }
        void Hit(ZombieBodyPart part, float damage)
        {
            foreach (var region in actor.GetComponentsInChildren<ZombieHitRegion>(true))
                if (region.BodyPart == part) { region.TakeDamage(new DamageInfo { Amount = damage, Direction = Vector3.forward }); return; }
            Assert.Fail("Missing region");
        }
        [Test] public void SpawnIsCleanAndGoreToggleClearsAll()
        {
            Assert.That(pool.ActiveEffects, Is.Zero);
            Hit(ZombieBodyPart.LeftHand, 35); Assert.That(pool.ActiveEffects, Is.EqualTo(1));
            ZombieGorePreference.SetGraphicGoreEnabled(false);
            Assert.That(pool.ActiveEffects, Is.Zero); Assert.That(pool.ActiveMarks, Is.Zero);
            ZombieGorePreference.SetGraphicGoreEnabled(true); Assert.That(pool.ActiveEffects, Is.Zero);
        }
        [Test] public void HeadSeverIsSingleAndDeathStillCommits()
        {
            Hit(ZombieBodyPart.Head, 75); Hit(ZombieBodyPart.Head, 75);
            Assert.That(pool.TotalRequests, Is.EqualTo(1)); Assert.That(pool.ActiveEffects, Is.EqualTo(1));
            Assert.That(actor.GetComponent<ZombieHealth>().IsAlive, Is.False);
        }
        [Test] public void ArmSeverCancelsEarlierWristBleeding()
        {
            Hit(ZombieBodyPart.LeftHand, 35); Hit(ZombieBodyPart.LeftArm, 50);
            Assert.That(pool.TotalRequests, Is.EqualTo(2)); Assert.That(pool.ActiveEffects, Is.EqualTo(1));
        }
        [Test] public void DisableAndReuseAreClean()
        {
            Hit(ZombieBodyPart.RightHand, 35); actor.SetActive(false);
            Assert.That(pool.ActiveEffects, Is.Zero); actor.SetActive(true);
            Assert.That(pool.ActiveEffects, Is.Zero); Assert.That(sever.SeveredPartsMask, Is.Zero);
        }
        [Test] public void RestoreClearsTransientBleedingWithoutReplay()
        {
            Hit(ZombieBodyPart.LeftHand, 35); sever.RestoreState(sever.CaptureState());
            Assert.That(pool.ActiveEffects, Is.Zero); Assert.That(pool.TotalRequests, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator SurfaceAndEffectPoolsRemainBoundedAndGoreClearsThem()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var anchor = new GameObject("Owned probe anchor");
            try
            {
                floor.transform.position = new Vector3(10, -.1f, 0); floor.transform.localScale = new Vector3(10, .2f, 10);
                floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/LastSignal/Assets/Zombie/Enemies/Zombie/Materials/M_AcceptanceGround.mat");
                anchor.transform.position = new Vector3(10, 1, 0);
                Physics.SyncTransforms();
                var presenter = actor.GetComponent<ZombieBloodVfxPresenter>();
                int materials = Resources.FindObjectsOfTypeAll<Material>().Length;
                for (int i = 0; i < 80; i++) pool.Play(presenter, anchor.transform, new ZombieSeverPresentation(ZombieBodyPart.Head, anchor.transform.position, Vector3.forward));
                Assert.That(pool.ActiveEffects, Is.EqualTo(presenter.Profile.effectCapacity));
                Assert.That(pool.ActiveMarks, Is.InRange(1, presenter.Profile.markCapacity));
                Assert.That(Resources.FindObjectsOfTypeAll<Material>().Length, Is.EqualTo(materials));
                foreach (Transform child in pool.transform)
                    if (child.name.StartsWith("LS_BloodSurface") && child.gameObject.activeSelf)
                        Assert.That(Vector3.Dot(child.forward, Vector3.up), Is.LessThan(-.99f));
                floor.SetActive(false);
                yield return null;
                Assert.That(pool.ActiveMarks, Is.Zero, "Unloaded support must not leave floating marks");
                ZombieGorePreference.SetGraphicGoreEnabled(false);
                Assert.That(pool.ActiveMarks, Is.Zero); Assert.That(pool.ActiveEffects, Is.Zero);
            }
            finally { Object.DestroyImmediate(anchor); Object.DestroyImmediate(floor); }
        }
        [Test] public void DeathLeaseRequiresTerminalStateAndCellUnloadClearsIt()
        {
            Hit(ZombieBodyPart.Head, 75);
            var controller = actor.GetComponent<ZombieController>();
            var presenter = actor.GetComponent<ZombieBloodVfxPresenter>();
            Assert.That(presenter.RetainDeathPresentation(controller, "blood-test-cell"), Is.False);
            // Fixture does not initialize navigation/AI. Reproduce the controller's committed death state.
            controller.Runtime.Transition(ZombieState.Dead);
            Assert.That(presenter.RetainDeathPresentation(controller, "blood-test-cell"), Is.True);
            Assert.That(pool.RetainedCorpses, Is.EqualTo(1));
            ZombieBloodVfxPool.ReleaseCell("blood-test-cell");
            Assert.That(pool.RetainedCorpses, Is.Zero); Assert.That(actor.activeSelf, Is.False);
            Assert.That(pool.ActiveEffects, Is.Zero);
        }
        [UnityTest] public IEnumerator BleedingExpiresWithinBoundedLifetime()
        {
            Hit(ZombieBodyPart.LeftHand, 35);
            yield return new WaitForSeconds(3.3f);
            Assert.That(pool.ActiveEffects, Is.Zero);
        }
        [UnityTest] public IEnumerator HeadSprayFollowsStumpDecaysAndExpires()
        {
            var presenter = actor.GetComponent<ZombieBloodVfxPresenter>();
            var anchor = new GameObject("Moving stump fixture");
            try
            {
                pool.Play(presenter, anchor.transform, new ZombieSeverPresentation(ZombieBodyPart.Head, Vector3.zero, Vector3.forward));
                ParticleSystem spurt = null, burst = null;
                foreach (var ps in pool.GetComponentsInChildren<ParticleSystem>())
                    if (ps.emission.enabled && !ps.main.loop && ps.emission.burstCount > 0) { burst = ps; break; }
                Assert.That(burst, Is.Not.Null);
                Assert.That(burst.emission.GetBurst(0).count.constant, Is.EqualTo(32));
                Assert.That(burst.main.startSpeed.constant, Is.EqualTo(presenter.Profile.head.burstSpeed));
                foreach (var ps in pool.GetComponentsInChildren<ParticleSystem>())
                    if (ps.emission.enabled && ps.main.loop && ps.main.maxParticles == 48) { spurt = ps; break; }
                Assert.That(spurt, Is.Not.Null);
                Assert.That(spurt.main.startSpeed.constant, Is.EqualTo(presenter.Profile.head.spurtSpeed).Within(.01f));
                anchor.transform.position = new Vector3(2, 3, 4);
                yield return null; yield return null;
                Assert.That(Vector3.Distance(spurt.transform.position, anchor.transform.position), Is.LessThan(.001f));
                Assert.That(spurt.main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.World));
                yield return new WaitForSeconds(1);
                Assert.That(spurt.main.startSpeed.constant, Is.LessThan(presenter.Profile.head.spurtSpeed));
                yield return new WaitForSeconds(presenter.Profile.head.spurtDuration - 1 + .05f);
                Assert.That(spurt.emission.rateOverTime.constant, Is.Zero);
                bool dripping = false;
                foreach (var ps in pool.GetComponentsInChildren<ParticleSystem>())
                    if (ps.emission.enabled && ps.main.maxParticles == 24 && ps.emission.rateOverTime.constant > 0) dripping = true;
                Assert.That(dripping, Is.True);
                yield return new WaitForSeconds(presenter.Profile.head.dripDuration + 1.6f);
                Assert.That(pool.ActiveEffects, Is.Zero);
            }
            finally { Object.DestroyImmediate(anchor); }
        }
        [Test] public void RecycledHeadSlotRestoresHandSpeedAndCone()
        {
            var presenter = actor.GetComponent<ZombieBloodVfxPresenter>();
            for (int i = 0; i < presenter.Profile.effectCapacity; i++)
                pool.Play(presenter, actor.transform, new ZombieSeverPresentation(ZombieBodyPart.Head, Vector3.zero, Vector3.forward));
            pool.ReleaseOwner(presenter);
            pool.Play(presenter, actor.transform, new ZombieSeverPresentation(ZombieBodyPart.LeftHand, Vector3.zero, Vector3.forward));
            int checkedSpurts = 0;
            foreach (var ps in pool.GetComponentsInChildren<ParticleSystem>())
            {
                if (!ps.emission.enabled || !ps.main.loop || ps.main.maxParticles != 48) continue;
                Assert.That(ps.main.startSpeed.constant, Is.EqualTo(presenter.Profile.hand.spurtSpeed));
                Assert.That(ps.shape.angle, Is.EqualTo(presenter.Profile.hand.spurtConeAngle));
                checkedSpurts++;
            }
            Assert.That(checkedSpurts, Is.EqualTo(1)); Assert.That(pool.ActiveEffects, Is.EqualTo(1));
        }
    }
}
#endif
