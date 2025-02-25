using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;
using System.Text.RegularExpressions;
using UnityEngine.AI;
using System;
using Unity.VisualScripting;

public class DataManager : MonoBehaviour
{
    private string fileDirectory;
    [SerializeField] private string fileName;
    private StreamWriter writer;

    [Header("Player Reference")]
    [SerializeField] private BicycleAI player;

    [Header("SessionManager Reference")]
    [SerializeField] private SessionManager sessionManager;

    [Header("Exercise Bike Script Reference")]
    [SerializeField] private FTMS_UI exerciseBikeScript;

    [Header("Heartrate Monitor Script Reference")]
    [SerializeField] private HeartrateScanner heartrateScript;

    [Header("Performance Metrics")]
    [SerializeField] public float currentPower; 
    [SerializeField] public float currentCadence; 
    [SerializeField] public float currentSpeed; 
    [SerializeField] public float currentHeartRate;

    [Header("Study Information")]
    [SerializeField] public bool dataLoggingFlag;
    private bool dataCoroutineRunning;
    private bool listeningForPowerOutput;
    [SerializeField] public int participantID;
    [SerializeField] public ScenarioMode scenarioMode;
    [SerializeField] public DateTime dateAndTime;
    [SerializeField] private string messagePopUp;

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

        dataCoroutineRunning = false;
        listeningForPowerOutput = false;
        dataLoggingFlag = false;
        sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();

        fileDirectory = Path.Combine(Application.dataPath, "Performance Logs");

        if (!Directory.Exists(fileDirectory))
        {
            Directory.CreateDirectory(fileDirectory);
        }

        string directory = Path.Combine(fileDirectory, fileName += "_" + scenarioMode.ToString() + "_" + participantID.ToString() + ".csv");

        writer = new StreamWriter(directory, false);

        writer.WriteLine("Current Time,CurrentPower,CurrentCadence,CurrentSpeed,CurrentHeartRate");
        writer.Flush();

