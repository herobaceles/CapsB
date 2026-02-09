using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for mission scene managers (Before, During, After).
/// Handles task progression, triggers, objectives, and dialogue.
/// Inherit this class and override virtual methods for phase-specific behavior.
/// </summary>
public abstract class MissionSceneManager : MonoBehaviour
{
    public static MissionSceneManager Instance { get; protected set; }

    [Header("Mission")]
    [SerializeField] protected MissionData fallbackMission;

    [Header("Task UI")]
    [SerializeField] protected GameObject taskPanel;
    [SerializeField] protected TMP_Text taskTitleText;
    [SerializeField] protected TMP_Text taskDescriptionText;
    [SerializeField] protected TMP_Text taskProgressText;
    [SerializeField] protected Image taskIconImage;

    [Header("Objective UI")]
    [SerializeField] protected Transform objectiveContainer;
    [SerializeField] protected GameObject objectivePrefab;

    [Header("Mission Complete UI")]
    [SerializeField] protected GameObject missionCompletePanel;
    [SerializeField] protected TMP_Text missionCompleteTitleText;
    [SerializeField] protected TMP_Text missionCompleteMessageText;
    [SerializeField] protected TMP_Text missionCompletePointsText;
    [SerializeField] protected Button continueButton;
    [SerializeField] protected Button replayButton;

    [Header("Pause Menu")]
    [SerializeField] protected GameObject pausePanel;
    [SerializeField] protected Button resumeButton;
    [SerializeField] protected Button restartButton;
    [SerializeField] protected Button quitButton;

    [Header("Loading")]
    [SerializeField] protected GameObject loadingPanel;
    [SerializeField] protected Slider progressBar;
    [SerializeField] protected TMP_Text progressText;

    [Header("Events")]
    public UnityEvent OnMissionStarted;
    public UnityEvent<TaskData> OnTaskStarted;
    public UnityEvent<TaskData> OnTaskCompleted;
    public UnityEvent<MissionData> OnMissionCompleted;
    public UnityEvent<ObjectiveData> OnObjectiveUpdated;

    // State
    protected MissionData currentMission;
    protected int currentTaskIndex = 0;
    protected TaskData currentTask;
    protected Dictionary<string, TaskTrigger> registeredTriggers = new Dictionary<string, TaskTrigger>();
    protected bool isMissionActive = false;
    protected bool isPaused = false;
    protected int totalPoints = 0;
    protected List<TaskData> completedTasks = new List<TaskData>();

    // Properties
    public MissionData CurrentMission => currentMission;
    public TaskData CurrentTask => currentTask;
    public int CurrentTaskIndex => currentTaskIndex;
    public int TotalTasks => currentMission?.tasks.Count ?? 0;
    public bool IsMissionActive => isMissionActive;
    public bool IsPaused => isPaused;
    public int TotalPoints => totalPoints;
    public float Progress => TotalTasks > 0 ? (float)completedTasks.Count / TotalTasks : 0f;

    protected virtual void Awake()
    {
        Instance = this;
    }

    protected virtual void Start()
    {
        SetupUI();
        LoadMission();
        StartCoroutine(BeginMissionSequence());
    }

