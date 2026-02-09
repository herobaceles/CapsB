using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the During Mission phase - response and evacuation tasks.
/// Examples: Navigating flooded areas, rescuing people, finding safe routes.
/// Inherits from MissionSceneManager for core functionality.
/// </summary>
public class DuringMissionManager : MissionSceneManager
{
    [Header("During Phase Specific")]
    [SerializeField] private GameObject emergencyUI;
    [SerializeField] private GameObject waterLevelIndicator;
    [SerializeField] private Slider waterLevelSlider;
    [SerializeField] private TMP_Text waterLevelText;
    [SerializeField] private GameObject dangerWarning;

    [Header("Timer (Optional)")]
    [SerializeField] private bool useTimer = false;
    [SerializeField] private float missionTimeLimit = 300f; // 5 minutes default
    [SerializeField] private TMP_Text timerText;

    private float remainingTime;
    private bool timerActive = false;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // Setup phase-specific UI
        if (emergencyUI != null)
            emergencyUI.SetActive(true);

        if (dangerWarning != null)
            dangerWarning.SetActive(false);

        // Initialize timer
        if (useTimer)
        {
            remainingTime = missionTimeLimit;
            timerActive = true;
        }
    }

    protected override void Update()
    {
        base.Update();

        // Update timer
        if (timerActive && IsMissionActive && !IsPaused)
        {
            remainingTime -= Time.deltaTime;

            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }

            if (remainingTime <= 0)
            {
                OnTimeExpired();
            }
            else if (remainingTime <= 30f)
            {
                // Warning when time is low
                if (dangerWarning != null)
                    dangerWarning.SetActive(true);
            }
        }
    }

    protected override string GetCompletionMessage()
    {
        return "You made it through safely! Quick thinking and staying calm during emergencies is critical!";
    }

    /// <summary>
    /// Update the water level display
    /// </summary>
    public void SetWaterLevel(float level)
    {
        level = Mathf.Clamp01(level);

        if (waterLevelSlider != null)
            waterLevelSlider.value = level;

        if (waterLevelText != null)
        {
            if (level < 0.3f)
                waterLevelText.text = "Water Level: LOW";
            else if (level < 0.7f)
                waterLevelText.text = "Water Level: MEDIUM";
            else
                waterLevelText.text = "Water Level: HIGH";
        }

        // Show danger warning at high levels
        if (dangerWarning != null)
            dangerWarning.SetActive(level > 0.7f);
    }

    /// <summary>
    /// Show water level indicator
    /// </summary>
    public void ShowWaterLevel()
    {
        if (waterLevelIndicator != null)
            waterLevelIndicator.SetActive(true);
    }

    /// <summary>
    /// Hide water level indicator
    /// </summary>
    public void HideWaterLevel()
    {
        if (waterLevelIndicator != null)
            waterLevelIndicator.SetActive(false);
    }

    /// <summary>
    /// Called when timer runs out
    /// </summary>
    protected virtual void OnTimeExpired()
    {
        timerActive = false;

        Debug.Log("DuringMissionManager: Time expired!");

        // Skip dialogue - return to mission select directly
        // TODO: Add DialoguePanel to mission scenes if dialogue is needed
        ReturnToMissionSelect();
    }

    /// <summary>
    /// Add bonus time
    /// </summary>
    public void AddBonusTime(float seconds)
    {
        if (useTimer)
        {
            remainingTime += seconds;
            Debug.Log($"DuringMissionManager: Added {seconds}s bonus time. New total: {remainingTime}s");
        }
    }

    /// <summary>
    /// Pause the timer
    /// </summary>
    public void PauseTimer()
    {
        timerActive = false;
    }

    /// <summary>
    /// Resume the timer
    /// </summary>
    public void ResumeTimer()
    {
        if (useTimer && IsMissionActive)
            timerActive = true;
    }

    public override void PauseMission()
    {
        base.PauseMission();
        PauseTimer();
    }

    public override void ResumeMission()
    {
        base.ResumeMission();
        ResumeTimer();
    }
}
