using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    // Current Data
    [SerializeField] public TextMeshProUGUI currentPowerData;
    [SerializeField] public TextMeshProUGUI currentHeartrateData;
    [SerializeField] public TextMeshProUGUI currentCadenceData;
    [SerializeField] public TextMeshProUGUI currentSpeedData;

    // Target Data
    [SerializeField] public TextMeshProUGUI targetPowerData;
    [SerializeField] public TextMeshProUGUI targetHeartrateData;
    [SerializeField] public TextMeshProUGUI targetCadenceData;
    [SerializeField] public TextMeshProUGUI targetSpeedData;

    public void UpdateText(TextMeshProUGUI txt, string val)
    {
        txt.text = val;
    }
}
