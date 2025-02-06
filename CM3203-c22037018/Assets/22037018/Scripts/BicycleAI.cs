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

    // Collisions
    [SerializeField] private Transform bikeInfront;
    [SerializeField] private Transform bikeBehind;

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
        if (IsAIInPath() && overtakeCoroutine == null)
        {
            overtakeCoroutine = StartCoroutine(OvertakeRoutine());
        }
    }

    private bool IsAIInPath()
    {
        if (navmeshAgent.path == null || navmeshAgent.path.corners.Length < 2)
            return false;

        Vector3[] pathCorners = navmeshAgent.path.corners;
        for (int i = 0; i < pathCorners.Length - 1; i++)
        {
            Vector3 direction = (pathCorners[i + 1] - pathCorners[i]).normalized;
            float segmentLength = Vector3.Distance(pathCorners[i], pathCorners[i + 1]);
            if (Physics.Raycast(pathCorners[i], direction, out RaycastHit hit, segmentLength, aiLayer))
            {
                if (hit.collider.CompareTag("AI"))
                {
                    return true;
                }
            }
        }
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
            Vector3[] pathCorners = navmeshAgent.path.corners;
            for (int i = 0; i < pathCorners.Length - 1; i++)
            {
                Gizmos.DrawLine(pathCorners[i], pathCorners[i + 1]);
                Gizmos.color = Color.black;
                Gizmos.DrawRay(pathCorners[i], (pathCorners[i + 1] - pathCorners[i]).normalized * Vector3.Distance(pathCorners[i], pathCorners[i + 1]));
            }
        }
    }


    #endregion


    public void SetCollisionFrontRear(Transform obj, bool frontFlag)
    {
        if (frontFlag)
        {
            bikeInfront = obj;
        }
        else
        {
            bikeBehind = obj;
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
