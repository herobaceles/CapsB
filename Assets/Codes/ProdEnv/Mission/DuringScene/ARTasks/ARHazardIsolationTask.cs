using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AR task for placing warning barriers around electrical hazards.
/// Player drags cones, tape, and signs to secure the danger zone.
/// </summary>
public class ARHazardIsolationTask : ARTaskBase
{
    [Header("Hazard Area")]
    [SerializeField] private RectTransform hazardZone;
    [SerializeField] private Image hazardHighlight;

    [Header("Required Items")]
    [SerializeField] private List<BarrierItem> requiredBarriers = new List<BarrierItem>();

    [Header("Placement Slots")]
    [SerializeField] private List<RectTransform> placementSlots = new List<RectTransform>();

    [Header("Feedback")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image dangerIndicator;

    [Header("Learning Content")]
    [TextArea(2, 4)]
    [SerializeField] private string learningMessage = "Always assume downed wires are live. Secure the area and keep others away!";

    private int correctPlacements;
    private Dictionary<BarrierItem, bool> placedItems = new Dictionary<BarrierItem, bool>();

    [System.Serializable]
    public class BarrierItem
    {
        public string itemId;
        public RectTransform rectTransform;
        public BarrierType type;
        [HideInInspector] public Vector2 originalPosition;
        [HideInInspector] public bool isPlaced;
    }

    public enum BarrierType
    {
        Cone,
        BarrierTape,
        DangerSign
    }

    protected override void OnTaskShow()
    {
        correctPlacements = 0;
        placedItems.Clear();

        foreach (var item in requiredBarriers)
        {
            if (item.rectTransform != null)
            {
                item.originalPosition = item.rectTransform.anchoredPosition;
                item.isPlaced = false;
                placedItems[item] = false;

                // Add drag handler
                var handler = item.rectTransform.GetComponent<HazardDragHandler>();
                if (handler == null)
                {
                    handler = item.rectTransform.gameObject.AddComponent<HazardDragHandler>();
                }
                handler.Initialize(item, this);
            }
        }

        UpdateProgress();

        if (dangerIndicator != null)
            dangerIndicator.color = Color.red;
    }

    protected override void OnTaskHide()
    {
        foreach (var item in requiredBarriers)
        {
            if (item.rectTransform != null)
            {
                var handler = item.rectTransform.GetComponent<HazardDragHandler>();
                if (handler != null)
                    Destroy(handler);
            }
        }
    }

    protected override bool ValidateCompletion()
    {
        // Need all barriers placed correctly
        return correctPlacements >= requiredBarriers.Count;
    }

    public void OnBarrierPlaced(BarrierItem item, Vector2 dropPosition)
    {
        if (!isActive || isCompleted) return;

        // Check if dropped on a valid placement slot
        RectTransform nearestSlot = FindNearestSlot(dropPosition);

        if (nearestSlot != null && IsValidPlacement(item, nearestSlot))
        {
            // Snap to slot
            item.rectTransform.anchoredPosition = nearestSlot.anchoredPosition;
            item.isPlaced = true;
            placedItems[item] = true;
            correctPlacements++;

            Debug.Log($"ARHazardIsolationTask: {item.type} placed correctly ({correctPlacements}/{requiredBarriers.Count})");

            UpdateProgress();
            CheckCompletion();
        }
        else
        {
            // Invalid placement - return to original
            item.rectTransform.anchoredPosition = item.originalPosition;

            if (DuringMissionStoryDirector.Instance != null)
            {
                DuringMissionStoryDirector.Instance.SpeakLine("That barrier needs to go around the hazard area.", 2f);
            }
        }
    }

    private RectTransform FindNearestSlot(Vector2 position)
    {
        float minDist = 100f; // Snap distance threshold
        RectTransform nearest = null;

        foreach (var slot in placementSlots)
        {
            if (slot == null) continue;

            // Check if position is within slot bounds
            if (RectTransformUtility.RectangleContainsScreenPoint(slot, position))
            {
                return slot;
            }
        }

        return nearest;
    }

    private bool IsValidPlacement(BarrierItem item, RectTransform slot)
    {
        // In a more complex implementation, you'd check if this slot
        // accepts this type of barrier. For now, accept any barrier in any slot.
        return slot != null;
    }

    private void UpdateProgress()
    {
        if (progressText != null)
        {
            progressText.text = $"Barriers placed: {correctPlacements}/{requiredBarriers.Count}";
        }

        // Update danger indicator
        if (dangerIndicator != null)
        {
            float progress = requiredBarriers.Count > 0 
                ? (float)correctPlacements / requiredBarriers.Count 
                : 0f;

            dangerIndicator.color = Color.Lerp(Color.red, Color.green, progress);
        }
    }
}

/// <summary>
/// Drag handler for hazard isolation barriers.
/// </summary>
public class HazardDragHandler : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler, 
    UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IEndDragHandler
{
    private ARHazardIsolationTask.BarrierItem item;
    private ARHazardIsolationTask task;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Vector2 dragOffset;

    public void Initialize(ARHazardIsolationTask.BarrierItem item, ARHazardIsolationTask task)
    {
        this.item = item;
        this.task = task;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (item == null || item.isPlaced) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        dragOffset = item.rectTransform.anchoredPosition - localPoint;
        item.rectTransform.SetAsLastSibling();
    }

    public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (item == null || item.isPlaced) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            item.rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (item == null || item.isPlaced) return;

        task?.OnBarrierPlaced(item, eventData.position);
    }
}
