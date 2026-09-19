using Unity.Profiling;
using UnityEngine;

namespace LastSignal
{
    public readonly struct ZombieObservation
    {
        public readonly float Evidence;
        public readonly Vector3 Position, Direction;
        public bool Visible => Evidence > 0;
        public ZombieObservation(float evidence, Vector3 position, Vector3 direction)
        { Evidence = evidence; Position = position; Direction = direction; }
    }
    public sealed class ZombiePerception : MonoBehaviour
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Zombie.Perception");
        [SerializeField] Transform origin;
        Transform target, head;
        CharacterController capsule;
        public int Raycasts { get; private set; }
        public Vector3 HeadSample { get; private set; }
        public Vector3 ChestSample { get; private set; }
        public bool HeadVisible { get; private set; }
        public bool ChestVisible { get; private set; }
        public Vector3 Origin => origin ? origin.position : transform.position + Vector3.up * 1.5f;
        public void Configure(Transform eye) => origin = eye;
        public bool Bind(GameObject player)
        {
            Clear(); if (!player) return false;
            target = player.transform; capsule = player.GetComponent<CharacterController>();
            var look = player.GetComponent<FirstPersonLook>();
            head = look && look.View ? look.View.transform : null;
            return capsule && head;
        }
        public void Clear() { target = head = null; capsule = null; HeadVisible = ChestVisible = false; }
        public ZombieObservation Evaluate(ZombieDefinition tuning)
        {
            using (Marker.Auto())
            {
                HeadVisible = ChestVisible = false;
                if (!target || !target.gameObject.activeInHierarchy || !capsule || !head) return default;
                HeadSample = head.position;
                ChestSample = target.TransformPoint(capsule.center + Vector3.up * capsule.height * .15f);
                HeadVisible = VisiblePoint(HeadSample, tuning);
                ChestVisible = VisiblePoint(ChestSample, tuning);
                float evidence = (HeadVisible ? .5f : 0) + (ChestVisible ? .5f : 0);
                // Only positive observations release exact target knowledge to the decision layer.
                return evidence > 0 ? new ZombieObservation(evidence, target.position, target.forward) : default;
            }
        }
        bool VisiblePoint(Vector3 point, ZombieDefinition tuning)
        {
            Vector3 offset = point - Origin;
            if (!InFieldOfView(transform.forward, offset, tuning.SightDistance, tuning.HorizontalFov, tuning.VerticalHalfAngle)) return false;
            Raycasts++;
            return !Physics.Raycast(Origin, offset.normalized, offset.magnitude, tuning.OcclusionMask, QueryTriggerInteraction.Ignore);
        }
        public static bool InFieldOfView(Vector3 forward, Vector3 offset, float range, float horizontalFov, float verticalHalfAngle)
        {
            if (offset.sqrMagnitude > range * range) return false;
            Vector3 planar = new Vector3(offset.x, 0, offset.z);
            if (Mathf.Abs(offset.y) > planar.magnitude * Mathf.Tan(verticalHalfAngle * Mathf.Deg2Rad)) return false;
            forward.y = 0;
            return planar.sqrMagnitude > .0001f && Vector3.Dot(forward.normalized, planar.normalized) >= Mathf.Cos(horizontalFov * .5f * Mathf.Deg2Rad);
        }
#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            var controller = GetComponent<ZombieController>(); if (!controller || !controller.Definition) return;
            var tuning = controller.Definition; var eye = Origin;
            Gizmos.color = Color.yellow; Gizmos.DrawLine(eye, eye + transform.forward * tuning.SightDistance);
            foreach (float angle in new[] { -tuning.HorizontalFov * .5f, tuning.HorizontalFov * .5f })
                Gizmos.DrawLine(eye, eye + Quaternion.Euler(0, angle, 0) * transform.forward * tuning.SightDistance);
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireArc(eye, Vector3.up, Quaternion.Euler(0,-tuning.HorizontalFov*.5f,0)*transform.forward,tuning.HorizontalFov,tuning.SightDistance);
            if (!Application.isPlaying || !target) return;
            Gizmos.color = HeadVisible ? Color.green : Color.red; Gizmos.DrawLine(eye, HeadSample);
            Gizmos.color = ChestVisible ? Color.green : Color.red; Gizmos.DrawLine(eye, ChestSample);
        }
#endif
    }
}
