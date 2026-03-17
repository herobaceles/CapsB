using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles the During-phase gameplay loop. Keeps the top-down map active
/// and routes the player into AR flood mini-scenes for hazard interactions.
/// Orchestrates story dialogue, map tutorial, and task progression.
/// </summary>
public class DuringMissionManager : MissionSceneManager
{
    public new static DuringMissionManager Instance { get; private set; }

    [System.Serializable]
    private class FloodZoneBinding
    {
        [SerializeField] private string taskId;
        [SerializeField] private GameObject zoneRoot;

        public string TaskId => taskId;
        public GameObject ZoneRoot => zoneRoot;
    }

    [System.Serializable]
    private class ARTaskBinding
    {
        [SerializeField] private string taskId;
        [SerializeField] private ARTaskBase arTask;

        public string TaskId => taskId;
        public ARTaskBase ARTask => arTask;
    }

    [Header("During Phase UI")]
    [SerializeField] private GameObject mapUI;
    [SerializeField] private GameObject miniSceneUI;
    [SerializeField] private GameObject evacuationMarker;

    [Header("Map Display")]
    [SerializeField] private DuringMissionMapDisplay mapDisplay;

    [Header("Flood Zone Bindings")]
    [SerializeField] private List<FloodZoneBinding> floodZoneBindings = new List<FloodZoneBinding>();

    [Header("AR Task Bindings")]
    [SerializeField] private List<ARTaskBinding> arTaskBindings = new List<ARTaskBinding>();

    [Header("Intro Dialogue")]
    [SerializeField] private bool playIntroOnStart = true;
    [SerializeField] private float introDelay = 0.5f;
    [SerializeField] private float introLineDuration = 3f;
    [SerializeField] private NPCFollower introNPC;
    [SerializeField, TextArea] private string[] introDialogueLines;

    [Header("Map Tutorial Task")]
    [Tooltip("Task ID that requires player to open backpack and view map")]
    [SerializeField] private string mapTutorialTaskId = "tutorial_open_map";
    [SerializeField] private bool mapTutorialCompleted;

    [Header("Mission Timer")]
    [SerializeField] private TMP_Text missionTimerText;
    [SerializeField] private GameObject missionTimeoutPanel;
    [SerializeField] private Button missionTimeoutRestartButton;

    [Header("Events")]
    public UnityEvent OnBackpackOpened;
    public UnityEvent OnMapViewed;

    private ARTaskBase activeARTask;
    private bool isMiniSceneActive;
    private bool introDialoguePlayed;
    private Coroutine introDialogueRoutine;
    private bool backpackOpenedThisTask;
    private bool mapViewedThisTask;

    private bool useMissionTimer;
    private float missionTimeLimit;
    private Coroutine missionTimerRoutine;
    private float missionElapsedTime;
    private bool missionTimerStarted;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        ResetPlayerState();

        if (introNPC == null)
            introNPC = FindObjectOfType<NPCFollower>();

        if (mapDisplay == null)
            mapDisplay = FindObjectOfType<DuringMissionMapDisplay>();

