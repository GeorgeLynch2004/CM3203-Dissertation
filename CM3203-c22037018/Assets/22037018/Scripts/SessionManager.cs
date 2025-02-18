using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ScenarioMode
{
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
    [SerializeField] private ScenarioMode scenarioMode;
    [SerializeField] private ResistanceProfile resistanceProfile;

    [SerializeField] private List<GameObject> activeAI = new List<GameObject>();

    // Cooperative Mode: Paceline Rotation
    [SerializeField] private float pullTime; // Time each AI spends at the front
    private Coroutine pacelineCoroutine;

    [Header("Ramp Test Settings")]
    [SerializeField] private float warmupDuration;
    [SerializeField] private float rampDuration = 60; // 1 minute
    [SerializeField] private float rampIncrement = 0.1f; // 10% every 10 seconds

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        BeginSession();
    }

    private void BeginSession()
    {
        Init();

        if (resistanceProfile == ResistanceProfile.Ramp)
        {
            StartCoroutine(RampTest());
        }

        if (scenarioMode == ScenarioMode.Cooperative && activeAI.Count > 1)
        {
            pacelineCoroutine = StartCoroutine(PacelineRoutine());
        }
    }

    private void Init()
    {
        activeAI = GameObject.FindGameObjectsWithTag("AI")
            .Where(ai =>
                (scenarioMode == ScenarioMode.Cooperative && ai.GetComponent<BicycleAI>().GetAIType() == AIType.Teammate) ||
                (scenarioMode == ScenarioMode.Competitive && ai.GetComponent<BicycleAI>().GetAIType() == AIType.Competitor))
            .OrderBy(ai => Vector3.Distance(ai.transform.position, Vector3.zero))
            .ToList();
    }

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

    #region Ramp Test

    private IEnumerator RampTest()
    {
        float rampTime = 0;

        while (rampTime < rampDuration)
        {
            rampTime += Time.deltaTime;
            yield return new WaitForSeconds(10);

            foreach (GameObject ai in activeAI)
            {
                ai.GetComponent<UnityEngine.AI.NavMeshAgent>().speed = CalculateSpeed(0,0,0);
            }
        }
    }

    #endregion

    #region Data Handling

    public float CalculateSpeed(float cadence, float resistance, float power)
    {
        // Example formula: Speed estimation for a Wattbike (adjust as needed)
        float speed = (cadence * 0.1f) + (resistance * 0.2f) + (power * 0.05f);
        return speed;
    }

    public void IncreaseResistance(float resistance)
    {
        // method to increase resistance
    }

    #endregion
}
