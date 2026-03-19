using UnityEngine;

public class AfterSceneController : MonoBehaviour
{
    /// <summary>
    /// Activates the DisinfectHouse AR trigger and disables the quiz trigger (for After_03 flow).
    /// </summary>
    public void ActivateDisinfectTrigger()
    {
        if (arTriggerDisinfectHouse != null)
            arTriggerDisinfectHouse.SetActive(true);
        if (quizZoneTrigger != null)
            quizZoneTrigger.SetActive(false);
        Debug.Log("AfterSceneController: DisinfectHouse AR trigger activated, quiz trigger deactivated.");
    }
    [Header("Managers")]
    [SerializeField] private AfterSceneARManager arManager;
    [SerializeField] private AfterSceneMissionTracker missionTracker;
    [SerializeField] private AfterSceneDialogueManager dialogueManager;
    [SerializeField] private AfterSceneUIManager uiManager;
    [SerializeField] private AfterSceneItemSpawner itemSpawner;

    [Header("Exploration Phase References")]
    [SerializeField] private GameObject playerController;
    [SerializeField] private GameObject gameplayCamera;
    [SerializeField] private GameObject joystickUI;
    [SerializeField] private GameObject houseInteriorEnvironment;
    [SerializeField] private GameObject arTriggerHiddenDanger;
    [SerializeField] private GameObject arTriggerKitchenSafety;
    [SerializeField] private GameObject arTriggerDisinfectHouse;
    [SerializeField] private GameObject quizZoneTrigger;

    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    private MissionData currentMission;
    private MissionMode currentMissionMode;

    public void InitializeMission(MissionData mission, MissionMode mode)
    {
        currentMission = mission;
        currentMissionMode = mode;
    }

    public void ShowFeedback(bool isCorrect, Vector3 worldPosition)
    {
        if (uiManager != null)
        {
            Camera cam = null;
            if (arManager != null)
            {
                cam = arManager.GetCurrentARCamera();
            }

            uiManager.ShowFeedbackIconAtWorldPosition(isCorrect, worldPosition, cam);
            return;
        }

        if (AfterRecoveryARController.Instance != null)
        {
            AfterRecoveryARController.Instance.TriggerFeedback(isCorrect, worldPosition);
        }
    }

    /// <summary>
    /// Starts a lightweight phase for the quiz-only task (after_quiz_zone).
    /// This brings up the environment and player controls, then enables only
    /// the QuizZoneTrigger while keeping AR mission triggers disabled.
    /// </summary>
    public void StartQuizOnlyPhase()
    {
        if (joystickUI != null) joystickUI.SetActive(true);
        if (playerController != null) playerController.SetActive(true);
        if (gameplayCamera != null) gameplayCamera.SetActive(true);

        if (houseInteriorEnvironment != null) houseInteriorEnvironment.SetActive(true);

        if (playerController != null)
        {
            playerController.transform.position = spawnPosition;
        }

        // Quiz-only: enable quiz trigger, keep AR triggers off so the
        // player first answers the quiz before starting Hidden Danger AR.
        if (quizZoneTrigger != null) quizZoneTrigger.SetActive(true);
        if (arTriggerHiddenDanger != null) arTriggerHiddenDanger.SetActive(false);
        if (arTriggerKitchenSafety != null) arTriggerKitchenSafety.SetActive(false);

        // Only disable arTriggerDisinfectHouse if quiz is not completed (for After_03)
        bool shouldDisableDisinfect = true;
        var afterMissionManager = AfterMissionManager.Instance;
        if (afterMissionManager != null && afterMissionManager.CurrentMissionIdIs("after_03") && afterMissionManager.IsStartQuizCompleted())
        {
            shouldDisableDisinfect = false;
        }
        if (arTriggerDisinfectHouse != null) arTriggerDisinfectHouse.SetActive(!shouldDisableDisinfect ? true : false);

        Debug.Log($"AfterSceneController: Quiz-only phase started. QuizZoneTrigger is now active. arTriggerDisinfectHouse active: {arTriggerDisinfectHouse?.activeSelf}");
    }

