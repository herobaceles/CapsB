using UnityEngine;

public class ARMissionTrigger : MonoBehaviour
{
    public GameObject arMissionUI;
    private bool triggered = false;
    public PreparingGoBagManager preparingGoBagManager; // Assign in inspector

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        Debug.Log("Something entered trigger");

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered trigger");

            // Show dialogue first, then start AR after Next is pressed
            if (preparingGoBagManager != null)
            {
                preparingGoBagManager.ShowBagFoundDialogue(() => {
                    if (arMissionUI != null)
                        arMissionUI.SetActive(false);
                    if (BeforeMissionManager.Instance != null)
                        BeforeMissionManager.Instance.StartARMission();
                    triggered = true;
                    gameObject.SetActive(false); // Hide or disable the trigger after use
                });
            }
            else
            {
                // fallback: start AR immediately if manager not assigned
                if (arMissionUI != null)
                    arMissionUI.SetActive(false);
                if (BeforeMissionManager.Instance != null)
                    BeforeMissionManager.Instance.StartARMission();
                triggered = true;
                gameObject.SetActive(false);
            }
        }
    }
}
