using LastSignal.Inventory.Data;
using LastSignal.Persistence;
using UnityEngine;

namespace LastSignal.Vehicles
{
    public sealed partial class VehicleActor
    {
        public SaveResult Capture(out VehicleWorldSnapshot snapshot)
        {
            snapshot = null;
            if (!ownership.Stable || Resources == null || Resources.Busy) return new SaveResult(SaveError.Busy, "Vehicle transition in progress.");
            var world = flow.GetComponent<VehicleWorld>();
            if (!world || !world.ContainsResidentPosition(transform.position)) return new SaveResult(SaveError.InvalidData, "Pickup is outside its resident region.");
            var result = VehicleResourceSnapshots.Capture(Resources, definition.StableId, out var resources);
            if (!result.Success) return result;
            snapshot = new VehicleWorldSnapshot { version = 1, occupiedVehicleId = Occupied ? Resources.Id : null,
                vehicles = new[] { new VehicleSnapshot { resources = resources, pose = SaveSession.Pose(transform), lights = LightsOn, ownerCell = "resident" } } };
            return SaveResult.Ok;
        }
        public SaveResult Restore(VehicleWorldSnapshot snapshot, ItemCatalog catalog)
        {
            if (snapshot == null) return SaveResult.Ok; // Legacy save: authored parked baseline, engine off.
            if (!flow.Restoring || Occupied || snapshot.vehicles == null || snapshot.vehicles.Length != 1)
                return new SaveResult(SaveError.InvalidData, "Vehicle restore is not ready.");
            var saved = snapshot.vehicles[0];
            var result = VehicleResourceSnapshots.Restore(saved.resources, definition.StableId, definition.StableId,
                tuning, definition.FuelItem, catalog, out var restored);
            if (!result.Success) return result;
            var p = saved.pose;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.position = new Vector3(p.x, p.y, p.z); body.rotation = new Quaternion(p.qx, p.qy, p.qz, p.qw);
            transform.SetPositionAndRotation(body.position, body.rotation); Physics.SyncTransforms();
            // Ground and body collision are validated before exposing occupancy or enabling input.
            bool ground = false;
            foreach (var hit in Physics.RaycastAll(transform.position + Vector3.up * 3, Vector3.down, 6, solidMask, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(transform) && hit.normal.y >= .7f) { ground = true; break; }
            if (!ground) return new SaveResult(SaveError.InvalidData, "Vehicle restore has no supporting ground.");
            foreach (var box in GetComponents<BoxCollider>())
                foreach (var other in Physics.OverlapBox(transform.TransformPoint(box.center), box.size * .49f, transform.rotation, solidMask, QueryTriggerInteraction.Ignore))
                    if (!other.transform.IsChildOf(transform) && !other.transform.IsChildOf(flow.Player.transform))
                        return new SaveResult(SaveError.InvalidData, "Vehicle restore overlaps world geometry.");
            Resources = restored; LightsOn = saved.lights; Suspend(); ClearImpactContacts();
            return SaveResult.Ok;
        }
        public bool RestoreOccupancy(VehicleWorldSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.occupiedVehicleId)) return true;
            var look = flow.Player.GetComponent<FirstPersonLook>();
            float pitch = look.Pitch; Quaternion facing = flow.Player.transform.rotation;
            if (snapshot.occupiedVehicleId != Resources.Id || !AttachDriver(true)) return false;
            flow.Player.transform.rotation = facing; look.RestorePitch(pitch); return true;
        }
    }
}
