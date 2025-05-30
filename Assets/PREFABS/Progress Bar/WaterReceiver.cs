// WaterReceiver.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class WaterReceiver : MonoBehaviour
{
    [Header("UI & Progress Settings")]
    [Tooltip("Drag the UI Slider GameObject for this object's progress bar here.")]
    public Slider progressBarUI;

    [Tooltip("The TOTAL time in seconds required for the plant to grow through ALL stages.")]
    public float totalGrowthTime = 4.0f; // Renamed from requiredPouringTime for clarity

    [Header("Growth/Model Settings")]
    [Tooltip("An array of GameObjects representing different growth stages/models.")]
    public GameObject[] growthStages;

    [Tooltip("Thresholds (0.0 to 1.0) at which the plant advances to the NEXT stage. " +
             "Size should be (number of stages - 1). E.g., for 3 stages, thresholds at [0.5, 1.0].")]
    public float[] growthStageThresholds; // e.g., for 4 stages: [0.25f, 0.5f, 0.75f]

    [Header("Events")]
    [Tooltip("This event is called when the plant reaches its FINAL growth stage.")]
    public UnityEvent OnGrowthComplete;
    [Tooltip("This event is called when watering starts for this object.")]
    public UnityEvent OnPouringStart;
    [Tooltip("This event is called when watering stops for this object.")]
    public UnityEvent OnPouringStop;


    private float currentPouringTime = 0f; // Total accumulated watering time
    private int currentGrowthStageIndex = 0; // Currently active stage
    private bool isBeingPouredOn = false; // Is water currently hitting this plant?
    private bool isFullyGrown = false; // Is the plant at its final stage?

    void Start()
    {
        // UI Setup
        if (progressBarUI == null)
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: Progress Bar UI Slider reference is not set. Progress will not be displayed.", this);
        }
        else
        {
            progressBarUI.maxValue = totalGrowthTime;
            progressBarUI.value = currentPouringTime; // Start from 0 (or saved progress if applicable)
            progressBarUI.gameObject.SetActive(true); // Keep bar always visible
        }

        // Initialize Growth Stages
        InitializeGrowthStages();

        // Debugging Setup Checks
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError($"WaterReceiver on {gameObject.name}: No Collider found. It won't be detectable by WateringCanDetector.", this);
        }
        if (!gameObject.CompareTag("WaterReceiver"))
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: Tag is not set to 'WaterReceiver'. It might not be detected.", this);
        }

        // Verify thresholds match stages count
        if (growthStages.Length > 1 && growthStageThresholds.Length != growthStages.Length - 1)
        {
            Debug.LogError($"WaterReceiver on {gameObject.name}: Growth stage thresholds count mismatch! " +
                           $"Expected {growthStages.Length - 1} thresholds for {growthStages.Length} stages, got {growthStageThresholds.Length}.", this);
            enabled = false; // Disable if misconfigured
        }
    }

    void InitializeGrowthStages()
    {
        if (growthStages == null || growthStages.Length == 0)
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: No growth stages assigned in Inspector. Plant will not change models.", this);
            return;
        }

        // Set initial stage
        SetGrowthStage(0);
        isFullyGrown = false;
        // The bar is shown in Start(), so no need to hide here initially.
    }

    void SetGrowthStage(int newIndex)
    {
        if (newIndex < 0 || newIndex >= growthStages.Length)
        {
            Debug.LogError($"WaterReceiver on {gameObject.name}: Attempted to set invalid growth stage index: {newIndex}", this);
            return;
        }

        // Deactivate current stage
        if (currentGrowthStageIndex >= 0 && currentGrowthStageIndex < growthStages.Length && growthStages[currentGrowthStageIndex] != null)
        {
            growthStages[currentGrowthStageIndex].SetActive(false);
        }

        // Activate new stage
        currentGrowthStageIndex = newIndex;
        if (growthStages[currentGrowthStageIndex] != null)
        {
            growthStages[currentGrowthStageIndex].SetActive(true);
            Debug.Log($"{gameObject.name} changed to growth stage: {currentGrowthStageIndex}");
        }
        else
        {
            Debug.LogError($"WaterReceiver on {gameObject.name}: Growth stage at index {currentGrowthStageIndex} is null! Please assign a GameObject.", this);
        }

        // Hide progress bar if this is the final stage
        if (currentGrowthStageIndex == growthStages.Length - 1)
        {
            isFullyGrown = true;
            if (progressBarUI != null)
            {
                progressBarUI.gameObject.SetActive(false); // Hide the progress bar
            }
            Debug.Log($"{gameObject.name} reached final growth stage. Progress bar hidden.");
            OnGrowthComplete?.Invoke(); // Trigger final action event
        }
    }

    /// Called by WateringCanDetector when pouring *starts* on this object.
    public void OnStartPouring()
    {
        if (!isBeingPouredOn && !isFullyGrown) // Only start if not already pouring and not fully grown
        {
            isBeingPouredOn = true;
            OnPouringStart?.Invoke();
            Debug.Log($"{gameObject.name} started being poured on.");
        }
    }

    /// Called by WateringCanDetector when pouring *continues* on this object.
    public void OnContinuePouring()
    {
        if (isBeingPouredOn && !isFullyGrown) // Only update if currently pouring and not fully grown
        {
            currentPouringTime += Time.deltaTime;

            // Clamp time to prevent overshooting the total growth time
            currentPouringTime = Mathf.Min(currentPouringTime, totalGrowthTime);

            if (progressBarUI != null)
            {
                progressBarUI.value = currentPouringTime;
            }

            // --- Check for growth stage advancements ---
            // If there are more stages to go and current time exceeds the threshold for the next stage
            if (currentGrowthStageIndex < growthStages.Length - 1 && currentGrowthStageIndex < growthStageThresholds.Length)
            {
                float thresholdTime = totalGrowthTime * growthStageThresholds[currentGrowthStageIndex];
                if (currentPouringTime >= thresholdTime)
                {
                    SetGrowthStage(currentGrowthStageIndex + 1); // Advance to the next stage
                }
            }
        }
    }

    /// Called by WateringCanDetector when pouring *stops* on this object.
    public void OnStopPouring()
    {
        if (isBeingPouredOn && !isFullyGrown) // Only stop if it was actively being poured on and not fully grown
        {
            isBeingPouredOn = false;
            OnPouringStop?.Invoke();
            Debug.Log($"{gameObject.name} stopped being poured on. Progress saved at: {currentPouringTime:F2}s");
        }
    }

    //Resets the plant's entire lifecycle (e.g., for replanting)
    public void ResetPlantToInitialState()
    {
        currentPouringTime = 0f;
        isFullyGrown = false;
        isBeingPouredOn = false; // Not pouring when reset
        if (progressBarUI != null)
        {
            progressBarUI.value = 0;
            progressBarUI.gameObject.SetActive(true); // Show bar again
        }
        SetGrowthStage(0); // Go back to the very first model
        Debug.Log($"{gameObject.name} reset to initial growth state.");
    }
}