using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.VFX;
using Unity.Profiling;
using UnityEngine.InputSystem.XR;
using System.Text;
using System.Linq;

/// <summary>
/// AutomatedTest is a script for performing automated testing of two different particle effects and no particle effects.
/// The tests use the movement data recorded with the MovementRecorder script and are run three times for each configuration.
/// The test results are saved to a CSV file.
/// </summary>
public struct RecordedTransform
{
    public float timestamp;
    public Vector3 position;
    public Quaternion rotation;
}

public class AutomatedTest : MonoBehaviour
{
    // UI elements to display information and debug text
    public TMPro.TextMeshProUGUI infoText;
    public TMPro.TextMeshProUGUI debugText;
    
    // Components and game objects involved in the testing process
    public TrackedPoseDriver trackedPoseDriver;
    public GameObject cameraGameObject;
    public Camera mainCamera;
    public GameObject MRInteractionSetup;
    public GameObject XROrigin;
    public GameObject CameraOffset;
    public GameObject CameraMover;
    public GameObject MovementAnchorObject;
    public GameObject vfxParticleEffect;
    public GameObject builtinParticleEffect;
    public VisualEffect vfxParticleEffectVisual; // VFX particle effect component
    public ParticleSystem builtinParticleSystem;
    public GameObject Environment3D; // 3D VR environment to toggle
    public Color solidColor; // Solid color for passthrough mode

    // Test configuration parameters
    public float waitTime = 5f; // Time to wait before starting and after finishing
    public float particleStartRate = 20f;
    public float particleMaxRate = 200f;
    public float particleMaxRateVFXPassthrough;
    public float particleMaxRateVFXImmersive;
    public float particleMaxRateBuiltInPassthrough;
    public float particleMaxRateBuiltInImmersive;
    public float timeToIncreaseParticles = 5f;

    // FPS and profiler related variables
    public float fpsThreshold = 30.0f;  // The FPS threshold
    private float previousFPS = 0.0f;   // To store the last FPS value
    private float safeParticleRate;     // To store the safe particle rate before reduction
    private int framesCount;
    private float framesTime;
    private float lastFPS;

    // Flags and state variables
    private bool particlesOn = false;
    private bool passthroughOn = false;
    private bool isRecording = false; // Flag to control recording
    private float startTime;

    // Lists to store recorded data and transforms
    private List<string> recordedData;
    private List<RecordedTransform> recordedTransforms = new List<RecordedTransform>();

    // Profiler recorders for performance metrics
    private ProfilerRecorder triangleRecorder;
    private ProfilerRecorder drawCallsRecorder;
    private ProfilerRecorder verticesRecorder;
    private ProfilerRecorder memoryUsageRecorder;
    private ProfilerRecorder gpuUsageRecorder;

    // Offset values for the camera
    private Vector3 cameraLocalPositionOffset;
    private Quaternion cameraLocalRotationOffset;

    /// <summary>
    /// Called when the script instance is being loaded. Initializes the particle emission rate.
    /// </summary>
    private void Start()
    {
        startTime = Time.time;
        var emission = builtinParticleEffect.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = particleStartRate;
    }

