using System;
using UnityEngine;

namespace LastSignal.Persistence
{
    [DisallowMultipleComponent]
    public sealed class PersistentEntityId : MonoBehaviour
    {
        [SerializeField] string id;
        public string Id => id;
#if UNITY_EDITOR
        void Reset() => id = Guid.NewGuid().ToString("N");
        [ContextMenu("Assign New Persistent ID (breaks existing saves)")]
        void AssignNewId()
        {
            UnityEditor.Undo.RecordObject(this, "Assign persistent identity");
            id = Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
