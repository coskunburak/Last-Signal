using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using LastSignal.Forest;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastSignal.Editor.FirstTownForest
{
    internal static class ForestPrototypeTerrain
    {
        internal static Transform Child(string name, Transform parent)
        { var g = new GameObject(name); g.transform.SetParent(parent, false); return g.transform; }
        internal static Vector3 Ground(Terrain terrain, Vector2 p) => new Vector3(p.x, terrain.SampleHeight(new Vector3(p.x, 0, p.y)) + terrain.transform.position.y, p.y);
        internal static Terrain Build(ForestPrototypeSettings settings, ForestPrototypeAssets assets, Transform parent, string folder)
        {
            var data = new TerrainData { name = "Forest120", heightmapResolution = 513, alphamapResolution = 512,
                baseMapResolution = 1024, size = new Vector3(120, 18, 120) };
            var heights = new float[513, 513];
            for (int y = 0; y < 513; y++) for (int x = 0; x < 513; x++)
                heights[y, x] = settings.Height(new Vector2(x * 120f / 512, y * 120f / 512)) / 18;
            data.SetHeights(0, 0, heights); data.terrainLayers = assets.layers;
            var splat = new float[512, 512, 4];
            for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++)
            {
                var w = settings.SurfaceWeights(new Vector2(x * 120f / 511, y * 120f / 511));
                for (int layer = 0; layer < 4; layer++) splat[y, x, layer] = w[layer];
            }
            data.SetAlphamaps(0, 0, splat);
            AssetDatabase.CreateAsset(data, folder + "/Terrain.asset");
            var g = Terrain.CreateTerrainGameObject(data); g.name = "Terrain — 120 x 120 metres"; g.transform.SetParent(parent, false);
            var terrain = g.GetComponent<Terrain>(); terrain.materialTemplate = assets.terrainMaterial;
            terrain.drawInstanced = true; terrain.heightmapPixelError = 4; terrain.basemapDistance = 100;
            terrain.detailObjectDistance = 38; terrain.detailObjectDensity = 1; terrain.shadowCastingMode = ShadowCastingMode.On;
            return terrain;
        }
        internal static GameObject Place(GameObject prefab, Transform parent, Terrain terrain, Vector2 p, float scale, float yaw, float sink = .035f)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            g.transform.SetPositionAndRotation(Ground(terrain, p) - Vector3.up * sink, Quaternion.Euler(0, yaw, 0));
            g.transform.localScale = Vector3.one * scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform); return g;
        }
        internal static void Populate(ForestPrototypeState state, ForestPrototypeAssets assets)
        {
            var s = state.settings; var t = state.terrain; var rng = new System.Random(s.seed ^ 719);
            float R() => (float)rng.NextDouble();
            var points = s.SampleCanopy(); var digest = new StringBuilder();
            var vegetation = Child("Vegetation", state.generated.transform);
            state.canopy = Child("Canopy — decorative, no gathering authority", vegetation);
            state.undergrowth = Child("Undergrowth", vegetation);
            var heroRoot = Child("HeroTrees", vegetation);
            Place(assets.hero, heroRoot, t, s.hero, 1, 205);
            foreach (var p in points)
            {
                // Broadleaf is uncommon and follows the moist habitat; neighboring conifers use multiple crowns.
                int variant = R() < .07f * s.Moisture(p) ? 5 : rng.Next(5);
                float scale = Mathf.Lerp(.87f, 1.1f, R());
                var g = Place(assets.canopy[variant], state.canopy, t, p, scale, R() * 360);
                g.name = "Tree_" + state.canopyCount.ToString("D3") + "_" + assets.canopy[variant].name;
                digest.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0:F4},{1:F4},{2},{3:F4};", p.x, p.y, variant, scale);
                state.canopyCount++;
            }
            using (var sha = SHA256.Create()) state.placementDigest = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(digest.ToString()))).Replace("-", "");

            // A separate deterministic stream keeps canopy unchanged when floor budgets are adjusted.
            rng = new System.Random(s.seed ^ 391);
            var fernRoot = Child("Fern colonies", state.undergrowth);
            var shrubRoot = Child("Low shrubs", state.undergrowth);
            var details = Child("EnvironmentalDetails", state.generated.transform);
            var rocks = Child("Embedded rocks", details); var debris = Child("Fallen branches", details);
            var occupied = new Dictionary<Vector2Int, List<Vector2>>();
            bool Clear(Vector2 p, float distance)
            {
                var cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                    if (occupied.TryGetValue(cell + new Vector2Int(x, y), out var found))
                        foreach (var q in found) if ((q - p).sqrMagnitude < distance * distance) return false;
                if (!occupied.TryGetValue(cell, out var list)) occupied[cell] = list = new List<Vector2>(); list.Add(p); return true;
            }
            bool Allowed(Vector2 p, float margin)
            {
                if (p.x < 2 || p.y < 2 || p.x > 118 || p.y > 118 || s.Excluded(p, 0) || s.TrailDistance(p) < margin || s.Slope(p) > 28) return false;
                return !points.Any(q => (q - p).sqrMagnitude < .85f * .85f);
            }
            for (int attempt = 0; attempt < 45000 && state.fernCount < s.fernBudget; attempt++)
            {
                // Correlated colonies with small local patches, lower density in the dry clearing.
                var p = new Vector2(R() * 120, R() * 120);
                float patch = Mathf.SmoothStep(.35f, .65f, s.Noise(p, .13f, 21));
                float density = patch * s.Moisture(p) * (s.ClearingDistance(p) < -2 ? .15f : 1);
                if (!Allowed(p, .45f) || R() > density || !Clear(p, .55f)) continue;
                Place(assets.ferns[rng.Next(assets.ferns.Count)], fernRoot, t, p, Mathf.Lerp(.85f, 1.75f, R()), R() * 360, .015f);
                state.fernCount++;
            }
            for (int attempt = 0; attempt < 12000 && state.shrubCount < s.shrubBudget; attempt++)
            {
                var p = new Vector2(R() * 120, R() * 120);
                if (!Allowed(p, 1) || R() > s.Density(p) * .55f || !Clear(p, .95f)) continue;
                Place(assets.shrub, shrubRoot, t, p, Mathf.Lerp(.55f, 1, R()), R() * 360); state.shrubCount++;
            }
            for (int attempt = 0; attempt < 15000 && state.rockCount < s.rockBudget; attempt++)
            {
                var p = new Vector2(R() * 120, R() * 120);
                if (!Allowed(p, 1.7f) || R() > s.Noise(p, .05f, 27) * .4f || !Clear(p, .9f)) continue;
                float scale = Mathf.Lerp(.25f, 1.6f, R());
                Place(assets.rock, rocks, t, p, scale, R() * 360, .18f * scale); state.rockCount++;
            }
            for (int i = 0; i < 120; i++)
            {
                var p = new Vector2(R() * 120, R() * 120); if (!Allowed(p, 2.5f) || R() > s.Density(p)) continue;
                Place(assets.debris, debris, t, p, Mathf.Lerp(.65f, 1.35f, R()), R() * 360, .02f);
            }
            Grass(state, assets);
            Surround(state, assets);
        }
        static void Surround(ForestPrototypeState state, ForestPrototypeAssets assets)
        {
            // Visual-only apron. The playable Terrain and its collision remain exactly 120 x 120 m.
            var parent = Child("Non-playable distant dressing", state.generated.transform);
            float Height(Vector2 p)
            {
                var clamped = new Vector2(Mathf.Clamp(p.x, 0, 120), Mathf.Clamp(p.y, 0, 120));
                float outside = Vector2.Distance(p, clamped);
                return state.settings.Height(p) + 3.5f * Mathf.SmoothStep(0, 1, Mathf.Clamp01(outside / 22));
            }
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var indices = new List<int>();
            for (int z = -24; z < 144; z += 2) for (int x = -24; x < 144; x += 2)
            {
                if (x >= 0 && x < 120 && z >= 0 && z < 120) continue;
                int start = vertices.Count;
                foreach (var p in new[] { new Vector2(x, z), new Vector2(x, z + 2), new Vector2(x + 2, z + 2), new Vector2(x + 2, z) })
                { vertices.Add(new Vector3(p.x, Height(p), p.y)); uv.Add(p / 3.5f); }
                indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            var mesh = new Mesh { name = "Visual-only terrain apron" }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, state.generationFolder + "/DistantGround.asset");
            var ground = Child("Uncollidable distant ground", parent); ground.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            ground.gameObject.AddComponent<MeshRenderer>().sharedMaterial = assets.distantGroundMaterial;
            var rng = new System.Random(state.settings.seed ^ 1183); var accepted = new List<Vector2>();
            for (int i = 0; i < 6000 && accepted.Count < 150; i++)
            {
                var p = new Vector2(-22 + (float)rng.NextDouble() * 164, -22 + (float)rng.NextDouble() * 164);
                if (p.x > -2 && p.x < 122 && p.y > -2 && p.y < 122) continue;
                if (accepted.Any(q => (p - q).sqrMagnitude < 36)) continue;
                accepted.Add(p);
                var g = (GameObject)PrefabUtility.InstantiatePrefab(assets.canopy[rng.Next(5)], parent);
                g.name = "Distant decorative spruce"; g.transform.position = new Vector3(p.x, Height(p) - .06f, p.y);
                g.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360, 0);
                g.transform.localScale = Vector3.one * (.9f + (float)rng.NextDouble() * .2f);
                foreach (var c in g.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
                foreach (var lod in g.GetComponentsInChildren<LODGroup>())
                {
                    var levels = lod.GetLODs();
                    // Vendor renderers can be nested. Remove geometry components, not parents
                    // that may also contain retained LOD renderers or other pending removals.
                    var retained = levels[levels.Length - 1].renderers;
                    foreach (var r in g.GetComponentsInChildren<MeshRenderer>())
                        if (!retained.Contains(r))
                        {
                            var filter = r.GetComponent<MeshFilter>();
                            UnityEngine.Object.DestroyImmediate(r);
                            if (filter) UnityEngine.Object.DestroyImmediate(filter);
                        }
                    lod.SetLODs(new[] { new LOD(.009f, retained) { fadeTransitionWidth = .2f } }); lod.RecalculateBounds();
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
            }
        }
        static void Grass(ForestPrototypeState state, ForestPrototypeAssets assets)
        {
            var data = state.terrain.terrainData; var s = state.settings;
            data.SetDetailResolution(256, 16); data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            data.detailPrototypes = new[] { new DetailPrototype { prototype = assets.grass, usePrototypeMesh = true, useInstancing = true,
                renderMode = DetailRenderMode.VertexLit, minWidth = .65f, maxWidth = 1.2f, minHeight = .65f, maxHeight = 1.3f,
                healthyColor = Color.white, dryColor = Color.white, noiseSpread = .17f } };
            var layer = new int[256, 256]; var random = new System.Random(s.seed ^ 877);
            // Shuffle cells so the bounded budget never fills only one side of the terrain.
            var indices = Enumerable.Range(0, 256 * 256).ToArray();
            for (int i = indices.Length - 1; i > 0; i--) { int j = random.Next(i + 1); (indices[i], indices[j]) = (indices[j], indices[i]); }
            foreach (int i in indices)
            {
                if (state.detailCount >= s.detailBudget) break;
                int x = i % 256, y = i / 256; var p = new Vector2((x + .5f) * 120 / 256, (y + .5f) * 120 / 256);
                float patch = s.Noise(p, .19f, 31); float open = Mathf.Lerp(.45f, .9f, 1 - s.Density(p));
                if (s.Excluded(p, 0) || s.TrailDistance(p) < .25f || patch < .44f || random.NextDouble() > open * patch) continue;
                layer[y, x] = 1; state.detailCount++;
            }
            data.SetDetailLayer(0, 0, 0, layer);
        }
    }
}
