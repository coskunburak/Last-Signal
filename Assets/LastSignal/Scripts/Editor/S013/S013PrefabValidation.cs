using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LastSignal.Art.Editor
{
    public static class S013PrefabValidation
    {
        // Explicit inputs: decorative foliage need not carry a collider; blocking props must.
        // License approval comes from the reviewed registry, never from a nonempty vendor name.
        public static List<string> Inspect(GameObject root, bool requiresCollider, bool licenseApproved)
        {
            var issues = new List<string>();
            if (!root) { issues.Add("MISSING_ROOT"); return issues; }
            if (!licenseApproved) issues.Add("LICENSE_NOT_APPROVED");
            if ((root.transform.localScale - Vector3.one).sqrMagnitude > .000001f) issues.Add("ROOT_SCALE");
            if (requiresCollider && root.GetComponentsInChildren<Collider>(true).Length == 0) issues.Add("REQUIRED_COLLIDER");
            var ids = new HashSet<string>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var scale = t.localScale;
                if (scale.x <= 0 || scale.y <= 0 || scale.z <= 0) issues.Add("NONPOSITIVE_SCALE:" + t.name);
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) issues.Add("MISSING_SCRIPT:" + t.name);
                var filter = t.GetComponent<MeshFilter>();
                if (filter && !filter.sharedMesh) issues.Add("MISSING_MESH:" + t.name);
                var skin = t.GetComponent<SkinnedMeshRenderer>();
                if (skin && !skin.sharedMesh) issues.Add("MISSING_SKINNED_MESH:" + t.name);
                var renderer = t.GetComponent<Renderer>();
                if (renderer)
                {
                    if (renderer.sharedMaterials.Length == 0) issues.Add("EMPTY_MATERIAL_SLOTS:" + t.name);
                    foreach (var m in renderer.sharedMaterials)
                    {
                        if (!m) { issues.Add("MISSING_MATERIAL:" + t.name); continue; }
                        if (!m.shader || !m.shader.isSupported) issues.Add("UNSUPPORTED_SHADER:" + m.name);
                        else if (m.GetTag("RenderPipeline", false, "") != "UniversalPipeline") issues.Add("NON_URP_SHADER:" + m.name);
                    }
                }
                foreach (var component in t.GetComponents<MonoBehaviour>())
                {
                    if (!component) continue;
                    var serialized = new SerializedObject(component);
                    foreach (var field in new[] { "stableId", "persistentId" })
                    {
                        var property = serialized.FindProperty(field);
                        if (property == null || property.propertyType != SerializedPropertyType.String || string.IsNullOrWhiteSpace(property.stringValue)) continue;
                        // Field/type namespace avoids confusing a loot anchor with its linked world-item identity.
                        var key = component.GetType().FullName + ":" + field + ":" + property.stringValue;
                        if (!ids.Add(key)) issues.Add("DUPLICATE_ID:" + key);
                    }
                }
            }
            return issues;
        }
    }
}
