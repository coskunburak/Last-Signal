using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterController = TreeChoppingSimulation.CharacterController;

public class PieceWiseObjectDestruction : MonoBehaviour
{

    public float breakMultiplier;
    public float strength;
  

    private Vector3 impactPoint = Vector3.zero;
    private float impactRadius = 0f;

    public GameObject DetachedObjectsHolder;
    public GameObject playerController;
    public GameObject topTree;
    public Rigidbody[] rigids;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Axe" || collision.gameObject.tag == "Tree")
        {
            playerController.GetComponent<CharacterController>().stopLerp();

            if (CheckMomentumOrVelocity(collision, strength))
            {
                //impact point location
                impactPoint = collision.contacts[0].point;

                //impact radius
                impactRadius = playerController.GetComponent<CharacterController>().lastPower / breakMultiplier;

                //to check overlapCapsule using impactRadius as centre of axe blade
                Collider[] impactedPieces = Physics.OverlapSphere(impactPoint, impactRadius);

                //make impactedpieces non kinematic
                foreach (Collider impactedPiece in impactedPieces)
                {
                    if (impactedPiece.tag == "Axe" || impactedPiece.tag == "Player") continue;
                    Rigidbody rb = impactedPiece.GetComponent<Rigidbody>();
                    Destroy(impactedPiece.gameObject, 3);
                    rb.isKinematic = false;
                    rb.AddForce(new Vector3(3f, 0f, 0f), ForceMode.Impulse);
                }

                playerController.GetComponent<CharacterController>().lastPower = 0f;

                GetComponent<Collider>().enabled = !GetComponent<Rigidbody>().isKinematic;
                topTree.GetComponent<TreeFall>().CurrentPieceStatus();
            }

        }

       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    bool CheckMomentumOrVelocity(Collision collision, float check)
    {
        if (collision.rigidbody != null)
        {
            return playerController.GetComponent<CharacterController>().lastPower * collision.rigidbody.mass > check;
        }
        //If no rigidbody don't check mass (should never be here, mass is also always 1 in our simluation)
        return playerController.GetComponent<CharacterController>().lastPower > check;
    }
}
