using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AR task for managing go-bag supplies at the evacuation center.
/// Player must allocate items to "Use Now" or "Save for Later" categories.
/// </summary>
public class ARSupplyAllocationTask : ARTaskBase
{
    [Header("Supply Items")]
    [SerializeField] private List<SupplyItem> supplyItems = new List<SupplyItem>();

    [Header("Allocation Zones")]
    [SerializeField] private RectTransform useNowZone;
    [SerializeField] private RectTransform saveForLaterZone;
    [SerializeField] private Image useNowHighlight;
    [SerializeField] private Image saveHighlight;

    [Header("Resource Display")]
    [SerializeField] private Slider resourceMeter;
    [SerializeField] private TMP_Text resourceText;
    [SerializeField] private float startingResources = 100f;

    [Header("Feedback")]
    [SerializeField] private TMP_Text statusText;

    [Header("Learning")]
    [TextArea(2, 4)]
    [SerializeField] private string learningMessage = "Conserve supplies during emergencies. Only use what you need immediately!";

    private float currentResources;
    private int correctAllocations;
    private Dictionary<SupplyItem, bool> allocatedItems = new Dictionary<SupplyItem, bool>();

    [System.Serializable]
    public class SupplyItem
    {
        public string itemId;
        public string itemName;
        public RectTransform rectTransform;
        public Image icon;
        public AllocationCategory correctCategory;
        public float resourceCost = 10f;
        [HideInInspector] public Vector2 originalPosition;
        [HideInInspector] public bool isAllocated;
    }

    public enum AllocationCategory
    {
        UseNow,
        SaveForLater
    }

    protected override void OnTaskShow()
    {
        currentResources = startingResources;
        correctAllocations = 0;
        allocatedItems.Clear();

        foreach (var item in supplyItems)
        {
            if (item.rectTransform != null)
            {
                item.originalPosition = item.rectTransform.anchoredPosition;
                item.isAllocated = false;
                allocatedItems[item] = false;

                var handler = item.rectTransform.GetComponent<SupplyDragHandler>();
                if (handler == null)
                {
                    handler = item.rectTransform.gameObject.AddComponent<SupplyDragHandler>();
                }
                handler.Initialize(item, this);
            }
        }

        UpdateResourceDisplay();
        UpdateStatus();
    }

    protected override void OnTaskHide()
    {
        foreach (var item in supplyItems)
        {
            if (item.rectTransform != null)
            {
                var handler = item.rectTransform.GetComponent<SupplyDragHandler>();
                if (handler != null)
                    Destroy(handler);
            }
        }
    }

    protected override bool ValidateCompletion()
    {
        // Need all items allocated
        int allocated = 0;
        foreach (var item in supplyItems)
        {
            if (item.isAllocated) allocated++;
        }
        return allocated >= supplyItems.Count;
    }

    public void OnSupplyDropped(SupplyItem item, Vector2 dropPosition)
    {
        if (!isActive || isCompleted) return;

        AllocationCategory? droppedCategory = GetDropCategory(dropPosition);

        if (droppedCategory.HasValue)
        {
            bool isCorrect = droppedCategory.Value == item.correctCategory;

            // Snap to zone
            RectTransform targetZone = droppedCategory.Value == AllocationCategory.UseNow 
                ? useNowZone 
                : saveForLaterZone;

            item.rectTransform.anchoredPosition = targetZone.anchoredPosition + 
                new Vector2(Random.Range(-50f, 50f), Random.Range(-30f, 30f));

            item.isAllocated = true;
            allocatedItems[item] = isCorrect;

            if (isCorrect)
            {
                correctAllocations++;

                if (item.icon != null)
                    item.icon.color = Color.green;

                Debug.Log($"ARSupplyAllocationTask: {item.itemName} allocated correctly to {droppedCategory.Value}");
            }
            else
            {
                // Wrong allocation - consume extra resources
                currentResources -= item.resourceCost * 0.5f;

                if (item.icon != null)
                    item.icon.color = Color.yellow;

                if (DuringMissionStoryDirector.Instance != null)
                {
                    string msg = droppedCategory.Value == AllocationCategory.UseNow
                        ? $"We might need that {item.itemName} later. Save some for emergencies."
                        : $"We should use the {item.itemName} now while it's needed.";

                    DuringMissionStoryDirector.Instance.SpeakLine(msg, 2.5f);
                }

                Debug.Log($"ARSupplyAllocationTask: {item.itemName} allocated incorrectly");
            }

            UpdateResourceDisplay();
            UpdateStatus();
            CheckCompletion();
        }
        else
        {
            // Dropped outside zones - return
            item.rectTransform.anchoredPosition = item.originalPosition;
        }
    }

    private AllocationCategory? GetDropCategory(Vector2 position)
    {
        if (useNowZone != null && RectTransformUtility.RectangleContainsScreenPoint(useNowZone, position))
            return AllocationCategory.UseNow;

        if (saveForLaterZone != null && RectTransformUtility.RectangleContainsScreenPoint(saveForLaterZone, position))
            return AllocationCategory.SaveForLater;

        return null;
    }

    private void UpdateResourceDisplay()
    {
        if (resourceMeter != null)
            resourceMeter.value = currentResources / startingResources;

        if (resourceText != null)
            resourceText.text = $"Resources: {currentResources:F0}/{startingResources:F0}";
    }

    private void UpdateStatus()
    {
        if (statusText == null) return;

        int allocated = 0;
        foreach (var item in supplyItems)
        {
            if (item.isAllocated) allocated++;
        }

        statusText.text = $"Items allocated: {allocated}/{supplyItems.Count}";
    }

    public void ShowZoneHighlights(bool show)
    {
        if (useNowHighlight != null)
            useNowHighlight.enabled = show;

        if (saveHighlight != null)
            saveHighlight.enabled = show;
    }
}

public class SupplyDragHandler : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler,
    UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IEndDragHandler
{
    private ARSupplyAllocationTask.SupplyItem item;
    private ARSupplyAllocationTask task;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Vector2 dragOffset;

    public void Initialize(ARSupplyAllocationTask.SupplyItem item, ARSupplyAllocationTask task)
    {
        this.item = item;
        this.task = task;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (item == null || item.isAllocated) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        dragOffset = item.rectTransform.anchoredPosition - localPoint;
        item.rectTransform.SetAsLastSibling();

        task?.ShowZoneHighlights(true);
    }

    public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (item == null || item.isAllocated) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            item.rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (item == null || item.isAllocated) return;

        task?.ShowZoneHighlights(false);
        task?.OnSupplyDropped(item, eventData.position);
    }
}
