using UnityEngine;
using System.Collections;

/// <summary>
/// Legacy After-phase AR controller.
///
/// - Owns AR-specific state (IsARActive, CurrentMissionMode).
/// - Talks to HiddenDangerSpawner for spawning/clearing hazards.
/// - Provides a singleton Instance used by older scripts
///   (HiddenDangerItem, AfterARTapDetector, MudPileInteraction,
///   AfterSceneController, AfterRecoveryQuizManager).
///
/// All mission progression and UI is handled by AfterMissionManager.
/// This class should NOT inherit MissionSceneManager or override
/// mission lifecycle methods.
/// </summary>
public class AfterRecoveryARController : MonoBehaviour
{
    public static AfterRecoveryARController Instance { get; private set; }

    [Header("Sub-task Handlers")]
    [SerializeField] private HiddenDangerSpawner hiddenDangerSpawner;

    [Header("AR House Roots (Scene Objects)")]
    [Tooltip("Root GameObject for the Cleanup Gear AR house. Should be present in the scene and initially inactive.")]
    [SerializeField] private GameObject cleanupGearHouseRoot;

    [Tooltip("Root GameObject for the Hidden Danger AR house. Should be present in the scene and initially inactive.")]
    [SerializeField] private GameObject hiddenDangerHouseRoot;

    [Tooltip("Root GameObject for the Kitchen Safety AR house. Should be present in the scene and initially inactive.")]
    [SerializeField] private GameObject kitchenSafetyHouseRoot;

    [Tooltip("Root GameObject for the Disinfect House AR house. Should be present in the scene and initially inactive.")]
    [SerializeField] private GameObject disinfectHouseRoot;

    [Header("AR UI")]
    [Tooltip("Root GameObject containing all After-phase AR UI. Shown while AR is active.")]
    [SerializeField] private GameObject arUIRoot;

    [Header("AR Feedback Icons")]
    [Tooltip("Prefab or GameObject used for correct feedback (green check).")]
    [SerializeField] private GameObject greenCheckPrefab;

    [Tooltip("Prefab or GameObject used for incorrect feedback (red cross).")]
    [SerializeField] private GameObject redCrossPrefab;

    [Tooltip("Optional parent for spawned feedback icons (e.g. AR root). If null, icons are spawned at the scene root.")]
    [SerializeField] private Transform feedbackRoot;

    [Tooltip("How long feedback icons remain visible before being destroyed.")]
    [SerializeField] private float feedbackLifetime = 0.75f;

    [Header("Gameplay Root (Non-AR)")]
    [Tooltip("Optional root object for normal gameplay (player, triggers, interior). Will be disabled while AR is active.")]
    [SerializeField] private GameObject gameplayRoot;

    [Header("Gameplay UI (Non-AR)")]
    [Tooltip("Root GameObject for on-screen joystick / movement UI. Will be hidden while AR is active.")]
    [SerializeField] private GameObject joystickRoot;

    [Header("Camera")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private bool disableGameplayCameraInAR = true;

    [Header("Audio Feedback (Optional)")]
    [Tooltip("AudioSource for the correct-answer SFX (ding). Leave null to disable.")]
    [SerializeField] private AudioSource correctAnswerSfx;

    [Tooltip("AudioSource for the wrong-answer SFX (buzzer). Leave null to disable.")]
    [SerializeField] private AudioSource wrongAnswerSfx;

    /// <summary>
    /// True while an AR recovery session is active.
    /// Used by detectors/items to gate behaviour.
    /// </summary>
    public bool IsARActive { get; private set; }

    /// <summary>
    /// Current mission mode for this AR session (HiddenDanger,
    /// CleanupGear, KitchenSafety, DisinfectHouse, etc.).
    /// </summary>
    public MissionMode CurrentMissionMode { get; private set; } = MissionMode.HiddenDanger;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (hiddenDangerSpawner == null)
            hiddenDangerSpawner = FindObjectOfType<HiddenDangerSpawner>();
    }

    /// <summary>
    /// Public entry point used by quiz manager and other legacy
    /// scripts to start an AR recovery session for a given mode.
    /// </summary>
    public void EnableARRecovery(MissionMode mode)
    {
        EnableARRecovery_Internal(mode);
    }

    /// <summary>
    /// Public entry point used by quiz manager and other legacy
    /// scripts to start an AR recovery session for a given mode.
    /// </summary>
    public void EnableARRecovery(MissionMode mode, string taskId)
    {
        // taskId is currently not used by this legacy controller,
        // but we keep it for backward compatibility with AfterMissionManager.
        EnableARRecovery_Internal(mode);
    }

