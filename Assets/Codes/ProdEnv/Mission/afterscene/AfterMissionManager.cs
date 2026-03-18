using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the After Mission phase — recovery tasks.
/// Examples: cleaning up debris, scanning for hidden hazards, disinfecting the house.
///
/// Mirrors BeforeMissionManager and DuringMissionManager in architecture:
///   - Extends MissionSceneManager
///   - Uses ARModeBinding inner class (like DuringMissionManager.ARTaskBinding)
///   - Delegates to AfterRecoveryARController for AR sub-tasks
///   - Overrides CompleteMission() to show a phase-specific completion banner
/// </summary>
public class AfterMissionManager : MissionSceneManager
{
    public new static AfterMissionManager Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inner types
    // -----------------------------------------------------------------------

    [System.Serializable]
    private class ARModeBinding
    {
        [SerializeField] private string taskId;
        [SerializeField] private MissionMode mode;

        public string TaskId  => taskId;
        public MissionMode Mode => mode;
    }

    // -----------------------------------------------------------------------
    // Inspector fields
    // -----------------------------------------------------------------------

    [Header("After Phase UI")]
    [SerializeField] private GameObject preparationUI;

    [Header("Camera")]
    [SerializeField] private Camera gameplayCamera;

    [Header("AR Controller")]
    [Tooltip("Reference to AfterRecoveryARController in the scene.")]
    [SerializeField] private AfterRecoveryARController arController;

    [Header("AR Mode Bindings")]
    [Tooltip("Maps each MissionData task ID to the AR recovery mode launched when its trigger fires.")]
    [SerializeField] private List<ARModeBinding> arModeBindings = new List<ARModeBinding>();

    // -----------------------------------------------------------------------
    // Private state
    // -----------------------------------------------------------------------

