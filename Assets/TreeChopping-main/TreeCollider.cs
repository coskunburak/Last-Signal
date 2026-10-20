using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// Written by Guilty_Party 2025
/// Use as you see fit.
/// 
/// Attach to your tree collider prefab along with a capsule collider.
/// Gets instantiated by the TreeColliderManager.
/// YOU MUST REMOVE THE COLLIDERS THAT WILL BE ATTACHED TO YOUR TREE PREFABS PLACED ON YOUR TERRAIN.
/// This system relies on using its own colliders, it will not work with unity tree colliders.
/// </summary>
public class TreeCollider : MonoBehaviour
{
    private Terrain terrain;
    private Vector3 worldPosition;
    private int prototypeIndex;

    // these variables are used to choose the current health of the tree on start up to add some randomness.
    [SerializeField] private float currHealth = 3f;
    [SerializeField] private float minHealth = 3f;
    [SerializeField] private float maxHealth = 12f;

    //inventory items added on tree chopped.
    //[SerializeField] InventoryItem logItem;
    //[SerializeField] InventoryItem branchItem;

    //the prefab that is instantiated when the tree is fully cut down.
    //You can use this or grab the prefab from the terrain. Please see the comments in the SpawnFelledTreePrefab() method.
    //as the script comes it gets the terrain tree prefab from the terrain.
    public GameObject fallenTreeePrefab;

    //[Tooltip("Chance to spawn a stick once the tree is chopped. Smaller number = smaller chance.")]
    //[Range(0, 100)]
    //public int chanceToSpawnBranch;

    //[Tooltip("Chance to get wood once the tree is chopped. Smaller number = smaller chance.")]
    //[Range(0, 100)]
    //public int chanceToGetWood;


    public void Initialize(Terrain t, Vector3 pos, int protoIndex)
    {
        terrain = t;
        worldPosition = pos;
        prototypeIndex = protoIndex;
        currHealth = Random.Range(minHealth, maxHealth);
    }

    //call this from your axe script or whatever you're using to register hits on a tree.
    public void TakeDamage(float amount)
    {
        currHealth -= amount;
        
        //SoundManager.Instance.PlaySound(
        //    SoundManager.Instance.woodHitClips[Random.Range(0, SoundManager.Instance.woodHitClips.Length)],
        //    transform.position,
        //    1f
        //);

        //int logSpawnChance = Random.Range(0, 100);
        //if (logSpawnChance < chanceToGetWood + Player.instance.baseResourceGainChance)
        //{
        //    bool added = InventoryUI.Instance.TryAddItem(logItem);
        //    if (added)
        //    {
        //        PickupNotifierUI.Instance.ShowPickupMessage($"Got: {logItem.itemName}", logItem.icon);
        //    }
        //    else
        //    {
        //        PickupNotifierUI.Instance.ShowPickupMessage($"{logItem.name} not added. Weight exceeded.", logItem.icon);
        //    }
        //}

        //int spawnNumber = Random.Range(0, 100);
        //if (spawnNumber <= chanceToSpawnBranch)
        //{
        //    InventoryUI.Instance.TryAddItem(branchItem);
        //    PickupNotifierUI.Instance.ShowPickupMessage($"Got: {branchItem.itemName}", branchItem.icon);
        //}

        if (currHealth <= 0f)
        {
            SpawnFelledTreePrefab();
            RemoveTreeFromTerrain();
            Destroy(gameObject);
            //PlayerXP.Instance.GainXP(3);
            //SoundManager.Instance.PlaySound(
            //    SoundManager.Instance.treeFallClips[Random.Range(0, SoundManager.Instance.treeFallClips.Length)],
            //    transform.position
            //);
        }
    }

    void SpawnFelledTreePrefab()
    {
        //this gets the tree prefab from the terrain data.
        
        TreePrototype[] prototypes = terrain.terrainData.treePrototypes;
        if (prototypeIndex >= 0 && prototypeIndex < prototypes.Length)
           
        {
            GameObject treePrefab = prototypes[prototypeIndex].prefab;
            if (treePrefab != null)
            {
                //comment this if you want to insytantiate a paticular prefab instead of grabbing the one from the terrain.
                GameObject go = Instantiate(treePrefab, worldPosition, Quaternion.identity);

                //uncomment this line out if you want to instantiate a paticular prefab.
                //GameObject go = Instantiate(fallenTreeePrefab, worldPosition, Quaternion.identity);

                //You WILL want to size and position this collider, otherwise it's a sphere and your tree will roll away.
                CapsuleCollider col = go.AddComponent<CapsuleCollider>();

                //size and position the collider.
                col.radius = .5f;
                col.height = 2f;
                col.center = new Vector3(0, 1, 0);
                Destroy(go, 10f);

                //You'll probably want to add a rigid body to the prefab if it doesn't already have one.

                Rigidbody rb = go.AddComponent<Rigidbody>();

                //freeze rigidbody rotation on the Y axis otherwise the tree just rolls away.
                //comment out this line if you like trees that just roll away.
                rb.constraints = RigidbodyConstraints.FreezeRotationY;

                //award the last log.
                //InventoryUI.Instance.TryAddItem(logItem);
                //PickupNotifierUI.Instance.ShowPickupMessage($"Got: {logItem.itemName}", logItem.icon);
            }
        }
    }

    void RemoveTreeFromTerrain()
    {
        TreeInstance[] trees = terrain.terrainData.treeInstances;
        List<TreeInstance> treeList = new(trees);

        int index = -1;
        float closest = float.MaxValue;
        for (int i = 0; i < treeList.Count; i++)
        {
            Vector3 treePos = Vector3.Scale(treeList[i].position, terrain.terrainData.size) + terrain.transform.position;
            float dist = Vector3.Distance(treePos, worldPosition);
            

            if (dist < 2f && dist < closest)
            {
                index = i;
                closest = dist;
            }
        }

        if (index != -1)
        {
            treeList.RemoveAt(index);
            terrain.terrainData.treeInstances = treeList.ToArray();
            terrain.Flush();
        }
    }

     
}
