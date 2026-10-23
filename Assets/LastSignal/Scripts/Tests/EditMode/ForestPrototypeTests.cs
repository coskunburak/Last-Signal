using System.Linq;
using LastSignal.Editor.FirstTownForest;
using LastSignal.Forest;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LastSignal.Tests
{
    public sealed class ForestPrototypeTests
    {
        ForestPrototypeSettings settings;
        [SetUp] public void Setup() => settings = ScriptableObject.CreateInstance<ForestPrototypeSettings>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(settings);

        [Test] public void CanopyIsDeterministicSpacedAndKeepsTrailAndClearingOpen()
        {
            var first = settings.SampleCanopy(); var second = settings.SampleCanopy();
            CollectionAssert.AreEqual(first, second);
            Assert.That(first.Count, Is.InRange(120, settings.canopyBudget));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(settings.TrailDistance(first[i]), Is.GreaterThanOrEqualTo(2.5f));
                Assert.That(settings.ClearingDistance(first[i]), Is.GreaterThanOrEqualTo(1));
                Assert.That(settings.Excluded(first[i], .6f), Is.False);
                for (int j = 0; j < i; j++) Assert.That(Vector2.Distance(first[i], first[j]), Is.GreaterThanOrEqualTo(settings.trunkSpacing - .001f));
            }
            settings.seed++;
            Assert.That(settings.SampleCanopy().SequenceEqual(first), Is.False);
        }
        [Test] public void ArtistExclusionsAndFloorBudgetChangesDoNotPerturbCanopyAuthority()
        {
            var original = settings.SampleCanopy(); settings.fernBudget = 0; settings.detailBudget = 0;
            CollectionAssert.AreEqual(original, settings.SampleCanopy());
            settings.exclusions = new[] { new Bounds(new Vector3(30, 0, 30), new Vector3(20, 50, 20)) };
            foreach (var p in settings.SampleCanopy()) Assert.That(settings.Excluded(p, .6f), Is.False);
        }
        [Test] public void TerrainIsVariedWalkableAndSurfaceWeightsAreNormalized()
        {
            float low = float.MaxValue, high = float.MinValue;
            for (int z = 2; z < 118; z += 2) for (int x = 2; x < 118; x += 2)
            {
                var p = new Vector2(x, z); float h = settings.Height(p); low = Mathf.Min(low, h); high = Mathf.Max(high, h);
                Assert.That(h, Is.InRange(0, 18));
                var w = settings.SurfaceWeights(p);
                Assert.That(w.x + w.y + w.z + w.w, Is.EqualTo(1).Within(.0001));
                for (int i = 0; i < 4; i++) Assert.That(w[i], Is.InRange(0, 1));
            }
            Assert.That(high - low, Is.InRange(3, 12));
            for (int z = 6; z <= 114; z++) Assert.That(settings.Slope(new Vector2(settings.TrailX(z), z)), Is.LessThan(30));
        }
        [Test] public void AuthoredSceneHasOwnedCollisionLodsAndNoHarvestingOrRigidBodies()
        {
            var scene = SceneManager.GetSceneByPath(ForestPrototypeAuthoring.ScenePath); bool opened = !scene.IsValid();
            if (opened) scene = EditorSceneManager.OpenScene(ForestPrototypeAuthoring.ScenePath, OpenSceneMode.Additive);
            try
            {
                var state = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ForestPrototypeState>()).Single();
                Assert.That(state.terrain.terrainData.size, Is.EqualTo(new Vector3(120, 18, 120)));
                Assert.That(state.terrain.GetComponent<TerrainCollider>().terrainData, Is.SameAs(state.terrain.terrainData));
                Assert.That(state.terrain.terrainData.terrainLayers.Length, Is.EqualTo(4));
                Assert.That(state.canopy.childCount, Is.EqualTo(state.canopyCount));
                Assert.That(state.generated.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
                Assert.That(state.generated.GetComponentsInChildren<LastSignal.Gathering.ResourceNodeView>(true), Is.Empty);
                Assert.That(state.shots.Length, Is.EqualTo(6));
                foreach (Transform tree in state.canopy)
                {
                    Assert.That(tree.GetComponent<CapsuleCollider>(), Is.Not.Null);
                    var group = tree.GetComponentInChildren<LODGroup>(); Assert.That(group.GetLODs().Length, Is.EqualTo(4));
                    long previous = long.MaxValue;
                    foreach (var lod in group.GetLODs())
                    {
                        long triangles = 0;
                        foreach (var r in lod.renderers)
                        {
                            Assert.That(r, Is.Not.Null); var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                            for (int i = 0; i < mesh.subMeshCount; i++) triangles += mesh.GetIndexCount(i) / 3;
                        }
                        Assert.That(triangles, Is.LessThan(previous)); previous = triangles;
                    }
                    Assert.That(tree.position.y, Is.EqualTo(state.terrain.SampleHeight(tree.position) - .035f).Within(.005));
                }
                foreach (var filter in state.generated.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh; long count = 0;
                    for (int i = 0; i < mesh.subMeshCount; i++) count += mesh.GetIndexCount(i) / 3;
                    Assert.That(count, Is.LessThanOrEqualTo(180000), filter.name + " must never use the original high-poly fir.");
                }
                foreach (string name in new[] { "Woodland_Mask", "Fern_Mask" })
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(state.generationFolder + "/Textures/" + name + ".png");
                    Assert.That(importer, Is.Not.Null); Assert.That(importer.sRGBTexture, Is.False); Assert.That(importer.isReadable, Is.False);
                }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
