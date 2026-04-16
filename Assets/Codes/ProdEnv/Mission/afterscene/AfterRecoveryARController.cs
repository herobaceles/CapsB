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
    [SerializeField] private After_ARPlacementManager placementManager;

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

    [Header("Disinfect Tools (Hand Items)")]
    [Tooltip("Root GameObject that contains the Spray Bottle, cleaning rag and any other DisinfectHouse hand-held items.")]
    [SerializeField] private GameObject disinfectToolsRoot;

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

    // Tracks an optionally-disabled player GameObject so we can restore it
    // when AR ends for this controller.
    private GameObject disabledPlayer;
    private bool disabledPlayerByThisController;

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

        // Ensure any leftover feedback icons from a previous AR
        // task are cleared before starting a new one.
        ClearFeedbackIcons();

        CurrentMissionMode = mode;
        IsARActive = true;

        if (arUIRoot != null)
            arUIRoot.SetActive(true);

        // Default: hide disinfect hand tools; they are only used in
        // DisinfectHouse mode and should not appear in other AR tasks.
        if (disinfectToolsRoot != null)
            disinfectToolsRoot.SetActive(false);

        // Hide the normal gameplay root (player, triggers, interior) while
        // AR is active so the player only sees the AR house and UI.
        if (gameplayRoot != null)
            gameplayRoot.SetActive(false);

        // Hide joystick / movement UI while in AR.
        if (joystickRoot != null)
            joystickRoot.SetActive(false);

        if (disableGameplayCameraInAR && gameplayCamera != null)
            gameplayCamera.gameObject.SetActive(false);

        // Also explicitly disable any active player so the AR UI doesn't show
        // an avatar falling while the AR house is placed. We'll restore it
        // when AR ends in DisableAR().
        try
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null && playerObj.activeInHierarchy)
            {
                disabledPlayer = playerObj;
                disabledPlayerByThisController = true;
                playerObj.SetActive(false);
            }
        }
        catch { }

        if (ARRuntimeContext.Instance != null)
        {
            ARRuntimeContext.Instance.SetARActive(true);
        }

        GameObject activeHouseRoot = null;
        TaskData taskForAR = null;

        // Use the current mission task as AR guidance source when
        // available so AR placement manager can show AR dialogues.
        if (AfterMissionManager.Instance != null)
        {
            taskForAR = AfterMissionManager.Instance.CurrentTask;
        }

        // Select AR house root based on the active mission mode.
        // Do NOT activate any house yet; visibility will be
        // controlled by the placement manager after a successful
        // tap-to-place.
        if (cleanupGearHouseRoot != null)
        {
            cleanupGearHouseRoot.SetActive(false);
            if (mode == MissionMode.CleanupGear)
                activeHouseRoot = cleanupGearHouseRoot;
        }

        if (hiddenDangerHouseRoot != null)
        {
            hiddenDangerHouseRoot.SetActive(false);
            if (mode == MissionMode.HiddenDanger)
                activeHouseRoot = hiddenDangerHouseRoot;
        }

        if (kitchenSafetyHouseRoot != null)
        {
            kitchenSafetyHouseRoot.SetActive(false);
            if (mode == MissionMode.KitchenSafety)
                activeHouseRoot = kitchenSafetyHouseRoot;
        }

        if (disinfectHouseRoot != null)
        {
            disinfectHouseRoot.SetActive(false);
            if (mode == MissionMode.DisinfectHouse)
                activeHouseRoot = disinfectHouseRoot;
        }

        // If we are NOT in DisinfectHouse mode, make doubly sure
        // that any stray hand-held disinfect items are disabled.
        if (mode != MissionMode.DisinfectHouse)
        {
            HideAllHandItemsFollowingARCamera();
        }

        if (placementManager != null && activeHouseRoot != null)
        {
            // For HiddenDanger, delay spawning hazards until after
            // the house has been placed so spawn points stay aligned
            // with the moved house.
            if (mode == MissionMode.HiddenDanger && hiddenDangerSpawner != null)
            {
                placementManager.OnHousePlaced -= HandleHiddenDangerHousePlaced;
                placementManager.OnHousePlaced += HandleHiddenDangerHousePlaced;
            }

            // For DisinfectHouse, delay enabling the disinfect hand
            // tools until after the AR house has been placed. This
            // ensures the spray bottle and cleaning rag only appear
            // once the virtual house is visible, not as soon as AR
            // starts.
            if (mode == MissionMode.DisinfectHouse && disinfectToolsRoot != null)
            {
                placementManager.OnHousePlaced -= HandleDisinfectHousePlaced;
                placementManager.OnHousePlaced += HandleDisinfectHousePlaced;
            }

            placementManager.BeginPlacement(activeHouseRoot, taskForAR);
        }
        else
        {
            // Fallback: immediate parenting without tap-to-place.
            AttachHouseToARRoot(activeHouseRoot);

            // In the fallback path, start HiddenDanger spawning
            // immediately so behaviour matches the legacy setup.
            if (hiddenDangerSpawner != null && mode == MissionMode.HiddenDanger)
            {
                hiddenDangerSpawner.StartSpawning();
            }
        }

        Debug.Log($"AfterRecoveryARController: AR session started for mode {mode}.");
    }

    /// <summary>
    /// Called when the Hidden Danger house has been placed in AR
    /// space. Starts the hazard spawner so snakes/rats appear at
    /// the correct spawn points relative to the placed house.
    /// </summary>
    private void HandleHiddenDangerHousePlaced(GameObject houseRoot)
    {
        if (placementManager != null)
            placementManager.OnHousePlaced -= HandleHiddenDangerHousePlaced;

        if (hiddenDangerSpawner != null && CurrentMissionMode == MissionMode.HiddenDanger)
        {
            hiddenDangerSpawner.StartSpawning();
        }
    }

    /// <summary>
    /// Called when the DisinfectHouse AR house has been placed.
    /// Enables the disinfect hand tools so the spray bottle and
    /// cleaning rag appear only after the house is visible.
    /// </summary>
    private void HandleDisinfectHousePlaced(GameObject houseRoot)
    {
        if (placementManager != null)
            placementManager.OnHousePlaced -= HandleDisinfectHousePlaced;

        if (CurrentMissionMode == MissionMode.DisinfectHouse && disinfectToolsRoot != null)
        {
            disinfectToolsRoot.SetActive(true);
        }
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

        // Always hide disinfect hand tools when AR ends so they
        // do not leak into other missions or scenes.
        if (disinfectToolsRoot != null)
            disinfectToolsRoot.SetActive(false);

        // Extra safety: force-disable any hand-held items that
        // still have HandItemsFollowARCamera attached, even if
        // they were not wired under disinfectToolsRoot.
        HideAllHandItemsFollowingARCamera();

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

        // Restore any player GameObject we disabled when AR started.
        if (disabledPlayerByThisController && disabledPlayer != null)
        {
            try
            {
                disabledPlayer.SetActive(true);
            }
            catch { }
            disabledPlayer = null;
            disabledPlayerByThisController = false;
        }
        // Clean up any lingering feedback icons when AR ends.
        ClearFeedbackIcons();

        Debug.Log("AfterRecoveryARController: AR session disabled.");
    }

    /// <summary>
    /// Finds all HandItemsFollowARCamera helpers in the scene and
    /// disables their GameObjects. This is used as a safety net
    /// so that disinfect hand tools cannot remain visible when AR
    /// ends or when switching to a non-DisinfectHouse AR mode.
    /// </summary>
    private void HideAllHandItemsFollowingARCamera()
    {
        var items = FindObjectsOfType<HandItemsFollowARCamera>(true);
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item != null && item.gameObject.activeSelf)
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    private void ClearFeedbackIcons()
    {
        if (feedbackRoot == null)
            return;

        for (int i = feedbackRoot.childCount - 1; i >= 0; i--)
        {
            var child = feedbackRoot.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void AttachHouseToARRoot(GameObject houseRoot)
    {
        if (houseRoot == null)
            return;

        var ctx = ARRuntimeContext.Instance;
        if (ctx == null || ctx.ARRoot == null)
            return;

        // First, try to anchor the house on a detected plane at the
        // centre of the screen. If this succeeds, the house will be
        // parented under the ARAnchor and kept stable by ARCore/ARKit.
        if (ctx.TryAnchorHouseAtScreenCenter(houseRoot))
            return;

        // Fallback: if anchoring is not available (e.g., no planes
        // detected yet or no ARAnchorManager present), keep the old
        // behaviour of parenting directly under ARRoot.
        Transform houseTransform = houseRoot.transform;
        Transform arRootTransform = ctx.ARRoot.transform;

        if (houseTransform.parent == arRootTransform)
            return;

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

        // Optionally rotate icon toward camera here if needed.

        if (feedbackLifetime > 0f)
        {
            Destroy(iconInstance, feedbackLifetime);
        }

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