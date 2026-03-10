using UnityEngine;

public class AfterRecoveryQuizManager : MonoBehaviour
{
    [SerializeField] private GameObject[] quizPanels;
    [SerializeField] private GameObject achievementBanner;

    private int currentQuizIndex;

    public void StartQuizSequence()
    {
        currentQuizIndex = 0;

        if (quizPanels == null || quizPanels.Length == 0)
        {
            Debug.LogWarning("AfterRecoveryQuizManager: No quiz panels assigned!");
            return;
        }

        ShowQuiz();
    }

    void ShowQuiz()
    {
        for (int i = 0; i < quizPanels.Length; i++)
        {
            if (quizPanels[i] != null)
                quizPanels[i].SetActive(i == currentQuizIndex);
        }
    }

    public void OnCorrectAnswer()
    {
        currentQuizIndex++;

        if (currentQuizIndex < quizPanels.Length)
        {
            ShowQuiz();
        }
        else
        {
            ShowAchievement();
        }
    }

    void ShowAchievement()
    {
        foreach (var panel in quizPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        if (achievementBanner != null)
            achievementBanner.SetActive(true);
        else
            Debug.LogWarning("AfterRecoveryQuizManager: Achievement banner not assigned!");
    }

    public void ProceedToAR()
    {
        if (achievementBanner != null)
            achievementBanner.SetActive(false);

        Debug.Log("AfterRecoveryQuizManager: Launching Cleanup Gear AR Mission directly!");

        // BRIDGE LOGIC: Talk to the Controller and force the Cleanup Gear mode
        if (AfterRecoveryARController.Instance != null)
        {
            AfterRecoveryARController.Instance.EnableARRecovery(MissionMode.CleanupGear);
        }
    }
}