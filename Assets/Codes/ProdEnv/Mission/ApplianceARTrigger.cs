using UnityEngine;

public class ApplianceARTrigger : MonoBehaviour
{
    public int triggerId; // Set this in the Inspector (0, 1, 2)
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"ApplianceARTrigger {triggerId} entered by {other.name}");
        if (triggered) return;
        if (other.CompareTag("Player"))
        {
            triggered = true;
            AppliancesTaskManager.Instance.OnARTriggerEntered(triggerId);
            // Optionally disable the trigger to prevent re-entry
            gameObject.SetActive(false);
        }
    }
}
