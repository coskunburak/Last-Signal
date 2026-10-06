using UnityEngine;

namespace MotionCore.Vehicle.Core
{
    /// <summary>
    /// Grip modifiers for the surface under a wheel. Multipliers are relative to the
    /// wheel's authored friction (1 = unchanged, &lt;1 = more slippery, &gt;1 = grippier).
    /// </summary>
    public struct SurfaceProperties
    {
        public float forwardGripMultiplier;
        public float sidewaysGripMultiplier;

        public static SurfaceProperties Default => new SurfaceProperties
        {
            forwardGripMultiplier = 1f,
            sidewaysGripMultiplier = 1f
        };
    }

    /// <summary>
    /// Extension point for surface-aware grip (e.g. asphalt vs grass vs sand vs ice).
    /// Implement this on a component on — or above — the vehicle; the controller
    /// auto-detects it and queries it each physics step for every grounded wheel,
    /// scaling that wheel's friction by the returned multipliers.
    ///
    /// The free core ships no concrete surface provider; detecting the surface (by
    /// physics material, terrain texture, tags, etc.) is left to the game using it.
    /// </summary>
    public interface IVehicleSurfaceProvider
    {
        SurfaceProperties GetSurface(WheelCollider wheel, in WheelHit hit);
    }
}
