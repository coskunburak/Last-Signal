#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public class ZombieDamagePlayTests
    {
        const string Evidence = "Docs/Implementation/S004/Evidence/20260919-P4";
        SessionFlow session;
        ZombieController zombie;
        ZombieHealth health;
        WeaponFireResolver.ShotResult lastShot;
        readonly List<GameObject> owned = new List<GameObject>();
        IEnumerator Load(string path = "Assets/LastSignal/Scenes/ZombieAcceptance.unity")
        {
            Directory.CreateDirectory(Evidence + "/visual");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; session = Object.FindAnyObjectByType<SessionFlow>(); session.Resume();
            session.Player.GetComponent<FirstPersonMotor>().enabled = false;
            zombie = session.GetComponent<ZombieEncounter>().Actor; health = zombie.GetComponent<ZombieHealth>();
            if (path.Contains("ZombieAcceptance")) zombie.GetComponent<NavMeshAgent>().Warp(new Vector3(-8, 0, 0));
            zombie.transform.rotation = Quaternion.identity; Place(zombie.transform.position + Vector3.forward * 4);
            yield return Until(() => session.Player.GetComponent<PlayerCombatController>().ActiveWeapon.RuntimeState.State == WeaponState.Ready);
            session.Player.GetComponent<PlayerCombatController>().ActiveWeapon.ShotFired += result => lastShot = result;
        }
        void Place(Vector3 position)
        {
            var cc = session.Player.GetComponent<CharacterController>(); cc.enabled = false;
            session.Player.transform.position = position;
            session.Player.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(zombie.transform.position - position, Vector3.up));
            cc.enabled = true; Physics.SyncTransforms();
        }
        IEnumerator Until(Func<bool> predicate, float timeout = 6)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(predicate(), Is.True, "Timed out; state=" + (zombie ? zombie.Runtime.State.ToString() : "none"));
        }
        ZombieHitRegion Region(DamageRegion type)
        {
            foreach (var r in zombie.GetComponentsInChildren<ZombieHitRegion>()) if (r.Region == type && (type == DamageRegion.Head || r.name == "Damage_Chest")) return r;
            throw new Exception("Missing region");
        }
        void Fire(DamageRegion type)
        {
            var weapon = session.Player.GetComponent<PlayerCombatController>().ActiveWeapon;
            Assert.That(weapon.RuntimeState.CanFire);
            var camera = session.Player.GetComponent<FirstPersonLook>().View;
            camera.transform.LookAt(Region(type).HitCollider.bounds.center); Physics.SyncTransforms();
            weapon.OnFirePressed(); weapon.OnFireReleased();
            File.AppendAllText(Evidence+"/rifle-trace.txt", "id="+lastShot.ShotId+" intended="+type+" hit="+lastShot.Hit+" obstruction="+lastShot.MuzzleObstructed+" collider="+(lastShot.Collider?lastShot.Collider.name:"none")+" HP="+health.CurrentHealth+" state="+zombie.Runtime.State+"\n");
        }
        WeaponFireResolver.ShotResult Resolve(DamageRegion type, float amount = 30)
        {
            var collider = Region(type).HitCollider;
            var origin = collider.bounds.center + zombie.transform.forward * 3;
            Physics.SyncTransforms();
            return WeaponFireResolver.Resolve(origin, (collider.bounds.center-origin).normalized, origin, 10, .5f, amount, session.Player, 1<<8);
        }
        public static void Render(Camera camera, string path, int width = 960, int height = 960)
        {
            var priorTarget = camera.targetTexture; var priorActive = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try { camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { camera.targetTexture = priorTarget; RenderTexture.active = priorActive; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
        }

        void Capture(string name, bool hitboxes = false)
        {
            var camera = session.Player.GetComponent<FirstPersonLook>().View;
            camera.transform.LookAt(zombie.transform.position + Vector3.up * (zombie.IsDead ? .4f : 1.2f));
            Render(camera, Evidence + "/visual/" + name + "-fps.png", 960, 640);
            var go = new GameObject("OwnedP4EvidenceCamera"); var side = go.AddComponent<Camera>(); side.enabled = false;
            side.cullingMask &= ~((1 << 30) | (1 << 29));
            side.transform.position = zombie.transform.position + new Vector3(3, 2, 3);
            side.transform.LookAt(zombie.transform.position + new Vector3(0, .8f, -.3f));
            var overlays = new List<GameObject>(); var materials = new List<Material>();
            try
            {
                if (hitboxes) foreach (var region in zombie.GetComponentsInChildren<ZombieHitRegion>())
                {
                    var box = region.GetComponent<BoxCollider>();
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); materials.Add(mat);
                    mat.color = region.Region == DamageRegion.Head ? Color.yellow : Color.cyan;
                    for(int axis=0;axis<3;axis++)for(int u=-1;u<=1;u+=2)for(int v=-1;v<=1;v+=2)
                    {
                        var edge=new GameObject("OwnedHitboxEdge");overlays.Add(edge);var line=edge.AddComponent<LineRenderer>();
                        var from=Vector3.zero;var to=Vector3.zero;from[axis]=-.5f;to[axis]=.5f;
                        from[(axis+1)%3]=to[(axis+1)%3]=u*.5f;from[(axis+2)%3]=to[(axis+2)%3]=v*.5f;
                        line.positionCount=2;line.SetPosition(0,box.transform.TransformPoint(box.center+Vector3.Scale(from,box.size)));
                        line.SetPosition(1,box.transform.TransformPoint(box.center+Vector3.Scale(to,box.size)));
                        line.startWidth=line.endWidth=.003f;line.sharedMaterial=mat;
                    }
                }
                Render(side, Evidence + "/visual/" + name + "-side.png", 960, 640);
            }
            finally { foreach(var overlay in overlays) Object.DestroyImmediate(overlay); foreach(var mat in materials)Object.DestroyImmediate(mat); Object.DestroyImmediate(go); }
            File.AppendAllText(Evidence + "/visual/trace.tsv", name + "\t" + zombie.Runtime.State + "\t" + health.LastDamage.ShotId + "\t" + health.LastDamage.Region + "\t" + health.LastDamage.BaseAmount + "\t" + health.LastDamage.Multiplier + "\t" + health.LastDamage.Amount + "\t" + health.LastHealthBefore + "\t" + health.CurrentHealth + "\t" + health.DamageTransactions + "\n");
        }
        [UnityTest] public IEnumerator ActualProductionRifleBodyHeadReactionAndDeath()
        {
            yield return Load(); Capture("layout-idle", true);
            int deaths = 0; health.Died += () => deaths++;
            Fire(DamageRegion.Body); Assert.That(health.CurrentHealth, Is.EqualTo(70)); Assert.That(health.DamageTransactions, Is.EqualTo(1));
            Assert.That(zombie.Runtime.State, Is.EqualTo(ZombieState.HitReact)); Capture("body-hit-react-start");
            yield return new WaitForSeconds(.12f); Capture("body-hit-react-peak");
            yield return Until(() => zombie.Runtime.State != ZombieState.HitReact); Capture("reaction-recovered");
            Fire(DamageRegion.Head); Assert.That(health.CurrentHealth, Is.EqualTo(10)); Assert.That(health.LastDamage.Region, Is.EqualTo(DamageRegion.Head)); Capture("head-shot");
            yield return new WaitForSeconds(.2f); Fire(DamageRegion.Body);
            Assert.That(zombie.IsDead); Assert.That(deaths, Is.EqualTo(1)); Assert.That(health.CurrentHealth, Is.Zero); Capture("lethal-body");
            yield return new WaitForSeconds(.7f); Capture("death-middle");
            yield return Until(() => zombie.GetComponent<ZombieAnimationPresenter>().CorpseSettled); Capture("corpse-final");
            var position = zombie.transform.position; var rotation = zombie.transform.rotation;
            int rays = zombie.Perception.Raycasts, paths = zombie.Navigation.PathRequests;
            for (int i=0;i<20;i++) health.TakeDamage(new DamageInfo { Amount=1000 });
            yield return new WaitForSeconds(3); Capture("corpse-inactive");
            Assert.That(zombie.transform.position, Is.EqualTo(position)); Assert.That(zombie.transform.rotation, Is.EqualTo(rotation));
            Assert.That(zombie.Perception.Raycasts, Is.EqualTo(rays)); Assert.That(zombie.Navigation.PathRequests, Is.EqualTo(paths));
            Assert.That(zombie.GetComponent<NavMeshAgent>().enabled, Is.False); Assert.That(zombie.Target, Is.Null);
            Assert.That(zombie.GetComponentInChildren<Animator>().enabled, Is.False); Assert.That(deaths, Is.EqualTo(1));
            foreach(var c in zombie.GetComponentsInChildren<Collider>()) Assert.That(c.enabled, Is.False);
            Assert.That(zombie.Initialize(), Is.False); Assert.That(zombie.Bind(session.Player), Is.False);
        }
        [UnityTest] public IEnumerator LethalRifleDuringWindupCommitAndRecoveryCancelsOldStrike()
        {
            foreach(var phase in new[]{ZombieState.AttackWindup,ZombieState.AttackCommit,ZombieState.Recovering})
            {
                yield return Load(); Fire(DamageRegion.Body); yield return new WaitForSeconds(.6f);
                Fire(DamageRegion.Head); Assert.That(health.CurrentHealth,Is.EqualTo(10));
                yield return new WaitForSeconds(.2f); Place(zombie.transform.position+zombie.transform.forward*1.25f);
                yield return Until(()=>zombie.Runtime.State==phase);
                // Resolver is the real weapon path; overkill isolates the cancellation edge.
                float before=session.Player.GetComponent<PlayerHealth>().CurrentHealth;
                Fire(DamageRegion.Body); Assert.That(zombie.IsDead); Capture("lethal-"+phase);
                yield return new WaitForSeconds(2);
                Assert.That(session.Player.GetComponent<PlayerHealth>().CurrentHealth,Is.EqualTo(before));
                Assert.That(zombie.AttackTime,Is.Zero); Assert.That(zombie.AttackSequence,Is.Zero); Assert.That(zombie.IsDead);
            }
        }
        [UnityTest] public IEnumerator ReactionCooldownAppliesDamageWithoutRestartAndAllowsLaterReaction()
        {
            yield return Load(); zombie.transform.rotation=Quaternion.Euler(0,180,0);
            Resolve(DamageRegion.Body,1); Assert.That(zombie.Runtime.State,Is.EqualTo(ZombieState.HitReact));
            yield return new WaitForSeconds(.15f); float remaining=zombie.ReactionRemaining;
            Resolve(DamageRegion.Body,1); Assert.That(zombie.ReactionRemaining,Is.EqualTo(remaining));
            yield return new WaitForSeconds(.45f); Assert.That(zombie.Runtime.State,Is.Not.EqualTo(ZombieState.HitReact));
            for(int i=0;i<5;i++){ Resolve(DamageRegion.Body,1); Assert.That(zombie.Runtime.State,Is.Not.EqualTo(ZombieState.HitReact)); yield return new WaitForSeconds(.12f); }
            yield return new WaitForSeconds(.5f); Resolve(DamageRegion.Body,1); Assert.That(zombie.Runtime.State,Is.EqualTo(ZombieState.HitReact));
            Assert.That(health.DamageTransactions,Is.EqualTo(8)); Assert.That(health.CurrentHealth,Is.EqualTo(92));
        }
        [UnityTest] public IEnumerator PauseReactionDeathAndSessionResetAreSafe()
        {
            yield return Load(); Fire(DamageRegion.Body); yield return new WaitForSeconds(.1f);
            session.Pause(); float remaining=zombie.ReactionRemaining;
            float anim=zombie.GetComponent<ZombieAnimationPresenter>().DamageTime;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(zombie.ReactionRemaining,Is.EqualTo(remaining)); Assert.That(zombie.GetComponent<ZombieAnimationPresenter>().DamageTime,Is.EqualTo(anim));
            session.Resume(); yield return Until(()=>zombie.Runtime.State!=ZombieState.HitReact);
            Resolve(DamageRegion.Head,1000); session.Pause(); anim=zombie.GetComponent<ZombieAnimationPresenter>().DamageTime;
            yield return new WaitForSecondsRealtime(.6f); Assert.That(zombie.IsDead); Assert.That(zombie.GetComponent<ZombieAnimationPresenter>().DamageTime,Is.EqualTo(anim));
            session.Resume(); yield return Until(()=>zombie.GetComponent<ZombieAnimationPresenter>().CorpseSettled);
            session.ReturnToMenu(); yield return null; session.BeginSession(); session.Resume(); yield return null;
            zombie=session.GetComponent<ZombieEncounter>().Actor; health=zombie.GetComponent<ZombieHealth>();
            Assert.That(health.IsAlive); Assert.That(health.CurrentHealth,Is.EqualTo(health.MaxHealth)); Assert.That(zombie.Target,Is.EqualTo(session.Player)); Assert.That(zombie.IsDead,Is.False); Capture("new-session-alive");
            foreach(bool dying in new[]{false,true})
            {
                if(dying)Resolve(DamageRegion.Head,1000); else Resolve(DamageRegion.Body,1);
                session.ReturnToMenu(); yield return null; session.BeginSession(); session.Resume(); yield return null;
                zombie=session.GetComponent<ZombieEncounter>().Actor; health=zombie.GetComponent<ZombieHealth>(); Assert.That(health.IsAlive);
            }
        }
        [UnityTest] public IEnumerator HiddenSearchKnowledgeIsNotRevealedByDamage()
        {
            yield return Load(); yield return Until(()=>zombie.Runtime.State==ZombieState.Chasing);
            Place(new Vector3(100,0,100)); yield return Until(()=>zombie.Runtime.State==ZombieState.Searching);
            var remembered=zombie.Runtime.LastKnownPosition; float age=zombie.Runtime.SearchAge; int point=zombie.Search.PointIndex;
            Resolve(DamageRegion.Body); Assert.That(zombie.Runtime.State,Is.EqualTo(ZombieState.HitReact));
            yield return Until(()=>zombie.Runtime.State==ZombieState.Searching);
            Assert.That(zombie.Runtime.LastKnownPosition,Is.EqualTo(remembered)); Assert.That(zombie.Runtime.SearchAge,Is.InRange(age,age+.1f)); Assert.That(zombie.Search.PointIndex,Is.EqualTo(point));
        }
        [UnityTest] public IEnumerator OverlapWrongColliderAndIndependentActorsHaveOneOwner()
        {
            yield return Load(); var r=Region(DamageRegion.Body); var duplicate=r.gameObject.AddComponent<BoxCollider>();
            var box=(BoxCollider)r.HitCollider; duplicate.center=box.center; duplicate.size=box.size*.9f;
            // One actual collider remains nearest. The extra collider must not cause a second dispatch.
            Resolve(DamageRegion.Body); Assert.That(health.DamageTransactions,Is.EqualTo(1)); Object.DestroyImmediate(duplicate);
            var other=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/LS_Zombie_Runtime.prefab"),new Vector3(-6,0,0),Quaternion.identity); owned.Add(other);
            var otherHealth=other.GetComponent<ZombieHealth>(); Resolve(DamageRegion.Head,1000);
            Assert.That(otherHealth.CurrentHealth,Is.EqualTo(otherHealth.MaxHealth)); Assert.That(otherHealth.DamageTransactions,Is.Zero);
        }
        [UnityTest] public IEnumerator FirstCompleteCombatLoopInNormalRoute()
        {
            yield return Load("Assets/Scenes/SampleScene.unity"); Place(zombie.transform.position+zombie.transform.forward*3);
            yield return Until(()=>zombie.Runtime.State==ZombieState.Chasing); Capture("loop-chase");
            yield return Until(()=>zombie.Runtime.State==ZombieState.AttackCommit,10);
            var playerHealth=session.Player.GetComponent<PlayerHealth>();
            var motor=session.Player.GetComponent<FirstPersonMotor>();
            while(!zombie.ContactConsumed){motor.Simulate(Vector2.right,false,Time.deltaTime);yield return null;}
            Assert.That(playerHealth.DamageTransactions,Is.Zero); Capture("loop-dodge");
            Fire(DamageRegion.Body); Assert.That(health.CurrentHealth,Is.EqualTo(70)); Capture("loop-body-reaction");
            yield return Until(()=>zombie.Runtime.State!=ZombieState.HitReact);
            Place(zombie.transform.position+zombie.transform.forward*1.25f);
            yield return Until(()=>playerHealth.DamageTransactions>0,10); Capture("loop-player-hit");
            Fire(DamageRegion.Head); Assert.That(health.CurrentHealth,Is.EqualTo(10));
            yield return new WaitForSeconds(.2f); Fire(DamageRegion.Body); Assert.That(zombie.IsDead); Capture("loop-lethal");
            int transactions=playerHealth.DamageTransactions;
            yield return new WaitForSeconds(3); Assert.That(playerHealth.DamageTransactions,Is.EqualTo(transactions)); Capture("loop-complete");
        }
        [UnityTest] public IEnumerator SurvivingReactionInterruptsAllAttackPhases()
        {
            foreach(var phase in new[]{ZombieState.AttackWindup,ZombieState.AttackCommit,ZombieState.Recovering})
            {
                yield return Load(); Place(zombie.transform.position+zombie.transform.forward*1.25f);
                yield return Until(()=>zombie.Runtime.State==phase);
                int before=session.Player.GetComponent<PlayerHealth>().DamageTransactions;
                Resolve(DamageRegion.Body); Assert.That(zombie.Runtime.State,Is.EqualTo(ZombieState.HitReact));
                yield return new WaitForSeconds(.45f);
                Assert.That(session.Player.GetComponent<PlayerHealth>().DamageTransactions,Is.EqualTo(before));
                Assert.That(zombie.AttackTime,Is.Zero);
            }
        }
        [UnityTest] public IEnumerator UnrelatedChildColliderCannotResolveToRootHealth()
        {
            yield return Load(); var go=new GameObject("OwnedNonRegion");go.layer=8;go.transform.SetParent(zombie.transform);
            go.transform.localPosition=new Vector3(0,3,0);go.AddComponent<BoxCollider>();
            Physics.SyncTransforms();var origin=go.transform.position+Vector3.forward*3;
            var hit=WeaponFireResolver.Resolve(origin,Vector3.back,origin,10,.5f,30,session.Player,1<<8);
            Assert.That(hit.Hit);Assert.That(hit.Collider.gameObject,Is.EqualTo(go));Assert.That(health.DamageTransactions,Is.Zero);
            Object.DestroyImmediate(go);
        }
        [UnityTest] public IEnumerator HeadRegionFollowsIdleWalkAttackAndReactionPoses()
        {
            yield return Load();zombie.SetPaused(true);health.SetDamageEnabled(true);
            var animator=zombie.GetComponentInChildren<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(string pose in new[]{"Idle","Locomotion","Attack","HitReact"})
            {
                animator.speed=1;animator.Play(pose,0,0);animator.Update(pose=="HitReact"?.2f:.3f);animator.speed=0;
                Physics.SyncTransforms();var shot=Resolve(DamageRegion.Head,.01f);
                Assert.That(shot.Collider,Is.EqualTo(Region(DamageRegion.Head).HitCollider));
                Assert.That(health.LastDamage.Region,Is.EqualTo(DamageRegion.Head));
                Capture("layout-"+pose,true);
            }
            Assert.That(health.DamageTransactions,Is.EqualTo(4));
        }
        [UnityTest] public IEnumerator NearMuzzleDamageableReceivesOneHitButWallStopsDistantDamage()
        {
            yield return Load();zombie.SetPaused(true);health.SetDamageEnabled(true);
            var target=Region(DamageRegion.Head).HitCollider;Physics.SyncTransforms();
            var origin=target.bounds.center+Vector3.forward*.4f;
            var hit=WeaponFireResolver.Resolve(origin,Vector3.back,origin,10,.5f,1,session.Player,1<<8);
            Assert.That(hit.MuzzleObstructed);Assert.That(health.DamageTransactions,Is.EqualTo(1));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);owned.Add(wall);
            wall.transform.position=origin+Vector3.back*.08f;wall.transform.localScale=new Vector3(.3f,.3f,.03f);Physics.SyncTransforms();
            hit=WeaponFireResolver.Resolve(origin,Vector3.back,origin,10,.5f,1,session.Player,(1<<8)|1);
            Assert.That(hit.MuzzleObstructed);Assert.That(hit.Collider.gameObject,Is.EqualTo(wall));Assert.That(health.DamageTransactions,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator ProfileLiveAndMixedPopulationAndWarmedDamage()
        {
            yield return Load(); Place(new Vector3(-8,0,12));
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/LS_Zombie_Runtime.prefab");
            var report=new StringBuilder("Editor smoke. Mean marker totals; overlapping nested CPU markers. Direct GC covers synchronous owned paths only.\n");
            foreach(int liveCount in new[]{1,10})
            {
                var actors=new List<ZombieController>(); var corpses=new List<ZombieController>();
                for(int i=0;i<liveCount*2;i++)
                {
                    var go=Object.Instantiate(prefab,new Vector3(-9+(i%5),0,-7+(i/5)),Quaternion.identity); owned.Add(go);
                    var actor=go.GetComponent<ZombieController>(); Assert.That(actor.Initialize()); Assert.That(actor.Bind(session.Player));
                    if(i>=liveCount){go.GetComponent<ZombieHealth>().TakeDamage(new DamageInfo{Amount=1000});corpses.Add(actor);} else actors.Add(actor);
                }
                yield return new WaitForSeconds(2.5f);
                foreach(var actor in actors)actor.MeasureManagedAllocations=true;
                using(var ai=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"LastSignal.Zombie.AI",1))
                using(var perception=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"LastSignal.Zombie.Perception",1))
                using(var nav=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"LastSignal.Zombie.Navigation",1))
                using(var animation=ProfilerRecorder.StartNew(ProfilerCategory.Animation,"Animators.Update",1))
                {
                    long a=0,p=0,n=0,an=0;int frames=0;float elapsed=0;
                    while(elapsed<5||frames<300){yield return null;frames++;elapsed+=Time.unscaledDeltaTime;a+=ai.LastValue;p+=perception.LastValue;n+=nav.LastValue;an+=animation.LastValue;}
                    long bytes=0;foreach(var actor in actors)bytes+=actor.ManagedTickBytes;
                    foreach(var corpse in corpses){Assert.That(corpse.Perception.Raycasts,Is.Zero);Assert.That(corpse.Navigation.PathRequests,Is.Zero);Assert.That(corpse.GetComponent<ZombieAnimationPresenter>().CorpseSettled);}
                    report.AppendLine("live="+liveCount+" dead="+liveCount+" frames="+frames+" seconds="+elapsed+" AI_ms="+a/(frames*1e6)+" perception_ms="+p/(frames*1e6)+" nav_ms="+n/(frames*1e6)+" animator_ms="+an/(frames*1e6)+" valid="+ai.Valid+","+perception.Valid+","+nav.Valid+","+animation.Valid+" direct_AI_bytes="+bytes);
                    Assert.That(bytes,Is.Zero);
                }
                foreach(var actor in actors){owned.Remove(actor.gameObject);Object.DestroyImmediate(actor.gameObject);}foreach(var corpse in corpses){owned.Remove(corpse.gameObject);Object.DestroyImmediate(corpse.gameObject);}
            }
            // Warm actual ray -> region -> health; tiny legal positive damage avoids destruction during the allocation sample.
            var region=Region(DamageRegion.Body);var origin=region.HitCollider.bounds.center+Vector3.forward*3;var direction=(region.HitCollider.bounds.center-origin).normalized;
            zombie.SetPaused(true); health.SetDamageEnabled(true); Physics.SyncTransforms();
            for(int i=0;i<32;i++)WeaponFireResolver.Resolve(origin,direction,origin,10,.5f,.001f,session.Player,1<<8);
            int before=health.DamageTransactions;long start=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)WeaponFireResolver.Resolve(origin,direction,origin,10,.5f,.001f,session.Player,1<<8);
            long damageBytes=GC.GetAllocatedBytesForCurrentThread()-start;
            Assert.That(health.DamageTransactions-before,Is.EqualTo(1000));Assert.That(damageBytes,Is.Zero);
            report.AppendLine("warmed_resolver_hits=1000 direct_damage_bytes="+damageBytes);File.WriteAllText(Evidence+"/performance.txt",report.ToString());
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {foreach(var go in owned)if(go)Object.DestroyImmediate(go);owned.Clear();if(session)session.ReturnToMenu();Time.timeScale=1;yield return null;}
    }
}
#endif
