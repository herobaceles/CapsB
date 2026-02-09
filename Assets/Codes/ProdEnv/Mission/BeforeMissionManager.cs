using UnityEngine;
using System.Collections;
using BaHanda.AR;

/// <summary>
/// Manages the Before Mission phase - preparation tasks.
/// Examples: Packing emergency bags, securing the home, planning evacuation routes.
/// Integrates with AR handlers for immersive experiences.
/// </summary>
public class BeforeMissionManager : MissionSceneManager
{
    [Header("Before Phase Specific")]
    [SerializeField] private GameObject preparationUI;
    [SerializeField] private GameObject inventoryPanel;

    [Header("AR Handlers")]
    [SerializeField] private GoBagPackingARHandler goBagARHandler;

    // Current active AR handler
    private IARMissionHandler activeARHandler;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        // Setup phase-specific UI
        if (preparationUI != null)
            preparationUI.SetActive(true);

        // Subscribe to AR handler events
        SubscribeToARHandlers();
    }

    private void SubscribeToARHandlers()
    {
        if (goBagARHandler != null)
        {
            goBagARHandler.OnARCompleted += OnGoBagARCompleted;
            goBagARHandler.OnProgressChanged += OnARProgressChanged;
        }
    }

    private void UnsubscribeFromARHandlers()
    {
        if (goBagARHandler != null)
        {
            goBagARHandler.OnARCompleted -= OnGoBagARCompleted;
            goBagARHandler.OnProgressChanged -= OnARProgressChanged;
        }
    }

    protected override string GetCompletionMessage()
    {
        return "Excellent preparation! Your emergency kit is ready. Being prepared saves lives!";
    }

    #region AR Integration

    /// <summary>
    /// Start the Go Bag packing AR experience.
    /// Called by TaskTrigger when player enters the trigger zone.
    /// </summary>
    public void StartGoBagPackingAR()
    {
        if (goBagARHandler == null)
        {
            Debug.LogError("BeforeMissionManager: GoBagPackingARHandler not assigned!");
            return;
        }

        Debug.Log("BeforeMissionManager: Starting Go Bag AR experience");

        activeARHandler = goBagARHandler;
        goBagARHandler.StartAR();
    }

    /// <summary>
    /// Called when Go Bag AR is completed
    /// </summary>
    private void OnGoBagARCompleted()
    {
        Debug.Log("BeforeMissionManager: Go Bag AR completed!");

        StartCoroutine(CompleteARTaskSequence());
    }

    /// <summary>
    /// Called when AR progress changes
    /// </summary>
    private void OnARProgressChanged(float progress)
    {
        Debug.Log($"BeforeMissionManager: AR progress = {progress:P0}");

        // Could update UI here
    }

    private IEnumerator CompleteARTaskSequence()
    {
        // Wait for visual feedback
        yield return new WaitForSeconds(1.5f);

        // End AR session
        if (activeARHandler != null)
        {
            activeARHandler.EndAR();
            activeARHandler = null;
        }

        yield return new WaitForSeconds(0.5f);

        // Complete the current mission task
        CompleteCurrentTask();
    }

    /// <summary>
    /// End any active AR session
    /// </summary>
    public void EndActiveAR()
    {
        if (activeARHandler != null && activeARHandler.IsActive)
        {
            activeARHandler.EndAR();
            activeARHandler = null;
        }
    }

    #endregion

    #region Task Trigger Integration

    /// <summary>
    /// Override to intercept trigger activation and start AR instead of completing directly.
    /// </summary>
    public override void OnTriggerActivated(string taskId)
    {
        if (!IsMissionActive) return;

        // Check if current task matches and requires AR
        if (CurrentTask != null && CurrentTask.taskId == taskId)
        {
            if (ShouldStartARForTask(taskId))
            {
                StartARForTask(taskId);
            }
            else
            {
                // No AR needed, complete directly
                base.OnTriggerActivated(taskId);
            }
        }
    }

    /// <summary>
    /// Determines if a task should launch AR mode
    /// </summary>
    private bool ShouldStartARForTask(string taskId)
    {
        string id = taskId.ToLower();
        return id == "go_bag" || id == "gobag" || id == "prepare_gobag" || id == "emergency_kit";
    }

    /// <summary>
    /// Start the appropriate AR experience for the given task.
    /// </summary>
    private void StartARForTask(string taskId)
    {
        Debug.Log($"BeforeMissionManager: Starting AR for task - {taskId}");

        switch (taskId.ToLower())
        {
            case "go_bag":
            case "gobag":
            case "prepare_gobag":
            case "emergency_kit":
                StartGoBagPackingAR();
                break;

            // Add more task types here for future Before missions
            // case "circuit_breaker":
            //     StartCircuitBreakerAR();
            //     break;

            default:
                Debug.LogWarning($"BeforeMissionManager: Unknown AR task ID - {taskId}");
                // Fall back to base behavior
                base.OnTriggerActivated(taskId);
                break;
        }
    }

    #endregion

    #region Legacy Methods

    /// <summary>
    /// Check if player has collected all required items for emergency kit
    /// </summary>
    public bool IsEmergencyKitComplete()
    {
        if (goBagARHandler != null)
        {
            return goBagARHandler.IsCompleted;
        }

        // Fallback to base objective checking
        if (CurrentTask == null) return false;

        foreach (var objective in CurrentTask.objectives)
        {
            if (!objective.isCompleted)
                return false;
        }
        return true;
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

    #endregion

    protected override void OnDestroy()
    {
        UnsubscribeFromARHandlers();
        base.OnDestroy();
    }
}
