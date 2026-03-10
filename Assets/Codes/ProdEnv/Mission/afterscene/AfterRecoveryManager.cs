using UnityEngine;
using TMPro;

public class AfterRecoveryManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject objectiveBanner;

    public GameObject quiz1Panel;
    public GameObject quiz2Panel;
    public GameObject quiz3Panel;

    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;

    public GameObject achievementBanner;

    [Header("AR Phase")]
    public GameObject arRecoveryRoot;

    private int currentQuiz = 0;
    private int recoveredObjects = 0;

    void Start()
    {
        ShowObjective();
    }

    void ShowObjective()
    {
        objectiveBanner.SetActive(true);

        Invoke(nameof(StartQuiz1), 3f);
    }

    void StartQuiz1()
    {
        objectiveBanner.SetActive(false);
        currentQuiz = 1;
        quiz1Panel.SetActive(true);
    }

    void StartQuiz2()
    {
        currentQuiz = 2;
        quiz2Panel.SetActive(true);
    }

    void StartQuiz3()
    {
        currentQuiz = 3;
        quiz3Panel.SetActive(true);
    }

    public void OnCorrectAnswer()
    {
        HideAllQuizPanels();

        switch (currentQuiz)
        {
            case 1:
                feedbackText.text = "Exactly. We must check for deep cracks... Let's go in.";
                break;

            case 2:
                feedbackText.text = "That's right! Snakes and rats can be hiding in the debris.";
                break;

            case 3:
                feedbackText.text = "Perfect! Now we're ready.";
                break;
        }

        feedbackPanel.SetActive(true);
    }

    public void OnWrongAnswer()
    {
        feedbackText.text = "That's not correct. Think carefully about safety after a flood.";
        feedbackPanel.SetActive(true);
    }

    public void ContinueAfterFeedback()
    {
        feedbackPanel.SetActive(false);

        switch (currentQuiz)
        {
            case 1:
                StartQuiz2();
                break;

            case 2:
                StartQuiz3();
                break;

            case 3:
                ShowAchievement();
                break;
        }
    }

    void ShowAchievement()
    {
        achievementBanner.SetActive(true);

        Invoke(nameof(StartARPhase), 3f);
    }

    void StartARPhase()
    {
        achievementBanner.SetActive(false);
        arRecoveryRoot.SetActive(true);
    }

    void HideAllQuizPanels()
    {
        quiz1Panel.SetActive(false);
        quiz2Panel.SetActive(false);
        quiz3Panel.SetActive(false);
    }

    // Called by snake & mouse
    public void RegisterRecoveredObject()
    {
        recoveredObjects++;

        if (recoveredObjects >= 2)
        {
            CompleteRecoveryMission();
        }
    }

    void CompleteRecoveryMission()
    {
        Debug.Log("Recovery Mission Complete!");
        // Add scene transition or next phase here
    }
}