    /// <summary>
    /// Called once per frame. Updates FPS calculation and records performance data if recording is active.
    /// </summary>
    private void Update()
    {
        if (!isRecording) return; // Skip the rest of the update if not recording

        // Update frames for FPS calculation
        framesCount++;
        framesTime += Time.deltaTime;
        if (framesTime >= 0.5f) // Calculate FPS every half second
        {
            lastFPS = framesCount / framesTime;
            framesCount = 0;
            framesTime = 0;
        }

        // Prepare the data string
        string data = Time.time + ", " + 
                      lastFPS + ", " +
                      triangleRecorder.LastValue + ", " + 
                      drawCallsRecorder.LastValue + ", " + 
                      verticesRecorder.LastValue + ", " + 
                      memoryUsageRecorder.LastValue + ", " + 
                      gpuUsageRecorder.LastValue + ", " + 
                      vfxParticleEffectVisual.aliveParticleCount + "," +
                      builtinParticleSystem.particleCount + "," +
                      particlesOn;

        // Write the data to file
        recordedData.Add(data);

        // Update particle effect emission rates over time
        if (vfxParticleEffect.activeSelf)
        {
            float timeElapsed = Time.time - startTime;
            float rate = passthroughOn 
                ? Mathf.Lerp(particleStartRate, particleMaxRateVFXPassthrough, timeElapsed / timeToIncreaseParticles)
                : Mathf.Lerp(particleStartRate, particleMaxRateVFXImmersive, timeElapsed / timeToIncreaseParticles);
            vfxParticleEffectVisual.SetFloat("SpawnRate", rate);
        }

        if (builtinParticleEffect.activeSelf)
        {
            float timeElapsed = Time.time - startTime;
            float rate = passthroughOn 
                ? Mathf.Lerp(particleStartRate, particleMaxRateBuiltInPassthrough, timeElapsed / timeToIncreaseParticles)
                : Mathf.Lerp(particleStartRate, particleMaxRateBuiltInImmersive, timeElapsed / timeToIncreaseParticles);
            var emission = builtinParticleEffect.GetComponent<ParticleSystem>().emission;
            emission.rateOverTime = rate;
        }

        // Display debug information
        debugText.text = "Main Camera world: " + mainCamera.transform.position.ToString() + "\n" +
                         "Main Camera local: " + mainCamera.transform.localPosition.ToString() + "\n" +
                         "Camera mover world: " + CameraMover.transform.position.ToString()  + "\n" +
                         "Camera mover local: " + CameraMover.transform.localPosition.ToString()  + "\n" +
                         "Movement anchor object: " + MovementAnchorObject.transform.position.ToString();
    }

    /// <summary>
    /// Starts the automated test sequence.
    /// </summary>
    public void StartTest()
    {
        StartCoroutine(ExecuteTests());
    }

    /// <summary>
    /// Starts recording performance data.
    /// </summary>
    private void StartRecording()
    {
        if (!isRecording)
        {
            recordedData = new List<string>();
            Debug.Log("Starting recording!");

            // Initialize recorders
            triangleRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            memoryUsageRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            gpuUsageRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Usage");

            isRecording = true;
        }
    }

    /// <summary>
    /// Stops recording performance data and saves it to a CSV file.
    /// </summary>
    /// <param name="filename">The name of the file to save the data to.</param>
    private void StopRecording(string filename)
    {
        try
        {
            if (isRecording)
            {
                string filePath = Path.Combine(Application.persistentDataPath, filename);
                File.WriteAllLines(filePath, recordedData);
                Debug.Log("Stopping recording!");
            }
        }
        finally
        {
            // Cleanup recorders
            triangleRecorder.Dispose();
            drawCallsRecorder.Dispose();
            verticesRecorder.Dispose();
            memoryUsageRecorder.Dispose();
            gpuUsageRecorder.Dispose();

            isRecording = false;
            recordedData.Clear();
            recordedTransforms.Clear();
        }
    }

    /// <summary>
    /// Executes the test sequence for each particle effect configuration and passthrough option.
    /// </summary>
    /// <returns>An enumerator for coroutine handling.</returns>
    private IEnumerator ExecuteTests()
    {
        trackedPoseDriver.enabled = false;
        CaptureCameraOffset();
        ResetPositions();
        Debug.Log("Testing started and tracked pose driver disabled");
        string[] particleEffectTypes = { "VFX", "BuiltIn", "none" };
        bool[] passthroughOptions = { false, true };

        foreach (var particleEffect in particleEffectTypes)
        {
            foreach (var passthrough in passthroughOptions)
            {
                for (int i = 0; i < 3; i++) // Repeat each test three times
                {
                    Debug.Log($"Starting test run {i+1} for {particleEffect} with passthrough {passthrough}");
                    infoText.text = "Particle effect: " + particleEffect + " Passthrough: " + passthrough + " Run: " + (i+1);
                    yield return ExecuteTest(particleEffect, passthrough);
                    builtinParticleSystem.Clear();
                    builtinParticleSystem.Stop();
                    vfxParticleEffectVisual.Reinit();
                    vfxParticleEffect.SetActive(false);
                    builtinParticleEffect.SetActive(false);
                    Debug.Log($"Ended test run {i+1} for {particleEffect} with passthrough {passthrough}");
                    yield return new WaitForSeconds(waitTime); // Wait before the next repetition
                }
            }
        }
        trackedPoseDriver.enabled = true;
        Debug.Log("Testing ended and tracked pose driver enabled");
        debugText.text = "Test has ended";
    }

