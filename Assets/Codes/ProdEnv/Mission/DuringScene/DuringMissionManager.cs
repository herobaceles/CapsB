using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles the During-phase gameplay loop. Keeps the top-down map active,
/// routes the player into AR flood mini-scenes, and tracks health/panic state.
/// Mirrors the responsibilities of <see cref="BeforeMissionManager"/>
/// but tuned for response-phase hazards.
/// </summary>
public class DuringMissionManager : MissionSceneManager
{
    public new static DuringMissionManager Instance { get; private set; }

    [System.Serializable]
    private class FloodZoneBinding
    {
        [SerializeField] private string taskId;
        [SerializeField] private GameObject zoneRoot;

        public string TaskId => taskId;
        public GameObject ZoneRoot => zoneRoot;
    }

    [Header("During Phase UI")]
    [SerializeField] private GameObject mapUI;
    [SerializeField] private GameObject miniSceneUI;
    [SerializeField] private GameObject evacuationMarker;

    [Header("Player State")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxPanic = 100f;
    [SerializeField] private float startingHealth = 100f;
    [SerializeField] private float startingPanic = 0f;

    [Header("Flood Zone Bindings")]
    [SerializeField] private List<FloodZoneBinding> floodZoneBindings = new List<FloodZoneBinding>();

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onHealthNormalizedChanged = new UnityEvent<float>();
    [SerializeField] private UnityEvent<float> onPanicNormalizedChanged = new UnityEvent<float>();

    private float currentHealth;
    private float currentPanic;
    private FloodZoneBinding activeZone;
    private bool isMiniSceneActive;

    public float CurrentHealth => currentHealth;
    public float CurrentPanic => currentPanic;
    public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
    public float PanicPercent => maxPanic <= 0f ? 0f : currentPanic / maxPanic;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        ResetPlayerState();
    }

    protected override void Start()
    {
        base.Start();
        ConfigureDefaultUIState();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
            Instance = null;
    }

    private void ConfigureDefaultUIState()
    {
        if (mapUI != null)
            mapUI.SetActive(true);
        if (miniSceneUI != null)
            miniSceneUI.SetActive(false);
        if (evacuationMarker != null)
            evacuationMarker.SetActive(false);
    }

    private void ResetPlayerState()
    {
        currentHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
        currentPanic = Mathf.Clamp(startingPanic, 0f, maxPanic);
        PublishPlayerState();
    }

    public override void StartTask(int taskIndex)
    {
        base.StartTask(taskIndex);
        HighlightFloodZone(CurrentTask?.taskId);

        if (evacuationMarker != null)
            evacuationMarker.SetActive(taskIndex >= TotalTasks - 1);
    }

    /// <summary>
    /// Called by flood zone triggers when the player steps into a hazard area.
    /// Switches UI from the map to the mini-scene representation.
    /// </summary>
    public void EnterFloodZone(string taskId)
    {
        if (!IsMissionActive || CurrentTask == null)
            return;

        if (!string.Equals(taskId, CurrentTask.taskId, System.StringComparison.OrdinalIgnoreCase))
            return;

        ToggleMiniScene(true);
    }

    /// <summary>
    /// Call when the mini-scene decisions are done and the player returns to the map.
    /// </summary>
    public void ExitFloodZone()
    {
        ToggleMiniScene(false);
    }

    /// <summary>
    /// Applies the result of a player decision inside a mini-scene.
    /// Positive delta heals/rests, negative damages or raises panic.
    /// </summary>
    public void ApplyDecisionOutcome(float healthDelta, float panicDelta)
    {
        currentHealth = Mathf.Clamp(currentHealth + healthDelta, 0f, maxHealth);
        currentPanic = Mathf.Clamp(currentPanic + panicDelta, 0f, maxPanic);
        PublishPlayerState();
        EvaluatePlayerState();
    }

    /// <summary>
    /// Completes the active task after the flood zone interactions succeed.
    /// Typically invoked by the mini-scene controller.
    /// </summary>
    public void CompleteActiveZone()
    {
        if (!IsMissionActive || CurrentTask == null)
            return;

        CompleteCurrentTask();
        ToggleMiniScene(false);
    }

    private void ToggleMiniScene(bool active)
    {
        isMiniSceneActive = active;

        if (mapUI != null)
            mapUI.SetActive(!active);
        if (miniSceneUI != null)
            miniSceneUI.SetActive(active);
    }

    private void HighlightFloodZone(string taskId)
    {
        activeZone = null;
        for (int i = 0; i < floodZoneBindings.Count; i++)
        {
            var binding = floodZoneBindings[i];
            if (binding == null || binding.ZoneRoot == null)
                continue;

            bool enable = !string.IsNullOrWhiteSpace(taskId) &&
                          string.Equals(binding.TaskId, taskId, System.StringComparison.OrdinalIgnoreCase);

            binding.ZoneRoot.SetActive(enable);
            if (enable)
                activeZone = binding;
        }
    }

    private void PublishPlayerState()
    {
        onHealthNormalizedChanged?.Invoke(HealthPercent);
        onPanicNormalizedChanged?.Invoke(PanicPercent);
    }

    private void EvaluatePlayerState()
    {
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            PublishPlayerState();
            HandleMissionFailure("You ran out of stamina while crossing the flood zone.");
        }
        else if (currentPanic >= maxPanic)
        {
            currentPanic = maxPanic;
            PublishPlayerState();
            HandleMissionFailure("Your panic level spiked. Take a breather and try again.");
        }
    }

    private void HandleMissionFailure(string reason)
    {
        Debug.LogWarning($"DuringMissionManager: Mission failed - {reason}");
        isMissionActive = false;
        ToggleMiniScene(false);
        ReturnToMissionSelect();
    }

    /// <summary>
    /// Invoked by the backpack HUD button so players can recover the map view on demand.
    /// </summary>
    public void ShowMapFromBackpack()
    {
        if (IsPaused)
            return;

        if (!isMiniSceneActive)
            return;

        ToggleMiniScene(false);
    }
}
