using Unity.Profiling;
using UnityEngine;

namespace LastSignal.Vehicles
{
    public sealed partial class VehicleActor
    {
        static readonly ProfilerMarker ImpactMarker = new ProfilerMarker("LastSignal.Vehicle.ZombieImpact");
        struct PhysicalContact
        {
            public Collider Other, Own;
            public ZombieHealth Health;
            public bool Infected;
            public ulong Identity;
        }
        // Allocated once. No callback arrays, dictionaries, delegates or per-hit subscriptions.
        readonly PhysicalContact[] impactContacts = new PhysicalContact[128];
        Collider[] responseColliders;
        void OnEnable()
        {
            responseColliders = GetComponents<Collider>();
            feelWheels = GetComponentsInChildren<WheelCollider>(true);
            VehicleInfectedContactResponse.Register(responseColliders, true);
        }
        Vector3 stepVelocity, stepAngularVelocity, stepCenter;
        public VehicleZombieImpactReceipt LastZombieImpact { get; private set; }
        public int ZombieImpactCount { get; private set; }
        public int ImpactSuppressionCount { get; private set; }
        public int ImpactContactCount { get; private set; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public string DescribeImpactContacts()
        {
            var details = new System.Text.StringBuilder();
            foreach (var c in impactContacts)
                if (c.Identity != 0)
                    details.Append($"[other={c.Other}; own={c.Own}; infected={c.Infected}; alive={(c.Health && c.Health.IsAlive)}; otherBounds={(c.Other ? c.Other.bounds.ToString() : "missing")}; carPosition={transform.position}] ");
            return details.ToString();
        }
#endif
        public event System.Action<VehicleZombieImpactReceipt> ZombieImpactCommitted;

        public float LastObservedClosingSpeed { get; private set; }
        void FixedUpdate()
        {
            VehicleInfectedContactResponse.SetStep(Time.fixedDeltaTime);
            AdvanceBodyFeel(Time.fixedDeltaTime);
            if (body)
            {
                stepVelocity = body.linearVelocity; stepAngularVelocity = body.angularVelocity;
                stepCenter = body.worldCenterOfMass;
            }
            for (int i = 0; i < impactContacts.Length; i++)
            {
                ref var c = ref impactContacts[i];
                if (c.Identity == 0) continue;
                // Unity need not send Exit when a collider is disabled/destroyed on death/despawn.
                if (!c.Other || !c.Own || !c.Other.enabled || !c.Own.enabled ||
                    !c.Other.gameObject.activeInHierarchy || c.Infected && (!c.Health || !c.Health.IsAlive))
                    ReleaseImpactContact(i);
            }
        }
        void ReleaseImpactContact(int i)
        { impacts.End(impactContacts[i].Identity); impactContacts[i] = default; ImpactContactCount--; }
        void ClearImpactContacts()
        { System.Array.Clear(impactContacts, 0, impactContacts.Length); impacts.Reset(); ImpactContactCount = 0; }
        static Vector3 ImpactNoisePosition(in ContactPoint contact)
        {
            // Solver contact points can penetrate the emitting chassis. Project onto its
            // outward surface so the source itself cannot occlude an otherwise clear ray.
            // Rays crossing the chassis still meet the ordinary hearing occlusion mask.
            Vector3 outward = -contact.normal.normalized;
            var collider = contact.thisCollider;
            float reach = collider.bounds.extents.magnitude + .05f;
            var ray = new Ray(contact.point + outward * reach, -outward);
            return collider.Raycast(ray, out var surface, reach * 2)
                ? surface.point + outward * .02f : contact.point;
        }
        void OnCollisionEnter(Collision collision)
        {
            if (!isActiveAndEnabled) return;
            using (ImpactMarker.Auto())
            {
                // Even unavailable/initial overlap enters the ledger, so resume cannot turn Stay into damage.
                for (int n = 0; n < collision.contactCount; n++)
                {
                    var contact = collision.GetContact(n);
                    var own = contact.thisCollider; var other = contact.otherCollider;
                    if (!own || !other || own.isTrigger || other.isTrigger || own is WheelCollider ||
                        own.attachedRigidbody != body || own.gameObject != gameObject) continue;
                    bool exists = false; int free = -1;
                    for (int i = 0; i < impactContacts.Length; i++)
                    {
                        ref var c = ref impactContacts[i];
                        if (c.Identity == 0) { if (free < 0) free = i; }
                        else if (c.Other == other) { exists = true; break; }
                    }
                    if (exists) continue;
                    if (free < 0) { ImpactSuppressionCount++; continue; }
                    var target = other.GetComponentInParent<ZombieHealth>();
                    ulong identity = target ? EntityId.ToULong(target.GetEntityId()) : other.attachedRigidbody ?
                        EntityId.ToULong(other.attachedRigidbody.GetEntityId()) : EntityId.ToULong(other.GetEntityId());
                    impactContacts[free] = new PhysicalContact { Own = own, Other = other, Health = target,
                        Infected = target, Identity = identity };
                    ImpactContactCount++;
                    bool accepted = impacts.Begin(identity, Time.timeAsDouble);
                    if (!accepted) { ImpactSuppressionCount++; continue; }
                    if (!Ready || Resources == null || Resources.Busy || !body || body.isKinematic || !body.detectCollisions) continue;
                    if (!target)
                    {
                        if (Resources.ApplyImpact(collision.relativeVelocity.magnitude) && impactSequence < ulong.MaxValue &&
                            noise.Impact(contact.point, ++impactSequence)) Presented?.Invoke("impact");
                        continue;
                    }
                    if (!target.IsAlive || !target.isActiveAndEnabled || impactSequence == ulong.MaxValue) continue;
                    // Pre-solver chassis point motion avoids post-solver deceleration and ambiguous
                    // Collision.relativeVelocity receiver signs. NavMesh owns living infected motion.
                    var navigation = target.GetComponent<ZombieNavigation>();
                    var targetBody = other.attachedRigidbody;
                    bool navigated = navigation && navigation.Ready;
                    Vector3 targetMotion = navigated ? navigation.Velocity : targetBody ? targetBody.GetPointVelocity(contact.point) : Vector3.zero;
                    Vector3 motion = VehicleZombieImpactTuning.RelativePointMotion(stepVelocity, stepAngularVelocity,
                        stepCenter, contact.point, targetMotion);
                    float speed = VehicleZombieImpactTuning.ClosingSpeed(motion, contact.normal);
                    // Choose the strongest body contact, not the engine's arbitrary first manifold point.
                    for (int k = n + 1; k < collision.contactCount; k++)
                    {
                        var candidate = collision.GetContact(k);
                        if (candidate.otherCollider != other || candidate.thisCollider.attachedRigidbody != body || candidate.thisCollider.gameObject != gameObject) continue;
                        Vector3 candidateTargetMotion = navigated ? navigation.Velocity : targetBody ? targetBody.GetPointVelocity(candidate.point) : Vector3.zero;
                        Vector3 candidateMotion = VehicleZombieImpactTuning.RelativePointMotion(stepVelocity, stepAngularVelocity,
                            stepCenter, candidate.point, candidateTargetMotion);
                        float candidateSpeed = VehicleZombieImpactTuning.ClosingSpeed(candidateMotion, candidate.normal);
                        if (candidateSpeed > speed) { speed = candidateSpeed; contact = candidate; motion = candidateMotion; }
                    }
                    LastObservedClosingSpeed = speed;
                    float severity = tuning.zombieImpact.Severity(speed);
                    if (severity <= 0) continue;
                    Vector3 direction = Vector3.ProjectOnPlane(motion, Vector3.up).normalized;
                    float damage = tuning.zombieImpact.Damage(severity);
                    int before = target.DamageTransactions;
                    ulong transaction = ++impactSequence;
                    target.TakeDamage(new DamageInfo { Amount = damage, BaseAmount = damage, Multiplier = 1,
                        ShotId = transaction, Category = DamageCategory.VehicleImpact, BodyPart = ZombieBodyPart.Torso,
                        Region = DamageRegion.Body, SourcePosition = transform.position, HitPoint = contact.point,
                        HitNormal = contact.normal, Direction = direction, Instigator = driver ? driver : gameObject });
                    if (target.DamageTransactions == before) continue;
                    double condition = Resources.Condition;
                    Resources.ApplyInfectedImpact(severity);
                    noise.Impact(ImpactNoisePosition(contact), transaction, severity);
                    LastZombieImpact = new VehicleZombieImpactReceipt(transaction, identity, contact.point,
                        direction, speed, severity, damage, condition - Resources.Condition);
                    ZombieImpactCount++;
                    BeginBodyFeel(target, LastZombieImpact);
                    ZombieImpactCommitted?.Invoke(LastZombieImpact);
                }
            }
        }
        void OnCollisionExit(Collision collision)
        {
            for (int i = 0; i < impactContacts.Length; i++)
                if (impactContacts[i].Identity != 0 && impactContacts[i].Other == collision.collider) ReleaseImpactContact(i);
        }
    }
}
