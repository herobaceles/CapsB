using UnityEngine;

/// <summary>
/// Manages the Before Mission phase - preparation tasks.
/// Examples: Packing emergency bags, securing the home, planning evacuation routes.
/// Inherits from MissionSceneManager for core functionality.
/// </summary>
public class BeforeMissionManager : MissionSceneManager
{
    [Header("Before Phase Specific")]
    [SerializeField] private GameObject preparationUI;
    [SerializeField] private GameObject inventoryPanel;

    protected override void Awake()
    {
        base.Awake();
        // Phase-specific initialization
    }

    protected override void Start()
    {
        base.Start();

        // Setup phase-specific UI
        if (preparationUI != null)
            preparationUI.SetActive(true);
    }

    protected override string GetCompletionMessage()
    {
        return "Excellent preparation! You're ready for when the flood comes. Being prepared saves lives!";
    }

    /// <summary>
    /// Check if player has collected all required items for emergency kit
    /// </summary>
    public bool IsEmergencyKitComplete()
    {
        // Override logic for checking collected items
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
}
