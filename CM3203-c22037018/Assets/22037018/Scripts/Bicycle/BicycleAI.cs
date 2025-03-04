using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;
using System.Linq;
using Unity.VisualScripting;

public enum AIType
{
    Player, 
    Competitor,
    Teammate
}

public enum AIState
{
    Pulling,
    Drafting,
}

public class BicycleAI : MonoBehaviour
{
    #region variables

    // AI Type
    [SerializeField] private AIType aiType;
    [SerializeField] private AIState aiState;

    // AI Hardcoded Performance Profile
    private Dictionary<string, List<float>> profiles;

    // Navmesh
    [Header("Navmesh Settings")]
    [SerializeField] private NavMeshAgent navmeshAgent;
    [SerializeField] private Transform MapTargetPosition;
    [SerializeField] private Transform pacelineTargetPosition;
    [SerializeField] private float selfPathDistance;
    [SerializeField] private float targetsPathDistance;
    [SerializeField] public float desiredAcceleration;

    // Collisions
    [Header("Collision Settings")]
    [SerializeField] private Transform anticipationCollider;
    [SerializeField] private Transform bikeInfront;
    [SerializeField] private Transform bikeBehind;
    [SerializeField] private Transform anticipationCollision;

    // Player Object Reference
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Transform bodyParent;

    private SessionManager sessionManager;

    // Overtake Coroutine
    private Coroutine overtakeCoroutine;
    public Transform raycastOrigin;
    public LayerMask aiLayer;
    public float detectionDistance = 5f;

    // Path smoothing
    [Header("Path Smoothing Settings")]
    [SerializeField, Range(0,100)] private int smoothingFactor = 5;

    

    #endregion


    // Start is called before the first frame update
    void Start()
    {
        playerObject = GameObject.FindGameObjectsWithTag("AI")
                           .FirstOrDefault(obj => obj.GetComponent<BicycleAI>().aiType == AIType.Player);
        navmeshAgent = GetComponent<NavMeshAgent>();
        sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();
    }

    private void Update() 
    {
        // Baseline Game Loop
        if (sessionManager.GetCurrentScenarioMode() == ScenarioMode.Baseline)
        {
            TakePull(MapTargetPosition);
        }
        // Cooperative Game Loop
        if (sessionManager.GetCurrentScenarioMode() == ScenarioMode.Cooperative)
        {
            if (aiState == AIState.Pulling)
            {
                if (MapTargetPosition != null)
                    TakePull(MapTargetPosition);
            }
            else if (aiState == AIState.Drafting)
            {
                if (pacelineTargetPosition != null)
                    DraftTeammate(pacelineTargetPosition);
            }
        }
        // Competitive Game Loop
        if (sessionManager.GetCurrentScenarioMode() == ScenarioMode.Competitive)
        {
            // Generate performance profiles based on users previous performance in baseline and cooperative scenario.
            if (profiles == null)
            {
                profiles = LoadAIPerformanceProfiles();
            }

            // Permanently have the AI trained to the target on the map not an opponent as the goal is the same for all bikes: getting into first.
            if (MapTargetPosition != null)
            {
                TakePull(MapTargetPosition);
            }
        }

         
        
    }



    // AI Type: Player


    #region AI Type: Competitor

    // A method designed to fetch a list of wattage values for each second of the workout tailored to the athletes performance in the previous two scenarios.
    private Dictionary<string, List<float>> LoadAIPerformanceProfiles()
    { 
        DataManager dataManager = GameObject.FindAnyObjectByType<DataManager>();
        
        if (dataManager != null)
        {
            return dataManager.GenerateWorkoutProfiles(dataManager.participantID, dataManager.fileDirectory);
        }
        return null;
    }

    #endregion


    #region AI Type: Teammate

    public void TakePull(Transform target)
    {
        SetSmoothedDestination(target.position);
    }

    public void DraftTeammate(Transform target)
    {
        SetSmoothedDestination(target.position);

        selfPathDistance = GetPathDistance(navmeshAgent, transform.position);
        targetsPathDistance = GetPathDistance(navmeshAgent, pacelineTargetPosition.position);

        if (aiType == AIType.Teammate)
        {
            if (targetsPathDistance > 6f)
            {
                UpdatePace(pacelineTargetPosition.GetComponent<NavMeshAgent>().speed + 2);
            }
            else if (targetsPathDistance < 4f)
            {
                UpdatePace(pacelineTargetPosition.GetComponent<NavMeshAgent>().speed - 2);
            }
            else
            {
                UpdatePace(pacelineTargetPosition.GetComponent<NavMeshAgent>().speed);
            }
        }
        else if (aiType == AIType.Player)
        {
            if (targetsPathDistance < 4f)
            {
                UpdatePace(navmeshAgent.speed - 2);
            }
        }
        
    }

