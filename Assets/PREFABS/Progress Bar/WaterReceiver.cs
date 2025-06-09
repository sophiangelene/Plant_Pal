// WaterReceiver.cs
using UnityEngine;
using UnityEngine.UI; // Required for Image, Slider, Button
using UnityEngine.Events; // Required for UnityEvent
using System.Collections; // Required for Coroutines
using TMPro; // Required if using TextMeshPro for text components

public class WaterReceiver : MonoBehaviour
{
    [Header("UI & Progress Settings")]
    [Tooltip("Drag the UI Slider GameObject for this object's progress bar here.")]
    public Slider progressBarUI;

    [Tooltip("The TOTAL time in seconds required for the plant to grow through ALL stages.")]
    public float totalGrowthTime = 4.0f;

    [Header("Growth/Model Settings")]
    [Tooltip("An array of GameObjects representing different growth stages/models.")]
    public GameObject[] growthStages;

    [Tooltip("Thresholds (0.0 to 1.0) at which the plant advances to the NEXT stage. " +
             "Size should be (number of stages - 1). E.g., for 3 stages, thresholds at [0.5, 1.0].")]
    public float[] growthStageThresholds;

    [Header("Events")]
    [Tooltip("This event is called when the plant reaches its FINAL growth stage.")]
    public UnityEvent OnGrowthComplete;
    [Tooltip("This event is called when watering starts for this object.")]
    public UnityEvent OnPouringStart;
    [Tooltip("This event is called when watering stops for this object.")]
    public UnityEvent OnPouringStop;

    [Header("Growth Stage Popups (Image)")]
    [Tooltip("The root GameObject for the growth stage image popup UI (e.g., the Panel).")]
    public GameObject growthPopupUIRoot;
    [Tooltip("The Image component inside the growth stage popup.")]
    public Image growthPopupImage;
    [Tooltip("Sprites to display for each stage transition. Size should match growthStageThresholds length.")]
    public Sprite[] growthPopupSprites;
    [Tooltip("How long the growth stage popup image stays on screen (in seconds).")]
    public float popupDisplayDuration = 2.0f;

    [Header("Final Fact Popup")] // For the final fact image + "Cool!" button
    [Tooltip("The root GameObject for the final fact popup UI (e.g., 'Pop Up - Pumpkin').")]
    public GameObject finalFactPopupUIRoot;
    [Tooltip("The Button component for the 'Cool!' button.")]
    public Button coolButton;

    // --- Private Variables ---
    private float currentPouringTime = 0f; // Total accumulated watering time
    private int currentGrowthStageIndex = 0; // Currently active stage
    private bool isBeingPouredOn = false; // Is water currently hitting this plant?
    private bool isFullyGrown = false; // Is the plant at its final stage?

    // --- Start Method ---
    void Start()
    {
        // --- Progress Bar UI Setup ---
        if (progressBarUI == null)
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: Progress Bar UI Slider reference is not set. Progress will not be displayed.", this);
        }
        else
        {
            progressBarUI.maxValue = totalGrowthTime;
            progressBarUI.value = currentPouringTime;
            progressBarUI.gameObject.SetActive(true); // Keep bar always visible
        }

