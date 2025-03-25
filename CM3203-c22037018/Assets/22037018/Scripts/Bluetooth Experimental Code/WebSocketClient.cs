using UnityEngine;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json.Linq;
using Meta.XR.MultiplayerBlocks.Fusion.Editor; // Install "Newtonsoft.Json" via Unity Package Manager

public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }
    private WebSocket ws;
    [SerializeField] public string output;
    [SerializeField] private string pyInstallPath;
    [SerializeField] private string pyScriptPath;
    private bool isConnecting = false;
    private float reconnectTimer = 0f;
    private const float RECONNECT_INTERVAL = 3f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    
    async void Start()
    {
        await ConnectToWebSocket();
    }

    private async Task ConnectToWebSocket()
    {
        if (isConnecting) return;

        isConnecting = true;
        output = "Attempting to connect...";

        if (ws != null)
        {
            await ws.Close();
        }

        ws = new WebSocket("ws://localhost:8765");
        SetupWebSocketCallbacks();

        try
        {
            await ws.Connect();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to connect: {e.Message}");
            output = "Connection failed - will retry in 3 seconds";
            isConnecting = false;
        }
    }

    private void SetupWebSocketCallbacks()
    {
        ws.OnMessage += (bytes) =>
        {
            try
            {
                string message = Encoding.UTF8.GetString(bytes);
                JObject json = JObject.Parse(message);
                // Use null-safe conversion for all values
                int heartRate = json["heart_rate"]?.ToObject<int>() ?? 0;
                float speed = json["speed"]?.ToObject<float>() ?? 0;
                int averageSpeed = json["average_speed"]?.ToObject<int>() ?? 0;
                float rpm = json["rpm"]?.ToObject<float>() ?? 0;
                float averageRpm = json["average_rpm"]?.ToObject<float>() ?? 0;
                int distance = json["distance"]?.ToObject<int>() ?? 0;
                int resistance = json["resistance"]?.ToObject<int>() ?? 0;
                int power = json["power"]?.ToObject<int>() ?? 0;
                int averagePower = json["average_power"]?.ToObject<int>() ?? 0;
                int expendedEnergy = json["expended_energy"]?.ToObject<int>() ?? 0;
                // Update the output string
                output = $"Speed: {speed:F1} km/h, RPM: {rpm:F1}, Power: {power} W";

                DataManager.Instance.ProcessDataFromPython(output);

            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error parsing WebSocket message: {e.Message}");
                output = $"Error parsing data: {e.Message}";
            }
        };

        ws.OnOpen += () =>
        {
            UnityEngine.Debug.Log("WebSocket connection open!");
            output = "Connected to sensor server";
            isConnecting = false;
            reconnectTimer = 0f;
        };

        ws.OnError += (e) =>
        {
            UnityEngine.Debug.Log($"WebSocket error: {e}");
            output = $"Error: {e}";
            isConnecting = false;
        };

        ws.OnClose += (e) =>
        {
            UnityEngine.Debug.Log($"WebSocket connection closed with code: {e}");
            output = $"Connection closed: {e} - will retry in 3 seconds";
            isConnecting = false;
        };
    }

    private void RunPythonScript()
    {
        UnityEngine.Debug.Log($"Running command: {pyInstallPath} {pyScriptPath}");

        Process process = new Process();
        process.StartInfo.FileName = pyInstallPath;
        process.StartInfo.Arguments = pyScriptPath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        // Read output asynchronously
        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                UnityEngine.Debug.Log("Python Output: " + args.Data);
        };

        // Read errors asynchronously
        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                UnityEngine.Debug.LogError("Python Error: " + args.Data);
        };

        try
        {
            // Start the Python process
            process.Start();

            // Begin reading the standard output and standard error asynchronously
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for the process to exit
            process.WaitForExit();

            // Check the exit code to see if the script executed successfully
            if (process.ExitCode != 0)
            {
                UnityEngine.Debug.LogError($"Python script ended with error. Exit code: {process.ExitCode}");
            }
            else
            {
                UnityEngine.Debug.Log("Python script completed successfully.");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error executing Python script: " + e.Message);
        }
    }


    private void Update()
    {
        // Process WebSocket messages
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null && ws.State == WebSocketState.Open)
        {
            ws.DispatchMessageQueue();
        }
#endif

        // Handle reconnection
        if (ws == null || (ws.State != WebSocketState.Open && ws.State != WebSocketState.Connecting && !isConnecting))
        {
            reconnectTimer += Time.deltaTime;

            if (reconnectTimer >= RECONNECT_INTERVAL)
            {
                reconnectTimer = 0f;
                UnityEngine.Debug.Log("Attempting to reconnect...");
                _ = ConnectToWebSocket();
            }
        }
    }

    private async void OnApplicationQuit()
    {
        if (ws != null && ws.State == WebSocketState.Open)
        {
            await ws.Close();
        }
    }
}