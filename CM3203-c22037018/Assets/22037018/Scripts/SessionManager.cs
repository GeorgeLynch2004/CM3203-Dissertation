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

public class SessionManager : MonoBehaviour
{
    [SerializeField] private ScenarioMode scenarioMode;

    [SerializeField] private List<GameObject> activeAI = new List<GameObject>();

    // Cooperative Mode: Paceline Rotation
    [SerializeField] private float pullTime; // Time each AI spends at the front
    private Coroutine pacelineCoroutine;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        BeginSession();
    }

    private void BeginSession()
    {
        Init();

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
}
