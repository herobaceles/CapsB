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
    // Helper for AfterSceneController to check mission state
    public bool IsStartQuizCompleted() => startQuizCompleted;
    public bool CurrentMissionIdIs(string id) => currentMission != null && string.Equals(currentMission.missionId, id, System.StringComparison.OrdinalIgnoreCase);
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

    [Header("After Achievements UI")]
    [SerializeField] private GameObject achievementsPanel;

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
    private AfterSceneController sceneController;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
        Instance = this;

        if (arController == null)
            arController = FindObjectOfType<AfterRecoveryARController>();
        
        sceneController = FindObjectOfType<AfterSceneController>();
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

        // For After_01 and After_03, start in a quiz-only exploration phase so only the
        // quiz zone/AR trigger is visible/active before AR tasks. The quiz
        // itself is shown when the player enters the correct trigger.
        if (currentMission != null &&
            (string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentMission.missionId, "after_03", System.StringComparison.OrdinalIgnoreCase)))
        {
            if (sceneController != null)
            {
                // Initialize mission mode for correct AR trigger logic
                sceneController.InitializeMission(currentMission, MissionMode.DisinfectHouse);
                sceneController.StartQuizOnlyPhase();
            }

            // For after_03, start the quiz immediately after dialog (no trigger required)
            if (string.Equals(currentMission.missionId, "after_03", System.StringComparison.OrdinalIgnoreCase) && !IsStartQuizCompleted())
            {
                StartStartQuizForCurrentTask("after03_quiz_zone");
                yield break;
            }

            StartMission();
            yield break;
        }

        // For other After-phase missions (after_02, etc.) we
        // want the normal mission-level start quiz (if configured) to
        // run before tasks begin, just like in the base class.
        yield return RunMissionStartQuizIfAvailable();

        Debug.Log("AfterMissionManager: Starting mission after intro dialogue/start quiz.");
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

        if (!isCurrentTask)
        {
            Debug.LogWarning($"AfterMissionManager: Trigger '{taskId}' entered but current task is '{currentTask?.taskId}'. Ignoring for mission flow. MissionId: {currentMission?.missionId}");
        }

        // FIX 1: Support quiz trigger for both after_01 and after_03
        if (currentMission != null &&
            (
                string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentMission.missionId, "after_03", System.StringComparison.OrdinalIgnoreCase)
            ) &&
            (
                string.Equals(taskId, "after01_quiz_zone", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(taskId, "after03_quiz_zone", System.StringComparison.OrdinalIgnoreCase)
            ) &&
            !startQuizCompleted)
        {
            Debug.Log($"AfterMissionManager: Quiz trigger activated for mission '{currentMission.missionId}', task '{taskId}'. Showing quiz.");
            StartStartQuizForCurrentTask(taskId);
            return;
        }

        // FIX: For After_03, handle AR trigger separately from quiz logic
        if (currentMission != null &&
            string.Equals(currentMission.missionId, "after_03", System.StringComparison.OrdinalIgnoreCase) &&
            string.Equals(taskId, "ARTrigger_DisinfectHouse", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"AfterMissionManager: AR trigger entered for mission 'after_03', task '{taskId}'. Starting AR task.");
            
            // Make sure the quiz is completed before allowing AR trigger
            if (!startQuizCompleted)
            {
                Debug.LogWarning("AfterMissionManager: AR trigger entered but quiz not completed yet. Ignoring.");
                return;
            }

            MissionMode? boundMode = FindARMode(taskId);
            if (boundMode.HasValue)
            {
                LaunchARMode(boundMode.Value, taskId);
            }
            else
            {
                // Fallback to default AR mode if binding not found
                LaunchARMode(MissionMode.DisinfectHouse, taskId);
            }
            return;
        }

        if (isCurrentTask)
        {
            MissionMode? boundMode = FindARMode(taskId);

            // Fallbacks for missions that rely on conventional task ids
            // instead of explicit AR Mode Bindings configured in the
            // inspector. These are keyed by task id only so they work
            // even if the mission asset ids are misconfigured.
            if (!boundMode.HasValue)
            {
                if (string.Equals(taskId, "after_02_Safe_items", System.StringComparison.OrdinalIgnoreCase))
                {
                    boundMode = MissionMode.KitchenSafety;
                }
                else if (string.Equals(taskId, "AR_DisinfectHouse", System.StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(taskId, "after_03_disinfect_mud", System.StringComparison.OrdinalIgnoreCase))
                {
                    boundMode = MissionMode.DisinfectHouse;
                }
            }

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
            Debug.LogWarning($"AfterMissionManager: Start quiz sequence missing or invalid for mission '{currentMission?.missionId}', task '{taskId}'. Completing quiz task immediately.");
            OnStartQuizAnsweredCorrectlyForTask(taskId);
            return;
        }

        if (quizDialogueUI == null)
            quizDialogueUI = FindObjectOfType<QuizDialogueUIManager>();

        if (quizDialogueUI == null)
        {
            Debug.LogError($"AfterMissionManager: QuizDialogueUIManager not found for mission '{currentMission?.missionId}', task '{taskId}'. Skipping quiz gate to avoid soft lock.");
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
        Debug.Log($"AfterMissionManager: Start quiz for mission '{currentMission?.missionId}', task '{taskId}' answered correctly; completing quiz-zone task.");

        if (sceneController == null)
            sceneController = FindObjectOfType<AfterSceneController>();

        if (currentMission != null && sceneController != null)
        {
            if (string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase))
            {
                sceneController.StartExplorationPhaseInternal();
                CompleteCurrentTask();
                AutoLaunchFirstBoundARAfterQuiz();
            }
            else if (string.Equals(currentMission.missionId, "after_03", System.StringComparison.OrdinalIgnoreCase))
            {
                // FIX: For After_03, deactivate quiz trigger and activate AR trigger
                sceneController.DeactivateQuizTrigger();
                sceneController.ActivateDisinfectTrigger();
                
                // Complete the quiz task and move to next task (AR task)
                CompleteCurrentTask();
            }
            else
            {
                CompleteCurrentTask();
                AutoLaunchFirstBoundARAfterQuiz();
            }
        }
        else
        {
            CompleteCurrentTask();
            AutoLaunchFirstBoundARAfterQuiz();
        }
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
            // Special-case: for Mission_After_01, CleanupGear AR is auto-launched
            // after the quiz and can legitimately report completion even if the
            // mission's currentTask was not updated as expected. In that case,
            // force-switch the current task to the reported one so progression
            // can continue to the HiddenDanger task.
            if (currentMission != null &&
                string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase))
            {
                var tasks = currentMission.tasks;
                if (tasks != null)
                {
                    for (int i = 0; i < tasks.Count; i++)
                    {
                        var t = tasks[i];
                        if (t != null && string.Equals(t.taskId, taskId, System.StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.LogWarning($"AfterMissionManager: NotifyInteractionComplete for '{taskId}' did not match current task '{currentTask?.taskId}'. For After_01, forcing current task index to {i}.");
                            currentTaskIndex = i;
                            currentTask = t;
                            isCurrentTask = true;
                            break;
                        }
                    }
                }
            }

            if (!isCurrentTask)
            {
                Debug.LogWarning($"AfterMissionManager: NotifyInteractionComplete called for task '{taskId}', but current task is '{currentTask?.taskId}'.");
                return;
            }
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

        if (currentTask != null)
        {
            Debug.Log($"AfterMissionManager: Next current task is '{currentTask.taskId}'.");
        }
        else
        {
            Debug.Log("AfterMissionManager: No next current task; mission may be complete.");
        }
    }

    protected override void CompleteMission()
    {
        if (preparationUI != null)
            preparationUI.SetActive(false);

        if (achievementsPanel != null)
            achievementsPanel.SetActive(true);

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
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
        base.CompleteMission();
    }

    // -----------------------------------------------------------------------
    // UI overrides
    // -----------------------------------------------------------------------

    protected override void UpdateTaskUI()
    {
        // For After-phase missions (after_01, after_02, after_03, etc.) we
        // want the on-screen TaskPanel to stay hidden so the player is
        // guided by world-space cues, dialogue, and AR triggers instead of
        // a traditional task list.
        if (currentMission != null &&
            currentMission.phase == MissionPhase.After)
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
    /// Immediately launches AR for the first AR-bound task after the quiz,
    /// using AR Mode Bindings as the source of truth. This does NOT require
    /// any additional TaskTrigger for after01_cleanup_gear.
    /// </summary>
    private void AutoLaunchFirstBoundARAfterQuiz()
    {
        if (currentMission == null ||
            !string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase))
            return;

        if (arModeBindings == null || arModeBindings.Count == 0)
        {
            Debug.LogWarning("AfterMissionManager: No AR Mode Bindings configured; cannot auto-launch AR after quiz.");
            return;
        }

        // Find the first AR binding that is NOT the quiz task itself.
        foreach (var binding in arModeBindings)
        {
            if (binding == null) continue;
            if (string.Equals(binding.TaskId, "after01_quiz_zone", System.StringComparison.OrdinalIgnoreCase))
                continue;

            Debug.LogWarning($"AfterMissionManager: Auto-launching AR mode {binding.Mode} for task '{binding.TaskId}' immediately after quiz.");
            LaunchARMode(binding.Mode, binding.TaskId);
            return;
        }

        Debug.LogWarning("AfterMissionManager: No suitable AR task found in bindings to auto-launch after quiz.");
    }

    protected override void MoveToNextTask()
    {
        if (currentMission == null)
        {
            Debug.LogWarning("AfterMissionManager: MoveToNextTask called but no currentMission is assigned. Did you forget to set the mission asset in the inspector?");
        }

        // Use the base implementation for task progression; AR auto-launch
        // after the quiz is handled by AutoLaunchARAfterQuiz().
        base.MoveToNextTask();
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