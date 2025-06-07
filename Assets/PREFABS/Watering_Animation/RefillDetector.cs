using UnityEngine;

public class RefillDetector : MonoBehaviour
{   
    private PourDetector pourDetector; // Private variable to hold the reference
    public string pourDetectorTag = "WateringCan";

    public float refillRate = 0.5f; // Refill 1 unit every 0.5 seconds
    private float nextRefillTime; // Stores the time when the next unit can be added
    void Start()
    {
        // Find the GameObject by tag
        GameObject pourDetectorObject = GameObject.FindWithTag(pourDetectorTag);

        if (pourDetectorObject != null)
        {
            // Get the PourDetector component from that GameObject
            pourDetector = pourDetectorObject.GetComponent<PourDetector>();
        }

        // Initialize nextRefillTime to allow immediate refill on first contact if desired
        nextRefillTime = Time.time;
    }
    void OnCollisionStay(Collision collisionInfo)
    {
        if (collisionInfo.gameObject.CompareTag("Pot"))
        {
            Debug.Log("Colliding with: " + collisionInfo.gameObject.name);

            // Check if enough time has passed since the last refill
            if (Time.time >= nextRefillTime)
            {
                // Make sure the can isn't already full before refilling
                // You'll need to know the max capacity of the watering can.
                // Let's assume PourDetector has a 'maxWaterCapacity' variable.
                if (pourDetector.currentWaterUnits < pourDetector.totalWaterUnits) // Assuming you add maxWaterCapacity to PourDetector
                {
                    pourDetector.currentWaterUnits++; // Increase water units
                    Debug.Log($"Refilled 1 unit. Current water: {pourDetector.currentWaterUnits}");

                    // Set the time for the next refill
                    nextRefillTime = Time.time + refillRate;
                }
                else
                {
                    Debug.Log("Watering can is full.");
                    // Optionally, reset nextRefillTime to prevent continuous "full" messages
                    nextRefillTime = Time.time + refillRate;
                }
            }
        }   
    }
}
