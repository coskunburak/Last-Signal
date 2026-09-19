#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LastSignal.Tests
{
    public class ZombiePresentationTests
    {
        GameObject instance;
        Animator animator;
        [SetUp]
        public void SetUp()
        {
            instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Enemies/Zombie/Prefabs/LS_Zombie_Shambler.prefab"));
            animator = instance.GetComponentInChildren<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Update(0);
        }
        [TearDown] public void TearDown() { if (instance) Object.DestroyImmediate(instance); }

        [Test]
        public void EveryPresentationStateAnimatesWithoutMovingActorRoot()
        {
            foreach (var state in new[] { "Idle", "Locomotion", "Attack", "HitReact", "Death" })
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
        public void DeathHoldsFinalPoseAndClearsGroundOnProductionController()
        {
            animator.Play("Death", 0, 0); animator.Update(0);
            for (int i = 0; i < 120; i++) animator.Update(1f / 60);
            var head = animator.GetBoneTransform(HumanBodyBones.Head).position;
            for (int i = 0; i < 120; i++) animator.Update(1f / 60);
            Assert.That(Vector3.Distance(head, animator.GetBoneTransform(HumanBodyBones.Head).position), Is.LessThan(.001f));
            var mesh = new Mesh();
            try
            {
                var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>(); skin.BakeMesh(mesh);
                foreach (var vertex in mesh.vertices) Assert.That(skin.transform.TransformPoint(vertex).y, Is.GreaterThan(-.01f));
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
#endif
