using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    [SerializeField] private bool dataLoggingEnabled;


    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (!dataLoggingEnabled) return;


    }

    private bool getDataLoggingEnabled()
    {
        return dataLoggingEnabled;
    }

    private void setDataLoggingEnabled(bool state)
    {
        dataLoggingEnabled = state;
    }

    private void LogPower()
    {
        // get current power from player

        // get target power from player

        // combine with timestamp and add to array
    }

    private void LogHeartrate()
    {
        // get current hr from player

        // get target hr from player

        // combine with timestamp and add to array
    }

    private void LogCadence()
    {
        // get current cadence from player

        // get target cadence from player

        // combine with timestamp and add to array
    }
}
