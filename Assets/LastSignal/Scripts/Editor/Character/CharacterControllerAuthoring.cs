using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastSignal.Editor
{
    public static class CharacterControllerAuthoring
    {
        public static void Extend()
        {
            var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(DieselCharacterAuthoring.ControllerPath);
            if(!c)throw new InvalidOperationException("Existing WorldBody controller missing; refusing replacement.");
            foreach(string p in new[]{"Grounded","Sprinting","Dead","Seated","MeleeActive","Treating"}) Param(c,p,AnimatorControllerParameterType.Bool);
            foreach(string p in new[]{"VerticalVelocity","MovementDirection","AimAmount","ReloadTime","MeleeTime"}) Param(c,p,AnimatorControllerParameterType.Float);
            Param(c,"WeaponState",AnimatorControllerParameterType.Int);
            foreach(string p in new[]{"Land","Interact","Pickup","Consume","Hit"})Param(c,p,AnimatorControllerParameterType.Trigger);
            var layers=c.layers; layers[0].iKPass=true; c.layers=layers;
            var sm=c.layers[0].stateMachine;
            if(sm.states.Any(s=>s.state.name=="Character Death"))
            {
                var actionLayer=c.layers.FirstOrDefault(l=>l.name=="Character Actions");
                if(actionLayer!=null) foreach(var entry in actionLayer.stateMachine.states)
                    if(entry.state.name=="MeleeActive"){entry.state.timeParameter="MeleeTime";entry.state.timeParameterActive=true;EditorUtility.SetDirty(entry.state);}
                EnsureGripLayer(c); UseCoherentLocomotion(c); EditorUtility.SetDirty(c); return;
            } // Author once; preserve later hand tuning.
            var idle=sm.states.First(s=>s.state.name=="Standing Idle").state;
            var move=sm.states.First(s=>s.state.name=="Standing Move").state;
            // Retain the original directional tree and all existing clip references.
            var run=(BlendTree)move.motion;
            var walk=new BlendTree{name="Character Walk Directional",blendType=run.blendType,blendParameter="MoveX",blendParameterY="MoveY"};
            AssetDatabase.AddObjectToAsset(walk,c);
            walk.children=run.children.Select(child=>{ child.timeScale=.58f; if(child.position.y>.9f && Mathf.Abs(child.position.x)<.1f){child.motion=Clip("Walk_Loop");child.timeScale=1;}return child; }).ToArray();
            var sprint=new BlendTree{name="Character Sprint Directional",blendType=run.blendType,blendParameter="MoveX",blendParameterY="MoveY"};
            AssetDatabase.AddObjectToAsset(sprint,c);
            sprint.children=run.children.Select(child=>{if(child.position.y>.9f&&Mathf.Abs(child.position.x)<.1f)child.motion=Clip("Sprint_Loop");return child;}).ToArray();
            var speed=new BlendTree{name="Character Walk Run Sprint",blendType=BlendTreeType.Simple1D,blendParameter="HorizontalSpeed",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(speed,c);speed.AddChild(walk,3.2f);speed.AddChild(run,4.3f);speed.AddChild(sprint,5.5f);move.motion=speed;
            foreach(var state in sm.states)foreach(var t in state.state.transitions)Gate(t,true);
            foreach(var t in sm.anyStateTransitions)Gate(t,true);
            var dead=State(sm,"Character Death",Human("Combat/HumanM@Death01.fbx"));
            var seat=State(sm,"Character Seated",Clip("Driving_Loop"));
            var jump=State(sm,"Character Jump",Clip("Jump_Start"));
            var fall=State(sm,"Character Fall",Clip("Jump_Loop"));
            var land=State(sm,"Character Land",Clip("Jump_Land"));
            var death=sm.AddAnyStateTransition(dead);Setup(death,.08f);death.AddCondition(AnimatorConditionMode.If,0,"Dead");
            var seated=sm.AddAnyStateTransition(seat);Setup(seated,.12f);seated.AddCondition(AnimatorConditionMode.If,0,"Seated");seated.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");
            var up=sm.AddAnyStateTransition(jump);Setup(up,.1f);Gate(up,false);up.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");up.AddCondition(AnimatorConditionMode.Greater,.1f,"VerticalVelocity");
            var down=sm.AddAnyStateTransition(fall);Setup(down,.12f);Gate(down,false);down.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");down.AddCondition(AnimatorConditionMode.Less,.1f,"VerticalVelocity");
            var landed=sm.AddAnyStateTransition(land);Setup(landed,.08f);Gate(landed,true);landed.AddCondition(AnimatorConditionMode.If,0,"Land");landed.AddCondition(AnimatorConditionMode.IfNot,0,"IsSliding");landed.AddCondition(AnimatorConditionMode.IfNot,0,"IsCrouching");
            // Terminal/seat/air conditions are disjoint from locomotion; death has explicit priority.
            sm.anyStateTransitions=new[]{death,seated,up,down,landed}.Concat(sm.anyStateTransitions.Where(t=>t!=death&&t!=seated&&t!=up&&t!=down&&t!=landed)).ToArray();
            Return(dead,idle,"Dead",false);Return(seat,idle,"Seated",false);
            Return(jump,idle,"Grounded",true);Return(fall,idle,"Grounded",true);
            var end=land.AddTransition(idle);Setup(end,.1f);end.hasExitTime=true;end.exitTime=.85f;
            BuildActions(c);
            EnsureGripLayer(c);
            UseCoherentLocomotion(c);
            EditorUtility.SetDirty(c);
        }
        static void UseCoherentLocomotion(AnimatorController c)
        {
            // The extracted UAL walk/sprint have reversed planted-foot travel on this retarget.
            // Preserve the assets, but use the existing directionally matched HumanM family.
            var run=AssetDatabase.LoadAllAssetsAtPath(DieselCharacterAuthoring.ControllerPath)
                .OfType<BlendTree>().First(t=>t.name=="Standing Directional");
            var move=c.layers[0].stateMachine.states.First(s=>s.state.name=="Standing Move").state;
            move.motion=run;EditorUtility.SetDirty(move);
            var layers=c.layers;
            int action=Array.FindIndex(layers,l=>l.name=="Character Actions");
            if(action>=0){layers[action].defaultWeight=0;c.layers=layers;}
        }
        static void EnsureGripLayer(AnimatorController c)
        {
            if(c.layers.Any(l=>l.name=="Character Grip"))return;
            string path=DieselCharacterAuthoring.Root+"/Character_Fingers.mask";
            var mask=new AvatarMask();
            for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers,true);mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers,true);
            AssetDatabase.CreateAsset(mask,path);
            var sm=new AnimatorStateMachine{name="Character Grip"};AssetDatabase.AddObjectToAsset(sm,c);
            var grip=State(sm,"Weapon Grip",Clip("Pistol_Idle_Loop"));grip.speed=0;sm.defaultState=grip;
            c.AddLayer(new AnimatorControllerLayer{name="Character Grip",stateMachine=sm,avatarMask=mask,defaultWeight=0,blendingMode=AnimatorLayerBlendingMode.Override,iKPass=false});
        }
        static void BuildActions(AnimatorController c)
        {
            string path=DieselCharacterAuthoring.Root+"/Character_UpperBody.mask";
            var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if(!mask){mask=new AvatarMask();AssetDatabase.CreateAsset(mask,path);}
            for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
            foreach(var part in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers})mask.SetHumanoidBodyPartActive(part,true);
            var sm=new AnimatorStateMachine{name="Character Actions"};AssetDatabase.AddObjectToAsset(sm,c);
            var layer=new AnimatorControllerLayer{name="Character Actions",stateMachine=sm,avatarMask=mask,defaultWeight=1,blendingMode=AnimatorLayerBlendingMode.Override,iKPass=false};
            c.AddLayer(layer);
            var empty=sm.AddState("No Action");empty.writeDefaultValues=false;sm.defaultState=empty;
            AddAction(sm,empty,"Interact",Clip("Interact"),false);
            AddAction(sm,empty,"Pickup",Clip("PickUp_Table"),false);
            AddAction(sm,empty,"Consume",Clip("Consume"),false);
            AddAction(sm,empty,"Hit",Human("Combat/HumanM@CombatDamage01.fbx"),false);
            AddAction(sm,empty,"MeleeActive",Human("Combat/1H/HumanM@Attack1H01_R.fbx"),true);
            // Death/seat cancels all outstanding upper-body actions immediately.
            foreach(var child in sm.states.Where(s=>s.state!=empty))
            {
                Return(child.state,empty,"Dead",true);Return(child.state,empty,"Seated",true);
            }
            EditorUtility.SetDirty(mask);
        }
        static void AddAction(AnimatorStateMachine sm,AnimatorState empty,string parameter,AnimationClip clip,bool sustained)
        {
            var state=State(sm,parameter,clip);state.tag="Action";state.writeDefaultValues=false;
            if(sustained){state.timeParameter="MeleeTime";state.timeParameterActive=true;}
            var t=sm.AddAnyStateTransition(state);Setup(t,.08f);t.AddCondition(AnimatorConditionMode.If,0,parameter);Gate(t,false);
            var exit=state.AddTransition(empty);Setup(exit,.1f);
            if(sustained)exit.AddCondition(AnimatorConditionMode.IfNot,0,parameter);
            else{exit.hasExitTime=true;exit.exitTime=.95f;}
        }
        static void Param(AnimatorController c,string name,AnimatorControllerParameterType type)
        {
            if(c.parameters.Any(p=>p.name==name))return;
            c.AddParameter(new AnimatorControllerParameter{name=name,type=type,defaultBool=name=="Grounded"});
        }
        static void Gate(AnimatorStateTransition t,bool grounded)
        {
            t.AddCondition(AnimatorConditionMode.IfNot,0,"Dead");t.AddCondition(AnimatorConditionMode.IfNot,0,"Seated");
            if(grounded)t.AddCondition(AnimatorConditionMode.If,0,"Grounded");
        }
        static AnimatorState State(AnimatorStateMachine sm,string name,Motion clip)
        {var s=sm.AddState(name);s.motion=clip;s.writeDefaultValues=false;return s;}
        static void Setup(AnimatorStateTransition t,float duration)
        {t.hasExitTime=false;t.hasFixedDuration=true;t.duration=duration;t.canTransitionToSelf=false;t.interruptionSource=TransitionInterruptionSource.SourceThenDestination;}
        static void Return(AnimatorState from,AnimatorState to,string parameter,bool value)
        {var t=from.AddTransition(to);Setup(t,.1f);t.AddCondition(value?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,parameter);}
        static AnimationClip Clip(string name)
        {return AssetDatabase.LoadAssetAtPath<AnimationClip>(DieselCharacterAuthoring.Root+"/Clips/"+name+".anim")??throw new InvalidOperationException(name);}
        static AnimationClip Human(string relative)
        {
            var clip=AssetDatabase.LoadAllAssetsAtPath(DieselCharacterAuthoring.HumanRoot+relative).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__"));
            if(!clip||!clip.isHumanMotion)throw new InvalidOperationException("Humanoid clip required: "+relative);return clip;
        }
    }
}
