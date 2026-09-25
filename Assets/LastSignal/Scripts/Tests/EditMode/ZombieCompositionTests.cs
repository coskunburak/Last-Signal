using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Tests
{
    public class ZombieCompositionTests
    {
        [Test] public void RuntimeWrapperHasOneAuthorityAndNoMissingComponents()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/LS_Zombie_Runtime.prefab");
            Assert.That(prefab,Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<ZombieController>().Length,Is.EqualTo(1));
            Assert.That(prefab.GetComponent<ZombiePerception>(),Is.Not.Null);
            Assert.That(prefab.GetComponent<ZombieNavigation>(),Is.Not.Null);
            Assert.That(prefab.GetComponent<ZombieAnimationPresenter>(),Is.Not.Null);
            Assert.That(prefab.GetComponent<NavMeshAgent>().enabled,Is.False,"Encounter validates spawn before enabling navigation");
            Assert.That(prefab.GetComponentInChildren<Animator>().applyRootMotion,Is.False);
            foreach(var component in prefab.GetComponentsInChildren<Component>(true))Assert.That(component,Is.Not.Null);
        }
        [Test] public void NormalBakeDoesNotIncludeOtherOpenAcceptanceScenes()
        {
            var data=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/LastSignal/Enemies/Zombie/NormalNavMesh.asset");
            Assert.That(data,Is.Not.Null);
            // Normal ground is 40x40 at the origin, with targets up to z=20.
            // The parkour scene extends to z=30 and must never contaminate this bake.
            Assert.That(data.sourceBounds.max.z,Is.LessThan(21));
            Assert.That(data.sourceBounds.min.z,Is.GreaterThan(-21));
            Assert.That(data.sourceBounds.max.y,Is.LessThan(3));
        }
    }
}
