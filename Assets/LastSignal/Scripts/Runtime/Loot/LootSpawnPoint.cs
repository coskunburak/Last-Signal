using System;
using UnityEngine;

namespace LastSignal.Loot
{
    [DisallowMultipleComponent]
    public sealed class LootSpawnPoint : MonoBehaviour
    {
        [SerializeField] string stableId;
        [SerializeField] LootProfile profile;
        [Tooltip("Conservative half size around the marker center. Prefab collider must fit.")]
        [SerializeField] Vector3 clearance = new Vector3(.12f, .1f, .12f);
        public string StableId => stableId;
        public LootProfile Profile => profile;
        public Vector3 Clearance => clearance;
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(stableId)) { error = "Missing stable point ID."; return false; }
            if (!profile) { error = "Missing loot profile."; return false; }
            if (!Finite(transform.position) || !Finite(clearance) || clearance.x <= 0 || clearance.y <= 0 || clearance.z <= 0 ||
                Vector3.Distance(transform.lossyScale, Vector3.one) > .001f || Vector3.Dot(transform.up, Vector3.up) < .999f)
            { error = "Point must be finite, upright, unit scale with positive clearance."; return false; }
            return profile.Validate(out error);
        }
        static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
#if UNITY_EDITOR
        void Reset() => stableId = Guid.NewGuid().ToString("N");
        [ContextMenu("Assign New Stable ID (breaks seed identity)")]
        void NewId() { UnityEditor.Undo.RecordObject(this, "New loot point ID"); stableId = Guid.NewGuid().ToString("N"); UnityEditor.EditorUtility.SetDirty(this); }
        void OnDrawGizmos()
        {
            Gizmos.color = Validate(out _) ? new Color(.2f,.8f,.4f) : Color.red;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, clearance * 2);
            Gizmos.DrawLine(Vector3.zero, Vector3.down * (clearance.y + .03f));
            UnityEditor.Handles.Label(transform.position + Vector3.up * .2f, profile ? profile.name : "Missing loot profile");
        }
#endif
    }
}
