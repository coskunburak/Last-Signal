using System;
using System.Collections.Generic;
using LastSignal.Inventory;
using LastSignal.Persistence;
using LastSignal.WorldCells;
using UnityEngine;

namespace LastSignal.Gathering
{
    [DisallowMultipleComponent, RequireComponent(typeof(SaveSession))]
    public sealed class GatheringWorld : MonoBehaviour
    {
        public HarvestManifest manifest;
        public ResourceNodeView[] residentViews = Array.Empty<ResourceNodeView>();
        public Collider[] protectedAreas = Array.Empty<Collider>();
        public Collider[] additionalGround = Array.Empty<Collider>();
        [Tooltip("Owner must review roads, exits, POI and navigation exclusions in the isolated stage.")]
        public bool clearanceApproved;
        SessionFlow flow; SaveSession save; WorldCellManager cells;
        GatheringSnapshot state;
        string sessionToken;
        long generation;
        bool physicsSuspended;
        readonly Dictionary<string,ResourceNodeView> views = new Dictionary<string,ResourceNodeView>(StringComparer.Ordinal);
        readonly Dictionary<string,WorldItem> pickups = new Dictionary<string,WorldItem>(StringComparer.Ordinal);
        readonly Dictionary<string,GameObject> pickupRoots = new Dictionary<string,GameObject>(StringComparer.Ordinal);
        readonly RaycastHit[] groundHits = new RaycastHit[16];
        readonly Collider[] overlaps = new Collider[32];
        public bool RecoveryRequired { get; private set; }
        public string PlayerFeedback { get; private set; } = "";
        public float FeedbackUntil { get; private set; }
        public void ReportHit(HarvestReason reason)
        {
            PlayerFeedback=reason==HarvestReason.Accepted?"Wood cut":reason==HarvestReason.WrongTool?"Requires axe":
                reason==HarvestReason.BrokenTool?"Axe broken":reason==HarvestReason.Depleted?"Tree already felled":
                reason==HarvestReason.PhysicsNotReady?"This tree is not ready for chopping":
                reason==HarvestReason.AwaitingFall?"Cut complete — stand clear of the tree":
                reason==HarvestReason.Blocked?"Aim at the existing trunk notch":reason==HarvestReason.Busy?BusyFeedback():
                "Tree cannot be harvested";
            FeedbackUntil=Time.unscaledTime+2.5f;
        }
        string BusyFeedback()
        {
            if(RecoveryRequired) return "Harvesting unavailable — restore a verified checkpoint";
            if(!clearanceApproved) return "Harvesting unavailable — area clearance not approved";
            if(flow && flow.Restoring) return "Loading harvest state";
            if(flow && flow.Paused) return "Resume the game to harvest";
            if(cells && (!cells.Stable || cells.Failure!=null)) return "Harvesting unavailable — area not ready";
            if(!Ready) return "Harvesting unavailable";
            // A ready world's Busy rejection is the simultaneous physical-tree activation limit.
            return "Wait for nearby trees to settle";
        }
        public string Feedback { get; private set; } = "Gathering not initialized";
        public bool Ready => state != null && !RecoveryRequired && clearanceApproved && flow && flow.Player &&
            flow.Generation==generation && !flow.Paused && !flow.Restoring && !flow.PlayerDead &&
            !OwnershipTransaction.Active && (!cells || (cells.Stable && cells.Failure==null));
        public void Begin()
        {
            End(); flow=GetComponent<SessionFlow>(); save=GetComponent<SaveSession>(); cells=GetComponent<WorldCellManager>();
            generation=flow.Generation; sessionToken=Guid.NewGuid().ToString("N");
            if (!manifest || !manifest.Valid) { Feedback="Invalid harvest manifest"; return; }
            var nodes=new ResourceNodeState[manifest.anchors.Length];
            for (int i=0;i<nodes.Length;i++)
            {
                var a=manifest.anchors[i];
                if (!save.Catalog || save.Catalog.GetItem(a.definition.output.Id)!=a.definition.output) { Feedback="Harvest output missing from session catalog"; return; }
                nodes[i]=GatheringState.Create(a.nodeId,a.cellId,a.definition,new TransformSnapshot {
                    x=a.position.x,y=a.position.y,z=a.position.z,qx=a.rotation.x,qy=a.rotation.y,qz=a.rotation.z,qw=a.rotation.w });
            }
            state=new GatheringSnapshot { nodes=nodes,manifestId=manifest.manifestId,manifestRevision=manifest.revision };
            if (cells) { cells.CellReady+=CellReady; cells.CellLeaving+=CellLeaving; }
            foreach (var view in residentViews) if (view) { view.world=this; Register(view); }
            try { Capture(); }
            catch (InvalidOperationException e) { RecoveryRequired=true; Feedback=e.Message; }
            flow.Player.GetComponent<PlayerCombatController>().EnsureStartingEquipment();
            if (flow.Player) foreach (var melee in flow.Player.GetComponent<PlayerCombatController>().MeleeWeapons)
                if (melee) melee.BindGathering(this);
            if (!RecoveryRequired) Feedback=clearanceApproved ? "Gathering ready" : "Owner clearance review required";
        }
        public void End()
        {
            if (cells) { cells.CellReady-=CellReady; cells.CellLeaving-=CellLeaving; }
            ClearPickups();
            foreach (var view in views.Values) if (view) view.Present(null);
            views.Clear(); state=null; physicsSuspended=false;
        }
        void ClearPickups()
        {
            foreach (var root in pickupRoots.Values) if (root) { root.SetActive(false); Destroy(root); }
            pickupRoots.Clear(); pickups.Clear();
        }
        void FixedUpdate()
        {
            if(state==null || !flow) return;
            bool suspend=flow.PlayerDead || flow.Restoring || (cells && !cells.Stable);
            if(suspend && !physicsSuspended)
            { physicsSuspended=true; foreach(var view in views.Values) if(view && view.physicsBody) view.physicsBody.Suspend(); }
            else if(!suspend && physicsSuspended && Ready)
            { physicsSuspended=false; foreach(var view in views.Values) if(view && view.physicsBody) view.physicsBody.Resume(false); }
        }
        void OnDisable() => End();
        public void Register(ResourceNodeView view)
        {
            if (!view || state==null || GatheringState.Find(state,view.nodeId)==null) return;
            if (views.TryGetValue(view.nodeId,out var old) && old && old!=view)
            { RecoveryRequired=true; Feedback="Duplicate harvest anchor"; return; }
            var node=GatheringState.Find(state,view.nodeId);
            if (!SamePose(node.anchor,view.transform.position,view.transform.rotation) ||
                (cells && cells.OwnerForPosition(view.transform.position)!=node.cellId))
            { RecoveryRequired=true; Feedback="Harvest anchor pose/owner cell mismatch"; return; }
            if ((node.tree!=null) != (view.physicsBody!=null) || (view.physicsBody && !view.physicsBody.Valid))
            { RecoveryRequired=true; Feedback="Physical tree authoring mismatch"; return; }
            views[view.nodeId]=view; view.Present(GatheringState.Find(state,view.nodeId));
        }
        public void Unregister(ResourceNodeView view)
        {
            if (view && views.TryGetValue(view.nodeId,out var old) && old==view)
            {
                if(view.physicsBody) view.physicsBody.Suspend();
                views.Remove(view.nodeId);
            }
        }
        void CellReady(string id)
        {
            var content=cells.Content(id);
            if (content) foreach (var view in content.GetComponentsInChildren<ResourceNodeView>(true))
            { view.world=this; Register(view); }
            try { Project(); Capture(); }
            catch (Exception e) { RequireRecovery(); if (flow && !flow.Restoring) flow.Pause(); Debug.LogException(e); }
        }
        void CellLeaving(string id)
        {
            if (state==null) return;
            SampleTrees();
            foreach(var view in views.Values) if(view && view.physicsBody && GatheringState.Find(state,view.nodeId)?.cellId==id)
                view.physicsBody.gameObject.SetActive(false);
            foreach (var n in state.nodes) if (n.cellId==id && n.pickup!=null && pickups.TryGetValue(n.pickup.id,out var p))
            {
                if (pickupRoots.TryGetValue(n.pickup.id,out var root) && root) { root.SetActive(false); Destroy(root); }
                pickupRoots.Remove(n.pickup.id); pickups.Remove(n.pickup.id);
            }
        }
        public GatheringSnapshot Capture()
        {
            if (state==null || RecoveryRequired) throw new InvalidOperationException("Gathering recovery/authoring required.");
            foreach (var n in state.nodes)
                if (ResidentOrLoaded(n.cellId) && (!views.TryGetValue(n.id,out var view) || !view ||
                    !SamePose(n.anchor,view.transform.position,view.transform.rotation)))
                    throw new InvalidOperationException("Missing/relocated ready harvest anchor.");
            foreach (var n in state.nodes)
                if (n.reward==RewardLocation.World && ResidentOrLoaded(n.cellId) &&
                    (!pickups.TryGetValue(n.pickup.id,out var p) || !p || !OwnsPickup(p)))
                    throw new InvalidOperationException("Harvest projection disappeared without a pickup receipt.");
            SampleTrees();
            return GatheringState.Copy(state);
        }
        public bool Compatible(GatheringSnapshot value)
        {
            if (!manifest || !manifest.Valid) return false;
            if (value==null) return true; // Old save: untouched authored nodes, not a respawn of persisted nodes.
            if (!GatheringState.Valid(value) || value.manifestId!=manifest.manifestId || value.manifestRevision!=manifest.revision || value.nodes.Length!=manifest.anchors.Length) return false;
            foreach (var a in manifest.anchors)
            {
                var n=GatheringState.Find(value,a.nodeId); var d=a.definition;
                if (n==null || !SamePose(n.anchor,a.position,a.rotation) || n.cellId!=a.cellId || n.definitionId!=d.definitionId || n.definitionRevision!=d.revision ||
                    (n.tree!=null)!=d.physicalTree || (n.tree?.version==2 && Mathf.Abs(n.tree.cutStep-d.PhysicalPenetration)>.00001f) ||
                    n.maximumHealth!=d.health || n.damage!=d.strikeDamage || n.budget!=d.yield || n.outputId!=d.output.Id.Value) return false;
            }
            return true;
        }
        static bool SamePose(TransformSnapshot p,Vector3 position,Quaternion rotation) => p!=null &&
            Vector3.Distance(new Vector3(p.x,p.y,p.z),position)<.001f &&
            Mathf.Abs(Quaternion.Dot(new Quaternion(p.qx,p.qy,p.qz,p.qw),rotation))>.99999f;
        public void Restore(GatheringSnapshot value)
        {
            if (!Compatible(value) || state==null) throw new InvalidOperationException("Gathering catalog/anchor mismatch.");
            ClearPickups();
            foreach(var view in views.Values) if(view && view.physicsBody) view.physicsBody.ResetProjection();
            if (value!=null) state=GatheringState.Copy(value);
            RecoveryRequired=false;
            foreach (var view in residentViews) if (view) Register(view);
            if (cells) foreach (var id in cells.DefinedCellIds) if (cells.State(id)==CellState.Ready) CellReady(id);
            if (RecoveryRequired) throw new InvalidOperationException("Harvest anchor registration failed during restore.");
            Project(); Capture();
        }
        internal void Adopt(GatheringSnapshot candidate) { state=candidate; }
        internal void RequireRecovery() { RecoveryRequired=true; Feedback="Save outcome unknown: load a verified checkpoint before gathering"; }
        public bool Pending(string id) => GatheringState.Find(state,id)?.reward==RewardLocation.Pending;
        public bool ToolUsable => state!=null && state.axeCondition>0;
        internal HarvestReason TryHit(ResourceNodeView view, MeleeWeaponController weapon, ulong swing, Collider hit, Vector3 point)
        {
            // Legacy S003 save/pickup data stays supported, but live axe hits must never enter its
            // health-to-hidden-tree shortcut. Missing authoring cannot silently bypass S004 physics.
            if(!view || !view.physicsBody || !view.physicsBody.Valid) return HarvestReason.PhysicsNotReady;
            if (!Ready) return HarvestReason.Busy;
            var n=GatheringState.Find(state,view.nodeId);
            var combat=flow.Player.GetComponent<PlayerCombatController>();
            if (!weapon || weapon.GatheringAuthority!=this || weapon.Owner!=flow.Player || !weapon.Allowed || weapon.Simulation==null ||
                weapon.Simulation.State!=MeleeState.Active || weapon.Simulation.SwingId!=swing || swing>long.MaxValue ||
                combat.SelectedSlot!=PlayerCombatController.CombatSlot.Melee || combat.Melee!=weapon ||
                combat.FindMelee(MeleeWeaponDefinition.AxeId)!=weapon || weapon.Definition.WeaponId!=MeleeWeaponDefinition.AxeId)
                return HarvestReason.WrongTool;
            if (n==null || !SamePose(n.anchor,view.transform.position,view.transform.rotation) || !hit || !hit.enabled || !hit.gameObject.activeInHierarchy || hit.GetComponentInParent<ResourceNodeView>()!=view ||
                !views.TryGetValue(view.nodeId,out var registered) || registered!=view || !CellUsable(n.cellId)) return HarvestReason.Invalid;
            var delta=point-weapon.Eye.position;
            if (!float.IsFinite(delta.sqrMagnitude) || delta.magnitude>weapon.Definition.Range || delta.magnitude<.001f ||
                Vector3.Dot(weapon.Eye.forward,delta.normalized)<.65f || Vector3.Distance(weapon.Eye.position,weapon.Origin.position)>1.5f)
                return HarvestReason.Blocked;
            if(view.physicsBody)
            {
                if(!view.physicsBody.Accepts(hit,point)) { Feedback="Strike the trunk at the existing notch"; return HarvestReason.Blocked; }
                int active=0; foreach(var v in views.Values) if(v && v.physicsBody && v.physicsBody.ActiveSimulation) active++;
                if(!view.physicsBody.ActiveSimulation && active>=4) { Feedback="Wait for nearby trees to settle"; return HarvestReason.Busy; }
                SampleTrees();
                var local=view.transform.InverseTransformPoint(point);
                var direction=view.transform.InverseTransformDirection(weapon.Eye.forward);
                var cut=TreeStructure.Cut(state,n.id,n.revision,sessionToken,(long)swing,weapon.Definition.WeaponId,local,direction,out var next);
                if(cut!=HarvestReason.Accepted) { Feedback=cut==HarvestReason.Blocked?"Continue the existing notch":cut.ToString(); return cut; }
                var committed=Commit(next);
                if(committed==HarvestReason.Accepted && !RecoveryRequired) view.physicsBody.AcceptedHit(point,weapon.Eye.forward);
                return committed;
            }
            return HarvestReason.PhysicsNotReady;
        }
        public bool ContainsResidentTreePosition(Vector3 position)
        {
            if(state==null || !TreeStructure.Finite(position)) return false;
            foreach(var pair in views)
            {
                var view=pair.Value; var n=GatheringState.Find(state,pair.Key);
                if(!view || n==null || n.cellId!="resident") continue;
                float radius=view.physicsBody && view.physicsBody.profile?view.physicsBody.profile.height+2:3;
                if((view.transform.position-position).sqrMagnitude<=radius*radius) return true;
            }
            return false;
        }
        void SampleTrees()
        {
            if(state==null) return;
            foreach(var pair in views) if(pair.Value && pair.Value.physicsBody && pair.Value.physicsBody.isActiveAndEnabled)
                pair.Value.physicsBody.Sample(GatheringState.Find(state,pair.Key)?.tree);
        }
        internal void FailTreePhysics() { RequireRecovery(); if(flow) flow.Pause(); }
        internal bool CommitTreeFailure(TreePhysicsBody physical, long expectedRevision)
        {
            if(!physical || !physical.node || !Ready || !views.TryGetValue(physical.node.nodeId,out var view) ||
                view!=physical.node || view.physicsBody!=physical) { RequireRecovery(); if(flow) flow.Pause(); return false; }
            SampleTrees();
            var reason=TreeStructure.Fracture(state,view.nodeId,expectedRevision,out var candidate);
            if(reason==HarvestReason.Duplicate) return true;
            if(reason==HarvestReason.Accepted && Commit(candidate)==HarvestReason.Accepted && !RecoveryRequired) return true;
            RequireRecovery(); if(flow) flow.Pause(); return false;
        }
        bool CellUsable(string id) => !cells ? id=="resident" : cells.Stable && cells.CurrentCell==id && (id=="resident" || cells.State(id)==CellState.Ready);
        HarvestReason Commit(GatheringSnapshot candidate)
        {
            var outcome=save.CommitGathering(this,candidate,out var result);
            if (outcome==DurableOutcome.RecoveryRequired) { RequireRecovery(); flow.Pause(); return HarvestReason.RecoveryRequired; }
            if (outcome!=DurableOutcome.Committed) { Feedback="Save rejected: "+result.Error; return HarvestReason.PersistenceFailed; }
            Feedback="Harvest committed";
            try { Project(); } catch (Exception e) { Feedback="Committed; projection requires reload"; RequireRecovery(); flow.Pause(); Debug.LogException(e); }
            return HarvestReason.Accepted;
        }
        public bool RetryReward(ResourceNodeView view)
        {
            if (!Ready || !view || (view.physicsBody && !view.physicsBody.CanCollect) || Vector3.Distance(flow.Player.transform.position,view.transform.position)>2.5f) return false;
            var n=GatheringState.Find(state,view.nodeId);
            if (n==null || !SamePose(n.anchor,view.transform.position,view.transform.rotation) || !views.TryGetValue(n.id,out var registered) || registered!=view || view.world!=this ||
                n.reward!=RewardLocation.Pending || !CellUsable(n.cellId) || state.sequence==long.MaxValue) return false;
            var ground=Ground(view,n.cellId); if (ground==null) { Feedback="No safe drop point; wood remains pending"; return false; }
            SampleTrees();
            var candidate=GatheringState.Copy(state); candidate.sequence++;
            var changed=GatheringState.Find(candidate,n.id); changed.reward=RewardLocation.World; changed.pickup.transform=ground;
            return Commit(candidate)==HarvestReason.Accepted;
        }
        TransformSnapshot Ground(ResourceNodeView view,string cell)
        {
            if (!clearanceApproved) return null;
            for (int attempt=0;attempt<12;attempt++)
            {
                float angle=(attempt%6)*Mathf.PI/3;
                var candidate=view.transform.position+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(attempt<6?2f:3f);
                int count=Physics.RaycastNonAlloc(candidate+Vector3.up*3,Vector3.down,groundHits,6,~(1<<2),QueryTriggerInteraction.Ignore);
                if (count==groundHits.Length) continue;
                RaycastHit ground=default; float closest=float.MaxValue;
                for (int i=0;i<count;i++) if (groundHits[i].distance<closest) { closest=groundHits[i].distance; ground=groundHits[i]; }
                if (!ground.collider || ground.normal.y<.85f) continue;
                bool surface=ground.collider is TerrainCollider;
                foreach (var allowed in additionalGround) if (allowed==ground.collider) surface=true;
                if (!surface) continue;
                var pos=ground.point+Vector3.up*.26f;
                if (cells && (cells.OwnerForPosition(pos)!=cell ||
                    cells.OwnerForPosition(pos+new Vector3(.6f,0,.3f))!=cell || cells.OwnerForPosition(pos+new Vector3(-.6f,0,.3f))!=cell ||
                    cells.OwnerForPosition(pos+new Vector3(.6f,0,-.3f))!=cell || cells.OwnerForPosition(pos+new Vector3(-.6f,0,-.3f))!=cell)) continue;
                var bounds=new Bounds(pos,new Vector3(1.2f,.5f,.6f)); bool blocked=false;
                foreach (var area in protectedAreas) if (area && area.bounds.Intersects(bounds)) { blocked=true; break; }
                if (blocked) continue;
                count=Physics.OverlapBoxNonAlloc(pos,bounds.extents,overlaps,Quaternion.identity,~(1<<2),QueryTriggerInteraction.Collide);
                if (count==overlaps.Length) continue;
                for (int i=0;i<count;i++) if (overlaps[i]!=ground.collider) { blocked=true; break; }
                if (!blocked) return new TransformSnapshot { x=pos.x,y=pos.y,z=pos.z,qw=1 };
            }
            return null;
        }
        void Project()
        {
            if (state==null) return;
            foreach (var n in state.nodes)
            {
                if (views.TryGetValue(n.id,out var view) && view) view.Present(n);
                if (n.reward!=RewardLocation.World || !ResidentOrLoaded(n.cellId) || pickups.ContainsKey(n.pickup.id)) continue;
                var p=n.pickup; var definition=save.Catalog.GetItem(new Inventory.Data.StableItemId(p.definitionId));
                var parent=new GameObject("Harvest pickup staging"); parent.SetActive(false);
                parent.transform.SetParent(transform,false);
                GameObject go=null;
                try
                {
                    go=Instantiate(definition.WorldPrefab,parent.transform);
                    go.transform.SetPositionAndRotation(new Vector3(p.transform.x,p.transform.y,p.transform.z),new Quaternion(p.transform.qx,p.transform.qy,p.transform.qz,p.transform.qw));
                    var item=go.GetComponent<WorldItem>(); item.Configure(definition,p.quantity); item.AssignPersistentIdentity(p.id,WorldItemOrigin.Harvest);
                    item.BindPickupGuard(CanPickup);
                    item.BindPersistence((id,quantity)=>RecordPickup(n.id,id,quantity));
                    pickups.Add(p.id,item); pickupRoots.Add(p.id,parent); go.SetActive(true); parent.SetActive(true);
                }
                catch { parent.SetActive(false); Destroy(parent); throw; }
            }
        }
        bool CanPickup(WorldItem item,PlayerInventory inventory) => Ready && inventory && inventory.gameObject==flow.Player &&
            OwnsPickup(item) && Vector3.Distance(inventory.transform.position,item.transform.position)<=2.5f &&
            CellUsable(NodeForPickup(item.PersistentId).cellId);
        ResourceNodeState NodeForPickup(string id)
        {
            if (state!=null) foreach (var n in state.nodes) if (n.pickup!=null && n.pickup.id==id) return n;
            return null;
        }
        public bool OwnsPickup(WorldItem item)
        {
            if (!item || !pickups.TryGetValue(item.PersistentId,out var owned) || owned!=item) return false;
            var n=NodeForPickup(item.PersistentId);
            return n!=null && n.reward==RewardLocation.World && item.Origin==WorldItemOrigin.Harvest &&
                item.Definition.Id.Value==n.outputId && item.Quantity==n.pickup.quantity;
        }
        bool ResidentOrLoaded(string cell) => cell=="resident" || (cells && cells.State(cell)==CellState.Ready);
        void RecordPickup(string nodeId,string id,int quantity)
        {
            var n=GatheringState.Find(state,nodeId);
            n.pickup.quantity=quantity;
            if (quantity==0)
            {
                n.reward=RewardLocation.Consumed; n.pickup.disposition=EntityDisposition.Consumed;
                if(views.TryGetValue(nodeId,out var view) && view && view.physicsBody) view.Present(n);
            }
        }
    }
}
