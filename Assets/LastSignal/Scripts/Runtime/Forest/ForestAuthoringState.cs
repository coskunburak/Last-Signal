using UnityEngine;
namespace LastSignal.Forest
{
    /// <summary>Serialized ownership boundary; no Update. Generated content is replaceable, HeroTrees is preserved.</summary>
    public sealed class ForestAuthoringState : MonoBehaviour
    {
        public ForestLayout layout;
        public Terrain terrain;
        public TerrainData baselineTerrain;
        public GameObject generated;
        public Transform heroTrees;
        public Transform[] treeShadowRoots;
        void OnEnable()
        {
            if (!Application.isPlaying || treeShadowRoots == null) return;
            foreach (var root in treeShadowRoots)
                if (root) foreach (Transform tree in root)
                    RenderingQuality.VegetationShadowController.Attach(tree.gameObject);
        }
        public GameObject legacyForest;
        public string sourceHash, generationFolder, placementDigest;
        public int trees,bushes,rocks,grassInstances;
        public int grassPrototypeIndex=-1;
    }
}
