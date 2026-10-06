#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombiePresentationTests
    {
        GameObject instance;
        GameObject gameplayRoot;
        Animator animator;
        [SetUp]
        public void SetUp()
        {
            instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Prefabs/Enemies/Zombie/LS_Zombie_Shirtless_Visual.prefab"));
            gameplayRoot = new GameObject("OwnedZombieGameplayRoot");
            instance.transform.SetParent(gameplayRoot.transform, false);
            animator = instance.GetComponentInChildren<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Update(0);
        }
        [TearDown] public void TearDown() { if (gameplayRoot) Object.DestroyImmediate(gameplayRoot); }

        [Test]
        public void EveryPresentationStateAnimatesWithoutMovingActorRoot()
        {
            foreach (var state in new[] { "Idle", "Locomotion", "Run", "Attack", "AttackAlternate",
                "HitReact", "Death", "FlyingBackDeath" })
            {
                animator.Play(state, 0, 0); animator.Update(0);
                var initial = animator.GetBoneTransform(HumanBodyBones.Head).position;
                float movement = 0;
                for (int i = 0; i < 180; i++)
                {
                    animator.Update(1f / 60);
                    var head = animator.GetBoneTransform(HumanBodyBones.Head).position;
                    movement = Mathf.Max(movement, Vector3.Distance(initial, head));
                    Assert.That(float.IsNaN(head.sqrMagnitude) || float.IsInfinity(head.sqrMagnitude), Is.False);
                    Assert.That(animator.transform.localPosition.sqrMagnitude, Is.LessThan(.000001f));
                    Assert.That(instance.transform.position, Is.EqualTo(Vector3.zero));
                }
                Assert.That(movement, Is.GreaterThan(.01f), state + " must visibly animate");
            }
        }

        [Test]
        public void DeathHoldsFinalPoseAndClearsGroundOnProductionController() => AssertGroundedDeath(false);

        [Test]
        public void HeavyFrontalTorsoDeathUsesFlyingBackAndKeepsGameplayRootFixed() => AssertGroundedDeath(true);

        [Test]
        public void HeadSeverUsesFlyingBackAndKeepsGameplayRootFixed() => AssertGroundedDeath(false, true);

        [Test]
        public void SideBodyHitUsesAuthoredUpperBodyClipAndReturnsToIdle()
        {
            var presenter = instance.AddComponent<ZombieAnimationPresenter>();
            presenter.Configure(animator);
            var definition = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(
                "Assets/LastSignal/Data/Enemies/Zombie/Shambler.asset");
            Assert.That(presenter.Initialize(definition), Is.True);
            var impact = ZombieImpactReaction.Select(new DamageInfo { Amount = 30,
                Direction = Vector3.right, BodyPart = ZombieBodyPart.Torso },
                Quaternion.identity, 100, false);
            Assert.That(impact.Side, Is.EqualTo(ZombieImpactSide.Left));
            Assert.That(impact.UseLegacyClip, Is.True);
            presenter.BeginDamage(false, impact);
            presenter.AdvanceDamage(.12f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True);
            Assert.That(animator.GetCurrentAnimatorStateInfo(1).IsName("HitReact"), Is.True);
            Assert.That(animator.GetLayerWeight(1), Is.GreaterThan(.5f));
            Assert.That(gameplayRoot.transform.position, Is.EqualTo(Vector3.zero));
            presenter.EndReaction();
            Assert.That(animator.GetLayerWeight(1), Is.Zero);
        }

        [Test]
        public void WalkingBodyReactionKeepsClipAtRealTimeWhileLegsKeepWalking()
        {
            var presenter = instance.AddComponent<ZombieAnimationPresenter>();
            presenter.Configure(animator);
            var definition = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(
                "Assets/LastSignal/Data/Enemies/Zombie/Shambler.asset");
            Assert.That(presenter.Initialize(definition), Is.True);
            presenter.Present(.92f, false, 1f, false);
            animator.Update(.2f);
            var impact = ZombieImpactReaction.Select(new DamageInfo { Amount = 30,
                Direction = Vector3.back, BodyPart = ZombieBodyPart.Torso },
                Quaternion.identity, 100, false);
            presenter.BeginDamage(false, impact);
            presenter.AdvanceDamage(.12f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
            var overlay = animator.GetCurrentAnimatorStateInfo(1);
            Assert.That(overlay.IsName("HitReact"), Is.True);
            Assert.That(animator.GetFloat("HitReactSpeed"),
                Is.EqualTo(definition.MeasuredWalkSpeed / .92f).Within(.02f));
            Assert.That(overlay.normalizedTime, Is.EqualTo(.12f / definition.HitReactDuration).Within(.08f));
            Assert.That(animator.GetLayerWeight(1), Is.GreaterThan(.5f));
        }

        [TestCase(false, AnimatorCullingMode.CullCompletely)]
        [TestCase(true, AnimatorCullingMode.CullCompletely)]
        [TestCase(false, AnimatorCullingMode.CullUpdateTransforms)]
        [TestCase(true, AnimatorCullingMode.CullUpdateTransforms)]
        public void AttackClockSurvivesPauseAndCullingPolicy(bool alternate, AnimatorCullingMode culling)
        {
            var presenter = instance.AddComponent<ZombieAnimationPresenter>();
            presenter.Configure(animator);
            var definition = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(
                "Assets/LastSignal/Data/Enemies/Zombie/Shambler.asset");
            Assert.IsTrue(presenter.Initialize(definition));
            animator.cullingMode = culling;
            presenter.BeginAttack(alternate);
            Assert.That(animator.cullingMode, Is.EqualTo(AnimatorCullingMode.AlwaysAnimate));
            Assert.IsFalse(animator.applyRootMotion);
            string state = alternate ? "AttackAlternate" : "Attack";
            float commit = definition.AttackCommitTimeFor(alternate);
            float contact = definition.AttackContactTimeFor(alternate);
            float duration = definition.AttackClipDurationFor(alternate);
            presenter.AdvanceAttack(commit);
            var pose = animator.GetCurrentAnimatorStateInfo(0);
            Assert.IsTrue(pose.IsName(state));
            Assert.That(pose.normalizedTime, Is.EqualTo(commit / duration).Within(.005f));
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.IsNotNull(hand);
            Vector3 pausedHand = hand.position;
            presenter.SetPaused(true);
            presenter.AdvanceAttack(1f);
            animator.Update(.2f); // Automatic Animator evaluation must also remain frozen.
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                Is.EqualTo(pose.normalizedTime).Within(.0001f));
            Assert.That(Vector3.Distance(hand.position, pausedHand), Is.LessThan(.0001f));
            presenter.SetPaused(false);
            presenter.AdvanceAttack(contact - commit);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                Is.EqualTo(contact / duration).Within(.005f));
            Assert.That(gameplayRoot.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(instance.transform.localPosition.sqrMagnitude, Is.LessThan(.000001f));
            Assert.That(animator.speed, Is.Zero, "Only the gameplay tick advances an active attack.");
            presenter.EndAttack();
            Assert.That(animator.cullingMode, Is.EqualTo(culling));
            Assert.That(animator.speed, Is.EqualTo(1f));
        }

        void AssertGroundedDeath(bool heavyFrontalTorso, bool headSever = false)
        {
            var presenter = instance.AddComponent<ZombieAnimationPresenter>();
            presenter.Configure(animator);
            var definition = AssetDatabase.LoadAssetAtPath<ZombieDefinition>(
                "Assets/LastSignal/Data/Enemies/Zombie/Shambler.asset");
            Assert.That(presenter.Initialize(definition), Is.True);
            var impact = headSever
                ? ZombieImpactReaction.Select(new DamageInfo { Amount = 40, Direction = Vector3.forward,
                    BodyPart = ZombieBodyPart.Head }, Quaternion.identity, 100, true)
                : heavyFrontalTorso
                    ? ZombieImpactReaction.Select(new DamageInfo { Amount = 30, Direction = Vector3.back,
                        BodyPart = ZombieBodyPart.Torso }, Quaternion.identity, 100, false)
                    : ZombieImpactReaction.Legacy;
            presenter.BeginDamage(true, impact);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(
                heavyFrontalTorso || headSever ? "FlyingBackDeath" : "Death"), Is.True);
            for (int i = 0; i < 240 && !presenter.CorpseSettled; i++) presenter.AdvanceDamage(1f / 60);
            Assert.That(presenter.CorpseSettled, Is.True);
            var head = animator.GetBoneTransform(HumanBodyBones.Head).position;
            for (int i = 0; i < 120; i++) presenter.AdvanceDamage(1f / 60);
            Assert.That(Vector3.Distance(head, animator.GetBoneTransform(HumanBodyBones.Head).position), Is.LessThan(.001f));
            var mesh = new Mesh();
            try
            {
                var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>(); skin.BakeMesh(mesh);
                foreach (var vertex in mesh.vertices) Assert.That(skin.transform.TransformPoint(vertex).y, Is.GreaterThan(-.01f));
                Assert.That(gameplayRoot.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
#endif