    /// <summary>
    /// Internal entry point used by AfterSceneController when the
    /// newer ARManager is not configured.
    /// </summary>
    public void EnableARRecovery_Internal(MissionMode mode)
    {
        // If an AR session is already active for a different mode,
        // clean it up first so we never have multiple AR houses or
        // planes active at the same time.
        if (IsARActive && CurrentMissionMode != mode)
        {
            DisableAR();
        }

        CurrentMissionMode = mode;
        IsARActive = true;

        if (arUIRoot != null)
            arUIRoot.SetActive(true);

        // Hide the normal gameplay root (player, triggers, interior) while
        // AR is active so the player only sees the AR house and UI.
        if (gameplayRoot != null)
            gameplayRoot.SetActive(false);

        // Hide joystick / movement UI while in AR.
        if (joystickRoot != null)
            joystickRoot.SetActive(false);

        if (disableGameplayCameraInAR && gameplayCamera != null)
            gameplayCamera.gameObject.SetActive(false);

        if (ARRuntimeContext.Instance != null)
        {
            ARRuntimeContext.Instance.SetARActive(true);
        }

        GameObject activeHouseRoot = null;

        // Toggle AR house roots based on the active mission mode.
        if (cleanupGearHouseRoot != null)
        {
            bool active = mode == MissionMode.CleanupGear;
            cleanupGearHouseRoot.SetActive(active);
            if (active) activeHouseRoot = cleanupGearHouseRoot;
        }

        if (hiddenDangerHouseRoot != null)
        {
            bool active = mode == MissionMode.HiddenDanger;
            hiddenDangerHouseRoot.SetActive(active);
            if (active) activeHouseRoot = hiddenDangerHouseRoot;
        }

        if (kitchenSafetyHouseRoot != null)
        {
            bool active = mode == MissionMode.KitchenSafety;
            kitchenSafetyHouseRoot.SetActive(active);
            if (active) activeHouseRoot = kitchenSafetyHouseRoot;
        }

        if (disinfectHouseRoot != null)
        {
            bool active = mode == MissionMode.DisinfectHouse;
            disinfectHouseRoot.SetActive(active);
            if (active) activeHouseRoot = disinfectHouseRoot;
        }

        // Ensure the active AR house lives under the ARRoot so it
        // stays in AR space and does not drift relative to tracking.
        AttachHouseToARRoot(activeHouseRoot);

        // Only HiddenDanger mode actually relies on the spawner.
        if (hiddenDangerSpawner != null && mode == MissionMode.HiddenDanger)
        {
            hiddenDangerSpawner.StartSpawning();
        }

        Debug.Log($"AfterRecoveryARController: AR session started for mode {mode}.");
    }

    /// <summary>
    /// Disables the current AR session and restores gameplay camera.
    /// </summary>
    public void DisableAR()
    {
        if (!IsARActive)
            return;

        IsARActive = false;

        if (hiddenDangerSpawner != null)
        {
            hiddenDangerSpawner.StopSpawning();
        }

        // Hide any AR house roots that were shown for CleanupGear, HiddenDanger,
        // KitchenSafety or DisinfectHouse.
        if (cleanupGearHouseRoot != null)
            cleanupGearHouseRoot.SetActive(false);

        if (hiddenDangerHouseRoot != null)
            hiddenDangerHouseRoot.SetActive(false);

        if (kitchenSafetyHouseRoot != null)
            kitchenSafetyHouseRoot.SetActive(false);

        if (disinfectHouseRoot != null)
            disinfectHouseRoot.SetActive(false);

        if (arUIRoot != null)
            arUIRoot.SetActive(false);

        // Restore the normal gameplay root and camera when AR ends.
        if (gameplayRoot != null)
            gameplayRoot.SetActive(true);

        // Restore joystick / movement UI for non-AR gameplay.
        if (joystickRoot != null)
            joystickRoot.SetActive(true);

        if (gameplayCamera != null)
            gameplayCamera.gameObject.SetActive(true);

        if (ARRuntimeContext.Instance != null)
        {
            ARRuntimeContext.Instance.SetARActive(false);
        }

        Debug.Log("AfterRecoveryARController: AR session disabled.");
    }

