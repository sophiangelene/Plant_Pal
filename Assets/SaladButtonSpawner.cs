using UnityEngine;
using UnityEngine.UI;

public class SaladButtonSpawner : MonoBehaviour
{
    public WaterReceiver plant1;
    public WaterReceiver plant2;
    public WaterReceiver plant3;
    public GameObject makeSaladButton;

    private bool buttonShown = false;

    void Update()
    {
        if (!buttonShown &&
            plant1.getIsFullyGrown() &&
            plant2.getIsFullyGrown() &&
            plant3.getIsFullyGrown())
        {
            makeSaladButton.SetActive(true);  // Show the button
            buttonShown = true;
            Debug.Log("All plants fully grown! Salad button spawned.");
        }
    }
}
