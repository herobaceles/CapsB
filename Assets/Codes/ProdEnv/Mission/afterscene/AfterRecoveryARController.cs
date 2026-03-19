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

    [Header("Gameplay Control")] 
    [Tooltip("Player controller to disable while AR is active.")]
    [SerializeField] private IsometricPlayerController playerController;
    [Tooltip("Root GameObject for the on-screen joystick UI to hide while AR is active.")]
    [SerializeField] private GameObject joystickRoot;

    [Header("Environment Roots")]
    [Tooltip("Normal gameplay environment root (e.g. interior house) that should be hidden while AR is active.")]
    [SerializeField] private GameObject defaultEnvironmentRoot;
    [Tooltip("Any additional objects that should be hidden while AR is active (player, triggers, extra UI, etc.).")]
    [SerializeField] private GameObject[] additionalObjectsToHideInAR;

    [Header("AR House Roots")]
    [SerializeField] private GameObject cleanupGearHouseRoot;
    [SerializeField] private GameObject hiddenDangerHouseRoot;
    [SerializeField] private GameObject kitchenSafetyHouseRoot;
    [SerializeField] private GameObject disinfectHouseRoot;

    [Header("Disinfect House Tools")]
    [Tooltip("Spray bottle prefab to attach to the AR camera in DisinfectHouse mode.")]
    [SerializeField] private GameObject disinfectSprayPrefab;
    [Tooltip("Towel prefab (e.g. taoru.obj) to attach to the AR camera in DisinfectHouse mode.")]
    [SerializeField] private GameObject disinfectTowelPrefab;
    [Tooltip("Local offset from the AR camera for the spray bottle (right side of the screen).")]
    [SerializeField] private Vector3 disinfectSprayLocalOffset = new Vector3(0.3f, -0.15f, 0.7f);
    [Tooltip("Local offset from the AR camera for the towel (left side of the screen).")]
    [SerializeField] private Vector3 disinfectTowelLocalOffset = new Vector3(-0.3f, -0.15f, 0.7f);

    [Header("Feedback")]
    [Tooltip("Prefab for a green-check style feedback icon shown on correct taps.")]
    [SerializeField] private GameObject correctFeedbackPrefab;
    [Tooltip("Prefab for a red-X style feedback icon shown on incorrect taps.")]
    [SerializeField] private GameObject incorrectFeedbackPrefab;
    [Tooltip("Lifetime in seconds before feedback icons are destroyed.")]
    [SerializeField] private float feedbackLifetime = 1.5f;

    // -----------------------------------------------------------------------
    // Private state
    // -----------------------------------------------------------------------

    private bool arActive;
    private string activeTaskId;
    private MissionMode currentMissionMode;

    private GameObject activeDisinfectSpray;
    private GameObject activeDisinfectTowel;

    // Simple objective tracking for AR sessions that report recovered
    // items via HandleItemRecovered (e.g. CleanupGear / HiddenDanger / KitchenSafety).
    private int recoveredItemCount;

    [Tooltip("Runtime threshold used by HandleItemRecovered to decide when to auto-complete the current AR task. This is set from the per-mode fields below each time EnableARRecovery is called.")]
    [SerializeField] private int autoCompleteOnRecoveredItemCount = 0;

    [Header("Mode-Specific Completion Counts")]
    [Tooltip("Number of recovered items required to auto-complete CleanupGear.")]
    [SerializeField] private int cleanupGearRequiredRecoveredItems = 0;
    [Tooltip("Number of recovered items required to auto-complete HiddenDanger (e.g. snake + rat).")]
    [SerializeField] private int hiddenDangerRequiredRecoveredItems = 0;
    [Tooltip("Number of recovered items required to auto-complete KitchenSafety (safe items).")]
    [SerializeField] private int kitchenSafetyRequiredRecoveredItems = 0;

    /// <summary>
    /// Read-only accessor for the currently active mission mode.
    /// Used by item/interactable scripts that need to branch
    /// behaviour based on the active AR scenario.
    /// </summary>
    public MissionMode CurrentMissionMode => currentMissionMode;

    /// <summary>
    /// Indicates whether an AR session is currently active.
    /// Useful for AR tap detectors and other systems that should
    /// only run while AR is active.
    /// </summary>
    public bool IsARActive => arActive;

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
        recoveredItemCount = 0;

        // Reset and then configure the auto-complete threshold per mode
        // so that AR sessions cannot leak counts into one another.
        autoCompleteOnRecoveredItemCount = 0;

        switch (mode)
        {
            case MissionMode.CleanupGear:
                autoCompleteOnRecoveredItemCount = cleanupGearRequiredRecoveredItems;
                break;

            case MissionMode.HiddenDanger:
                autoCompleteOnRecoveredItemCount = hiddenDangerRequiredRecoveredItems;
                break;

            case MissionMode.KitchenSafety:
                autoCompleteOnRecoveredItemCount = kitchenSafetyRequiredRecoveredItems;
                break;

            case MissionMode.DisinfectHouse:
                // Use the same completion-count semantics as CleanupGear or
                // KitchenSafety: each cleaned mud pile reports via
                // MudPileInteraction -> HandleItemRecovered, and once the
                // configured number is reached the AR session ends.
                autoCompleteOnRecoveredItemCount = cleanupGearRequiredRecoveredItems;
                break;

            default:
                // Other modes can either use hidden-danger spawner
                // signalling or leave auto-complete count unchanged.
                break;
        }
        Debug.Log($"AfterRecoveryARController: Starting AR recovery — mode: {mode}");

        // Disable normal gameplay controls while AR is running.
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }

        if (joystickRoot != null)
        {
            joystickRoot.SetActive(false);
        }

        if (defaultEnvironmentRoot != null)
            defaultEnvironmentRoot.SetActive(false);

        if (additionalObjectsToHideInAR != null)
        {
            for (int i = 0; i < additionalObjectsToHideInAR.Length; i++)
            {
                if (additionalObjectsToHideInAR[i] != null)
                    additionalObjectsToHideInAR[i].SetActive(false);
            }
        }

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

        // Clean up any DisinfectHouse-specific tools parented to the camera.
        if (activeDisinfectSpray != null)
        {
            Destroy(activeDisinfectSpray);
            activeDisinfectSpray = null;
        }
        if (activeDisinfectTowel != null)
        {
            Destroy(activeDisinfectTowel);
            activeDisinfectTowel = null;
        }

        if (hiddenDangerSpawner != null)
            hiddenDangerSpawner.StopSpawning();

        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.SetARActive(false);

        // Re-enable normal gameplay controls now that AR is done.
        if (playerController != null)
        {
            if (!playerController.gameObject.activeSelf)
            {
                playerController.gameObject.SetActive(true);
            }
            playerController.SetMovementEnabled(true);
        }

        if (joystickRoot != null)
        {
            joystickRoot.SetActive(true);
        }

        if (defaultEnvironmentRoot != null)
            defaultEnvironmentRoot.SetActive(true);

        if (additionalObjectsToHideInAR != null)
        {
            for (int i = 0; i < additionalObjectsToHideInAR.Length; i++)
            {
                if (additionalObjectsToHideInAR[i] != null)
                    additionalObjectsToHideInAR[i].SetActive(true);
            }
        }

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
        Debug.Log($"AfterRecoveryARController.TriggerFeedback: isCorrect={isCorrect} at {worldPosition}");

        GameObject prefab = isCorrect ? correctFeedbackPrefab : incorrectFeedbackPrefab;
        if (prefab == null)
        {
            return;
        }

        // Try to face the feedback icon toward the active AR camera for better visibility.
        Camera cam = null;
        if (ARRuntimeContext.Instance != null)
        {
            cam = ARRuntimeContext.Instance.ResolveARCamera();
        }
        if (cam == null && gameplayCamera != null)
        {
            cam = gameplayCamera;
        }

        Quaternion rotation = cam != null
            ? Quaternion.LookRotation(cam.transform.forward, Vector3.up)
            : Quaternion.identity;

        GameObject instance = Instantiate(prefab, worldPosition, rotation);

        if (feedbackLifetime > 0f)
        {
            Destroy(instance, feedbackLifetime);
        }
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

        // Generic count-based auto-completion for AR tasks that use
        // HiddenDangerItem / MudPileInteraction recovery callbacks.
        if (arActive)
        {
            recoveredItemCount++;
            Debug.Log($"AfterRecoveryARController: Recovered items {recoveredItemCount}/{autoCompleteOnRecoveredItemCount}.");

            if (autoCompleteOnRecoveredItemCount > 0 && recoveredItemCount >= autoCompleteOnRecoveredItemCount)
            {
                Debug.Log("AfterRecoveryARController: Auto-completing AR task after required items recovered.");
                DisableAR();
            }
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private void DispatchByMode(MissionMode mode)
    {
        if (cleanupGearHouseRoot != null) cleanupGearHouseRoot.SetActive(false);
        if (hiddenDangerHouseRoot != null) hiddenDangerHouseRoot.SetActive(false);
        if (kitchenSafetyHouseRoot != null) kitchenSafetyHouseRoot.SetActive(false);
        if (disinfectHouseRoot != null) disinfectHouseRoot.SetActive(false);

        switch (mode)
        {
            case MissionMode.CleanupGear:
                if (cleanupGearHouseRoot != null)
                    cleanupGearHouseRoot.SetActive(true);
                break;

            case MissionMode.HiddenDanger:
                if (hiddenDangerHouseRoot != null)
                    hiddenDangerHouseRoot.SetActive(true);
                break;

            case MissionMode.KitchenSafety:
                if (kitchenSafetyHouseRoot != null)
                    kitchenSafetyHouseRoot.SetActive(true);
                break;

            case MissionMode.DisinfectHouse:
                if (disinfectHouseRoot != null)
                    disinfectHouseRoot.SetActive(true);
                AttachDisinfectToolsToCamera();
                break;

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

    private void AttachDisinfectToolsToCamera()
    {
        if (disinfectSprayPrefab == null && disinfectTowelPrefab == null)
            return;

        Camera arCamera = ARRuntimeContext.Instance != null
            ? ARRuntimeContext.Instance.ResolveARCamera()
            : null;

        if (arCamera == null)
        {
            Debug.LogWarning("AfterRecoveryARController: Cannot attach disinfect tools — AR camera not found.");
            return;
        }

        if (disinfectSprayPrefab != null && activeDisinfectSpray == null)
        {
            activeDisinfectSpray = Instantiate(disinfectSprayPrefab, arCamera.transform);
            activeDisinfectSpray.transform.localPosition = disinfectSprayLocalOffset;
            activeDisinfectSpray.transform.localRotation = Quaternion.identity;
        }

        if (disinfectTowelPrefab != null && activeDisinfectTowel == null)
        {
            activeDisinfectTowel = Instantiate(disinfectTowelPrefab, arCamera.transform);
            activeDisinfectTowel.transform.localPosition = disinfectTowelLocalOffset;
            activeDisinfectTowel.transform.localRotation = Quaternion.identity;
        }
    }
}