    public void PeelOffPaceLine()
    {
        StartCoroutine(PeelOffRoutine());
    }

    private IEnumerator PeelOffRoutine()
    {
        float peelOffDuration = 1.5f; // Time to move sideways
        float rejoinDuration = 1.5f; // Time to move back behind
        float elapsedTime = 0f;

        Transform rootTransform = transform; // Root reference
        Vector3 sideDirection = rootTransform.right.normalized; // Get right direction

        // // Step 1: Move bodyParent to the right (peel off)
        // while (elapsedTime < peelOffDuration)
        // {
        //     Vector3 newPosition = bodyParent.position + sideDirection * (.5f / peelOffDuration * Time.deltaTime);
        //     bodyParent.position = newPosition;

        //     elapsedTime += Time.deltaTime;
        //     yield return null;
        // }

        // Step 2: Reduce pace and move backward
        elapsedTime = 0f;
        while (elapsedTime < rejoinDuration)
        {
            if (pacelineTargetPosition != null) UpdatePace(pacelineTargetPosition.GetComponent<NavMeshAgent>().speed - 2);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Step 3: Wait until rejoining is possible (bikeInFront == pacelineTarget)
        while (bikeInfront != pacelineTargetPosition)
        {
            yield return null; // Keep checking every frame
        }

        // // Step 4: Move bodyParent back left to rejoin the paceline
        // elapsedTime = 0f;
        // while (elapsedTime < peelOffDuration)
        // {
        //     Vector3 newPosition = bodyParent.position - sideDirection * (2f / peelOffDuration * Time.deltaTime);
        //     bodyParent.position = newPosition;

        //     elapsedTime += Time.deltaTime;
        //     yield return null;
        // }
    }

    public void UpdatePacelinePosition(Transform target)
    {
        if (target == null)
        {
            aiState = AIState.Pulling;
            pacelineTargetPosition = null;
        }
        else
        {
            aiState = AIState.Drafting;
            pacelineTargetPosition = target;
        }
    }

    #endregion

    // AI Type: All

    public void UpdatePace(float speed)
    {
        if (navmeshAgent != null)
        {
            navmeshAgent.speed = speed;
        }
    }

    #region Path Smoothing

    private void SetSmoothedDestination(Vector3 pos)
    {
        NavMeshPath path = new NavMeshPath();
        
        if (!navmeshAgent.CalculatePath(pos, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning("Invalid or incomplete path! Path status: " + path.status);
            return;
        }

        List<Vector3> smoothed = SmoothPathWithCatmullRom(path);
        
        if (smoothed.Count > 1)
        {
            FollowSmoothedPath(smoothed);
        }
        else
        {
            Debug.LogWarning("Smoothed path is empty! Unable to navigate.");
        }
    }

    private void EnsureAgentOnNavMesh()
    {
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            Debug.LogError("Agent is not on the NavMesh!");
            return;
        }

        transform.position = hit.position;
        navmeshAgent.Warp(hit.position);
    }


    private List<Vector3> SmoothPath(NavMeshPath path)
    {
        List<Vector3> smoothedPath = new List<Vector3>();
        Vector3[] corners = path.corners;
        
        if (corners.Length < 2)
        {
            return smoothedPath;
        }

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 p0 = corners[Mathf.Max(i - 1, 0)];
            Vector3 p1 = corners[i];
            Vector3 p2 = corners[i + 1];
            Vector3 p3 = corners[Mathf.Min(i + 2, corners.Length - 1)];

            for (int tIndex = 0; tIndex <= smoothingFactor; tIndex++)
            {
                float t = tIndex / (float)smoothingFactor;
                Vector3 point = CalculateBezierPoint(t, p0, p1, p2, p3);
                
                if (NavMesh.SamplePosition(point, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                {
                    smoothedPath.Add(hit.position);
                }
            }
        }

        return smoothedPath;
    }

    private List<Vector3> SmoothPathWithCatmullRom(NavMeshPath path)
    {
        List<Vector3> smoothedPath = new List<Vector3>();
        Vector3[] corners = path.corners;

        if (corners.Length < 2)
        {
            Debug.LogWarning("Path has less than 2 corners, cannot smooth.");
            return smoothedPath;
        }

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 p0 = corners[Mathf.Max(i - 1, 0)];
            Vector3 p1 = corners[i];
            Vector3 p2 = corners[i + 1];
            Vector3 p3 = corners[Mathf.Min(i + 2, corners.Length - 1)];

            for (int tIndex = 0; tIndex <= smoothingFactor; tIndex++)
            {
                float t = tIndex / (float)smoothingFactor;
                Vector3 point = CalculateCatmullRomPoint(t, p0, p1, p2, p3);

                if (NavMesh.SamplePosition(point, out NavMeshHit hit, 2.0f, NavMesh.AllAreas)) // Increased radius to 2.0f
                {
                    smoothedPath.Add(hit.position);
                }
                else
                {
                    Debug.LogWarning("Failed to sample position on NavMesh for point: " + point);
                }
            }
        }

        return smoothedPath;
    }

    private Vector3 CalculateCatmullRomPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        float a0 = -0.5f * t3 + t2 - 0.5f * t;
        float a1 = 1.5f * t3 - 2.5f * t2 + 1.0f;
        float a2 = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
        float a3 = 0.5f * t3 - 0.5f * t2;

        return a0 * p0 + a1 * p1 + a2 * p2 + a3 * p3;
    }

