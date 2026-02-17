using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Manages the Before Mission phase - preparation tasks.
/// Examples: Packing emergency bags, securing the home, planning evacuation routes.
/// Integrates with AR handlers for immersive experiences.
/// </summary>
[System.Serializable]
public class MissionTrigger
{
    public string missionId;         // e.g., "before_01"
    public GameObject triggerObject; // The trigger GameObject for this mission
}

public class BeforeMissionManager : MissionSceneManager
{
    public new static BeforeMissionManager Instance { get; private set; }

    [Header("Before Phase Specific")]
    [SerializeField] private GameObject preparationUI;
    [SerializeField] private GameObject inventoryPanel;
    [Header("Task Info UI")]
    [SerializeField] private TMPro.TextMeshProUGUI selectedTaskText; // Assign in inspector: TextMeshProUGUI above main panel
    [SerializeField] private TMPro.TextMeshProUGUI selectedTaskDescriptionText; // Assign in inspector: TextMeshProUGUI for description
    [SerializeField] private TMPro.TextMeshProUGUI selectedTaskObjectivesText; // Assign in inspector: TextMeshProUGUI for objectives/tasks
        [Header("Task Panels")]
        [SerializeField] private GameObject goBagPanel;
        [SerializeField] private GameObject circuitBreakerPanel;
        [SerializeField] private GameObject appliancesPanel;

    [Header("AR Mission")]
    [SerializeField] private GameObject arSession;
    [SerializeField] private GameObject arSessionRoot;
    [SerializeField] private Camera normalCamera;
    [SerializeField] private Camera arCamera;

    [Header("UI Panels")]
    [SerializeField] private GameObject gameUI;

    [Header("Mission Triggers")]
    public List<MissionTrigger> missionTriggers;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();

        // Show preparation UI by default
        if (preparationUI != null)
            preparationUI.SetActive(true);

        // Ensure AR camera is disabled at scene start
        if (arCamera != null)
            arCamera.gameObject.SetActive(false);
        
        // Set the selected task text UI
            var mission = MissionSelectManager.SelectedMission;
            var goBagManager = GameObject.FindObjectOfType<PreparingGoBagManager>(true);
            var circuitBreakerManager = GameObject.FindObjectOfType<CircuitBreakerManager>(true);
            var appliancesManager = GameObject.FindObjectOfType<AppliancesTaskManager>(true);
            if (goBagManager != null) goBagManager.gameObject.SetActive(false);
            if (circuitBreakerManager != null) circuitBreakerManager.gameObject.SetActive(false);
            if (appliancesManager != null) appliancesManager.gameObject.SetActive(false);

