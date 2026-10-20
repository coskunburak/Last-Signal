using UnityEngine;
using System.Collections.Generic;

public class TreeFall : MonoBehaviour
{
    [Range(0, 20)]
    public int numSections = 4;

    //precentage at which tree should break
    public float PercentageThreshold;

    public GameObject parentTree;
    public GameObject[] InitialPieces;
    public GameObject[] treetoppieces;
    public GameObject treebottom;


    [SerializeField]
    private float section_height = 0;
    private float[] minmax;
    private List<List<GameObject>> initialSections;

    // Start is called before the first frame update
    void Start()
    {
        //make sure you dont tag tree parent with the same tag 
        InitialPieces = GameObject.FindGameObjectsWithTag("Tree");
        minmax = FindMinMaxHeights(InitialPieces);
        section_height = (minmax[1] - minmax[0])/numSections;
        initialSections = createSections(InitialPieces);
    }

    //to implement for each row of 
    public void CurrentPieceStatus()
    {
        GameObject[] currentpieces = findRemainingTreePieces();
        List<List<GameObject>> currentSections = createSections(currentpieces);
        for (int isec = 0; isec < numSections; isec++)
        {
            float currentSectionPiecesCount = currentSections[isec].Count;
            Debug.Log("Section " + isec + " has " + currentSectionPiecesCount + " of " + initialSections[isec].Count + " pieces remaining");
            if (((currentSectionPiecesCount / initialSections[isec].Count)) < (PercentageThreshold / 100))
            {
                Debug.Log("Destroy Tree, section " + isec + " is too weak");
                //set all pieces as non kinematic and enable gravity(rigidbody)
                foreach (GameObject treepiece in treetoppieces)
                {
                    treepiece.GetComponent<Rigidbody>().isKinematic = false;
                }

                for (int isec2 = isec; isec2 < numSections; isec2++)
                {
                    foreach (GameObject pieces in currentSections[isec2])
                    {
                        Rigidbody rb = pieces.gameObject.GetComponent<Rigidbody>();
                        if (rb != null)
                            rb.isKinematic = false;
                    }
                }
                break;
            }
        }

        checkFloatingObjects(currentpieces);

    }

    private GameObject[] findRemainingTreePieces()
    {
        GameObject[] remainingpieces = GameObject.FindGameObjectsWithTag("Tree");
        List<GameObject> trueremainingpieces = new List<GameObject>();
        foreach (GameObject piece in remainingpieces)
        {
            if (piece.GetComponent<Rigidbody>().isKinematic)
                trueremainingpieces.Add(piece);
        }
        return trueremainingpieces.ToArray();
    }

    private List<List<GameObject>> createSections(GameObject[] myObjects)
    {
        List<List<GameObject>> sectionedObjects = new List<List<GameObject>>();
        for (float secyval = minmax[0]; secyval < minmax[1] - 1e-15f; secyval += section_height)
        {
            //Debug.Log("Current section min pos " + secyval + " max pos " + (secyval + section_height));
            List<GameObject> curSection = new List<GameObject>();
            foreach (GameObject curObject in myObjects)
            {
                float objectGlobalY = curObject.transform.position.y;
                //Debug.Log("GLobal position of object transform " + objectGlobalY);
                if (secyval <= objectGlobalY && objectGlobalY <= secyval + section_height)
                {
                    curSection.Add(curObject); //Add object in the section
                }
            }
            sectionedObjects.Add(curSection); //Add the section to the list of sections
        }
        return sectionedObjects;
    }

    public float[] FindMinMaxHeights(GameObject[] myObjects)
    {
        float[] minmax2 = new float[2];
        minmax2[0] = float.MaxValue;
        minmax2[1] = float.MinValue;
        //Debug.Log("Number of initial objects: " + myObjects.Length);
        //for (int ichild=0; ichild < parentTree.transform.childCount; ichild++)
        //{
        //}
        foreach (GameObject curObject in myObjects)
        {
            //Transform curTransform = parentTree.transform.GetChild(ichild);
            //GameObject curObject = curTransform.gameObject;
            //Debug.Log("Local Transform position of object: " + curObject.transform.position);
            //Debug.Log("Current object name: " + curObject.name);
            //Debug.Log("Local transform position of object: " + curObject.transform.position);
            //Debug.Log("GLobal Transform position of object: " + curObject.transform.TransformPoint(curObject.transform.position));
            //Debug.Log("local transform opsition fo object not using go: " + curTransform.position);
            //Debug.Log("global transform opsition fo object not using go: " + curTransform.TransformPoint(curTransform.position));
            foreach (Vector3 localposition in curObject.GetComponent<MeshFilter>().mesh.vertices)
            {
                Vector3 position = curObject.transform.TransformPoint(localposition);
                //Debug.Log("local position of vertex " + localposition);
                //Debug.Log("global position of vertex " + position);
                if (position.y < minmax2[0]) minmax2[0] = position.y;
                if (position.y > minmax2[1]) minmax2[1] = position.y;
            }
        }
        return minmax2;
    }
    // Update is called once per frame 
    public void checkFloatingObjects(GameObject[] myObjects)
    {
        foreach (GameObject myObject in myObjects)
        {
            Bounds bound = myObject.GetComponent<Renderer>().bounds;
            Collider[] colliders = Physics.OverlapBox(myObject.transform.position, bound.size*0.5f);
            //Debug.Log("checking: " + myObject.name + " has " + colliders.Length);
            int finalColliderCount = 0;
            foreach (Collider collider in colliders)
            {
                if (collider.gameObject != null  //The tree piece isn't destroyed
                    && collider.gameObject.GetComponent<Rigidbody>().isKinematic  //and it's connected to the tree
                    && collider.name != myObject.name) //and it isn't the object we are currently checking
                    finalColliderCount += 1; //then count the piece
            }
            if (finalColliderCount == 0)
            {
                myObject.gameObject.GetComponent<Rigidbody>().isKinematic = false; //make the object fall, nothing is supporting it
            }
        }
    }
    void Update()
    {
        //only for testing, place to check after each axe strike 
        //CurrentPieceStatus();
    }
}