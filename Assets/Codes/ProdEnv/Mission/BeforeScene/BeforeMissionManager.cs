using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the Before Mission phase - preparation tasks.
/// Examples: Packing emergency bags, securing the home, planning evacuation routes.
/// Integrates with AR handlers for immersive experiences.
/// </summary>
public class BeforeMissionManager : MissionSceneManager
{
    public new static BeforeMissionManager Instance { get; private set; }

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

    [Header("Camera")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private bool disableGameplayCameraInAR = false;

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

        ResolveGameplayCamera();
        SetGameplayCameraActive(true);

        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.SetARActive(false);

        // Show preparation UI by default
        if (preparationUI != null)
            preparationUI.SetActive(true);
    }

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

        // Prefer "Main Camera" from current scene roots, excluding ARCoreRoot hierarchy
        for (int i = 0; i < sceneRoots.Length; i++)
        {
            var cameras = sceneRoots[i].GetComponentsInChildren<Camera>(true);
            for (int j = 0; j < cameras.Length; j++)
            {
                var candidate = cameras[j];
                if (candidate == null)
                    continue;

                if (arRootTransform != null && candidate.transform.IsChildOf(arRootTransform))
                    continue;

                if (arCamera != null && candidate == arCamera)
                    continue;

                if (string.Equals(candidate.name, "Main Camera", System.StringComparison.OrdinalIgnoreCase))
                {
                    gameplayCamera = candidate;
                    Debug.Log($"BeforeMissionManager: Gameplay camera bound to scene main camera '{gameplayCamera.name}'.");
                    return;
                }
            }
        }

        if (Camera.main != null)
        {
            bool isUnderArRoot = arRootTransform != null && Camera.main.transform.IsChildOf(arRootTransform);
            if ((arCamera == null || Camera.main != arCamera) && !isUnderArRoot)
            {
                gameplayCamera = Camera.main;
                Debug.Log($"BeforeMissionManager: Gameplay camera bound via Camera.main '{gameplayCamera.name}'.");
                return;
            }
        }

        var allCameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < allCameras.Length; i++)
        {
            var candidate = allCameras[i];
            if (candidate == null)
                continue;

            if (arCamera != null && candidate == arCamera)
                continue;

            if (arRootTransform != null && candidate.transform.IsChildOf(arRootTransform))
                continue;

            if (candidate.gameObject.scene != activeScene)
                continue;

            if (candidate != null)
            {
                gameplayCamera = candidate;
                Debug.Log($"BeforeMissionManager: Gameplay camera fallback bound to '{gameplayCamera.name}'.");
                break;
            }
        }

        if (gameplayCamera == null)
            Debug.LogWarning("BeforeMissionManager: No scene gameplay camera found (excluding ARCoreRoot). Black screen may occur.");
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

        if (ARRuntimeContext.Instance == null)
        {
            Debug.LogError("BeforeMissionManager: ARRuntimeContext is missing. Ensure ARBootstrapPersistent exists in Boot scene.");
            return;
        }

        // Hide normal UI
        if (preparationUI != null)
            preparationUI.SetActive(false);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (gameUI != null)
            gameUI.SetActive(false);

        ARRuntimeContext.Instance.SetARActive(true);

        if (disableGameplayCameraInAR)
            StartCoroutine(DisableGameplayCameraWhenARReady());
        else
            SetGameplayCameraActive(true);
    }

    private IEnumerator DisableGameplayCameraWhenARReady()
    {
        float timeout = 3.0f;
        while (timeout > 0f)
        {
            var arCamera = ARRuntimeContext.Instance != null ? ARRuntimeContext.Instance.ResolveARCamera() : null;
            if (arCamera != null && arCamera.gameObject.activeInHierarchy && arCamera.enabled)
            {
                SetGameplayCameraActive(false);
                yield break;
            }

            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("BeforeMissionManager: AR camera did not become ready in time. Keeping gameplay camera active to avoid black screen.");
        SetGameplayCameraActive(true);
    }

    /// <summary>
    /// Ends AR mission and returns to normal gameplay.
    /// </summary>
    public void EndARMission()
    {
        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.SetARActive(false);

        SetGameplayCameraActive(true);

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


    private bool waitingForContinue = false;

    protected override void CompleteMission()
    {
        // Hide game UI to prevent overlap
        if (gameUI != null)
            gameUI.SetActive(false);

        // Show the Mission Complete UI and wait for user to press Continue
        if (missionCompletePanel != null)
            missionCompletePanel.SetActive(true);
        if (missionCompleteTitleText != null)
            missionCompleteTitleText.text = "Before Phase Completed!";
        if (missionCompleteMessageText != null)
            missionCompleteMessageText.text = "You are now ready.";

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

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
            Instance = null;
    }
}
