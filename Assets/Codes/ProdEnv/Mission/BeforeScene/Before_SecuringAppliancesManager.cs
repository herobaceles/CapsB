using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using UnityEngine.UI;

/// <summary>
/// Mission controller for "Before_03 – Securing Appliances".
/// Handles timer, flood line, appliance validation, and dialogue.
/// </summary>
public class Before_SecuringAppliancesManager : MonoBehaviour
{
    private const string SecuringAppliancesTaskId = "before_03_secure_appliances";

    [Header("Dialogue Data")]
    [SerializeField] private string dialogueSpeaker = "Prof. Lingap";

    [Header("Dialogue Settings")]
    [SerializeField] private bool suppressDialogue = false;

    public static Before_SecuringAppliancesManager Instance { get; private set; }

    [Header("Mission Id")]
    [SerializeField] private string missionId = "before_03";
    [SerializeField] private bool ignoreMissionIdCheck = false;
    [SerializeField] private bool autoStartOnEnable = true;

    [Header("Appliances")]
    [SerializeField] private List<ApplianceSecureItem> appliances = new List<ApplianceSecureItem>();
    [SerializeField] private List<ApplianceElevatedArea> elevatedAreas = new List<ApplianceElevatedArea>();
    [SerializeField] private bool waitForRuntimeApplianceRegistration = true;
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private ARCameraBinder cameraBinder;

    [Header("Flood Settings")]
    [SerializeField] private float projectedFloodHeight = 0.8f; // meters in world space
    [SerializeField] private Transform floodLine; // optional visual line; its Y will be set to projectedFloodHeight
    [SerializeField] private LineRenderer floodLineVisualizer; // optional line renderer to show flood height
    [SerializeField] private TMP_Text floodHeightHintText; // UI text showing "Raise above flood line"

    [Header("UI")]
    [SerializeField] private GameObject floodWarningUI;
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject achievementsPanel;
    [SerializeField] private TMP_Text achievementText;
    [SerializeField] private Button achievementProceedButton;
    [SerializeField] private Button achievementRestartButton;

    [Header("Audio")]
    [SerializeField] private AudioClip selectApplianceSfx;
    [SerializeField] private AudioClip placeApplianceSfx;
    [SerializeField] private AudioClip illegalMoveSfx;
    [SerializeField] private AudioClip achievementCompleteSfx;

    [Header("AR Return Sync")]
    [SerializeField] private bool syncPlacementsBackToScene = true;
    [SerializeField] private bool despawnSpawnedHouseOnReturn = true;
    
    [Header("AR Placement")]
    [SerializeField] private ApplianceARPlacementManager03 arPlacementManager;
    private bool missionActive;
    private int illegalMoves;
    private bool missionStarted;
    private bool applianceEventsHooked;
    private bool appliancesRegistered;
    private Transform spawnedHouseRoot;
    private ApplianceSecureItem selectedAppliance;
    private bool arGuidanceShown;
    private bool achievementsPanelResolved;

    // Cache of initial local transforms so the AR placement can be
    // restarted without reloading the entire scene/AR content.
    private readonly Dictionary<ApplianceSecureItem, Vector3> initialLocalPositions =
        new Dictionary<ApplianceSecureItem, Vector3>();
    private readonly Dictionary<ApplianceSecureItem, Quaternion> initialLocalRotations =
        new Dictionary<ApplianceSecureItem, Quaternion>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        suppressDialogue = false;
        appliancesRegistered = !waitForRuntimeApplianceRegistration && appliances != null && appliances.Count > 0;

