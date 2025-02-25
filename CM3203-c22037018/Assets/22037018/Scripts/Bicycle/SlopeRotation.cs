using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // Required for accessing NavMeshAgent

public class SlopeRotation : MonoBehaviour
{
    [SerializeField] private float raycastDistance = 10f; // Max distance of the raycast, can adjust based on terrain height
    [SerializeField] private Transform leftOrigin;
    [SerializeField] private Transform rightOrigin;
    [SerializeField] private LayerMask layerMask; // Add a LayerMask field to specify the "Bike Track" layer
    [SerializeField] private float currentLeanAngle;
    [SerializeField] private float speedThreshold = 12f; // Speed threshold to start leaning
    [SerializeField] private float lerpTime = 0.5f; // Time it takes to lerp in and out

    private NavMeshAgent agent; // Reference to the NavMeshAgent
    private float targetLeanAngle; // The target angle we want to reach
    private float currentLerpTime = 0f; // Timer to keep track of lerping

    void Start()
    {
        // Get the NavMeshAgent component on the same GameObject
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        // Only proceed with rotation if the NavMeshAgent's speed is over the threshold
        if (agent.velocity.magnitude > speedThreshold)
        {
            // Get the leftmost and rightmost positions of the cube
            Vector3 leftmost = leftOrigin.position;
            Vector3 rightmost = rightOrigin.position;

            // Raycast downward from both positions with layerMask applied
            RaycastHit hitLeft;
            RaycastHit hitRight;

            bool leftHit = Physics.Raycast(leftmost, Vector3.down, out hitLeft, raycastDistance, layerMask);
            bool rightHit = Physics.Raycast(rightmost, Vector3.down, out hitRight, raycastDistance, layerMask);

            // If both raycasts hit a surface
            if (leftHit && rightHit)
            {
                // Calculate the difference in height between the left and right hit points
                float heightDifference = hitLeft.point.y - hitRight.point.y;

                // Calculate the direction vector from the left hit to the right hit
                Vector3 horizontalDifference = new Vector3(hitRight.point.x - hitLeft.point.x, 0, hitRight.point.z - hitLeft.point.z);

                // Calculate the rotation angle between the two hit points based on the horizontal difference and height difference
                float angle = Mathf.Atan2(heightDifference, horizontalDifference.magnitude) * Mathf.Rad2Deg;

                // Update the target lean angle
                targetLeanAngle = angle;
            }
        }
        else
        {
            // If the speed goes below the threshold, reset the targetLeanAngle to 0
            targetLeanAngle = 0f;
        }

        // Smoothly interpolate to the target angle
        if (Mathf.Abs(targetLeanAngle - currentLeanAngle) > 0.01f) // Check if a significant difference exists
        {
            currentLeanAngle = Mathf.LerpAngle(currentLeanAngle, targetLeanAngle, Time.deltaTime / lerpTime);
        }

        // Apply the rotation to the object
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, -currentLeanAngle);
    }
}
