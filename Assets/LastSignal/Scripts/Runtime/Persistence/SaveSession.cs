using System;
using System.Collections;
using System.Collections.Generic;
using LastSignal.Inventory;
using LastSignal.Inventory.Data;
using LastSignal.Loot;
using LastSignal.Shelter;
using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Persistence
{
    /// <summary>Session composition boundary with optional cell snapshots. Explicit API; no autosave, hotkeys or idle scanning.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(SessionFlow), typeof(LootPopulationService), typeof(ShelterLoop))]
    public sealed class SaveSession : MonoBehaviour
    {
        [SerializeField] ItemCatalog catalog;
        [SerializeField] WeaponDefinition rifle;
        [SerializeField] string worldId;
        [SerializeField] string contentVersion = "shelter-v1";
        const string WeaponId = "weapon.rifle";
        SessionFlow flow;
        SaveFileStore store;
        string storePath;
        bool busy;
        long restoreGeneration;
        public SaveResult LastResult { get; private set; }
        public double CaptureMilliseconds { get; private set; }
        public double WriteMilliseconds { get; private set; }
        public double ReadMilliseconds { get; private set; }
        public double HydrateMilliseconds { get; private set; }
        public string DefaultPath => System.IO.Path.Combine(Application.persistentDataPath, "saves", "current.json");
        public void Configure(ItemCatalog itemCatalog, WeaponDefinition weapon, string stableWorldId)
        { catalog = itemCatalog; rifle = weapon; worldId = stableWorldId; store = null; storePath = null; }
        SessionFlow Flow => flow ? flow : flow = GetComponent<SessionFlow>();
        public SaveResult ValidateAuthoring()
        {
            if (!catalog || !rifle || !rifle.HasValidAmmunitionConfiguration || string.IsNullOrWhiteSpace(worldId)) return Invalid("Save world/catalog/rifle not configured.");
            var definitions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in catalog.EditorItems)
                if (!item || !item.Id.IsValid || !definitions.Add(item.Id.Value) || !item.WorldPrefab || !item.WorldPrefab.GetComponent<WorldItem>() || item.MaxStack < 1)
                    return Invalid("Invalid or duplicate catalog identity/prefab.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var door in SceneComponents<DoorInteractable>())
                if (!AddIdentity(door.GetComponent<PersistentEntityId>(), ids)) return Invalid("Every persistent door requires a unique authored identity.");
            foreach (var encounter in SceneComponents<ZombieEncounter>())
                if (!AddIdentity(encounter.GetComponent<PersistentEntityId>(), ids)) return Invalid("Encounter requires a unique authored identity.");
            foreach (var point in SceneComponents<LootSpawnPoint>())
                if (!point.Validate(out _) || !ids.Add(point.StableId)) return Invalid("Invalid or duplicate loot point identity.");
            if (SceneComponents<ZombieEncounter>().Count != 1 || !GetComponent<ShelterLoop>().Validate(out _)) return Invalid("V1 requires the current one-encounter shelter composition.");
            return SaveResult.Ok;
        }
        static bool AddIdentity(PersistentEntityId identity, HashSet<string> ids) => identity && !string.IsNullOrWhiteSpace(identity.Id) && ids.Add(identity.Id);
        SaveCodec Codec()
        {
            var limits = new Dictionary<string,int>(StringComparer.Ordinal);
            foreach (var item in catalog.EditorItems) limits.Add(item.Id.Value,item.MaxStack);
            return new SaveCodec(new SaveValidation(worldId, contentVersion, limits, WeaponId, rifle.MagazineCapacity));
        }
        SaveFileStore Store(string path)
        {
            path = System.IO.Path.GetFullPath(path);
            if (store == null || storePath != path) { storePath = path; store = new SaveFileStore(path,Codec()); }
            return store;
        }
        public SaveResult Capture(out SaveGame snapshot)
        {
            snapshot = null;
            var clock = GetComponent<LastSignal.WorldTime.WorldClock>();
            var cells = GetComponent<LastSignal.WorldCells.WorldCellManager>();
            if (cells && !cells.Stable) return Busy();
            if (busy || OwnershipTransaction.Active || Flow.Restoring || (clock && (clock.Sleeping || (clock.Simulation != null && clock.Simulation.Advancing)))) return Busy();
            if (Flow.InMenu || Flow.PlayerDead) return Invalid("A living, ready session is required to save.");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var result = ValidateAuthoring(); if (!result.Success) return result;
            var player = Flow.Player; var inv = player.GetComponent<PlayerInventory>(); var shelter = GetComponent<ShelterLoop>();
            result = InventorySnapshots.Capture(inv,"player.inventory",out var carried); if (!result.Success) return result;
            result = InventorySnapshots.Capture(shelter.Storage,"shelter.storage",out var stash); if (!result.Success) return result;
            var weapon = player.GetComponent<PlayerCombatController>().ActiveWeapon;
            if (!weapon || weapon.Definition != rifle || weapon.RuntimeState == null) return Busy();
            var doors = new List<DoorSnapshot>();
            foreach (var door in SceneComponents<DoorInteractable>())
            {
                if (door.Busy) return new SaveResult(SaveError.Busy,"Wait for the door movement to finish.");
                doors.Add(new DoorSnapshot { id=door.GetComponent<PersistentEntityId>().Id,open=door.IsOpen });
            }
            doors.Sort((a,b)=>StringComparer.Ordinal.Compare(a.id,b.id));
            var points = new Dictionary<string,LootSpawnPoint>(StringComparer.Ordinal);
            foreach (var point in SceneComponents<LootSpawnPoint>()) points.Add(point.StableId,point);
            var live = new Dictionary<string,WorldItem>(StringComparer.Ordinal);
            foreach (var item in SceneComponents<WorldItem>())
            {
                if (!item.Available) continue;
                if (string.IsNullOrEmpty(item.PersistentId) || live.ContainsKey(item.PersistentId)) return Invalid("Duplicate world item identity.");
                live.Add(item.PersistentId,item);
            }
            var opportunities = new List<LootOpportunitySnapshot>(); var items = new List<WorldItemSnapshot>();
            var loot = GetComponent<LootPopulationService>();
            if (!loot.Populated || loot.Results.Count != points.Count) return Invalid("Loot baseline has unresolved points.");
            foreach (var resolved in loot.Results)
            {
                if (!points.TryGetValue(resolved.PointId,out var point) || resolved.Selection.Outcome == LootOutcome.Invalid) return Invalid("Unresolved/invalid loot baseline.");
                var opportunity = new LootOpportunitySnapshot { id=resolved.PointId };
                switch (resolved.Selection.Outcome)
                {
                    case LootOutcome.Spawned:
                        opportunity.outcome=OpportunityOutcome.Generated; opportunity.entityId="loot:"+resolved.PointId;
                        if (!loot.TryGetConsumed(opportunity.entityId,out bool consumed)) return Invalid("Unknown generated entity receipt.");
                        if (live.TryGetValue(opportunity.entityId,out var item))
                        {
                            if(consumed) return Invalid("Consumed entity is unexpectedly still present.");
                            items.Add(ItemState(item)); live.Remove(opportunity.entityId);
                        }
                        else if (!consumed) return Invalid("Generated entity disappeared without a consumption receipt.");
                        else items.Add(new WorldItemSnapshot { id=opportunity.entityId,definitionId=resolved.Selection.Item.Id.Value,
                            origin=WorldItemOrigin.Loot,disposition=EntityDisposition.Consumed,quantity=0,transform=Pose(point.transform) });
                        break;
                    case LootOutcome.Empty: opportunity.outcome=OpportunityOutcome.Empty; break;
                    case LootOutcome.Blocked: opportunity.outcome=OpportunityOutcome.Blocked; break;
                }
                opportunities.Add(opportunity);
            }
            foreach (var item in live.Values)
            {
                if (item.Origin != WorldItemOrigin.Drop) return Invalid("Unregistered authored/generated world item.");
                items.Add(ItemState(item));
            }
            items.Sort((a,b)=>StringComparer.Ordinal.Compare(a.id,b.id));
            var encounter = SceneComponents<ZombieEncounter>()[0]; var actor=encounter.Actor;
            if (!actor) return Invalid("Missing persistent encounter actor.");
            var look=player.GetComponent<FirstPersonLook>();
            snapshot = new SaveGame {
                worldTime=clock ? clock.Capture() : null,
                header=new SaveHeader { schemaVersion=cells ? (GetComponent<LastSignal.AI.WorldPopulationManager>() ? 4 : 3) : clock ? SaveValidation.SchemaVersion : 1,contentVersion=contentVersion,worldId=worldId,seed=loot.Seed,generation=1,
                    buildId=string.IsNullOrEmpty(Application.buildGUID)?"editor-"+Application.unityVersion:Application.buildGUID,timestampUtc=DateTimeOffset.UtcNow.ToString("O") },
                player=new PlayerSnapshot { id="player.local",transform=Pose(player.transform),health=player.GetComponent<PlayerHealth>().CurrentHealth,
                    pitch=look.Pitch,crouching=player.GetComponent<PlayerStance>().IsCrouching },
                inventory=carried,weapon=new WeaponSnapshot {definitionId=WeaponId,magazine=weapon.RuntimeState.CurrentMagazine},
                shelter=new ShelterSnapshot {storage=stash,onExpedition=shelter.State==ExpeditionState.Expedition,expeditionIndex=shelter.ExpeditionIndex},
                world=new WorldSnapshot {doors=doors.ToArray(),opportunities=opportunities.ToArray(),items=items.ToArray(),enemies=new[] {
                    new EnemySnapshot {id=encounter.GetComponent<PersistentEntityId>().Id,health=actor.GetComponent<ZombieHealth>().CurrentHealth,transform=Pose(actor.transform)} }}
            };
            if (cells)
            {
                try { snapshot.cells = cells.Capture(); }
                catch (InvalidOperationException e) { snapshot = null; return new SaveResult(SaveError.Busy,e.Message); }
            }
            
            var pop = GetComponent<LastSignal.AI.WorldPopulationManager>();
            if (pop && cells) snapshot.population = pop.GetSaveSnapshot();
            
            result = Codec().Encode(snapshot,out _); CaptureMilliseconds=watch.Elapsed.TotalMilliseconds;
            if (!result.Success) snapshot=null;
            return result;
        }
        public SaveResult Save(string path = null)
        {
            var result=Capture(out var snapshot); if (!result.Success) return LastResult=result;
            busy=true;
            try
            {
                var storage=Store(path??DefaultPath); var generation=storage.SessionGeneration;
                result=storage.Read(generation,out var previous);
                if (result.Success)
                {
                    if (previous.header.generation==long.MaxValue) return LastResult=Invalid("Save generation exhausted.");
                    snapshot.header.generation=previous.header.generation+1;
                }
                else if (result.Error!=SaveError.MissingFile) return LastResult=result;
                var watch=System.Diagnostics.Stopwatch.StartNew();
                result=storage.Write(snapshot,generation); WriteMilliseconds=watch.Elapsed.TotalMilliseconds;
                return LastResult=result;
            }
            catch (Exception e) when(e is System.IO.IOException || e is ArgumentException || e is UnauthorizedAccessException)
            { return LastResult=new SaveResult(SaveError.IoFailure,"Save path failed: "+e.GetType().Name); }
            finally { busy=false; }
        }
        /// <summary>Load is menu-only. Validate before creating a session; one frame allows existing weapon Start.</summary>
        public IEnumerator Load(string path = null)
        {
            if (busy || OwnershipTransaction.Active || !Flow.InMenu) { LastResult=Busy(); yield break; }
            restoreGeneration = 0;
            var operation = LoadCore(path);
            try { while (operation.MoveNext()) yield return operation.Current; }
            finally
            {
                (operation as IDisposable)?.Dispose();
                // Disposing an abandoned load cannot strand input in the restore state or affect
                // a replacement session. The disk source remains untouched.
                if (flow && restoreGeneration != 0 && flow.Generation == restoreGeneration && flow.Restoring)
                    flow.ReturnToMenu();
                restoreGeneration = 0; busy = false;
            }
        }
        IEnumerator LoadCore(string path)
        {
            if (busy || OwnershipTransaction.Active || !Flow.InMenu) { LastResult=Busy(); yield break; }
            var authoring=ValidateAuthoring(); if (!authoring.Success) { LastResult=authoring; yield break; }
            busy=true; SaveGame candidate=null; SaveResult read;
            var watch=System.Diagnostics.Stopwatch.StartNew();
            try { var storage=Store(path??DefaultPath); read=storage.Read(storage.SessionGeneration,out candidate); }
            catch (Exception e) { read=Invalid("Load path failed: "+e.GetType().Name); }
            ReadMilliseconds=watch.Elapsed.TotalMilliseconds;
            if (!read.Success) { LastResult=read; busy=false; yield break; }
            var topology=ValidateTopology(candidate);
            if (!topology.Success) { LastResult=topology; busy=false; yield break; }
            var originalDoors=new Dictionary<DoorInteractable,bool>();
            foreach (var door in SceneComponents<DoorInteractable>()) originalDoors.Add(door,door.IsOpen);
            long generation=0;
            try { Flow.BeginRestoreSession(); generation=Flow.Generation; restoreGeneration=generation; }
            catch (Exception e) { LastResult=Invalid("Session initialization failed: "+e.GetType().Name); }
            if (generation==0 || !Flow.Player) { FailHydration(originalDoors); yield break; }
            // Existing PlayerCombatController.Start creates and initializes the production rifle.
            yield return null;
            if (!this || !Flow || Flow.Generation!=generation || !Flow.Restoring || !Flow.Player)
            { LastResult=new SaveResult(SaveError.StaleSession,"Load session was replaced; no hydration applied."); busy=false; yield break; }
            var cells = GetComponent<LastSignal.WorldCells.WorldCellManager>();
            if (cells)
            {
                GetComponent<LastSignal.WorldTime.WorldClock>().Restore(candidate.worldTime);
                yield return cells.Restore(candidate.cells, candidate.worldTime.seconds);
                if (Flow.Generation != generation || !Flow.Restoring) { LastResult = new SaveResult(SaveError.StaleSession,"Cell restore session replaced."); yield break; }
                if (cells.Failure != null || !cells.Stable) { LastResult = Invalid(cells.Failure ?? "Cell restore incomplete."); FailHydration(originalDoors); yield break; }
            }
            watch.Restart();
            try
            {
                Hydrate(candidate);
                Physics.SyncTransforms();
                if (!PlayerLocationClear()) throw new InvalidOperationException("Saved player capsule overlaps solid world geometry.");
                // Finalize transform-dependent state only after all hydration and physics synchronization.
                var clock=GetComponent<LastSignal.WorldTime.WorldClock>();
                if(clock) clock.Restore(candidate.worldTime ?? LastSignal.WorldTime.WorldTimeSnapshot.LegacyDefault());
                Flow.CompleteRestore(); LastResult=SaveResult.Ok;
            }
            catch (Exception e)
            {
                LastResult=Invalid("Hydration failed safely: "+e.GetType().Name+": "+e.Message);
                FailHydration(originalDoors);
            }
            finally { HydrateMilliseconds=watch.Elapsed.TotalMilliseconds; busy=false; }
        }
        void FailHydration(Dictionary<DoorInteractable,bool> originalDoors)
        {
            Flow.ReturnToMenu();
            foreach(var pair in originalDoors) if(pair.Key) pair.Key.RestoreOpen(pair.Value);
            busy=false;
        }
        SaveResult ValidateTopology(SaveGame state)
        {
            var cells = GetComponent<LastSignal.WorldCells.WorldCellManager>();
            if ((cells && (state.header.schemaVersion < 3 || !cells.ValidateTopology(state.cells))) || (!cells && state.header.schemaVersion >= 3)) return Invalid("Incompatible cell topology/schema.");
            if(state.header.schemaVersion >= 2 && !GetComponent<LastSignal.WorldTime.WorldClock>()) return Invalid("This scene cannot restore world-time schema 2.");
            if(state.player.id!="player.local" || state.inventory.id!="player.inventory" || state.shelter.storage.id!="shelter.storage") return Invalid("Unexpected canonical owner identity.");
            var expected=new HashSet<string>(StringComparer.Ordinal);
            foreach(var door in SceneComponents<DoorInteractable>()) expected.Add(door.GetComponent<PersistentEntityId>().Id);
            if(expected.Count!=state.world.doors.Length) return Invalid("Door topology changed.");
            foreach(var door in state.world.doors) if(!expected.Remove(door.id)) return Invalid("Unknown door identity.");
            foreach(var point in SceneComponents<LootSpawnPoint>()) expected.Add(point.StableId);
            if(expected.Count!=state.world.opportunities.Length) return Invalid("Loot topology changed.");
            foreach(var point in state.world.opportunities)
            {
                if(!expected.Remove(point.id)) return Invalid("Unknown loot point.");
                if(point.outcome==OpportunityOutcome.Generated && point.entityId!="loot:"+point.id) return Invalid("Loot entity belongs to the wrong opportunity.");
            }
            var encounter=SceneComponents<ZombieEncounter>()[0];
            if(state.world.enemies.Length!=1 || state.world.enemies[0].id!=encounter.GetComponent<PersistentEntityId>().Id) return Invalid("Enemy topology changed.");
            foreach(var item in state.world.items)
                if(item.origin==WorldItemOrigin.Authored) return Invalid("Authored WorldItem hydration is not supported by this world version.");
            // Menu must not contain unowned authored items: those require their own baseline contract.
            foreach(var item in SceneComponents<WorldItem>()) if(item.Available) return Invalid("Unexpected authored world item in menu baseline.");
            return SaveResult.Ok;
        }
        void Hydrate(SaveGame state)
        {
            var player=Flow.Player; var shelter=GetComponent<ShelterLoop>();
            Require(InventorySnapshots.Restore(player.GetComponent<PlayerInventory>(),state.inventory,"player.inventory",catalog));
            Require(InventorySnapshots.Restore(shelter.Storage,state.shelter.storage,"shelter.storage",catalog));
            var doors=new Dictionary<string,DoorInteractable>(StringComparer.Ordinal);
            foreach(var door in SceneComponents<DoorInteractable>()) doors.Add(door.GetComponent<PersistentEntityId>().Id,door);
            foreach(var door in state.world.doors) doors[door.id].RestoreOpen(door.open);
            GetComponent<LootPopulationService>().Restore(state,catalog);
            var pop = GetComponent<LastSignal.AI.WorldPopulationManager>();
            if (pop) pop.SyncFromSave(state.population);
            var capsule=player.GetComponent<CharacterController>(); capsule.enabled=false;
            ApplyPose(player.transform,state.player.transform); capsule.enabled=true;
            if(!player.GetComponent<PlayerStance>().TrySetCrouching(state.player.crouching)) throw new InvalidOperationException("Saved stance is obstructed.");
            player.GetComponent<FirstPersonLook>().RestorePitch(state.player.pitch);
            player.GetComponent<PlayerHealth>().RestoreHealth(state.player.health);
            var weapon=player.GetComponent<PlayerCombatController>().ActiveWeapon;
            if(!weapon || weapon.Definition!=rifle) throw new InvalidOperationException("Missing production rifle.");
            weapon.Initialize(player.GetComponent<FirstPersonLook>().View.transform,player,state.weapon.magazine); weapon.RequestEquip();
            shelter.RestoreExpedition(state.shelter.onExpedition,state.shelter.expeditionIndex);
            var actor=SceneComponents<ZombieEncounter>()[0].Actor; var enemy=state.world.enemies[0];
            var agent=actor.GetComponent<NavMeshAgent>(); var position=Position(enemy.transform);
            if(!NavMesh.SamplePosition(position,out var hit,.5f,NavMesh.AllAreas) || !agent.Warp(hit.position)) throw new InvalidOperationException("Saved enemy is outside navigation.");
            actor.transform.rotation=Rotation(enemy.transform); actor.GetComponent<ZombieHealth>().RestoreHealth(enemy.health);
        }
        bool PlayerLocationClear()
        {
            var c=Flow.Player.GetComponent<CharacterController>(); var center=Flow.Player.transform.TransformPoint(c.center);
            float offset=Mathf.Max(0,c.height*.5f-c.radius);
            return !Physics.CheckCapsule(center+Vector3.up*offset,center-Vector3.up*offset,c.radius*.95f,~((1<<8)|(1<<2)),QueryTriggerInteraction.Ignore);
        }
        List<T> SceneComponents<T>() where T:Component
        {
            var found=new List<T>();
            foreach(var root in gameObject.scene.GetRootGameObjects())
                if(root.activeInHierarchy && !root.GetComponent<LastSignal.WorldCells.WorldCellContent>()) found.AddRange(root.GetComponentsInChildren<T>(false));
            return found;
        }
        static WorldItemSnapshot ItemState(WorldItem item) => new WorldItemSnapshot {id=item.PersistentId,definitionId=item.Definition.Id.Value,
            origin=item.Origin,disposition=EntityDisposition.Present,quantity=item.Quantity,transform=Pose(item.transform)};
        public static TransformSnapshot Pose(Transform t) { var p=t.position;var q=t.rotation;return new TransformSnapshot{x=p.x,y=p.y,z=p.z,qx=q.x,qy=q.y,qz=q.z,qw=q.w}; }
        static Vector3 Position(TransformSnapshot p)=>new Vector3(p.x,p.y,p.z);
        static Quaternion Rotation(TransformSnapshot p)=>new Quaternion(p.qx,p.qy,p.qz,p.qw);
        static void ApplyPose(Transform t,TransformSnapshot p)=>t.SetPositionAndRotation(Position(p),Rotation(p));
        static void Require(SaveResult result) { if(!result.Success) throw new InvalidOperationException(result.Message); }
        static SaveResult Invalid(string message)=>new SaveResult(SaveError.InvalidData,message);
        static SaveResult Busy()=>new SaveResult(SaveError.Busy,"A ready session and completed ownership transaction are required.");
    }
}