            if (mission != null)
            {
                if (selectedTaskText != null)
                    selectedTaskText.text = $"Selected Task: {mission.missionName}";
                if (selectedTaskDescriptionText != null)
                    selectedTaskDescriptionText.text = mission.missionDescription;
                if (selectedTaskObjectivesText != null)
                {
                    if (mission.tasks != null && mission.tasks.Count > 0)
                        selectedTaskObjectivesText.text = string.Join("\n", mission.tasks.Select(t => t.taskName));
                    else
                        selectedTaskObjectivesText.text = "No objectives for this mission.";
                }

                // Unique logic for each mission/task
                if (goBagPanel != null) goBagPanel.SetActive(false);
                if (circuitBreakerPanel != null) circuitBreakerPanel.SetActive(false);
                if (appliancesPanel != null) appliancesPanel.SetActive(false);

                if (mission.missionId == "before_01") // Preparing Go Bag
                {
                    if (goBagPanel != null) goBagPanel.SetActive(true);
                    if (goBagManager != null) goBagManager.gameObject.SetActive(true);
                    if (circuitBreakerManager != null) circuitBreakerManager.gameObject.SetActive(false);
                    if (appliancesManager != null) appliancesManager.gameObject.SetActive(false);
                    Debug.Log("PreparingGoBagManager is running");
                }
                else if (mission.missionId == "before_02") // Circuit Breaker
                {
                    if (circuitBreakerPanel != null) circuitBreakerPanel.SetActive(true);
                    if (circuitBreakerManager != null) circuitBreakerManager.gameObject.SetActive(true);
                    if (goBagManager != null) goBagManager.gameObject.SetActive(false);
                    if (appliancesManager != null) appliancesManager.gameObject.SetActive(false);
                    Debug.Log("CircuitBreakerManager is running");
                }
                else if (mission.missionId == "before_03") // Securing Appliances
                {
                    if (appliancesPanel != null) appliancesPanel.SetActive(true);
                    if (appliancesManager != null) appliancesManager.gameObject.SetActive(true);
                    if (goBagManager != null) goBagManager.gameObject.SetActive(false);
                    if (circuitBreakerManager != null) circuitBreakerManager.gameObject.SetActive(false);
                    // appliancesManager?.StartAppliancesTask(); // No longer needed; AppliancesTaskManager handles intro in Start()
                    Debug.Log("AppliancesTaskManager is running");
                }
            }
        else
        {
            if (selectedTaskText != null)
                selectedTaskText.text = "No mission selected.";
            if (selectedTaskDescriptionText != null)
                selectedTaskDescriptionText.text = "";
            if (selectedTaskObjectivesText != null)
                selectedTaskObjectivesText.text = "";
            if (goBagPanel != null) goBagPanel.SetActive(false);
            if (circuitBreakerPanel != null) circuitBreakerPanel.SetActive(false);
            if (appliancesPanel != null) appliancesPanel.SetActive(false);
        }

        // Deactivate all triggers first
        if (missionTriggers != null)
        {
            foreach (var trig in missionTriggers)
            {
                if (trig.triggerObject != null)
                    trig.triggerObject.SetActive(false);
            }
        }
        // Activate only the trigger for the selected mission
        if (mission != null && missionTriggers != null)
        {
            foreach (var trig in missionTriggers)
            {
                if (trig.missionId == mission.missionId && trig.triggerObject != null)
                {
                    trig.triggerObject.SetActive(true);
                    Debug.Log($"Activating trigger: {trig.triggerObject.name}");
                }
            }
        }
    }

    /// <summary>
    /// Starts the AR mission phase. Called by ARMissionTrigger.
    /// </summary>
    public void StartARMission()
    {
        Debug.Log("AR Mission Started");

        // Hide normal UI panels
        if (preparationUI != null)
            preparationUI.SetActive(false);
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        if (gameUI != null)
            gameUI.SetActive(false);

        // Hide all task panels
        if (goBagPanel != null) goBagPanel.SetActive(false);
        if (circuitBreakerPanel != null) circuitBreakerPanel.SetActive(false);

        // Switch cameras
        if (normalCamera != null)
            normalCamera.gameObject.SetActive(false);
        if (arSession != null)
            arSession.SetActive(true);
        if (arSessionRoot != null)
            arSessionRoot.SetActive(true);
        if (arCamera != null)
            arCamera.gameObject.SetActive(true);

        // Reset AR mission system if needed
        if (ARMissionManager.Instance != null)
        {
            // Optional: reset values if needed later
        }
    }

    /// <summary>
    /// Ends AR mission and returns to normal gameplay.
    /// </summary>
    public void EndARMission()
    {
        if (arSession != null)
            arSession.SetActive(false);

        if (arSessionRoot != null)
            arSessionRoot.SetActive(false);

        if (arCamera != null)
            arCamera.gameObject.SetActive(false);

        if (normalCamera != null)
            normalCamera.gameObject.SetActive(true);

        if (preparationUI != null)
            preparationUI.SetActive(true);

        if (gameUI != null)
            gameUI.SetActive(true);
    }

    /// <summary>
    /// Show the inventory panel
    /// </summary>
    public void ShowInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
    }

    /// <summary>
    /// Hide the inventory panel
    /// </summary>
    public void HideInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
            Instance = null;
    }
}
