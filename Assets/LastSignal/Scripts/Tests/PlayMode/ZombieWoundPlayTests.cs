#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using LastSignal.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    public sealed class ZombieWoundPlayTests
    {
        InputFixtureIsolation.SceneScope sceneScope;
        GameObject actor;
        ZombieHealth health;
        ZombieLocalizedWounds wounds;
        ZombieBloodVfxPresenter presenter;
        ZombieBloodVfxPool pool;
        Camera camera;
        bool oldGore, oldReduced;
        ulong shotId;
        [SetUp] public void Setup()
        {
            sceneScope = new InputFixtureIsolation.SceneScope();
            oldGore = ZombieGorePreference.GraphicGoreEnabled; oldReduced = CombatPresentationPreference.ReducedEffects;
            ZombieGorePreference.SetGraphicGoreEnabled(true); CombatPresentationPreference.SetReducedEffects(false);
            actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab"), new Vector3(4100,100,4100), Quaternion.identity);
            health = actor.GetComponent<ZombieHealth>(); wounds = actor.GetComponent<ZombieLocalizedWounds>(); presenter = actor.GetComponent<ZombieBloodVfxPresenter>();
            pool = ZombieBloodVfxPool.Warm(presenter.Profile);
            camera = new GameObject("Wound test camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.transform.position = actor.transform.position + Vector3.forward * 3;
            Assert.That(wounds.Resolver.Surfaces.Count, Is.EqualTo(15));
        }
        [TearDown] public void Cleanup()
        {
            if (actor) Object.DestroyImmediate(actor); if (camera) Object.DestroyImmediate(camera.gameObject);
            if (pool) Object.DestroyImmediate(pool.gameObject);
            ZombieGorePreference.SetGraphicGoreEnabled(oldGore); CombatPresentationPreference.SetReducedEffects(oldReduced);
            sceneScope?.Dispose();
        }
        ZombieWoundSurfaceResolver.Contact Contact(string surface, int variant = 0, bool rear = false)
        {
            var s = wounds.Resolver.Surfaces.Single(x => x.renderer.name == surface); s.Invalidate(); s.Sample();
            Vector3 facing = actor.transform.forward * (rear ? -1 : 1);
            Vector3 target = s.renderer.bounds.center + actor.transform.right * (variant * .075f);
            float best = float.PositiveInfinity; ZombieWoundSurfaceResolver.Contact result = default;
            for (int i = 0; i < s.triangles.Length; i += 3)
            {
                var a = s.world[s.triangles[i]]; var b = s.world[s.triangles[i + 1]]; var c = s.world[s.triangles[i + 2]];
                var normal = Vector3.Cross(b-a,c-a).normalized; if (Vector3.Dot(normal,facing) < .35f) continue;
                var point = (a+b+c)/3; float score = (target-point).sqrMagnitude;
                if (score >= best || !wounds.Resolver.Raycast(new Ray(point+normal*.015f,-normal),.03f,s.part,out var hit) || hit.surface != s) continue;
                best = score; result = hit;
            }
            Assert.That(result.Valid, "Production surface must have an exposed triangle: " + surface); return result;
        }
        DamageReceipt FireAt(ZombieWoundSurfaceResolver.Contact c, float damage = 1)
        {
            var r = actor.GetComponentsInChildren<ZombieHitRegion>(true).First(x => x.BodyPart == c.surface.part);
            return r.ReceiveDamage(new DamageInfo { Amount = damage, ShotId = ++shotId, Category = DamageCategory.Bullet,
                HitCollider = r.HitCollider, Direction = -c.normal, HitNormal = c.normal, HitPoint = c.position, SourcePosition = c.position+c.normal*2 });
        }
        [TestCase("Ribcage")] [TestCase("Head")] [TestCase("ArmL")] [TestCase("ForeArmR")]
        [TestCase("HandL")] [TestCase("LegL")] [TestCase("LowerLegR")] [TestCase("Pelvis")]
        public void FirstImpactCreatesWoundOnActualProductionSurface(string name)
        {
            var contact = Contact(name); var receipt = FireAt(contact);
            Assert.That(receipt.Committed); Assert.That(wounds.WoundCount, Is.EqualTo(1));
            Assert.That(wounds.TryGetContact(0, out var attached)); Assert.That(attached.surface.renderer.name, Is.EqualTo(name));
            Assert.That(attached.Evaluate(out var point, out _)); Assert.That(Vector3.Distance(point,contact.position), Is.LessThan(.025f));
            Assert.That(Vector3.Distance(pool.LastHitOrigin, contact.position), Is.LessThan(.025f));
        }
        [Test] public void SeparateChestPointsRemainSeparateAndNearbyImpactsMerge()
        {
            var a=Contact("Ribcage",-1); var b=Contact("Ribcage",1); Assert.That(Vector3.Distance(a.position,b.position), Is.GreaterThan(.05f));
            FireAt(a); FireAt(b); Assert.That(wounds.WoundCount, Is.EqualTo(2)); float severity=wounds.SeverityAt(0);
            for(int i=0;i<5;i++) FireAt(a);
            Assert.That(wounds.WoundCount, Is.EqualTo(2)); Assert.That(wounds.MergedWounds, Is.EqualTo(5)); Assert.That(wounds.SeverityAt(0), Is.GreaterThan(severity));
            Assert.That(pool.TotalHitRequests, Is.EqualTo(7));
        }
        [Test] public void BackImpactCannotAttachToFrontTriangle()
        {
            var c=Contact("Ribcage",0,true); FireAt(c); Assert.That(wounds.TryGetContact(0,out var w));
            w.Evaluate(out var p,out var n); Assert.That(Vector3.Dot(n,actor.transform.forward), Is.LessThan(-.25f));
            Assert.That(Vector3.Distance(p,c.position), Is.LessThan(.025f));
        }
        [Test] public void SevereBackBulletDoesNotOpenLegacyFrontChestPatch()
        {
            FireAt(Contact("Ribcage",0,true),45);
            var old=actor.GetComponentsInChildren<Transform>(true).Single(x=>x.name=="TorsoWound_Visual");
            Assert.That(old.gameObject.activeSelf,Is.False);Assert.That(wounds.WoundCount,Is.EqualTo(1));
            var sever=actor.GetComponent<ZombieDismemberment>();var state=sever.CaptureState();sever.RestoreState(state);
            ZombieGorePreference.SetGraphicGoreEnabled(false);ZombieGorePreference.SetGraphicGoreEnabled(true);
            Assert.That(old.gameObject.activeSelf,Is.False);
        }
        [UnityTest] public IEnumerator AttachmentFollowsTranslationRotationAndArmPose()
        {
            FireAt(Contact("ArmR")); Assert.That(wounds.TryGetContact(0,out var c)); c.Evaluate(out var before,out _);
            var local=actor.transform.InverseTransformPoint(before); actor.transform.position+=new Vector3(1,.2f,-2); actor.transform.rotation=Quaternion.Euler(0,80,0);
            yield return null; c.Evaluate(out var after,out _); Assert.That(Vector3.Distance(after,actor.transform.TransformPoint(local)), Is.LessThan(.02f));
            var animator=actor.GetComponentInChildren<Animator>();
            var arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm); Assert.That(arm,Is.Not.Null);
            animator.enabled=false; arm.localRotation*=Quaternion.Euler(25,0,0);
            yield return null; Assert.That(c.Evaluate(out var posed,out _)); Assert.That(Vector3.Distance(posed,after), Is.GreaterThan(.005f));
            Assert.That(wounds.WoundCount, Is.EqualTo(1)); Assert.That(c.surface.renderer.bounds.Contains(posed));
        }
        [Test] public void FatalHitHasWoundBeforeSingleDeathEvent()
        {
            int deaths=0; health.Died+=()=>{deaths++;Assert.That(wounds.WoundCount,Is.EqualTo(1));};
            var receipt=FireAt(Contact("Ribcage"),200); Assert.That(receipt.Terminal); Assert.That(deaths,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator BulletBleedingFollowsAndExpires()
        {
            FireAt(Contact("Ribcage")); Assert.That(pool.ActiveBleeds,Is.EqualTo(1));
            actor.transform.position+=Vector3.right; yield return null;
            Assert.That(wounds.TryGetContact(0,out var c)); Assert.That(c.Evaluate(out var attached,out _));
            Assert.That(pool.TryGetBleedOrigin(presenter,out var emitter)); Assert.That(Vector3.Distance(emitter,attached),Is.LessThan(.015f));
            float end=Time.time+presenter.Profile.woundProfile.bleedSeconds+presenter.Profile.woundProfile.dripSeconds+1.7f;
            while(Time.time<end) yield return null;
            Assert.That(pool.ActiveBleeds,Is.Zero); Assert.That(pool.ActiveHits,Is.Zero);
        }
        [Test] public void NormalAndLowQualityHaveBoundedWoundAndBloodPools()
        {
            foreach(var name in new[]{"Head","Ribcage","Pelvis","ArmL","ArmR","HandL","HandR","LegL","LegR","LowerLegL","LowerLegR"}) FireAt(Contact(name));
            Assert.That(wounds.WoundCount,Is.EqualTo(8)); Assert.That(pool.ActiveHits,Is.LessThanOrEqualTo(presenter.Profile.hitCapacity));
            CombatPresentationPreference.SetReducedEffects(true); FireAt(Contact("Ribcage")); Assert.That(wounds.WoundCount,Is.EqualTo(4));
        }
        [Test] public void GoreOffClearsWoundsAndDoesNotReplayWhenReenabled()
        {
            FireAt(Contact("Ribcage")); ZombieGorePreference.SetGraphicGoreEnabled(false);
            Assert.That(wounds.WoundCount,Is.Zero); Assert.That(pool.ActiveHits,Is.Zero);
            FireAt(Contact("Ribcage")); Assert.That(health.DamageTransactions,Is.EqualTo(2)); Assert.That(wounds.WoundCount,Is.Zero);
            ZombieGorePreference.SetGraphicGoreEnabled(true); Assert.That(pool.TotalHitRequests,Is.EqualTo(1)); Assert.That(wounds.WoundCount,Is.Zero);
        }
        [Test] public void SaveRestorePreservesWoundsWithoutReplayingBlood()
        {
            FireAt(Contact("Ribcage")); var sever=actor.GetComponent<ZombieDismemberment>();
            var saved=JsonUtility.FromJson<ZombieAnatomySnapshot>(JsonUtility.ToJson(sever.CaptureState()));
            int requests=pool.TotalHitRequests; sever.RestoreState(saved);
            Assert.That(wounds.WoundCount,Is.EqualTo(1)); Assert.That(pool.TotalHitRequests,Is.EqualTo(requests)); Assert.That(pool.ActiveHits,Is.Zero);
        }
        [Test] public void ActorReuseClearsWoundsAndPerInstanceGeometry()
        {
            FireAt(Contact("Head")); actor.SetActive(false); Assert.That(wounds.WoundCount,Is.Zero); Assert.That(pool.ActiveHits,Is.Zero);
            actor.SetActive(true); Assert.That(wounds.WoundCount,Is.Zero);
            foreach(var s in wounds.Resolver.Surfaces) Assert.That(s.renderer.sharedMesh,Is.SameAs(s.original));
        }
        [Test] public void HiddenSeverSurfaceCannotKeepOverlayOrBleedAttachment()
        {
            FireAt(Contact("ArmL")); wounds.TryGetContact(0,out var contact); contact.surface.renderer.enabled=false;
            Assert.That(contact.Evaluate(out _,out _),Is.False);
        }
        [Test] public void DuplicatePresentationAndForeignOwnerCannotCreateExtraWoundsOrBlood()
        {
            var receipt=FireAt(Contact("Ribcage")); int count=pool.TotalHitRequests;
            presenter.PresentImpact(receipt); Assert.That(pool.TotalHitRequests,Is.EqualTo(count)); Assert.That(wounds.WoundCount,Is.EqualTo(1));
            var other=new GameObject("Foreign owner");
            try {var foreign=other.AddComponent<ZombieHealth>(); wounds.Present(new DamageReceipt(receipt.Hit,foreign,1,false,false));Assert.That(wounds.WoundCount,Is.EqualTo(1));}
            finally{Object.DestroyImmediate(other);}
        }
        [Test] public void HeadDentIsLocalBoundedAndDoesNotModifySharedAsset()
        {
            var c=Contact("Head"); var source=c.surface.original; var original=source.vertices;
            FireAt(c,15); Assert.That(c.surface.renderer.sharedMesh,Is.Not.SameAs(source));
            var changed=c.surface.renderer.sharedMesh.vertices; int moved=0;
            for(int i=0;i<changed.Length;i++)
            {
                float d=Vector3.Distance(original[i],changed[i]); Assert.That(d,Is.LessThanOrEqualTo(presenter.Profile.woundProfile.maximumIndent+.00001f));
                if(d>.000001f)moved++;
            }
            Assert.That(moved,Is.GreaterThan(0));Assert.That(moved,Is.LessThan(original.Length/4));Assert.That(source.vertices,Is.EqualTo(original));
        }
        [Test] public void SessionResetClearsLivingWoundsAndTransientEffects()
        {
            FireAt(Contact("Ribcage"));ZombieBloodVfxPool.ResetSession();
            Assert.That(wounds.WoundCount,Is.Zero);Assert.That(pool.ActiveHits,Is.Zero);Assert.That(pool.ActiveMarks,Is.Zero);
        }
        [Test] public void EncounterTerminalSnapshotSurvivesCorpseRemoval()
        {
            var root=new GameObject("Persistent encounter record");
            try
            {
                root.AddComponent<PersistentEntityId>(); var encounter=root.AddComponent<ZombieEncounter>();
                typeof(ZombieEncounter).GetProperty("Actor").SetValue(encounter,actor.GetComponent<ZombieController>());
                var corpse=Corpse();
                typeof(ZombieEncounter).GetMethod("RememberDeath",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(encounter,null);
                var saved=encounter.CaptureEnemy();Assert.That(saved.health,Is.Zero);
                Object.DestroyImmediate(actor);actor=null;
                Assert.That(encounter.CaptureEnemy(),Is.SameAs(saved));Assert.That(encounter.CaptureEnemy().health,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        ZombieCorpseImpactReceiver Corpse()
        {
            FireAt(Contact("Ribcage"),200);
            // Isolated renderer fixture: terminal navigation/death behavior is covered by ZombieDamagePlayTests.
            actor.GetComponent<ZombieController>().Runtime.Transition(ZombieState.Dead);
            foreach(var c in actor.GetComponentsInChildren<Collider>()) c.enabled=false;
            Assert.That(presenter.RetainDeathPresentation(actor.GetComponent<ZombieController>(),"wound-cell"));
            return actor.GetComponent<ZombieCorpseImpactReceiver>();
        }
        [TestCase("Ribcage")] [TestCase("Head")] [TestCase("LowerLegL")]
        public void CorpseBulletsUsePresentationOnlyAndDoNotExtendLease(string surface)
        {
            int deaths=0;health.Died+=()=>deaths++; var corpse=Corpse(); float expiry=corpse.Expires; var c=Contact(surface);
            int transactions=health.DamageTransactions, before=pool.TotalHitRequests;
            Physics.SyncTransforms(); var shot=WeaponFireResolver.Resolve(c.position+c.normal*2,-c.normal,c.position+c.normal*2,5,.2f,20,null,~(1<<2));
            Assert.That(shot.Receipt.CorpseImpact && shot.ImpactAccepted); Assert.That(shot.DamageCommitted,Is.False);
            Assert.That(shot.Receipt.AppliedDamage,Is.Zero); Assert.That(health.CurrentHealth,Is.Zero); Assert.That(health.DamageTransactions,Is.EqualTo(transactions));
            Assert.That(deaths,Is.EqualTo(1)); Assert.That(corpse.Expires,Is.EqualTo(expiry)); Assert.That(pool.TotalHitRequests,Is.EqualTo(before+1));
        }
        [Test] public void CorpseRemainsOccludedByWallAndObeysHitMask()
        {
            Corpse(); var c=Contact("Ribcage"); var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                wall.transform.position=c.position+c.normal; wall.transform.localScale=Vector3.one*.5f; Physics.SyncTransforms();
                var shot=WeaponFireResolver.Resolve(c.position+c.normal*2,-c.normal,c.position+c.normal*2,5,.2f,20,null,~(1<<2));
                Assert.That(shot.Collider,Is.SameAs(wall.GetComponent<Collider>())); Assert.That(shot.ImpactAccepted,Is.False);
                Assert.That(ZombieCorpseImpactReceiver.Raycast(new Ray(c.position+c.normal,-c.normal),2,0,out _,out _),Is.False);
            }
            finally {Object.DestroyImmediate(wall);}
        }
        [Test] public void CorpseReceiptDedupAndGoreParityKeepHealthUntouched()
        {
            var corpse=Corpse();var c=Contact("Ribcage");int count=health.DamageTransactions;
            var hit=new DamageInfo {Amount=5,ShotId=100,Category=DamageCategory.Bullet,Direction=-c.normal};
            Assert.That(corpse.Receive(hit,c).CorpseImpact);Assert.That(corpse.Receive(hit,c).PresentationAccepted,Is.False);
            ZombieGorePreference.SetGraphicGoreEnabled(false);hit.ShotId++;
            Assert.That(corpse.Receive(hit,c).CorpseImpact);Assert.That(health.DamageTransactions,Is.EqualTo(count));Assert.That(pool.ActiveHits,Is.Zero);
        }
        [Test] public void CellUnloadAndSessionResetReleaseCorpseAndEffects()
        {
            Corpse();ZombieBloodVfxPool.ReleaseCell("wound-cell");Assert.That(pool.RetainedCorpses,Is.Zero);Assert.That(pool.ActiveHits,Is.Zero);Assert.That(actor.activeSelf,Is.False);
            ZombieBloodVfxPool.ResetSession();Assert.That(pool.ActiveMarks,Is.Zero);
        }
        [UnityTest] public IEnumerator CorpseExpiresAndReleasesWoundResources()
        {
            var corpse=Corpse(); var oldScale=Time.timeScale;
            try {Time.timeScale=20; float end=corpse.Expires+.2f;while(Time.time<end) yield return null; Assert.That(!actor || !actor.activeSelf);Assert.That(pool.RetainedCorpses,Is.Zero);Assert.That(pool.ActiveHits,Is.Zero);}
            finally{Time.timeScale=oldScale;}
        }
    }
}
#endif
