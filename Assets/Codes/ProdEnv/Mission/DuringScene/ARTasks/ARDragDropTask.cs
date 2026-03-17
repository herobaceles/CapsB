using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Drag-and-drop sorting task for AR mini-games.
/// Used for route signage, hazard sorting, debris categorization, etc.
/// </summary>
public class ARDragDropTask : ARTaskBase
{
    [Header("Drag-Drop Setup")]
    [SerializeField] private List<DraggableItem> draggableItems = new List<DraggableItem>();
    [SerializeField] private List<DropZone> dropZones = new List<DropZone>();

    [Header("Validation")]
    [SerializeField] private bool requireAllCorrect = true;
    [SerializeField] private int minimumCorrect = 0;
    [SerializeField] private bool penalizeWrongPlacements = false;
    [SerializeField] private int maxWrongAttempts = 3;

    [Header("Visual Feedback")]
    [SerializeField] private Color correctPlacementColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color wrongPlacementColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private int correctPlacements;
    private int wrongAttempts;
    private Dictionary<DraggableItem, DropZone> currentPlacements = new Dictionary<DraggableItem, DropZone>();

    [System.Serializable]
    public class DraggableItem
    {
        public string itemId;
        public RectTransform rectTransform;
        public Image image;
        public string correctZoneId;
        [HideInInspector] public Vector2 originalPosition;
        [HideInInspector] public bool isPlaced;
    }

    [System.Serializable]
    public class DropZone
    {
        public string zoneId;
        public RectTransform rectTransform;
        public Image highlightImage;
        public string[] acceptedItemIds;
    }

    protected override void OnTaskShow()
    {
        correctPlacements = 0;
        wrongAttempts = 0;
        currentPlacements.Clear();

        // Store original positions and setup event handlers
        foreach (var item in draggableItems)
        {
            if (item.rectTransform != null)
            {
                item.originalPosition = item.rectTransform.anchoredPosition;
                item.isPlaced = false;

                // Add drag handlers
                var handler = item.rectTransform.GetComponent<DragHandler>();
                if (handler == null)
                {
                    handler = item.rectTransform.gameObject.AddComponent<DragHandler>();
                }
                handler.Initialize(item, this);
            }
        }

        // Reset zone highlights
        foreach (var zone in dropZones)
        {
            if (zone.highlightImage != null)
                zone.highlightImage.enabled = false;
        }

        Debug.Log($"ARDragDropTask [{taskId}]: Initialized with {draggableItems.Count} items and {dropZones.Count} zones.");
    }

    protected override void OnTaskHide()
    {
        // Remove handlers
        foreach (var item in draggableItems)
        {
            if (item.rectTransform != null)
            {
                var handler = item.rectTransform.GetComponent<DragHandler>();
                if (handler != null)
                    Destroy(handler);
            }
        }
    }

    protected override bool ValidateCompletion()
    {
        if (requireAllCorrect)
        {
            return correctPlacements >= draggableItems.Count;
        }
        else
        {
            return correctPlacements >= minimumCorrect;
        }
    }

    /// <summary>
    /// Called by DragHandler when item is dropped.
    /// </summary>
    public void OnItemDropped(DraggableItem item, Vector2 dropPosition)
    {
        if (!isActive || isCompleted) return;

        DropZone targetZone = FindZoneAtPosition(dropPosition);

        if (targetZone != null)
        {
            bool isCorrect = IsCorrectPlacement(item, targetZone);

            if (isCorrect)
            {
                // Snap to zone center
                item.rectTransform.anchoredPosition = targetZone.rectTransform.anchoredPosition;
                item.isPlaced = true;
                currentPlacements[item] = targetZone;
                correctPlacements++;

                // Visual feedback
                if (item.image != null)
                    item.image.color = correctPlacementColor;

                Debug.Log($"ARDragDropTask [{taskId}]: Correct placement - {item.itemId} in {targetZone.zoneId}");

                // Play success sound/feedback here if needed

                CheckCompletion();
            }
            else
            {
                // Wrong placement
                wrongAttempts++;

                // Flash wrong color
                if (item.image != null)
                {
                    StartCoroutine(FlashColor(item.image, wrongPlacementColor, 0.3f));
                }

                // Return to original position
                item.rectTransform.anchoredPosition = item.originalPosition;

                Debug.Log($"ARDragDropTask [{taskId}]: Wrong placement - {item.itemId} (attempt {wrongAttempts}/{maxWrongAttempts})");

                // Speak feedback
                if (DuringMissionStoryDirector.Instance != null)
                {
                    DuringMissionStoryDirector.Instance.SpeakLine("That's not quite right. Try again!", 2f);
                }

                if (penalizeWrongPlacements && wrongAttempts >= maxWrongAttempts)
                {
                    FailTask("Too many wrong placements");
                }
            }
        }
        else
        {
            // Dropped outside any zone - return to original
            item.rectTransform.anchoredPosition = item.originalPosition;
        }
    }

    private DropZone FindZoneAtPosition(Vector2 screenPosition)
    {
        foreach (var zone in dropZones)
        {
            if (zone.rectTransform == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(zone.rectTransform, screenPosition))
            {
                return zone;
            }
        }
        return null;
    }

    private bool IsCorrectPlacement(DraggableItem item, DropZone zone)
    {
        // Check if item's correct zone matches
        if (!string.IsNullOrEmpty(item.correctZoneId))
        {
            return string.Equals(item.correctZoneId, zone.zoneId, System.StringComparison.OrdinalIgnoreCase);
        }

        // Alternative: check zone's accepted items
        if (zone.acceptedItemIds != null)
        {
            foreach (string acceptedId in zone.acceptedItemIds)
            {
                if (string.Equals(acceptedId, item.itemId, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    public void HighlightZones(bool show)
    {
        foreach (var zone in dropZones)
        {
            if (zone.highlightImage != null)
                zone.highlightImage.enabled = show;
        }
    }

    private System.Collections.IEnumerator FlashColor(Image img, Color flashColor, float duration)
    {
        Color original = img.color;
        img.color = flashColor;
        yield return new WaitForSeconds(duration);
        img.color = original;
    }

    /// <summary>
    /// Reset all items to original positions.
    /// </summary>
    public void ResetItems()
    {
        foreach (var item in draggableItems)
        {
            if (item.rectTransform != null)
            {
                item.rectTransform.anchoredPosition = item.originalPosition;
                item.isPlaced = false;

                if (item.image != null)
                    item.image.color = Color.white;
            }
        }

        correctPlacements = 0;
        wrongAttempts = 0;
        currentPlacements.Clear();
    }
}

/// <summary>
/// Handles drag events for draggable items.
/// </summary>
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ARDragDropTask.DraggableItem item;
    private ARDragDropTask task;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Vector2 dragOffset;

    public void Initialize(ARDragDropTask.DraggableItem item, ARDragDropTask task)
    {
        this.item = item;
        this.task = task;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null || item.isPlaced) return;

        // Calculate offset from item center
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        dragOffset = item.rectTransform.anchoredPosition - localPoint;

        // Bring to front
        item.rectTransform.SetAsLastSibling();

        // Show zone highlights
        task?.HighlightZones(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (item == null || item.isPlaced) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            item.rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (item == null || item.isPlaced) return;

        // Hide zone highlights
        task?.HighlightZones(false);

        // Notify task of drop
        task?.OnItemDropped(item, eventData.position);
    }
}
