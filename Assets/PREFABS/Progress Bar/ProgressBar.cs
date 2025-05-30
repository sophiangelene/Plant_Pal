// Assets/Scripts/UI/WateringProgressBar.cs (or wherever you put new scripts)
using UnityEngine;
using UnityEngine.UI; // Required for Slider
using System;       // Required for Action (for events)

public class WateringProgressBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the PourDetector script from the watering can's Pour Detector GameObject here.")]
    public PourDetector pourDetector;

    [Tooltip("Drag the UI Slider GameObject for the progress bar here.")]
    public Slider progressBarUI;

    [Header("Settings")]
    [Tooltip("The total time in seconds required to fill the progress bar.")]
    public float timeToFill = 4.0f;

    // Optional: Event for when the bar is filled
    public event Action OnProgressBarFull;

    private float currentPouringTime = 0f;
    private bool wasPouringLastFrame = false; // To track state changes (for showing/hiding UI)

    void Start()
    {
        // Basic error checking
        if (pourDetector == null)
        {
            Debug.LogError("WateringProgressBar: PourDetector reference not set! Please assign it in the Inspector.", this);
            enabled = false; // Disable script if no detector
            return;
        }
        if (progressBarUI == null)
        {
            Debug.LogWarning("WateringProgressBar: Progress Bar UI Slider reference not set. Progress will not be displayed.", this);
        }

        // Initialize progress bar properties
        if (progressBarUI != null)
        {
            progressBarUI.maxValue = timeToFill;
            progressBarUI.value = 0;
            progressBarUI.gameObject.SetActive(false); // Hide the progress bar initially
        }
    }

    void Update()
    {
        // Get the current pouring status directly from PourDetector
        bool isCurrentlyPouring = pourDetector.isPouring;

        // Logic for showing/hiding the progress bar UI
        if (isCurrentlyPouring && !wasPouringLastFrame)
        {
            // Pouring just started, show the UI
            if (progressBarUI != null)
            {
                progressBarUI.gameObject.SetActive(true);
            }
        }
        else if (!isCurrentlyPouring && wasPouringLastFrame)
        {
            // Pouring just stopped, hide the UI and reset if not full
            currentPouringTime = 0f; // Reset time if pouring stops
            if (progressBarUI != null)
            {
                progressBarUI.value = 0;
                progressBarUI.gameObject.SetActive(false);
            }
        }

        // Only update progress if currently pouring
        if (isCurrentlyPouring)
        {
            currentPouringTime += Time.deltaTime;

            // Clamp time to prevent overshooting maxValue
            currentPouringTime = Mathf.Min(currentPouringTime, timeToFill);

            // Update UI
            if (progressBarUI != null)
            {
                progressBarUI.value = currentPouringTime;
            }

            // Check if progress is full
            if (currentPouringTime >= timeToFill)
            {
                Debug.Log("Pouring Complete! Progress bar full and resetting.");
                OnProgressBarFull?.Invoke(); // Trigger event for any actions

                // Action: Reset the progress bar immediately after filling
                // This ensures it goes to 0 and disappears until next full pour
                currentPouringTime = 0f;
                if (progressBarUI != null)
                {
                    progressBarUI.value = 0;
                    progressBarUI.gameObject.SetActive(false); // Hide it again
                }
            }
        }

        // Update the 'wasPouringLastFrame' for the next frame's comparison
        wasPouringLastFrame = isCurrentlyPouring;
    }

    // Public method to manually reset the progress bar (if needed by other scripts)
    public void ResetProgressBar()
    {
        currentPouringTime = 0f;
        if (progressBarUI != null)
        {
            progressBarUI.value = 0;
            progressBarUI.gameObject.SetActive(false);
        }
        wasPouringLastFrame = false;
    }
}