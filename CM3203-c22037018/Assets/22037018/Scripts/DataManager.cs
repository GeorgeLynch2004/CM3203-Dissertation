using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;
using System.Text.RegularExpressions;
using UnityEngine.AI;
using System;
using Unity.VisualScripting;
using Unity.XR.CoreUtils.Datums;
using System.Linq;

public class DataManager : MonoBehaviour
{
    public string fileDirectory;
    private string directory;
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
    [SerializeField] public float currentDurationData;
    private List<float> lastMinutePower = new List<float>();

    [Header("Study Information")]
    [SerializeField] public bool dataLoggingFlag;
    private bool dataCoroutineRunning;
    private bool listeningForPowerOutput;
    [SerializeField] public int participantID;
    [SerializeField] public float participantWeight;
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
        currentDurationData = 0;
        sessionManager = GameObject.Find("SessionManager").GetComponent<SessionManager>();

        fileDirectory = Path.Combine(Application.dataPath, "Performance Logs");

        if (!Directory.Exists(fileDirectory))
        {
            Directory.CreateDirectory(fileDirectory);
        }

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
            currentDurationData++;
            string logEntry = string.Format("{0},{1},{2},{3},{4}",
                time.ToString("dd-MM-yyyy HH:mm:ss"), currentPower, currentCadence, (float)Math.Round(currentSpeed,2), currentHeartRate);

            
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
                sessionManager.BeginSession();

                messagePopUp = "Beginning to log data in 10 seconds.";

                // set up the writer now the scenario mode has been determined
                string logFileName = $"{fileName}_{scenarioMode}_{participantID}.csv";
                directory = Path.Combine(fileDirectory, logFileName);

                writer = new StreamWriter(directory, false);

                writer.WriteLine($"ParticipantID,ScenarioMode,Date & Time");
                writer.WriteLine($"{participantID},{scenarioMode},{dateAndTime.ToString("dd-MM-yyyy")}");
                writer.WriteLine("");
                writer.WriteLine("Current Time,CurrentPower,CurrentCadence,CurrentSpeed,CurrentHeartRate");
                writer.Flush();

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

                    CalculateFileFooter();

               
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

    private void CalculateFileFooter()
    {
        string[] lines = File.ReadAllLines(directory);

        // remove last 5 seconds of data as it is empty garbage
        if (lines.Length > 5)
        {
            lines = lines.SkipLast(5).ToArray();

            // Overwrite the original file with the modified lines
            File.WriteAllLines(directory, lines);
        }
        else
        {
            // If there are fewer than or exactly 5 lines, clear the file
            File.WriteAllText(directory, string.Empty);
        }

        List<PerformanceData> data = new List<PerformanceData>();

        float totalPower = 0f;
        float totalCadence = 0f;
        float totalSpeed = 0f;
        float totalHeartRate = 0f;

        float maxPower = 0f;
        float maxCadence = 0f;
        float maxSpeed = 0f;
        float maxHeartRate = 0f;
        float totalTime = 0f;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(',');



            if (values.Length >= 5)
            {
                float power = 0f, cadence = 0f, speed = 0f, heartRate = 0f;

                // Try parsing values and handle invalid data
                if (float.TryParse(values[1], out power) && float.TryParse(values[2], out cadence) &&
                    float.TryParse(values[3], out speed) && float.TryParse(values[4], out heartRate))
                {
                    data.Add(new PerformanceData(power, cadence, speed, heartRate));

                    // average
                    totalPower += power;
                    totalCadence += cadence;
                    totalSpeed += speed;
                    totalHeartRate += heartRate;

                    // max
                    maxPower = Mathf.Max(maxPower, power);
                    maxHeartRate = Mathf.Max(maxHeartRate, heartRate);
                    maxSpeed = Mathf.Max(maxSpeed, speed);
                    maxCadence = Mathf.Max(maxCadence, cadence);

                    // increment time
                    totalTime += 1f;

                    if (lastMinutePower.Count == 60)
                    {
                        // Remove the oldest power value
                        lastMinutePower.RemoveAt(0);
                    }

                    // Add the current power value to the list
                    lastMinutePower.Add(power);
                }
                else
                {
                    // Log or handle the invalid data if needed (skip or continue)
                    Debug.LogWarning($"Invalid data at line {i}: {line}");
                }
            }
        }

