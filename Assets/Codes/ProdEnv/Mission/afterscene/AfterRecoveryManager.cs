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
    // CHANGE THIS NUMBER in the Inspector to however many items you want
    public int totalItemsRequired = 6; 

    private int currentQuiz = 0;
    private int recoveredObjects = 0;

    void Start()
    {
        // 1. Default to hidden danger just in case you test directly in the scene
        string savedID = "hiddendangermission"; 

        // 2. READ DIRECTLY FROM YOUR MAIN MENU SCRIPT!
        if (MissionSelectManager.SelectedMission != null)
        {
            savedID = MissionSelectManager.SelectedMission.missionId.ToLower().Trim();
            Debug.Log("AfterRecoveryManager read mission ID directly from Main Menu: " + savedID);
        }

        // 3. Apply the logic based on the actual ID
        if (savedID == "hiddendangermission")
        {
            // If it IS Mission 1, run normally
            ShowObjective();
        }
        else
        {
            // If it is Kitchen or Disinfect, FORCE all Mission 1 UI to stay hidden!
            if (objectiveBanner != null) objectiveBanner.SetActive(false);
            if (quiz1Panel != null) quiz1Panel.SetActive(false);
            if (quiz2Panel != null) quiz2Panel.SetActive(false);
            if (quiz3Panel != null) quiz3Panel.SetActive(false);
            if (feedbackPanel != null) feedbackPanel.SetActive(false);
            if (achievementBanner != null) achievementBanner.SetActive(false);
            
            // Shut this specific script down so it stops interfering with your other missions
            this.enabled = false; 
        }
    }

    void ShowObjective()
    {
        if (objectiveBanner != null) objectiveBanner.SetActive(true);
        Invoke(nameof(StartQuiz1), 3f);
    }

    void StartQuiz1()
    {
        if (objectiveBanner != null) objectiveBanner.SetActive(false);
        currentQuiz = 1;
        if (quiz1Panel != null) quiz1Panel.SetActive(true);
    }

    void StartQuiz2()
    {
        currentQuiz = 2;
        if (quiz2Panel != null) quiz2Panel.SetActive(true);
    }

    void StartQuiz3()
    {
        currentQuiz = 3;
        if (quiz3Panel != null) quiz3Panel.SetActive(true);
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

        if (feedbackPanel != null) feedbackPanel.SetActive(true);
    }

    public void OnWrongAnswer()
    {
        feedbackText.text = "That's not correct. Think carefully about safety after a flood.";
        if (feedbackPanel != null) feedbackPanel.SetActive(true);
    }

    public void ContinueAfterFeedback()
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

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
        if (achievementBanner != null) achievementBanner.SetActive(true);
        Invoke(nameof(StartARPhase), 3f);
    }

    void StartARPhase()
    {
        if (achievementBanner != null) achievementBanner.SetActive(false);
        if (arRecoveryRoot != null) arRecoveryRoot.SetActive(true);
    }

    void HideAllQuizPanels()
    {
        if (quiz1Panel != null) quiz1Panel.SetActive(false);
        if (quiz2Panel != null) quiz2Panel.SetActive(false);
        if (quiz3Panel != null) quiz3Panel.SetActive(false);
    }

    // Called by snake & mouse
    public void RegisterRecoveredObject()
    {
        recoveredObjects++;
        Debug.Log("Items collected: " + recoveredObjects + " / " + totalItemsRequired);

        // Updated check to use the variable instead of the hardcoded '2'
        if (recoveredObjects >= totalItemsRequired)
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