using UnityEngine;

public class ARMission02Trigger : MonoBehaviour
{
    public GameObject arMissionUI;
    public BreakerTaskManager breakerTaskManager; // Assign in inspector (next mission manager)
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Reset joystick to prevent stuck movement
        var joystick = GameObject.FindObjectOfType<Joystick>();
        if (joystick != null)
        {
            joystick.ResetJoystick();
            Debug.Log("mission_02_trigger: Joystick reset.");
        }
                // Stop player movement if possible
                var playerController = other.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.SetMovementEnabled(false);
                    Debug.Log("mission_02_trigger: Player movement disabled.");
                }
                // Optionally, lock movement in ARMissionManager if used
                if (ARMissionManager.Instance != null)
                {
                    // ARMissionManager uses a private movementLocked flag, set it if needed
                    // If you want to expose a method, add one to ARMissionManager
                    Debug.Log("mission_02_trigger: ARMissionManager present (movement lock handled internally).");
                }
        Debug.Log($"mission_02_trigger: Trigger entered by {other.name}, tag: {other.tag}");
        if (triggered)
            return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("mission_02_trigger: Player detected, opening AR UI and starting breaker task.");

            // Show AR UI for next mission if needed
            if (arMissionUI != null)
            {
                arMissionUI.SetActive(false);
                Debug.Log("mission_02_trigger: AR UI kept hidden to avoid full-screen overlay.");
            }

            // Ensure AR session/camera is enabled (critical for AR to work)
            if (BeforeMissionManager.Instance != null)
            {
                BeforeMissionManager.Instance.StartARMission();
                Debug.Log("mission_02_trigger: Called BeforeMissionManager.Instance.StartARMission().");
            }

            // Start the next mission manager (e.g., breaker task)
            if (breakerTaskManager != null)
            {
                breakerTaskManager.StartBreakerTask();
                Debug.Log("mission_02_trigger: BreakerTaskManager started.");
            }

            triggered = true;
            gameObject.SetActive(false); // Disable trigger after use
        }
    }
}
