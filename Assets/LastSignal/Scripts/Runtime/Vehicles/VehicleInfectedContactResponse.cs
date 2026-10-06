using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace LastSignal.Vehicles
{
    // Immutable ID-only snapshots: Physics.ContactModifyEvent can execute on worker threads.
    // No gameplay commits, Unity object access, allocation, or velocity writes in the callback.
    public static class VehicleInfectedContactResponse
    {
        readonly struct Entry
        {
            public readonly EntityId Id;
            public readonly int Divisor;
            public readonly float Mass;
            public Entry(EntityId id, float mass, int divisor) { Id = id; Mass = mass; Divisor = divisor; }
        }
        static Entry[] vehicles = Array.Empty<Entry>(), infected = Array.Empty<Entry>();
        static bool subscribed;
        static float step = .02f;
        static void Subscribe()
        {
            if (subscribed) return;
            Physics.ContactModifyEvent += Modify;
            Physics.ContactModifyEventCCD += Modify;
            subscribed = true;
        }
        public static void Register(Collider[] colliders, bool vehicle, float effectiveMass = 80)
        {
            Subscribe();
            var list = new List<Entry>(vehicle ? vehicles : infected);
            foreach (var c in colliders)
            {
                if (!c || c.isTrigger || c is WheelCollider) continue;
                EntityId id = c.GetEntityId(); list.RemoveAll(e => e.Id == id);
                list.Add(new Entry(id, effectiveMass, colliders.Length));
                if (vehicle) c.hasModifiableContacts = true;
            }
            if (vehicle) Volatile.Write(ref vehicles, list.ToArray());
            else Volatile.Write(ref infected, list.ToArray());
        }
        public static void Unregister(Collider[] colliders, bool vehicle)
        {
            if (colliders == null) return;
            var list = new List<Entry>(vehicle ? vehicles : infected);
            foreach (var c in colliders)
            {
                if (!c) continue;
                EntityId id = c.GetEntityId(); list.RemoveAll(e => e.Id == id);
                if (vehicle) c.hasModifiableContacts = false;
            }
            if (vehicle) Volatile.Write(ref vehicles, list.ToArray());
            else Volatile.Write(ref infected, list.ToArray());
            if (vehicles.Length == 0 && infected.Length == 0 && subscribed)
            {
                Physics.ContactModifyEvent -= Modify; Physics.ContactModifyEventCCD -= Modify; subscribed = false;
            }
        }
        public static void SetStep(float value) => Volatile.Write(ref step, value);
        static bool Find(Entry[] entries, EntityId id, out Entry found)
        { foreach (var e in entries) if (e.Id == id) { found = e; return true; } found = default; return false; }
        static void Modify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
        {
            var cars = Volatile.Read(ref vehicles); var targets = Volatile.Read(ref infected);
            float dt = Volatile.Read(ref step);
            for (int i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i];
                bool first = Find(cars, pair.colliderEntityId, out var car);
                if (!first && !Find(cars, pair.otherColliderEntityId, out car)) continue;
                if (!Find(targets, first ? pair.otherColliderEntityId : pair.colliderEntityId, out var target)) continue;
                Vector3 velocity = first ? pair.bodyVelocity : pair.otherBodyVelocity;
                for (int n = 0; n < pair.contactCount; n++)
                {
                    var normal = pair.GetNormal(n);
                    // A human-sized body's finite impulse budget replaces the moved-static
                    // capsule's infinite resistance. Only car approach contributes road-speed impulse;
                    // a walking agent against a parked car receives a small bounded push budget.
                    float closing = Mathf.Max(0, Vector3.Dot(velocity, first ? -normal : normal));
                    float impulse = target.Mass * (closing + .25f * dt) /
                        Mathf.Max(1, car.Divisor * target.Divisor * pair.contactCount);
                    pair.SetMaxImpulse(n, Mathf.Max(.001f, impulse));
                    pair.SetStaticFriction(n, 0); pair.SetDynamicFriction(n, 0); pair.SetBounciness(n, 0);
                }
            }
        }
    }
}
