using System;
using UnityEngine;
namespace LastSignal.Persistence
{
    [Serializable] public sealed class ZombieWoundEntry
    {
        public string surface;
        public int triangle;
        public Vector3 barycentric;
        public float radius, severity, seed;
    }
    [Serializable] public sealed class ZombieWoundSnapshot
    {
        public int version;
        public ZombieWoundEntry[] entries;
        public static bool Valid(ZombieWoundSnapshot value)
        {
            if (value == null || value.version == 0 && (value.entries == null || value.entries.Length == 0)) return true;
            if (value.version != 1 || value.entries == null || value.entries.Length > ZombieLocalizedWounds.MaximumWounds) return false;
            foreach (var e in value.entries)
                if (e == null || string.IsNullOrEmpty(e.surface) || e.surface.Length > 40 || ZombieWoundSurfaceResolver.PartFor(e.surface) == ZombieBodyPart.Unspecified ||
                    e.triangle < 0 || e.triangle > 100000 || !ZombieWoundSurfaceResolver.Finite(e.barycentric) ||
                    e.barycentric.x < 0 || e.barycentric.y < 0 || e.barycentric.z < 0 || Mathf.Abs(e.barycentric.x + e.barycentric.y + e.barycentric.z - 1) > .001f ||
                    !float.IsFinite(e.radius) || e.radius < .005f || e.radius > .2f || !float.IsFinite(e.severity) || e.severity < 0 || e.severity > 1 ||
                    !float.IsFinite(e.seed) || Mathf.Abs(e.seed) > 10000) return false;
            return true;
        }
    }
}