    /// <summary>
    /// Executes a single test run with the specified particle effect and passthrough configuration.
    /// </summary>
    /// <param name="particleEffect">The type of particle effect to use.</param>
    /// <param name="passthrough">Whether passthrough mode is enabled.</returns>
    /// <returns>An enumerator for coroutine handling.</returns>
    private IEnumerator ExecuteTest(string particleEffect, bool passthrough)
    {
        particlesOn = particleEffect != "None";
        passthroughOn = passthrough;

        if (particleEffect == "BuiltIn")
        {
            builtinParticleSystem.Clear();
            builtinParticleSystem.Stop();
            vfxParticleEffectVisual.Reinit();
            builtinParticleEffect.SetActive(true);
            builtinParticleSystem.Play();
            vfxParticleEffect.SetActive(false);
        }
        else if (particleEffect == "VFX")
        {
            builtinParticleSystem.Clear();
            builtinParticleSystem.Stop();
            vfxParticleEffectVisual.Reinit();
            builtinParticleEffect.SetActive(false);
            vfxParticleEffect.SetActive(true);
        }
        else
        {
            builtinParticleSystem.Clear();
            builtinParticleSystem.Stop();
            vfxParticleEffectVisual.Reinit();
            builtinParticleEffect.SetActive(false);
            vfxParticleEffect.SetActive(false);
        }

        if (passthroughOn)
        {
            Environment3D.SetActive(false); // Hide 3D environment
            mainCamera.clearFlags = CameraClearFlags.SolidColor; // Change camera background to solid color with alpha 0
            mainCamera.backgroundColor = solidColor;
        }
        else
        {
            Environment3D.SetActive(true); // Show 3D environment
            mainCamera.clearFlags = CameraClearFlags.Skybox; // Change camera background to skybox
        }

        string dateTime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"RecordedData_{particleEffect}_Passthrough{passthrough}_{dateTime}.csv";

        Debug.Log("Recording starting " + particleEffect + "-" + passthrough + "-" + dateTime);
        StartRecording();
        yield return StartCoroutine(ReadCSVAndMoveCamera());

        StopRecording(filename);
        Debug.Log("Recording stopped " + particleEffect + "-" + passthrough + "-" + dateTime);
    }

    /// <summary>
    /// Reads movement data from the latest CSV file and starts the camera movement.
    /// </summary>
    /// <returns>An enumerator for coroutine handling.</returns>
    private IEnumerator ReadCSVAndMoveCamera()
    {
        string newestFilePath = GetNewestMovementDataFilePath();

        if (File.Exists(newestFilePath))
        {
            // Read the CSV file
            string[] lines = File.ReadAllLines(newestFilePath);

            // Parse the CSV lines
            foreach (string line in lines)
            {
                string[] elements = line.Split(',');
                RecordedTransform rt = new RecordedTransform
                {
                    timestamp = float.Parse(elements[0], CultureInfo.InvariantCulture),
                    position = new Vector3(
                        float.Parse(elements[1], CultureInfo.InvariantCulture),
                        float.Parse(elements[2], CultureInfo.InvariantCulture),
                        float.Parse(elements[3], CultureInfo.InvariantCulture)),
                    rotation = new Quaternion(
                        float.Parse(elements[4], CultureInfo.InvariantCulture),
                        float.Parse(elements[5], CultureInfo.InvariantCulture),
                        float.Parse(elements[6], CultureInfo.InvariantCulture),
                        float.Parse(elements[7], CultureInfo.InvariantCulture))
                };
                recordedTransforms.Add(rt);
            }

            // Sort the list based on the timestamp
            recordedTransforms.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

            // Start the movement coroutine
            yield return StartCoroutine(MoveCameraThroughRecordedTransforms());
        }
        else
        {
            Debug.LogError("Could not find file at " + newestFilePath);
            debugText.text = "Could not find file at " + newestFilePath;
        }
    }

