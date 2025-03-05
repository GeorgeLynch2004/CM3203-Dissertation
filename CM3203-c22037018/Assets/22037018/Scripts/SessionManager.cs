using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

public enum ScenarioMode
{
    Undecided,
    Baseline,
    Cooperative,
    Competitive
}

public enum ResistanceProfile
{
    Consistent,
    Ramp,
}

public class SessionManager : MonoBehaviour
{
    [Header("Scenario Settings")]
    [SerializeField] private ScenarioMode scenarioMode;
    private ScenarioMode previousFrameScenarioMode;
    [SerializeField] private ResistanceProfile resistanceProfile;

    [Header("XR Settings")]
    [SerializeField] private GameObject XROrigin;
    [SerializeField] private Transform spawnPose;
    [SerializeField] private Transform bikePose;
    private bool lerping;
    private XRInputSubsystem xrInputSubsystem;

    [SerializeField] private List<GameObject> activeAI = new List<GameObject>();

    // Cooperative Mode: Paceline Rotation
    [Header("Paceline Settings")]
    [SerializeField] private float pullTime; // Time each AI spends at the front
    private Coroutine pacelineCoroutine;

    [Header("Race Settings")]
    [SerializeField] private List<float> aiPerformanceVariations;
    private Coroutine raceCoroutine;

    [Header("Ramp Test Settings")]
    [SerializeField] private float warmupDuration;
    [SerializeField] private float rampDuration = 60; // 1 minute
    [SerializeField] private float rampIncrement = 0.1f; // 10% every 10 seconds

