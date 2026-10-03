#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LastSignal.Inventory;
using LastSignal.Objectives;
using LastSignal.Persistence;
using LastSignal.Shelter;
using LastSignal.WorldTime;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Slice
{
    // Explicit technical acceptance. Positioning fixtures are never reported as human traversal.
    public sealed class S012Acceptance : MonoBehaviour
    {
        public SessionFlow flow;
        public RelayAcceptance relay;
        string output;
        int errors;
        public static readonly Vector3[] Direct = {new Vector3(-355.8f,.05f,-139),new Vector3(-290,.05f,-110),new Vector3(-190,.05f,-110),new Vector3(-170,.05f,-110),new Vector3(-160,.05f,-52),new Vector3(-150,.05f,-52),new Vector3(-150,.05f,100),new Vector3(-100,.05f,100)};
        public static readonly Vector3[] Covered = {new Vector3(-355.8f,.05f,-139),new Vector3(-300,.05f,-100),new Vector3(-300,.05f,20),new Vector3(-280,.05f,24),new Vector3(-300,.05f,24),new Vector3(-300,.05f,140),new Vector3(-100,.05f,140),new Vector3(-80,.05f,125),new Vector3(-80,.05f,100),new Vector3(-100,.05f,100)};
        public static readonly Vector3[] Return = {new Vector3(-100,.05f,100),new Vector3(-75,.05f,90),new Vector3(-38.5f,.05f,-130),new Vector3(-40,.05f,-170),new Vector3(-330,.05f,-170),new Vector3(-350,.05f,-139),new Vector3(-355.8f,.05f,-139)};
        static void Check(bool ok,string reason) => RelayAcceptance.Check(ok,reason);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i+1<args.Length;i++) if(args[i]=="-s012Acceptance")
                new GameObject("S012 technical acceptance").AddComponent<S012Acceptance>().output=args[i+1];
        }
        public void Bind()
        {
            flow=FindAnyObjectByType<SessionFlow>(); relay=gameObject.AddComponent<RelayAcceptance>();
            relay.flow=flow; relay.mission=flow.GetComponent<RelayMission>();
        }
        IEnumerator Start()
        {
            if(output==null)yield break;
            Directory.CreateDirectory(output); Application.logMessageReceived+=Observe;
            double started=Time.realtimeSinceStartupAsDouble;
            yield return null; Bind();
            var stack=new Stack<IEnumerator>(); stack.Push(All()); Exception failure=null;
            while(stack.Count>0)
            {
                bool next=false; object current=null;
                try { next=stack.Peek().MoveNext(); if(next)current=stack.Peek().Current; } catch(Exception e){failure=e;break;}
                if(!next){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            Application.logMessageReceived-=Observe;
            File.WriteAllText(Path.Combine(output,"standalone.txt"),$"Result={(failure==null&&errors==0?"PASS":"FAIL")}\nElapsedSeconds={Time.realtimeSinceStartupAsDouble-started:F3}\nErrors={errors}\nUnity={Application.unityVersion}\nHardware={SystemInfo.deviceModel}; {SystemInfo.processorType}; {SystemInfo.graphicsDeviceName}; RAM={SystemInfo.systemMemorySize}\nFixture positioning for interactions/combat/death; accelerated public motor traversal is NOT a human 30–45 minute run.\n{failure}");
            if(failure!=null)Debug.LogException(failure);
            Application.Quit(failure==null&&errors==0?0:1);
        }
        void Observe(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        IEnumerator Fresh(){flow.ReturnToMenu();yield return null;flow.BeginSession();yield return null;flow.Resume();}
        IEnumerator All()
        {
            yield return relay.Run(false,false,output,"clue-first");
            yield return Shelter(output);
            yield return DeathCheckpoint(output,"after-shelter");
            yield return Fresh(); yield return relay.Run(true,false,output,"fuse-first");
            yield return Fresh(); yield return relay.Run(true,true,output,"recovery");
            yield return Traverse(Direct,false,false,output,"direct-walk");
            yield return Traverse(Return,false,false,output,"relay-return-walk");
            yield return Traverse(Covered,false,false,output,"covered-walk");
            yield return Traverse(Direct,true,false,output,"direct-sprint");
            yield return Traverse(Covered,false,true,output,"covered-crouch");
            yield return Fresh(); yield return Melee(output);
            yield return Fresh(); yield return DeathCheckpoint(output,"before-expedition");
            relay.Pick(relay.mission.fuse);yield return null; yield return DeathCheckpoint(output,"carrying-fuse");
            relay.Use(relay.mission.radio);relay.mission.CloseJournal();yield return DeathCheckpoint(output,"mission-active");
            yield return Cells(output);
        }
        public IEnumerator DeathCheckpoint(string directory,string name)
        {
            var saves=flow.GetComponent<SaveSession>(); flow.Pause(); string path=Path.Combine(directory,name+"-checkpoint.json");
            Check(saves.Save(path).Success,saves.LastResult.Message); byte[] original=File.ReadAllBytes(path);
            int fuse=flow.Player.GetComponent<PlayerInventory>().GetTotalQuantity(relay.mission.fuse);
            int reward=relay.mission.Progress.RewardCount;
            // Explicit lethal-damage fixture: this verifies recovery, not an encounter win/loss.
            flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo{Amount=1000});
            Check(flow.PlayerDead,"Death fixture");Check(!saves.Save(path).Success,"Dead save must reject");
            Check(Convert.ToBase64String(original)==Convert.ToBase64String(File.ReadAllBytes(path)),"Rejected dead save changed checkpoint");
            flow.ReturnToMenu();yield return null;yield return saves.Load(path);Check(saves.LastResult.Success,saves.LastResult.Message);
            Check(!flow.PlayerDead&&flow.Player.GetComponent<PlayerInventory>().GetTotalQuantity(relay.mission.fuse)==fuse&&relay.mission.Progress.RewardCount==reward,"Death restored ownership/receipts");
            File.WriteAllText(Path.Combine(directory,name+"-death.txt"),"PASS lethal fixture -> rejected dead save -> unchanged checkpoint -> living load; fuse/reward preserved. No death bag.\n");
        }
        void Socket(ShelterSite site,int i)
        {
            var socket=site.sockets[i];relay.Place(new Vector3(socket.transform.position.x+1,.05f,socket.transform.position.z-(i==0?1.1f:0)));
            relay.Aim(socket.transform.position);Check(flow.Player.GetComponent<InteractionController>().TryInteract(),"Socket ray "+i);
        }
        void Use(ShelterPoint point)
        {
            relay.Place(new Vector3(point.transform.position.x,.05f,point.transform.position.z)+(point.Action==ShelterAction.Leave?Vector3.left:Vector3.right)*1.15f);
            relay.Aim(point.transform.position);Check(flow.Player.GetComponent<InteractionController>().TryInteract(),"Shelter interaction "+point.Action);
        }
        public IEnumerator Shelter(string directory)
        {
            var site=flow.GetComponent<ShelterSite>();var loop=flow.GetComponent<ShelterLoop>();
            relay.Pick(site.material);relay.Pick(site.fuel);
            if(loop.Inventory.GetTotalQuantity(site.recipe.tool)==0)relay.Pick(site.recipe.tool);
            LastSignal.Inventory.Data.ItemDefinition filler=null;
            foreach(var item in FindObjectsByType<WorldItem>())if(item.Available&&item.Definition!=site.material&&item.Definition!=site.fuel&&item.Definition!=site.recipe.tool&&item.Definition!=site.recipe.output){filler=item.Definition;break;}
            Check(filler,"Available ordinary filler");relay.Pick(filler);yield return null;
            if(loop.State==ExpeditionState.Expedition)Use(loop.ReturnPoint);
            for(int i=0;i<3;i++){Socket(site,i);if(i==0)site.Act(0);site.Act(1);Check(site.Production.Installed((ShelterModule)i),"Paid module "+i);site.Close();}
            Use(loop.StoragePoint);
            Check(loop.Transfer(true,site.material,14).Moved==14,"Deposit remaining scrap");
            Check(loop.Transfer(true,site.fuel,4).Moved==4,"Deposit fuel");
            Check(loop.Transfer(true,site.recipe.tool,1).Moved==1,"Deposit tool");
            Check(loop.Transfer(true,filler,1).Moved==1,"Fourth slot occupied");loop.ClosePreparation();
            Socket(site,2);site.Act(2);Check(site.Production.Status==CraftStatus.Running,"Craft running");
            site.Act(3);site.Act(4);Check(loop.Storage.GetTotalQuantity(site.material)==14,"Cancel conserved scrap");
            site.Act(2);site.Act(5);site.Act(6);site.Close();yield return new WaitForSeconds(2);
            // Controlled wound fixture isolates wound persistence from encounter skill.
            flow.Player.GetComponent<PlayerHealth>().TakeDamage(new DamageInfo{Amount=10});
            float wounded=flow.Player.GetComponent<PlayerHealth>().CurrentHealth;
            yield return relay.Roundtrip(Path.Combine(directory,"active-job.json"));
            Check(Mathf.Approximately(wounded,flow.Player.GetComponent<PlayerHealth>().CurrentHealth),"Wounded active-job generation restored");
            Check(site.Production.Status==CraftStatus.Running,"Restored active job");
            float deadline=Time.realtimeSinceStartup+30;
            while(site.Production.Status==CraftStatus.Running&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(site.Production.Status==CraftStatus.CompletedWaitingOutput,"Clock completed craft");
            Socket(site,2);site.Act(4);Check(site.Production.Status==CraftStatus.CompletedWaitingOutput,"Full storage preserves output");site.Close();
            yield return relay.Roundtrip(Path.Combine(directory,"full-output.json"));
            Use(loop.StoragePoint);Check(loop.Transfer(false,filler,1).Moved==1,"Free output slot");loop.ClosePreparation();
            Socket(site,2);site.Act(4);site.Act(4);Check(loop.Storage.GetTotalQuantity(site.recipe.output)==10,"Exactly one ammunition output");
            site.Act(7);Check(site.Production.Upgraded,"Paid upgrade");site.Close();
            Check(loop.Storage.GetTotalQuantity(site.material)==6,"20 scrap = 6 modules + 2 craft + 6 upgrade + 6 retained");
            yield return relay.Roundtrip(Path.Combine(directory,"shelter-complete.json"));
            Check(site.Production.Upgraded&&loop.Storage.GetTotalQuantity(site.recipe.output)==10,"Shelter and relay restored together");
            var bed=FindAnyObjectByType<RestPoint>();
            relay.Place(new Vector3(bed.transform.position.x+1.3f,.05f,bed.transform.position.z-1));
            var clock=flow.GetComponent<WorldClock>();flow.Resume();
            Check(clock.RequestSleep(bed,3600).Accepted,"Real shelter rest accepted");
            while(clock.Sleeping)yield return null;
            Check(flow.Player.GetComponent<PlayerHealth>().CurrentHealth>wounded,"Real sleep healed wound");
            File.WriteAllText(Path.Combine(directory,"shelter.txt"),"PASS real pickups, paid modules, 4-slot full output rejection, cancellation, active-job quit/load, output quit/load, 10 rifle ammo once, paid upgrade, final save/load. Fixture positioning.\n");
        }
        public IEnumerator Traverse(Vector3[] route,bool sprint,bool crouch,string directory,string name)
        {
            // Start positioning is explicit. Every following segment uses the production motor/collision.
            relay.Place(route[0]);flow.Resume();var motor=flow.Player.GetComponent<FirstPersonMotor>();
            var stance=flow.Player.GetComponent<PlayerStance>();Check(stance.TrySetCrouching(crouch),"Stance change");
            bool enabled=motor.enabled;motor.enabled=false;float seconds=0,distance=0;int steps=0;
            try
            {
                for(int i=1;i<route.Length;i++)
                {
                    float segment=0;
                    while(Vector2.Distance(new Vector2(flow.Player.transform.position.x,flow.Player.transform.position.z),new Vector2(route[i].x,route[i].z))>.18f)
                    {
                        Vector3 delta=route[i]-flow.Player.transform.position;delta.y=0;flow.Player.transform.rotation=Quaternion.LookRotation(delta);
                        Vector3 before=flow.Player.transform.position;motor.Simulate(Vector2.up,sprint,1f/60);
                        distance+=Vector3.Distance(before,flow.Player.transform.position);seconds+=1f/60;segment+=1f/60;
                        Check(segment<250,"Traversal blocked "+name+" segment="+i+" position="+flow.Player.transform.position);
                        Check(flow.Player.transform.position.y>-.1f,"Ground continuity");
                        if(++steps%120==0)yield return null;
                    }
                }
            }
            finally{motor.enabled=enabled;stance.TrySetCrouching(false);}
            File.WriteAllText(Path.Combine(directory,name+".txt"),$"PASS; motorDistanceMeters={distance:F2}; motorSimulationSeconds={seconds:F2}; sprint={sprint}; crouch={crouch}; frame-batched motor diagnostic; NOT wall-clock gameplay duration; input/encounter not assessed by this test.\n");
        }
        public IEnumerator Melee(string directory)
        {
            flow.Resume();yield return null;
            var actor=flow.GetComponent<ZombieEncounter>().Actor;
            var health=actor.GetComponent<ZombieHealth>();var combat=flow.Player.GetComponent<PlayerCombatController>();
            Check(combat.SelectSlot(PlayerCombatController.CombatSlot.Melee),"Select production crowbar");
            float before=health.CurrentHealth;int impacts=0;System.Action count=()=>impacts++;combat.Melee.ImpactCommitted+=count;
            try
            {
                for(int i=0;i<6&&health.CurrentHealth==before;i++)
                {
                    relay.Place(actor.transform.position+actor.transform.forward*1.25f);
                    var look=flow.Player.GetComponent<FirstPersonLook>();
                    var target=actor.transform.position+Vector3.up*1.2f;
                    var delta=target-look.View.transform.position;
                    flow.Player.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(delta,Vector3.up));
                    look.ApplyRecoil(Mathf.Atan2(-delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg-look.Pitch,0);
                    Check(combat.Melee.TryAttack(),"Crowbar accepted / "+combat.Melee.LastRejectionReason);
                    yield return new WaitForSeconds(1.2f);
                }
                Check(health.CurrentHealth<before&&impacts>0,"Actual crowbar damage and impact");
                Check(flow.Noise!=null,"Canonical gameplay noise remains bound");
            }
            finally{combat.Melee.ImpactCommitted-=count;combat.SelectSlot(PlayerCombatController.CombatSlot.Firearm);}
            File.WriteAllText(Path.Combine(directory,"melee.txt"),$"PASS production crowbar; initial health={before}, final={health.CurrentHealth}, impacts={impacts}; approach positioning fixture; live AI retained.\n");
        }
        public IEnumerator Cells(string directory)
        {
            var cells=flow.GetComponent<LastSignal.WorldCells.WorldCellManager>();
            foreach(var id in new List<string>(cells.DefinedCellIds))
            {
                Check(cells.Request(id),"Cell request");float deadline=Time.realtimeSinceStartup+15;
                while(cells.State(id)!=LastSignal.WorldCells.CellState.Ready&&Time.realtimeSinceStartup<deadline)yield return null;
                Check(cells.TryEnter(id),"CellReady enter");yield return relay.Roundtrip(Path.Combine(directory,id.Replace(':','-')+".json"));
                Check(cells.CurrentCell==id,"Restored player cell");
                Check(cells.ReturnToResident(new Vector3(-110,.05f,109)),"Resident return");yield return null;
                Check(cells.State(id)==LastSignal.WorldCells.CellState.Unloaded,"Old cell unloaded");
            }
            File.WriteAllText(Path.Combine(directory,"cells.txt"),"PASS both existing cells: request/CellReady/enter/save/menu/load/return/unload. Explicit portal traversal, not continuous walking.\n");
        }
    }
}
#endif
