using System;
using LastSignal.Atmosphere;
using UnityEngine;

namespace LastSignal.VisualSlice
{
    /// <summary>Serialized rollback records. No runtime callbacks; artist content has a separate root.</summary>
    public sealed class FirstTownSliceState : MonoBehaviour
    {
        public FirstTownSliceProfile profile;
        public string sourceHash, baselineTerrainHash, generation, placementDigest;
        public string generatedTerrainHash, atmosphereHash, generatedSkyHash, generatedLightingHash;
        public Terrain terrain;
        public TerrainData baselineTerrain;
        public Material baselineTerrainMaterial;
        public GameObject generated;
        public Transform artistContent;
        public RendererRecord[] renderers = Array.Empty<RendererRecord>();
        public GameObject[] hiddenVegetation = Array.Empty<GameObject>();
        public TownAtmosphere atmosphere;
        public Material baselineSky;
        public TownLightingProfile baselineLighting;
        public TownAtmosphere.Surface[] baselineSurfaces;
        public Renderer[] baselineWetRenderers;
        public TownAtmosphere.Lamp[] baselineLamps;
        public int trees, shrubs, rocks, grass;
        public string productionSaveSlot, reviewSaveSlot;
        public bool reviewSlotEnabled;
        [Serializable] public sealed class RendererRecord
        {
            public Renderer target;
            public bool enabled;
            public Material[] materials;
        }
    }
}