        // --- Growth Stage Popup UI Setup (initial state) ---
        if (growthPopupUIRoot != null)
        {
            growthPopupUIRoot.SetActive(false); // Ensure popup is hidden at start
        }
        else
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: Growth Popup UI Root is not assigned. Popups will not display.", this);
        }

        // --- Final Fact Popup UI Setup (initial state) ---
        if (finalFactPopupUIRoot != null)
        {
            finalFactPopupUIRoot.SetActive(false); // Ensure popup is hidden at start
        }
        else
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: Final Fact Popup UI Root is not assigned. Final popup will not display.", this);
        }

        // --- Initialize Growth Stages (sets initial model) ---
        InitializeGrowthStages();

        // --- Add Listener to the Cool Button ---
        if (coolButton != null)
        {
            coolButton.onClick.AddListener(OnCoolButtonPressed);
            coolButton.gameObject.SetActive(false);
            Debug.Log($"[WaterReceiver] Cool Button listener added for {gameObject.name}. Button interactable: {coolButton.interactable}");
        }
        else
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: Cool Button is NOT assigned. Button functionality will be missing.", this);
        }

        // --- Debugging Setup Checks (for collider and tag) ---
        if (GetComponent<Collider>() == null)
        {
            Debug.LogError($"WaterReceiver on {gameObject.name}: No Collider found. It won't be detectable by WateringCanDetector.", this);
        }
        if (!gameObject.CompareTag("WaterReceiver"))
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: Tag is not set to 'WaterReceiver'. It might not be detected.", this);
        }

        // --- Verify thresholds match stages count ---
        if (growthStages.Length > 1 && growthStageThresholds.Length != growthStages.Length - 1)
        {
            Debug.LogError($"WaterReceiver on {gameObject.name}: Growth stage thresholds count mismatch! " +
                           $"Expected {growthStages.Length - 1} thresholds for {growthStages.Length} stages, got {growthStageThresholds.Length}. Disabling script.", this);
            enabled = false; // Disable if misconfigured
        }
    }

    // --- Initialize Growth Stages Method ---
    void InitializeGrowthStages()
    {
        if (growthStages == null || growthStages.Length == 0)
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: No growth stages assigned in Inspector. Plant will not change models.", this);
            return;
        }

        SetGrowthStage(0); // Set initial stage (doesn't show popup for initial state)
        isFullyGrown = false;
        currentPouringTime = 0; // Ensure fresh start for new plant
        if (progressBarUI != null) progressBarUI.value = 0;
    }

    // --- Set Growth Stage Method ---
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

            // Show growth stage popup for new stages (not for initial stage 0 or final stage)
            if (newIndex > 0 && newIndex < growthStages.Length) // Only show popup for intermediate stages
            {
                ShowGrowthStagePopup(newIndex); // Call the unified popup method for images
            }
        }
        else
        {
            Debug.LogError($"WaterReceiver on {gameObject.name}: Growth stage at index {currentGrowthStageIndex} is null! Please assign a GameObject.", this);
        }

        // Hide progress bar and mark as fully grown if this is the final stage
        if (currentGrowthStageIndex == growthStages.Length - 1)
        {
            isFullyGrown = true;
            if (progressBarUI != null)
            {
                progressBarUI.gameObject.SetActive(false); // Hide the progress bar
            }
            Debug.Log($"{gameObject.name} reached final growth stage. Progress bar hidden.");
            OnGrowthComplete?.Invoke(); // Trigger final action event

            ShowFinalFactPopup(); // Show the final fact popup
        }
    }

    // --- OnStartPouring Method ---
    public void OnStartPouring()
    {
        if (!isBeingPouredOn && !isFullyGrown)
        {
            isBeingPouredOn = true;
            OnPouringStart?.Invoke();
            Debug.Log($"{gameObject.name} started being poured on.");
        }
    }

    // --- OnContinuePouring Method ---
    public void OnContinuePouring()
    {
        if (isBeingPouredOn && !isFullyGrown)
        {
            currentPouringTime += Time.deltaTime;
            currentPouringTime = Mathf.Min(currentPouringTime, totalGrowthTime);

            if (progressBarUI != null)
            {
                progressBarUI.value = currentPouringTime;
            }

            // Check for growth stage advancements
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

    // --- OnStopPouring Method ---
    public void OnStopPouring()
    {
        if (isBeingPouredOn && !isFullyGrown)
        {
            isBeingPouredOn = false;
            OnPouringStop?.Invoke();
            Debug.Log($"{gameObject.name} stopped being poured on. Progress saved at: {currentPouringTime:F2}s");
        }
    }

    // --- ShowGrowthStagePopup Method ---
    /// <summary>
    /// Displays a growth stage popup image for a set duration.
    /// </summary>
    void ShowGrowthStagePopup(int stageIndex) // Unified method for image popups
    {
        if (growthPopupUIRoot == null || growthPopupImage == null || growthPopupSprites == null || growthPopupSprites.Length == 0)
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: Cannot show growth stage popup. Missing UI references or sprites.", this);
            return;
        }

        // Determine which sprite to show based on the stageIndex
        // stageIndex-1 because the array is 0-indexed and corresponds to the *transition* achieved.
        if (stageIndex - 1 >= 0 && stageIndex - 1 < growthPopupSprites.Length && growthPopupSprites[stageIndex - 1] != null)
        {
            growthPopupImage.sprite = growthPopupSprites[stageIndex - 1]; // Set the image for this stage
        }
        else
        {
            Debug.Log($"[WaterReceiver] No growth popup sprite assigned for stage {stageIndex} or sprite is null. Skipping popup display for this stage.");
            // Ensure the popup is hidden if no sprite is provided for it.
            if (growthPopupUIRoot != null)
            {
                growthPopupUIRoot.SetActive(false);
            }
            return; // Exit as no sprite to show
        }

        growthPopupUIRoot.SetActive(true); // Activate the popup panel
        StopAllCoroutines(); // Stop any previous popup hiding coroutines
        StartCoroutine(HideGrowthStagePopupAfterDelay(popupDisplayDuration)); // Start coroutine to hide after delay
        Debug.Log($"[WaterReceiver] Showing growth stage popup for stage {stageIndex}.");
    }

    // --- HideGrowthStagePopupAfterDelay Coroutine ---
    IEnumerator HideGrowthStagePopupAfterDelay(float delay) // Coroutine to hide growth stage popup
    {
        Debug.Log($"[WaterReceiver] Starting HideGrowthStagePopupAfterDelay coroutine for {delay}s.");
        yield return new WaitForSeconds(delay);

        if (growthPopupUIRoot != null)
        {
            growthPopupUIRoot.SetActive(false);
            Debug.Log("[WaterReceiver] Growth stage popup hidden.");
        }
        else
        {
            Debug.LogWarning("[WaterReceiver] HideGrowthStagePopupAfterDelay: Popup UI Root is null. It might have been destroyed or deactivated.");
        }
    }

    // --- ShowFinalFactPopup Method ---
    /// <summary>
    /// Displays the final fact popup.
    /// </summary>
    void ShowFinalFactPopup()
    {
        if (finalFactPopupUIRoot != null) // Only check the root, as image/button are children
        {
            // Hide the growth stage popup if it's still active
            if (growthPopupUIRoot != null)
            {
                growthPopupUIRoot.SetActive(false);
            }
            StopAllCoroutines(); // Stop any lingering HideGrowthStagePopupAfterDelay coroutines

            finalFactPopupUIRoot.SetActive(true); // Simply activate the parent GameObject
            Debug.Log($"[WaterReceiver] Displaying final fact popup for {gameObject.name}.");

            if (coolButton != null)
            {
                coolButton.gameObject.SetActive(true); // Make the button GameObject visible when the popup appears
                                                       
            }
        }
        else
        {
            Debug.LogWarning($"WaterReceiver on {gameObject.name}: Cannot show final fact popup. FinalFactPopupUIRoot is not assigned.", this);
        }
    }

    // --- OnCoolButtonPressed Method ---
    /// <summary>
    /// Called when the 'Cool!' button is pressed.
    /// </summary>
    public void OnCoolButtonPressed()
    {
        Debug.Log($"[WaterReceiver] 'Cool!' button pressed for {gameObject.name}. Hiding final popup.");
        if (finalFactPopupUIRoot != null)
        {
            finalFactPopupUIRoot.SetActive(false); // Hide the final popup
            Debug.Log($"[WaterReceiver] Final popup for {gameObject.name} set to inactive.");
        }
        else
        {
            Debug.LogWarning($"[WaterReceiver] OnCoolButtonPressed: FinalFactPopupUIRoot is null, cannot hide.", this);
        }

        // Destroy the button's GameObject
        if (coolButton != null)
        {
            Destroy(coolButton.gameObject);
            Debug.Log($"[WaterReceiver] Cool button GameObject destroyed for {gameObject.name}.");
        }
        else
        {
            Debug.LogWarning($"[WaterReceiver] OnCoolButtonPressed: CoolButton reference is null, cannot destroy.", this);
        }
    }

    // --- ResetPlantToInitialState Method ---
    /// <summary>
    /// Resets the plant's entire lifecycle (e.g., for replanting)
    /// </summary>
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

        if (growthPopupUIRoot != null)
        {
            growthPopupUIRoot.SetActive(false); // Hide growth popup if it was active
        }
        if (finalFactPopupUIRoot != null)
        {
            finalFactPopupUIRoot.SetActive(false); // Hide final fact popup on reset
        }

        SetGrowthStage(0); // Go back to the very first model
        Debug.Log($"{gameObject.name} reset to initial growth state.");
    }

    // --- getIsFullyGrown Method ---
    // Get status of plant
    public bool getIsFullyGrown()
    {
        return isFullyGrown;
    }
}