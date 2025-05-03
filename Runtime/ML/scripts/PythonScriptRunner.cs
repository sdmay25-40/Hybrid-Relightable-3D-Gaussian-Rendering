using System.Diagnostics;  // For starting and managing external processes
using UnityEngine;         // Unity game engine functionality
using System.IO;           // For file path and file existence checking
using System.Collections;  // For using Coroutines
using System;              // For general C# features like DateTime

public class PythonScriptRunner : MonoBehaviour
{
    // Paths to the Python scripts to be executed
    private string vidConverter = "Packages/HybridRelightable3DGaussianRendering/Runtime/ML/scripts/vid2image.py";
    private string sfm = "Packages/HybridRelightable3DGaussianRendering/Runtime/ML/scripts/SfMScript.py";
    private string optimizer = "Packages/HybridRelightable3DGaussianRendering/Runtime/ML/scripts/main.py";
    public string pythonExecutable = "C:\\YOUR\\FILE\\PATH\\python.exe";

    // Max allowed time (in seconds) for a script to run before timing out
    public float timeoutInSeconds = 36000f;

    // Unity method that runs when the scene starts
    void Start()
    {
        // Start the coroutine to run scripts one after the other
        StartCoroutine(RunSequentialScripts());
    }

    // Coroutine that runs the Python scripts sequentially
    IEnumerator RunSequentialScripts()
    {
        // Check if the first script exists
        if (File.Exists(vidConverter))
        {
            // Run the first script
            yield return StartCoroutine(RunPythonScriptAsync(vidConverter, timeoutInSeconds));
        }
        else
        {
            // Log an error if the file does not exist
            UnityEngine.Debug.LogError("Python script not found: " + vidConverter);
            yield break;
        }

        // Check if the second script exists
        if (File.Exists(sfm))
        {
            // Run the second script
            yield return StartCoroutine(RunPythonScriptAsync(sfm, timeoutInSeconds));
        }
        else
        {
            // Log an error if the second file does not exist
            UnityEngine.Debug.LogError("Python script not found: " + sfm);
        }
        if (File.Exists(optimizer))
        {
            // Run the second script
            yield return StartCoroutine(RunPythonScriptAsync(optimizer, timeoutInSeconds));
        }
        else
        {
            // Log an error if the second file does not exist
            UnityEngine.Debug.LogError("Python script not found: " + optimizer);
        }
    }

    // Coroutine to run a Python script asynchronously with a timeout
    IEnumerator RunPythonScriptAsync(string scriptPath, float timeoutSeconds)
    {
        if (!File.Exists(pythonExecutable))
        {
            UnityEngine.Debug.LogError($"Cannot find python at {pythonExecutable}");
            yield break;
        }

        // Configure the process to run the Python script
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonExecutable,
            Arguments = $"\"{scriptPath}\"", // Enclose script path in quotes in case it contains spaces
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Application.dataPath + "/.."
        };

        // Use a using block to ensure proper disposal of the process
        using (Process process = new Process())
        {
            process.StartInfo = startInfo;

            // Output handler for stdout
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    UnityEngine.Debug.Log($"[{DateTime.Now:HH:mm:ss}] Python output: {e.Data}");
                }
            };

            // Error handler for stderr
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    UnityEngine.Debug.LogError($"[{DateTime.Now:HH:mm:ss}] Python error: {e.Data}");
                }
            };

            // Start the process and begin reading output asynchronously
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Start the stopwatch to track timeout
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Wait until the process exits or times out
            while (!process.HasExited)
            {
                if (stopwatch.Elapsed.TotalSeconds > timeoutSeconds)
                {
                    UnityEngine.Debug.LogError("Timeout reached. Killing Python process...");
                    try
                    {
                        process.Kill(); // Try to kill the process if it exceeds time limit
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError("Failed to kill Python process: " + ex.Message);
                    }

                    UnityEngine.Debug.LogError("Script forcibly terminated after timeout.");
                    yield break;
                }

                yield return null; // Wait until the next frame
            }

            stopwatch.Stop(); // Stop the stopwatch once the process ends

            // Check exit code to determine if script ran successfully
            if (process.ExitCode == 0)
            {
                UnityEngine.Debug.Log("Python script completed successfully.");
            }
            else
            {
                UnityEngine.Debug.LogError($"Python script exited with code: {process.ExitCode}");
            }
        }
    }

    // Helper method to check if Python is installed and callable
    bool IsPythonInstalled(string pythonExecutable)
    {
        try
        {
            // Try running "python --version" to check if Python responds
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // If either output or error contains data, Python is available
                return !string.IsNullOrEmpty(output) || !string.IsNullOrEmpty(error);
            }
        }
        catch
        {
            // If an exception occurs, Python is not available
            return false;
        }
    }
}