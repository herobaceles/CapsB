using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Mission controller for "Before_03 – Securing Appliances".
/// Handles timer, flood line, appliance validation, and dialogue.
/// </summary>
public class SecuringAppliancesManager : MonoBehaviour
{
    private const string SecuringAppliancesTaskId = "before_03_secure_appliances";

    [Header("Dialogue Data")]
    [SerializeField] private string dialogueSpeaker = "Professor Lingap";

    [Header("Dialogue Settings")]
    [SerializeField] private bool suppressDialogue = false;

    public static SecuringAppliancesManager Instance { get; private set; }

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
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statusText;

    [Header("Timer (visual only)")]
    [SerializeField] private float missionTimeSeconds = 300f; // 5 minutes

    [Header("AR Return Sync")]
    [SerializeField] private bool syncPlacementsBackToScene = true;
    [SerializeField] private bool despawnSpawnedHouseOnReturn = true;

    private float timeRemaining;
    private bool missionActive;
    private int illegalMoves;
    private bool missionStarted;
    private bool applianceEventsHooked;
    private bool appliancesRegistered;
    private Transform spawnedHouseRoot;
    private ApplianceSecureItem selectedAppliance;

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

        SetupFloodLine();
        UpdateStatusText();
    }

    private void OnDisable()
    {
        UnhookApplianceEvents();
    }

    private void Update()
    {
        if (!missionActive)
            return;

        UpdateTimer();
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

        ShowWarning($"Selected {selectedAppliance.ApplianceName}. Tap an elevated area.");
        UpdateStatusText();
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
            return;
        }

        if (!applianceToPlace.PlaceOnArea(area))
        {
            ShowWarning("Cannot place on that area.");
            return;
        }

        ShowWarning($"Placed {applianceName} on {areaName}.");

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
        timeRemaining = missionTimeSeconds;
        missionActive = true;

        // Show all mission UI
        if (floodWarningUI != null)
            floodWarningUI.SetActive(true);
        if (timerText != null)
            timerText.gameObject.SetActive(true);
        if (statusText != null)
            statusText.gameObject.SetActive(true);
        DisableFloodLineVisuals();

        // Skip intro dialogue; go straight into gameplay.
        ShowStartQuizGate();

        UpdateStatusText();
        UpdateTimerUI();

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

    private void UpdateTimer()
    {
        timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
        UpdateTimerUI();
        // Visual-only timer; no hard fail
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
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
        if (timerText != null)
            timerText.gameObject.SetActive(false);
        if (floodLineVisualizer != null)
            floodLineVisualizer.gameObject.SetActive(false);
        if (statusText != null)
            statusText.gameObject.SetActive(false);

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
        var completeLines = BuildDialogueLines(task?.completeDialogue);

        if (ProdDialogueManager.Instance != null && completeLines != null && completeLines.Count > 0)
        {
            ProdDialogueManager.Instance.ShowDialogueSequence(completeLines, () =>
            {
                FinalizeAndReturnToScene();
            });
        }
        else
        {
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

        CompleteExpectedTask();
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

    public void ShowWarning(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