    private void AttachHouseToARRoot(GameObject houseRoot)
    {
        if (houseRoot == null)
            return;

        if (ARRuntimeContext.Instance == null || ARRuntimeContext.Instance.ARRoot == null)
            return;

        Transform houseTransform = houseRoot.transform;
        Transform arRootTransform = ARRuntimeContext.Instance.ARRoot.transform;

        if (houseTransform.parent == arRootTransform)
            return;

        // Keep the current world pose while reparenting so the house
        // does not visually jump, but now moves consistently with AR.
        houseTransform.SetParent(arRootTransform, true);
    }

    /// <summary>
    /// Called by HiddenDangerSpawner when the player clears one
    /// or more dangers in the HiddenDanger mode.
    /// </summary>
    public void OnHiddenDangerCleared(int foundCount, int requiredCount)
    {
        Debug.Log($"AfterRecoveryARController: Hidden danger progress {foundCount}/{requiredCount}.");

        if (foundCount < requiredCount)
            return;

        // When all dangers are cleared, first report completion into the
        // AfterMissionManager so the mission flow (tasks, achievements,
        // completion UI) can advance correctly.
        var missionManager = AfterMissionManager.Instance;
        if (missionManager != null &&
            missionManager.CurrentMissionIdIs("after_01"))
        {
            Debug.Log("AfterRecoveryARController: All Hidden Dangers cleared for after_01; notifying AfterMissionManager for task 'after01_hidden_danger'.");
            missionManager.NotifyInteractionComplete("after01_hidden_danger");
        }
        else
        {
            // Legacy fallback: notify the higher-level scene controller so it
            // can drive any remaining progression.
            var sceneController = FindObjectOfType<AfterSceneController>();
            if (sceneController != null)
            {
                sceneController.OnMissionCompletedForMode(CurrentMissionMode);
            }
        }

        // Finally, shut down the AR session.
        DisableAR();
    }

    /// <summary>
    /// Fallback path for older scripts that report a recovered
    /// object directly to this controller (e.g. MudPileInteraction).
    /// Prefer reporting via AfterSceneController when available.
    /// </summary>
    public void HandleItemRecovered(GameObject obj)
    {
        if (obj == null)
            return;

        var sceneController = FindObjectOfType<AfterSceneController>();
        if (sceneController != null)
        {
            sceneController.OnGenericItemRecovered(obj);
        }
        else
        {
            Debug.LogWarning($"AfterRecoveryARController: HandleItemRecovered called for '{obj.name}' but no AfterSceneController was found.");
        }
    }

    /// <summary>
    /// Centralised visual feedback entry point used by
    /// AfterSceneController, HiddenDangerItem and AfterARTapDetector.
    /// Spawns green check / red cross icons at the given world
    /// position so that feedback is visible in AR.
    /// </summary>
    public void TriggerFeedback(bool isCorrect, Vector3 worldPosition)
    {
        GameObject prefab = isCorrect ? greenCheckPrefab : redCrossPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"AfterRecoveryARController: No {(isCorrect ? "greenCheck" : "redCross")} prefab assigned for feedback.");
            return;
        }

        Transform parent = feedbackRoot != null ? feedbackRoot : null;
        GameObject iconInstance = Instantiate(prefab, worldPosition, Quaternion.identity, parent);

        // Face the AR camera if available so the icon is clearly visible
        // in AR. Falls back to Camera.main if needed.
        Camera cam = null;
        if (ARRuntimeContext.Instance != null)
        {
            cam = ARRuntimeContext.Instance.ResolveARCamera();
        }
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam != null)
        {
            iconInstance.transform.LookAt(cam.transform);
        }

        if (feedbackLifetime > 0f)
        {
            Destroy(iconInstance, feedbackLifetime);
        }

        // Play audio feedback once per interaction if configured
        if (isCorrect)
        {
            if (correctAnswerSfx != null)
                correctAnswerSfx.Play();
        }
        else
        {
            if (wrongAnswerSfx != null)
                wrongAnswerSfx.Play();
        }
    }

    /// <summary>
    /// Called by AfterSceneController once its mission tracker
    /// decides that a mode is complete. Ensures AR is shut down
    /// cleanly even if this controller started it.
    /// </summary>
    public void HandleMissionCompletionFromController(MissionMode mode)
    {
        if (IsARActive && mode == CurrentMissionMode)
        {
            Debug.Log($"AfterRecoveryARController: Mission complete for mode {mode}, disabling AR.");
            DisableAR();
        }
    }
}