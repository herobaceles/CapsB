using UnityEngine;
using TMPro;
using System;

public class BannerManager : MonoBehaviour
{
    [Header("Objective Banner")]
    [SerializeField] private GameObject objectiveBannerRoot;
    [SerializeField] private TMP_Text objectiveBannerText;

    [Header("Achievement Banner")]
    [SerializeField] private GameObject achievementBannerRoot;
    [SerializeField] private TMP_Text achievementBannerText;

    public void ShowObjective(string message, float seconds)
    {
        // Display objective banner; implementation will show the UI and hide it after `seconds`.
    }

    public void ShowAchievement(string message, Action onDismiss = null)
    {
        // Display achievement banner and call onDismiss when dismissed.
        onDismiss?.Invoke();
    }

    public void ShowLevelComplete(string message)
    {
        // Display level complete banner.
    }

    // Initialize from existing manager/inspector values
    public void Initialize(GameObject objectiveRoot, TMP_Text objectiveText, GameObject achievementRoot, TMP_Text achievementText)
    {
        if (objectiveRoot != null) objectiveBannerRoot = objectiveRoot;
        if (objectiveText != null) objectiveBannerText = objectiveText;
        if (achievementRoot != null) achievementBannerRoot = achievementRoot;
        if (achievementText != null) achievementBannerText = achievementText;
    }
}
