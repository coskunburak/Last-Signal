using System;
using System.Linq;
using System.Reflection;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public sealed class ZombieWoundTests
    {
        const string Prefab = "Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab";
        GameObject root, child;
        ZombieHealth health;
        ZombieHitRegion region;
        [SetUp] public void Setup()
        {
            root = new GameObject("Wound authority"); health = root.AddComponent<ZombieHealth>();
            typeof(ZombieHealth).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(health, null);
            child = new GameObject("Region"); child.transform.SetParent(root.transform, false);
            var box = child.AddComponent<BoxCollider>(); region = child.AddComponent<ZombieHitRegion>();
            region.Configure(health, DamageRegion.Body, 1, box, ZombieBodyPart.Torso);
        }
        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(root);
        DamageInfo Hit(ulong id = 1, int sub = 0) => new DamageInfo { Amount = 10, Category = DamageCategory.Bullet,
            ShotId = id, SubHitId = sub, Direction = Vector3.forward, HitNormal = Vector3.back, HitPoint = Vector3.zero, HitCollider = region.HitCollider };
        [Test] public void AcceptedReceiptCarriesOwnerGenerationAndCoordinates()
        {
            var r = region.ReceiveDamage(Hit()); Assert.That(r.Committed && r.PresentationAccepted);
            Assert.That(r.CorpseImpact, Is.False); Assert.That(r.Target, Is.SameAs(health));
            Assert.That(r.Generation, Is.EqualTo(health.LifetimeGeneration)); Assert.That(r.Hit.BodyPart, Is.EqualTo(ZombieBodyPart.Torso));
            Assert.That(r.Hit.HitPoint, Is.EqualTo(Vector3.zero)); Assert.That(r.Hit.Direction, Is.EqualTo(Vector3.forward));
        }
        [Test] public void DuplicateIdentityCannotCommitTwice()
        { region.ReceiveDamage(Hit()); Assert.That(region.ReceiveDamage(Hit()).PresentationAccepted, Is.False); Assert.That(health.DamageTransactions, Is.EqualTo(1)); }
        [Test] public void DistinctShotsAndSubHitsArePreserved()
        { region.ReceiveDamage(Hit(1)); region.ReceiveDamage(Hit(2)); region.ReceiveDamage(Hit(2, 1)); Assert.That(health.DamageTransactions, Is.EqualTo(3)); }
        [Test] public void RejectedReceiptCannotClaimPresentation()
        { health.SetDamageEnabled(false); Assert.That(region.ReceiveDamage(Hit()).PresentationAccepted, Is.False); }
        [Test] public void WrongColliderOwnerIsRejected()
        { var h = Hit(); h.HitCollider = root.AddComponent<BoxCollider>(); Assert.That(region.ReceiveDamage(h).Committed, Is.False); }
        [Test] public void NonfiniteCoordinatesAreRejected()
        { var h = Hit(); h.HitPoint = new Vector3(float.NaN, 0, 0); Assert.That(region.ReceiveDamage(h).Committed, Is.False); }
        [Test] public void SeverityAndFatalReceiptDoNotDuplicateDeath()
        {
            int deaths = 0; health.Died += () => deaths++;
            var h = Hit(); h.Amount = 200; var r = region.ReceiveDamage(h);
            Assert.That(r.Terminal); Assert.That(r.Severity, Is.EqualTo(ZombieImpactSeverity.Heavy));
            Assert.That(region.ReceiveDamage(Hit(2)).PresentationAccepted, Is.False); Assert.That(deaths, Is.EqualTo(1));
        }
        [Test] public void RestoringCompletedHealthNeverEmitsAnotherKillOrDamageReceipt()
        {
            int deaths=0,commits=0;health.Died+=()=>deaths++;health.DamageCommitted+=_=>commits++;
            var restore=typeof(ZombieHealth).GetMethod("RestoreHealth",BindingFlags.NonPublic|BindingFlags.Instance);
            restore.Invoke(health,new object[]{0f});restore.Invoke(health,new object[]{0f});
            Assert.That(health.IsAlive,Is.False);Assert.That(deaths,Is.Zero);Assert.That(commits,Is.Zero);Assert.That(health.DamageTransactions,Is.Zero);
        }
        [Test] public void GoreOptionsDoNotChangeHealthTransaction()
        {
            bool original = ZombieGorePreference.GraphicGoreEnabled;
            try { ZombieGorePreference.SetGraphicGoreEnabled(false); region.ReceiveDamage(Hit(1)); ZombieGorePreference.SetGraphicGoreEnabled(true); region.ReceiveDamage(Hit(2)); Assert.That(health.CurrentHealth, Is.EqualTo(80)); }
            finally { ZombieGorePreference.SetGraphicGoreEnabled(original); }
        }
        [TestCase("LowerLegL", ZombieBodyPart.LeftLeg)] [TestCase("LowerLegR", ZombieBodyPart.RightLeg)]
        [TestCase("Pelvis", ZombieBodyPart.Torso)] [TestCase("Head", ZombieBodyPart.Head)]
        [TestCase("HandL", ZombieBodyPart.LeftHand)] [TestCase("ForeArmR", ZombieBodyPart.RightArm)]
        public void ProductionSurfaceNamesHaveAnatomy(string name, ZombieBodyPart part)
        { Assert.That(ZombieWoundSurfaceResolver.PartFor(name), Is.EqualTo(part)); }
        [Test] public void RayTriangleReturnsBarycentricContactAndRejectsBackface()
        {
            Vector3 a = new Vector3(-1, -1, 0), b = new Vector3(0, 1, 0), c = new Vector3(1, -1, 0);
            Assert.That(ZombieWoundSurfaceResolver.Intersect(new Ray(Vector3.back, Vector3.forward), a, b, c, out var d, out var bary));
            Assert.That(d, Is.EqualTo(1).Within(.0001f)); Assert.That((a*bary.x+b*bary.y+c*bary.z).magnitude, Is.LessThan(.0001f));
            Assert.That(ZombieWoundSurfaceResolver.Intersect(new Ray(Vector3.forward, Vector3.back), a, b, c, out _, out _), Is.False);
        }
        [Test] public void GrazingClosestPointStaysOnTriangleInsteadOfHitboxCentre()
        {
            var bary=ZombieWoundSurfaceResolver.ClosestBarycentric(new Vector3(2,0,0),Vector3.zero,Vector3.right,Vector3.up);
            Assert.That(bary,Is.EqualTo(Vector3.up));
            bary=ZombieWoundSurfaceResolver.ClosestBarycentric(new Vector3(.2f,.3f,1),Vector3.zero,Vector3.right,Vector3.up);
            Assert.That(bary.x,Is.EqualTo(.5f).Within(.0001f));Assert.That(bary.y,Is.EqualTo(.2f).Within(.0001f));Assert.That(bary.z,Is.EqualTo(.3f).Within(.0001f));
        }
        [Test] public void MissingRendererFailsWithoutInventingSurface()
        { var resolver = new ZombieWoundSurfaceResolver(root); Assert.That(resolver.Resolve(Hit(), .35f, out _), Is.False); Assert.That(resolver.ResolutionMisses, Is.EqualTo(1)); }
        [Test] public void WoundProfileRejectsInvalidBudgetsAndNonfiniteValues()
        {
            var p = ScriptableObject.CreateInstance<ZombieWoundProfile>();
            try { Assert.That(p.IsValid); p.lowCapacity = 9; Assert.That(p.IsValid, Is.False); p.lowCapacity = 4; p.radius = float.NaN; Assert.That(p.IsValid, Is.False); }
            finally { UnityEngine.Object.DestroyImmediate(p); }
        }
        [Test] public void ProductionPrefabHasExplicitRuntimeAndProfileBindings()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            Assert.That(prefab.GetComponent<ZombieLocalizedWounds>(), Is.Not.Null); Assert.That(prefab.GetComponent<ZombieCorpseImpactReceiver>(), Is.Not.Null);
            var p = prefab.GetComponent<ZombieBloodVfxPresenter>().Profile;
            Assert.That(p.IsValid && p.woundProfile.IsValid); Assert.That(p.woundMaterial.shader.name, Is.EqualTo("LastSignal/LocalizedWound"));
        }
        [Test] public void AllProductionSurfacesHaveReadableRestCoordinatesAndMatchingSkinBindings()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var skins = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s => ZombieWoundSurfaceResolver.PartFor(s.name) != ZombieBodyPart.Unspecified).ToArray();
            Assert.That(skins.Length, Is.EqualTo(15));
            foreach (var s in skins)
            {
                var m = s.sharedMesh; Assert.That(m.isReadable, s.name); Assert.That(AssetDatabase.GetAssetPath(m), Does.StartWith("Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/"));
                Assert.That(m.vertexCount, Is.EqualTo(m.boneWeights.Length)); Assert.That(m.bindposes.Length, Is.EqualTo(s.bones.Length));
                Assert.That(s.bones.All(b => b), s.name); Assert.That(s.quality, Is.EqualTo(SkinQuality.Bone4));
                var rest = new System.Collections.Generic.List<Vector3>(); m.GetUVs(2, rest); Assert.That(rest, Is.EqualTo(m.vertices), s.name);
            }
        }
        [Test] public void MissingLegacyWoundsNormalizeAsValidWithoutChangingAnatomy()
        {
            var state = JsonUtility.FromJson<ZombieAnatomySnapshot>("{\"severedMask\":0,\"torsoDamage\":0,\"regionalDamage\":[0,0,0,0,0,0,0,0,0]}");
            Assert.That(SaveValidation.ValidZombieAnatomy(state)); Assert.That(ZombieWoundSnapshot.Valid(state.wounds));
        }
        [Test] public void VersionedWoundRecordsRoundTripAndRejectMalformedInput()
        {
            var data = new ZombieWoundSnapshot { version = 1, entries = new[] { new ZombieWoundEntry { surface = "Head", triangle = 2, barycentric = new Vector3(.2f,.3f,.5f), radius = .035f, severity = .5f, seed = 12 } } };
            var copy = JsonUtility.FromJson<ZombieWoundSnapshot>(JsonUtility.ToJson(data)); Assert.That(ZombieWoundSnapshot.Valid(copy));
            copy.entries[0].barycentric.x = float.NaN; Assert.That(ZombieWoundSnapshot.Valid(copy), Is.False);
            data.version = 99; Assert.That(ZombieWoundSnapshot.Valid(data), Is.False);
        }
        [Test] public void SaveBudgetAndUnknownSurfaceAreRejected()
        {
            Assert.That(ZombieWoundSnapshot.Valid(new ZombieWoundSnapshot { version = 1, entries = new ZombieWoundEntry[9] }), Is.False);
            Assert.That(ZombieWoundSnapshot.Valid(new ZombieWoundSnapshot { version = 1, entries = new[] { new ZombieWoundEntry { surface = "Other", barycentric = Vector3.right, radius = .03f } } }), Is.False);
        }
        [Test] public void AuthoringUsesExistingDerivativeAndPreservesGuidOnRepeat()
        {
            // Exercise the same entry point used for production; no alternate test-only authoring path.
            string before = AssetDatabase.AssetPathToGUID("Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/Head.asset");
            string prefabGuid = AssetDatabase.AssetPathToGUID(Prefab);
            var blood = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab).GetComponent<ZombieBloodVfxPresenter>().Profile;
            var paths = System.IO.Directory.GetFiles(LastSignal.Editor.ZombieWoundAuthoring.MeshFolder, "*.asset")
                .Concat(new[]{Prefab, LastSignal.Editor.ZombieWoundAuthoring.ProfilePath, AssetDatabase.GetAssetPath(blood)})
                .ToArray();
            // Preserve the working copies, including uncommitted authoring, rather than Git versions.
            var originals = paths.SelectMany(p => new[]{p, p + ".meta"})
                .ToDictionary(p => p, p => System.IO.File.ReadAllBytes(p));
            try
            {
                LastSignal.Editor.ZombieWoundAuthoring.Bind();
                Assert.That(AssetDatabase.AssetPathToGUID("Assets/LastSignal/Models/Enemies/Zombie/WoundSurfaces/Head.asset"), Is.EqualTo(before));
                Assert.That(AssetDatabase.AssetPathToGUID(Prefab), Is.EqualTo(prefabGuid));
            }
            finally
            {
                var changed = originals.Where(p => !System.IO.File.Exists(p.Key) ||
                    !System.IO.File.ReadAllBytes(p.Key).SequenceEqual(p.Value)).Select(p => p.Key).ToArray();
                foreach (var path in changed) System.IO.File.WriteAllBytes(path, originals[path]);
                foreach (var path in changed.Select(p => p.EndsWith(".meta") ? p.Substring(0, p.Length - 5) : p).Distinct())
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