    private bool waitingForContinue;
    private bool startQuizCompleted;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        if (arController == null)
            arController = FindObjectOfType<AfterRecoveryARController>();
    }

    protected override void LoadMission()
    {
        base.LoadMission();

        if (currentMission == null)
        {
            var fromResources = Resources.Load<MissionData>("Missions/Mission_After_01");
            if (fromResources != null)
            {
                currentMission = fromResources;
                Debug.Log("AfterMissionManager: Loaded fallback Mission_After_01 from Resources.");
            }
        }

        if (currentMission == null)
            Debug.LogError("AfterMissionManager: No mission found. Assign a MissionData via MissionSelectManager or the fallbackMission field.");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
            Instance = null;
    }

    // -----------------------------------------------------------------------
    // Mission flow overrides
    // -----------------------------------------------------------------------

    /// <summary>
    /// Begin sequence for After-phase missions. Copies the base
    /// intro-dialogue logic but deliberately skips the automatic
    /// mission-level startQuiz so that the quiz can be triggered
    /// later by a specific task trigger (e.g. after01_quiz_zone).
    /// </summary>
    protected override IEnumerator BeginMissionSequence()
    {
        yield return new WaitForSeconds(0.5f);

        if (currentMission == null)
        {
            Debug.LogError("AfterMissionManager: Cannot start - no mission loaded!");
            yield break;
        }

        if (ProdDialogueManager.Instance != null)
        {
            bool dialogueFinished = false;

            if (currentMission.introDialogueRich != null && currentMission.introDialogueRich.Count > 0)
            {
                ProdDialogueManager.Instance.ShowDialogueSequence(currentMission.introDialogueRich, () => dialogueFinished = true);

                while (!dialogueFinished)
                    yield return null;
            }
        }
        else if (currentMission.introDialogueRich != null && currentMission.introDialogueRich.Count > 0)
        {
            Debug.LogWarning("AfterMissionManager: Intro dialogue configured but ProdDialogueManager is missing. Skipping dialogue.");
        }

        Debug.Log("AfterMissionManager: Starting mission after intro dialogue (no auto start quiz).");

        // For After_01, start in a quiz-only exploration phase so only the
        // quiz zone trigger is visible/active before AR tasks.
        if (currentMission != null &&
            string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase))
        {
            var sceneController = FindObjectOfType<AfterSceneController>();
            if (sceneController != null)
            {
                sceneController.StartQuizOnlyPhase();
            }
        }

        StartMission();
    }

    /// <summary>
    /// After-phase override: if the triggered task has an AR mode bound, delegate to the
    /// AR controller and hold mission progression until the AR flow reports completion
    /// via NotifyInteractionComplete(taskId). Otherwise falls back to the base behaviour
    /// (immediately completes the task).
    /// </summary>
    public override void OnTriggerActivated(string taskId)
    {
        if (!isMissionActive)
            return;

        bool isCurrentTask = currentTask != null &&
            string.Equals(currentTask.taskId, taskId, System.StringComparison.OrdinalIgnoreCase);

        // Special-case: for Mission_After_01 we want the mission-level
        // startQuiz to appear only when the player enters the quiz zone
        // trigger (taskId "after01_quiz_zone"), not immediately on load.
        if (isCurrentTask &&
            currentMission != null &&
            string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase) &&
            string.Equals(taskId, "after01_quiz_zone", System.StringComparison.OrdinalIgnoreCase) &&
            !startQuizCompleted)
        {
            StartStartQuizForCurrentTask(taskId);
            return;
        }

        if (isCurrentTask)
        {
            MissionMode? boundMode = FindARMode(taskId);
            if (boundMode.HasValue)
            {
                Debug.Log($"AfterMissionManager: Trigger entered for AR task '{taskId}', launching mode {boundMode.Value}.");
                LaunchARMode(boundMode.Value, taskId);
                return;
            }
        }

        base.OnTriggerActivated(taskId);
    }

    /// <summary>
    /// Shows the mission-level startQuiz when the quiz-zone task trigger
    /// is activated, then completes that task once the player answers
    /// correctly (and any follow-up dialogue finishes).
    /// </summary>
    private void StartStartQuizForCurrentTask(string taskId)
    {
        var sequence = GetStartQuizSequence(currentMission);

        if (sequence == null || sequence.Count == 0)
        {
            Debug.LogWarning("AfterMissionManager: Start quiz sequence missing or invalid for Mission_After_01; completing quiz task immediately.");
            OnStartQuizAnsweredCorrectlyForTask(taskId);
            return;
        }

        if (quizDialogueUI == null)
            quizDialogueUI = FindObjectOfType<QuizDialogueUIManager>();

        if (quizDialogueUI == null)
        {
            Debug.LogWarning("AfterMissionManager: QuizDialogueUIManager not found; skipping quiz gate to avoid soft lock.");
            OnStartQuizAnsweredCorrectlyForTask(taskId);
            return;
        }

        int currentIndex = 0;

        System.Action runNext = null;
        runNext = () =>
        {
            if (currentIndex >= sequence.Count)
            {
                OnStartQuizAnsweredCorrectlyForTask(taskId);
                return;
            }

            var quizData = sequence[currentIndex];

            if (!IsStartQuizValid(quizData))
            {
                currentIndex++;
                runNext();
                return;
            }

            quizDialogueUI.ShowQuiz(quizData, () =>
            {
                var dialogueManager = ProdDialogueManager.Instance;
                if (dialogueManager != null &&
                    quizData.correctAnswerDialogueRich != null &&
                    quizData.correctAnswerDialogueRich.Count > 0)
                {
                    dialogueManager.ShowDialogueSequence(quizData.correctAnswerDialogueRich, () =>
                    {
                        currentIndex++;
                        runNext();
                    });
                }
                else
                {
                    currentIndex++;
                    runNext();
                }
            });
        };

        runNext();
    }

    private void OnStartQuizAnsweredCorrectlyForTask(string taskId)
    {
        startQuizCompleted = true;
        Debug.Log($"AfterMissionManager: Start quiz for task '{taskId}' answered correctly; completing quiz-zone task.");

        // When the quiz gate is cleared for After_01, switch the scene
        // controller into full exploration mode so AR triggers become
        // visible/active for the next task.
        if (currentMission != null &&
            string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase))
        {
            var sceneController = FindObjectOfType<AfterSceneController>();
            if (sceneController != null)
            {
                sceneController.StartExplorationPhaseInternal();
            }
        }

        CompleteCurrentTask();
    }

    /// <summary>
    /// Backward-compatible entry point called by AfterRecoveryARController when an
    /// AR sub-task completes but no explicit task id was bound. Delegates to
    /// NotifyInteractionComplete for the current task when possible.
    /// </summary>
    public void NotifyARTaskComplete()
    {
        if (currentTask == null)
        {
            Debug.LogWarning("AfterMissionManager: NotifyARTaskComplete called but there is no current task.");
            return;
        }

        NotifyInteractionComplete(currentTask.taskId);
    }

    /// <summary>
    /// Entry point for AR-driven progress from AfterRecoveryARController.
    /// Marks all objectives for the specified task as completed and advances the
    /// mission flow.
    /// </summary>
    public void NotifyInteractionComplete(string taskId)
    {
        if (!isMissionActive)
        {
            Debug.LogWarning("AfterMissionManager: NotifyInteractionComplete called while mission is not active.");
            return;
        }

        bool isCurrentTask = currentTask != null &&
            string.Equals(currentTask.taskId, taskId, System.StringComparison.OrdinalIgnoreCase);

        if (!isCurrentTask)
        {
            Debug.LogWarning($"AfterMissionManager: NotifyInteractionComplete called for task '{taskId}', but current task is '{currentTask?.taskId}'.");
            return;
        }

        if (currentTask.objectives != null && currentTask.objectives.Count > 0)
        {
            for (int i = 0; i < currentTask.objectives.Count; i++)
            {
                var objective = currentTask.objectives[i];
                if (objective == null)
                    continue;

                if (!objective.isCompleted)
                {
                    objective.currentCount = objective.requiredCount;
                    objective.isCompleted = true;
                    OnObjectiveUpdated?.Invoke(objective);
                }
            }

            UpdateObjectivesUI();
        }

        Debug.Log($"AfterMissionManager: AR interaction complete for task '{taskId}', advancing mission.");

        if (preparationUI != null)
            preparationUI.SetActive(true);

        CompleteCurrentTask();
    }

    protected override void CompleteMission()
    {
        if (preparationUI != null)
            preparationUI.SetActive(false);

        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(true);
        if (missionCompleteTitleText != null)
            missionCompleteTitleText.text = "After Phase Completed!";
        if (missionCompleteMessageText != null)
            missionCompleteMessageText.text = "Great work! The community is safer thanks to you.";

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueAfterBanner);
            continueButton.onClick.AddListener(OnContinueAfterBanner);
        }

        waitingForContinue = true;
    }

    private void OnContinueAfterBanner()
    {
        if (!waitingForContinue) return;
        waitingForContinue = false;
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);
        base.CompleteMission();
    }

    // -----------------------------------------------------------------------
    // UI overrides
    // -----------------------------------------------------------------------

    protected override void UpdateTaskUI()
    {
        // For Mission_After_01 we want the on-screen TaskPanel to
        // stay hidden so the player is guided by world-space cues,
        // quiz, and AR rather than a traditional task list.
        if (currentMission != null &&
            string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase))
        {
            if (taskPanel != null)
                taskPanel.SetActive(false);
            return;
        }

        base.UpdateTaskUI();
    }

    // -----------------------------------------------------------------------
    // AR helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// After-phase hook: when moving from the quiz-zone task to the first
    /// AR task in Mission_After_01, automatically launch the bound AR mode
    /// once all completion dialogue has finished.
    /// </summary>
    protected override void MoveToNextTask()
    {
        bool shouldAutoLaunchAR = false;

        // We only care about the transition from the quiz gate task to the
        // next task in Mission_After_01, and only after the quiz sequence
        // has been completed.
        if (currentMission != null &&
            string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase) &&
            currentTask != null &&
            string.Equals(currentTask.taskId, "after01_quiz_zone", System.StringComparison.OrdinalIgnoreCase) &&
            startQuizCompleted)
        {
            shouldAutoLaunchAR = true;
        }

        base.MoveToNextTask();

        // After base.MoveToNextTask, currentTask now points at the next task
        // (or the mission has completed). If this was the special After_01
        // transition, automatically start the AR mode bound to the new task.
        if (shouldAutoLaunchAR && isMissionActive && currentTask != null)
        {
            MissionMode? boundMode = FindARMode(currentTask.taskId);
            if (boundMode.HasValue)
            {
                Debug.Log($"AfterMissionManager: Auto-launching AR mode {boundMode.Value} for task '{currentTask.taskId}' after quiz sequence.");
                LaunchARMode(boundMode.Value, currentTask.taskId);
            }
            else
            {
                Debug.LogWarning($"AfterMissionManager: No AR mode bound for task '{currentTask.taskId}' to auto-launch after quiz.");
            }
        }
    }

    private void LaunchARMode(MissionMode mode, string taskId)
    {
        if (arController == null)
        {
            Debug.LogError("AfterMissionManager: AfterRecoveryARController not assigned. Cannot launch AR mode.");
            return;
        }

        if (preparationUI != null)
            preparationUI.SetActive(false);

        arController.EnableARRecovery(mode, taskId);
    }

    private MissionMode? FindARMode(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId)) return null;

        foreach (var binding in arModeBindings)
        {
            if (binding != null &&
                string.Equals(binding.TaskId, taskId, System.StringComparison.OrdinalIgnoreCase))
            {
                return binding.Mode;
            }
        }
        return null;
    }

    // -----------------------------------------------------------------------
    // Camera helpers — mirrors BeforeMissionManager.ResolveGameplayCamera()
    // -----------------------------------------------------------------------

    private void ResolveGameplayCamera()
    {
        if (gameplayCamera != null)
            return;

        Camera arCamera = ARRuntimeContext.Instance != null ? ARRuntimeContext.Instance.ResolveARCamera() : null;
        Transform arRootTransform = ARRuntimeContext.Instance != null && ARRuntimeContext.Instance.ARRoot != null
            ? ARRuntimeContext.Instance.ARRoot.transform
            : null;

        Scene activeScene = SceneManager.GetActiveScene();
        var sceneRoots = activeScene.GetRootGameObjects();

        for (int i = 0; i < sceneRoots.Length; i++)
        {
            var cameras = sceneRoots[i].GetComponentsInChildren<Camera>(true);
            for (int j = 0; j < cameras.Length; j++)
            {
                var candidate = cameras[j];
                if (candidate == null) continue;
                if (arRootTransform != null && candidate.transform.IsChildOf(arRootTransform)) continue;
                if (arCamera != null && candidate == arCamera) continue;

                if (string.Equals(candidate.name, "Main Camera", System.StringComparison.OrdinalIgnoreCase))
                {
                    gameplayCamera = candidate;
                    Debug.Log($"AfterMissionManager: Gameplay camera bound to '{gameplayCamera.name}'.");
                    return;
                }
            }
        }

        if (Camera.main != null)
        {
            bool underArRoot = arRootTransform != null && Camera.main.transform.IsChildOf(arRootTransform);
            if ((arCamera == null || Camera.main != arCamera) && !underArRoot)
            {
                gameplayCamera = Camera.main;
                Debug.Log($"AfterMissionManager: Gameplay camera bound via Camera.main '{gameplayCamera.name}'.");
                return;
            }
        }

        var allCameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < allCameras.Length; i++)
        {
            var candidate = allCameras[i];
            if (candidate == null) continue;
            if (arCamera != null && candidate == arCamera) continue;
            if (arRootTransform != null && candidate.transform.IsChildOf(arRootTransform)) continue;
            if (candidate.gameObject.scene != activeScene) continue;

            gameplayCamera = candidate;
            Debug.Log($"AfterMissionManager: Gameplay camera fallback bound to '{gameplayCamera.name}'.");
            break;
        }

        if (gameplayCamera == null)
            Debug.LogWarning("AfterMissionManager: No gameplay camera found (excluding ARCoreRoot). Black screen may occur.");
    }

    private void SetGameplayCameraActive(bool active)
    {
        ResolveGameplayCamera();

        Camera arCamera = ARRuntimeContext.Instance != null ? ARRuntimeContext.Instance.ResolveARCamera() : null;
        if (gameplayCamera != null && arCamera != null && gameplayCamera == arCamera)
            return;

        if (gameplayCamera != null)
            gameplayCamera.gameObject.SetActive(active);
    }
}
