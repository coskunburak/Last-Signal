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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab");
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
            var profile = AssetDatabase.LoadAssetAtPath<ZombieBloodVfxProfile>("Assets/LastSignal/Data/VFX/Blood/ZombieBlood.asset");
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
        [Test] public void ProductionTorsoWoundIsSurfaceSkinnedAndLit()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab");
            var binding = new SerializedObject(prefab.GetComponent<ZombieDismemberment>()).FindProperty("torsoWoundVisual");
            var wound = binding.objectReferenceValue as GameObject;
            Assert.That(wound, Is.Not.Null);
            Assert.That(wound.activeSelf, Is.False);
            var skin = wound.GetComponent<SkinnedMeshRenderer>();
            Assert.That(skin, Is.Not.Null);
            var body = wound.transform.parent.GetComponent<SkinnedMeshRenderer>();
            Assert.That(body, Is.Not.Null);
            Assert.That(skin.bones, Is.EqualTo(body.bones));
            Assert.That(skin.quality, Is.EqualTo(body.quality));
            Assert.That(skin.rootBone, Is.EqualTo(body.rootBone));
            Assert.That(skin.sharedMesh.vertexCount, Is.InRange(80, 160));
            Assert.That(skin.sharedMesh.boneWeights.Length, Is.EqualTo(skin.sharedMesh.vertexCount));
            foreach (var w in skin.sharedMesh.boneWeights)
                Assert.That(w.weight0 + w.weight1 + w.weight2 + w.weight3, Is.EqualTo(1).Within(.001f));
            Assert.That(skin.sharedMesh.bindposes.Length, Is.EqualTo(skin.bones.Length));
            Assert.That(skin.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(skin.sharedMaterial.shader.isSupported, Is.True);
            Assert.That(skin.sharedMaterial.GetTexture("_BaseMap"), Is.Not.Null);
            Assert.That(skin.sharedMaterial.GetFloat("_Metallic"), Is.Zero);
            Assert.That(skin.sharedMaterial.GetFloat("_Smoothness"), Is.InRange(.25f, .5f));
            Assert.That(skin.receiveShadows, Is.True);
        }
        [Test] public void HeadPressureIsStrongPulsedAndFiniteWithoutIncreasingCapacity()
        {
            var p = AssetDatabase.LoadAssetAtPath<ZombieBloodVfxProfile>("Assets/LastSignal/Data/VFX/Blood/ZombieBlood.asset");
            Assert.That(p.IsValid, Is.True);
            Assert.That(p.head.burstCount, Is.EqualTo(32));
            Assert.That(p.head.spurtRate, Is.GreaterThanOrEqualTo(60));
            Assert.That(p.head.spurtSpeed, Is.GreaterThan(p.arm.spurtSpeed));
            Assert.That(p.PressureAt(p.head, .15f), Is.GreaterThan(.95f));
            Assert.That(p.PressureAt(p.head, 1.5f), Is.LessThan(.4f));
            Assert.That(p.PressureAt(p.head, p.head.spurtDuration), Is.Zero);
            Assert.That(ZombieBloodVfxProfile.PulseAt(p.head, .5f / p.head.pulseFrequency), Is.LessThan(.35f));
            Assert.That(ZombieBloodVfxProfile.PulseAt(p.head, 1 / p.head.pulseFrequency), Is.GreaterThan(.95f));
            Assert.That(p.effectCapacity, Is.EqualTo(16)); Assert.That(p.markCapacity, Is.EqualTo(32));
            var copy = Object.Instantiate(p);
            try { copy.head.spurtSpeed = float.NaN; Assert.That(copy.IsValid, Is.False); }
            finally { Object.DestroyImmediate(copy); }
        }
    }
}
