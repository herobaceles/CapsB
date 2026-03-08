using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AR task for verifying information sources and preventing misinformation.
/// Player must sort news items into "Official" vs "Rumor" categories.
/// </summary>
public class ARRumorVerificationTask : ARTaskBase
{
    [Header("Information Items")]
    [SerializeField] private List<InfoItem> informationItems = new List<InfoItem>();

    [Header("Sorting Zones")]
    [SerializeField] private RectTransform officialAnnouncementZone;
    [SerializeField] private RectTransform rumorCrowdZone;

    [Header("Visual Feedback")]
    [SerializeField] private TMP_Text panicMeterLabel;
    [SerializeField] private Slider panicMeter;
    [SerializeField] private float maxPanic = 100f;
    [SerializeField] private float panicPerWrongSort = 15f;

    [Header("Learning")]
    [TextArea(2, 4)]
    [SerializeField] private string learningMessage = "Always verify information from official sources (MDRRMO, LGU). Don't spread unverified rumors!";

    private float currentPanic;
    private int correctSorts;

    [System.Serializable]
    public class InfoItem
    {
        public string itemId;
        [TextArea(1, 2)] public string headlineText;
        public RectTransform rectTransform;
        public TMP_Text textComponent;
        public Image backgroundImage;
        public bool isOfficial;
        public string sourceTag; // e.g., "MDRRMO Update", "Facebook Post"
        [HideInInspector] public Vector2 originalPosition;
        [HideInInspector] public bool isSorted;
    }

    protected override void OnTaskShow()
    {
        currentPanic = 0f;
        correctSorts = 0;

        foreach (var item in informationItems)
        {
            if (item.rectTransform != null)
            {
                item.originalPosition = item.rectTransform.anchoredPosition;
                item.isSorted = false;

                // Set text content
                if (item.textComponent != null)
                {
                    item.textComponent.text = $"{item.headlineText}\n<size=80%><color=#888>[{item.sourceTag}]</color></size>";
                }

                // Add drag handler
                var handler = item.rectTransform.GetComponent<RumorDragHandler>();
                if (handler == null)
                {
                    handler = item.rectTransform.gameObject.AddComponent<RumorDragHandler>();
                }
                handler.Initialize(item, this);
            }
        }

        UpdatePanicDisplay();
    }

    protected override void OnTaskHide()
    {
        foreach (var item in informationItems)
        {
            if (item.rectTransform != null)
            {
                var handler = item.rectTransform.GetComponent<RumorDragHandler>();
                if (handler != null)
                    Destroy(handler);
            }
        }
    }

    protected override bool ValidateCompletion()
    {
        // All items sorted and panic not maxed
        int sorted = 0;
        foreach (var item in informationItems)
        {
            if (item.isSorted) sorted++;
        }
        return sorted >= informationItems.Count && currentPanic < maxPanic;
    }

    public void OnInfoDropped(InfoItem item, Vector2 dropPosition)
    {
        if (!isActive || isCompleted) return;

        bool? droppedInOfficial = IsInOfficialZone(dropPosition);
        bool? droppedInRumor = IsInRumorZone(dropPosition);

        if (droppedInOfficial == true || droppedInRumor == true)
        {
            bool placedInOfficial = droppedInOfficial == true;
            bool isCorrect = (placedInOfficial && item.isOfficial) || (!placedInOfficial && !item.isOfficial);

            // Snap to zone
            RectTransform targetZone = placedInOfficial ? officialAnnouncementZone : rumorCrowdZone;
            item.rectTransform.anchoredPosition = targetZone.anchoredPosition +
                new Vector2(Random.Range(-60f, 60f), Random.Range(-40f, 40f));

            item.isSorted = true;

            if (isCorrect)
            {
                correctSorts++;

                if (item.backgroundImage != null)
                    item.backgroundImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);

                Debug.Log($"ARRumorVerificationTask: {item.itemId} sorted correctly");
            }
            else
            {
                // Wrong sort - panic increases
                currentPanic += panicPerWrongSort;

                if (item.backgroundImage != null)
                    item.backgroundImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

                string feedback;
                if (placedInOfficial && !item.isOfficial)
                {
                    feedback = "That's a rumor! Spreading unverified info causes panic.";
                }
                else
                {
                    feedback = "That was actually from an official source. Don't dismiss verified updates!";
                }

                if (DuringMissionStoryDirector.Instance != null)
                {
                    DuringMissionStoryDirector.Instance.SpeakLine(feedback, 3f);
                }

                Debug.Log($"ARRumorVerificationTask: {item.itemId} sorted incorrectly");
            }

            UpdatePanicDisplay();

            // Check for failure
            if (currentPanic >= maxPanic)
            {
                FailTask("Panic level too high from spreading misinformation!");
                return;
            }

            CheckCompletion();
        }
        else
        {
            // Dropped outside zones
            item.rectTransform.anchoredPosition = item.originalPosition;
        }
    }

    private bool? IsInOfficialZone(Vector2 position)
    {
        if (officialAnnouncementZone == null) return null;
        return RectTransformUtility.RectangleContainsScreenPoint(officialAnnouncementZone, position);
    }

    private bool? IsInRumorZone(Vector2 position)
    {
        if (rumorCrowdZone == null) return null;
        return RectTransformUtility.RectangleContainsScreenPoint(rumorCrowdZone, position);
    }

    private void UpdatePanicDisplay()
    {
        if (panicMeter != null)
            panicMeter.value = currentPanic / maxPanic;

        if (panicMeterLabel != null)
            panicMeterLabel.text = $"Community Panic: {currentPanic:F0}%";
    }
}

public class RumorDragHandler : MonoBehaviour, UnityEngine.EventSystems.IBeginDragHandler,
    UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IEndDragHandler
{
    private ARRumorVerificationTask.InfoItem item;
    private ARRumorVerificationTask task;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Vector2 dragOffset;

    public void Initialize(ARRumorVerificationTask.InfoItem item, ARRumorVerificationTask task)
    {
        this.item = item;
        this.task = task;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();
    }

    public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (item == null || item.isSorted) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        dragOffset = item.rectTransform.anchoredPosition - localPoint;
        item.rectTransform.SetAsLastSibling();
    }

    public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (item == null || item.isSorted) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            item.rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (item == null || item.isSorted) return;

        task?.OnInfoDropped(item, eventData.position);
    }
}
