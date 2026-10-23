using UnityEngine;

namespace LastSignal.Forest
{
    /// <summary>Only Generated belongs to the rebuild operation. Other scene roots are preserved.</summary>
    public sealed class ForestPrototypeState : MonoBehaviour
    {
        public ForestPrototypeSettings settings;
        public GameObject generated;
        public Terrain terrain;
        public Transform canopy;
        public Transform[] treeShadowRoots;
        public Transform undergrowth;
        public Camera gameplayCamera;
        public Transform player;
        public Transform[] shots;
        public string generationFolder;
        public string placementDigest;
        public int canopyCount, fernCount, shrubCount, rockCount, detailCount;
        public bool navigationNeedsBake = true;
    }
}
