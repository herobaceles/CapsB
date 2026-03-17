using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mission selection hub. Lives in the MissionSelect scene.
/// Displays available missions by phase and handles loading mission scenes.
/// </summary>
public class MissionSelectManager : MonoBehaviour
{
    public static MissionSelectManager Instance { get; private set; }

    /// <summary>
    /// The selected mission - phase managers read this when their scene loads.
    /// </summary>
    public static MissionData SelectedMission { get; private set; }

    public static void SetSelectedMission(MissionData mission)
    {
        SelectedMission = mission;
    }

    [Header("Panels")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject missionSelectPanel;
    [SerializeField] private GameObject missionDetailPanel;
    [SerializeField] private GameObject loadingPanel;

    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;

    [Header("Player Info")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerGreetingText;

    [Header("Phase Tabs")]
    [SerializeField] private Button beforeTabButton;
    [SerializeField] private Button duringTabButton;
    [SerializeField] private Button afterTabButton;
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = Color.gray;

    [Header("Mission List")]
    [SerializeField] private Transform missionListContainer;
    [SerializeField] private GameObject missionButtonPrefab;

    [Header("Mission Details")]
    [SerializeField] private TMP_Text missionTitleText;
    [SerializeField] private TMP_Text missionDescriptionText;
    [SerializeField] private TMP_Text missionTaskCountText;
    [SerializeField] private Image missionIconImage;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text lockedReasonText;
    [SerializeField] private Button startMissionButton;
    [SerializeField] private GameObject completedBadge;

    [Header("Available Missions")]
    [SerializeField] private MissionData[] allMissions;

    // State
    private MissionPhase currentPhase = MissionPhase.Before;
    private MissionData selectedMission;
    private Dictionary<MissionPhase, List<MissionData>> missionsByPhase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetupUI();
        OrganizeMissionsByPhase();
        DisplayPlayerInfo();
        StartCoroutine(ShowWelcome());
    }

    private void SetupUI()
    {
        // Hide panels
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (missionSelectPanel != null) missionSelectPanel.SetActive(false);
        if (missionDetailPanel != null) missionDetailPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);

        // Setup tab buttons
        if (beforeTabButton != null)
            beforeTabButton.onClick.AddListener(() => SwitchToPhase(MissionPhase.Before));
        if (duringTabButton != null)
            duringTabButton.onClick.AddListener(() => SwitchToPhase(MissionPhase.During));
        if (afterTabButton != null)
            afterTabButton.onClick.AddListener(() => SwitchToPhase(MissionPhase.After));

        // Setup start button
        if (startMissionButton != null)
            startMissionButton.onClick.AddListener(StartSelectedMission);
    }

    private void OrganizeMissionsByPhase()
    {
        missionsByPhase = new Dictionary<MissionPhase, List<MissionData>>
        {
            { MissionPhase.Before, new List<MissionData>() },
            { MissionPhase.During, new List<MissionData>() },
            { MissionPhase.After, new List<MissionData>() }
        };

        if (allMissions == null) return;

        foreach (var mission in allMissions)
        {
            if (mission != null)
            {
                missionsByPhase[mission.phase].Add(mission);
            }
        }

        // Sort by sortOrder
        foreach (var phase in missionsByPhase.Keys.ToList())
        {
            missionsByPhase[phase] = missionsByPhase[phase].OrderBy(m => m.sortOrder).ToList();
        }
    }

    private void DisplayPlayerInfo()
    {
        if (PlayerData.Instance != null)
        {
            if (playerNameText != null)
                playerNameText.text = PlayerData.Instance.PlayerName;

            if (playerGreetingText != null)
                playerGreetingText.text = PlayerData.Instance.GetGreeting();
        }

    }

    private IEnumerator ShowWelcome()
    {
        yield return new WaitForSeconds(0.3f);

        // Skip dialogue, go directly to mission select
        ShowMissionSelect();
    }

    private void ShowMissionSelect()
    {
        if (welcomePanel != null)
            welcomePanel.SetActive(false);

        if (missionSelectPanel != null)
            missionSelectPanel.SetActive(true);

        // Default to Before phase
        SwitchToPhase(MissionPhase.Before);
    }

    #region Phase Switching

    public void SwitchToPhase(MissionPhase phase)
    {
        currentPhase = phase;

        // Update tab visuals
        UpdateTabColors();

        // Populate mission list
        PopulateMissionList(phase);

        // Hide detail panel until mission selected
        if (missionDetailPanel != null)
            missionDetailPanel.SetActive(false);

        Debug.Log($"MissionSelectManager: Switched to phase - {phase}");
    }