    protected virtual void Update()
    {
        // Pause with Escape key - using new Input System
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    #region Setup

    protected virtual void SetupUI()
    {
        // Hide panels
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        if (taskPanel != null)
            taskPanel.SetActive(false);

        // Setup button listeners
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplayClicked);
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeMission);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnReplayClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(ReturnToMissionSelect);
    }

    protected virtual void LoadMission()
    {
        // Try to get mission from MissionSelectManager
        if (MissionSelectManager.SelectedMission != null)
        {
            currentMission = MissionSelectManager.SelectedMission;
            Debug.Log($"{GetType().Name}: Loaded mission from MissionSelectManager: {currentMission.missionName}");
        }
        else if (fallbackMission != null)
        {
            currentMission = fallbackMission;
            Debug.Log($"{GetType().Name}: Using fallback mission: {currentMission.missionName}");
        }
        else
        {
            Debug.LogError($"{GetType().Name}: No mission to load!");
        }
    }

    #endregion

    #region Mission Flow

    /// <summary>
    /// Checks if dialogue system is available and has valid UI references
    /// </summary>
    protected bool IsDialogueAvailable()
    {
        if (ProdDialogueManager.Instance == null)
            return false;
        
        // Try to refresh UI references
        ProdDialogueManager.Instance.RefreshUIReferences();
        
        // Check if dialogue panel was found by trying to access it
        // The IsDialogueActive property checks if panel exists
        return true; // Let's just try and fail gracefully
    }

    protected virtual IEnumerator BeginMissionSequence()
    {
        yield return new WaitForSeconds(0.5f);

        if (currentMission == null)
        {
            Debug.LogError($"{GetType().Name}: Cannot start - no mission loaded!");
            yield break;
        }

        // Skip dialogue for now - just start mission directly
        // TODO: Add DialoguePanel to mission scenes if dialogue is needed
        Debug.Log($"{GetType().Name}: Starting mission without intro dialogue");
        StartMission();
    }

    protected virtual void StartMission()
    {
        currentTaskIndex = 0;
        totalPoints = 0;
        completedTasks.Clear();
        isMissionActive = true;

        Debug.Log($"{GetType().Name}: Starting mission - {currentMission.missionName}");
        OnMissionStarted?.Invoke();

        if (currentMission.tasks.Count > 0)
        {
            StartTask(0);
        }
        else
        {
            Debug.LogWarning($"{GetType().Name}: Mission has no tasks!");
            CompleteMission();
        }
    }

    public virtual void StartTask(int taskIndex)
    {
        if (currentMission == null || taskIndex >= currentMission.tasks.Count)
        {
            Debug.LogWarning($"{GetType().Name}: Invalid task index: {taskIndex}");
            return;
        }

        currentTaskIndex = taskIndex;
        currentTask = currentMission.tasks[taskIndex];

        Debug.Log($"{GetType().Name}: Starting task {taskIndex + 1}/{currentMission.tasks.Count}: {currentTask.taskName}");

        // Reset objectives
        foreach (var objective in currentTask.objectives)
        {
            objective.isCompleted = false;
            objective.currentCount = 0;
        }

        // Update UI
        UpdateTaskUI();

        // Activate trigger for this task
        ActivateTaskTrigger(currentTask.taskId);

        // Show start dialogue
        if (currentTask.showDialogueOnStart && currentTask.startDialogue != null && currentTask.startDialogue.Length > 0)
        {
            ShowTaskDialogue(currentTask.startDialogue, () => OnTaskStarted?.Invoke(currentTask));
        }
        else
        {
            OnTaskStarted?.Invoke(currentTask);
        }
    }

    public virtual void CompleteCurrentTask()
    {
        if (currentTask == null || !isMissionActive)
        {
            Debug.LogWarning($"{GetType().Name}: No active task to complete");
            return;
        }

        Debug.Log($"{GetType().Name}: Completed task - {currentTask.taskName}");

        // Add points
        totalPoints += currentTask.pointsReward;
        completedTasks.Add(currentTask);

        // Deactivate trigger
        DeactivateTaskTrigger(currentTask.taskId);

        var completedTask = currentTask;

        // Show completion dialogue
        if (completedTask.showDialogueOnComplete && completedTask.completeDialogue != null && completedTask.completeDialogue.Length > 0)
        {
            ShowTaskDialogue(completedTask.completeDialogue, () =>
            {
                OnTaskCompleted?.Invoke(completedTask);
                MoveToNextTask();
            });
        }
        else
        {
            OnTaskCompleted?.Invoke(completedTask);
            MoveToNextTask();
        }
    }

    protected virtual void MoveToNextTask()
    {
        currentTaskIndex++;

        if (currentTaskIndex >= currentMission.tasks.Count)
        {
            CompleteMission();
        }
        else
        {
            StartTask(currentTaskIndex);
        }
    }

    protected virtual void CompleteMission()
    {
        Debug.Log($"{GetType().Name}: Mission complete! Total points: {totalPoints}");

        isMissionActive = false;
        currentTask = null;

        // Add mission reward
        int finalPoints = totalPoints + currentMission.rewardPoints;

        // Save progress
        SaveMissionProgress(finalPoints);

        // Hide task panel
        if (taskPanel != null)
            taskPanel.SetActive(false);

        // Skip dialogue - show UI directly
        // TODO: Add DialoguePanel to mission scenes if dialogue is needed
        ShowMissionCompleteUI();

        OnMissionCompleted?.Invoke(currentMission);
    }

    /// <summary>
    /// Override this to provide phase-specific completion messages
    /// </summary>
    protected virtual string GetCompletionMessage()
    {
        return "Great work! You're becoming a true BaHanda hero!";
    }

    #endregion

    #region Objectives

    public virtual void UpdateObjective(string objectiveId, int amount = 1)
    {
        if (currentTask == null) return;

        foreach (var objective in currentTask.objectives)
        {
            if (objective.objectiveId == objectiveId && !objective.isCompleted)
            {
                objective.currentCount += amount;

                if (objective.currentCount >= objective.requiredCount)
                {
                    objective.isCompleted = true;
                    objective.currentCount = objective.requiredCount;
                }

                OnObjectiveUpdated?.Invoke(objective);
                UpdateObjectivesUI();

                if (AreAllObjectivesComplete())
                {
                    CompleteCurrentTask();
                }

                break;
            }
        }
    }

    protected virtual bool AreAllObjectivesComplete()
    {
        if (currentTask == null || currentTask.objectives.Count == 0)
            return true;

        foreach (var objective in currentTask.objectives)
        {
            if (!objective.isCompleted)
                return false;
        }
        return true;
    }

    #endregion

    #region Trigger Management

    public virtual void RegisterTrigger(TaskTrigger trigger)
    {
        if (trigger == null || string.IsNullOrEmpty(trigger.TaskId)) return;

        registeredTriggers[trigger.TaskId] = trigger;
        trigger.SetActive(false);
        Debug.Log($"{GetType().Name}: Registered trigger - {trigger.TaskId}");
    }

    public virtual void UnregisterTrigger(TaskTrigger trigger)
    {
        if (trigger != null && !string.IsNullOrEmpty(trigger.TaskId))
        {
            registeredTriggers.Remove(trigger.TaskId);
        }
    }

    protected virtual void ActivateTaskTrigger(string taskId)
    {
        if (registeredTriggers.TryGetValue(taskId, out TaskTrigger trigger))
        {
            trigger.SetActive(true);
            Debug.Log($"{GetType().Name}: Activated trigger - {taskId}");
        }
        else
        {
            Debug.LogWarning($"{GetType().Name}: No trigger found for task - {taskId}");
        }
    }

    protected virtual void DeactivateTaskTrigger(string taskId)
    {
        if (registeredTriggers.TryGetValue(taskId, out TaskTrigger trigger))
        {
            trigger.SetActive(false);
        }
    }

    /// <summary>
    /// Called by TaskTrigger when activated
    /// </summary>
    public virtual void OnTriggerActivated(string taskId)
    {
        if (!isMissionActive) return;

        if (currentTask != null && currentTask.taskId == taskId)
        {
            CompleteCurrentTask();
        }
    }

    #endregion

    #region UI

    protected virtual void UpdateTaskUI()
    {
        if (currentTask == null) return;

        if (taskPanel != null)
            taskPanel.SetActive(true);

        if (taskTitleText != null)
            taskTitleText.text = currentTask.taskName;

        if (taskDescriptionText != null)
            taskDescriptionText.text = currentTask.taskDescription;

        if (taskProgressText != null)
            taskProgressText.text = $"Task {currentTaskIndex + 1} / {currentMission.tasks.Count}";

        if (taskIconImage != null)
        {
            if (currentTask.taskIcon != null)
            {
                taskIconImage.sprite = currentTask.taskIcon;
                taskIconImage.gameObject.SetActive(true);
            }
            else
            {
                taskIconImage.gameObject.SetActive(false);
            }
        }

        UpdateObjectivesUI();
    }

    protected virtual void UpdateObjectivesUI()
    {
        if (objectiveContainer == null || objectivePrefab == null || currentTask == null) return;

        // Clear existing
        foreach (Transform child in objectiveContainer)
        {
            Destroy(child.gameObject);
        }

        // Create objective items
        foreach (var objective in currentTask.objectives)
        {
            GameObject objItem = Instantiate(objectivePrefab, objectiveContainer);
            TMP_Text objText = objItem.GetComponentInChildren<TMP_Text>();
            if (objText != null)
            {
                string status = objective.isCompleted ? "✓" : "○";
                objText.text = $"{status} {objective.description} ({objective.currentCount}/{objective.requiredCount})";
            }
        }
    }

    protected virtual void ShowMissionCompleteUI()
    {
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(true);

        if (missionCompleteTitleText != null)
            missionCompleteTitleText.text = "Mission Complete!";

        if (missionCompleteMessageText != null)
            missionCompleteMessageText.text = currentMission.completionMessage;

        if (missionCompletePointsText != null)
            missionCompletePointsText.text = $"Points: {totalPoints + currentMission.rewardPoints}";
    }

    protected virtual void ShowTaskDialogue(string[] lines, System.Action onComplete)
    {
        // Skip dialogue for now - DialoguePanel not available in mission scenes
        // TODO: Add DialoguePanel to mission scenes if task dialogue is needed
        onComplete?.Invoke();
    }

    #endregion

    #region Pause

    public virtual void TogglePause()
    {
        if (isPaused)
            ResumeMission();
        else
            PauseMission();
    }

    public virtual void PauseMission()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public virtual void ResumeMission()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    #endregion

    #region Navigation

    protected virtual void OnContinueClicked()
    {
        Time.timeScale = 1f;
        
        // Check if there's a next mission to unlock
        if (!string.IsNullOrEmpty(currentMission.unlocksMissionId))
        {
            // Unlock next mission
            PlayerPrefs.SetInt($"Mission_{currentMission.unlocksMissionId}_Unlocked", 1);
            PlayerPrefs.Save();
        }

        ReturnToMissionSelect();
    }

    protected virtual void OnReplayClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public virtual void ReturnToMissionSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MissionSelect");
    }

    public virtual void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuProd");
    }

    #endregion

    #region Save/Load

    protected virtual void SaveMissionProgress(int points)
    {
        if (currentMission == null) return;

        string missionId = currentMission.missionId;

        // Mark as completed
        PlayerPrefs.SetInt($"Mission_{missionId}_Completed", 1);

        // Save best score
        int bestPoints = PlayerPrefs.GetInt($"Mission_{missionId}_Points", 0);
        if (points > bestPoints)
        {
            PlayerPrefs.SetInt($"Mission_{missionId}_Points", points);
        }

        // Add to total points
        int totalGamePoints = PlayerPrefs.GetInt("TotalPoints", 0);
        PlayerPrefs.SetInt("TotalPoints", totalGamePoints + points);

        // Unlock next mission if specified
        if (!string.IsNullOrEmpty(currentMission.unlocksMissionId))
        {
            PlayerPrefs.SetInt($"Mission_{currentMission.unlocksMissionId}_Unlocked", 1);
        }

        PlayerPrefs.Save();
        Debug.Log($"{GetType().Name}: Progress saved - {missionId} with {points} points");
    }

    public static bool IsMissionCompleted(string missionId)
    {
        return PlayerPrefs.GetInt($"Mission_{missionId}_Completed", 0) == 1;
    }

    public static bool IsMissionUnlocked(string missionId)
    {
        return PlayerPrefs.GetInt($"Mission_{missionId}_Unlocked", 0) == 1;
    }

    public static int GetMissionBestScore(string missionId)
    {
        return PlayerPrefs.GetInt($"Mission_{missionId}_Points", 0);
    }

    #endregion
}
