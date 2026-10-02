using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombieBloodVfxTests
    {
        GameObject actor;
        ZombieDismemberment sever;
        ZombieHealth health;
        bool original;
        [SetUp] public void SetUp()
        {
            original = ZombieGorePreference.GraphicGoreEnabled;
            ZombieGorePreference.SetGraphicGoreEnabled(true);
            actor = new GameObject("Blood event fixture");
            health = actor.AddComponent<ZombieHealth>();
            sever = actor.AddComponent<ZombieDismemberment>();
            sever.Configure(new[] { new ZombieDismemberment.PartBinding {
                part = ZombieBodyPart.RightArm, severThreshold = 10, detachAnchor = actor.transform } }, 40);
            typeof(ZombieHealth).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(health, null);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(actor); ZombieGorePreference.SetGraphicGoreEnabled(original); }
        [Test] public void DuplicateSeverAndRestoreDoNotReplay()
        {
            int requests = 0; sever.SeverPresented += _ => requests++;
            health.TakeDamage(new DamageInfo { Amount = 10, BodyPart = ZombieBodyPart.RightArm });
            health.TakeDamage(new DamageInfo { Amount = 10, BodyPart = ZombieBodyPart.RightArm });
            Assert.That(requests, Is.EqualTo(1));
            sever.RestoreState(sever.CaptureState());
            Assert.That(requests, Is.EqualTo(1));
        }
        [Test] public void ReducedGoreSuppressesRequestButKeepsAuthority()
        {
            int requests = 0; sever.SeverPresented += _ => requests++;
            ZombieGorePreference.SetGraphicGoreEnabled(false);
            health.TakeDamage(new DamageInfo { Amount = 10, BodyPart = ZombieBodyPart.RightArm });
            Assert.That(requests, Is.Zero); Assert.That(health.CurrentHealth, Is.EqualTo(90));
            Assert.That(sever.CanUseRightArmAttack, Is.False);
        }
        [Test] public void ResetNotifiesPresentationAndClearsAnatomy()
        {
            int resets = 0; sever.PresentationReset += () => resets++;
            health.TakeDamage(new DamageInfo { Amount = 10, BodyPart = ZombieBodyPart.RightArm });
            sever.ResetState(); Assert.That(resets, Is.EqualTo(1)); Assert.That(sever.SeveredPartsMask, Is.Zero);
        }
        [Test] public void ProductionProfileHasOrderedBudgetsAndExplicitAnchors()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/LS_Zombie_Runtime.prefab");
            var presenter = prefab.GetComponent<ZombieBloodVfxPresenter>();
            Assert.That(presenter, Is.Not.Null);
            var profile = presenter.Profile;
            Assert.That(profile.IsValid, Is.True);
            Assert.That(profile.head.scale, Is.GreaterThan(profile.arm.scale));
            Assert.That(profile.arm.scale, Is.GreaterThan(profile.hand.scale));
            foreach (var part in new[] { ZombieBodyPart.Head, ZombieBodyPart.LeftArm, ZombieBodyPart.RightArm, ZombieBodyPart.LeftHand, ZombieBodyPart.RightHand })
                Assert.That(presenter.AnchorFor(part), Is.Not.Null);
            Assert.That(profile.ForPart(ZombieBodyPart.Torso), Is.Null);
        }
        [Test] public void PresentationExceptionCannotPreventFatalHealthCommit()
        {
            sever.Configure(new[] { new ZombieDismemberment.PartBinding { part = ZombieBodyPart.Head,
                severThreshold = 10, fatalOnSever = true, detachAnchor = actor.transform } }, 40);
            sever.SeverPresented += _ => throw new System.InvalidOperationException("Expected blood presentation failure");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, "InvalidOperationException: Expected blood presentation failure");
            health.TakeDamage(new DamageInfo { Amount = 10, BodyPart = ZombieBodyPart.Head });
            Assert.That(health.IsAlive, Is.False); Assert.That(sever.IsSevered(ZombieBodyPart.Head), Is.True);
        }
        [Test] public void AuthoredPrefabsHaveBoundedParticlesAndSupportedMaterials()
        {
            var profile = AssetDatabase.LoadAssetAtPath<ZombieBloodVfxProfile>("Assets/LastSignal/Blood VFX/ProjectOwned/ZombieBlood.asset");
            foreach (var prefab in new[] { profile.burstPrefab, profile.spurtPrefab, profile.dripPrefab })
                foreach (var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    Assert.That(ps.main.playOnAwake, Is.False); Assert.That(ps.main.maxParticles, Is.LessThanOrEqualTo(48));
                    if (!ps.emission.enabled) continue;
                    var material = ps.GetComponent<ParticleSystemRenderer>().sharedMaterial;
                    Assert.That(material, Is.Not.Null); Assert.That(material.shader.isSupported, Is.True);
                    Assert.That(material.GetFloat("_EmissionMultiply"), Is.Zero);
                }
            var surface = profile.surfacePrefab.GetComponent<Renderer>().sharedMaterial;
            Assert.That(surface.shader.isSupported, Is.True); Assert.That(surface.GetTexture("_BaseMap"), Is.Not.Null);
        }
        [Test] public void InvalidProfileFailsClosed()
        {
            var profile = ScriptableObject.CreateInstance<ZombieBloodVfxProfile>();
            Assert.That(profile.IsValid, Is.False); Object.DestroyImmediate(profile);
        }
    }
}
