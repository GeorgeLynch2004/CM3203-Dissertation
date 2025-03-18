using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UnityGraphLoader : MonoBehaviour
{
    [SerializeField] private RawImage powerGraph;
    [SerializeField] private RawImage heartrateGraph;
    [SerializeField] private RawImage combinedGraph;

    public void RunDataVisualisationScript(string scriptName, string csvFileName, string outputImageFolder)
    {
        // Define all the necessary paths
        string scriptPath = Path.Combine(Application.streamingAssetsPath, scriptName);
        string csvPath = Path.Combine(Application.streamingAssetsPath, csvFileName);
        string destScriptPath = Path.Combine(Application.persistentDataPath, scriptName);
        string destCsvPath = Path.Combine(Application.persistentDataPath, csvFileName);
        string imagePath = Path.Combine(Application.persistentDataPath, outputImageFolder);

        // Copy Python script to persistentDataPath if needed
        if (!File.Exists(destScriptPath))
        {
            File.Copy(scriptPath, destScriptPath);
        }

        // Copy CSV file to persistentDataPath if needed
        if (File.Exists(csvPath) && !File.Exists(destCsvPath))
        {
            File.Copy(csvPath, destCsvPath);
        }

        // Create process to run Python script
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "python";  // Uses Python from PATH
        psi.Arguments = $"\"{destScriptPath}\" \"{destCsvPath}\" \"{imagePath}\"";  // Pass both CSV and image paths
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        UnityEngine.Debug.Log($"Command: python \"{destScriptPath}\" \"{destCsvPath}\" \"{imagePath}\"");

        // Execute Python process
        using (Process process = Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrEmpty(output))
                UnityEngine.Debug.Log($"Python output: {output}");

            if (!string.IsNullOrEmpty(error))
                UnityEngine.Debug.LogError($"Python error: {error}");

            UnityEngine.Debug.Log("Python script execution completed.");
        }


        LoadImage(powerGraph, Path.Combine(imagePath, "power_graph.png"));
        LoadImage(heartrateGraph, Path.Combine(imagePath, "heartrate_graph.png"));
        LoadImage(combinedGraph, Path.Combine(imagePath, "combined_graph.png"));
    }

    public void LoadImage(RawImage img, string imagePath)
    {
        if (File.Exists(imagePath))
        {
            byte[] fileData = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            img.texture = texture;
            UnityEngine.Debug.Log($"Image successfully loaded from: {imagePath}");
        }
        else
        {
            UnityEngine.Debug.LogError($"Image not found at: {imagePath}");
        }
    }
}