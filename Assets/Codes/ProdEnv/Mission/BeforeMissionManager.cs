using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the Before Mission phase - preparation tasks.
/// Examples: Packing emergency bags, securing the home, planning evacuation routes.
/// Integrates with AR handlers for immersive experiences.
/// </summary>
public class BeforeMissionManager : MissionSceneManager
{
    public static BeforeMissionManager Instance { get; private set; }

    [System.Serializable]
    private class MissionTriggerBinding
    {
        [SerializeField] private string missionId;
        [SerializeField] private List<GameObject> triggerObjects = new List<GameObject>();

        public string MissionId => missionId;
        public List<GameObject> TriggerObjects => triggerObjects;
    }

    [Header("Before Phase Specific")]
    [SerializeField] private GameObject preparationUI;
    [SerializeField] private GameObject inventoryPanel;

    [Header("AR Mission")]
    [SerializeField] private GameObject arSession;
    [SerializeField] private GameObject arSessionRoot;
    [SerializeField] private Camera normalCamera;
    [SerializeField] private Camera arCamera;

    [Header("UI Panels")]
    [SerializeField] private GameObject gameUI;

    [Header("Mission Trigger Bindings")]
    [SerializeField] private List<MissionTriggerBinding> missionTriggerBindings = new List<MissionTriggerBinding>();

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();

        ApplyMissionTriggerBindings();

        // Show preparation UI by default
        if (preparationUI != null)
            preparationUI.SetActive(true);

        // Ensure AR camera is disabled at scene start
        if (arCamera != null)
            arCamera.gameObject.SetActive(false);
    }

    private void ApplyMissionTriggerBindings()
    {
        if (missionTriggerBindings == null || missionTriggerBindings.Count == 0)
            return;

        string activeMissionId = currentMission != null ? currentMission.missionId : null;
        bool hasActiveMission = !string.IsNullOrWhiteSpace(activeMissionId);

        foreach (var binding in missionTriggerBindings)
        {
            if (binding == null || binding.TriggerObjects == null)
                continue;

            bool shouldEnable = hasActiveMission &&
                                !string.IsNullOrWhiteSpace(binding.MissionId) &&
                                string.Equals(binding.MissionId.Trim(), activeMissionId.Trim(), System.StringComparison.OrdinalIgnoreCase);

            foreach (var triggerObject in binding.TriggerObjects)
            {
                if (triggerObject != null)
                    triggerObject.SetActive(shouldEnable);
            }
        }

        if (hasActiveMission)
        {
            Debug.Log($"BeforeMissionManager: Applied mission trigger bindings for mission '{activeMissionId}'.");
        }
        else
        {
            Debug.LogWarning("BeforeMissionManager: No active mission found while applying mission trigger bindings. All bound triggers were disabled.");
        }
    }

    protected override IEnumerator BeginMissionSequence()
    {
        // Wait for PreparingGoBagManager cutscene to finish before starting mission tasks
        if (PreparingGoBagManager.Instance != null && PreparingGoBagManager.Instance.IsCutscenePlaying)
        {
            Debug.Log("BeforeMissionManager: Waiting for cutscene to finish...");
            yield return new WaitUntil(() => !PreparingGoBagManager.Instance.IsCutscenePlaying);
            Debug.Log("BeforeMissionManager: Cutscene finished, starting mission.");
        }

        yield return base.BeginMissionSequence();
    }

    /// <summary>
    /// Starts the AR mission phase.
    /// Called by ARMissionTrigger.
    /// </summary>
    public void StartARMission()
    {
        Debug.Log("AR Mission Started");

        // Hide normal UI
        if (preparationUI != null)
            preparationUI.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (gameUI != null)
            gameUI.SetActive(false);

        // Disable normal gameplay camera
        if (normalCamera != null)
            normalCamera.gameObject.SetActive(false);

        // Enable AR systems
        if (arSession != null)
            arSession.SetActive(true);

        if (arSessionRoot != null)
            arSessionRoot.SetActive(true);

        if (arCamera != null)
            arCamera.gameObject.SetActive(true);
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