        StartCoroutine(ListenForPowerOutputStart());
    }

    // Update is called once per frame
    void Update()
    {
        // Start logging data when the flag is true and the coroutine isn't already running
        if (dataLoggingFlag && !dataCoroutineRunning)
            StartCoroutine(LogData());
        else if (!dataLoggingFlag && !listeningForPowerOutput)
            StartCoroutine(ListenForPowerOutputStart());
        else if (dataLoggingFlag)
        {
            StartCoroutine(ListenForPowerOutputEnd());
        }

        // Update current date/time and scenario mode
        dateAndTime = DateTime.Now;
        scenarioMode = sessionManager.GetCurrentScenarioMode();

        // Update the HUD
        UpdateHUD();
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

    private IEnumerator LogData()
    {
        dataCoroutineRunning = true;

        while (dataLoggingFlag)
        {
            DateTime time = DateTime.Now;
            string logEntry = string.Format("{0},{1},{2},{3},{4}",
                time, currentPower, currentCadence, currentSpeed, currentHeartRate);


            writer.WriteLine(logEntry);
            Debug.Log($"Written: {logEntry}");

            yield return new WaitForSeconds(1f);
        }

        dataCoroutineRunning = false; // Set this to false when logging stops
    }

    private IEnumerator ListenForPowerOutputStart()
    {
        listeningForPowerOutput = true;

        while (!dataLoggingFlag)
        {
            if (currentPower > 0)
            {
                messagePopUp = "Beginning to log data in 10 seconds.";
                yield return new WaitForSeconds(10f);
                messagePopUp = "";
                dataLoggingFlag = true;
                yield return null;
            }

            // Optionally, you can yield here for a small time to avoid a tight loop:
            yield return null;
        }

        listeningForPowerOutput = false; // Stop listening once logging starts
    }

    private IEnumerator ListenForPowerOutputEnd()
    {
        // Track the time elapsed since the last power output was detected
        float timeWithoutPower = 0f;
        const float maxInactivityTime = 5f; // 5 seconds of inactivity to stop logging

        while (dataLoggingFlag)
        {
            if (currentPower > 0)
            {
                // Reset the inactivity timer if power is detected
                timeWithoutPower = 0f;
            }
            else
            {
                // Accumulate the inactivity time
                timeWithoutPower += Time.deltaTime;

                // Check if the inactivity time has exceeded the threshold
                if (timeWithoutPower >= maxInactivityTime)
                {
                    dataLoggingFlag = false;
                    messagePopUp = "No power detected. Stopping data logging.";
                    writer.Close();
                    Debug.Log($"File: {fileName} saved successfully.");


                }
            }

            // Yield here to avoid tight looping
            yield return null;
        }

        listeningForPowerOutput = false; // Stop listening once logging has been stopped
    }

    private IEnumerator UploadPopUpMessage(string msg, float timeframe)
    {
        // Wait until the current message is cleared (messagePopUp becomes "")
        while (!string.IsNullOrEmpty(messagePopUp))
        {
            yield return null; // Wait for the current message to be cleared
        }

        // Set the new message to be displayed
        messagePopUp = msg;

        // Wait for the specified timeframe
        yield return new WaitForSeconds(timeframe);

        // Clear the message after the timeframe has elapsed
        messagePopUp = "";
    }


    private void OnApplicationQuit() {
        
    }

    public void ProcessDataFromBike(string info)
    {
        if (info.Length == 0) return;

        string[] strings = info.Split(new char[] {',', ' '}, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < strings.Length; i++)
        {
            strings[i] = Regex.Replace(strings[i], @"[^\d.]", "");

            if (i == 3) 
            {
                currentCadence = float.Parse(strings[i].Replace("\n", ""));
            }
            if (i == 5)
            {
                currentPower = float.Parse(strings[i]);
            }

            
        }

        float rawPower = float.Parse(strings[5]);
        float newSpeed = 0;

        // Assuming the desired acceleration value is given
        float acceleration = player.desiredAcceleration; // Adjust as needed for smooth acceleration/deceleration

        // Calculate the target speed using the existing method
        float targetSpeed = CalculateSpeed(rawPower, dragCoefficient, frontalArea, rollingResistanceCoefficient, bikeMass);

        // Access the NavMeshAgent component
        NavMeshAgent navMeshAgent = player.GetComponent<NavMeshAgent>();

        

        // Smooth the transition using a simple linear interpolation (lerp)
        float speedDifference = targetSpeed - navMeshAgent.speed;

        // Applying acceleration to gradually change the speed
        newSpeed = Mathf.Lerp(navMeshAgent.speed, targetSpeed, acceleration * Time.deltaTime);

        // Ensure speed is clamped to avoid going below zero
        newSpeed = Mathf.Max(0, newSpeed);

        currentSpeed = newSpeed;     

        // Update the player's pace with the new smooth speed
        player.UpdatePace(currentSpeed);

        

    }

    public void ProcessDataFromHR(string info)
    {
        currentHeartRate = float.Parse(info);
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

    private void UpdateHUD()
    {
        if (headsUpDisplay != null)
        {
            if (headsUpDisplay.currentCadenceData != null)
                headsUpDisplay.UpdateText(headsUpDisplay.currentCadenceData, "Cadence: " + Mathf.Round(currentCadence).ToString() + "RPM");

            if (headsUpDisplay.currentSpeedData != null)
                headsUpDisplay.UpdateText(headsUpDisplay.currentSpeedData, "Speed: " + Mathf.Round(currentSpeed).ToString() + "MPH");

            if (headsUpDisplay.currentPowerData != null)
                headsUpDisplay.UpdateText(headsUpDisplay.currentPowerData, "Power: " + Mathf.Round(currentPower).ToString() + "W");

            if (headsUpDisplay.currentHeartrateData != null)
                headsUpDisplay.UpdateText(headsUpDisplay.currentHeartrateData, "Heartrate: " + Mathf.Round(currentHeartRate).ToString() + "BPM");

            if (headsUpDisplay.dataLoggingFlag != null)
                headsUpDisplay.UpdateText(headsUpDisplay.dataLoggingFlag, "Data Logging: " + dataLoggingFlag.ToString());

            if (headsUpDisplay.participantID != null)
                headsUpDisplay.UpdateText(headsUpDisplay.participantID, "Participant ID: " + participantID.ToString());

            if (headsUpDisplay.selectedScenarioMode != null)
                headsUpDisplay.UpdateText(headsUpDisplay.selectedScenarioMode, "Scenario: " + scenarioMode.ToString());

            if (headsUpDisplay.dateAndTime != null)
                headsUpDisplay.UpdateText(headsUpDisplay.dateAndTime, dateAndTime.ToString("dd-MM-yyyy HH:mm:ss"));

            if (headsUpDisplay.messagePopUp != null)
            {
                headsUpDisplay.UpdateText(headsUpDisplay.messagePopUp, messagePopUp);
            }
        }
    }

}
