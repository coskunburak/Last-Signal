using UnityEngine;
using LastSignal.Noise;

namespace LastSignal.Gathering
{
    public enum TreeMotionState { Standing, BeingChopped, StructurallyWeakened, Fracturing, JointBroken, Falling, GroundImpact, Settling, Resting, Depleted }
    /// <summary>Physical projection under the stable node anchor. Never awards items or rotates a falling transform.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    public sealed class TreePhysicsBody : MonoBehaviour
    {
        public ResourceNodeView node;
        public TreePhysicsProfile profile;
        public MeshFilter topMesh, stumpMesh;
        public Collider trunkCollider, stumpCollider;
        public TreeAudioBank audioBank;
        public ParticleSystem chips, dust;
        Rigidbody body;
        Renderer[] bodyRenderers;
        FixedJoint connection;
        BoxCollider stumpBase;
        TreeCutSnapshot state;
        ResourceNodeState projection;
        bool suspended;
        long seenRevision=-1, lastImpulseRevision=-1;
        float stableFor, activeFallSeconds, nextBranchContact;
        int branchContacts;
        bool creakPlayed, fallCuePlayed;
        public event System.Action<TreeAudioEvent,Vector3,float> FeedbackRequested;
        public int SignificantImpactCount { get; private set; }
        bool silentRestore, initialized, failurePending, touched;
        Vector3 pendingVelocity, pendingAngular;
        Vector3 initialPosition; Quaternion initialRotation;
        Audio.ProductionAudio audioDirector;
        SessionFlow session;
        ulong noiseSource;
        public TreeMotionState Motion { get; private set; }
        public Rigidbody Body => body ? body : GetComponent<Rigidbody>();
        public FixedJoint Connection => connection;
        public float LastCollisionImpulse { get; private set; }
        public float CutDepth => state?.depth??0;
        public float RemainingSupport => TreeStructure.RemainingArea(CutDepth);
        public bool ActiveSimulation => body && !body.isKinematic && !body.IsSleeping();
        public bool Fractured => state!=null && state.fractured;
        public bool CanCollect => state!=null && state.fractured && state.resting;
        public bool Valid => Body && node && trunkCollider && stumpCollider && trunkCollider is CapsuleCollider && !trunkCollider.isTrigger && !stumpCollider.isTrigger && profile && profile.Valid && topMesh && stumpMesh &&
            topMesh.sharedMesh && stumpMesh.sharedMesh && topMesh.GetComponent<Renderer>() && stumpMesh.GetComponent<Renderer>() &&
            trunkCollider.attachedRigidbody==Body && !stumpCollider.attachedRigidbody && transform.parent==node.transform &&
            transform.lossyScale==Vector3.one && node.transform.lossyScale==Vector3.one;
        void Initialize()
        {
            if(initialized) return;
            body=GetComponent<Rigidbody>(); bodyRenderers=GetComponentsInChildren<Renderer>(true); initialPosition=transform.localPosition; initialRotation=transform.localRotation;
            if(stumpCollider is BoxCollider)
            {
                stumpBase=stumpCollider.gameObject.AddComponent<BoxCollider>();
                stumpBase.enabled=false; stumpBase.sharedMaterial=stumpCollider.sharedMaterial;
            }
            initialized=true;
            if(node && node.world)
            { session=node.world.GetComponent<SessionFlow>(); audioDirector=node.world.GetComponent<Audio.ProductionAudio>(); }
            noiseSource=EntityId.ToULong(GetEntityId());
        }
        public bool Accepts(Collider hit,Vector3 point)
        {
            if(!Valid || hit!=trunkCollider || failurePending || (state!=null&&state.fractured)) return false;
            var p=node.transform.InverseTransformPoint(point);
            return TreeStructure.Finite(p) && Mathf.Abs(p.y-profile.cutHeight)<=.2f &&
                new Vector2(p.x,p.z).magnitude<=profile.radius*1.5f;
        }
        public void ResetProjection()
        {
            Initialize();
            if(!body || !node) { FailProjection(); return; }
            body.isKinematic=true; body.detectCollisions=false;
            if(connection) { Destroy(connection); connection=null; }
            // Lifecycle restore only. No falling motion ever uses transform updates.
            body.position=node.transform.TransformPoint(initialPosition); body.rotation=node.transform.rotation*initialRotation;
            seenRevision=-1; lastImpulseRevision=-1; state=null; projection=null; failurePending=false; touched=false; stableFor=0; activeFallSeconds=0;
            silentRestore=true; creakPlayed=false; fallCuePlayed=false; branchContacts=0; SignificantImpactCount=0; Motion=TreeMotionState.Standing;
        }
        public void Present(ResourceNodeState n)
        {
            Initialize();
            if(n?.tree==null) { ResetProjection(); return; }
            if(!Valid || !profile.Supports(n.tree)) { FailProjection(); return; }
            bool first=seenRevision<0 || suspended;
            state=n.tree; projection=n; suspended=false;
            int index=profile.Index(state);
            topMesh.sharedMesh=state.depth>0?profile.cutTops[index]:profile.intactTop;
            stumpMesh.sharedMesh=state.depth>0?profile.cutStumps[index]:profile.intactStump;
            ConfigureStumpCollision(state.depth,state.side);
            body.mass=profile.mass; body.centerOfMass=profile.centerOfMass;
            body.linearDamping=.05f; body.angularDamping=.1f; body.useGravity=true;
            body.interpolation=RigidbodyInterpolation.Interpolate; body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            body.solverIterations=12; body.solverVelocityIterations=6; body.maxAngularVelocity=8; body.sleepThreshold=.005f;
            if(first)
            {
                lastImpulseRevision=n.revision;
                body.ResetInertiaTensor();
                if(state.hasPose)
                { body.position=state.position; body.rotation=state.rotation; }
                silentRestore=true;
            }
            body.detectCollisions=true;
            Physics.IgnoreCollision(trunkCollider,stumpCollider,!state.fractured); // Suppress attachment contacts only; released wood can hit its stump.
            if(stumpBase && stumpBase.enabled) Physics.IgnoreCollision(trunkCollider,stumpBase,!state.fractured);
            if(state.fractured)
            {
                if(connection) { Destroy(connection); connection=null; }
                body.isKinematic=state.resting;
                if(first && !state.resting) { body.linearVelocity=state.velocity; body.angularVelocity=state.angularVelocity; }
                Motion=state.resting?TreeMotionState.Resting:TreeMotionState.Falling;
            }
            else
            {
                if(!connection)
                {
                    connection=gameObject.AddComponent<FixedJoint>();
                    connection.autoConfigureConnectedAnchor=false;
                    connection.enableCollision=false; connection.enablePreprocessing=true;
                }
                var support=Vector3.up*profile.cutHeight-TreeStructure.Direction(state.side)*TreeStructure.SupportOffset(profile.radius,state.depth);
                var supportWorld=node.transform.TransformPoint(support);
                connection.anchor=transform.InverseTransformPoint(supportWorld);
                connection.connectedAnchor=supportWorld;
                connection.breakForce=state.depth==0?float.PositiveInfinity:
                    profile.axialStrength*Mathf.PI*profile.radius*profile.radius*TreeStructure.RemainingArea(state.depth);
                connection.breakTorque=state.depth==0?float.PositiveInfinity:TreeStructure.MomentCapacity(profile.radius,profile.bendingStrength,state.depth);
                // A loaded notch must still carry gravity through its weakened joint. In particular,
                // a fully cut checkpoint cannot wait for another accepted hit to resume physics.
                body.isKinematic=n.revision==0;
                if(first && !body.isKinematic) { body.linearVelocity=state.velocity; body.angularVelocity=state.angularVelocity; }
                Motion=n.revision==0?TreeMotionState.Standing:TreeMotionState.StructurallyWeakened;
            }
            bool depleted=n.reward==RewardLocation.Consumed;
            // Resource consumption is not destruction of the fallen tree. Keep the accepted pose,
            // bark, crown, stump and collision visible even when its one yield is fully collected.
            foreach(var renderer in bodyRenderers) if(renderer) renderer.enabled=true;
            if(depleted) { body.isKinematic=true; Motion=TreeMotionState.Depleted; }
            seenRevision=n.revision;
        }
        public void AcceptedHit(Vector3 point,Vector3 direction)
        {
            if(state==null||state.fractured||failurePending||suspended||!isActiveAndEnabled||seenRevision<=0||seenRevision==lastImpulseRevision) return;
            if(!body || !trunkCollider || !stumpCollider) { FailProjection(); return; }
            lastImpulseRevision=seenRevision;
            silentRestore=false; Motion=TreeMotionState.BeingChopped;
            body.isKinematic=false; body.WakeUp();
            body.AddForceAtPosition(direction.normalized*profile.axeImpulse,point,ForceMode.Impulse);
            if(chips) { chips.transform.position=point; chips.Emit(6); }
            Feedback(TreeAudioEvent.WoodHit,point,.7f);
            Feedback(state.depth>.5f?TreeAudioEvent.StructuralCrack:TreeAudioEvent.BarkFracture,point,.35f);
        }
        void OnJointBreak(float force)
        {
            if(state==null||state.fractured||failurePending||suspended||!isActiveAndEnabled||!node||!node.isActiveAndEnabled) return;
            if(!body || !trunkCollider || !stumpCollider) { FailProjection(); return; }
            if(session && (session.PlayerDead || session.Restoring || session.InMenu)) { Suspend(); return; }
            failurePending=true; Motion=TreeMotionState.Fracturing;
            // The solver has failed a fully constrained joint. Freeze only for the synchronous durable receipt.
            var velocity=body.linearVelocity; var angular=body.angularVelocity;
            pendingVelocity=velocity; pendingAngular=angular;
            Sample(state); body.isKinematic=true;
            if(!node.world || !node.world.CommitTreeFailure(this,seenRevision)) return; // Fail closed; world requests recovery.
            connection=null; failurePending=false; Motion=TreeMotionState.JointBroken;
            body.isKinematic=false; body.linearVelocity=velocity; body.angularVelocity=angular;
            if(!silentRestore) Feedback(TreeAudioEvent.JointFailure,node.transform.TransformPoint(Vector3.up*profile.cutHeight),1);
            if(!silentRestore && chips) { chips.transform.position=node.transform.TransformPoint(Vector3.up*profile.cutHeight); chips.Emit(8); }
        }
        void FixedUpdate()
        {
            if(state==null || failurePending || suspended) return;
            if(!body || !trunkCollider || !stumpCollider || !topMesh || !stumpMesh || !topMesh.sharedMesh || !stumpMesh.sharedMesh ||
                (!state.fractured && seenRevision>0 && !connection))
            { FailProjection(); return; }
            if(body.isKinematic) return;
            if(!state.fractured)
            {
                if(!silentRestore && !creakPlayed && body.angularVelocity.sqrMagnitude>.0025f)
                { creakPlayed=true; Feedback(TreeAudioEvent.Creak,body.worldCenterOfMass,.4f); }
                return;
            }
            if(!silentRestore && !fallCuePlayed && body.angularVelocity.sqrMagnitude>.04f)
            { fallCuePlayed=true; Feedback(TreeAudioEvent.FallingMotion,body.worldCenterOfMass,.6f); }
            activeFallSeconds+=Time.fixedDeltaTime;
            if(activeFallSeconds>90 || !TreeStructure.Finite(body.position) || !TreeStructure.Finite(body.linearVelocity) || !TreeStructure.Finite(body.angularVelocity) ||
                body.linearVelocity.sqrMagnitude>10000 || body.angularVelocity.sqrMagnitude>10000)
            {
                failurePending=true;
                if(node.world) node.world.FailTreePhysics();
                body.isKinematic=true; // Exceptional recovery, never reported as resting or collectible.
                Debug.LogError("S004_PHYSICS_RECOVERY_REQUIRED: invalid/runaway/unsettled body; restore a verified save.",this);
                return;
            }
            bool quiet=touched && body.linearVelocity.sqrMagnitude<profile.settleSpeed*profile.settleSpeed &&
                body.angularVelocity.sqrMagnitude<profile.settleAngularSpeed*profile.settleAngularSpeed;
            stableFor=quiet?stableFor+Time.fixedDeltaTime:0;
            Motion=quiet?TreeMotionState.Settling:TreeMotionState.Falling;
            if(stableFor>=profile.settleSeconds)
            {
                body.Sleep(); state.resting=true; body.isKinematic=true; Sample(state); Motion=TreeMotionState.Resting;
                if(!silentRestore) Feedback(TreeAudioEvent.Settling,body.worldCenterOfMass,.4f);
            }
        }
        void OnCollisionEnter(Collision c) => RecordCollision(c);
        void OnCollisionStay(Collision c) => RecordCollision(c);
        void RecordCollision(Collision c)
        {
            if(state==null||!state.fractured||state.resting||failurePending||suspended||!isActiveAndEnabled||
                projection==null||projection.reward==RewardLocation.Consumed||!body||!profile||c.contactCount==0) return;
            touched=true; LastCollisionImpulse=c.impulse.magnitude;
            // The released butt can strike its own stump before tipping. That is not the
            // world landing; reserve the one impact cue/noise receipt for external contact.
            if(c.collider==stumpCollider || c.collider==stumpBase) return;
            if(LastCollisionImpulse<profile.mass*.15f) return;
            if(state.impactRecorded)
            {
                if(!silentRestore && branchContacts<2 && Time.time>=nextBranchContact && LastCollisionImpulse>profile.mass*.3f)
                { branchContacts++; nextBranchContact=Time.time+.5f; Feedback(TreeAudioEvent.BranchContact,c.GetContact(0).point,.35f); }
                return;
            }
            SignificantImpactCount++;
            state.impactRecorded=true; Motion=TreeMotionState.GroundImpact;
            var point=c.GetContact(0).point;
            if(silentRestore) return;
            Feedback(TreeAudioEvent.GroundImpact,point,Mathf.Clamp01(LastCollisionImpulse/(profile.mass*3)));
            if(dust) { dust.transform.position=point; dust.Emit(12); }
            // One event per fracture, never tied to voice playback or volume. Restore is silent.
            session?.Noise?.TryEmit(new GameplayNoiseRequest(noiseSource,point,GameplayNoiseCategory.TreeImpact,
                session.NoiseTuning.TreeImpact,1),out _);
        }
        void ConfigureStumpCollision(float depth,int side)
        {
            if(!(stumpCollider is BoxCollider support) || !stumpBase) return;
            if(depth<=0)
            {
                stumpBase.enabled=false;
                support.center=Vector3.up*(profile.cutHeight*.5f);
                support.size=new Vector3(profile.radius*1.4f,profile.cutHeight,profile.radius*1.4f);
                return;
            }
            // The authored wedge reaches cutHeight-radius*depth at its open edge.
            // Keep the solid lower stump, but narrow upper contact to wood that remains
            // behind the notch. A full-height box would hold a severed large trunk upright.
            float baseHeight=Mathf.Max(.05f,profile.cutHeight-profile.radius*depth);
            stumpBase.center=Vector3.up*(baseHeight*.5f);
            stumpBase.size=new Vector3(profile.radius*1.4f,baseHeight,profile.radius*1.4f);
            stumpBase.enabled=true;
            float width=Mathf.Max(.01f,profile.radius*(1-depth)*1.41421356f);
            support.center=-TreeStructure.Direction(side)*(profile.radius*depth)+Vector3.up*((profile.cutHeight+baseHeight)*.5f);
            support.size=new Vector3(width,Mathf.Max(.01f,profile.cutHeight-baseHeight),width);
        }
        public void Sample(TreeCutSnapshot target)
        {
            if(!initialized || !body || target==null || suspended) return;
            target.hasPose=true; target.position=body.position; target.rotation=body.rotation;
            target.velocity=failurePending?pendingVelocity:body.isKinematic?Vector3.zero:body.linearVelocity;
            target.angularVelocity=failurePending?pendingAngular:body.isKinematic?Vector3.zero:body.angularVelocity;
            if(state!=null) { target.resting=state.resting; target.impactRecorded=state.impactRecorded; }
        }
        void FailProjection()
        {
            failurePending=true;
            if(body) body.isKinematic=true;
            if(node && node.world) node.world.FailTreePhysics();
        }
        void Feedback(TreeAudioEvent kind,Vector3 point,float gain)
        {
            try { FeedbackRequested?.Invoke(kind,point,gain); } catch(System.Exception e) { Debug.LogException(e,this); } // Observational only, never authorizes resources/noise.
            var clip=audioBank?audioBank.Clip(kind):null;
            if(audioBank && audioBank.CanPlay(gameObject.scene.path) && clip && audioDirector) audioDirector.PlayTreeClip(clip,node.transform,point,gain);
        }
        public void Suspend()
        {
            if(!initialized || suspended) return;
            Sample(state); suspended=true;
            if(body) body.isKinematic=true;
            if(audioDirector&&node) audioDirector.StopOwner(node.transform);
        }
        public void Resume(bool suppressReplay=true)
        {
            if(!initialized || !suspended || projection==null || !isActiveAndEnabled) return;
            bool wasSilent=silentRestore; seenRevision=-1; Present(projection);
            if(!suppressReplay) silentRestore=wasSilent;
        }
        void OnDisable() => Suspend();
        void OnEnable()
        {
            if(!initialized || !suspended || projection==null) return;
            Resume(); // Resume saved physical state, with presentation/noise replay suppressed.
        }
        void OnDrawGizmosSelected()
        {
            if(!profile) return;
            Gizmos.color=Color.yellow; Gizmos.DrawWireSphere(transform.TransformPoint(profile.centerOfMass),.15f);
        }
    }
}
