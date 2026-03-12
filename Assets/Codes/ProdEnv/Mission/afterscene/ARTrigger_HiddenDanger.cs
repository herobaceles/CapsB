using UnityEngine;

public class ARTrigger_HiddenDanger : MonoBehaviour
{
    public string playerTag = "Player";
    public AfterRecoveryARController arController;

    private bool triggered = false;

    private void Awake()
    {
        // Auto-find controller if not assigned in Inspector
        if (arController == null)
        {
            arController = AfterRecoveryARController.Instance;
            
            // Fallback if Instance isn't set yet
            if (arController == null)
                arController = FindObjectOfType<AfterRecoveryARController>(true);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag(playerTag))
        {
            triggered = true;

            if (arController != null)
            {
                // Triggers the AR phase using the mode set during the Controller's Start()
                arController.EnableARRecovery(arController.currentMissionMode); 
                Debug.Log($"<color=green><b>AR Trigger:</b></color> Starting {arController.currentMissionMode} phase.");
            }
            else
            {
                Debug.LogWarning("ARTrigger_HiddenDanger: AfterRecoveryARController reference missing.");
            }

            // Hide the visual helper of the trigger
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            // Disable the collider so it doesn't block AR raycasts or taps
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
                triggerCollider.enabled = false;
        }
    }
}