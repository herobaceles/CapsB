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
    [Header("Camera Focus")]
    [SerializeField] private IsometricCameraController cameraController;
    [SerializeField] private Transform focusPoint;
    [SerializeField] private float focusDistance = 16f;
    [SerializeField] private float focusAngle = 45f;

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

    [Header("Wrong Choice Feedback")]
    [TextArea(2, 4)]
    [SerializeField] private string wrongChoiceFeedbackText = "Dangerous! That alley could have hidden hazards. Try again.";
    [TextArea(2, 4)]
    [SerializeField] private string wrongChoiceVoiceLine = "Careful! Narrow alleys can hide floodwater depth and debris.";
    [SerializeField] private float wrongChoiceVoiceDuration = 3f;

    [Header("Learning Content")]
    [TextArea(2, 4)]
    [SerializeField] private string learningMessage = "Always follow official evacuation markers. Avoid shortcuts during floods!";

    [Header("Gameplay HUD To Hide")]
    [SerializeField] private GameObject[] gameplayUIRoots;

    [Header("Player Control")]
    [SerializeField] private IsometricPlayerController playerController;

    private bool choiceMade;
    private bool correctChoice;
    private Vector2 dragOffset;
    private Transform previousCameraTarget;
    private float previousCameraDistance;
    private float previousCameraAngle;
    private bool[] previousUIStates;
    private bool previousMovementEnabled;

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

        if (cameraController == null)
            cameraController = FindObjectOfType<IsometricCameraController>();

        if (playerController == null)
            playerController = FindObjectOfType<IsometricPlayerController>();

        if (string.IsNullOrWhiteSpace(wrongChoiceFeedbackText))
            wrongChoiceFeedbackText = "Dangerous! That alley could have hidden hazards. Try again.";

        if (string.IsNullOrWhiteSpace(wrongChoiceVoiceLine))
            wrongChoiceVoiceLine = "Careful! Narrow alleys can hide floodwater depth and debris.";

        if (wrongChoiceVoiceDuration <= 0f)
            wrongChoiceVoiceDuration = 3f;
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

        if (cameraController == null)
            cameraController = FindObjectOfType<IsometricCameraController>();

        if (playerController == null)
            playerController = FindObjectOfType<IsometricPlayerController>();

        base.Awake();
    }

    public override void StartTask()
    {
        // Immediately stop and lock player movement before the UI appears
        if (playerController == null)
            playerController = FindObjectOfType<IsometricPlayerController>();

        if (playerController != null)
        {
            previousMovementEnabled = playerController.IsMovementEnabled;
            playerController.SetMovementEnabled(false);
        }

        base.StartTask();
    }

    protected override void OnTaskShow()
    {
        choiceMade = false;
        correctChoice = false;

        // Focus camera on the route choice area
        if (cameraController != null && focusPoint != null)
        {
            previousCameraTarget = cameraController.Target;
            previousCameraDistance = cameraController.CurrentDistance;
            previousCameraAngle = cameraController.CurrentAngle;

            cameraController.Target = focusPoint;
            cameraController.SetDistance(focusDistance);
            cameraController.SetAngle(focusAngle);
            cameraController.SnapToTarget();
        }

        // Hide gameplay HUD while this choice is active
        if (gameplayUIRoots != null && gameplayUIRoots.Length > 0)
        {
            if (previousUIStates == null || previousUIStates.Length != gameplayUIRoots.Length)
                previousUIStates = new bool[gameplayUIRoots.Length];

            for (int i = 0; i < gameplayUIRoots.Length; i++)
            {
                var root = gameplayUIRoots[i];
                if (root == null) continue;

                previousUIStates[i] = root.activeSelf;
                root.SetActive(false);
            }
        }

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

        // Restore camera to previous follow target
        if (cameraController != null && previousCameraTarget != null)
        {
            cameraController.Target = previousCameraTarget;
            cameraController.SetDistance(previousCameraDistance);
            cameraController.SetAngle(previousCameraAngle);
            cameraController.SnapToTarget();
        }

        // Restore gameplay HUD visibility
        if (gameplayUIRoots != null && gameplayUIRoots.Length > 0)
        {
            for (int i = 0; i < gameplayUIRoots.Length; i++)
            {
                var root = gameplayUIRoots[i];
                if (root == null) continue;

                bool wasActive = previousUIStates != null && i < previousUIStates.Length
                    ? previousUIStates[i]
                    : true;

                root.SetActive(wasActive);
            }
        }

        // Re-enable player movement if it was previously enabled
        if (playerController != null)
        {
            playerController.SetMovementEnabled(previousMovementEnabled);
        }
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
            var message = string.IsNullOrWhiteSpace(wrongChoiceFeedbackText)
                ? "Dangerous! That alley could have hidden hazards. Try again."
                : wrongChoiceFeedbackText;

            feedbackText.text = message;
            feedbackText.color = wrongColor;
        }

        Debug.Log("ARRouteChoiceTask: Risky route selected (wrong)");

        // Speak warning
        if (DuringMissionStoryDirector.Instance != null)
        {
            var line = string.IsNullOrWhiteSpace(wrongChoiceVoiceLine)
                ? "Careful! Narrow alleys can hide floodwater depth and debris."
                : wrongChoiceVoiceLine;

            var duration = wrongChoiceVoiceDuration > 0f ? wrongChoiceVoiceDuration : 3f;

            DuringMissionStoryDirector.Instance.SpeakLine(line, duration);
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

