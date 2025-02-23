using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ColliderType
    {
        front,
        back,
        anticipation,
    }

public class BikeDetectionCollider : MonoBehaviour
{
    

    [SerializeField] private ColliderType colliderType;
    [SerializeField] private BicycleAI parent;

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "AI")
        {
            parent.GetComponent<BicycleAI>().SetCollision(other.gameObject.transform.root, colliderType);
        }

    }

    private void OnTriggerExit(Collider other) {
        if (other.gameObject.tag == "Player" || other.gameObject.tag == "AI")
        {
            parent.GetComponent<BicycleAI>().SetCollision(null, colliderType);
        }
    }


}
