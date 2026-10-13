using UnityEngine;

namespace LastSignal
{
    public enum ImpactSurfaceKind { Default, Flesh, Bone, Metal, Stone, Wood, Soil }

    /// <summary>Optional explicit override; existing AudioSurface tags provide the fallback.</summary>
    [DisallowMultipleComponent]
    public sealed class ImpactSurface : MonoBehaviour
    {
        public ImpactSurfaceKind kind;
        public static ImpactSurfaceKind Resolve(Collider collider)
        {
            if (!collider) return ImpactSurfaceKind.Default;
            var region = collider.GetComponent<ZombieHitRegion>();
            if (region && region.Owner) return region.Region == DamageRegion.Head ? ImpactSurfaceKind.Bone : ImpactSurfaceKind.Flesh;
            var explicitSurface = collider.GetComponentInParent<ImpactSurface>();
            if (explicitSurface) return explicitSurface.kind;
            if (collider is TerrainCollider) return ImpactSurfaceKind.Soil;
            switch (Audio.AudioSurface.Resolve(collider))
            {
                case Audio.AudioSurfaceKind.Metal: return ImpactSurfaceKind.Metal;
                case Audio.AudioSurfaceKind.Stone: return ImpactSurfaceKind.Stone;
                case Audio.AudioSurfaceKind.Wood: return ImpactSurfaceKind.Wood;
                case Audio.AudioSurfaceKind.Dirt:
                case Audio.AudioSurfaceKind.Grass:
                case Audio.AudioSurfaceKind.Gravel: return ImpactSurfaceKind.Soil;
                default: return ImpactSurfaceKind.Default;
            }
        }
        public static bool Organic(ImpactSurfaceKind kind) => kind == ImpactSurfaceKind.Flesh || kind == ImpactSurfaceKind.Bone;
    }
}
