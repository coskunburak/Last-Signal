using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LastSignal.Atmosphere;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace LastSignal.Editor.FirstTown
{
    public static class FirstTownAtmospherePolish
    {
        const string Root="Assets/LastSignal/Art/FirstTownAtmosphere/";
        [MenuItem("Last Signal/First Town/Atmosphere/Add surface and fixture polish (no bake)")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit PlayMode.");
            var scene=SceneManager.GetActiveScene();if(scene.path!=FirstTownAuthoring.ScenePath)throw new InvalidOperationException("Open FirstTownCore.");
            var d=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TownAtmosphere>(true)).Single();
            if(d.transform.Find("Selective road puddles"))throw new InvalidOperationException("Polish already installed.");
            var mat=new Material(Shader.Find("HDRP/Lit")){name="Road puddle",renderQueue=3000};
            mat.SetFloat("_SurfaceType",1);mat.SetFloat("_BlendMode",0);mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha",1);mat.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);mat.SetFloat("_ZWrite",0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetOverrideTag("RenderType","Transparent");mat.SetShaderPassEnabled("ShadowCaster",false);
            mat.SetColor("_BaseColor",new Color(.13f,.17f,.18f,.46f));mat.SetFloat("_Smoothness",.79f);mat.SetFloat("_Metallic",0);
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(mat);
            AssetDatabase.CreateAsset(mat,Root+"Puddle.mat");
            var mesh=new Mesh {name="Shallow irregular puddle"};var vertices=new Vector3[17];var uv=new Vector2[17];var triangles=new int[48];uv[0]=new Vector2(.5f,.5f);
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8,r=.85f+.13f*Mathf.Sin(i*2.7f);vertices[i+1]=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);uv[i+1]=new Vector2(vertices[i+1].x*.5f+.5f,vertices[i+1].z*.5f+.5f);triangles[i*3]=0;triangles[i*3+1]=(i+1)%16+1;triangles[i*3+2]=i+1;}
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Root+"PuddleMesh.asset");
            var group=new GameObject("Selective road puddles");group.transform.SetParent(d.transform);var renderers=d.wetRenderers.ToList();
            var spots=new[]{new Vector2(-3,-96),new Vector2(3,-39),new Vector2(-3,22),new Vector2(3,92),new Vector2(3,162),new Vector2(55,-137),new Vector2(88,-143),new Vector2(155,-137),new Vector2(-52,-3),new Vector2(117,40),new Vector2(123,120),new Vector2(-160,-137)};
            Physics.SyncTransforms();
            foreach(var p in spots)
            {
                if(!Physics.Raycast(new Vector3(p.x,20,p.y),Vector3.down,out var hit,21,~((1<<8)|(1<<2)),QueryTriggerInteraction.Ignore) || hit.point.y>.3f || hit.normal.y<.98f)continue;
                var g=new GameObject("Road depression "+p);g.transform.SetParent(group.transform);g.transform.position=new Vector3(p.x,Mathf.Max(.035f,hit.point.y+.015f),p.y);g.transform.localScale=new Vector3(1.5f,1,.75f);
                g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.enabled=false;
                // Enabled by the coordinator only once wet; no collider or navigation change.
                renderers.Add(r);
            }
            d.wetRenderers=renderers.ToArray();d.surfaces=d.surfaces.Concat(new[]{new TownAtmosphere.Surface {material=mat,darkening=0,smoothness=.79f,wetOnly=true}}).ToArray();
            var pole=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LastSignal/Art/FirstTown/Prefabs/LightPole.prefab");
            foreach(var lamp in d.lamps)
            {
                if(!lamp.light || !lamp.light.transform.IsChildOf(d.transform))continue;
                var fixture=lamp.light.transform.Find("Fixture");if(fixture)fixture.localRotation=Quaternion.Euler(90,0,0);
                if(lamp.interior || !pole)continue;
                var g=(GameObject)PrefabUtility.InstantiatePrefab(pole,d.transform);g.name=lamp.light.name+" pole";
                g.transform.position=new Vector3(lamp.light.transform.position.x,0,lamp.light.transform.position.z);
                var rs=g.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                g.transform.localScale*=lamp.light.transform.position.y/Mathf.Max(.1f,b.size.y);
                b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);g.transform.position-=Vector3.up*b.min.y;
                foreach(var c in g.GetComponentsInChildren<Collider>())c.enabled=false;
            }
            var fixtureMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Fixture.mat");
            fixtureMaterial.EnableKeyword("_EMISSION");fixtureMaterial.SetColor("_EmissionColor",new Color(1.1f,.62f,.28f));EditorUtility.SetDirty(fixtureMaterial);
            var rain=AssetDatabase.LoadAssetAtPath<Material>(Root+"RainStreak.mat");rain.SetColor("_BaseColor",new Color(.8f,.86f,.9f,.7f));EditorUtility.SetDirty(rain);
            var splash=AssetDatabase.LoadAssetAtPath<Material>(Root+"RainSplash.mat");splash.SetColor("_BaseColor",new Color(.8f,.86f,.9f,.85f));EditorUtility.SetDirty(splash);
            EditorUtility.SetDirty(d);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Docs/Implementation/FirstTownAtmosphere/polish.json","{\"status\":\"IMPLEMENTED\",\"puddles\":"+group.transform.childCount+",\"visual\":\"NOT_RUN\"}");
        }
    }
}
