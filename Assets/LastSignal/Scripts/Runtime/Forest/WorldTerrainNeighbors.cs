using UnityEngine;

namespace LastSignal.Forest
{
    /// <summary>Restores the two authored terrain links on scene load. Does not generate or modify TerrainData.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class WorldTerrainNeighbors : MonoBehaviour
    {
        [SerializeField] Terrain core;
        [SerializeField] Terrain west;
        public Terrain Core => core;
        public Terrain West => west;
        public bool HasValidLayout => core && west && core != west && core.terrainData && west.terrainData &&
            core.gameObject.scene == gameObject.scene && west.gameObject.scene == gameObject.scene &&
            core.transform.rotation == Quaternion.identity && west.transform.rotation == Quaternion.identity &&
            core.transform.lossyScale == Vector3.one && west.transform.lossyScale == Vector3.one &&
            core.terrainData.size == west.terrainData.size &&
            core.terrainData.heightmapResolution == west.terrainData.heightmapResolution &&
            (west.transform.position + Vector3.right * west.terrainData.size.x - core.transform.position).sqrMagnitude < .000001f;
        public void Configure(Terrain coreTerrain, Terrain westTerrain)
        { core = coreTerrain; west = westTerrain; Connect(); }
        void OnEnable() => Connect();
        public void Connect()
        {
            if (!HasValidLayout) return;
            west.SetNeighbors(null, null, core, null);
            core.SetNeighbors(west, null, null, null);
        }
        void OnDisable()
        {
            if (core && core.leftNeighbor == west) core.SetNeighbors(null, core.topNeighbor, core.rightNeighbor, core.bottomNeighbor);
            if (west && west.rightNeighbor == core) west.SetNeighbors(west.leftNeighbor, west.topNeighbor, null, west.bottomNeighbor);
        }
    }
}