        // calculate averages
        int dataCount = data.Count;
        if (dataCount > 0)
        {
            float avgPower = (float)Math.Round(totalPower / dataCount, 2);
            float avgCadence = (float)Math.Round(totalCadence / dataCount, 2);
            float avgSpeed = (float)Math.Round(totalSpeed / dataCount, 2);
            float avgHeartRate = (float)Math.Round(totalHeartRate / dataCount, 2);

            // calculate performance metrics
            float powerToWeightRatio = 0;
            if (participantWeight > 0)
            {
                powerToWeightRatio = avgPower / participantWeight;
            }

            float ftp = 0;

            if (lastMinutePower.Count == 60)
            {
                float lastMinuteAveragePower = lastMinutePower.Average();
                ftp = (float)Math.Round(lastMinuteAveragePower * 0.75f, 2);
            }

            float heartRateRecovery = (float)Math.Round(maxHeartRate - avgHeartRate,2);
            float efficiencyFactor = (float)Math.Round(avgPower / avgHeartRate,2);
            float peakPowerOutput = maxPower;
            float maximumHeartrate = maxHeartRate;
            float totalWorkDone = totalPower * totalTime;

            using (StreamWriter writer = new StreamWriter(directory, true))
            {
                writer.WriteLine($"");
                writer.WriteLine($"Averages:");
                writer.WriteLine($"Average Power,{avgPower}");
                writer.WriteLine($"Average Cadence,{avgCadence}");
                writer.WriteLine($"Average Speed,{avgSpeed}");
                writer.WriteLine($"Average Heart Rate,{avgHeartRate}");
                if (powerToWeightRatio > 0)
                {
                    writer.WriteLine($"Average Power to Weight Ratio (P/W),{powerToWeightRatio}");
                }
                else
                {
                    writer.WriteLine($"Average Power to Weight Ratio (P/W),N/A");
                }
                
                writer.WriteLine($"");
                writer.WriteLine($"Performance Metrics:");
                writer.WriteLine($"Functional Threshold Power (FTP),{ftp}");
                writer.WriteLine($"Heart Rate Recovery (HRR),{heartRateRecovery}");
                writer.WriteLine($"Efficiency Factor (EF),{efficiencyFactor}");
                writer.WriteLine($"Peak Power Output (PPO),{peakPowerOutput}");
                writer.WriteLine($"Maximum Heart Rate,{maximumHeartrate}");
                writer.WriteLine($"Total Work Done,{totalWorkDone}");
            }
        }
        else
        {
            Debug.LogError("No valid data to process.");
        }
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
        return (float)Math.Round(speed * mpsToMph,1);
    }

    private void UpdateHUD()
    {
        if (headsUpDisplay != null)
        {
            if (headsUpDisplay.currentCadenceData != null)
                headsUpDisplay.UpdateText(headsUpDisplay.currentCadenceData, "Cadence (RPM): " + Mathf.Round(currentCadence).ToString());

            if (headsUpDisplay.currentSpeedData != null)
                headsUpDisplay.UpdateText(headsUpDisplay.currentSpeedData, "Speed (MPH): " + Mathf.Round(currentSpeed).ToString());

            if (headsUpDisplay.currentPowerData != null)
                headsUpDisplay.UpdateText(headsUpDisplay.currentPowerData, "Power (W): " + Mathf.Round(currentPower).ToString());

            if (headsUpDisplay.currentHeartrateData != null)
                headsUpDisplay.UpdateText(headsUpDisplay.currentHeartrateData, "Heartrate (BPM): " + Mathf.Round(currentHeartRate).ToString());

            if (headsUpDisplay.currentDurationData != null)
                headsUpDisplay.UpdateText(headsUpDisplay.currentDurationData, "Duration (S): " + Math.Round(currentDurationData,2).ToString());

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

    public Dictionary<string, List<float>> GenerateWorkoutProfiles(int id, string performance_log_directory)
    {
        Dictionary<string, List<float>> dict = new Dictionary<string, List<float>>();

        if (Directory.Exists(performance_log_directory))
        {
            string[] files = Directory.GetFiles(performance_log_directory);
            string[] participantIDFiles = files
    .Where(file => Path.GetFileName(file).Contains(id.ToString()) && !file.EndsWith(".meta"))
    .ToArray();


            if (participantIDFiles.Length > 0)
            {
                List<List<float>> allFilePowerData = new List<List<float>>();
                List<List<float>> allFileHeartrateData = new List<List<float>>();

                foreach (var file in participantIDFiles)
                {
                    Debug.Log($"File Found for Participant ID ({id}): {file.ToString()}");

                    // Get Power Data from file
                    List<float> powerData = GetColumnData(file, 1);
                    // Get Heartrate Data from file
                    List<float> heartrateData = GetColumnData(file,4);

                    // add them to the greater arrays
                    allFilePowerData.Add(powerData);
                    allFileHeartrateData.Add(heartrateData);
                }

                // FOR DEBUGGING
                Debug.Log(allFilePowerData.ToString());
                Debug.Log(allFileHeartrateData.ToString());

                // Calculate averages across all file data
                List<float> averageHistoricalPowerProfile = CalculateListAverage(allFilePowerData);
                List<float> averageHistoricalHeartrateProfile = CalculateListAverage(allFileHeartrateData);

                // FOR DEBUGGING
                Debug.Log(averageHistoricalPowerProfile.ToString());
                Debug.Log(averageHistoricalHeartrateProfile.ToString());

                dict = new Dictionary<string, List<float>>(){
                    { "Power", averageHistoricalPowerProfile },
                    { "Heartrate", averageHistoricalHeartrateProfile }
                };
            }
            else
            {
                Debug.LogError("There were no files found for that participant ID.");
            }
        }
        else
        {
            Debug.LogError("Directory Inputted does not exist.");
        }

        return dict;
    }

    private List<float> CalculateListAverage(List<List<float>> data)
    {
        // Determine the maximum list length
        int maxListLength = 0;
        foreach (var list in data)
        {
            if (list.Count > maxListLength)
            {
                maxListLength = list.Count;
            }
        }

        List<float> averages = new List<float>();

        // Iterate over each index in the lists
        for (int i = 0; i < maxListLength; i++)
        {
            List<float> valuesAtIndex = new List<float>();

            // Collect the nth element from each list if it exists
            foreach (var list in data)
            {
                if (i < list.Count)
                {
                    valuesAtIndex.Add(list[i]);
                }
            }

            // If we have multiple values, calculate the average
            if (valuesAtIndex.Count > 0)
            {
                float sum = 0f;
                foreach (var value in valuesAtIndex)
                {
                    sum += value;
                }

                averages.Add(sum / valuesAtIndex.Count);
            }
        }

        return averages;
    }

    List<float> GetColumnData(string filePath, int columnIndex)
    {
        List<float> columnData = new List<float>();

        // Check if the file exists
        if (File.Exists(filePath))
        {
            // Read all lines from the file
            string[] lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                // Split the line into columns (assuming commas as separators)
                string[] columns = line.Split(',');

                // Ensure the column index is within the bounds of the current row
                if (columnIndex < columns.Length)
                {
                    // If the value in the column is empty, stop processing
                    if (string.IsNullOrWhiteSpace(columns[columnIndex]))
                    {
                        break; // Stop reading further rows
                    }

                    // Try parsing the value in the column
                    float val = 0;
                    if (float.TryParse(columns[columnIndex], out val))
                    {
                        columnData.Add(val);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("CSV file not found at path: " + filePath);
        }

        return columnData;
    }

}

public class PerformanceData
{
    public float power;
    public float cadence;
    public float speed;
    public float heartRate;

    public PerformanceData(float power, float cadence, float speed, float heartRate)
    {
        this.power = power;
        this.cadence = cadence;
        this.speed = speed;
        this.heartRate = heartRate;
    }
}
