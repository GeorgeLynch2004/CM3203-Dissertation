using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

public enum AIType
{
    Player,
    Competitor,
    Teammate
}

public class BicycleAI : MonoBehaviour
{
    // AI Type
    [SerializeField] private AIType aiType;

    // Events
    [SerializeField] private UnityEvent currentAIDirective;
    [SerializeField] private UnityEvent neutralActions;
    [SerializeField] private UnityEvent offenceActions;
    [SerializeField] private UnityEvent defenceActions;

    // Navmesh
    [SerializeField] private NavMeshAgent navmeshAgent;
    [SerializeField] private Transform carrotOnTheStick;
    [SerializeField] private Transform anticipationCollider;

    // Collisions
    [SerializeField] private Transform bikeInfront;
    [SerializeField] private Transform bikeBehind;
    [SerializeField] private Transform anticipationCollision;

    // Player Object Reference
    [SerializeField] private GameObject playerObject;

    // Overtake Coroutine
    private Coroutine overtakeCoroutine;
    public Transform raycastOrigin;
    public LayerMask aiLayer;
    public float detectionDistance = 5f;


    // Start is called before the first frame update
    void Start()
    {
        playerObject = GameObject.FindWithTag("Player");
        navmeshAgent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        navmeshAgent.SetDestination(carrotOnTheStick.position);
        currentAIDirective.Invoke();
    }


    // AI Type: Player
    

    // AI Type: Competitor
 

    // AI Type: Teammate

    private void DraftTeammate()
    {

    }

    private void PaceLineSequence()
    {

    }


    // AI Type: All

    private void UpdatePace(float speed)
    {
        if (navmeshAgent != null)
        {
            navmeshAgent.speed = speed;
        }
    }

    #region Overtaking

    public void Overtake()
    {
        if (IsBicycleInPath() && overtakeCoroutine == null)
        {
            overtakeCoroutine = StartCoroutine(OvertakeRoutine());
        }
    }

    private bool IsBicycleInPath()
    {
        float raycastDistance = detectionDistance; // Adjust this based on how far ahead you want to check.

        if (navmeshAgent.path == null || navmeshAgent.path.corners.Length < 2)
            return false;

        Vector3[] pathCorners = navmeshAgent.path.corners;
        Vector3 lastPoint = pathCorners[0];
        
        for (int i = 0; i < pathCorners.Length - 1; i++)
        {
            Vector3 direction = (pathCorners[i + 1] - pathCorners[i]).normalized;
            float segmentLength = Vector3.Distance(pathCorners[i], pathCorners[i + 1]);

            // Ensure we only check within the defined raycastDistance
            if (segmentLength > raycastDistance)
            {
                segmentLength = raycastDistance;
            }

            if (Physics.Raycast(pathCorners[i], direction, out RaycastHit hit, segmentLength, aiLayer))
            {
                if (hit.collider.CompareTag("Bicycle"))
                {
                    UpdateCollisionAnticipationColliderPosition(hit.point);
                    return true;
                }
            }
            
            lastPoint = pathCorners[i] + direction * segmentLength;
            raycastDistance -= segmentLength;
            if (raycastDistance <= 0)
            {
                break;
            }
        }
        
        // If no hit, set anticipation collider to end of raycast
        UpdateCollisionAnticipationColliderPosition(lastPoint);
        return false;
    }

    private IEnumerator OvertakeRoutine()
    {
        float duration = 1.5f; // Duration of the overtake maneuver
        float elapsedTime = 0f;
        Vector3 startOffset = Vector3.right * 2f; // Move 2 units to the right
        Vector3 startPosition = navmeshAgent.transform.position;
        Vector3 targetPosition = startPosition + startOffset;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            Vector3 currentBasePosition = navmeshAgent.transform.position;
            Vector3 newPosition = Vector3.Lerp(currentBasePosition, currentBasePosition + startOffset, Time.deltaTime / duration);
            navmeshAgent.transform.position = newPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        overtakeCoroutine = null;
    }

    private void OnDrawGizmos()
    {
        if (navmeshAgent != null && navmeshAgent.path != null && navmeshAgent.path.corners.Length > 1)
        {
            Gizmos.color = Color.black;
            float raycastDistance = detectionDistance; // Same distance as used in the IsBicycleInPath method
            Vector3[] pathCorners = navmeshAgent.path.corners;

            for (int i = 0; i < pathCorners.Length - 1 && raycastDistance > 0; i++)
            {
                Vector3 direction = (pathCorners[i + 1] - pathCorners[i]).normalized;
                float segmentLength = Vector3.Distance(pathCorners[i], pathCorners[i + 1]);

                if (segmentLength > raycastDistance)
                {
                    segmentLength = raycastDistance;
                }

                Gizmos.DrawRay(pathCorners[i], direction * segmentLength);

                raycastDistance -= segmentLength;
            }
        }
    }

    private void UpdateCollisionAnticipationColliderPosition(Vector3 pos)
    {
        if (anticipationCollider != null)
        {
            anticipationCollider.position = pos;
        }
    }

    #endregion





    public void SetCollision(Transform obj, ColliderType colType)
    {
        if (colType == ColliderType.front)
        {
            bikeInfront = obj;
        }
        else if (colType == ColliderType.back)
        {
            bikeBehind = obj;
        }
        else if (colType == ColliderType.anticipation)
        {
            anticipationCollision = obj;
        }
    }

    public void updateTargetPosition(Transform destination)
    {
        carrotOnTheStick = destination;
    }

    public Transform getTargetPosition()
    {
        return carrotOnTheStick;
    }
}
