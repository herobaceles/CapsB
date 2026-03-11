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
            arController = FindObjectOfType<AfterRecoveryARController>(true);

            if (arController != null)
                Debug.Log("ARTrigger_HiddenDanger: Found AfterRecoveryARController automatically.");
            else
                Debug.LogWarning("ARTrigger_HiddenDanger: Could not find AfterRecoveryARController in scene.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag(playerTag))
        {
            triggered = true;

            if (arController != null)
            {
                // CRITICAL FIX: Pass the controller's current mode to resolve error CS7036.
                // This respects whichever mission the player selected in the Main Menu!
                arController.EnableARRecovery(arController.currentMissionMode); 
                Debug.Log(arController.currentMissionMode + " AR Phase Started via Trigger!");
            }
            else
            {
                Debug.LogWarning("ARTrigger_HiddenDanger: arController reference missing.");
            }

            // Hide the trigger object if it has a mesh
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = false;

            // CRITICAL FIX: Turn off the collider so it doesn't block AR finger taps!
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
                triggerCollider.enabled = false;
        }
    }
}