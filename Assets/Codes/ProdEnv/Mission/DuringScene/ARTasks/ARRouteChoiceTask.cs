using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// AR task for choosing between safe/risky routes.
/// Player must select evacuation markers on the correct path.
/// </summary>
public class ARRouteChoiceTask : ARTaskBase, IBeginDragHandler, IDragHandler
{
    [Header("Drag Settings")]
    [SerializeField] private RectTransform draggablePanel;
    [SerializeField] private Canvas parentCanvas;

    [Header("Route Options")]
    [SerializeField] private Button safeRouteButton;
    [SerializeField] private Button riskyRouteButton;
    [SerializeField] private Image safeRouteHighlight;
    [SerializeField] private Image riskyRouteHighlight;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;

    [Header("Learning Content")]
    [TextArea(2, 4)]
    [SerializeField] private string learningMessage = "Always follow official evacuation markers. Avoid shortcuts during floods!";

    private bool choiceMade;
    private bool correctChoice;
    private Vector2 dragOffset;

    private void OnValidate()
    {
        // Keep the task canvas pointing at this panel
        if (draggablePanel == null)
            draggablePanel = GetComponent<RectTransform>();

        if (taskCanvas == null && draggablePanel != null)
            taskCanvas = draggablePanel.gameObject;

        if (canvasGroup == null && taskCanvas != null)
            canvasGroup = taskCanvas.GetComponent<CanvasGroup>();

        if (parentCanvas == null && draggablePanel != null)
            parentCanvas = draggablePanel.GetComponentInParent<Canvas>();
    }

    protected override void Awake()
    {
        // Default the task canvas/canvas group to this panel so no extra wrapper is needed
        if (draggablePanel == null)
            draggablePanel = GetComponent<RectTransform>();

        if (taskCanvas == null)
            taskCanvas = draggablePanel != null ? draggablePanel.gameObject : gameObject;

        if (canvasGroup == null && taskCanvas != null)
            canvasGroup = taskCanvas.GetComponent<CanvasGroup>();

        if (parentCanvas == null && draggablePanel != null)
            parentCanvas = draggablePanel.GetComponentInParent<Canvas>();

        base.Awake();
    }

    protected override void OnTaskShow()
    {
        choiceMade = false;
        correctChoice = false;

        if (safeRouteButton != null)
            safeRouteButton.onClick.AddListener(OnSafeRouteSelected);

        if (riskyRouteButton != null)
            riskyRouteButton.onClick.AddListener(OnRiskyRouteSelected);

        // Reset highlights
        if (safeRouteHighlight != null)
            safeRouteHighlight.enabled = false;

        if (riskyRouteHighlight != null)
            riskyRouteHighlight.enabled = false;

        if (feedbackText != null)
            feedbackText.text = instructions;
    }

    protected override void OnTaskHide()
    {
        if (safeRouteButton != null)
            safeRouteButton.onClick.RemoveListener(OnSafeRouteSelected);

        if (riskyRouteButton != null)
            riskyRouteButton.onClick.RemoveListener(OnRiskyRouteSelected);
    }

    protected override bool ValidateCompletion()
    {
        return choiceMade && correctChoice;
    }

    private void OnSafeRouteSelected()
    {
        if (choiceMade) return;

        choiceMade = true;
        correctChoice = true;

        // Visual feedback
        if (safeRouteHighlight != null)
        {
            safeRouteHighlight.enabled = true;
            safeRouteHighlight.color = correctColor;
        }

        if (feedbackText != null)
        {
            feedbackText.text = "Correct! " + learningMessage;
            feedbackText.color = correctColor;
        }

        Debug.Log("ARRouteChoiceTask: Safe route selected (correct)");

        // Complete after showing feedback
        Invoke(nameof(DelayedComplete), 2f);
    }

    private void OnRiskyRouteSelected()
    {
        if (choiceMade) return;

        // Show consequence
        if (riskyRouteHighlight != null)
        {
            riskyRouteHighlight.enabled = true;
            riskyRouteHighlight.color = wrongColor;
        }

        if (feedbackText != null)
        {
            feedbackText.text = "Dangerous! That alley could have hidden hazards. Try again.";
            feedbackText.color = wrongColor;
        }

        Debug.Log("ARRouteChoiceTask: Risky route selected (wrong)");

        // Speak warning
        if (DuringMissionStoryDirector.Instance != null)
        {
            DuringMissionStoryDirector.Instance.SpeakLine("Careful! Narrow alleys can hide floodwater depth and debris.", 3f);
        }

        // Reset after delay
        Invoke(nameof(ResetChoice), 2f);
    }

    private void ResetChoice()
    {
        if (riskyRouteHighlight != null)
            riskyRouteHighlight.enabled = false;

        if (feedbackText != null)
        {
            feedbackText.text = instructions;
            feedbackText.color = Color.white;
        }
    }

    private void DelayedComplete()
    {
        CheckCompletion();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (draggablePanel == null || parentCanvas == null) return;

        // Cache the offset between the pointer and the panel's anchored position to keep drag stable
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            draggablePanel.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint))
        {
            dragOffset = draggablePanel.anchoredPosition - localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggablePanel == null || parentCanvas == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            draggablePanel.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint))
        {
            draggablePanel.anchoredPosition = localPoint + dragOffset;
        }
    }
}
