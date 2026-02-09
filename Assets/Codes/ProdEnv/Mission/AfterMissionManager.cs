using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the After Mission phase - recovery and rebuilding tasks.
/// Examples: First aid, damage assessment, helping neighbors, cleanup.
/// Inherits from MissionSceneManager for core functionality.
/// </summary>
public class AfterMissionManager : MissionSceneManager
{
    [Header("After Phase Specific")]
    [SerializeField] private GameObject recoveryUI;
    [SerializeField] private GameObject firstAidPanel;
    [SerializeField] private GameObject damageReportPanel;

    [Header("Community Status")]
    [SerializeField] private Slider communityHealthSlider;
    [SerializeField] private TMP_Text communityStatusText;
    [SerializeField] private TMP_Text rescuedCountText;

    private int rescuedPeople = 0;
    private int totalPeopleToRescue = 10;
    private float communityHealth = 0f;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // Setup phase-specific UI
        if (recoveryUI != null)
            recoveryUI.SetActive(true);

        if (firstAidPanel != null)
            firstAidPanel.SetActive(false);

        if (damageReportPanel != null)
            damageReportPanel.SetActive(false);

        UpdateCommunityStatus();
    }

    protected override string GetCompletionMessage()
    {
        return "Amazing work helping the community recover! Together, we rebuild stronger than before!";
    }

    /// <summary>
    /// Record a rescued person
    /// </summary>
    public void RescuePerson()
    {
        rescuedPeople++;
        UpdateRescuedCount();

        // Add points
        if (CurrentMission != null)
        {
            // Bonus points for rescues
            Debug.Log($"AfterMissionManager: Rescued person #{rescuedPeople}");
        }

        // Improve community health
        AddCommunityHealth(0.1f);
    }

    /// <summary>
    /// Update the rescued count display
    /// </summary>
    private void UpdateRescuedCount()
    {
        if (rescuedCountText != null)
        {
            rescuedCountText.text = $"Rescued: {rescuedPeople}/{totalPeopleToRescue}";
        }
    }

    /// <summary>
    /// Add to community health meter
    /// </summary>
    public void AddCommunityHealth(float amount)
    {
        communityHealth = Mathf.Clamp01(communityHealth + amount);
        UpdateCommunityStatus();
    }

    /// <summary>
    /// Update the community status display
    /// </summary>
    private void UpdateCommunityStatus()
    {
        if (communityHealthSlider != null)
            communityHealthSlider.value = communityHealth;

        if (communityStatusText != null)
        {
            if (communityHealth < 0.25f)
                communityStatusText.text = "Community: CRITICAL";
            else if (communityHealth < 0.5f)
                communityStatusText.text = "Community: RECOVERING";
            else if (communityHealth < 0.75f)
                communityStatusText.text = "Community: STABLE";
            else
                communityStatusText.text = "Community: THRIVING";
        }
    }

    /// <summary>
    /// Open the first aid mini-game or panel
    /// </summary>
    public void ShowFirstAid()
    {
        if (firstAidPanel != null)
            firstAidPanel.SetActive(true);
    }

    /// <summary>
    /// Close the first aid panel
    /// </summary>
    public void HideFirstAid()
    {
        if (firstAidPanel != null)
            firstAidPanel.SetActive(false);
    }

    /// <summary>
    /// Complete a first aid task
    /// </summary>
    public void CompleteFirstAid(string patientId)
    {
        Debug.Log($"AfterMissionManager: First aid completed for {patientId}");

        // Update objective if tracking first aid
        UpdateObjective("firstaid_" + patientId);

        // Improve community health
        AddCommunityHealth(0.15f);

        HideFirstAid();
    }

    /// <summary>
    /// Show damage assessment report
    /// </summary>
    public void ShowDamageReport()
    {
        if (damageReportPanel != null)
            damageReportPanel.SetActive(true);
    }

    /// <summary>
    /// Hide damage report
    /// </summary>
    public void HideDamageReport()
    {
        if (damageReportPanel != null)
            damageReportPanel.SetActive(false);
    }

    /// <summary>
    /// Record damage assessment for an area
    /// </summary>
    public void RecordDamage(string areaId, int damageLevel)
    {
        Debug.Log($"AfterMissionManager: Recorded damage level {damageLevel} for area {areaId}");

        // Update objective
        UpdateObjective("damage_" + areaId);

        // Small contribution to community health (knowledge helps recovery)
        AddCommunityHealth(0.05f);
    }

    /// <summary>
    /// Set the total number of people to rescue (for tracking)
    /// </summary>
    public void SetTotalToRescue(int total)
    {
        totalPeopleToRescue = total;
        UpdateRescuedCount();
    }

    /// <summary>
    /// Get current rescue progress
    /// </summary>
    public float GetRescueProgress()
    {
        if (totalPeopleToRescue <= 0) return 1f;
        return (float)rescuedPeople / totalPeopleToRescue;
    }
}
