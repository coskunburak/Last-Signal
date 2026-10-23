using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LastSignal.Editor
{
    /// <summary>Explicit local asset authoring only. Does not run tests, builds or enter Play mode.</summary>
    public static class ArberryCharacterAuthoring
    {
        public const string SourceFolder = "Assets/ThirdParty/Characters/Survival Char";
        public const string SourcePath = SourceFolder + "/survival_character.fbx";
        public const string Root = "Assets/LastSignal/Art/CharacterIntegration/Arberry";
        public const string BodyPath = Root + "/Prefabs/Arberry_WorldBody.prefab";
        public const string Docs = "Docs/Implementation/ArberryIntegration";
        static string Textures => SourceFolder + "/Textures 2/Textures/";
        static void Guard() { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before authoring."); }
        static void Folders()
        {
            foreach (var f in new[] { "Materials", "Textures", "Profiles", "Prefabs" }) Directory.CreateDirectory(Root + "/" + f);
            Directory.CreateDirectory(Docs); AssetDatabase.Refresh();
        }
        [MenuItem("Last Signal/Character/Arberry/1 Record source metadata")]
        public static void Audit()
        {
            Guard(); Folders(); var sb = new StringBuilder("# Local source metadata\n\nAssetDatabase inventory; authoring inspection, not a verification gate.\n\n");
            var paths = AssetDatabase.FindAssets("", new[] { SourceFolder }).Select(AssetDatabase.GUIDToAssetPath).Where(p => !AssetDatabase.IsValidFolder(p));
            foreach (var p in paths)
            {
                if (AssetImporter.GetAtPath(p) is TextureImporter t)
                {
                    t.GetSourceTextureWidthAndHeight(out int w, out int h);
                    sb.AppendLine($"- `{p}`: {w}×{h}; type={t.textureType}; sRGB={t.sRGBTexture}; source RGBA8+mips estimate={(long)w*h*4*4/3/1048576f:F2} MiB (not runtime residency).");
                }
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            var importer = (ModelImporter)AssetImporter.GetAtPath(SourcePath);
            sb.AppendLine($"\nImporter: {importer.animationType}, {importer.avatarSetup}; scale={importer.globalScale}; optimize={importer.optimizeGameObjects}; clips={importer.defaultClipAnimations.Length}.");
            foreach (var kv in importer.GetExternalObjectMap()) sb.AppendLine($"- Original remap `{kv.Key.name}` → `{AssetDatabase.GetAssetPath(kv.Value)}`");
            long total = 0;
            foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var m = r.sharedMesh; long triangles = m.triangles.Length / 3; total += triangles;
                sb.AppendLine($"- {r.name}: vertices={m.vertexCount}; triangles={triangles}; slots={m.subMeshCount}; bones={r.bones.Length}; bindposes={m.bindposes.Length}; blendshapes={m.blendShapeCount}; bounds={r.bounds}; materials={string.Join(",",r.sharedMaterials.Select(x=>x.name))}");
                if (r.bones.Any(b => !b) || m.bindposes.Length != r.bones.Length) throw new InvalidOperationException("Invalid skin references: " + r.name);
            }
            sb.AppendLine($"\nTotal source triangles: {total}. No runtime measurement performed.");
            foreach (var t in model.GetComponentsInChildren<Transform>(true).Where(t => t == model.transform || t.childCount > 0))
                sb.AppendLine($"- hierarchy `{t.name}` parent=`{(t.parent ? t.parent.name : "none")}` scale={t.localScale}");
            File.WriteAllText(Docs + "/source-metadata.md", sb.ToString());
        }
        [MenuItem("Last Signal/Character/Arberry/2 Import own Humanoid")]
        public static void ImportRig()
        {
            Guard(); Folders(); var model = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
            var map = new Dictionary<HumanBodyBones,string> {
                {HumanBodyBones.Hips,"pelvis"},{HumanBodyBones.Spine,"spine_01"},{HumanBodyBones.Chest,"spine_03"},
                {HumanBodyBones.UpperChest,"spine_05"},{HumanBodyBones.Neck,"neck_02"},{HumanBodyBones.Head,"head"} };
            foreach (string side in new[] { "Left", "Right" })
            {
                string suffix = side == "Left" ? "_l" : "_r";
                string[] humans = { "Shoulder", "UpperArm", "LowerArm", "Hand", "UpperLeg", "LowerLeg", "Foot", "Toes" };
                string[] source = { "clavicle", "upperarm", "lowerarm", "hand", "thigh", "calf", "foot", "ball" };
                for (int i=0;i<humans.Length;i++) map[(HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),side+humans[i])] = source[i]+suffix;
                string[] fingers={"Thumb","Index","Middle","Ring","Little"}, segments={"Proximal","Intermediate","Distal"};
                for (int f=0;f<5;f++) for(int j=0;j<3;j++) map[(HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),side+fingers[f]+segments[j])] = (f==4?"pinky":fingers[f].ToLowerInvariant())+"_0"+(j+1)+suffix;
            }
            var transforms = model.GetComponentsInChildren<Transform>(true);
            foreach(var name in map.Values) if(transforms.Count(t=>t.name==name)!=1) throw new InvalidOperationException("Missing/ambiguous bone: "+name);
            var mi = (ModelImporter)AssetImporter.GetAtPath(SourcePath);
            mi.animationType=ModelImporterAnimationType.Human; mi.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            mi.optimizeGameObjects=false; mi.importAnimation=false; mi.importCameras=false; mi.importLights=false;
            mi.humanDescription = new HumanDescription {
                human=map.Select(p=>new HumanBone { humanName=HumanTrait.BoneName[(int)p.Key],boneName=p.Value,limit=new HumanLimit {useDefaultValues=true}}).ToArray(),
                skeleton=transforms.Select(t=>new SkeletonBone {name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
                upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.02f,legStretch=.02f,feetSpacing=0,hasTranslationDoF=false
            };
            mi.SaveAndReimport();
            var avatar=AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<Avatar>().SingleOrDefault();
            File.WriteAllText(Docs+"/rig-authoring.txt",$"Own imported Avatar: exists={avatar!=null}; valid={avatar&&avatar.isValid}; human={avatar&&avatar.isHuman}\n"+string.Join("\n",map.Select(x=>x.Key+"="+x.Value))+"\nDeformation, locomotion and visual acceptance NOT_RUN.\n");
            if(!avatar||!avatar.isValid||!avatar.isHuman) throw new InvalidOperationException("Arberry Avatar invalid; Diesel must remain active.");
        }
        // Decode locally, resize in linear storage (including encoded color values), and destroy CPU copies.
        static Texture2D Read(string relative, int size)
        {
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            if(!source.LoadImage(File.ReadAllBytes(Textures+relative))) { Object.DestroyImmediate(source); throw new IOException(relative); }
            int w=Math.Min(size,source.width),h=Math.Min(size,source.height);
            var rt=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            var previous=RenderTexture.active; bool gamma=GL.sRGBWrite;
            try { GL.sRGBWrite=false; Graphics.Blit(source,rt); RenderTexture.active=rt;
                var output=new Texture2D(w,h,TextureFormat.RGBA32,false,true); output.ReadPixels(new Rect(0,0,w,h),0,0);output.Apply();return output; }
            finally { GL.sRGBWrite=gamma; RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(source); }
        }
        static Texture2D SaveTexture(Texture2D texture,string name,bool color,bool normal=false,bool alpha=false)
        {
            string path=Root+"/Textures/"+name+".png";
            try { File.WriteAllBytes(path,texture.EncodeToPNG()); } finally { Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=color;importer.isReadable=false;importer.mipmapEnabled=true;
            importer.alphaSource=alpha?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
            importer.alphaIsTransparency=alpha&&color&&name.EndsWith("_Base",StringComparison.Ordinal);importer.wrapMode=TextureWrapMode.Repeat;
            importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            var standalone=importer.GetPlatformTextureSettings("Standalone");standalone.overridden=true;standalone.maxTextureSize=2048;
            standalone.format=normal?TextureImporterFormat.BC5:TextureImporterFormat.BC7;importer.SetPlatformTextureSettings(standalone);
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Texture2D Copy(string relative,string name,bool color,bool normal=false,int size=2048) => SaveTexture(Read(relative,size),name,color,normal);
        public static Color32 PackOrm(Color32 source) => new Color32(source.b,source.r,255,(byte)(255-source.g));
        static void Packed(Material mat,string relative,string name,int size)
        {
            var raw=Read(relative,size);var px=raw.GetPixels32();
            // Source OcclusionRoughnessMetallic: R occlusion, G roughness, B metal. HDRP: R metal, G occlusion, B detail mask, A smoothness.
            var sum=new double[3];var min=new byte[]{255,255,255};var max=new byte[3];
            foreach(var p in px) {
                sum[0]+=p.r/255.0;sum[1]+=p.g/255.0;sum[2]+=p.b/255.0;
                min[0]=Math.Min(min[0],p.r);min[1]=Math.Min(min[1],p.g);min[2]=Math.Min(min[2],p.b);
                max[0]=Math.Max(max[0],p.r);max[1]=Math.Max(max[1],p.g);max[2]=Math.Max(max[2],p.b);
            }
            File.AppendAllText(Docs+"/texture-channel-diagnostics.txt",relative+" RGB mean="+string.Join(",",sum.Select(v=>(v/px.Length).ToString("F4")))+" min="+string.Join(",",min)+" max="+string.Join(",",max)+"\n");
            for(int i=0;i<px.Length;i++) px[i]=PackOrm(px[i]);
            raw.SetPixels32(px);raw.Apply();var map=SaveTexture(raw,name+"_MetalSmoothAO",false,false,true);
            Rendering.HdrpMaterialMaskUtility.Apply(mat,map,true,true,1,1,1,true,Vector2.one,Vector2.zero);
        }
        static void Material(string name,string albedo,string normal,string orm=null,string opacity=null,string ao=null,int size=2048)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("HDRP/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.shader=Shader.Find("HDRP/Lit");
            var color=Read(albedo,size);
            if(opacity!=null){var mask=Read(opacity,size);var c=color.GetPixels32();var a=mask.GetPixels32();if(c.Length!=a.Length)throw new InvalidOperationException("Opacity dimensions differ");for(int i=0;i<c.Length;i++)c[i].a=a[i].r;color.SetPixels32(c);color.Apply();Object.DestroyImmediate(mask);}
            m.SetTexture("_BaseColorMap",SaveTexture(color,name+"_Base",true,false,opacity!=null));m.SetColor("_BaseColor",Color.white);
            m.SetTexture("_NormalMap",Copy(normal,name+"_Normal",false,true,size));m.SetFloat("_NormalScale",1);m.EnableKeyword("_NORMALMAP");
            m.shader=Shader.Find("HDRP/Lit");
            m.SetFloat("_SurfaceType",0);
            Rendering.HdrpMaterialMaskUtility.Apply(m,null,false,false,0,.25f,1,false,Vector2.one,Vector2.zero);
            if(orm!=null)Packed(m,orm,name,size);
            if(ao!=null){
                if(orm!=null)throw new InvalidOperationException("Separate AO plus ORM requires an explicit combined mask.");
                Rendering.HdrpMaterialMaskUtility.Apply(m,Copy(ao,name+"_AO",false,false,size),false,false,0,.25f,1,true,Vector2.one,Vector2.zero);
            }
            m.SetFloat("_AlphaCutoffEnable",opacity!=null?1:0);m.SetFloat("_AlphaCutoff",.45f);
            if(opacity!=null){m.EnableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;m.SetFloat("_Cull",2);}
            UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(m);
            EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);
        }
        [MenuItem("Last Signal/Character/Arberry/3 Reconstruct materials locally")]
        public static void Materials()
        {
            Guard();Folders();File.WriteAllText(Docs+"/texture-channel-diagnostics.txt","Local RGB diagnostics; channel interpretation is documented separately. No pixel data exported.\n");
            foreach(string part in new[]{"Backpack","Gloves","Jacket","Jeans","Shoes"}) Material(part+(part=="Backpack"?"2":"1"),part+"/"+part+"_BaseColor.jpg",part+"/"+part+"_Normal.jpg",part+"/"+part+"_OcclusionRoughnessMetallic.jpg");
            string[] names={"Head","Body2","Arms"};
            for(int i=0;i<3;i++){string tile=".100"+(i+1)+".png";Material(names[i],"Body/Body_BaseColor"+tile,"Body/Body_Normal"+tile,"Body/Body_OcclusionRoughnessMetallic"+tile);}
            Material("Mouth","Mouth/TeethBaseColor.png","Mouth/Teet_Normal.png","Mouth/Teeth_ORM.png",size:1024);
            Material("Hair3","HairStyle/HairBrown_Diffuse_2.jpg","HairStyle/HairBrown_Normal.jpg",opacity:"HairStyle/HairBrown_Alpha.jpg",ao:"HairStyle/HairBrown_AO.jpg");
            Material("Brows_Leashes","Eyebrow_EyeLeashes/Eyebrows_BaseColor.jpg","Eyebrow_EyeLeashes/Eyebrows_Normal.jpg",opacity:"Eyebrow_EyeLeashes/Eyebrows_Opacity.jpg",ao:"Eyebrow_EyeLeashes/Eyebrows_AO.jpg",size:1024);
            Material("Body_Arkit_Eye","Eye/Eye_BaseColor.png","Eye/Eye_Normal.jpg",ao:"Eye/Eye_AO.jpg",size:1024);
            var eye=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Body_Arkit_Eye.mat");
            // Preserve supplied colored specular and gloss in URP's specular workflow.
            var spec=Read("Eye/Eye_SpecularColourBrown.png",1024);var gloss=Read("Eye/Eye_Gloss.jpg",1024);
            var sp=spec.GetPixels32();var gp=gloss.GetPixels32();for(int i=0;i<sp.Length;i++)sp[i].a=gp[i].r;spec.SetPixels32(sp);spec.Apply();Object.DestroyImmediate(gloss);
            eye.SetTexture("_SpecGlossMap",SaveTexture(spec,"Eye_SpecGloss",true,false,true));eye.SetFloat("_WorkflowMode",0);eye.SetFloat("_Smoothness",1);eye.EnableKeyword("_SPECULAR_SETUP");eye.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(eye);AssetDatabase.SaveAssetIfDirty(eye);
        }
        [MenuItem("Last Signal/Character/Arberry/4 Assemble world body")]
        public static void Body()
        {
            Guard();Folders();var source=AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);var body=Object.Instantiate(source);body.name="WorldBody";
            try {
                var a=body.GetComponent<Animator>();if(!a||!a.avatar||!a.avatar.isHuman||!a.avatar.isValid)throw new InvalidOperationException("Own Humanoid required before assembly");
                body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);body.transform.localScale=Vector3.one;
                a.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(DieselCharacterAuthoring.ControllerPath);a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)){
                    r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+m.name+".mat")??throw new InvalidOperationException("No material "+m.name)).ToArray();
                    r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;r.updateWhenOffscreen=false;r.quality=SkinQuality.Auto;
                    // Root-bone local coordinates: cover crouch, arms, and seated poses without changing geometry.
                    var center=r.rootBone.InverseTransformPoint(new Vector3(0,.95f,0));
                    float scale=Mathf.Abs(r.rootBone.lossyScale.x);r.localBounds=new Bounds(center,new Vector3(2.7f,2.9f,2.7f)/scale);
                }
                foreach(var t in body.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                body.AddComponent<PlayerLocomotionPresenter>();body.AddComponent<CharacterBodyVisibility>();body.AddComponent<CharacterSeatPresentation>();
                body.AddComponent<CharacterWeaponRig>();
                PrefabUtility.SaveAsPrefabAsset(body,BodyPath);
            } finally {Object.DestroyImmediate(body);}
        }
        [MenuItem("Last Signal/Character/Arberry/Use Arberry presentation")]
        public static void UseArberry()=>Attach(BodyPath);
        [MenuItem("Last Signal/Character/Arberry/Restore Diesel presentation")]
        public static void UseDiesel()=>Attach(DieselCharacterAuthoring.BodyPath);
        static void Attach(string path)
        {
            Guard();var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);var animator=source?source.GetComponent<Animator>():null;
            if(!animator||!animator.avatar||!animator.avatar.isHuman||!animator.avatar.isValid)throw new InvalidOperationException("Cannot activate invalid presentation");
            var rig=source.GetComponent<CharacterWeaponRig>();
            if(!rig||rig.Profiles==null||rig.Profiles.Length!=5||rig.Profiles.Any(p=>!p||!p.WorldPrefab))throw new InvalidOperationException("Author weapon profiles before activating presentation");
            if(source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>!r.rootBone||r.bones.Any(b=>!b)||r.sharedMaterials.Any(m=>!m)))throw new InvalidOperationException("Incomplete renderer references; current player retained");
            var player=PrefabUtility.LoadPrefabContents(DieselCharacterAuthoring.PlayerPath);
            try{
                var old=player.transform.Find("WorldBody");if(old)Object.DestroyImmediate(old.gameObject);
                var body=(GameObject)PrefabUtility.InstantiatePrefab(source,player.transform);body.name="WorldBody";body.transform.localPosition=Vector3.zero;body.transform.localRotation=Quaternion.identity;body.transform.localScale=Vector3.one;
                player.GetComponent<CharacterInspectionCamera>().Configure(player.GetComponent<FirstPersonLook>().View,body.GetComponent<CharacterBodyVisibility>());
                player.GetComponent<CharacterFirstPersonActions>().Configure(player.transform.Find("View/WeaponParent"),body.GetComponent<PlayerLocomotionPresenter>());
                ArberryFirstPersonAuthoring.ConfigurePlayer(player,path==BodyPath);
                PrefabUtility.SaveAsPrefabAsset(player,DieselCharacterAuthoring.PlayerPath);
            }finally{PrefabUtility.UnloadPrefabContents(player);}
        }
    }
}
