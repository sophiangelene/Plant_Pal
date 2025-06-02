using System.Collections;
using UnityEngine;

public class PourDetector : MonoBehaviour
{
    public int pourThreshold = 45;
    public Transform origin = null;
    public GameObject streamPrefab = null;
    public bool isPouring = false;
    private Stream currentStream = null;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        bool pourCheck = CalculatePourAngle() < pourThreshold;

        if (isPouring != pourCheck)
        {
            isPouring = pourCheck;

            if (isPouring)
            {
                StartPour();
            }
            else
            {
                EndPour();
            }
        }
    }

    private void StartPour()
    {
        print("Start");
        currentStream = CreateStream();
        currentStream.Begin();
    }

    private void EndPour()
    {
        print("End");
        currentStream.End();
        currentStream = null;
    }

    private float CalculatePourAngle()
    {
        // Might need to use `up` transform attribute 
        // return transform.forward.y * Mathf.Rad2Deg;

        Vector3 pourDirection = -origin.up; // Assuming -spoutTransform.up is the flow direction
        float angleWithWorldUp = Vector3.Angle(pourDirection, Vector3.up);

        // Watering can upright = 90
        // Pour angle decreases as watering can tilts in the direction of spout
        float pourAngle = angleWithWorldUp - 90f;
        pourAngle = Mathf.Max(0f, pourAngle);

        // print(pourAngle);
        return pourAngle;
    }

    private Stream CreateStream()
    {
        GameObject streamObject = Instantiate(streamPrefab, origin.position, Quaternion.identity, transform);
        return streamObject.GetComponent<Stream>();
    }
}
