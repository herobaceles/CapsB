using UnityEngine;

public class WaterDetector : MonoBehaviour
{
    public bool isInWater;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
            isInWater = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
            isInWater = false;
    }
}