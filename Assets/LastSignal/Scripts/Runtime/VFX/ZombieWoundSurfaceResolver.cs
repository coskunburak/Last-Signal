using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace LastSignal
{
    /// <summary>Readable project-owned meshes only. CPU skinning at impact; three vertices per active anchor update.
    /// Rest-space coordinates in UV3/UV4 make the overlay follow exactly the source skinning, including bending.</summary>
    public sealed class ZombieWoundSurfaceResolver
    {
        static readonly ProfilerMarker Marker = new ProfilerMarker("LastSignal.Wounds.SurfaceResolve");
        public sealed class Surface
        {
            public SkinnedMeshRenderer renderer;
            public Mesh original;
            public Vector3[] rest, normals, deformed, world;
            public BoneWeight[] weights;
            public Matrix4x4[] bindposes, matrices;
            public Transform[] bones;
            public int[] triangles;
            public ZombieBodyPart part;
            public bool clothing;
            public string name;
            public bool Visible => renderer && renderer.enabled && renderer.gameObject.activeInHierarchy;
            int frame = -1;
            public void UpdateMatrices()
            {
                if (frame == Time.frameCount) return;
                frame = Time.frameCount;
                for (int i = 0; i < matrices.Length; i++)
                    matrices[i] = bones[i] ? bones[i].localToWorldMatrix * bindposes[i] : renderer.transform.localToWorldMatrix;
            }
            public Vector3 Skin(int vertex)
            {
                var w = weights[vertex]; var p = deformed[vertex];
                return matrices[w.boneIndex0].MultiplyPoint3x4(p) * w.weight0 +
                    matrices[w.boneIndex1].MultiplyPoint3x4(p) * w.weight1 +
                    matrices[w.boneIndex2].MultiplyPoint3x4(p) * w.weight2 +
                    matrices[w.boneIndex3].MultiplyPoint3x4(p) * w.weight3;
            }
            public void Sample() { UpdateMatrices(); for (int i = 0; i < world.Length; i++) world[i] = Skin(i); }
            public void Invalidate() => frame = -1;
        }
        public struct Contact
        {
            public Surface surface;
            public int triangle;
            public Vector3 barycentric, position, normal, restPoint, restNormal;
            public float distance;
            public bool Valid => surface != null && surface.Visible;
            public bool Evaluate(out Vector3 point, out Vector3 normal)
            {
                point = position; normal = this.normal;
                if (!Valid) return false;
                surface.UpdateMatrices(); int k = triangle * 3;
                Vector3 a = surface.Skin(surface.triangles[k]), b = surface.Skin(surface.triangles[k + 1]), c = surface.Skin(surface.triangles[k + 2]);
                point = a * barycentric.x + b * barycentric.y + c * barycentric.z;
                normal = Vector3.Cross(b - a, c - a).normalized;
                return Finite(point) && normal.sqrMagnitude > .5f;
            }
        }
        public readonly List<Surface> Surfaces = new List<Surface>(16);
        public int ResolutionMisses { get; private set; }
        public ZombieWoundSurfaceResolver(GameObject owner)
        {
            foreach (var r in owner.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var part = PartFor(r.name); var m = r.sharedMesh;
                if (part == ZombieBodyPart.Unspecified || !m || !m.isReadable || m.vertexCount > 16000 || !m.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord2)) continue;
                var weights = m.boneWeights; var bind = m.bindposes; var bones = r.bones;
                if (m.normals.Length != m.vertexCount || weights.Length != m.vertexCount || bind.Length == 0 || bind.Length != bones.Length) continue;
                bool valid = true;
                foreach (var w in weights) if (w.boneIndex0 >= bones.Length || w.boneIndex1 >= bones.Length || w.boneIndex2 >= bones.Length || w.boneIndex3 >= bones.Length) { valid = false; break; }
                if (!valid) continue;
                var vertices = m.vertices;
                Surfaces.Add(new Surface { renderer = r, name = r.name, original = m, rest = vertices, deformed = (Vector3[])vertices.Clone(),
                    normals = m.normals, world = new Vector3[vertices.Length], weights = weights, triangles = m.triangles,
                    bindposes = bind, bones = bones, matrices = new Matrix4x4[bones.Length], part = part,
                    clothing = r.name == "Pelvis" || part == ZombieBodyPart.LeftLeg || part == ZombieBodyPart.RightLeg });
            }
        }
        public bool Resolve(DamageInfo hit, float search, out Contact contact)
        {
            contact = default;
            if (!Finite(hit.HitPoint) || !Finite(hit.Direction) || hit.Direction.sqrMagnitude < .0001f) return false;
            Vector3 direction = hit.Direction.normalized;
            bool found = Raycast(new Ray(hit.HitPoint - direction * search, direction), search * 2, hit.BodyPart, out contact);
            // A hit can already lie on the mesh while the entry ray first crosses another triangle,
            // notably around the head and calves. Prefer a closer facing triangle at the actual hit.
            float best = Mathf.Min(.12f, search);
            if (found)
            {
                float rayDistance = Vector3.Distance(hit.HitPoint, contact.position);
                if (rayDistance <= .02f) return true;
                best = Mathf.Min(best, rayDistance);
            }
            // Anatomical capsules also enclose empty space. Keep nearest-surface projection bounded
            // and facing the incoming shot; never substitute the hitbox centre or the far side.
            best *= best;
            using (Marker.Auto()) foreach (var surface in Surfaces)
            {
                if (!surface.Visible || surface.part != hit.BodyPart || surface.renderer.bounds.SqrDistance(hit.HitPoint) > best) continue;
                surface.Invalidate(); surface.Sample(); var t = surface.triangles; var v = surface.world;
                for (int i = 0; i < t.Length; i += 3)
                {
                    Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                    Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                    if (Vector3.Dot(n, direction) > -.1f) continue;
                    Vector3 bary = ClosestBarycentric(hit.HitPoint, a, b, c);
                    Vector3 point = a * bary.x + b * bary.y + c * bary.z;
                    float d = (point - hit.HitPoint).sqrMagnitude; if (d >= best) continue;
                    best = d;
                    contact = new Contact { surface = surface, triangle = i / 3, barycentric = bary, position = point, normal = n,
                        restPoint = surface.rest[t[i]] * bary.x + surface.rest[t[i + 1]] * bary.y + surface.rest[t[i + 2]] * bary.z,
                        restNormal = (surface.normals[t[i]] * bary.x + surface.normals[t[i + 1]] * bary.y + surface.normals[t[i + 2]] * bary.z).normalized };
                }
            }
            if (!contact.Valid) ResolutionMisses++;
            return contact.Valid;
        }
        public bool Raycast(Ray ray, float distance, ZombieBodyPart part, out Contact contact)
        {
            contact = default; float best = distance;
            using (Marker.Auto()) foreach (var surface in Surfaces)
            {
                if (!surface.Visible || part != ZombieBodyPart.Unspecified && part != surface.part) continue;
                if (!surface.renderer.bounds.IntersectRay(ray, out float entry) || entry > best) continue;
                surface.Invalidate(); surface.Sample(); var t = surface.triangles; var v = surface.world;
                for (int i = 0; i < t.Length; i += 3)
                {
                    if (!Intersect(ray, v[t[i]], v[t[i + 1]], v[t[i + 2]], out float d, out Vector3 bary) || d >= best) continue;
                    best = d;
                    contact = new Contact { surface = surface, triangle = i / 3, barycentric = bary, distance = d,
                        position = ray.GetPoint(d), normal = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).normalized,
                        restPoint = surface.rest[t[i]] * bary.x + surface.rest[t[i + 1]] * bary.y + surface.rest[t[i + 2]] * bary.z,
                        restNormal = (surface.normals[t[i]] * bary.x + surface.normals[t[i + 1]] * bary.y + surface.normals[t[i + 2]] * bary.z).normalized };
                }
            }
            return contact.Valid;
        }
        public static Vector3 ClosestBarycentric(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a; var ac = c - a; var ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return Vector3.right;
            var bp = p - b; float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return Vector3.up;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) { float t = d1 / (d1 - d3); return new Vector3(1-t,t,0); }
            var cp = p - c; float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return Vector3.forward;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) { float t = d2 / (d2 - d6); return new Vector3(1-t,0,t); }
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4-d3 >= 0 && d5-d6 >= 0) { float t = (d4-d3) / ((d4-d3)+(d5-d6)); return new Vector3(0,1-t,t); }
            float sum = va + vb + vc;
            if (Mathf.Abs(sum) < 1e-12f) return Vector3.right;
            return new Vector3(va,vb,vc) / sum;
        }
        public static bool Intersect(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance, out Vector3 bary)
        {
            distance = 0; bary = default;
            var edge1 = b - a; var edge2 = c - a; var p = Vector3.Cross(ray.direction, edge2);
            float det = Vector3.Dot(edge1, p);
            if (det <= .00000001f) return false; // Front faces only: never project a rear shot onto the chest front.
            float inverse = 1 / det; var s = ray.origin - a;
            float u = Vector3.Dot(s, p) * inverse; if (u < 0 || u > 1) return false;
            var q = Vector3.Cross(s, edge1); float v = Vector3.Dot(ray.direction, q) * inverse;
            if (v < 0 || u + v > 1) return false;
            distance = Vector3.Dot(edge2, q) * inverse; bary = new Vector3(1 - u - v, u, v);
            return distance >= 0;
        }
        public static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
        public static ZombieBodyPart PartFor(string name)
        {
            switch (name)
            {
                case "Head": return ZombieBodyPart.Head;
                case "Ribcage": case "Pelvis": return ZombieBodyPart.Torso;
                case "ArmL": case "ForeArmL": return ZombieBodyPart.LeftArm;
                case "ArmR": case "ForeArmR": return ZombieBodyPart.RightArm;
                case "HandL": return ZombieBodyPart.LeftHand;
                case "HandR": return ZombieBodyPart.RightHand;
                case "LegL": case "CalfL": case "LowerLegL": case "FootL": return ZombieBodyPart.LeftLeg;
                case "LegR": case "CalfR": case "LowerLegR": case "FootR": return ZombieBodyPart.RightLeg;
                default: return ZombieBodyPart.Unspecified;
            }
        }
    }
}
