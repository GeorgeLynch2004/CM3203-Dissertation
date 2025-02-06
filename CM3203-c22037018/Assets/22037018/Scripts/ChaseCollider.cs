using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseCollider : MonoBehaviour
{
    private Transform carrot;

    private void Start() {
        carrot = gameObject.transform.GetChild(0);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.GetComponent<BicycleAI>() != null)
        {
            BicycleAI ai = other.gameObject.GetComponent<BicycleAI>();

            if (ai.getTargetPosition() != carrot)
            {
                ai.updateTargetPosition(carrot);
            }
        }
    }
}
