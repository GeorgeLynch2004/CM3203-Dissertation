using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;

public class DataManager : MonoBehaviour
{
    [SerializeField] private bool dataLoggingEnabled;
    private string fileDirectory;
    [SerializeField] private string fileName;
    private StreamWriter writer;

    [Header("Player Reference")]
    [SerializeField] private BicycleAI player;

    [Header("Exercise Bike Script Reference")]
    [SerializeField] private FTMS_UI exerciseBikeScript;

    [Header("Performance Metrics")]
    [SerializeField] public float currentPower; 
    [SerializeField] public float targetPower; 
    [SerializeField] public float currentCadence; 
    [SerializeField] public float targetCadence; 
    [SerializeField] public float currentSpeed; 
    [SerializeField] public float targetSpeed; 
    [SerializeField] public float currentHeartRate; 
    [SerializeField] public float targetHeartRate;

    [Header("Heads Up Display (HUD)")]
    [SerializeField] private HUD headsUpDisplay;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);

        fileDirectory = Application.persistentDataPath + "/" + fileName + ".csv";
        writer = new StreamWriter(fileDirectory, false);

        writer.WriteLine("Time,CurrentPower,TargetPower,CurrentCadence,TargetCadence,CurrentSpeed,TargetSpeed,CurrentHeartRate,TargetHeartRate");
        writer.Flush();

        EstablishDeviceConnections();
    }

    // Update is called once per frame
    void Update()
    {
        if (dataLoggingEnabled) LogData();

    }

    private void EstablishDeviceConnections()
    {
        // Exercise bike.
        exerciseBikeScript = GameObject.Find("FTMS_UI").GetComponent<FTMS_UI>();

        if (exerciseBikeScript != null)
        {
            // try to connect exercise bike automatically.
            exerciseBikeScript.connect();
        }

        // Heartrate Monitor
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

    public void ProcessDataFromBike(string info)
    {
        string[] strings = info.Split(new char[] {',', ' '}, System.StringSplitOptions.RemoveEmptyEntries);

        strings[1] = strings[1].Replace("RPM:", "");
        strings[3] = strings[3].Replace("Power", "");

        // Update HUD
        headsUpDisplay.UpdateText(headsUpDisplay.currentSpeedData, "Speed: " + strings[1]);
        headsUpDisplay.UpdateText(headsUpDisplay.currentCadenceData, "Cadence: " + strings[3]);

 

        // update player pace
        player.UpdatePace(float.Parse(strings[1]));
    }
}
