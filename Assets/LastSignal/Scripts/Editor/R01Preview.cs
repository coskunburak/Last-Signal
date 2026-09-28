using System.IO;
using UnityEditor;
using UnityEngine;
namespace LastSignal.EditorTools
{
    public static class R01Preview
    {
        public static void Run()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            var camera=new GameObject("PreviewCamera").AddComponent<Camera>();camera.nearClipPlane=.03f;camera.fieldOfView=70;camera.backgroundColor=new Color(.22f,.26f,.28f);camera.clearFlags=CameraClearFlags.SolidColor;
            var light=new GameObject("Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.transform.rotation=Quaternion.Euler(35,-30,0);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Combat/Crowbar/Crowbar_Viewmodel.prefab"));
            model.transform.SetParent(camera.transform,false);
            var animator=model.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/LastSignal/Combat/Crowbar/Crowbar_Idle.anim");clip.SampleAnimation(animator.gameObject,0);
            var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes("Docs/Implementation/PreS010-Recovery/Evidence/R01/20260926/crowbar-preview.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);
        }
    }
}
