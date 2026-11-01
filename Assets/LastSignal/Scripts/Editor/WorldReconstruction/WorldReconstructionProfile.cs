using System;
using System.IO;
using UnityEngine;

namespace LastSignal.Editor.WorldReconstruction
{
    // Editor-only planning data. Never owns runtime time, cells, graphics, saves or spawning.
    [Serializable] public sealed class WorldReconstructionProfile
    {
        public const string PlanPath = "Assets/LastSignal/Data/WorldReconstruction/WLD-S001/Plan.json";
        public int schemaVersion, seed, heightmapResolution, previewResolution;
        public string generatorVersion, sourceScene, sourceSceneGuid, sourceSceneSha256;
        public string sourceTerrainGuid, sourceTerrainSha256, proposedStage, generatedAssets;
        public Vector3 sourceOrigin, sourceSize;
        public WorldRectangle core, extension;
        public float seamBlendMeters, verticalPaddingMeters, protectionBlendMeters, mesoscaleMeters;
        public int additionalTreeBudget, additionalFernBudget;
        public MacroForm[] forms;
        public FoundationMask[] foundations;
        public RoutePlan[] routes;
        public Vector3[] drainage; // Upstream to downstream; y is a proposed bed elevation, not water level.
        public float drainageHalfWidth;

        public static WorldReconstructionProfile Load()
        {
            var p = JsonUtility.FromJson<WorldReconstructionProfile>(File.ReadAllText(PlanPath));
            if (p == null) throw new InvalidDataException("Missing world plan.");
            p.Validate(); return p;
        }
        public void Validate()
        {
            if (schemaVersion != 1 || generatorVersion != "WLD-G1-preview-v1") throw new InvalidDataException("Unsupported plan version.");
            if (sourceScene != "Assets/LastSignal/Scenes/Production/FirstTownMain_HDRP.unity" ||
                proposedStage != "Assets/LastSignal/Scenes/Validation/WLD_S001_NaturalWorld_Stage.unity" ||
                generatedAssets != "Assets/LastSignal/Art/WorldReconstruction/WLD-S001/Generations")
                throw new InvalidDataException("Unexpected source/output ownership. Re-audit before changing paths.");
            if (sourceSceneSha256 == null || sourceSceneSha256.Length != 64 || sourceTerrainSha256 == null || sourceTerrainSha256.Length != 64)
                throw new InvalidDataException("Source hashes required.");
            core.Validate(); extension.Validate();
            if (sourceOrigin != new Vector3(core.minX, -10, core.minZ) || sourceSize != new Vector3(core.Width, 20, core.Depth) ||
                extension.maxX != core.minX || extension.minZ != core.minZ || extension.maxZ != core.maxZ || extension.Width != core.Width)
                throw new InvalidDataException("This first plan supports one same-size western neighbor and the surveyed source interval only.");
            if (heightmapResolution != 513 || previewResolution < 33 || previewResolution > 257 ||
                !Finite(seamBlendMeters) || seamBlendMeters < 8 || seamBlendMeters > core.Width / 2 ||
                !Finite(verticalPaddingMeters) || verticalPaddingMeters < 2 || verticalPaddingMeters > 32 ||
                !Finite(protectionBlendMeters) || protectionBlendMeters < 2 || protectionBlendMeters > 64 ||
                !Finite(mesoscaleMeters) || mesoscaleMeters < 0 || mesoscaleMeters > 1 ||
                !Finite(drainageHalfWidth) || drainageHalfWidth < 2 || drainageHalfWidth > 30)
                throw new InvalidDataException("Invalid bounded preview/sampling settings.");
            if (forms == null || foundations == null || routes == null || drainage == null || drainage.Length < 2)
                throw new InvalidDataException("Explicit forms, protections, routes and drainage are required.");
            foreach (var f in forms)
                if (f == null || string.IsNullOrWhiteSpace(f.name) || !Finite(f.center) || !Finite(f.radius) ||
                    f.radius.x < 10 || f.radius.y < 10 || !Finite(f.yaw) || !Finite(f.height) || Mathf.Abs(f.height) > 80)
                    throw new InvalidDataException("Invalid macroform.");
            foreach (var f in foundations) { if (f == null || string.IsNullOrWhiteSpace(f.name)) throw new InvalidDataException("Invalid foundation."); f.area.Validate(); }
            foreach (var route in routes)
            {
                if (route == null || route.points == null || route.points.Length < 2 || !Finite(route.width) || route.width <= 0 ||
                    !Finite(route.shoulder) || route.shoulder < 0 || !Finite(route.maximumGrade) || route.maximumGrade < 0 || route.maximumGrade > 1)
                    throw new InvalidDataException("Invalid route. Grade zero means unverified, never approved.");
                foreach (var point in route.points) if (!Finite(point)) throw new InvalidDataException("Invalid route coordinate.");
            }
            for (int i = 0; i < drainage.Length; i++)
                if (!Finite(drainage[i]) || (i > 0 && drainage[i].y > drainage[i-1].y)) throw new InvalidDataException("Drainage must run downhill.");
            if (additionalTreeBudget < 0 || additionalTreeBudget > 250 || additionalFernBudget < 0 || additionalFernBudget > 500)
                throw new InvalidDataException("Initial additional vegetation budget exceeded.");
        }
        internal static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        internal static bool Finite(Vector2 v) => Finite(v.x) && Finite(v.y);
        internal static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    }
    [Serializable] public struct WorldRectangle
    {
        public float minX, maxX, minZ, maxZ;
        public float Width => maxX - minX;
        public float Depth => maxZ - minZ;
        public bool Contains(Vector2 p) => p.x >= minX && p.x <= maxX && p.y >= minZ && p.y <= maxZ;
        public float Distance(Vector2 p) => new Vector2(Mathf.Max(minX-p.x, 0, p.x-maxX), Mathf.Max(minZ-p.y, 0, p.y-maxZ)).magnitude;
        public void Validate()
        {
            if (!WorldReconstructionProfile.Finite(minX) || !WorldReconstructionProfile.Finite(maxX) ||
                !WorldReconstructionProfile.Finite(minZ) || !WorldReconstructionProfile.Finite(maxZ) || Width <= 0 || Depth <= 0)
                throw new InvalidDataException("Invalid terrain rectangle.");
        }
    }
    [Serializable] public sealed class MacroForm { public string name; public Vector2 center, radius; public float yaw, height; }
    [Serializable] public sealed class FoundationMask { public string name; public WorldRectangle area; }
    [Serializable] public sealed class RoutePlan
    {
        public string name, surface;
        public bool preserveExisting, vehicle;
        public float width, shoulder, maximumGrade; // Grade remains zero until vehicle acceptance supplies a value.
        public Vector2[] points;
    }
}
