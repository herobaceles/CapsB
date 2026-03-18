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

    protected override void Start()
    {
        base.Start();

        ResolveGameplayCamera();
        SetGameplayCameraActive(true);

        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.SetARActive(false);

        if (preparationUI != null)
            preparationUI.SetActive(true);
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
    // AR helpers
    // -----------------------------------------------------------------------

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
