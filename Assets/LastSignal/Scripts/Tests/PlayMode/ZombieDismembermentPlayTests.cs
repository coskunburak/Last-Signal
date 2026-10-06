#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombieDismembermentPlayTests
    {
        GameObject actor;
        ZombieHealth health;
        ZombieDismemberment sever;
        bool originalGraphicGore;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab");
            Assert.That(prefab, Is.Not.Null);
            originalGraphicGore = ZombieGorePreference.GraphicGoreEnabled;
            ZombieGorePreference.SetGraphicGoreEnabled(true);
            actor = Object.Instantiate(prefab);
            health = actor.GetComponent<ZombieHealth>();
            sever = actor.GetComponent<ZombieDismemberment>();
            Assert.That(sever, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            ZombieGorePreference.SetGraphicGoreEnabled(originalGraphicGore);
            if (actor) Object.DestroyImmediate(actor);
            var pool = Object.FindAnyObjectByType<ZombieDetachedPartPool>();
            if (pool) Object.DestroyImmediate(pool.gameObject);
        }

        ZombieHitRegion Region(ZombieBodyPart part)
        {
            foreach (var region in actor.GetComponentsInChildren<ZombieHitRegion>(true))
                if (region.BodyPart == part) return region;
            Assert.Fail("Missing hit region: " + part);
            return null;
        }

        SkinnedMeshRenderer Skin(string name)
        {
            foreach (var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skin.name == name) return skin;
            Assert.Fail("Missing body mesh: " + name);
            return null;
        }

        [Test]
        public void HandSeverDisablesOnlyItsMeshAndHitCollider()
        {
            var hand = Region(ZombieBodyPart.LeftHand);
            var other = Region(ZombieBodyPart.RightHand);
            hand.TakeDamage(new DamageInfo { Amount = 35, Direction = Vector3.forward });
            Assert.That(sever.IsSevered(ZombieBodyPart.LeftHand), Is.True);
            Assert.That(hand.HitCollider.enabled, Is.False);
            Assert.That(Skin("HandL").enabled, Is.False);
            Assert.That(other.HitCollider.enabled, Is.True);
            Assert.That(Skin("HandR").enabled, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(65));
            var pool = Object.FindAnyObjectByType<ZombieDetachedPartPool>();
            Assert.That(pool, Is.Not.Null);
            bool visible = false;
            foreach (Transform part in pool.transform) visible |= part.gameObject.activeSelf;
            Assert.That(visible, Is.True);
            ZombieGorePreference.SetGraphicGoreEnabled(false);
            foreach (Transform part in pool.transform) Assert.That(part.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ReducedGoreKeepsLogicalSeverWithoutDetachedPart()
        {
            ZombieGorePreference.SetGraphicGoreEnabled(false);
            Region(ZombieBodyPart.RightArm).TakeDamage(new DamageInfo { Amount = 50 });
            Assert.That(sever.CanUseRightArmAttack, Is.False);
            Assert.That(sever.IsSevered(ZombieBodyPart.RightHand), Is.True);
            Assert.That(Skin("ArmR").enabled, Is.False);
            Assert.That(Skin("HandR").enabled, Is.False);
            Assert.That(Object.FindAnyObjectByType<ZombieDetachedPartPool>(), Is.Null);
            Assert.That(health.CurrentHealth, Is.EqualTo(50));
        }

        [Test]
        public void HeadSeverUsesOneCanonicalDeath()
        {
            int deaths = 0; health.Died += () => deaths++;
            Region(ZombieBodyPart.Head).TakeDamage(new DamageInfo { Amount = 40 });
            Assert.That(sever.IsSevered(ZombieBodyPart.Head), Is.True);
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(Skin("Head").enabled, Is.False);
        }

        [Test]
        public void TorsoWoundTracksDamageSaveAndReducedGore()
        {
            GameObject wound = null;
            foreach (var child in actor.GetComponentsInChildren<Transform>(true))
                if (child.name == "TorsoWound_Visual") wound = child.gameObject;
            Assert.That(wound, Is.Not.Null);
            Assert.That(wound.activeSelf, Is.False);
            Region(ZombieBodyPart.Torso).TakeDamage(new DamageInfo { Amount = 40 });
            Assert.That(sever.TorsoDamaged, Is.True);
            Assert.That(wound.activeSelf, Is.True);
            var saved = sever.CaptureState();
            ZombieGorePreference.SetGraphicGoreEnabled(false);
            Assert.That(wound.activeSelf, Is.False);
            sever.RestoreState(saved);
            Assert.That(wound.activeSelf, Is.False);
            ZombieGorePreference.SetGraphicGoreEnabled(true);
            Assert.That(wound.activeSelf, Is.True);
        }
    }
}
#endif
