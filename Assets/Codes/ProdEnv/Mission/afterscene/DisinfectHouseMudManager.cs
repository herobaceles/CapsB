using UnityEngine;

/// <summary>
/// Aggregates cleaned mud piles for the After_03 Disinfect House AR mission.
/// Attach this to the DisinfectHouse AR house root and assign all MudPileInteraction
/// children in the inspector. When the required number of mud piles are cleaned,
/// this manager reports completion to AfterMissionManager.
/// </summary>
public class DisinfectHouseMudManager : MonoBehaviour
{
    [Header("Mud Piles")]
    [Tooltip("All mud piles that must be disinfected in this AR house.")]
    [SerializeField] private MudPileInteraction[] mudPiles;

    [Header("Progress")]
    [Tooltip("How many mud piles must be cleaned to complete the AR task.")]
    [SerializeField] private int requiredCount = 6;

    private int cleanedCount = 0;
    private bool completionReported = false;

    private void OnEnable()
    {
        SubscribeToMudEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromMudEvents();
    }

    private void SubscribeToMudEvents()
    {
        if (mudPiles == null)
            return;

        for (int i = 0; i < mudPiles.Length; i++)
        {
            var mud = mudPiles[i];
            if (mud == null)
                continue;

            mud.OnCleaned -= HandleMudCleaned;
            mud.OnCleaned += HandleMudCleaned;
        }
    }

    private void UnsubscribeFromMudEvents()
    {
        if (mudPiles == null)
            return;

        for (int i = 0; i < mudPiles.Length; i++)
        {
            var mud = mudPiles[i];
            if (mud == null)
                continue;

            mud.OnCleaned -= HandleMudCleaned;
        }
    }

    private void HandleMudCleaned(MudPileInteraction mud)
    {
        if (completionReported)
            return;

        cleanedCount++;
        int target = Mathf.Max(1, requiredCount);
        Debug.Log($"DisinfectHouseMudManager: Mud cleaned ({cleanedCount}/{target}).");

        if (cleanedCount >= target)
        {
            completionReported = true;

            var missionManager = AfterMissionManager.Instance;
            if (missionManager != null)
            {
                Debug.Log("DisinfectHouseMudManager: All mud piles cleaned; notifying AfterMissionManager for task 'after_03_disinfect_mud'.");
                missionManager.NotifyInteractionComplete("after_03_disinfect_mud");
            }
            else
            {
                Debug.LogWarning("DisinfectHouseMudManager: AfterMissionManager.Instance is null; cannot report completion.");
            }

            // Optionally shut down AR so post-AR dialogue/mission complete
            // are shown in the normal gameplay view.
            if (AfterRecoveryARController.Instance != null && AfterRecoveryARController.Instance.IsARActive)
            {
                AfterRecoveryARController.Instance.DisableAR();
            }
        }
    }
}