    private void FollowSmoothedPath(List<Vector3> pathPoints)
    {
        StartCoroutine(FollowPathCoroutine(pathPoints));
    }

    private IEnumerator FollowPathCoroutine(List<Vector3> pathPoints)
    {
        foreach (Vector3 point in pathPoints)
        {
            navmeshAgent.SetDestination(point);
            float timeout = 5f; // Max time to wait per waypoint
            float timer = 0f;

            while (!navmeshAgent.pathPending && 
                navmeshAgent.remainingDistance > navmeshAgent.stoppingDistance && 
                timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }


    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        return (uuu * p0) + (3 * uu * t * p1) + (3 * u * tt * p2) + (ttt * p3);
    }

    #endregion

    #region Overtaking

    public void Overtake()
    {
        if ((IsBicycleInPath() || anticipationCollision != null) && overtakeCoroutine == null)
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

    private List<Transform> GetBicyclesInPath()
    {
        List<Transform> bicycles = new List<Transform>();
        RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward, detectionDistance, aiLayer);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Bicycle"))
            {
                bicycles.Add(hit.transform);
            }
        }

        return bicycles;
    }

    private bool AreAnyBicyclesStillAhead(List<Transform> bicycles)
    {
        foreach (Transform bicycle in bicycles)
        {
            if (bicycle == null) continue; // Skip destroyed bicycles

            Vector3 aiVelocity = navmeshAgent.velocity.normalized; // AI’s movement direction
            Vector3 bicycleVelocity = bicycle.GetComponent<NavMeshAgent>().velocity.normalized; // Bicycle’s movement direction

            Vector3 aiToBicycle = bicycle.position - transform.position;

            // Project AI-to-Bicycle vector onto movement direction
            float relativePosition = Vector3.Dot(aiToBicycle, aiVelocity);

            if (relativePosition > 0) // If the bicycle is still ahead in AI’s movement direction
            {
                return true;
            }
        }

        return false; // All overtaken bicycles are now behind
    }

    private IEnumerator OvertakeRoutine()
    {
        float duration = 1.5f; // Duration to move sideways
        float elapsedTime = 0f;
        Vector3 startOffset = transform.right * 2f; // Move 2 units to the right
        Vector3 startPosition = navmeshAgent.transform.position;
        Vector3 targetPosition = startPosition + startOffset;

        // Detect all bicycles currently in the path at the start of overtaking
        List<Transform> overtakenBicycles = GetBicyclesInPath();

        // Move sideways to overtake
        while (elapsedTime < duration)
        {
            Vector3 currentBasePosition = navmeshAgent.transform.position;
            Vector3 newPosition = Vector3.Lerp(currentBasePosition, currentBasePosition + startOffset, Time.deltaTime / duration);
            navmeshAgent.transform.position = newPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Wait until the AI has fully overtaken all tracked bicycles
        while (AreAnyBicyclesStillAhead(overtakenBicycles))
        {
            yield return null; // Keep waiting
        }

        // Move back to original path
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            Vector3 currentBasePosition = navmeshAgent.transform.position;
            Vector3 newPosition = Vector3.Lerp(currentBasePosition, currentBasePosition - startOffset, Time.deltaTime / duration);
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

    #region Getters and Setters

    private float GetPathDistance(NavMeshAgent agent, Vector3 position)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(position, path);

        float totalDistance = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            totalDistance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return totalDistance;
    }


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

    public void updateMapTargetPosition(Transform destination)
    {
        MapTargetPosition = destination;
    }

    public Transform getMapTargetPosition()
    {
        return MapTargetPosition;
    }


    public AIType GetAIType()
    {
        return aiType;
    }

    public void SetAIState(AIState state)
    {
        aiState = state;
    }

    public AIState GetAIState(AIState state)
    {
        return aiState;
    }


    #endregion
}
