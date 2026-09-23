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
    public class ZombieMeleeTests
    {
        const string Evidence = "Docs/Implementation/S004/Evidence/20260919-P4/regression-P3";
        SessionFlow session;
        ZombieController zombie;
        PlayerHealth health;
        readonly List<GameObject> owned = new List<GameObject>();
        IEnumerator Load(string scene = "Assets/LastSignal/Scenes/ZombieAcceptance.unity")
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            session = Object.FindAnyObjectByType<SessionFlow>(); session.Resume();
            session.Player.GetComponent<FirstPersonMotor>().enabled = false;
            health = session.Player.GetComponent<PlayerHealth>();
            zombie = session.GetComponent<ZombieEncounter>().Actor;
            Assert.That(health, Is.Not.Null);
            if (scene.Contains("ZombieAcceptance"))
            {
                zombie.GetComponent<NavMeshAgent>().Warp(new Vector3(-8, 0, 0));
                zombie.transform.rotation = Quaternion.identity;
                Place(new Vector3(-8, 0, 1.25f));
            }
        }
        void Place(Vector3 position)
        {
            var c = session.Player.GetComponent<CharacterController>(); c.enabled = false;
            session.Player.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.ProjectOnPlane(zombie.transform.position - position, Vector3.up)));
            c.enabled = true; Physics.SyncTransforms();
        }
        IEnumerator Until(Func<bool> condition, float limit = 5)
        {
            float end = Time.realtimeSinceStartup + limit;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(condition(), Is.True, "Timeout: " + zombie.Runtime.State + " t=" + zombie.AttackTime + " result=" + zombie.LastMeleeResult);
        }
        void Capture(string label)
        {
            Directory.CreateDirectory(Evidence + "/visual");
            var view = session.Player.GetComponent<FirstPersonLook>().View;
            CaptureCamera(view, label + "-fps");
            var go = new GameObject("OwnedCombatEvidenceCamera"); owned.Add(go);
            var cam = go.AddComponent<Camera>(); cam.enabled = false;
            cam.transform.position = zombie.transform.position + new Vector3(3, 1.8f, 2.5f);
            cam.transform.LookAt(zombie.transform.position + new Vector3(0, 1, .6f));
            // World body has a pre-existing pink material; use a clear geometric capsule in external evidence instead.
            cam.cullingMask &= ~((1 << 30) | (1 << 29));
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule); owned.Add(capsule);
            capsule.name = "OwnedPlayerCapsuleVisualization";
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            var body = session.Player.GetComponent<CharacterController>();
            capsule.transform.position = session.Player.transform.TransformPoint(body.center);
            capsule.transform.localScale = new Vector3(body.radius * 2, body.height / 2, body.radius * 2);
            CaptureCamera(cam, label + "-side");
            Object.DestroyImmediate(go); owned.Remove(go); Object.DestroyImmediate(capsule); owned.Remove(capsule);
            File.AppendAllText(Evidence + "/visual/trace.txt", label + " seq=" + zombie.AttackSequence + " state=" + zombie.Runtime.State +
                " timer=" + zombie.AttackTime + " result=" + zombie.LastMeleeResult + " attempts=" + zombie.ContactAttempts +
                " hits=" + zombie.SuccessfulHits + " health=" + health.CurrentHealth + " distance=" + zombie.LastContactDistance +
                " angle=" + zombie.LastContactAngle + " facing=" + zombie.transform.forward + "\n");
        }
        void CaptureCamera(Camera cam, string name)
        {
            var rt = RenderTexture.GetTemporary(960, 640, 24); var old = RenderTexture.active; var target = cam.targetTexture;
            var texture = new Texture2D(960, 640, TextureFormat.RGB24, false);
            try { cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, 960, 640), 0, 0); texture.Apply(); File.WriteAllBytes(Evidence + "/visual/" + name + ".png", texture.EncodeToPNG()); }
            finally { cam.targetTexture = target; RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(texture); }
        }
        [UnityTest] public IEnumerator ValidStrikeHasOneTransactionAndVisibleRecovery()
        {
            yield return Load();
            yield return Until(() => zombie.Runtime.State == ZombieState.AttackWindup);
            Capture("hit-start"); Assert.That(health.DamageTransactions, Is.Zero);
            yield return Until(() => zombie.Runtime.State == ZombieState.AttackCommit);
            Assert.That(health.DamageTransactions, Is.Zero); Capture("hit-commit");
            yield return Until(() => zombie.ContactConsumed);
            Assert.That(zombie.LastMeleeResult, Is.EqualTo(MeleeResult.Hit));
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth - zombie.Definition.AttackDamage));
            Assert.That(health.DamageTransactions, Is.EqualTo(1)); Capture("hit-contact");
            var animator = zombie.GetComponentInChildren<Animator>();
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"), Is.True);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(zombie.AttackTime / zombie.Definition.AttackDuration).Within(.03f));
            yield return Until(() => zombie.Runtime.State == ZombieState.Recovering);
            ulong sequence = zombie.AttackSequence; Capture("hit-recovery");
            yield return new WaitForSeconds(.5f);
            Assert.That(zombie.Runtime.State, Is.EqualTo(ZombieState.Recovering));
            Assert.That(zombie.AttackSequence, Is.EqualTo(sequence)); Assert.That(health.DamageTransactions, Is.EqualTo(1));
            yield return Until(() => zombie.AttackSequence > sequence);
            Assert.That(health.DamageTransactions, Is.EqualTo(1)); Capture("hit-next-windup");
        }
        [UnityTest] public IEnumerator BackstepSidestepRearAndWallReallyMiss()
        {
            foreach (string scenario in new[] { "backstep", "sidestep", "rear", "wall" })
            {
                yield return Load(); yield return Until(() => zombie.Runtime.State == ZombieState.AttackCommit);
                var forward = zombie.transform.forward; var rotation = zombie.transform.rotation;
                if (scenario == "backstep") Place(zombie.transform.position + forward * 2.1f);
                if (scenario == "sidestep") Place(zombie.transform.position + forward * 1.05f + zombie.transform.right * .9f);
                if (scenario == "rear") Place(zombie.transform.position - forward * 1.1f);
                GameObject wall = null;
                if (scenario == "wall")
                {
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube); owned.Add(wall); wall.name = "OwnedContactBlocker";
                    wall.transform.position = zombie.transform.position + forward * .6f + Vector3.up;
                    wall.transform.localScale = new Vector3(2, 2, .12f); Physics.SyncTransforms();
                }
                yield return Until(() => zombie.ContactConsumed); Capture(scenario + "-contact");
                Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth)); Assert.That(zombie.ContactAttempts, Is.EqualTo(1));
                Assert.That(Quaternion.Angle(rotation, zombie.transform.rotation), Is.LessThan(.01f));
                Assert.That(zombie.LastMeleeResult, Is.EqualTo(scenario == "wall" ? MeleeResult.Occluded : scenario == "backstep" ? MeleeResult.OutOfRange : MeleeResult.OutsideArc));
                yield return Until(() => zombie.Runtime.State == ZombieState.Recovering); Capture(scenario + "-recovery");
                if (scenario == "wall") { yield return Until(() => zombie.Runtime.State == ZombieState.Searching); Object.DestroyImmediate(wall); owned.Remove(wall); }
                else if (scenario == "backstep") yield return Until(() => zombie.Runtime.State == ZombieState.Chasing);
                session.ReturnToMenu(); yield return null;
            }
        }
        [UnityTest] public IEnumerator PauseBothSidesOfCommitFreezesDamageAndResumesOnce()
        {
            foreach (var state in new[] { ZombieState.AttackWindup, ZombieState.AttackCommit })
            {
                yield return Load(); yield return Until(() => zombie.Runtime.State == state);
                session.Pause(); float timer = zombie.AttackTime;
                var animator = zombie.GetComponentInChildren<Animator>(); float normalized = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                yield return new WaitForSecondsRealtime(1.8f);
                Assert.That(zombie.AttackTime, Is.EqualTo(timer)); Assert.That(health.DamageTransactions, Is.Zero);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(normalized)); Capture("pause-" + state);
                session.Resume(); yield return Until(() => zombie.ContactConsumed);
                Assert.That(health.DamageTransactions, Is.EqualTo(1));
                session.ReturnToMenu(); yield return null;
            }
        }
        [UnityTest] public IEnumerator DeathIsTerminalAndRestartUsesFreshPlayerAndAttack()
        {
            yield return Load(); int deaths = 0; health.Died += () => deaths++;
            yield return Until(() => !health.IsAlive, 12); Capture("death");
            Assert.That(deaths, Is.EqualTo(1)); Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.DamageTransactions, Is.EqualTo(5));
            yield return null; Assert.That(zombie.Target, Is.Null);
            session.Pause(); session.Resume(); yield return null;
            Assert.That(session.Player.GetComponent<PlayerInputReader>().GameplayActive, Is.False);
            Assert.That(session.Player.GetComponent<PlayerCombatController>().ActiveWeapon, Is.Null);
            var oldPlayer = session.Player; session.ReturnToMenu(); yield return null; session.BeginSession(); session.Resume(); yield return null;
            Assert.That(session.Player != oldPlayer, Is.True);
            health = session.Player.GetComponent<PlayerHealth>(); zombie = session.GetComponent<ZombieEncounter>().Actor;
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth)); Assert.That(zombie.AttackSequence, Is.Zero);
            Assert.That(zombie.Target, Is.EqualTo(session.Player)); Capture("restart");
        }
        [UnityTest] public IEnumerator MenuDuringWindupCancelsWithoutDelayedDamage()
        {
            yield return Load(); yield return Until(() => zombie.Runtime.State == ZombieState.AttackWindup);
            var old = zombie; session.ReturnToMenu(); Assert.That(old.Target, Is.Null); Assert.That(old.AttackTime, Is.Zero);
            Assert.That(old.AttackSequence, Is.Zero); Assert.That(old.ContactConsumed, Is.False);
            yield return new WaitForSecondsRealtime(.6f); Assert.That(old == null, Is.True);
            session.BeginSession(); session.Resume(); yield return null;
            Assert.That(session.Player.GetComponent<PlayerHealth>().DamageTransactions, Is.Zero);
        }
        [UnityTest] public IEnumerator TargetDeathOrDisableDuringWindupCannotDamage()
        {
            foreach (bool death in new[] { true, false })
            {
                yield return Load(); yield return Until(() => zombie.Runtime.State == ZombieState.AttackWindup);
                if (death) health.TakeDamage(new DamageInfo { Amount = 1000 }); else session.Player.SetActive(false);
                yield return null; yield return null;
                Assert.That(zombie.Target, Is.Null); Assert.That(zombie.ContactAttempts, Is.Zero);
            }
        }
        [UnityTest] public IEnumerator NormalGameplayRouteApproachesAndAttacksProductionPlayer()
        {
            yield return Load("Assets/Scenes/SampleScene.unity");
            Place(zombie.transform.position + zombie.transform.forward * 3);
            yield return Until(() => zombie.Runtime.State == ZombieState.Chasing); Capture("normal-approach");
            yield return Until(() => health.DamageTransactions > 0, 10); Capture("normal-hit");
            Assert.That(health.DamageTransactions, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator LargeStepDuplicateColliderAndDisabledAnimatorStillHaveOneContact()
        {
            yield return Load(); yield return Until(() => zombie.Runtime.State == ZombieState.AttackWindup);
            var duplicate = session.Player.AddComponent<CapsuleCollider>(); duplicate.isTrigger = true;
            zombie.GetComponentInChildren<Animator>().enabled = false;
            zombie.Simulate(.9f);
            Assert.That(zombie.Runtime.State, Is.EqualTo(ZombieState.Recovering));
            Assert.That(health.DamageTransactions, Is.EqualTo(1)); Assert.That(zombie.ContactAttempts, Is.EqualTo(1));
            zombie.Simulate(.2f); Assert.That(health.DamageTransactions, Is.EqualTo(1));
            zombie.SetPaused(true); float timer = zombie.AttackTime;
            zombie.Simulate(5); Assert.That(zombie.AttackTime, Is.EqualTo(timer));
            Assert.That(health.DamageTransactions, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator HardAbortBeforeCommitAndDisabledActorCancelSequence()
        {
            yield return Load(); yield return Until(() => zombie.Runtime.State == ZombieState.AttackWindup);
            Place(zombie.transform.position + zombie.transform.forward * 4);
            yield return null; yield return null;
            Assert.That(zombie.Attacking, Is.False); Assert.That(health.DamageTransactions, Is.Zero);
            Assert.That(zombie.LastMeleeResult, Is.EqualTo(MeleeResult.Aborted));
            Place(zombie.transform.position + zombie.transform.forward * 1.25f);
            yield return Until(() => zombie.Runtime.State == ZombieState.AttackWindup);
            zombie.enabled = false; zombie.Simulate(5);
            Assert.That(zombie.Target, Is.Null); Assert.That(zombie.AttackTime, Is.Zero); Assert.That(health.DamageTransactions, Is.Zero);
        }
        [UnityTest] public IEnumerator ActualMotorSidestepAfterCommitMissesWithinReactionWindow()
        {
            yield return Load(); yield return Until(() => zombie.Runtime.State == ZombieState.AttackCommit);
            var motor = session.Player.GetComponent<FirstPersonMotor>();
            float end = zombie.Definition.AttackContactTime;
            while (!zombie.ContactConsumed && zombie.AttackTime <= end + .1f)
            { motor.Simulate(Vector2.right, false, Time.deltaTime); yield return null; }
            Assert.That(zombie.ContactConsumed, Is.True); Assert.That(health.DamageTransactions, Is.Zero);
            Assert.That(zombie.LastMeleeResult, Is.EqualTo(MeleeResult.OutsideArc)); Capture("motor-sidestep");
        }
        [UnityTest] public IEnumerator ProfileOneAndTenActiveMeleeActors()
        {
            Directory.CreateDirectory(Evidence);
            var report = new StringBuilder("Editor technical combat smoke; CPU marker totals, not standalone certification. Direct synchronous actor bytes include presentation.\n");
            foreach (int count in new[] { 1, 10 })
            {
                yield return Load(); session.GetComponent<ZombieEncounter>().End(); Place(new Vector3(-8, 0, 0));
                var serialized = new SerializedObject(health); serialized.FindProperty("maxHealth").floatValue = 10000;
                serialized.ApplyModifiedPropertiesWithoutUndo(); health.ResetForSession();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Enemies/Zombie/Prefabs/LS_Zombie_Runtime.prefab");
                var actors = new ZombieController[count];
                for (int i = 0; i < count; i++)
                {
                    Vector3 offset = Quaternion.Euler(0, i * 360f / count, 0) * Vector3.forward * 1.25f;
                    var go = Object.Instantiate(prefab, session.Player.transform.position + offset, Quaternion.LookRotation(-offset)); owned.Add(go);
                    actors[i] = go.GetComponent<ZombieController>(); Assert.That(actors[i].Initialize(), Is.True); Assert.That(actors[i].Bind(session.Player), Is.True);
                }
                zombie = actors[0]; yield return new WaitForSeconds(2.5f);
                using (var ai = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "LastSignal.Zombie.AI", 1))
                using (var attack = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "LastSignal.Zombie.Attack", 1))
                using (var validation = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "LastSignal.Zombie.MeleeValidation", 1))
                using (var animation = ProfilerRecorder.StartNew(ProfilerCategory.Animation, "Animators.Update", 1))
                {
                    foreach (var actor in actors) actor.MeasureManagedAllocations = true;
                    long a = 0, b = 0, c = 0, d = 0; int frames = 0; float elapsed = 0;
                    while (elapsed < 5 || frames < 300)
                    { yield return null; elapsed += Time.unscaledDeltaTime; frames++; a += ai.LastValue; b += attack.LastValue; c += validation.LastValue; d += animation.LastValue; }
                    long bytes = 0; int ticks = 0, hits = 0;
                    foreach (var actor in actors)
                    {
                        bytes += actor.ManagedTickBytes; ticks += actor.MeasuredTicks; hits += actor.SuccessfulHits;
                        Assert.That(actor.ContactAttempts, Is.LessThanOrEqualTo((int)actor.AttackSequence));
                        Assert.That(actor.SuccessfulHits, Is.GreaterThan(1));
                    }
                    Assert.That(health.DamageTransactions, Is.EqualTo(hits));
                    report.AppendLine("actors=" + count + " frames=" + frames + " seconds=" + elapsed + " AI_ms=" + a / (frames * 1e6) +
                        " attack_ms=" + b / (frames * 1e6) + " validation_ms=" + c / (frames * 1e6) + " animator_ms=" + d / (frames * 1e6) +
                        " marker_valid=" + ai.Valid + "," + attack.Valid + "," + validation.Valid + "," + animation.Valid + " direct_bytes=" + bytes + " ticks=" + ticks + " transactions=" + hits);
                }
                foreach (var actor in actors) { owned.Remove(actor.gameObject); Object.DestroyImmediate(actor.gameObject); }
            }
            File.WriteAllText(Evidence + "/performance.txt", report.ToString());
        }
        [UnityTest] public IEnumerator RebindingDuringCommitCannotTransferOldStrike()
        {
            yield return Load(); yield return Until(() => zombie.Runtime.State == ZombieState.AttackCommit);
            var replacement = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Player.prefab"), session.Player.transform.position, session.Player.transform.rotation);
            owned.Add(replacement); var nextHealth = replacement.GetComponent<PlayerHealth>();
            Assert.That(zombie.Bind(replacement), Is.True);
            Assert.That(zombie.Runtime.State, Is.EqualTo(ZombieState.Idle)); Assert.That(zombie.AttackTime, Is.Zero);
            zombie.Simulate(.9f); // B has not accumulated enough acquisition evidence for a new attack.
            Assert.That(nextHealth.DamageTransactions, Is.Zero); Assert.That(health.DamageTransactions, Is.Zero);
            Assert.That(zombie.Target, Is.EqualTo(replacement));
        }
        [UnityTest] public IEnumerator ContactClockAndPoseAlignAtThirtySixtyAndOneTwentyHz()
        {
            var report = new StringBuilder("Deterministic controller steps; real production Animator; not a hardware FPS benchmark.\n");
            foreach (int hz in new[] { 30, 60, 120 })
            {
                yield return Load(); yield return Until(() => zombie.Runtime.State == ZombieState.AttackWindup);
                while (!zombie.ContactConsumed)
                {
                    float before = zombie.AttackTime;
                    zombie.Simulate(1f / hz);
                    if (zombie.AttackTime < zombie.Definition.AttackContactTime) Assert.That(health.DamageTransactions, Is.Zero);
                    Assert.That(zombie.AttackTime - before, Is.EqualTo(1f / hz).Within(.0001f));
                }
                var animator = zombie.GetComponentInChildren<Animator>();
                float visualTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime * zombie.Definition.AttackDuration;
                Assert.That(health.DamageTransactions, Is.EqualTo(1));
                Assert.That(zombie.AttackTime - zombie.Definition.AttackContactTime, Is.InRange(0, 1f / hz + .0001f));
                Assert.That(visualTime, Is.EqualTo(zombie.AttackTime).Within(.005f));
                report.AppendLine("Hz=" + hz + " contactStepTime=" + zombie.AttackTime + " visualTime=" + visualTime + " health=" + health.CurrentHealth + " transactions=" + health.DamageTransactions);
                session.ReturnToMenu(); yield return null;
            }
            File.WriteAllText(Evidence + "/contact-rate-audit.txt", report.ToString());
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach (var go in owned) if (go) Object.DestroyImmediate(go); owned.Clear();
            if (session) session.ReturnToMenu(); Time.timeScale = 1; yield return null;
        }
    }
}
#endif
