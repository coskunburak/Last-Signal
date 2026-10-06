using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LastSignal.Art.Editor
{
 public static class S013Finalization
 {
  public static void Apply()
  {
   var root=GameObject.Find("S013 Production Cabin");
   foreach(var t in root.GetComponentsInChildren<TextMesh>())if(t.name.EndsWith(" label"))Object.DestroyImmediate(t.gameObject);
   var g=GameObject.Find("Cabin soil clearing");int n=128;var v=new Vector3[n*2];var uv=new Vector2[n*2];var tris=new int[n*6];
   for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,x=Mathf.Cos(a),z=Mathf.Sin(a),inner=3.25f/Mathf.Max(Mathf.Abs(x),Mathf.Abs(z));v[i*2]=new Vector3(x*inner,.008f,z*inner);v[i*2+1]=new Vector3(x*22,.008f,z*19.5f);uv[i*2]=new Vector2(v[i*2].x/44,v[i*2].z/39);uv[i*2+1]=new Vector2(v[i*2+1].x/44,v[i*2+1].z/39);int j=(i+1)%n,k=i*6;tris[k]=i*2;tris[k+1]=j*2+1;tris[k+2]=i*2+1;tris[k+3]=i*2;tris[k+4]=j*2;tris[k+5]=j*2+1;}
   var mesh=new Mesh{name="Cabin clearing with foundation cutout",vertices=v,uv=uv,triangles=tris};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,S013ProductionAuthoring.Root+"/Meshes/Clearing.asset");g.transform.localPosition=Vector3.zero;g.transform.localScale=Vector3.one;g.GetComponent<MeshFilter>().sharedMesh=mesh;
   var path=GameObject.Find("Entry path");path.transform.localPosition=new Vector3(13.5f,.014f,1);path.transform.localScale=new Vector3(15,.016f,2);var mat=path.GetComponent<Renderer>().sharedMaterial;mat.SetTextureScale("_BaseMap",new Vector2(1,4));EditorUtility.SetDirty(mat);
   foreach(var name in new[]{"Pine","Birch","Rock"})
   {
    var p=S013ProductionAuthoring.Root+"/Prefabs/"+name+".prefab";var asset=PrefabUtility.LoadPrefabContents(p);
    if(name=="Rock"){var c=asset.AddComponent<BoxCollider>();var b=asset.GetComponentInChildren<Renderer>().bounds;c.center=asset.transform.InverseTransformPoint(b.center);c.size=b.size*.8f;}
    else {var c=asset.AddComponent<CapsuleCollider>();c.radius=name=="Pine"?.65f:.26f;c.height=4;c.center=new Vector3(0,2,0);}
    PrefabUtility.SaveAsPrefabAsset(asset,p);PrefabUtility.UnloadPrefabContents(asset);
   }
   EditorSceneManager.MarkSceneDirty(g.scene);EditorSceneManager.SaveScene(g.scene);AssetDatabase.SaveAssets();
  }
  public static void Capture(string directory)
  {
   System.IO.Directory.CreateDirectory(directory);
   foreach(var c in GameObject.Find("S013 Benchmark Cameras").GetComponentsInChildren<Camera>())
   {var rt=RenderTexture.GetTemporary(1920,1080,24);var old=RenderTexture.active;var target=c.targetTexture;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory,c.name+".png"),tex.EncodeToPNG());}finally{c.targetTexture=target;RenderTexture.active=old;Object.DestroyImmediate(tex);RenderTexture.ReleaseTemporary(rt);}}
  }
 }
}
