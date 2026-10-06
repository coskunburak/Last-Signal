using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LastSignal.Art.Editor;
namespace LastSignal.Art.Tests
{
 public class S013ProductionAssetTests
 {
  const string Root="Assets/LastSignal/Art/S013/Production/Prefabs/";
  [TestCase("Wall_2m")][TestCase("Wall_1m")][TestCase("Doorway_2m")][TestCase("Window_2m")][TestCase("Floor_2m")]
  public void StructuralModulesHaveCompleteCompatibleGeometryAndCollision(string name)
  {
   var p=AssetDatabase.LoadAssetAtPath<GameObject>(Root+name+".prefab");Assert.IsNotNull(p);Assert.That(S013PrefabValidation.Inspect(p,true,true),Is.Empty);
   foreach(var f in p.GetComponentsInChildren<MeshFilter>()){var mesh=f.sharedMesh;Assert.AreEqual(mesh.vertexCount,mesh.normals.Length);Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);Assert.AreEqual(mesh.vertexCount,mesh.tangents.Length);foreach(var n in mesh.normals)Assert.That(n.sqrMagnitude,Is.InRange(.95f,1.05f));}
  }
  [Test] public void FloorAndWallUseContinuousTwoMetreSnap()
  {
   foreach(var name in new[]{"Wall_2m","Floor_2m"})
   {var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+name+".prefab");var g=Object.Instantiate(asset);try{var b=g.GetComponentsInChildren<Collider>()[0].bounds;Assert.That(b.size.x,Is.EqualTo(2).Within(.001));Assert.That(b.min.x,Is.EqualTo(0).Within(.001));Assert.That(b.max.x,Is.EqualTo(2).Within(.001));}finally{Object.DestroyImmediate(g);}}
  }
  static string[] Ids(Scene scene)
  {
   var ids=new List<string>();foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))
   {if(!c)continue;var serialized=new SerializedObject(c);foreach(string name in new[]{"stableId","persistentId","socketId"}){var p=serialized.FindProperty(name);if(p!=null&&p.propertyType==SerializedPropertyType.String&&!string.IsNullOrEmpty(p.stringValue))ids.Add(c.GetType().FullName+"/"+name+"="+p.stringValue);}}
   return ids.OrderBy(x=>x).ToArray();
  }
  [Test] public void ProductionScenePreservesExistingPersistenceAndSocketIdentities()
  {
   var a=EditorSceneManager.OpenScene("Assets/LastSignal/Scenes/Production/IntegratedGraybox.unity",OpenSceneMode.Additive);var b=EditorSceneManager.OpenScene(S013ProductionAuthoring.ScenePath,OpenSceneMode.Additive);
   try{var before=Ids(a);Assert.That(before.Length,Is.GreaterThan(5));CollectionAssert.AreEqual(before,Ids(b));}finally{EditorSceneManager.CloseScene(b,true);EditorSceneManager.CloseScene(a,true);}
  }
 }
}
