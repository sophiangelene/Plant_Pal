// WateringCanDetector.cs
using UnityEngine;
using System.Collections.Generic;

public class WateringCanDetector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the PourDetector script from your watering can's animation/logic GameObject here.")]
    public PourDetector pourDetector;

    [Header("Detection Settings")]
    [Tooltip("The tag of the objects you want to water.")]
    public string waterReceiverTag = "WaterReceiver";

    [Tooltip("Offset applied to the hole's center for detection. Fine-tune this.")]
    public Vector3 detectionOffset = Vector3.zero;

    [Tooltip("The layer(s) the WaterReceiver objects are on for optimized physics checks.")]
    public LayerMask waterReceiverLayers;

    [Header("Gizmo Settings")]
    [Tooltip("Color of the detection box in the editor for visualization.")]
    public Color gizmoColor = new Color(0, 1, 0, 0.5f);

    private BoxCollider pourAreaCollider;
    private HashSet<WaterReceiver> currentlyDetectedReceivers = new HashSet<WaterReceiver>();
    private Collider[] hitCollidersBuffer = new Collider[10];

    // Added to track the previous state of isPouring
    private bool wasPouringLastFrame = false;

    void Awake()
    {
        pourAreaCollider = GetComponent<BoxCollider>();
        if (pourAreaCollider == null)
        {
            Debug.LogError("WateringCanDetector requires a BoxCollider component on this GameObject.", this);
            enabled = false;
            return;
        }
        pourAreaCollider.isTrigger = true; // Ensure it's a trigger for detection

        if (pourDetector == null)
        {
            Debug.LogError("WateringCanDetector: PourDetector reference is not set! Please assign it in the Inspector.", this);
            enabled = false;
        }
    }

    void Update()
    {
        PerformDetection();
    }

    private void PerformDetection()
    {
        if (pourAreaCollider == null || pourDetector == null) return; // Ensure all references are set

        bool isCurrentlyPouring = pourDetector.isPouring; // Get the current pouring state

        // --- Handle the state change for isPouring ---
        if (!isCurrentlyPouring && wasPouringLastFrame)
        {
            // If pouring just stopped (e.g., button released),
            // tell ALL currently detected receivers to stop their progress.
            foreach (WaterReceiver receiver in currentlyDetectedReceivers)
            {
                receiver.OnStopPouring();
            }
            currentlyDetectedReceivers.Clear(); // Clear the set as nothing is being poured on anymore
            wasPouringLastFrame = false;
            return; 
        }

        // --- Only proceed if isCurrentlyPouring is true ---
        if (isCurrentlyPouring)
        {
            Vector3 worldCenter = transform.TransformPoint(pourAreaCollider.center + detectionOffset);
            Vector3 worldSize = Vector3.Scale(transform.lossyScale, pourAreaCollider.size);

            HashSet<WaterReceiver> newDetectionThisFrame = new HashSet<WaterReceiver>();

            int numColliders = Physics.OverlapBoxNonAlloc(
                worldCenter,
                worldSize / 2,
                hitCollidersBuffer,
                transform.rotation,
                waterReceiverLayers
            );

            for (int i = 0; i < numColliders; i++)
            {
                Collider hitCollider = hitCollidersBuffer[i];
                if (hitCollider != null && hitCollider.CompareTag(waterReceiverTag))
                {
                    WaterReceiver receiver = hitCollider.GetComponent<WaterReceiver>();
                    if (receiver != null && pourDetector.currentWaterUnits > 0)
                    {
                        newDetectionThisFrame.Add(receiver);

                        // If this receiver was NOT detected last frame by position, it just started being watered by position
                        if (!currentlyDetectedReceivers.Contains(receiver))
                        {
                            receiver.OnStartPouring();
                        }
                        receiver.OnContinuePouring(); // Tell it to continue progress (update timer)
                    }
                }
            }

            // --- Process objects that are NO LONGER detected by position this frame ---
            foreach (WaterReceiver receiver in currentlyDetectedReceivers)
            {
                if (!newDetectionThisFrame.Contains(receiver))
                {
                    receiver.OnStopPouring(); // Tell it to stop progress
                }
            }

            // Update the set of currently detected receivers for the next frame
            currentlyDetectedReceivers = newDetectionThisFrame;
        }
        else // If isCurrentlyPouring is false but wasPouringLastFrame was also false (e.g. at start)
        {
            // Do nothing, already handled or no action required if not pouring and wasn't pouring
        }

        wasPouringLastFrame = isCurrentlyPouring; // Update for the next frame
    }


    void OnDrawGizmos()
    {
        if (pourAreaCollider == null)
        {
            pourAreaCollider = GetComponent<BoxCollider>();
            if (pourAreaCollider == null) return;
        }

        Gizmos.color = gizmoColor;
        Vector3 worldCenter = transform.TransformPoint(pourAreaCollider.center + detectionOffset);
        Vector3 worldSize = Vector3.Scale(transform.lossyScale, pourAreaCollider.size);

        // Indicate if isPouring is true (e.g., a brighter gizmo)
        if (Application.isPlaying && pourDetector != null && pourDetector.isPouring)
        {
            Gizmos.color = new Color(0, 1, 0, 0.8f); // More opaque when actively pouring
        }


        Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, worldSize);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
    }
}