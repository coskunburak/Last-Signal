using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class CombatFeedbackTests
    {
        GameObject root, child, obstacle;
        ZombieHealth health;
        ZombieHitRegion region;
        readonly Vector3 origin = new Vector3(5000, 5000, 5000);
        bool originalGore;
        [SetUp] public void Setup()
        {
            originalGore = ZombieGorePreference.GraphicGoreEnabled;
            root = new GameObject("Receipt fixture"); root.transform.position = origin + Vector3.forward * 5;
            health = root.AddComponent<ZombieHealth>(); AwakeHealth();
            child = new GameObject("Chest"); child.transform.SetParent(root.transform, false); child.layer = 8;
            var box = child.AddComponent<BoxCollider>(); region = child.AddComponent<ZombieHitRegion>();
            region.Configure(health, DamageRegion.Body, 1, box, ZombieBodyPart.Torso);
        }
        void AwakeHealth() => typeof(ZombieHealth).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(health, null);
        [TearDown] public void Cleanup()
        { Object.DestroyImmediate(root); if (obstacle) Object.DestroyImmediate(obstacle); ZombieGorePreference.SetGraphicGoreEnabled(originalGore); }
        WeaponFireResolver.ShotResult Fire(float damage = 10, Vector3? muzzle = null, Vector3? direction = null, float range = 20)
        {
            Physics.SyncTransforms();
            return WeaponFireResolver.Resolve(origin, direction ?? Vector3.forward, muzzle ?? origin, range, .5f, damage, null, ~(1 << 2));
        }
        [Test] public void HitReceiptContainsActualCommittedAuthority()
        {
            var shot = Fire(15);
            Assert.That(shot.Hit && shot.DamageCommitted);
            Assert.That(shot.Receipt.Target, Is.SameAs(health));
            Assert.That(shot.Receipt.Hit.BodyPart, Is.EqualTo(ZombieBodyPart.Torso));
            Assert.That(shot.Receipt.AppliedDamage, Is.EqualTo(15));
            Assert.That(shot.Receipt.Hit.ShotId, Is.EqualTo(shot.ShotId));
            Assert.That(shot.Surface, Is.EqualTo(ImpactSurfaceKind.Flesh));
            Assert.That(health.DamageTransactions, Is.EqualTo(1));
            Assert.That(shot.EndPoint, Is.EqualTo(shot.HitPoint));
        }
        [Test] public void ColliderDiscoveryDoesNotConfirmRejectedDamage()
        {
            health.SetDamageEnabled(false); var shot = Fire();
            Assert.That(shot.Target, Is.Not.Null); Assert.That(shot.Hit);
            Assert.That(shot.DamageCommitted, Is.False); Assert.That(health.DamageTransactions, Is.Zero);
        }
        [Test] public void FatalCommitPublishesBeforeDeathAndDoesNotEmitSurvivingEvent()
        {
            int commits = 0, deaths = 0, survivors = 0;
            health.DamageCommitted += receipt => { commits++; Assert.That(health.CurrentHealth, Is.Zero); Assert.That(receipt.Terminal); Assert.That(deaths, Is.Zero); };
            health.Died += () => deaths++; health.Damaged += _ => survivors++;
            var shot = Fire(150); Fire(150);
            Assert.That(shot.Receipt.AppliedDamage, Is.EqualTo(100));
            Assert.That(commits, Is.EqualTo(1)); Assert.That(deaths, Is.EqualTo(1)); Assert.That(survivors, Is.Zero);
        }
        [Test] public void ReentrantCommittedListenerCannotDuplicateDeath()
        {
            int deaths = 0; health.Died += () => deaths++;
            health.DamageCommitted += receipt => { if (!receipt.Terminal) health.TakeDamage(new DamageInfo { Amount = 1000 }); };
            Fire(10); Assert.That(deaths, Is.EqualTo(1)); Assert.That(health.DamageTransactions, Is.EqualTo(2));
        }
        [Test] public void MissPreservesMuzzleAndMaximumRangeEndpoint()
        {
            var muzzle = origin + Vector3.right * .2f;
            var shot = Fire(muzzle: muzzle, direction: Vector3.up, range: 12);
            Assert.That(shot.Hit || shot.DamageCommitted, Is.False);
            Assert.That(shot.SourcePosition, Is.EqualTo(muzzle)); Assert.That(shot.EndPoint, Is.EqualTo(origin + Vector3.up * 12));
            Assert.That(shot.ShotId, Is.GreaterThan(0));
        }
        [Test] public void AcceptedResolutionsHaveIncreasingIdentity()
        { var a = Fire(); var b = Fire(); Assert.That(b.ShotId, Is.GreaterThan(a.ShotId)); }
        [Test] public void NavigationRootDoesNotBlockChildAndTwoRegionsDoNotMultiplyDamage()
        {
            root.layer = 2; root.AddComponent<BoxCollider>().size = Vector3.one * 2;
            var second = new GameObject("Second region"); second.transform.SetParent(root.transform, false); second.transform.localPosition = Vector3.forward;
            second.layer = 8; var box = second.AddComponent<BoxCollider>();
            second.AddComponent<ZombieHitRegion>().Configure(health, DamageRegion.Head, 2, box, ZombieBodyPart.Head);
            var shot = Fire(); Assert.That(shot.Collider, Is.SameAs(region.HitCollider));
            Assert.That(health.DamageTransactions, Is.EqualTo(1)); Assert.That(health.CurrentHealth, Is.EqualTo(90));
        }
        [Test] public void IncludedRootColliderStillCannotPretendToCommit()
        { root.AddComponent<BoxCollider>().size = Vector3.one * 2; var shot = Fire(); Assert.That(shot.Hit); Assert.That(shot.DamageCommitted, Is.False); }
        [Test] public void MuzzleObstructionStopsAtWallWithNoTargetDamage()
        {
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.transform.position = origin + new Vector3(.4f, 0, .25f);
            obstacle.transform.localScale = new Vector3(.2f, 1, .1f);
            var shot = Fire(muzzle: origin + Vector3.right * .4f);
            Assert.That(shot.MuzzleObstructed && shot.Hit); Assert.That(shot.Collider, Is.SameAs(obstacle.GetComponent<Collider>()));
            Assert.That(shot.EndPoint.z, Is.LessThan(origin.z + .4f)); Assert.That(health.DamageTransactions, Is.Zero);
        }
        [Test] public void ClippedBarrelCannotShootThroughWall()
        {
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.transform.position = origin + Vector3.forward * .3f;
            obstacle.transform.localScale = new Vector3(2, 2, .1f);
            var shot = Fire(muzzle: origin + Vector3.forward * .6f);
            Assert.That(shot.MuzzleObstructed); Assert.That(shot.Collider, Is.SameAs(obstacle.GetComponent<Collider>())); Assert.That(shot.DamageCommitted, Is.False);
        }
        [TestCase(ZombieBodyPart.Head, DamageRegion.Head, 2)]
        [TestCase(ZombieBodyPart.LeftArm, DamageRegion.Body, 1)]
        [TestCase(ZombieBodyPart.RightArm, DamageRegion.Body, 1)]
        public void AnatomicalAttributionAndMultiplierAreInReceipt(ZombieBodyPart part, DamageRegion type, float multiplier)
        {
            region.Configure(health, type, multiplier, region.HitCollider, part);
            var shot = Fire(); Assert.That(shot.Receipt.Hit.BodyPart, Is.EqualTo(part)); Assert.That(shot.Receipt.AppliedDamage, Is.EqualTo(10 * multiplier));
            Assert.That(shot.Surface, Is.EqualTo(type == DamageRegion.Head ? ImpactSurfaceKind.Bone : ImpactSurfaceKind.Flesh));
        }
        [TestCase(false)] [TestCase(true)] public void GatedSeverPreservesHealthDependenciesAndRestore(bool gore)
        {
            ZombieGorePreference.SetGraphicGoreEnabled(gore);
            var sever = root.AddComponent<ZombieDismemberment>();
            sever.Configure(new[] { new ZombieDismemberment.PartBinding { part = ZombieBodyPart.RightArm, severThreshold = 20,
                visualSeverApproved = false, hitColliders = new[] { region.HitCollider }, detachAnchor = child.transform } }, 40);
            AwakeHealth(); region.Configure(health, DamageRegion.Body, 1, region.HitCollider, ZombieBodyPart.RightArm);
            Assert.That(Fire().Receipt.Severed, Is.False);
            var shot = Fire(); Assert.That(shot.Receipt.Severed); Assert.That(health.CurrentHealth, Is.EqualTo(80));
            Assert.That(sever.CanUseRightArmAttack, Is.False); Assert.That(sever.IsSevered(ZombieBodyPart.RightHand));
            var state = sever.CaptureState(); sever.ResetState(); sever.RestoreState(state);
            Assert.That(region.HitCollider.enabled, Is.False); Assert.That(sever.CanUseRightArmAttack, Is.False);
        }
        [TestCase(float.NaN)] [TestCase(0)] [TestCase(-1)] public void InvalidDamageHasNoReceipt(float damage)
        { Assert.That(Fire(damage).DamageCommitted, Is.False); }
        [TestCase(Audio.AudioSurfaceKind.Metal, ImpactSurfaceKind.Metal)]
        [TestCase(Audio.AudioSurfaceKind.Wood, ImpactSurfaceKind.Wood)]
        [TestCase(Audio.AudioSurfaceKind.Stone, ImpactSurfaceKind.Stone)]
        [TestCase(Audio.AudioSurfaceKind.Dirt, ImpactSurfaceKind.Soil)]
        public void ExistingSurfaceTagsClassifyWithoutAssetLookup(Audio.AudioSurfaceKind source, ImpactSurfaceKind expected)
        { obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.AddComponent<Audio.AudioSurface>().surface = source; Assert.That(ImpactSurface.Resolve(obstacle.GetComponent<Collider>()), Is.EqualTo(expected)); }
        [Test] public void MissingProfileSafelyRejectsAndProductionProfileHasBoundedBudgets()
        {
            var empty = ScriptableObject.CreateInstance<CombatFeedbackProfile>();
            try { Assert.That(empty.IsValid, Is.False); } finally { Object.DestroyImmediate(empty); }
            var profile = AssetDatabase.LoadAssetAtPath<CombatFeedbackProfile>("Assets/LastSignal/Data/VFX/CombatFeedback.asset");
            Assert.That(profile.IsValid); Assert.That(profile.tracerCapacity, Is.LessThanOrEqualTo(16)); Assert.That(profile.markCapacity, Is.LessThanOrEqualTo(64));
        }
        [Test] public void SurfaceSupportRejectsAQuadOverTheEdge()
        {
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.transform.position = origin;
            Physics.SyncTransforms(); var collider = obstacle.GetComponent<Collider>(); var normal = Vector3.back;
            Assert.That(BallisticFeedbackPool.Supported(collider, origin + Vector3.back * .5f, normal, Quaternion.LookRotation(-normal), .1f));
            Assert.That(BallisticFeedbackPool.Supported(collider, origin + new Vector3(.49f, 0, -.5f), normal, Quaternion.LookRotation(-normal), .2f), Is.False);
        }
        [Test] public void ProductionImpactAudioHasVariantsAndPreservesVoiceBudget()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<Audio.AudioCatalog>("Assets/LastSignal/Audio/Config/S015AudioCatalog.asset");
            for (int cue = (int)Audio.AudioCue.ImpactDefault; cue <= (int)Audio.AudioCue.ImpactSever; cue++)
            {
                var entry = catalog.Get((Audio.AudioCue)cue); Assert.That(entry, Is.Not.Null);
                Assert.That(entry.clips.Length, Is.GreaterThan(1)); foreach (var clip in entry.clips) Assert.That(clip, Is.Not.Null);
                Assert.That(entry.spatial); Assert.That(entry.loop, Is.False); Assert.That(entry.range, Is.LessThanOrEqualTo(40));
            }
            Assert.That(Audio.ProductionAudio.VoiceBudget, Is.EqualTo(16));
        }
        [Test] public void ProductionCutsRemainVisuallyGatedUntilArtApproval()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab");
            var sever = prefab.GetComponent<ZombieDismemberment>();
            foreach (var part in new[] { ZombieBodyPart.Head, ZombieBodyPart.LeftArm, ZombieBodyPart.RightArm, ZombieBodyPart.LeftHand, ZombieBodyPart.RightHand })
                Assert.That(sever.IsVisualSeverApproved(part), Is.False, part.ToString());
        }
    }
}
