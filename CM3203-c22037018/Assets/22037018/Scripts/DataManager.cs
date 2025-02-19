using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    [SerializeField] private bool dataLoggingEnabled;
    private string fileDirectory;
    [SerializeField] private string fileName;
    private StreamWriter writer;

    [Header("Performance Metrics")]
    [SerializeField] private float currentPower; 
    [SerializeField] private float targetPower; 
    [SerializeField] private float currentCadence; 
    [SerializeField] private float targetCadence; 
    [SerializeField] private float currentSpeed; 
    [SerializeField] private float targetSpeed; 
    [SerializeField] private float currentHeartRate; 
    [SerializeField] private float targetHeartRate; 

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);

        fileDirectory = Application.persistentDataPath + "/" + fileName + ".csv";
        writer = new StreamWriter(fileDirectory, false);

        writer.WriteLine("Time,CurrentPower,TargetPower,CurrentCadence,TargetCadence,CurrentSpeed,TargetSpeed,CurrentHeartRate,TargetHeartRate");
        writer.Flush();
    }

    // Update is called once per frame
    void Update()
    {
        if (dataLoggingEnabled) LogData();


    }

    private void LogData()
    {
        float timeStamp = Time.time;
        string logEntry = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
            timeStamp, currentPower, targetPower, currentCadence, targetCadence, 
            currentSpeed, targetSpeed, currentHeartRate, targetHeartRate);
        
        writer.WriteLine(logEntry);
    }

    private void OnApplicationQuit() {
        writer.Close();
    }

}