        if (missionTimeoutRestartButton != null)
        {
            missionTimeoutRestartButton.onClick.RemoveListener(RestartMissionFromTimeout);
            missionTimeoutRestartButton.onClick.AddListener(RestartMissionFromTimeout);
        }
    }

    /// <summary>
    /// Ensure we have a mission loaded even if MissionSelectManager was not set.
    /// Falls back to the Resources asset Mission_During_01 if nothing else is provided.
    /// </summary>
    protected override void LoadMission()
    {
        base.LoadMission();

        if (currentMission == null)
        {
            var fromResources = Resources.Load<MissionData>("Missions/Mission_During_01");
            if (fromResources != null)
            {
                currentMission = fromResources;
                Debug.Log("DuringMissionManager: Loaded fallback Mission_During_01 from Resources.");
            }
        }

        if (currentMission == null)
        {
            Debug.LogError("DuringMissionManager: No mission found. Set MissionSelectManager.SelectedMission or assign a fallback MissionData.");
        }
    }

    protected override void Start()
    {
        base.Start();
        ConfigureDefaultUIState();
        TryPlayIntroDialogue();
        SubscribeToMapEvents();
    }

    protected override void StartMission()
    {
        if (CurrentMission != null)
        {
            useMissionTimer = CurrentMission.useMissionTimer;
            missionTimeLimit = CurrentMission.missionTimeLimitSeconds;
        }
        else
        {
            useMissionTimer = false;
            missionTimeLimit = 0f;
        }

        base.StartMission();
        TryPlayIntroDialogue();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
            Instance = null;

        UnsubscribeFromMapEvents();
        StopMissionTimer();
    }

    private void SubscribeToMapEvents()
    {
        if (mapDisplay != null)
        {
            mapDisplay.OnMapOpened.AddListener(HandleMapOpened);
            mapDisplay.OnMapClosed.AddListener(HandleMapClosed);
        }
    }

    private void UnsubscribeFromMapEvents()
    {
        if (mapDisplay != null)
        {
            mapDisplay.OnMapOpened.RemoveListener(HandleMapOpened);
            mapDisplay.OnMapClosed.RemoveListener(HandleMapClosed);
        }
    }

    private void ConfigureDefaultUIState()
    {
        if (mapUI != null)
            mapUI.SetActive(true);
        if (miniSceneUI != null)
            miniSceneUI.SetActive(false);
        if (evacuationMarker != null)
            evacuationMarker.SetActive(false);

        if (missionTimeoutPanel != null)
            missionTimeoutPanel.SetActive(false);
    }

    private void ShowMapUI()
    {
        if (mapDisplay != null)
        {
            mapDisplay.ShowMap();
        }
    }

    private void ResetPlayerState()
    {
        introDialoguePlayed = false;
        mapTutorialCompleted = false;
        backpackOpenedThisTask = false;
        mapViewedThisTask = false;
        missionTimerStarted = false;

        if (introDialogueRoutine != null)
        {
            StopCoroutine(introDialogueRoutine);
            introDialogueRoutine = null;
        }
    }

    private void TryPlayIntroDialogue()
    {
        if (!playIntroOnStart || introDialoguePlayed)
            return;

        if (introNPC == null)
        {
            Debug.LogWarning("DuringMissionManager: Intro NPC not assigned for intro dialogue.");
            return;
        }

        if (introDialogueLines == null || introDialogueLines.Length == 0)
            return;

        if (!gameObject.activeInHierarchy)
            return;

        introDialoguePlayed = true;

        if (introDialogueRoutine != null)
            StopCoroutine(introDialogueRoutine);

        introDialogueRoutine = StartCoroutine(PlayIntroDialogueRoutine());
        Debug.Log($"DuringMissionManager: Queued intro dialogue ({introDialogueLines.Length} lines).");
    }

    private IEnumerator PlayIntroDialogueRoutine()
    {
        if (introDelay > 0f)
            yield return new WaitForSeconds(introDelay);

        foreach (string line in introDialogueLines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                Debug.Log($"DuringMissionManager: Intro line -> {line}");
                introNPC.SpeakLine(line, introLineDuration);
            }
        }

        introDialogueRoutine = null;
    }

    /// <summary>
    /// During-phase override: if the current task has an AR mini-scene bound, enter the zone and start it instead of auto-completing.
    /// Otherwise, fall back to the base behavior (complete on trigger).
    /// </summary>
    public override void OnTriggerActivated(string taskId)
    {
        if (!isMissionActive)
            return;

        bool isCurrentTask = currentTask != null &&
            string.Equals(currentTask.taskId, taskId, System.StringComparison.OrdinalIgnoreCase);

        if (isCurrentTask && activeARTask != null)
        {
            Debug.Log($"DuringMissionManager: Trigger entered for AR task {taskId}, starting mini-scene.");
            EnterFloodZone(taskId);
            StartActiveARTask();
            return;
        }

        base.OnTriggerActivated(taskId);
    }

    public override void StartTask(int taskIndex)
    {
        base.StartTask(taskIndex);
        HighlightFloodZone(CurrentTask?.taskId);

        // Reset tutorial tracking for new task
        backpackOpenedThisTask = false;
        mapViewedThisTask = false;

        // Check if this is the map tutorial task
        if (CurrentTask != null && IsMapTutorialTask(CurrentTask.taskId))
        {
            Debug.Log("DuringMissionManager: Map tutorial task started. Waiting for player to open backpack and view map.");
        }

        // Look for AR task binding
        activeARTask = FindARTask(CurrentTask?.taskId);
        if (activeARTask != null)
        {
            Debug.Log($"DuringMissionManager: AR task found for {CurrentTask?.taskId}");
        }

        // Update map display
        if (mapDisplay != null)
        {
            mapDisplay.RefreshTaskMarkers();
        }

        if (evacuationMarker != null)
            evacuationMarker.SetActive(taskIndex >= TotalTasks - 1);
    }

    private bool IsMapTutorialTask(string taskId)
    {
        return !string.IsNullOrWhiteSpace(mapTutorialTaskId) &&
               string.Equals(taskId, mapTutorialTaskId, System.StringComparison.OrdinalIgnoreCase);
    }

    private ARTaskBase FindARTask(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId)) return null;

        foreach (var binding in arTaskBindings)
        {
            if (binding != null && binding.ARTask != null &&
                string.Equals(binding.TaskId, taskId, System.StringComparison.OrdinalIgnoreCase))
            {
                return binding.ARTask;
            }
        }
        return null;
    }

    #region Map Tutorial Handlers

    private void HandleMapOpened()
    {
        mapViewedThisTask = true;
        OnMapViewed?.Invoke();

        Debug.Log("DuringMissionManager: Map opened.");

        // Start the mission timer on first map open
        if (useMissionTimer && IsMissionActive && !missionTimerStarted)
        {
            missionTimerStarted = true;
            StartMissionTimer();
        }

        // Check if tutorial task should complete
        CheckMapTutorialCompletion();
    }

    private void HandleMapClosed()
    {
        Debug.Log("DuringMissionManager: Map closed.");
    }

    /// <summary>
    /// Called when backpack UI is opened. Wire this to your backpack button.
    /// </summary>
    public void NotifyBackpackOpened()
    {
        backpackOpenedThisTask = true;
        OnBackpackOpened?.Invoke();

        Debug.Log("DuringMissionManager: Backpack opened.");

        CheckMapTutorialCompletion();
    }

    private void CheckMapTutorialCompletion()
    {
        if (CurrentTask == null || !IsMissionActive)
            return;

        if (!IsMapTutorialTask(CurrentTask.taskId))
            return;

        if (mapTutorialCompleted)
            return;

        // Both conditions met?
        if (backpackOpenedThisTask && mapViewedThisTask)
        {
            mapTutorialCompleted = true;
            Debug.Log("DuringMissionManager: Map tutorial completed!");

            // Speak completion line
            if (introNPC != null)
            {
                introNPC.SpeakLine("Great! Keep that map handy during our evacuation.", 3f);
            }

            // Complete the task after a short delay
            StartCoroutine(CompleteMapTutorialAfterDelay(1.5f));
        }
    }

    private IEnumerator CompleteMapTutorialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (CurrentTask != null && IsMapTutorialTask(CurrentTask.taskId))
        {
            CompleteCurrentTask();
        }
    }

    #endregion

    /// <summary>
    /// Called by flood zone triggers when the player steps into a hazard area.
    /// Switches UI from the map to the mini-scene representation.
    /// </summary>
    public void EnterFloodZone(string taskId)
    {
        if (!IsMissionActive || CurrentTask == null)
            return;

        if (!string.Equals(taskId, CurrentTask.taskId, System.StringComparison.OrdinalIgnoreCase))
            return;

        ToggleMiniScene(true);
    }

    /// <summary>
    /// Call when the mini-scene decisions are done and the player returns to the map.
    /// </summary>
    public void ExitFloodZone()
    {
        ToggleMiniScene(false);
    }

    /// <summary>
    /// Completes the active task after the flood zone interactions succeed.
    /// Typically invoked by the mini-scene controller.
    /// </summary>
    public void CompleteActiveZone()
    {
        if (!IsMissionActive || CurrentTask == null)
            return;

        CompleteCurrentTask();
        ToggleMiniScene(false);
    }

    private void ToggleMiniScene(bool active)
    {
        isMiniSceneActive = active;

        if (mapUI != null)
            mapUI.SetActive(!active);
        if (miniSceneUI != null)
            miniSceneUI.SetActive(active);

        if (mapDisplay != null)
        {
            if (active)
                mapDisplay.HideMap();
            else
                mapDisplay.ShowMap();
        }
    }

    private void HighlightFloodZone(string taskId)
    {
        for (int i = 0; i < floodZoneBindings.Count; i++)
        {
            var binding = floodZoneBindings[i];
            if (binding == null || binding.ZoneRoot == null)
                continue;

            bool enable = !string.IsNullOrWhiteSpace(taskId) &&
                          string.Equals(binding.TaskId, taskId, System.StringComparison.OrdinalIgnoreCase);

            binding.ZoneRoot.SetActive(enable);
        }
    }

    private void HandleMissionFailure(string reason)
    {
        Debug.LogWarning($"DuringMissionManager: Mission failed - {reason}");
        isMissionActive = false;
        StopMissionTimer();
        ToggleMiniScene(false);
        ReturnToMissionSelect();
    }

    #region Mission Timer

    private void StartMissionTimer()
    {
        if (!useMissionTimer || missionTimeLimit <= 0f)
            return;

        if (missionTimerRoutine != null)
            StopCoroutine(missionTimerRoutine);

        missionElapsedTime = 0f;

        if (missionTimerText != null)
            missionTimerText.gameObject.SetActive(true);

        UpdateMissionTimerUI();

        if (missionTimeoutPanel != null)
            missionTimeoutPanel.SetActive(true);

        missionTimerRoutine = StartCoroutine(MissionTimerRoutine());
    }

    private void StopMissionTimer()
    {
        if (missionTimerRoutine != null)
        {
            StopCoroutine(missionTimerRoutine);
            missionTimerRoutine = null;
        }
    }

    private IEnumerator MissionTimerRoutine()
    {
        while (IsMissionActive && missionElapsedTime < missionTimeLimit)
        {
            missionElapsedTime += Time.deltaTime;
            UpdateMissionTimerUI();
            yield return null;
        }

        if (!IsMissionActive)
        {
            missionTimerRoutine = null;
            yield break;
        }

        missionTimerRoutine = null;
        OnMissionTimerExpired();
    }

    private void UpdateMissionTimerUI()
    {
        if (missionTimerText == null || missionTimeLimit <= 0f)
            return;

        float remaining = Mathf.Max(0f, missionTimeLimit - missionElapsedTime);
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        missionTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnMissionTimerExpired()
    {
        Debug.LogWarning("DuringMissionManager: Mission timer expired.");
        HandleMissionTimeout();
    }

    private void HandleMissionTimeout()
    {
        if (!IsMissionActive)
            return;

        isMissionActive = false;
        StopMissionTimer();

        if (activeARTask != null && activeARTask.IsActive)
        {
            activeARTask.CancelTask();
        }

        if (isMiniSceneActive)
        {
            ToggleMiniScene(false);
        }

        if (DuringMissionStoryDirector.Instance != null)
        {
            DuringMissionStoryDirector.Instance.ClearQueue();
        }

        if (missionTimeoutPanel != null)
        {
            missionTimeoutPanel.SetActive(true);
        }

        StartCoroutine(RestartAfterDelay(2f));
    }

    public void RestartMissionFromTimeout()
    {
        StopMissionTimer();
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RestartMissionFromTimeout();
    }

    #endregion

    /// <summary>
    /// Invoked by the backpack HUD button so players can recover the map view on demand.
    /// </summary>
    public void ShowMapFromBackpack()
    {
        if (IsPaused)
            return;

        if (!isMiniSceneActive)
            return;

        ToggleMiniScene(false);
    }

    #region Debug Methods

    /// <summary>
    /// Developer tool: Skip the current task immediately.
    /// </summary>
    public void DebugSkipCurrentTask()
    {
        if (!IsMissionActive || CurrentTask == null)
        {
            Debug.LogWarning("DebugSkipCurrentTask: No active task to skip.");
            return;
        }

        Debug.Log($"[DEBUG] Skipping task: {CurrentTask.taskId}");

        // Cancel any active AR task
        if (activeARTask != null && activeARTask.IsActive)
        {
            activeARTask.CancelTask();
        }

        // Exit mini-scene if active
        if (isMiniSceneActive)
        {
            ToggleMiniScene(false);
        }

        // Clear any dialogue
        if (DuringMissionStoryDirector.Instance != null)
        {
            DuringMissionStoryDirector.Instance.ClearQueue();
        }

        // Force complete task
        CompleteCurrentTask();
    }

    /// <summary>
    /// Developer tool: Jump to a specific task index.
    /// </summary>
    public void DebugJumpToTask(int taskIndex)
    {
        if (!IsMissionActive)
        {
            Debug.LogWarning("DebugJumpToTask: No active mission.");
            return;
        }

        if (taskIndex < 0 || taskIndex >= TotalTasks)
        {
            Debug.LogWarning($"DebugJumpToTask: Invalid index {taskIndex}. Valid range: 0-{TotalTasks - 1}");
            return;
        }

        Debug.Log($"[DEBUG] Jumping to task index: {taskIndex}");

        // Cancel active AR task
        if (activeARTask != null && activeARTask.IsActive)
        {
            activeARTask.CancelTask();
        }

        // Exit mini-scene
        if (isMiniSceneActive)
        {
            ToggleMiniScene(false);
        }

        // Clear dialogue
        if (DuringMissionStoryDirector.Instance != null)
        {
            DuringMissionStoryDirector.Instance.ClearQueue();
        }

        // Start the target task
        StartTask(taskIndex);
    }

    /// <summary>
    /// Developer tool: Force complete the entire mission.
    /// </summary>
    public void DebugCompleteMission()
    {
        if (!IsMissionActive)
        {
            Debug.LogWarning("DebugCompleteMission: No active mission.");
            return;
        }

        Debug.Log("[DEBUG] Force completing mission.");

        // Cancel active AR task
        if (activeARTask != null && activeARTask.IsActive)
        {
            activeARTask.CancelTask();
        }

        // Exit mini-scene
        if (isMiniSceneActive)
        {
            ToggleMiniScene(false);
        }

        // Clear dialogue
        if (DuringMissionStoryDirector.Instance != null)
        {
            DuringMissionStoryDirector.Instance.ClearQueue();
        }

        // Force complete all remaining tasks internally
        isMissionActive = false;

        // Show completion
        SaveMissionProgress();
        OnMissionCompleted?.Invoke(currentMission);

        if (!TryProceedToNextMission())
        {
            ReturnToMissionSelect();
        }
    }

    #endregion

    #region AR Task Integration

    /// <summary>
    /// Start the AR task associated with the current mission task.
    /// Called when player enters a flood zone with an AR component.
    /// </summary>
    public void StartActiveARTask()
    {
        if (activeARTask == null)
        {
            Debug.LogWarning("DuringMissionManager: No AR task bound to current task.");
            return;
        }

        // Ensure the AR task GameObject is active so coroutines can start
        if (!activeARTask.gameObject.activeInHierarchy)
        {
            Debug.Log("DuringMissionManager: Enabling AR task GameObject before starting.");
            activeARTask.gameObject.SetActive(true);
        }

        if (activeARTask.IsActive)
        {
            Debug.LogWarning("DuringMissionManager: AR task already active.");
            return;
        }

        Debug.Log($"DuringMissionManager: Starting AR task for {CurrentTask?.taskId}");
        activeARTask.StartTask();
    }

    /// <summary>
    /// Get the current active AR task, if any.
    /// </summary>
    public ARTaskBase GetActiveARTask() => activeARTask;

    #endregion
}