    /// <summary>
    /// Moves the camera through the recorded transforms.
    /// </summary>
    /// <returns>An enumerator for coroutine handling.</returns>
    private IEnumerator MoveCameraThroughRecordedTransforms()
    {
        for (int i = 0; i < recordedTransforms.Count - 1; i++)
        {
            // Calculate the time difference between the current and the next transform
            float timeToNext = recordedTransforms[i + 1].timestamp - recordedTransforms[i].timestamp;

            // Start moving towards the next transform
            yield return MoveToTransform(recordedTransforms[i], recordedTransforms[i + 1], timeToNext);
        }
    }

    /// <summary>
    /// Smoothly moves the camera from the start transform to the end transform over the specified duration.
    /// </summary>
    /// <param name="startTransform">The starting transform.</param>
    /// <param name="endTransform">The ending transform.</param>
    /// <param name="duration">The duration of the movement.</param>
    /// <returns>An enumerator for coroutine handling.</returns>
    private IEnumerator MoveToTransform(RecordedTransform startTransform, RecordedTransform endTransform, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            Vector3 startPosition = MovementAnchorObject.transform.TransformPoint(startTransform.position);
            Vector3 endPosition = MovementAnchorObject.transform.TransformPoint(endTransform.position);
            Quaternion startRotation = MovementAnchorObject.transform.rotation * startTransform.rotation;
            Quaternion endRotation = MovementAnchorObject.transform.rotation * endTransform.rotation;

            CameraMover.transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / duration);
            CameraMover.transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / duration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // At the end of the MoveToTransform coroutine
        CameraMover.transform.position = MovementAnchorObject.transform.TransformPoint(endTransform.position);
        CameraMover.transform.rotation = MovementAnchorObject.transform.rotation * endTransform.rotation;
    }

    /// <summary>
    /// Gets the file path of the newest movement data CSV file.
    /// </summary>
    /// <returns>The file path of the newest movement data file.</returns>
    private string GetNewestMovementDataFilePath()
    {
        string directoryPath = Application.persistentDataPath;
        string[] fileEntries = Directory.GetFiles(directoryPath, "MovementData*.csv");
        if (fileEntries.Length == 0)
        {
            Debug.LogError("No 'MovementData' CSV files found.");
            return null;
        }

        string newestFilePath = fileEntries.OrderByDescending(f => new FileInfo(f).LastWriteTime).First();
        return newestFilePath;
    }

    /// <summary>
    /// Stops all coroutines and recording when the script is disabled.
    /// </summary>
    private void OnDisable()
    {
        StopAllCoroutines();
        StopRecording("");
    }

    /// <summary>
    /// Stops all coroutines and recording when the script is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        StopAllCoroutines();
        StopRecording("");
    }

    /// <summary>
    /// Adjusts the emission rates of the particle effects.
    /// </summary>
    /// <param name="rate">The new emission rate.</param>
    private void AdjustParticleRates(float rate)
    {
        var vfxEmission = vfxParticleEffect.GetComponent<VisualEffect>();
        var builtinEmission = builtinParticleEffect.GetComponent<ParticleSystem>().emission;

        vfxEmission.SetFloat("SpawnRate", rate);
        builtinEmission.rateOverTime = rate;
    }

    /// <summary>
    /// Captures the current offset of the camera.
    /// </summary>
    void CaptureCameraOffset()
    {
        cameraLocalPositionOffset = mainCamera.transform.localPosition;
        cameraLocalRotationOffset = mainCamera.transform.localRotation;
    }

    /// <summary>
    /// Resets the positions of the camera mover and main camera.
    /// </summary>
    void ResetPositions()
    {
        CameraMover.transform.position = Vector3.zero;
        CameraMover.transform.rotation = Quaternion.identity;
        mainCamera.transform.localPosition = Vector3.zero;
        mainCamera.transform.localRotation = Quaternion.identity;
    }
}