    private void UpdateTabColors()
    {
        if (beforeTabButton != null)
        {
            var colors = beforeTabButton.colors;
            colors.normalColor = currentPhase == MissionPhase.Before ? activeTabColor : inactiveTabColor;
            beforeTabButton.colors = colors;
        }

        if (duringTabButton != null)
        {
            var colors = duringTabButton.colors;
            colors.normalColor = currentPhase == MissionPhase.During ? activeTabColor : inactiveTabColor;
            duringTabButton.colors = colors;
        }

        if (afterTabButton != null)
        {
            var colors = afterTabButton.colors;
            colors.normalColor = currentPhase == MissionPhase.After ? activeTabColor : inactiveTabColor;
            afterTabButton.colors = colors;
        }
    }

    #endregion

    #region Mission List

    private void PopulateMissionList(MissionPhase phase)
    {
        if (missionListContainer == null) return;

        // Clear existing buttons
        foreach (Transform child in missionListContainer)
        {
            Destroy(child.gameObject);
        }

        var missions = missionsByPhase[phase];

        foreach (var mission in missions)
        {
            CreateMissionButton(mission);
        }

        // Auto-select first available mission
        var firstUnlocked = missions.FirstOrDefault(m => !IsMissionLocked(m));
        if (firstUnlocked != null)
        {
            SelectMission(firstUnlocked);
        }
    }

    private void CreateMissionButton(MissionData mission)
    {
        if (missionButtonPrefab == null) return;

        GameObject buttonObj = Instantiate(missionButtonPrefab, missionListContainer);

        // Get button component
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            MissionData missionRef = mission;
            button.onClick.AddListener(() => SelectMission(missionRef));
        }

        // Set text
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            bool isLocked = IsMissionLocked(mission);
            bool isCompleted = IsMissionCompleted(mission.missionId);

            string status = "";
            if (isCompleted) status = " ";
            else if (isLocked) status = " ";