        if (cameraBinder == null)
            cameraBinder = GetComponent<ARCameraBinder>();
    }

    private void OnDestroy()
    {
        UnhookAchievementButtons();

        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        if (autoStartOnEnable)
            TryStartMission();
    }

    public void StartMissionFromTrigger()
    {
        TryStartMission();
    }

    public void InitializeFromSpawnedRoot(GameObject spawnedRoot)
    {
        if (spawnedRoot == null)
            return;

        spawnedHouseRoot = spawnedRoot.transform;
        missionStarted = false;
        missionActive = false;
        illegalMoves = 0;
        ClearSelection();

        UnhookApplianceEvents();
        appliances = new List<ApplianceSecureItem>(spawnedRoot.GetComponentsInChildren<ApplianceSecureItem>(true));
        elevatedAreas = new List<ApplianceElevatedArea>(spawnedRoot.GetComponentsInChildren<ApplianceElevatedArea>(true));
        appliancesRegistered = appliances.Count > 0;

        // Record starting local transforms so we can restore them on restart.
        initialLocalPositions.Clear();
        initialLocalRotations.Clear();
        foreach (var app in appliances)
        {
            if (app == null) continue;
            initialLocalPositions[app] = app.transform.localPosition;
            initialLocalRotations[app] = app.transform.localRotation;
        }

        SetupFloodLine();
        UpdateStatusText();
    }

    private void ResolveArPlacementManager()
    {
        if (arPlacementManager != null)
            return;

        // Fallback: try to find an ApplianceARPlacementManager03 in the
        // scene so restart still works even if the reference was not
        // wired in the inspector.
        arPlacementManager = FindObjectOfType<ApplianceARPlacementManager03>(true);
    }

    private void OnDisable()
    {
        UnhookApplianceEvents();
    }

    private void Update()
    {
        if (!missionActive)
            return;

        HandleTapInput();
    }

    private void TryStartMission()
    {
        if (missionStarted)
            return;

        var activeMission = GetActiveMission();

        if (waitForRuntimeApplianceRegistration && !appliancesRegistered)
        {
            Debug.Log("SecuringAppliancesManager: Waiting for runtime appliance registration.");
            return;
        }

        if (!ignoreMissionIdCheck && activeMission != null &&
            !string.Equals(activeMission.missionId, missionId, System.StringComparison.OrdinalIgnoreCase))
        {
            // Not the active mission; keep disabled
            return;
        }

        missionStarted = true;
        SetupFloodLine();
        HookApplianceEvents();
        StartMissionFlow();
    }

    private void SetupFloodLine()
    {
        DisableFloodLineVisuals();

        // Push required heights to appliances
        foreach (var app in appliances)
        {
            if (app != null)
                app.SetRequiredFloodHeight(projectedFloodHeight);
        }
    }

    private void HandleTapInput()
    {
        if (!TryGetPointerDown(out Vector2 screenPosition, out int pointerId))
            return;

        if (IsPointerOverUI(pointerId))
            return;

        var cam = ResolveCamera();
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        var tappedAppliance = hit.collider.GetComponentInParent<ApplianceSecureItem>();
        if (tappedAppliance != null)
        {
            SelectAppliance(tappedAppliance);
            return;
        }

        var tappedArea = hit.collider.GetComponentInParent<ApplianceElevatedArea>();
        if (tappedArea != null)
        {
            TryPlaceSelectedOnArea(tappedArea);
        }
    }

    private bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = 0;
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }

        screenPosition = default;
        pointerId = int.MinValue;
        return false;
    }

    private bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        if (pointerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(pointerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    private Camera ResolveCamera()
    {
        if (cameraBinder != null)
        {
            cameraBinder.RebindCamera();
            if (cameraBinder.CurrentCamera != null)
                return cameraBinder.CurrentCamera;
        }

        if (interactionCamera != null)
            return interactionCamera;

        if (ARRuntimeContext.Instance != null)
        {
            var arCamera = ARRuntimeContext.Instance.ResolveARCamera();
            if (arCamera != null)
                return arCamera;
        }

        if (Camera.main != null)
            return Camera.main;

        var allCameras = FindObjectsOfType<Camera>();
        return allCameras.Length > 0 ? allCameras[0] : null;
    }

    private void SelectAppliance(ApplianceSecureItem appliance)
    {
        if (appliance == null)
            return;

        if (selectedAppliance == appliance)
            return;

        ClearSelection();
        selectedAppliance = appliance;
        selectedAppliance.SetSelected(true);

        if (AudioManager.Instance != null && selectApplianceSfx != null)
            AudioManager.Instance.PlaySFX(selectApplianceSfx);

        ShowWarning($"Selected {selectedAppliance.ApplianceName}. Tap an elevated area.");
        UpdateStatusText();
    UpdateAreaMarkers();
    }

    private void TryPlaceSelectedOnArea(ApplianceElevatedArea area)
    {
        if (area == null)
            return;

        if (selectedAppliance == null)
        {
            ShowWarning("Tap an appliance first.");
            return;
        }

        var applianceToPlace = selectedAppliance;
        string applianceName = applianceToPlace.ApplianceName;
        string areaName = area.AreaName;

        if (area.IsOccupiedByOther(applianceToPlace))
        {
            ShowWarning($"{areaName} is occupied.");
            if (AudioManager.Instance != null && illegalMoveSfx != null)
                AudioManager.Instance.PlaySFX(illegalMoveSfx);
            return;
        }

        if (!applianceToPlace.PlaceOnArea(area))
        {
            ShowWarning("Cannot place on that area.");
            if (AudioManager.Instance != null && illegalMoveSfx != null)
                AudioManager.Instance.PlaySFX(illegalMoveSfx);
            return;
        }

        ShowWarning($"Placed {applianceName} on {areaName}.");

        if (AudioManager.Instance != null && placeApplianceSfx != null)
            AudioManager.Instance.PlaySFX(placeApplianceSfx);

        if (!missionActive)
            return;

        ClearSelection();
        UpdateStatusText();
    }

    private void ClearSelection()
    {
        if (selectedAppliance != null)
            selectedAppliance.SetSelected(false);

        selectedAppliance = null;
        UpdateAreaMarkers();
    }

    private void HookApplianceEvents()
    {
        if (applianceEventsHooked)
            return;

        if (appliances == null || appliances.Count == 0)
        {
            Debug.LogWarning("SecuringAppliancesManager: No appliances assigned.");
            return;
        }

        foreach (var app in appliances)
        {
            if (app == null) continue;

            app.OnSecuredChanged += OnApplianceSecuredChanged;
            app.OnIllegalMove += OnIllegalMove;
        }

        applianceEventsHooked = true;
    }

    private void UnhookApplianceEvents()
    {
        if (!applianceEventsHooked || appliances == null)
            return;

        foreach (var app in appliances)
        {
            if (app == null) continue;
            app.OnSecuredChanged -= OnApplianceSecuredChanged;
            app.OnIllegalMove -= OnIllegalMove;
        }

        applianceEventsHooked = false;
    }

    private void StartMissionFlow()
    {
        missionActive = true;

        // Show all mission UI
        if (floodWarningUI != null)
            floodWarningUI.SetActive(true);
        if (statusPanel != null)
            statusPanel.SetActive(true);
        else if (statusText != null)
            statusText.gameObject.SetActive(true);
        DisableFloodLineVisuals();

        // Show optional AR guidance dialogue for this task so the player
        // knows how to interact with appliances while in AR.
        TryShowArGuidanceDialogue();

        UpdateStatusText();
    }

    private void ShowStartQuizGate()
    {
        // Quiz gate removed for AR flow; proceed directly to gameplay.
        CompleteStartGate();
    }

    private void CompleteStartGate()
    {
        if (floodWarningUI != null)
            floodWarningUI.SetActive(false);
    }

    private void TryShowArGuidanceDialogue()
    {
        if (arGuidanceShown)
            return;

        var mission = GetActiveMission();
        var task = GetTask(mission);
        var dialogueManager = ProdDialogueManager.Instance;

        if (dialogueManager == null || task == null ||
            task.arGuidanceDialogueRich == null || task.arGuidanceDialogueRich.Count == 0)
        {
            // No AR guidance configured; just proceed with normal flow.
            ShowStartQuizGate();
            return;
        }

        arGuidanceShown = true;

        // While guidance is showing, we keep missionActive true but rely on
        // the dialogue UI to focus the player's attention. When the dialogue
        // finishes, proceed into normal gameplay.
        dialogueManager.ShowDialogueSequence(task.arGuidanceDialogueRich, ShowStartQuizGate);
    }

    private TaskData GetTask(MissionData mission)
    {
        if (mission == null || mission.tasks == null)
            return null;

        foreach (var task in mission.tasks)
        {
            if (task != null && task.taskId == SecuringAppliancesTaskId)
                return task;
        }

        return null;
    }

    private List<ProdDialogueLine> BuildDialogueLines(string[] dialogue)
    {
        if (dialogue == null || dialogue.Length == 0)
            return null;

        var lines = new List<ProdDialogueLine>();
        foreach (var line in dialogue)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            lines.Add(new ProdDialogueLine(dialogueSpeaker, line));
        }

        return lines.Count > 0 ? lines : null;
    }

    private void OnApplianceSecuredChanged()
    {
        UpdateStatusText();
        if (AreAllSecured())
        {
            CompleteMission();
        }
    }

    private void OnIllegalMove()
    {
        illegalMoves++;
        if (AudioManager.Instance != null && illegalMoveSfx != null)
            AudioManager.Instance.PlaySFX(illegalMoveSfx);
        UpdateStatusText();
    }

    private bool AreAllSecured()
    {
        foreach (var app in appliances)
        {
            if (app == null) continue;
            if (!app.IsSecured)
                return false;
        }
        return true;
    }

    private void CompleteMission()
    {
        missionActive = false;
        ClearSelection();

        // Hide mission UI
        if (statusPanel != null)
            statusPanel.SetActive(false);
        else if (statusText != null)
            statusText.gameObject.SetActive(false);
        if (floodLineVisualizer != null)
            floodLineVisualizer.gameObject.SetActive(false);

        if (suppressDialogue)
        {
            FinalizeAndReturnToScene();
            return;
        }

        int securedCount = 0;
        foreach (var app in appliances)
            if (app != null && app.IsSecured) securedCount++;

        var mission = MissionSelectManager.SelectedMission;
        var task = GetTask(mission);
        var dialogueManager = ProdDialogueManager.Instance;

        // Use rich completion dialogue defined on the mission task, if present
        if (dialogueManager != null && task != null &&
            task.completeDialogueRich != null && task.completeDialogueRich.Count > 0)
        {
            dialogueManager.ShowDialogueSequence(task.completeDialogueRich, FinalizeAndReturnToScene);
        }
        else
        {
            // No completion dialogue configured; just wrap up the mission
            FinalizeAndReturnToScene();
        }
    }

    private void FinalizeAndReturnToScene()
    {
        SyncAppliancePlacementsToScene();

        if (despawnSpawnedHouseOnReturn && spawnedHouseRoot != null)
        {
            Destroy(spawnedHouseRoot.gameObject);
            spawnedHouseRoot = null;
        }

        if (BeforeMissionManager.Instance != null)
            BeforeMissionManager.Instance.EndARMission();

        ShowAchievementPanel();
    }

    private void CompleteExpectedTask()
    {
        var missionManager = BeforeMissionManager.Instance;
        if (missionManager == null || !missionManager.IsMissionActive)
            return;

        var currentTask = missionManager.CurrentTask;
        if (currentTask == null)
        {
            Debug.LogWarning("SecuringAppliancesManager: Cannot complete task because no current task is active.");
            return;
        }

        if (!string.Equals(currentTask.taskId, SecuringAppliancesTaskId, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"SecuringAppliancesManager: Current task '{currentTask.taskId}' does not match expected task '{SecuringAppliancesTaskId}'.");
            return;
        }

        missionManager.CompleteCurrentTask();
    }

    private void SyncAppliancePlacementsToScene()
    {
        if (!syncPlacementsBackToScene || spawnedHouseRoot == null || appliances == null || appliances.Count == 0)
            return;

        var allSceneAppliances = FindObjectsOfType<ApplianceSecureItem>(true);
        var candidateTargets = new List<ApplianceSecureItem>();

        foreach (var candidate in allSceneAppliances)
        {
            if (candidate == null)
                continue;

            if (candidate.transform.IsChildOf(spawnedHouseRoot))
                continue;

            candidateTargets.Add(candidate);
        }

        int syncedCount = 0;
        var usedTargets = new HashSet<ApplianceSecureItem>();

        foreach (var source in appliances)
        {
            if (source == null)
                continue;

            var target = FindBestTargetForSource(source, candidateTargets, usedTargets);
            if (target == null)
                continue;

            target.transform.localPosition = source.transform.localPosition;
            target.transform.localRotation = source.transform.localRotation;
            usedTargets.Add(target);
            syncedCount++;
        }

        Debug.Log($"SecuringAppliancesManager: Synced {syncedCount}/{appliances.Count} appliance placements from AR to scene.");
    }

    private ApplianceSecureItem FindBestTargetForSource(
        ApplianceSecureItem source,
        List<ApplianceSecureItem> candidates,
        HashSet<ApplianceSecureItem> used)
    {
        if (source == null || candidates == null)
            return null;

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (candidate == null || used.Contains(candidate))
                continue;

            if (string.Equals(candidate.name, source.name, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            if (candidate == null || used.Contains(candidate))
                continue;

            if (string.Equals(candidate.ApplianceName, source.ApplianceName, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }

    private void DisableFloodLineVisuals()
    {
        if (floodLine != null)
            floodLine.gameObject.SetActive(false);

        if (floodLineVisualizer != null)
        {
            floodLineVisualizer.enabled = false;
            floodLineVisualizer.positionCount = 0;
            floodLineVisualizer.gameObject.SetActive(false);
        }

        if (floodHeightHintText != null)
            floodHeightHintText.gameObject.SetActive(false);
    }

    private void UpdateAreaMarkers()
    {
        if (elevatedAreas == null)
            return;

        foreach (var area in elevatedAreas)
        {
            if (area == null)
                continue;

            bool show = selectedAppliance != null && area.CanAccept(selectedAppliance);
            area.SetMarkerVisible(show);
        }
    }

    private MissionData GetActiveMission()
    {
        if (MissionSelectManager.SelectedMission != null)
            return MissionSelectManager.SelectedMission;

        if (BeforeMissionManager.Instance != null && BeforeMissionManager.Instance.CurrentMission != null)
            return BeforeMissionManager.Instance.CurrentMission;

        return null;
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;
        int secured = 0;
        foreach (var app in appliances)
        {
            if (app != null)
            {
                if (app.IsSecured) secured++;
            }
        }

        string status = $"Secured: {secured}/{appliances.Count}";

        if (selectedAppliance != null)
            status += $"\nSelected: {selectedAppliance.ApplianceName}";
        else if (secured < appliances.Count)
            status += "\n[TAP APPLIANCE] then [TAP ELEVATED AREA]";

        if (illegalMoves > 0)
            status += $"\nWarnings: {illegalMoves}";

        statusText.text = status;
    }

    /// <summary>
    /// Restart the AR appliance placement, restoring all appliances to their
    /// original positions/rotations and clearing warnings. Call this from a
    /// UI button in the AR scene when the player wants to try again.
    /// </summary>
    public void RestartAppliancePlacement()
    {
        // If we have (or can find) an AR placement manager and a spawned house root,
        // treat restart as a full AR reset so the player can choose a
        // new plane and place the house again.
        ResolveArPlacementManager();

        if (arPlacementManager != null && spawnedHouseRoot == null)
        {
            missionActive = false;
            missionStarted = false;
            illegalMoves = 0;
            ClearSelection();
            UnhookApplianceEvents();
            appliancesRegistered = false;
            appliances.Clear();
            elevatedAreas.Clear();
            initialLocalPositions.Clear();
            initialLocalRotations.Clear();

            if (statusPanel != null)
                statusPanel.SetActive(false);
            else if (statusText != null)
                statusText.gameObject.SetActive(false);

            if (floodWarningUI != null)
                floodWarningUI.SetActive(false);

            DisableFloodLineVisuals();
            arPlacementManager.BeginPlacement(this);
            return;
        }

        if (arPlacementManager != null && spawnedHouseRoot != null)
        {
            // Stop current mission flow and unhook events for the
            // existing appliance instances.
            missionActive = false;
            missionStarted = false;
            illegalMoves = 0;
            ClearSelection();
            UnhookApplianceEvents();

            // Clear cached appliance data tied to the current house.
            appliancesRegistered = false;
            appliances.Clear();
            elevatedAreas.Clear();
            initialLocalPositions.Clear();
            initialLocalRotations.Clear();

            // Optionally hide mission UI until the new house is placed.
            if (statusPanel != null)
                statusPanel.SetActive(false);
            else if (statusText != null)
                statusText.gameObject.SetActive(false);
            if (floodWarningUI != null)
                floodWarningUI.SetActive(false);
            DisableFloodLineVisuals();

            // Remove the current AR house so a new one can be spawned
            // at a different location.
            Destroy(spawnedHouseRoot.gameObject);
            spawnedHouseRoot = null;

            // Begin a fresh AR placement cycle; when the player taps a
            // plane again, a new housePrefab will be spawned and this
            // manager will be re-initialized via InitializeFromSpawnedRoot.
            arPlacementManager.BeginPlacement(this);
            return;
        }

        // Fallback: if we don't have an AR placement manager reference,
        // just reset appliance transforms within the existing house.
        if (appliances == null || appliances.Count == 0)
            return;

        illegalMoves = 0;
        ClearSelection();

        foreach (var app in appliances)
        {
            if (app == null)
                continue;

            // Clear area assignment so IsSecured is recalculated as false.
            if (app.CurrentArea != null)
            {
                app.CurrentArea.Clear(app);
            }

            // Restore starting transform if we recorded it.
            if (initialLocalPositions.TryGetValue(app, out var pos))
                app.transform.localPosition = pos;
            if (initialLocalRotations.TryGetValue(app, out var rot))
                app.transform.localRotation = rot;

            app.SetSelected(false);
        }

        UpdateStatusText();
        UpdateAreaMarkers();
    }

    private void ShowAchievementPanel()
    {
        ResolveAchievementsPanel();

        if (achievementsPanel == null)
        {
            Debug.LogWarning("SecuringAppliancesManager: Achievements panel not found; completing task immediately.");
            CompleteExpectedTask();
            return;
        }

        achievementsPanel.SetActive(true);

        if (achievementCompleteSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(achievementCompleteSfx);

        if (achievementText != null)
            achievementText.text = "Securing Appliances Complete!";

        UnhookAchievementButtons();

        if (achievementProceedButton != null)
            achievementProceedButton.onClick.AddListener(OnAchievementProceedClicked);

        if (achievementRestartButton != null)
            achievementRestartButton.onClick.AddListener(OnAchievementRestartClicked);
    }

    private void HideAchievementPanel()
    {
        UnhookAchievementButtons();

        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
    }

    private void OnAchievementProceedClicked()
    {
        HideAchievementPanel();
        CompleteExpectedTask();
    }

    private void OnAchievementRestartClicked()
    {
        HideAchievementPanel();

        if (BeforeMissionManager.Instance != null && !BeforeMissionManager.Instance.IsARMissionActive)
            BeforeMissionManager.Instance.StartARMission();

        RestartAppliancePlacement();
    }

    private void UnhookAchievementButtons()
    {
        if (achievementProceedButton != null)
            achievementProceedButton.onClick.RemoveListener(OnAchievementProceedClicked);

        if (achievementRestartButton != null)
            achievementRestartButton.onClick.RemoveListener(OnAchievementRestartClicked);
    }

    private void ResolveAchievementsPanel()
    {
        if (achievementsPanelResolved)
            return;

        achievementsPanelResolved = true;

        if (achievementsPanel == null)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                var candidate = allObjects[i];
                if (candidate == null || !candidate.scene.IsValid())
                    continue;

                if (string.Equals(candidate.name, "AchievementsPanel", StringComparison.Ordinal))
                {
                    achievementsPanel = candidate;
                    break;
                }
            }
        }

        if (achievementsPanel == null)
            return;

        if (achievementText == null)
        {
            foreach (var text in achievementsPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && string.Equals(text.gameObject.name, "Text (TMP)", StringComparison.Ordinal))
                {
                    achievementText = text;
                    break;
                }
            }
        }

        if (achievementProceedButton == null)
        {
            Transform proceedTransform = achievementsPanel.transform.Find("ProceedButton");
            if (proceedTransform != null)
                achievementProceedButton = proceedTransform.GetComponent<Button>();
        }

        if (achievementRestartButton == null)
        {
            Transform restartTransform = achievementsPanel.transform.Find("RestartButton");
            if (restartTransform != null)
                achievementRestartButton = restartTransform.GetComponent<Button>();
        }
    }

    public void ShowWarning(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
