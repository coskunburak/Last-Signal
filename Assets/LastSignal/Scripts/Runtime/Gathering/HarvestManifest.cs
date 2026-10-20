using System;
using UnityEngine;

namespace LastSignal.Gathering
{
    [CreateAssetMenu(menuName = "Last Signal/Gathering/Harvest Manifest")]
    public sealed class HarvestManifest : ScriptableObject
    {
        public int version = 1, revision = 1;
        public string manifestId;
        public string sourceScene, sourceSha256;
        public Anchor[] anchors = Array.Empty<Anchor>();
        [Serializable] public sealed class Anchor
        {
            public string nodeId, cellId = "resident";
            // Editor locator only. Never an entity identity or a runtime discovery rule.
            public string sourceHierarchy;
            public Vector3 position, scale;
            public Quaternion rotation = Quaternion.identity;
            public ResourceNodeDefinition definition;
        }
        public bool Valid
        {
            get
            {
                if (version != 1 || revision < 1 || !GatheringState.Id(manifestId) || anchors == null || anchors.Length == 0 || anchors.Length > GatheringState.MaximumNodes) return false;
                var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                var definitions=new System.Collections.Generic.Dictionary<string,ResourceNodeDefinition>(StringComparer.Ordinal);
                foreach (var a in anchors)
                {
                    if (a == null || !GatheringState.Id(a.nodeId) || a.nodeId.Length > 100 || !ids.Add(a.nodeId) ||
                        !GatheringState.Id(a.cellId) || !a.definition || !a.definition.Valid || !Finite(a.position) ||
                        !Finite(a.scale) || a.scale.x<=0 || a.scale.y<=0 || a.scale.z<=0 ||
                        !Persistence.SaveValidation.Pose(new Persistence.TransformSnapshot { x=a.position.x,y=a.position.y,z=a.position.z,
                            qx=a.rotation.x,qy=a.rotation.y,qz=a.rotation.z,qw=a.rotation.w })) return false;
                    if (definitions.TryGetValue(a.definition.definitionId,out var previous) && previous!=a.definition) return false;
                    definitions[a.definition.definitionId]=a.definition;
                }
                return true;
            }
        }
        static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
    }
}