            buttonText.text = mission.missionName + status;
        }

        // Optional: Set icon if available
        Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && mission.missionIcon != null)
        {
            iconImage.sprite = mission.missionIcon;
        }
    }

    #endregion

    #region Mission Selection

    public void SelectMission(MissionData mission)
    {
        if (mission == null) return;

        selectedMission = mission;

        if (missionDetailPanel != null)
            missionDetailPanel.SetActive(true);

        UpdateMissionDetails(mission);

        Debug.Log($"MissionSelectManager: Selected mission - {mission.missionName}");
    }

    private void UpdateMissionDetails(MissionData mission)
    {
        bool isLocked = IsMissionLocked(mission);
        bool isCompleted = IsMissionCompleted(mission.missionId);

        if (missionTitleText != null)
            missionTitleText.text = mission.missionName;

        if (missionDescriptionText != null)
            missionDescriptionText.text = mission.missionDescription;

        if (missionTaskCountText != null)
            missionTaskCountText.text = $"Tasks: {mission.tasks.Count}";

        if (missionIconImage != null)
        {
            if (mission.missionIcon != null)
            {
                missionIconImage.sprite = mission.missionIcon;
                missionIconImage.gameObject.SetActive(true);
            }
            else
            {
                missionIconImage.gameObject.SetActive(false);
            }
        }

        // Lock state
        if (lockedOverlay != null)
            lockedOverlay.SetActive(isLocked);

        if (lockedReasonText != null && isLocked)
        {
            if (!string.IsNullOrEmpty(mission.requiredMissionId))
            {
                var requiredMission = allMissions.FirstOrDefault(m => m.missionId == mission.requiredMissionId);
                string requiredName = requiredMission != null ? requiredMission.missionName : mission.requiredMissionId;
                lockedReasonText.text = $"Complete \"{requiredName}\" to unlock";
            }
            else
            {
                lockedReasonText.text = "Locked";
            }
        }

        // Start button
        if (startMissionButton != null)
            startMissionButton.interactable = !isLocked;

        // Completed badge
        if (completedBadge != null)
            completedBadge.SetActive(isCompleted);
    }

    #endregion

    #region Start Mission

    public void StartSelectedMission()
    {
        if (selectedMission == null)
        {
            Debug.LogError("MissionSelectManager: No mission selected!");
            return;
        }

        if (IsMissionLocked(selectedMission))
        {
            Debug.LogWarning("MissionSelectManager: Cannot start locked mission!");
            return;
        }

        StartMission(selectedMission);
    }

    public void StartMission(MissionData mission)
    {
        if (mission == null)
        {
            Debug.LogError("MissionSelectManager: Cannot start null mission!");
            return;
        }

        SelectedMission = mission;
        Debug.Log($"MissionSelectManager: Starting mission - {mission.missionName}");

        // Skip dialogue for now - load directly
        // TODO: Re-enable dialogue once UI is properly set up
        LoadMissionScene(mission);
        
        /* DIALOGUE DISABLED - uncomment when ready:
        if (ProdDialogueManager.Instance != null && ProdDialogueManager.Instance.IsDialogueActive == false)
        {
            Debug.Log("MissionSelectManager: ProdDialogueManager found, showing dialogue...");
            ProdDialogueManager.Instance.CreateSequence()
                .AddProfessorLine($"Alright! Starting: {mission.missionName}")
                .AddProfessorLine("Good luck out there!")
                .OnComplete(() => {
                    Debug.Log("MissionSelectManager: Dialogue complete, loading mission scene...");
                    LoadMissionScene(mission);
                })
                .Play();
        }
        else
        {
            Debug.Log("MissionSelectManager: No ProdDialogueManager, loading directly...");
            LoadMissionScene(mission);
        }
        */
    }

    private void LoadMissionScene(MissionData mission)
    {
        // Determine scene name
        string sceneName = mission.missionSceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            // Default scene names based on phase
            sceneName = mission.phase switch
            {
                MissionPhase.Before => "BeforeMission",
                MissionPhase.During => "DuringMission",
                MissionPhase.After => "AfterMission",
                _ => "BeforeMission"
            };
        }

        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Show loading
        if (missionSelectPanel != null)
            missionSelectPanel.SetActive(false);
        if (missionDetailPanel != null)
            missionDetailPanel.SetActive(false);
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // Check scene exists
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"MissionSelectManager: Scene '{sceneName}' not found in Build Settings!");

            if (ProdDialogueManager.Instance != null)
            {
                ProdDialogueManager.Instance.ShowDialogue("Professor Lingap",
                    "That mission area isn't ready yet. Let's try another one!",
                    null, () => {
                        if (loadingPanel != null) loadingPanel.SetActive(false);
                        if (missionSelectPanel != null) missionSelectPanel.SetActive(true);
                    });
            }
            yield break;
        }

        // Load async
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            if (progressText != null)
                progressText.text = (progress * 100f).ToString("0") + "%";

            if (asyncLoad.progress >= 0.9f)
            {
                if (progressBar != null)
                    progressBar.value = 1f;
                if (progressText != null)
                    progressText.text = "100%";

                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    #endregion

    #region Navigation

    public void BackToMainMenu()
    {
        Debug.Log("MissionSelectManager: Returning to main menu");
        SceneManager.LoadScene("MainMenuProd");
    }

    #endregion

    #region Progress Queries

    public bool IsMissionLocked(MissionData mission)
    {
        if (mission == null) return true;

        // First mission of each phase is always unlocked
        if (!mission.isLocked && string.IsNullOrEmpty(mission.requiredMissionId))
            return false;

        // Check if explicitly unlocked
        if (PlayerPrefs.GetInt($"Mission_{mission.missionId}_Unlocked", 0) == 1)
            return false;

        // Check if required mission is completed
        if (!string.IsNullOrEmpty(mission.requiredMissionId))
        {
            return !IsMissionCompleted(mission.requiredMissionId);
        }

        return mission.isLocked;
    }

    public bool IsMissionCompleted(string missionId)
    {
        return PlayerPrefs.GetInt($"Mission_{missionId}_Completed", 0) == 1;
    }

    /// <summary>
    /// Get all missions for a specific phase
    /// </summary>
    public List<MissionData> GetMissionsForPhase(MissionPhase phase)
    {
        if (missionsByPhase != null && missionsByPhase.ContainsKey(phase))
            return missionsByPhase[phase];
        return new List<MissionData>();
    }

    /// <summary>
    /// Get total missions completed
    /// </summary>
    public int GetTotalCompletedMissions()
    {
        int count = 0;
        if (allMissions != null)
        {
            foreach (var mission in allMissions)
            {
                if (mission != null && IsMissionCompleted(mission.missionId))
                    count++;
            }
        }
        return count;
    }

    #endregion

    #if UNITY_EDITOR
    [ContextMenu("Reset All Progress")]
    public void ResetAllProgress()
    {
        if (allMissions != null)
        {
            foreach (var mission in allMissions)
            {
                if (mission != null)
                {
                    PlayerPrefs.DeleteKey($"Mission_{mission.missionId}_Completed");
                    PlayerPrefs.DeleteKey($"Mission_{mission.missionId}_Unlocked");
                }
            }
        }
        PlayerPrefs.Save();
        Debug.Log("MissionSelectManager: All progress reset!");
    }
    #endif
}
