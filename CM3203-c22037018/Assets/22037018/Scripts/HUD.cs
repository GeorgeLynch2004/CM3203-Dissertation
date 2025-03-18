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

    [Header("Data Parent")]
    [SerializeField] private GameObject dataParent;

    [Header("VR Camera")]
    [SerializeField] private Camera cameraToLookAt;



    public void UpdateText(TextMeshProUGUI txt, string val)
    {
        txt.text = val;
    }

    private void Update()
    {
        transform.LookAt(cameraToLookAt.transform);
    }

    public void ToggleHideData()
    {
        if (dataParent == null)
        {
            Debug.LogWarning("Data parent not assigned in editor");
            return;
        }
        dataParent.SetActive(!dataParent.activeSelf);
    }

    public void ReturnToTutorialArea()
    {
        SessionManager.Instance.SetUndecidedMode();
    }
}
