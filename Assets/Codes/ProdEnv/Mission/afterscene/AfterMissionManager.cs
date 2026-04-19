using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private TMP_Text achievementDetailText;

    [Header("After Outro UI")]
    [SerializeField] private OuntroPanelController outroPanelController;
    [SerializeField] private Button continueToSummaryButton;

    [Header("Audio")]
    [SerializeField] private AudioClip afterSceneBgmClip;

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

    protected override void Start()
    {
        base.Start();

        // Start background music specific to the After scene, if configured.
        if (AudioManager.Instance != null && afterSceneBgmClip != null)
        {
            AudioManager.Instance.PlayMusicIfDifferent(afterSceneBgmClip);
        }
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

        // Before showing any intro dialogue, pre-configure triggers for
        // missions that have a dedicated AR trigger so that only the
        // appropriate trigger is visible even while the dialogue is
        // on-screen. This avoids briefly showing unrelated triggers.
        if (sceneController != null &&
            string.Equals(currentMission.missionId, "after_02", System.StringComparison.OrdinalIgnoreCase))
        {
            sceneController.InitializeMission(currentMission, MissionMode.KitchenSafety);
            sceneController.ConfigureTriggersForCurrentMode();
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
                // Initialize mission mode for correct AR trigger logic.
                // After_01 is a HiddenDanger-style mission, while After_03
                // is the DisinfectHouse mission.
                var initialMode = string.Equals(currentMission.missionId, "after_03", System.StringComparison.OrdinalIgnoreCase)
                    ? MissionMode.DisinfectHouse
                    : MissionMode.HiddenDanger;

                sceneController.InitializeMission(currentMission, initialMode);
                sceneController.StartQuizOnlyPhase();

                // For Mission_After_01, show the cleanup gear props in the
                // HouseInterior during the quiz-zone task so players can see
                // them while moving around before AR starts.
                if (string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase))
                {
                    sceneController.ShowCleanupGearInteriorGroup();
                }
                else if (string.Equals(currentMission.missionId, "after_03", System.StringComparison.OrdinalIgnoreCase))
                {
                    // For Mission_After_03, show the DisinfectHouse props
                    // during the quiz-zone phase.
                    sceneController.ShowDisinfectHouseInteriorGroup();
                }
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

        // For After_02, we want to land directly in a KitchenSafety
        // exploration phase so that only the kitchen safety trigger is
        // visible and no global mission-level start quiz runs.
        if (currentMission != null &&
            string.Equals(currentMission.missionId, "after_02", System.StringComparison.OrdinalIgnoreCase))
        {
            if (sceneController != null)
            {
                sceneController.InitializeMission(currentMission, MissionMode.KitchenSafety);
                sceneController.StartExplorationPhaseInternal();

                // For Mission_After_02, show the kitchen safety props in the
                // HouseInterior during the Safe_items task.
                sceneController.ShowKitchenSafetyInteriorGroup();
            }

            Debug.Log("AfterMissionManager: Starting After_02 mission in KitchenSafety exploration phase.");
            StartMission();
            yield break;
        }

        // For other After-phase missions we
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
                if (string.Equals(taskId, "after01_hidden_danger", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(taskId, "ARTrigger_HiddenDanger", System.StringComparison.OrdinalIgnoreCase))
                {
                    boundMode = MissionMode.HiddenDanger;
                }
                else if (string.Equals(taskId, "after_02_Safe_items", System.StringComparison.OrdinalIgnoreCase))
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
                
                // Ensure the mission is actually started before completing the quiz
                // task; otherwise CompleteCurrentTask will be a no-op and the
                // disinfect trigger will never be activated by the mission system.
                if (!isMissionActive)
                {
                    Debug.Log("AfterMissionManager: Starting After_03 mission after quiz so disinfect task can activate.");
                    StartMission();
                }

                // Complete the quiz task and move to next task (AR task)
                CompleteCurrentTask();

                // Safety: ensure that after the quiz we really land on the
                // disinfect-mud task and that its trigger is activated. If, for
                // any reason, the base progression did not advance as expected,
                // force-switch the current task to after_03_disinfect_mud and
                // manually activate its trigger.
                if (currentMission != null && currentMission.tasks != null)
                {
                    for (int i = 0; i < currentMission.tasks.Count; i++)
                    {
                        var t = currentMission.tasks[i];
                        if (t != null &&
                            string.Equals(t.taskId, "after_03_disinfect_mud", System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (currentTask == null ||
                                !string.Equals(currentTask.taskId, "after_03_disinfect_mud", System.StringComparison.OrdinalIgnoreCase))
                            {
                                Debug.LogWarning("AfterMissionManager: Forcing current task to 'after_03_disinfect_mud' after quiz.");
                                currentTaskIndex = i;
                                currentTask = t;
                                ActivateTaskTrigger(currentTask.taskId);
                            }
                            break;
                        }
                    }
                }
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

        if (currentMission != null)
        {
            if (sceneController == null)
                sceneController = FindObjectOfType<AfterSceneController>();

            if (sceneController != null)
            {
                // For Mission_After_01, when the CleanupGear AR task
                // finishes, switch to the hidden danger props so they are
                // visible while the player walks to ARTrigger_HiddenDanger.
                if (string.Equals(currentMission.missionId, "after_01", System.StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(taskId, "after01_cleanup_gear", System.StringComparison.OrdinalIgnoreCase))
                {
                    sceneController.ShowHiddenDangerInteriorGroup();
                }

                // For Mission_After_03, when the disinfect-mud AR task
                // finishes, hide all interior groups so DisinfectHouse props
                // do not remain visible into other missions.
                if (string.Equals(currentMission.missionId, "after_03", System.StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(taskId, "after_03_disinfect_mud", System.StringComparison.OrdinalIgnoreCase))
                {
                    sceneController.HideAllInteriorGroups();
                }
            }
        }

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

        // Ensure no interior item groups remain active when the After
        // mission completes so they do not leak into other missions.
        if (sceneController == null)
            sceneController = FindObjectOfType<AfterSceneController>();

        if (sceneController != null)
        {
            sceneController.HideAllInteriorGroups();
        }

        // For Mission_After_03, show the MissionComplete banner first.
        // The player will click Continue to trigger the OuntroPanel.
        // Other missions keep the existing behaviour of immediately showing
        // the completion UI.
        ShowMissionCompleteBanner();
    }

    /// <summary>
    /// Shows the standard MissionComplete banner and wires the continue button
    /// to advance out of the After phase. Shared by all After missions so the
    /// after_03 outro flow can invoke it after the OuntroPanel finishes.
    /// </summary>
    private void ShowMissionCompleteBanner()
    {
        // Determine which After mission is currently running.
        string missionId = currentMission != null ? currentMission.missionId : null;
        bool isAfter01 = string.Equals(missionId, "after_01", System.StringComparison.OrdinalIgnoreCase);
        bool isAfter02 = string.Equals(missionId, "after_02", System.StringComparison.OrdinalIgnoreCase);
        bool isAfter03 = string.Equals(missionId, "after_03", System.StringComparison.OrdinalIgnoreCase);

        // For Mission_After_01 (Hidden Danger), show the AchievementsPanel with
        // a specific detail line. For After_02 and After_03 we only want the
        // MissionCompletePanel (no achievements panel).
        if (achievementsPanel != null)
        {
            if (isAfter01)
            {
                if (achievementDetailText != null)
                {
                    achievementDetailText.text = "Pest Control: Snake and rats cleared.";
                }

                achievementsPanel.SetActive(true);
            }
            else
            {
                achievementsPanel.SetActive(false);
            }
        }

        // Select mission-specific title/message
        string completeTitle = "Mission Complete";
        string completeMessage = string.Empty;

        if (isAfter01)
        {
            completeTitle = "Hidden Danger Completed!";
            completeMessage = "Well done! You’ve cleared out the hidden dangers and made the area safer.";
        }
        else if (isAfter02)
        {
            completeTitle = "Safe Choices";
            completeMessage = "Good job! You chose the safe items to eat.";
        }
        else if (isAfter03)
        {
            completeTitle = "Home Restored";
            completeMessage = "Great job! You made the home safe again.";
        }

        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(true);
        if (missionCompleteTitleText != null)
            missionCompleteTitleText.text = completeTitle;
        if (missionCompleteMessageText != null)
            missionCompleteMessageText.text = completeMessage;

        // Hide ReplayButton for after_03 mission; show only ContinueButton or ContinueToSummaryButton
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(!isAfter03);
        }

        // For after_03, wire ContinueToSummaryButton; for others, use regular ContinueButton
        if (isAfter03)
        {
            // Hide regular Continue button; show Continue to Summary button
            if (continueButton != null)
                continueButton.gameObject.SetActive(false);
            
            if (continueToSummaryButton != null)
            {
                continueToSummaryButton.gameObject.SetActive(true);
                continueToSummaryButton.onClick.RemoveListener(OnContinueToSummaryClicked);
                continueToSummaryButton.onClick.AddListener(OnContinueToSummaryClicked);
            }
        }
        else
        {
            // Show regular Continue button; hide Continue to Summary button
            if (continueToSummaryButton != null)
                continueToSummaryButton.gameObject.SetActive(false);
            
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.RemoveListener(OnContinueAfterBanner);
                continueButton.onClick.AddListener(OnContinueAfterBanner);
            }
        }

        waitingForContinue = true;
    }

    /// <summary>
    /// Called by the AchievementsPanel Proceed button. Hides only the
    /// achievements panel so that the underlying MissionComplete UI remains
    /// visible; does not complete or exit the mission.
    /// </summary>
    public void OnAchievementsProceedButton()
    {
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
    }

    /// <summary>
    /// Called by the AchievementsPanel Restart button. Uses the same replay
    /// behaviour as the base MissionSceneManager (reloads the current scene).
    /// </summary>
    public void OnAchievementsRestartButton()
    {
        OnReplayClicked();
    }

    private void OnContinueAfterBanner()
    {
        if (!waitingForContinue) return;
        waitingForContinue = false;
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);

        // For Mission_After_03, show the OuntroPanel when player clicks Continue.
        // When OuntroPanel finishes, it will navigate back to MissionSelectManager.
        // For other missions, proceed directly to mission completion.
        bool isAfter03 = currentMission != null &&
            string.Equals(currentMission.missionId, "after_03", System.StringComparison.OrdinalIgnoreCase);

        if (isAfter03 && outroPanelController != null)
        {
            outroPanelController.StartSequence(OnOuntroFinished, useNextButtonOnFinal: true);
        }
        else
        {
            base.CompleteMission();
        }
    }

    /// <summary>
    /// Called when the OuntroPanel finishes (player completes all pages).
    /// Navigates back to MissionSelectManager.
    /// </summary>
    private void OnOuntroFinished()
    {
        base.CompleteMission();
    }

    /// <summary>
    /// Called when player clicks "Continue to Summary" button on MissionCompleteUI (after_03 only).
    /// Shows the OuntroPanel.
    /// </summary>
    private void OnContinueToSummaryClicked()
    {
        if (!waitingForContinue) return;
        waitingForContinue = false;
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(false);
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);

        // Show OuntroPanel; when finished, navigate to MissionSelectManager
        if (outroPanelController != null)
        {
            outroPanelController.StartSequence(OnOuntroFinished, useNextButtonOnFinal: true);
        }
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