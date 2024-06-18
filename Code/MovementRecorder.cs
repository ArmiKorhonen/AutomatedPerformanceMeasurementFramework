using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine.VFX;
using System;

/// <summary>
/// MovementRecorder is a script used to record the movement of the VR headset (and optionally controllers)
/// relative to a specified anchor object within the scene. This ensures that the recorded movements can be
/// accurately recreated regardless of changes in the user's real-world position.
/// </summary>
public class MovementRecorder : MonoBehaviour
{
    // Reference to the main camera representing the headset
    public GameObject mainCamera;
    // Reference to the object used as an anchor for relative positioning
    public GameObject MovementAnchorObject;
    // References to the left and right hand controllers and their stabilized versions
    public GameObject leftController;
    public GameObject leftControllerStabilized;
    public GameObject rightController;
    public GameObject rightControllerStabilized;
    // References to the left and right hands
    public GameObject leftHand;
    public GameObject rightHand;
    // Reference to a particle effect that indicates recording status
    public GameObject particleEffect;

    // List to store recorded data as strings
    private List<string> recordedData;
    // Flag to indicate whether recording is active
    private bool isRecording = false;

    /// <summary>
    /// Called when the script instance is being loaded. Initializes the recorded data list.
    /// </summary>
    void Start()
    {
        recordedData = new List<string>();
    }

    /// <summary>
    /// Called once per frame. Records the position and rotation of the main camera (headset) if recording is active.
    /// </summary>
    void Update()
    {
        // Exit early if not recording
        if (!isRecording) return;

        // Record position and rotation of the main camera (headset)
        string data = Time.time + "," + 
                      FormatTransformData(mainCamera.transform);
                      // Uncomment the following lines to record additional data from controllers and hands
                      //FormatTransformData(leftController.transform) + "," + 
                      //FormatTransformData(leftControllerStabilized.transform) + "," + 
                      //FormatTransformData(rightController.transform) + "," + 
                      //FormatTransformData(rightControllerStabilized.transform) + "," + 
                      //FormatTransformData(leftHand.transform) + "," + 
                      //FormatTransformData(rightHand.transform);

        // Add recorded data to the list
        recordedData.Add(data);
    }

    /// <summary>
    /// Public method to toggle the recording state. Can be called by UI elements.
    /// </summary>
    /// <param name="shouldRecord">Boolean indicating whether recording should start or stop.</param>
    public void ToggleRecording(bool shouldRecord)
    {
        if (shouldRecord && !isRecording)
        {
            StartRecording();
        }
        else if (!shouldRecord && isRecording)
        {
            StopRecording();
        }
    }

    /// <summary>
    /// Starts the recording process. Resets positions, activates the particle effect, and initializes the recorded data list.
    /// </summary>
    private void StartRecording()
    {
        ResetPositions();
        Debug.Log("Starting recording movement");
        isRecording = true;
        recordedData = new List<string>();
        particleEffect.SetActive(true);
    }

    /// <summary>
    /// Stops the recording process. Saves the recorded data to a CSV file and deactivates the particle effect.
    /// </summary>
    private void StopRecording()
    {
        Debug.Log("Stopping recording movement");
        isRecording = false;

        // Generate a unique filename based on the current date and time
        string dateTime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = "MovementData_" + dateTime + ".csv";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        // Write recorded data to the file
        File.WriteAllLines(filePath, recordedData);
        particleEffect.SetActive(false);
    }

    /// <summary>
    /// Formats the position and rotation of a transform relative to the MovementAnchorObject into a comma-separated string.
    /// </summary>
    /// <param name="transform">The transform to format.</param>
    /// <returns>A string representing the relative position and rotation.</returns>
    string FormatTransformData(Transform transform)
    {
        Vector3 relativePosition = MovementAnchorObject.transform.InverseTransformPoint(transform.position);
        Quaternion relativeRotation = Quaternion.Inverse(MovementAnchorObject.transform.rotation) * transform.rotation;

        return relativePosition.x + "," + relativePosition.y + "," + relativePosition.z + "," +
            relativeRotation.x + "," + relativeRotation.y + "," + relativeRotation.z + "," + relativeRotation.w;
    }

    /// <summary>
    /// Resets the position and rotation of the main camera (headset) to the origin.
    /// </summary>
    void ResetPositions()
    {
        mainCamera.transform.localPosition = Vector3.zero;
        mainCamera.transform.localRotation = Quaternion.identity;
    }
}
