using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Unity.Collections;
using UnityEngine.XR.OpenXR.Features.Meta;

/// <summary>
/// RefreshRateSetter is a script designed to set the display refresh rate 
/// of the Meta Quest headset to the highest possible value available. 
/// Although the Meta Quest supports up to 120Hz, this script currently sets it to 90Hz.
/// </summary>
public class RefreshRateSetter : MonoBehaviour
{
    /// <summary>
    /// Called when the script instance is being loaded. Initiates the process 
    /// of setting the highest possible refresh rate.
    /// </summary>
    void Start()
    {
        SetHighestRefreshRate();
    }

    /// <summary>
    /// Sets the display refresh rate of the XR display subsystem to the highest 
    /// available rate. Logs success or failure of the operation.
    /// </summary>
    void SetHighestRefreshRate()
    {
        // Retrieve the active XR display subsystem
        var displaySubsystem = XRGeneralSettings.Instance
            .Manager
            .activeLoader
            .GetLoadedSubsystem<XRDisplaySubsystem>();

        // Check if the display subsystem is available
        if (displaySubsystem == null)
        {
            Debug.LogError("Display subsystem not available.");
            return;
        }

        // Attempt to retrieve the supported display refresh rates
        if (displaySubsystem.TryGetSupportedDisplayRefreshRates(Allocator.Temp, out var refreshRates))
        {
            float highestRate = 0f;

            // Iterate through the supported refresh rates to find the highest one
            foreach (var rate in refreshRates)
            {
                if (rate > highestRate)
                {
                    highestRate = rate;
                }
            }

            // Attempt to set the display refresh rate to the highest found rate
            if (highestRate > 0f && displaySubsystem.TryRequestDisplayRefreshRate(highestRate))
            {
                Debug.Log($"Successfully set refresh rate to {highestRate} Hz.");
            }
            else
            {
                Debug.LogError($"Failed to set refresh rate to {highestRate} Hz.");
            }
        }
        else
        {
            Debug.LogError("Unable to retrieve supported refresh rates.");
        }
    }
}