    private DataManager dataManager;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        lerping = false;
        dataManager = GameObject.FindAnyObjectByType<DataManager>();
    }

    private void Update()
    {
        HandleScenarioModeChange();
    }

    public void BeginSession()
    {
        InitBikeObjects();

        if (resistanceProfile != ResistanceProfile.Consistent)
        {
            // for if resistance is wanted to stay the same
        }

        // Scenario Mode Logic
        if (scenarioMode != ScenarioMode.Baseline) 
        {
            // baseline logic here    
        }
        if (scenarioMode == ScenarioMode.Cooperative && activeAI.Count > 1)
        {
            pacelineCoroutine = StartCoroutine(PacelineRoutine());
        }
        if (scenarioMode == ScenarioMode.Competitive && activeAI.Count > 1)
        {
            raceCoroutine = StartCoroutine(RaceCoroutine(aiPerformanceVariations, dataManager.GenerateWorkoutProfiles(dataManager.participantID, Path.Combine(Application.dataPath, "Performance Logs"))));
        }
    }

    private void InitBikeObjects()
    {
        activeAI = GameObject.FindGameObjectsWithTag("AI")
            .Where(ai =>
                (scenarioMode == ScenarioMode.Cooperative && (ai.GetComponent<BicycleAI>().GetAIType() == AIType.Teammate || ai.GetComponent<BicycleAI>().GetAIType() == AIType.Player)) ||
                (scenarioMode == ScenarioMode.Competitive && (ai.GetComponent<BicycleAI>().GetAIType() == AIType.Competitor || ai.GetComponent<BicycleAI>().GetAIType() == AIType.Player)))
            .OrderBy(ai => Vector3.Distance(ai.transform.position, Vector3.zero))
            .ToList();


    }

    #region Developer Menu

    // Method to recenter the headset
    // Method to get the running XR Input Subsystem
    // Method to get the running XR Input Subsystem
    private XRInputSubsystem GetXRInputSubsystem()
    {
        List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();

        // Get all the running XR input subsystems (this will give us a list of subsystems)
        SubsystemManager.GetInstances(inputSubsystems);

        // Log all available subsystems for debugging purposes
        if (inputSubsystems.Count == 0)
        {
            Debug.LogError("No XRInputSubsystems found.");
        }
        else
        {
            foreach (var subsystem in inputSubsystems)
            {
                Debug.Log("Found XRInputSubsystem: " + subsystem);
            }
        }

        // Find the first running XRInputSubsystem
        foreach (var subsystem in inputSubsystems)
        {
            if (subsystem.running)
            {
                Debug.Log("Using XRInputSubsystem: " + subsystem);
                return subsystem;
            }
        }

        return null;  // Return null if no running subsystem found
    }

    void RecenterXR()
    {
        // Get the active XR Input Subsystem
        xrInputSubsystem = GetXRInputSubsystem();


        if (xrInputSubsystem != null)
        {
            

            // Recenter the headset
            bool result = xrInputSubsystem.TryRecenter();
            Debug.Log("Trying to recenter device." + result);
        }
        else
        {
            Debug.LogWarning("XRInputSubsystem is null, recentering failed.");
        }
    }


    #endregion

    #region Scenario Modes

    public void SetBaselineMode()
    {
        scenarioMode = ScenarioMode.Baseline;
    }

    public void SetCooperativeMode()
    {
        scenarioMode = ScenarioMode.Cooperative;
    }

    public void SetCompetitiveMode()
    {
        scenarioMode = ScenarioMode.Competitive;
    }

    public ScenarioMode GetCurrentScenarioMode()
    {
        return scenarioMode;
    }

    private void HandleScenarioModeChange()
    {
        if (scenarioMode != previousFrameScenarioMode)
        {
            if (scenarioMode == ScenarioMode.Undecided && XROrigin.transform.GetWorldPose() != spawnPose.GetWorldPose())
            {
                if (lerping == false) MoveToPose(XROrigin.transform, spawnPose, true);
            }
            else if (scenarioMode != ScenarioMode.Undecided && XROrigin.transform.GetWorldPose() != bikePose.GetWorldPose())
            {
                if (lerping == false) MoveToPose(XROrigin.transform, bikePose, true);
            }
        }

        previousFrameScenarioMode = scenarioMode;
    }

    #endregion

    #region Manipulating XR Origin


    public void MoveToPose(Transform objToMove, Transform destination, bool setParent)
    {
        if (setParent)
        {
            objToMove.SetParent(destination, true);
        }

        StartCoroutine(LerpToPose(objToMove, destination, 2f));
    }

    private IEnumerator LerpToPose(Transform objToMove, Transform destination, float duration)
    {
        Vector3 startPosition = objToMove.position;
        Quaternion startRotation = objToMove.rotation;
        Vector3 endPosition = destination.position;
        Quaternion endRotation = destination.rotation;

        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            lerping = true;
            // Interpolate the position and rotation over time
            objToMove.position = Vector3.Lerp(startPosition, endPosition, timeElapsed / duration);
            objToMove.rotation = Quaternion.Lerp(startRotation, endRotation, timeElapsed / duration);

            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        // Ensure the object ends exactly at the destination pose
        objToMove.position = endPosition;
        objToMove.rotation = endRotation;
        lerping = false;
    }


    #endregion

    #region Paceline

    private IEnumerator PacelineRoutine()
    {
        while (true)
        {
            UpdatePacelinePositions();
            yield return new WaitForSeconds(pullTime);
            RotatePaceline();
        }
    }

    private void RotatePaceline()
    {
        if (activeAI.Count < 2) return; // No need to rotate if 0 or 1 AI

        // Move the front AI to the back
        GameObject firstAI = activeAI[0];
        activeAI.RemoveAt(0);
        activeAI.Add(firstAI);

        firstAI.GetComponent<BicycleAI>().PeelOffPaceLine();
        // Update AI roles in paceline
        UpdatePacelinePositions();
    }

    private void UpdatePacelinePositions()
    {
        for (int i = 0; i < activeAI.Count; i++)
        {
            if (i == 0)
            {
                // First AI is now leading the paceline
                activeAI[i].GetComponent<BicycleAI>().SetAIState(AIState.Pulling);
                activeAI[i].GetComponent<BicycleAI>().UpdatePacelinePosition(null);
            }
            else
            {
                // Other AI follow the one in front
                activeAI[i].GetComponent<BicycleAI>().SetAIState(AIState.Drafting);
                activeAI[i].GetComponent<BicycleAI>().UpdatePacelinePosition(activeAI[i-1].transform);
            }
        }
    }

    #endregion

    #region Race
    
    // this method
    private IEnumerator RaceCoroutine(List<float> performanceVariations, Dictionary<string, List<float>> performanceProfiles)
    {
        int second = 0;

        List<BicycleAI> enemies = FindObjectsOfType<BicycleAI>()
        .Where(ai => ai.GetAIType() == AIType.Competitor)
        .ToList();


        Debug.Log(performanceVariations.Count);
        Debug.Log(enemies.Count);

        if  (performanceVariations.Count != enemies.Count)
        {
            Debug.LogError("Incorrect performance variations quantity.");
            yield return null;
        }
        else if (performanceProfiles == null)
        {
            Debug.LogError("Failed to load performance profiles.");
            yield return null;
        }
        else
        {
            List<float> powerData = performanceProfiles["Power"];
            List<float> heartrateData = performanceProfiles["Heartrate"];

            List<float> gracePeriod = new List<float> { 100f, 100f, 100f, 100f, 100f };
            powerData.InsertRange(0, gracePeriod);

            while (second < powerData.Count) 
            {
                float power = powerData[second];
                float heartrate = heartrateData[second];

                for (int i = 0; i < enemies.Count; i++)
                {
                    float adjustedPower = power * (1 + performanceVariations[i] / 100f);

                    if (heartrate > 0)
                    {
                        // calculate adjustments to be made based on hr data.
                        float heartrateDifference = dataManager.currentHeartRate - heartrate;
                        float percentageDifference = heartrateDifference / heartrate * 100f;

                        if (heartrateDifference > 0)
                        {
                            adjustedPower -= adjustedPower * (percentageDifference / 100f);
                        }
                        else if (heartrateDifference < 0)
                        {
                            adjustedPower += adjustedPower * (Mathf.Abs(percentageDifference) / 100f);
                        }
                    }
                    

                    float speed = dataManager.CalculateSpeed(adjustedPower, 0.88f, 0.5f, 0.004f, 75f);

                    Debug.Log(speed);

                    // if the speed calculated is 0 that will cause the ai to grind to a half which we dont want
                    if (speed > 0)
                    {
                        enemies[i].UpdatePace(speed);
                    }
                    
                }

                second++;
                yield return new WaitForSeconds(1f);
            }
        }

        
    }

    #endregion
}