    public void StartExplorationPhaseInternal()
    {
        if (joystickUI != null) joystickUI.SetActive(true);
        if (playerController != null) playerController.SetActive(true);
        if (gameplayCamera != null) gameplayCamera.SetActive(true);

        if (houseInteriorEnvironment != null) houseInteriorEnvironment.SetActive(true);

        if (playerController != null)
        {
            playerController.transform.position = spawnPosition;
        }

        // Use the same branching rules as the legacy controller
        if (currentMissionMode == MissionMode.DisinfectHouse)
        {
            if (arTriggerDisinfectHouse != null) arTriggerDisinfectHouse.SetActive(true);
            if (arTriggerKitchenSafety != null) arTriggerKitchenSafety.SetActive(false);
            if (arTriggerHiddenDanger != null) arTriggerHiddenDanger.SetActive(false);
            if (quizZoneTrigger != null) quizZoneTrigger.SetActive(false);
        }
        else if (currentMissionMode == MissionMode.KitchenSafety)
        {
            if (arTriggerKitchenSafety != null) arTriggerKitchenSafety.SetActive(true);
            if (arTriggerHiddenDanger != null) arTriggerHiddenDanger.SetActive(false);
            if (arTriggerDisinfectHouse != null) arTriggerDisinfectHouse.SetActive(false);
            if (quizZoneTrigger != null) quizZoneTrigger.SetActive(false);
        }
        else
        {
            if (arTriggerHiddenDanger != null) arTriggerHiddenDanger.SetActive(true);
            if (quizZoneTrigger != null) quizZoneTrigger.SetActive(true);
            if (arTriggerKitchenSafety != null) arTriggerKitchenSafety.SetActive(false);
            if (arTriggerDisinfectHouse != null) arTriggerDisinfectHouse.SetActive(false);
        }
    }

    public void StartARForMode(MissionMode mode)
    {
        currentMissionMode = mode;

        if (arManager != null)
        {
            arManager.StartARForMode(mode);
        }
        else if (AfterRecoveryARController.Instance != null)
        {
            // Fallback to the shared AfterRecoveryARController if no local AR manager
            AfterRecoveryARController.Instance.EnableARRecovery(mode);
        }
    }

    public void EndARForCurrentMode()
    {
        if (arManager != null)
        {
            arManager.EndARForMode(currentMissionMode);
        }
        else if (ARRuntimeContext.Instance != null)
        {
            // Minimal fallback: just turn off AR session
            ARRuntimeContext.Instance.SetARActive(false);
        }
    }

    public void OnItemRecovered(HiddenDangerItem item)
    {
        if (item == null || missionTracker == null)
            return;

        missionTracker.HandleDangerRecovered(item);
        missionTracker.RecalculateProgress(currentMissionMode);

        if (missionTracker.IsMissionComplete(currentMissionMode))
        {
            OnMissionCompletedForMode(currentMissionMode);
        }
    }

    public void OnGenericItemRecovered(GameObject obj)
    {
        if (obj == null || missionTracker == null)
            return;

        missionTracker.HandleGenericItemRecovered(obj);
        missionTracker.RecalculateProgress(currentMissionMode);

        if (missionTracker.IsMissionComplete(currentMissionMode))
        {
            OnMissionCompletedForMode(currentMissionMode);
        }
    }

    public void OnMissionCompletedForMode(MissionMode mode)
    {
        // Delegate mission completion side effects to the legacy controller
        // so that dialogue, UI, and mission chaining remain identical.
        if (AfterRecoveryARController.Instance != null)
        {
            // In the new architecture, ending AR flows through DisableAR,
            // which reports completion back to AfterMissionManager.
            AfterRecoveryARController.Instance.DisableAR();
            return;
        }

        // Minimal fallback: just end AR session for this mode.
        if (arManager != null)
        {
            arManager.EndARForMode(mode);
        }
        else if (ARRuntimeContext.Instance != null)
        {
            ARRuntimeContext.Instance.SetARActive(false);
        }
    }

    public MissionMode GetCurrentMissionMode()
    {
        return currentMissionMode;
    }

    public MissionData GetCurrentMission()
    {
        return currentMission;
    }

    public void RegisterDangerItem(HiddenDangerItem item)
    {
        if (item == null)
            return;

        // Mirror the legacy registration pattern: subscribe a handler that
        // forwards recovered items into the mission tracker.
        item.OnRecovered -= OnItemRecovered;
        item.OnRecovered += OnItemRecovered;
    }
    // Deactivates the quiz trigger (quizZoneTrigger) if it exists
    public void DeactivateQuizTrigger()
    {
        if (quizZoneTrigger != null)
        {
            quizZoneTrigger.SetActive(false);
        }
    }
}