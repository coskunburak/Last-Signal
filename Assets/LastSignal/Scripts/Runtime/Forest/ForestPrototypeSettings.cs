using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Forest
{
    /// <summary>Metres, deterministic masks, and bounded authoring budgets. No runtime scattering.</summary>
    [CreateAssetMenu(menuName = "Last Signal/Forest 120 Settings")]
    public sealed class ForestPrototypeSettings : ScriptableObject
    {
        public const float Size = 120f;
        public int seed = 261021;
        [Range(60, 420)] public int canopyBudget = 290;
        [Range(3.5f, 8)] public float trunkSpacing = 4.6f;
        [Range(0, 2500)] public int fernBudget = 1500;
        [Range(0, 600)] public int shrubBudget = 220;
        [Range(0, 300)] public int rockBudget = 85;
        [Range(0, 16000)] public int detailBudget = 9000;
        public Bounds[] exclusions = Array.Empty<Bounds>();
        public Vector2 clearing = new Vector2(70, 68);
        public Vector2 hero = new Vector2(60, 64);
        public Vector2 spawn = new Vector2(48.64f, 13);

        public float Noise(Vector2 p, float frequency, int salt = 0) =>
            Mathf.PerlinNoise(p.x * frequency + (seed & 1023) * .37f + salt,
                p.y * frequency + ((seed >> 10) & 1023) * .41f + salt * .73f);

        public float TrailX(float z) => 52 + 12 * Mathf.Sin((z - 28) * .043f) + 4 * Mathf.Sin(z * .1f);
        public float TrailDistance(Vector2 p)
        {
            float d = float.PositiveInfinity;
            // Segment distance rather than x-distance keeps bends a consistent walkable width.
            for (int z = 6; z < 114; z += 2)
                d = Mathf.Min(d, ForestLayout.SegmentDistance(p, new Vector2(TrailX(z), z), new Vector2(TrailX(z + 2), z + 2)));
            return d - 1.25f;
        }
        public float ClearingDistance(Vector2 p)
        {
            Vector2 d = p - clearing;
            return new Vector2(d.x, d.y * 1.18f).magnitude - (10.5f + 1.8f * Mathf.Sin(Mathf.Atan2(d.y, d.x) * 3));
        }
        public float Height(Vector2 p)
        {
            float ridge = 3.5f * Mathf.Exp(-Mathf.Pow((p.x - 21 - 8 * Mathf.Sin(p.y * .045f)) / 21, 2));
            float drainage = -1.3f * Mathf.Exp(-Mathf.Pow((p.x - 91 - 5 * Mathf.Sin(p.y * .05f)) / 7, 2));
            float broad = 3 + .026f * p.y + ridge + drainage + 1.8f * (Noise(p, .016f) - .5f);
            float micro = .22f * (Noise(p, .19f, 7) - .5f) + .09f * (Noise(p, .53f, 11) - .5f);
            float trail = Mathf.SmoothStep(0, 1, Mathf.Clamp01(TrailDistance(p) / 3));
            return broad + micro * Mathf.Lerp(.15f, 1, trail);
        }
        public float Slope(Vector2 p)
        {
            float dx = Height(p + Vector2.right * .25f) - Height(p - Vector2.right * .25f);
            float dz = Height(p + Vector2.up * .25f) - Height(p - Vector2.up * .25f);
            return Mathf.Atan(new Vector2(dx, dz).magnitude / .5f) * Mathf.Rad2Deg;
        }
        public float Moisture(Vector2 p) => Mathf.Clamp01(.3f + .5f * Noise(p, .035f, 3) + .2f * Mathf.Exp(-Mathf.Pow((p.x - 91) / 15, 2)));
        public bool Excluded(Vector2 p, float margin)
        {
            if (Vector2.Distance(p, spawn) < 3 + margin || Vector2.Distance(p, hero) < 4 + margin) return true;
            foreach (var box in exclusions)
                if (p.x >= box.min.x - margin && p.x <= box.max.x + margin && p.y >= box.min.z - margin && p.y <= box.max.z + margin) return true;
            return false;
        }
        public float Density(Vector2 p)
        {
            float opening = Mathf.SmoothStep(0, 1, Mathf.Clamp01(ClearingDistance(p) / 8));
            float trail = Mathf.SmoothStep(0, 1, Mathf.Clamp01(TrailDistance(p) / 4));
            float habitat = Mathf.Lerp(.75f, 1, Moisture(p)) * Mathf.InverseLerp(29, 17, Slope(p));
            float cluster = Mathf.Lerp(.25f, 1, Mathf.SmoothStep(.2f, .75f, Noise(p, .052f, 17)));
            return opening * trail * cluster * habitat;
        }
        public Vector4 SurfaceWeights(Vector2 p)
        {
            float soil = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(TrailDistance(p) / 2.5f));
            float rock = Mathf.InverseLerp(17, 30, Slope(p));
            float moss = (1 - soil) * Moisture(p) * .45f;
            var w = new Vector4((1 - soil) * (1 - rock) * (1 - moss), soil, moss, rock * (1 - soil));
            return w / (w.x + w.y + w.z + w.w);
        }

        public List<Vector2> SampleCanopy()
        {
            var random = new System.Random(seed);
            var result = new List<Vector2>();
            var grid = new Dictionary<Vector2Int, List<Vector2>>();
            float spacing = Mathf.Max(3.5f, trunkSpacing);
            for (int attempt = 0; attempt < 60000 && result.Count < canopyBudget; attempt++)
            {
                var p = new Vector2(3 + (float)random.NextDouble() * 114, 3 + (float)random.NextDouble() * 114);
                if (Excluded(p, .6f) || TrailDistance(p) < 2.5f || ClearingDistance(p) < 1 || random.NextDouble() > Density(p)) continue;
                var cell = new Vector2Int(Mathf.FloorToInt(p.x / spacing), Mathf.FloorToInt(p.y / spacing));
                bool overlap = false;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                    if (grid.TryGetValue(cell + new Vector2Int(x, y), out var neighbors))
                        foreach (var n in neighbors) if ((n - p).sqrMagnitude < spacing * spacing) overlap = true;
                if (overlap) continue;
                result.Add(p);
                if (!grid.TryGetValue(cell, out var bucket)) grid[cell] = bucket = new List<Vector2>();
                bucket.Add(p);
            }
            return result;
        }
    }
}
