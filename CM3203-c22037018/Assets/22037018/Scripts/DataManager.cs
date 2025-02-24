using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;
using System.Text.RegularExpressions;

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

    [Header("Heartrate Monitor Script Reference")]
    [SerializeField] private HeartrateScanner heartrateScript;

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

    // Bicycle physics values
    float dragCoefficient = 0.88f; // Drag coefficient for a cyclist (varies by position)
    float frontalArea = 0.5f; // Frontal area of the cyclist + bike in square meters
    float rollingResistanceCoefficient = 0.004f; // Rolling resistance coefficient
    float bikeMass = 75f; // Combined mass of cyclist and bike in kg

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

    private void EstablishDeviceConnections()
    {
        // Exercise bike.
        exerciseBikeScript = GameObject.Find("FTMS_UI").GetComponent<FTMS_UI>();

        if (exerciseBikeScript != null)
        {
            // try to connect exercise bike automatically.
            exerciseBikeScript.connect();
        }

        // Heartrate monitor.
        heartrateScript = GameObject.Find("HeartrateMonitor").GetComponent<HeartrateScanner>();


        if (heartrateScript != null)
        {
            heartrateScript.connect();
        }
            

        
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
        if (info.Length == 0) return;

        string[] strings = info.Split(new char[] {',', ' '}, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < strings.Length; i++)
        {
            strings[i] = Regex.Replace(strings[i], @"[^\d.]", "");
            if (i == 1)
            {
                headsUpDisplay.UpdateText(headsUpDisplay.currentSpeedData, "Speed: " + CalculateSpeed(float.Parse(strings[5]), dragCoefficient, frontalArea, rollingResistanceCoefficient, bikeMass) + "MPH");
            }
            if (i == 3) 
            {
                headsUpDisplay.UpdateText(headsUpDisplay.currentCadenceData, "Cadence: " + strings[i].Replace("\n", "") + "RPM");
            }
            if (i == 5)
            {
                headsUpDisplay.UpdateText(headsUpDisplay.currentPowerData, "Power: " + strings[i] + "W");
            }

            
        }

        float rawPower = float.Parse(strings[5]);

        // update player pace
        player.UpdatePace(CalculateSpeed(rawPower, dragCoefficient, frontalArea, rollingResistanceCoefficient, bikeMass));
    }

    public void ProcessDataFromHR(string info)
    {
        headsUpDisplay.UpdateText(headsUpDisplay.currentHeartrateData, "Heartrate: " + info + "BPM");
    }

    private float CalculateSpeed(float powerOutput, float dragCoefficient, float frontalArea, float rollingResistanceCoefficient, float bikeMass)
    {
        // Constants
        const float g = 9.81f;          // Gravitational acceleration (m/s²)
        const float rho = 1.225f;       // Air density at sea level (kg/m³)
        const float riderMass = 75.0f;  // Assumed rider mass (kg)
        const float mpsToMph = 2.237f;  // Conversion factor from m/s to mph

        // Total mass (bike + rider)
        float totalMass = bikeMass + riderMass;

        // Calculate rolling resistance (F_r = C_rr * m * g)
        float rollingResistance = rollingResistanceCoefficient * totalMass * g;

        // Solving for speed using the power equation:
        // P = F * v, where F includes air drag and rolling resistance
        // P = (0.5 * rho * Cd * A * v^3) + (Crr * m * g * v)

        // This is a cubic equation that needs solving
        // We'll use a simple iterative approach to find the speed

        float speed = 0.0f;
        float speedIncrement = 0.1f;
        float calculatedPower = 0.0f;

        while (calculatedPower < powerOutput)
        {
            speed += speedIncrement;

            // Air resistance (F_a = 0.5 * rho * Cd * A * v^2)
            float airResistance = 0.5f * rho * dragCoefficient * frontalArea * speed * speed;

            // Total resistance force
            float totalResistance = airResistance + rollingResistance;

            // Power required to overcome resistance at this speed
            calculatedPower = totalResistance * speed;

            // Prevent infinite loop if power is too low
            if (speed > 50.0f)  // Arbitrary maximum speed limit
                break;
        }

        // Convert from m/s to mph before returning
        return speed * mpsToMph;
    }


}
