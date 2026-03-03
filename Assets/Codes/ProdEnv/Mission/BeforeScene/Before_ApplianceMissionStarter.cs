using UnityEngine;

/// <summary>
/// Simple trigger to start the Securing Appliances mission flow.
/// Attach to a trigger collider (IsTrigger=true) and assign the manager.
/// </summary>
public class ApplianceMissionStarter : MonoBehaviour
{
    [SerializeField] private SecuringAppliancesManager manager;
    [SerializeField] private ApplianceARPlacementManager03 arPlacementManager;
    [SerializeField] private GameObject arMissionUI;
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (arMissionUI != null)
            arMissionUI.SetActive(true);

        if (BeforeMissionManager.Instance != null)
            BeforeMissionManager.Instance.StartARMission();

        if (manager != null)
        {
            manager.gameObject.SetActive(true);

            if (arPlacementManager != null)
            {
                arPlacementManager.BeginPlacement(manager);
            }
            else
            {
                manager.StartMissionFromTrigger();
            }
        }

        gameObject.SetActive(false);
    }
}
