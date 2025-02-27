using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    // Current Data
    [Header("Current Data")]
    [SerializeField] public TextMeshProUGUI currentPowerData;
    [SerializeField] public TextMeshProUGUI currentHeartrateData;
    [SerializeField] public TextMeshProUGUI currentCadenceData;
    [SerializeField] public TextMeshProUGUI currentSpeedData;
    [SerializeField] public TextMeshProUGUI currentDurationData;

    // Developer Information
    [Header("Developer Information")]
    [SerializeField] public TextMeshProUGUI dataLoggingFlag;
    [SerializeField] public TextMeshProUGUI participantID;
    [SerializeField] public TextMeshProUGUI selectedScenarioMode;
    [SerializeField] public TextMeshProUGUI dateAndTime;
    [SerializeField] public TextMeshProUGUI messagePopUp;

    public void UpdateText(TextMeshProUGUI txt, string val)
    {
        txt.text = val;
    }
}
