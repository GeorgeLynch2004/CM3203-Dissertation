using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BicycleSmoothRotation : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float baseRotationSpeed = 5.0f;
    [SerializeField] private float minRotationSpeed = 1.0f;
    [SerializeField] private float lookAheadDistance = 2.0f; // Look ahead on path
    [SerializeField] private float cornerAnticipation = 5.0f; // How early to start turning

    private Vector3 smoothedDirection;

    private void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false; // Disable automatic rotation
        smoothedDirection = transform.forward;
    }

    private void Update()
    {
        if (agent.hasPath && agent.remainingDistance > 0.1f)
        {
            // Get a position further along the path to look ahead
            Vector3 targetPoint = FindLookAheadPoint();

            // Calculate direction to the look-ahead point
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                // Smooth the direction vector itself for more gradual changes
                direction.Normalize();
                smoothedDirection = Vector3.Slerp(smoothedDirection, direction, cornerAnticipation * Time.deltaTime);

                // Create rotation from smoothed direction
                Quaternion targetRotation = Quaternion.LookRotation(smoothedDirection);

                // Calculate rotation speed based on velocity but with a minimum
                float dynamicRotationSpeed = minRotationSpeed + baseRotationSpeed *
                                           (agent.velocity.magnitude / agent.speed);

                // Apply smoothed rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                                                     dynamicRotationSpeed * Time.deltaTime);
            }
        }
    }

    private Vector3 FindLookAheadPoint()
    {
        // Calculate how far ahead to look on the path
        float lookAheadDist = Mathf.Min(lookAheadDistance, agent.remainingDistance);

        if (lookAheadDist <= 0.1f)
            return agent.steeringTarget;

        // Get the corners from the agent's path
        Vector3[] corners = agent.path.corners;

        if (corners.Length < 2)
            return agent.steeringTarget;

        // Calculate the distance traveled along the path
        float distanceTraveled = 0f;
        Vector3 previousPoint = transform.position;

        // First, find which segment of the path we're currently on
        int currentSegment = 0;

        // Skip the first corner if it's behind us (common with NavMeshAgent paths)
        Vector3 dirToFirstCorner = corners[0] - transform.position;
        Vector3 forward = transform.forward;
        if (Vector3.Dot(dirToFirstCorner, forward) < 0 && corners.Length > 1)
        {
            previousPoint = corners[0];
            currentSegment = 1;
        }

        // Find the point that's lookAheadDist units along the path
        for (int i = currentSegment; i < corners.Length; i++)
        {
            Vector3 corner = corners[i];
            float segmentLength = Vector3.Distance(previousPoint, corner);

            if (distanceTraveled + segmentLength >= lookAheadDist)
            {
                // Interpolate to find the exact look-ahead point
                float t = (lookAheadDist - distanceTraveled) / segmentLength;
                return Vector3.Lerp(previousPoint, corner, t);
            }

            distanceTraveled += segmentLength;
            previousPoint = corner;
        }

        // If we can't look ahead far enough, return the last path point
        return corners[corners.Length - 1];
    }
}