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
}
