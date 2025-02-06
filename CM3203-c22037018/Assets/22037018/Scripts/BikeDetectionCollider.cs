using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BikeDetectionCollider : MonoBehaviour
{
    [SerializeField] private BicycleAI parent;
    [SerializeField] private bool frontFlag;

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "AI")
        {
            parent.GetComponent<BicycleAI>().SetCollisionFrontRear(other.gameObject.transform.root, frontFlag);
        }

    }

    private void OnTriggerExit(Collider other) {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "AI")
        {
            parent.GetComponent<BicycleAI>().SetCollisionFrontRear(null, frontFlag);
        }
    }


}
