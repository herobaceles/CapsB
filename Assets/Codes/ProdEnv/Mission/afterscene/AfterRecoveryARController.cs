using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the AR session and sub-task dispatch for the After phase.
///
/// Receives a MissionMode from AfterMissionManager, activates AR, and delegates to
/// the appropriate recovery sub-system (HiddenDangerSpawner for hazard/cleanup modes,
/// or no spawner for DamageAssessment which uses MissionData.startQuiz).
///
/// Single completion path: DisableAR() always calls
/// AfterMissionManager.Instance.NotifyARTaskComplete() so task progression is consistent.
/// </summary>
public class AfterRecoveryARController : MonoBehaviour
{
    public static AfterRecoveryARController Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector fields
    // -----------------------------------------------------------------------

    [Header("Sub-task Handlers")]
    [SerializeField] private HiddenDangerSpawner hiddenDangerSpawner;

    [Header("AR UI")]
    [Tooltip("Root GameObject containing all After-phase AR UI. Shown while AR is active.")]
    [SerializeField] private GameObject arUIRoot;

    [Header("Camera")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private bool disableGameplayCameraInAR = true;

    // -----------------------------------------------------------------------
    // Private state
    // -----------------------------------------------------------------------

    private bool arActive;
    private string activeTaskId;
    private MissionMode currentMissionMode;

    /// <summary>
    /// Read-only accessor for the currently active mission mode.
    /// Used by item/interactable scripts that need to branch
    /// behaviour based on the active AR scenario.
    /// </summary>
    public MissionMode CurrentMissionMode => currentMissionMode;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Activates AR and starts the recovery sub-task for <paramref name="mode"/>.
    /// Legacy entry point without an explicit task id. Delegates to the
    /// task-aware overload with a null id.
    /// </summary>
    public void EnableARRecovery(MissionMode mode)
    {
        EnableARRecovery(mode, null);
    }

    /// <summary>
    /// Task-aware overload used by AfterMissionManager. Binds the active
    /// mission task id so DisableAR() can report completion back to
    /// AfterMissionManager via NotifyInteractionComplete(taskId).
    /// </summary>
    public void EnableARRecovery(MissionMode mode, string taskId)
    {
        if (arActive)
        {
            Debug.LogWarning("AfterRecoveryARController: EnableARRecovery called while AR is already active.");
            return;
        }

        arActive = true;
        currentMissionMode = mode;
        activeTaskId = taskId;
        Debug.Log($"AfterRecoveryARController: Starting AR recovery — mode: {mode}");

        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.SetARActive(true);

        if (disableGameplayCameraInAR)
            StartCoroutine(DisableGameplayCameraWhenARReady());

        if (arUIRoot != null)
            arUIRoot.SetActive(true);

        DispatchByMode(mode);
    }

    /// <summary>
    /// Deactivates AR, restores gameplay camera, hides AR UI, and notifies
    /// AfterMissionManager that the current task is complete.
    /// Called by HiddenDangerSpawner when all items are cleared, or directly
    /// for modes that need no spawner.
    /// </summary>
    public void DisableAR()
    {
        if (!arActive) return;

        arActive = false;
        Debug.Log("AfterRecoveryARController: Disabling AR, returning to gameplay.");

        if (hiddenDangerSpawner != null)
            hiddenDangerSpawner.StopSpawning();

        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.SetARActive(false);

        if (disableGameplayCameraInAR && gameplayCamera != null)
            gameplayCamera.gameObject.SetActive(true);

        if (arUIRoot != null)
            arUIRoot.SetActive(false);

        if (!string.IsNullOrEmpty(activeTaskId))
        {
            AfterMissionManager.Instance?.NotifyInteractionComplete(activeTaskId);
        }
        else
        {
            // Fallback for legacy flows that did not bind a task id.
            AfterMissionManager.Instance?.NotifyARTaskComplete();
        }
    }

    /// <summary>
    /// Called by HiddenDangerSpawner whenever a hidden danger / mud pile is
    /// cleared. Aggregates progress at the AR layer and decides when to end
    /// the AR session.
    /// </summary>
    public void OnHiddenDangerCleared(int clearedCount, int requiredCount)
    {
        if (!arActive)
            return;

        Debug.Log($"AfterRecoveryARController: Hidden danger cleared {clearedCount}/{requiredCount}.");

        if (clearedCount >= requiredCount)
        {
            Debug.Log("AfterRecoveryARController: All hidden dangers cleared — ending AR session.");
            DisableAR();
        }
    }

    /// <summary>
    /// Shows simple correct/incorrect feedback at a world position.
    /// Hook this up to your AR UI (icon, particle, etc.) as needed.
    /// </summary>
    public void TriggerFeedback(bool isCorrect, Vector3 worldPosition)
    {
        // Placeholder: integrate with your AR UI system if desired.
        Debug.Log($"AfterRecoveryARController.TriggerFeedback: isCorrect={isCorrect} at {worldPosition}");
    }

    /// <summary>
    /// Legacy hook used by older item scripts (HiddenDangerItem,
    /// MudPileInteraction) to report that an AR interaction item
    /// was recovered. In the new architecture, these items should
    /// ultimately cause DisableAR() when the scenario is complete,
    /// and mission progression is handled by AfterMissionManager.
    /// </summary>
    public void HandleItemRecovered(GameObject obj)
    {
        if (obj == null)
            return;

        Debug.Log($"AfterRecoveryARController.HandleItemRecovered: '{obj.name}' in mode {currentMissionMode}.");

        // For now, counting and completion are delegated to the
        // scenario-specific scripts (e.g. counters on the AR
        // prefab roots) which will call DisableAR() when done.
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private void DispatchByMode(MissionMode mode)
    {
        switch (mode)
        {
            case MissionMode.CleanupGear:
            case MissionMode.HiddenDanger:
            case MissionMode.KitchenSafety:
            case MissionMode.DisinfectHouse:
            case MissionMode.HazardScan:
                StartHiddenDangerSession();
                break;

            case MissionMode.DamageAssessment:
                // Structural assessment is handled by MissionData.startQuiz.
                // No spawning needed; the quiz gate advances the task automatically.
                Debug.Log("AfterRecoveryARController: DamageAssessment — delegated to MissionData start quiz.");
                break;

            default:
                Debug.LogWarning($"AfterRecoveryARController: Unhandled MissionMode '{mode}'. Completing AR task immediately.");
                DisableAR();
                break;
        }
    }

    private void StartHiddenDangerSession()
    {
        if (hiddenDangerSpawner == null)
        {
            Debug.LogError("AfterRecoveryARController: HiddenDangerSpawner not assigned. Cannot start spawning.");
            DisableAR();
            return;
        }

        hiddenDangerSpawner.StartSpawning();
    }

    private IEnumerator DisableGameplayCameraWhenARReady()
    {
        if (gameplayCamera == null)
            yield break;

        float timeout = 3.0f;
        while (timeout > 0f)
        {
            Camera arCamera = ARRuntimeContext.Instance != null ? ARRuntimeContext.Instance.ResolveARCamera() : null;
            if (arCamera != null && arCamera.gameObject.activeInHierarchy && arCamera.enabled)
            {
                gameplayCamera.gameObject.SetActive(false);
                yield break;
            }
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("AfterRecoveryARController: AR camera not ready in time. Keeping gameplay camera active.");
    }
}
