using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Developer debug panel for skipping tasks and testing mission flow.
/// Toggle visibility via inspector or keyboard shortcut.
/// </summary>
public class DuringMissionDebugPanel : MonoBehaviour
{
    [Header("Enable")]
    [SerializeField] private bool enableDebugPanel = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Buttons")]
    [SerializeField] private Button skipTaskButton;
    [SerializeField] private Button prevTaskButton;
    [SerializeField] private Button completeMissionButton;
    [SerializeField] private Button showMapButton;

    [Header("Info Display")]
    [SerializeField] private TMP_Text taskInfoText;
    [SerializeField] private TMP_Text missionInfoText;

    private bool isPanelVisible;

    private void Awake()
    {
        #if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        // Disable debug panel in release builds
        enableDebugPanel = false;
        #endif

        if (!enableDebugPanel)
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            enabled = false;
            return;
        }

        SetupButtons();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            isPanelVisible = false;
        }
    }

    private void Update()
    {
        if (!enableDebugPanel) return;

        // Toggle panel with hotkey
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }

        // Update info display
        if (isPanelVisible)
        {
            UpdateInfoDisplay();
        }
    }

    private void SetupButtons()
    {
        if (skipTaskButton != null)
            skipTaskButton.onClick.AddListener(OnSkipTaskClicked);

        if (prevTaskButton != null)
            prevTaskButton.onClick.AddListener(OnPrevTaskClicked);

        if (completeMissionButton != null)
            completeMissionButton.onClick.AddListener(OnCompleteMissionClicked);

        if (showMapButton != null)
            showMapButton.onClick.AddListener(OnShowMapClicked);
    }

    private void OnDestroy()
    {
        if (skipTaskButton != null)
            skipTaskButton.onClick.RemoveListener(OnSkipTaskClicked);

        if (prevTaskButton != null)
            prevTaskButton.onClick.RemoveListener(OnPrevTaskClicked);

        if (completeMissionButton != null)
            completeMissionButton.onClick.RemoveListener(OnCompleteMissionClicked);

        if (showMapButton != null)
            showMapButton.onClick.RemoveListener(OnShowMapClicked);
    }

    #region Panel Control

    public void TogglePanel()
    {
        if (isPanelVisible)
            HidePanel();
        else
            ShowPanel();
    }

    public void ShowPanel()
    {
        if (panelRoot == null) return;

        isPanelVisible = true;
        panelRoot.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        UpdateInfoDisplay();
        Debug.Log("DebugPanel: Opened");
    }

    public void HidePanel()
    {
        if (panelRoot == null) return;

        isPanelVisible = false;
        panelRoot.SetActive(false);
        Debug.Log("DebugPanel: Closed");
    }

    #endregion

    #region Button Handlers

    private void OnSkipTaskClicked()
    {
        var manager = DuringMissionManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("DebugPanel: DuringMissionManager not found.");
            return;
        }

        if (!manager.IsMissionActive)
        {
            Debug.LogWarning("DebugPanel: No active mission to skip task.");
            return;
        }

        Debug.Log($"DebugPanel: Skipping task {manager.CurrentTaskIndex + 1}/{manager.TotalTasks}");
        manager.DebugSkipCurrentTask();
        UpdateInfoDisplay();
    }

    private void OnPrevTaskClicked()
    {
        var manager = DuringMissionManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("DebugPanel: DuringMissionManager not found.");
            return;
        }

        if (!manager.IsMissionActive)
        {
            Debug.LogWarning("DebugPanel: No active mission.");
            return;
        }

        int prevIndex = manager.CurrentTaskIndex - 1;
        if (prevIndex >= 0)
        {
            Debug.Log($"DebugPanel: Going to previous task {prevIndex + 1}");
            manager.DebugJumpToTask(prevIndex);
            UpdateInfoDisplay();
        }
        else
        {
            Debug.LogWarning("DebugPanel: Already at first task.");
        }
    }

    private void OnCompleteMissionClicked()
    {
        var manager = DuringMissionManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("DebugPanel: DuringMissionManager not found.");
            return;
        }

        if (!manager.IsMissionActive)
        {
            Debug.LogWarning("DebugPanel: No active mission to complete.");
            return;
        }

        Debug.Log("DebugPanel: Force completing mission.");
        manager.DebugCompleteMission();
    }

    private void OnShowMapClicked()
    {
        var mapDisplay = DuringMissionMapDisplay.Instance;
        if (mapDisplay != null)
        {
            mapDisplay.ToggleMap();
        }
        else
        {
            Debug.LogWarning("DebugPanel: DuringMissionMapDisplay not found.");
        }
    }

    #endregion

    #region Info Display

    private void UpdateInfoDisplay()
    {
        var manager = DuringMissionManager.Instance;

        if (missionInfoText != null)
        {
            if (manager != null && manager.CurrentMission != null)
            {
                missionInfoText.text = $"Mission: {manager.CurrentMission.missionName}\n" +
                                       $"ID: {manager.CurrentMission.missionId}\n" +
                                       $"Active: {manager.IsMissionActive}";
            }
            else
            {
                missionInfoText.text = "No mission loaded";
            }
        }

        if (taskInfoText != null)
        {
            if (manager != null && manager.CurrentTask != null)
            {
                taskInfoText.text = $"Task {manager.CurrentTaskIndex + 1}/{manager.TotalTasks}\n" +
                                    $"ID: {manager.CurrentTask.taskId}\n" +
                                    $"Name: {manager.CurrentTask.taskName}";
            }
            else
            {
                taskInfoText.text = "No active task";
            }
        }
    }

    #endregion
}
