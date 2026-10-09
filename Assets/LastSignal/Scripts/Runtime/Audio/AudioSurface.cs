using UnityEngine;
namespace LastSignal.Audio
{
    [DisallowMultipleComponent] public sealed class AudioSurface : MonoBehaviour
    {
        public AudioSurfaceKind surface;
        public Vector2 areaRadii;
        public float squareCutout;
        public bool ContainsArea(Vector3 world)
        {
            if(areaRadii.x<=0 || areaRadii.y<=0) return false;
            Vector3 p=transform.InverseTransformPoint(world);
            return Mathf.Abs(p.y)<1 && Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.z))>=squareCutout &&
                p.x*p.x/(areaRadii.x*areaRadii.x)+p.z*p.z/(areaRadii.y*areaRadii.y)<=1;
        }
        public static AudioSurfaceKind Resolve(Collider collider)
        {
            var tag=collider ? collider.GetComponentInParent<AudioSurface>() : null;
            return tag ? tag.surface : AudioSurfaceKind.Default;
        }
        public static AudioCue Cue(AudioSurfaceKind kind) => (AudioCue)(int)kind;
    }
}
