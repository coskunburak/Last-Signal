#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Persistence;
using UnityEngine;

namespace LastSignal.Objectives
{
    /// <summary>Opt-in automated production route. Fixture positioning only; never grants inventory,
    /// objective facts, damage, reward or phase. Commands use the real interaction/combat authorities.</summary>
    public sealed class RelayAcceptance : MonoBehaviour
    {
        public SessionFlow flow;
        public RelayMission mission;
        public readonly List<string> Trace = new List<string>();
        string output;
        bool standalone;
        double started;
        int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++) if(args[i]=="-s011Acceptance")
            { var runner=new GameObject("S011 production acceptance").AddComponent<RelayAcceptance>(); runner.output=args[i+1]; runner.standalone=true; return; }
        }
        IEnumerator Start()
        {
            if(!standalone) yield break;
            Directory.CreateDirectory(output); started=Time.realtimeSinceStartupAsDouble; Application.logMessageReceived+=Observe;
            var stack=new Stack<IEnumerator>(); stack.Push(All()); Exception failure=null;
            while(stack.Count>0)
            {
                bool next=false; object current=null;
                try { next=stack.Peek().MoveNext(); if(next) current=stack.Peek().Current; } catch(Exception e) { failure=e; break; }
                if(!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if(current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
            while(stack.Count>0) (stack.Pop() as IDisposable)?.Dispose();
            Application.logMessageReceived-=Observe;
            Record($"Elapsed={Time.realtimeSinceStartupAsDouble-started:F3}s; errors={errors}; result="+(failure==null&&errors==0?"PASS":"FAIL "+failure));
            File.WriteAllLines(Path.Combine(output,"standalone.txt"),Trace);
            if(failure!=null) Debug.LogException(failure);
            Application.Quit(failure==null&&errors==0?0:1);
        }
        void Observe(string text,string stack,LogType type) { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert) errors++; }
        IEnumerator All()
        {
            yield return null; flow=FindAnyObjectByType<SessionFlow>(); mission=flow.GetComponent<RelayMission>();
            yield return Run(false,false,output,"route-a");
            flow.ReturnToMenu(); yield return null; flow.BeginSession(); yield return null;
            yield return Run(true,false,output,"route-b");
            flow.ReturnToMenu(); yield return null; flow.BeginSession(); yield return null;
            yield return Run(true,true,output,"recovery");
            Record("Independent fresh A/B/recovery sessions completed; fixture positioning, not human traversal.");
        }
        void Record(string line) { Trace.Add(line); if(output!=null) File.WriteAllLines(Path.Combine(output,"progress.txt"),Trace); }
        public static void Check(bool valid,string message) { if(!valid) throw new InvalidOperationException(message); }
        public void Place(Vector3 position) => WorldTime.WorldTimeAcceptanceRoute.Place(flow.Player,position);
        public void Aim(Vector3 position) { flow.Player.GetComponent<FirstPersonLook>().View.transform.LookAt(position); Physics.SyncTransforms(); }
        public void Use(RelayPoint point)
        {
            Place(new Vector3(point.transform.position.x+1.5f,.05f,point.transform.position.z)); Aim(point.transform.position);
            Check(flow.Player.GetComponent<InteractionController>().TryInteract(),"Production interaction: "+point.stableId+" / "+mission.Feedback);
        }
        public void Pick(Inventory.Data.ItemDefinition definition)
        {
            foreach(var item in FindObjectsByType<WorldItem>()) if(item.Available&&item.Definition==definition)
            {
                Place(new Vector3(item.transform.position.x+(item.transform.position.x>10?-1.3f:1.3f),.05f,item.transform.position.z)); Aim(item.transform.position);
                Check(flow.Player.GetComponent<InteractionController>().TryInteract(),"Production pickup: "+definition.Id+" target="+item.transform.position+" player="+flow.Player.transform.position+" resolve="+flow.Player.GetComponent<InteractionController>().Resolve()?.Prompt); return;
            }
            throw new InvalidOperationException("Missing world resource "+definition.Id);
        }
        public IEnumerator ClearThreat()
        {
            var zombie=FindAnyObjectByType<ZombieController>(); if(!zombie) yield break;
            var health=zombie.GetComponent<ZombieHealth>(); var weapon=flow.Player.GetComponent<PlayerCombatController>().Firearm;
            ZombieHitRegion chest=null; foreach(var region in zombie.GetComponentsInChildren<ZombieHitRegion>()) if(region.name=="Damage_Chest") chest=region;
            Check(chest,"Authored combat hit region");
            for(int shot=0;shot<20&&health.IsAlive;shot++)
            {
                Place(zombie.transform.position+zombie.transform.forward*4); Aim(chest.HitCollider.bounds.center);
                weapon.OnFirePressed(); weapon.OnFireReleased(); yield return new WaitForSeconds(.25f);
            }
            Check(!health.IsAlive,"POI cleared through production rifle, not deleted actors");
        }
        public IEnumerator Roundtrip(string path)
        {
            var saves=flow.GetComponent<SaveSession>(); flow.Pause(); Check(saves.Save(path).Success,saves.LastResult.Message);
            flow.ReturnToMenu(); yield return null; yield return saves.Load(path); Check(saves.LastResult.Success,saves.LastResult.Message); flow.Resume();
        }
        public IEnumerator Run(bool early,bool recover,string directory,string name)
        {
            double start=Time.realtimeSinceStartupAsDouble; flow.Resume(); yield return new WaitForSeconds(.5f);
            Check(mission.Validate(),"Mission authoring"); Check(!mission.Progress.Radio&&!mission.Progress.Repaired,"Fresh independent session");
            yield return ClearThreat();
            Check(!mission.Journal.Contains(RelayMission.Clue),"Unknown journal does not leak clue");
            if(early)
            {
                Pick(mission.fuse); yield return null;
                Check(mission.Progress.Acquired&&!mission.Progress.Radio,"Early committed acquisition");
                yield return Roundtrip(Path.Combine(directory,name+"-early.json")); Check(mission.Progress.Acquired&&!mission.Progress.Radio,"Early acquisition restored");
            }
            Use(mission.radio); Check(mission.Progress.Radio,"Radio committed"); mission.CloseJournal();
            Use(mission.radio); mission.CloseJournal(); Check(mission.Progress.RewardCount==0,"Reread and presentation close do not reward");
            Place(mission.relay.transform.position+Vector3.right*1.5f); Check(mission.BeginRepair()==0,"Missing tool rejects repair");
            if(!early) { Pick(mission.fuse); yield return null; }
            if(recover)
            {
                var inventory=flow.Player.GetComponent<PlayerInventory>(); int slot=-1; for(int i=0;i<inventory.Capacity;i++) if(inventory.GetSlot(i).Item==mission.fuse) slot=i;
                Check(slot>=0&&inventory.TryDrop(slot,1),"Production fuse drop"); yield return null;
                Use(mission.radio); Check(mission.Recover(),"Existing fuse recall"); mission.Recover(); mission.CloseJournal(); Pick(mission.fuse); yield return null;
                Check(inventory.GetTotalQuantity(mission.fuse)==1,"Repeated recall did not duplicate fuse");
            }
            Pick(mission.tool); yield return null;
            Use(mission.relay); long stale=mission.Pending; Place(Vector3.zero); yield return null; Check(mission.Pending==0&&!mission.FinishRepair(stale),"Walk-away invalidates stale callback");
            Use(mission.relay); stale=mission.Pending; mission.CancelRepair(); Check(!mission.FinishRepair(stale),"Explicit cancel invalidates callback");
            Use(mission.relay); yield return Roundtrip(Path.Combine(directory,name+"-pending.json"));
            Check(mission.Pending==0&&!mission.Progress.Repaired&&flow.Player.GetComponent<PlayerInventory>().GetTotalQuantity(mission.fuse)==1,"Save during repair is coherent precommit; no callback restored");
            Use(mission.relay); yield return new WaitForSeconds(3.2f);
            Check(mission.Progress.Repaired&&mission.Progress.RewardCount==1,"Repair and one intel reward / "+mission.Feedback+" player="+flow.Player.transform.position+" pending="+mission.Pending);
            Check(mission.BeginRepair()==0,"Repair retry rejected"); Use(mission.radio); mission.CloseJournal(); Check(mission.Progress.Listened,"Return/contact beat");
            yield return Roundtrip(Path.Combine(directory,name+"-complete.json"));
            Use(mission.radio); mission.CloseJournal();
            Check(mission.Progress.Repaired&&mission.Progress.RewardCount==1&&mission.Progress.Phase==1&&mission.Progress.Listened,"Completed load retains one reward and Contact phase");
            Check(!mission.Recover(),"Completed repair cannot recover fuse");
            Check(flow.Player.GetComponent<PlayerInventory>().GetTotalQuantity(mission.fuse)==0,"Consumed fuse absent");
            var semantic=mission.Progress.Capture(); semantic.completedAt=0; File.WriteAllText(Path.Combine(directory,name+"-semantic.json"),JsonUtility.ToJson(semantic,true));
            if(standalone) { Use(mission.radio); yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(directory,name+".png")); yield return null; mission.CloseJournal(); }
            string line=$"{name}: PASS; seconds={Time.realtimeSinceStartupAsDouble-start:F3}; completion=1 repair=1 reward=1 phase=1 fuse=0; save/load before and after commit; real interactions/combat with fixture positioning.";
            Record(line); File.WriteAllText(Path.Combine(directory,name+".txt"),line);
        }
    }
}
#endif
