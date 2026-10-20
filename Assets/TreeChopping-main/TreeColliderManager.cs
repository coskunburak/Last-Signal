using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// /// Written by Guilty_Party 2025
/// Use as you see fit.
/// 
/// Adds and removes tree collider prefabs within the specified range of the player.
/// 
/// /// YOU MUST REMOVE THE COLLIDERS THAT WILL BE ATTACHED TO YOUR TREE PREFABS PLACED ON YOUR TERRAIN.
/// This system relies on using its own colliders, it will not work with unity tree colliders.
/// Add this script to a gameobject and fill out the settings/prefab references.
/// </summary>
public class TreeColliderManager : MonoBehaviour
{
    [Header("Preferences")]
    
    [Tooltip("Tree colliders will be set within this distance of the player.")]

    //Make sure the despawnRadius is greater than the spawn radius.
    [Range(0,100)]
    public float spawnRadius = 50f;
    [Tooltip("Tree colliders will be removed when the player moves this distance away.")]
    [Range(30,150)]
    public float despawnRadius = 60f;
    [Tooltip("How often the script checks the distance from the player and updates the colliders.")]
    public float updateInterval = 1f;

    [Space(5)]
    [Header("Refereneces")]
    [Tooltip("Your tree collider prefab. Literally just an empty game object with a capsule collider and the TreeCollider script attached. Size the collider as you see fit.")]
    public GameObject treeColliderPrefab;
    [Tooltip("Your player transform or the transform which the colliders will be spawned/despawned around.")]
    public Transform player;

    //dictionary to hold the tree collider prefabs
    private Dictionary<Vector3, GameObject> activeColliders = new();


    void Start()
    {
        InvokeRepeating(nameof(UpdateTreeColliders), 0f, updateInterval);
    }

    /// <summary>
    /// Adds and removes the tree collider prefabs at an interval set with the updateInterval variable.
    /// </summary>
    void UpdateTreeColliders()
    {
        //make sure you assign the player in the inspector or add an Awake() method and adding it, otherwise this script will silently do nothing.
        if (player == null) return;

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            TerrainData data = terrain.terrainData;
            TreeInstance[] trees = data.treeInstances;

            //check each tree
            foreach (TreeInstance tree in trees)
            {
                Vector3 worldPos = Vector3.Scale(tree.position, data.size) + terrain.transform.position;
                float dist = Vector3.Distance(worldPos, player.position);

                //if it's within range, spawn a collider prefab on it.
                if (dist < spawnRadius)
                {
                    
                    if (!activeColliders.ContainsKey(worldPos))
                    {
                        GameObject colliderObj = Instantiate(treeColliderPrefab, worldPos, Quaternion.identity);
                        colliderObj.GetComponent<TreeCollider>().Initialize(terrain, worldPos, tree.prototypeIndex);
                        activeColliders[worldPos] = colliderObj;
                    }
                }
            }
        }
        // Find any trees which are too far away from the player and remove them.
        List<Vector3> toRemove = new();
        foreach (var kvp in activeColliders)
        {
            float dist = Vector3.Distance(kvp.Key, player.position);
            if (dist > despawnRadius)
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            activeColliders.Remove(key);
        }
    }
}
