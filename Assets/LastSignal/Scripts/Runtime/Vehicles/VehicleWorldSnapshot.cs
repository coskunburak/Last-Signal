using System;
using LastSignal.Persistence;

namespace LastSignal.Vehicles
{
    [Serializable] public sealed class VehicleWorldSnapshot
    {
        public int version;
        public string occupiedVehicleId;
        public VehicleSnapshot[] vehicles;
    }
    [Serializable] public sealed class VehicleSnapshot
    {
        public VehicleResourceSnapshot resources;
        public TransformSnapshot pose;
        public string ownerCell;
        public bool lights;
    }
}
