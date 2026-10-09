using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using LastSignal.Audio;
namespace LastSignal.Editor
{
    public static class S015AudioAuthoring
    {
        const string Vendor="Assets/ThirdParty/S15 Sound Pack/";
        const string Root="Assets/LastSignal/Audio/";
        static AudioClip Clip(string path)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Vendor+path);
            if(!clip) throw new InvalidOperationException("Missing S015 clip: "+path);
            return clip;
        }
        static AudioClip[] Variants(string folder,string prefix,int limit=3)
        {
            var paths=Directory.GetFiles(Vendor+folder,prefix+"*.wav",SearchOption.AllDirectories);
            Array.Sort(paths,StringComparer.Ordinal); var clips=new AudioClip[Math.Min(limit,paths.Length)];
            if(clips.Length==0) throw new InvalidOperationException("Empty cue: "+prefix);
            for(int i=0;i<clips.Length;i++) clips[i]=Clip(paths[i].Replace('\\','/').Substring(Vendor.Length));
            return clips;
        }
        [MenuItem("Last Signal/S015/Author Production Audio")]
        public static void Author()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode must be OFF.");
            if(EditorSceneManager.GetActiveScene().path!="Assets/LastSignal/Scenes/Production/S013Cabin.unity") throw new InvalidOperationException("Open S013Cabin first.");
            var catalog=AssetDatabase.LoadAssetAtPath<AudioCatalog>(Root+"Config/S015AudioCatalog.asset");
            if(!catalog) { catalog=ScriptableObject.CreateInstance<AudioCatalog>(); AssetDatabase.CreateAsset(catalog,Root+"Config/S015AudioCatalog.asset"); }
            catalog.mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>(Root+"Mixers/LastSignal.mixer");
            if(!catalog.mixer) throw new InvalidOperationException("Last Signal mixer missing.");
            catalog.effects=Group(catalog,"Effects"); catalog.ambience=Group(catalog,"Ambience"); catalog.voice=Group(catalog,"Voice"); catalog.music=Group(catalog,"Music");
            var cues=new List<AudioCueEntry>();
            void Add(AudioCue id,AudioClip[] clips,float gain,AudioBus bus=AudioBus.Effects,bool spatial=true,int priority=100,float range=20,bool loop=false)
            { cues.Add(new AudioCueEntry {cue=id,clips=clips,gain=gain,bus=bus,spatial=spatial,priority=priority,range=range,loop=loop,pitchVariation=loop?0:.035f}); }
            string nox="Essentials_Series_NOX_SOUND/", foot=nox+"Footsteps_Essentials_NOX_SOUND/";
            var wood=Variants(foot+"Footsteps_Wood/Footsteps_Wood_Walk","Footsteps_Wood_Walk_");
            var dirt=Variants(foot+"Footsteps_DirtyGround/Footsteps_DirtyGround_Walk","Footsteps_DirtyGround_Walk_");
            Add(AudioCue.StepDefault,dirt,.28f,range:14); Add(AudioCue.StepWood,wood,.28f,range:14); Add(AudioCue.StepDirt,dirt,.25f,range:14);
            Add(AudioCue.StepGrass,Variants(foot+"Footsteps_Grass/Footsteps_Grass_Walk","Footsteps_Walk_Grass_Mono_"),.25f,range:14);
            Add(AudioCue.StepGravel,Variants(foot+"Footsteps_Gravel/Footsteps_Gravel_Walk","Footsteps_Gravel_Walk_"),.28f,range:14);
            Add(AudioCue.StepMetal,Variants(foot+"Footsteps_Metal/Footsteps_Metal_Walk","Footsteps_MetalV1_Walk_"),.25f,range:14);
            Add(AudioCue.StepStone,Variants(foot+"Footsteps_Rock/Footsteps_Rock_Walk","Footsteps_Rock_Walk_"),.25f,range:14);
            string zombie="ZombieHorrorPackageFree/WAV/";
            Add(AudioCue.ZombieIdle,Variants(zombie+"VO/Zombie01","Zombie001_Idle_A_"),.3f,AudioBus.Voice,priority:160,range:12);
            Add(AudioCue.ZombieAlert,Variants(zombie+"VO/Zombie03","Zombie003_Idle_A_",1),.48f,AudioBus.Voice,priority:60,range:16);
            Add(AudioCue.ZombieApproach,Variants(zombie+"VO/Zombie03","Zombie003_Idle_A_"),.36f,AudioBus.Voice,priority:90,range:16);
            Add(AudioCue.ZombieTelegraph,Variants(zombie+"VO/Zombie01","Zombie001_Attack_A_"),.72f,AudioBus.Voice,priority:16,range:8);
            Add(AudioCue.ZombieAttack,Variants(zombie+"Bite","Zombie_Attack_Bite_"),.45f,AudioBus.Voice,priority:40,range:8);
            Add(AudioCue.ZombieHurt,Variants(zombie+"VO/Zombie01","Zombie001_Hurt_A_"),.44f,AudioBus.Voice,priority:70,range:16);
            Add(AudioCue.ZombieDeath,Variants(zombie+"BodyFall","Foley_BodyFall_"),.45f,priority:70,range:16);
            string rifle="FreeWeaponSFX_v1.1/AssaultRifle/";
            Add(AudioCue.Fire,Variants(rifle+"Gunshots","sfx_wpn_ar_gunshot_"),.52f,spatial:false,priority:35);
            Add(AudioCue.DryFire,new[]{Clip(nox+"Vehicle_Essentials_NOX_SOUND/Vehicle_Essential_Car/Vehicle_Car_Button_Mono_01.wav")},.4f,spatial:false,priority:50);
            Add(AudioCue.ReloadStart,new[]{Clip(rifle+"Reload/sfx_wpn_ar_reload_02_magDraw.wav")},.3f,spatial:false,priority:60);
            Add(AudioCue.MagazineOut,new[]{Clip(rifle+"Reload/sfx_wpn_ar_reload_01_magOut.wav")},.38f,spatial:false,priority:55);
            Add(AudioCue.MagazineIn,new[]{Clip(rifle+"Reload/sfx_wpn_ar_reload_03_magIn.wav")},.38f,spatial:false,priority:55);
            Add(AudioCue.Bolt,new[]{Clip(rifle+"Reload/sfx_wpn_ar_reload_04_charge.wav")},.4f,spatial:false,priority:55);
            Add(AudioCue.Equip,Variants(rifle+"Foley","sfx_wpn_ar_foley_equip_"),.28f,spatial:false,priority:90);
            Add(AudioCue.MeleeSwing,Variants(zombie+"Foley","Foley_Movement_Clothes_A_"),.35f,spatial:false,priority:70);
            Add(AudioCue.MeleeImpact,Variants(zombie+"Impact","Impact_Flesh_0"),.5f,spatial:false,priority:40);
            Add(AudioCue.Confirm,new[]{Clip(nox+"Vehicle_Essentials_NOX_SOUND/Vehicle_Essential_Car/Vehicle_Car_Button_Mono_01.wav")},.18f,spatial:false,priority:80);
            string nature=nox+"Nature_Essentials_NOX_SOUND/";
            void Loop(AudioCue id,string path,float gain,AudioBus bus) => Add(id,new[]{Clip(path)},gain,bus,false,200,20,true);
            Loop(AudioCue.Wind,nature+"Ambiance_Wind_Forest_Loop_Stereo.wav",.12f,AudioBus.Ambience);
            Loop(AudioCue.Rain,nature+"Ambiance_Rain_Calm_Loop_Stereo.wav",.24f,AudioBus.Ambience);
            Loop(AudioCue.HeavyRain,nature+"Ambiance_Rain_Strong_Loop_Stereo.wav",.18f,AudioBus.Ambience);
            Loop(AudioCue.Night,nature+"Ambiance_Night_Loop_Stereo.wav",.15f,AudioBus.Ambience);
            Loop(AudioCue.Interior,nature+"Ambiance_Wind_Calm_Loop_Stereo.wav",.05f,AudioBus.Ambience);
            string music="Horror_Music_Pack_Starter_Kit/";
            Loop(AudioCue.Exploration,music+"WAV_ATMOSPHERIC_LOOP_Phosphoric_Synesthesia.wav",.11f,AudioBus.Music);
            Loop(AudioCue.Threat,music+"WAV_TENSION_LOOP_The_Wood_Monster.wav",.19f,AudioBus.Music);
            Loop(AudioCue.Relief,music+"WAV_AMBIENCE_LOOP_Mirrored_Reflections.wav",.1f,AudioBus.Music);
            catalog.cues=cues.ToArray(); EditorUtility.SetDirty(catalog);
            // Retune only selected clip import metadata; raw vendor waveforms remain untouched.
            var seen=new HashSet<string>();
            foreach(var cue in cues) foreach(var clip in cue.clips)
            {
                string path=AssetDatabase.GetAssetPath(clip); if(!seen.Add(path)) continue;
                var importer=(AudioImporter)AssetImporter.GetAtPath(path); var settings=importer.defaultSampleSettings;
                settings.loadType=cue.loop?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=cue.loop?AudioCompressionFormat.Vorbis:AudioCompressionFormat.ADPCM;
                settings.quality=.7f; importer.defaultSampleSettings=settings;
                importer.forceToMono=cue.spatial; importer.loadInBackground=cue.loop; importer.SaveAndReimport();
            }
            var flow=UnityEngine.Object.FindFirstObjectByType<SessionFlow>();
            if(!flow) throw new InvalidOperationException("Production SessionFlow missing.");
            var audio=flow.GetComponent<ProductionAudio>(); if(!audio) audio=Undo.AddComponent<ProductionAudio>(flow.gameObject);
            Undo.RecordObject(audio,"S015 audio config"); audio.Configure(catalog); EditorUtility.SetDirty(audio);
            AuthorWeapon();
            AddPrefab<FootstepAudioPresenter>("Assets/LastSignal/Prefabs/Player/Player.prefab");
            AddPrefab<ZombieAudioPresenter>("Assets/LastSignal/Prefabs/Resources/LS_Zombie_Runtime.prefab");
            foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>())
            {
                AudioSurfaceKind kind;
                if(c.name=="Cabin floor" || c.name=="Floor collision") kind=AudioSurfaceKind.Wood;
                else if(c.name=="Gravel contact zone" || c.name=="Entry path") kind=AudioSurfaceKind.Gravel;
                else if(c.name=="S012 ground 400 x 400 m") kind=AudioSurfaceKind.Grass;
                else continue;
                var surface=c.GetComponent<AudioSurface>(); if(!surface) surface=Undo.AddComponent<AudioSurface>(c.gameObject);
                Undo.RecordObject(surface,"S015 surface"); surface.surface=kind; EditorUtility.SetDirty(surface);
            }
            var clearing=GameObject.Find("Cabin soil clearing");
            if(clearing)
            {
                var area=clearing.GetComponent<AudioSurface>(); if(!area) area=Undo.AddComponent<AudioSurface>(clearing);
                Undo.RecordObject(area,"S015 clearing surface"); area.surface=AudioSurfaceKind.Dirt; area.areaRadii=new Vector2(22,19.5f); area.squareCutout=3.25f;
                audio.ConfigureGroundAreas(new[]{area}); EditorUtility.SetDirty(area); EditorUtility.SetDirty(audio);
            }
            var room=GameObject.Find("S015 Cabin Acoustics");
            if(!room) { room=new GameObject("S015 Cabin Acoustics"); Undo.RegisterCreatedObjectUndo(room,"S015 room"); }
            room.transform.position=new Vector3(-360,1.4f,-140);
            var reverb=room.GetComponent<AudioReverbZone>(); if(!reverb) reverb=room.AddComponent<AudioReverbZone>();
            reverb.reverbPreset=AudioReverbPreset.Room; reverb.minDistance=1.5f; reverb.maxDistance=4.5f;
            CreateUI(flow,audio);
            // Keep existing vehicle clips and gains, only route their already-authored sources to Effects.
            foreach(var vehicle in UnityEngine.Object.FindObjectsByType<LastSignal.Vehicles.VehicleAudioPresenter>(FindObjectsInactive.Include))
                foreach(var source in vehicle.GetComponentsInChildren<AudioSource>(true)) { Undo.RecordObject(source,"S015 vehicle bus"); source.outputAudioMixerGroup=catalog.effects; EditorUtility.SetDirty(source); }
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(flow.gameObject.scene); EditorSceneManager.SaveScene(flow.gameObject.scene);
            Debug.Log("S015 authoring saved. Tests and auditory acceptance NOT_RUN.");
        }
        static void AuthorWeapon()
        {
            const string path="Assets/LastSignal/Prefabs/Resources/Weapon_AssaultRifle.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var presenter=root.GetComponent<WeaponAudioPresenter>();
                if(!presenter) presenter=root.AddComponent<WeaponAudioPresenter>();
                var serialized=new SerializedObject(presenter);
                serialized.FindProperty("weapon").objectReferenceValue=root.GetComponent<WeaponController>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static AudioMixerGroup Group(AudioCatalog catalog,string name)
        { foreach(var group in catalog.mixer.FindMatchingGroups(name)) if(group.name==name) return group; throw new InvalidOperationException("Missing bus "+name); }
        static void AddPrefab<T>(string path) where T:Component
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try { if(!root.GetComponent<T>()) root.AddComponent<T>(); PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform)); var rect=go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor; rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
        }
        static Text Label(string name,Transform parent,string text,Vector2 pos,Vector2 size)
        {
            var label=Rect(name,parent,new Vector2(.5f,.5f),pos,size).gameObject.AddComponent<Text>();
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=18; label.color=Color.white; label.text=text;
            label.alignment=TextAnchor.MiddleLeft; label.raycastTarget=false; return label;
        }
        static void CreateUI(SessionFlow flow,ProductionAudio audio)
        {
            var old=GameObject.Find("S015 Audio UI"); if(old) Undo.DestroyObjectImmediate(old);
            var root=new GameObject("S015 Audio UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); Undo.RegisterCreatedObjectUndo(root,"S015 audio UI");
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay; root.GetComponent<Canvas>().sortingOrder=45;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            var panel=Rect("Ses Ayarları",root.transform,new Vector2(1,.5f),new Vector2(-250,0),new Vector2(420,360));
            panel.gameObject.AddComponent<Image>().color=new Color(.025f,.035f,.045f,.96f);
            Label("Title",panel,"SES AYARLARI",new Vector2(0,145),new Vector2(360,30));
            string[] names={"Genel","Efekt","Ortam","Sesler","Müzik"}; var sliders=new Slider[5];
            for(int i=0;i<5;i++)
            {
                float y=95-i*42; Label(names[i],panel,names[i],new Vector2(-125,y),new Vector2(110,30));
                var bar=Rect(names[i]+" Slider",panel,new Vector2(.5f,.5f),new Vector2(60,y),new Vector2(220,24));
                bar.gameObject.AddComponent<Image>().color=new Color(.13f,.18f,.20f);
                var slider=bar.gameObject.AddComponent<Slider>(); slider.minValue=0; slider.maxValue=1;
                var handle=Rect("Handle",bar,new Vector2(.5f,.5f),Vector2.zero,new Vector2(18,28)); var handleImage=handle.gameObject.AddComponent<Image>(); handleImage.color=new Color(.55f,.85f,.8f);
                slider.handleRect=handle; slider.targetGraphic=handleImage; slider.navigation=new Navigation {mode=Navigation.Mode.Automatic}; sliders[i]=slider;
            }
            var check=Rect("Captions",panel,new Vector2(.5f,.5f),new Vector2(-160,-135),new Vector2(26,26));
            var bg=check.gameObject.AddComponent<Image>(); bg.color=new Color(.2f,.25f,.28f);
            var tick=Rect("Check",check,new Vector2(.5f,.5f),Vector2.zero,new Vector2(18,18)).gameObject.AddComponent<Image>(); tick.color=new Color(.55f,.85f,.8f);
            var toggle=check.gameObject.AddComponent<Toggle>(); toggle.targetGraphic=bg; toggle.graphic=tick;
            Label("Caption label",panel,"Ses altyazıları",new Vector2(0,-135),new Vector2(270,30));
            var caption=Label("Audio caption",root.transform,"",Vector2.zero,new Vector2(640,48)); caption.alignment=TextAnchor.MiddleCenter;
            caption.rectTransform.anchorMin=caption.rectTransform.anchorMax=new Vector2(.5f,.25f); caption.fontSize=24;
            caption.gameObject.AddComponent<Outline>().effectColor=Color.black;
            var view=root.AddComponent<AudioSettingsView>(); view.audioOwner=audio; view.flow=flow; view.settingsPanel=panel.gameObject; view.sliders=sliders; view.captions=toggle; view.captionText=caption;
        }
    }
}
