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

    [Header("Route Gating")]
    [SerializeField] private Button chooseNowButton;
    [SerializeField] private GameObject routeButtonsRoot;

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

    [Header("Secondary Camera Views (Optional)")]
    [SerializeField] private Camera safeRouteCamera;
    [SerializeField] private Camera unsafeRouteCamera;
    [SerializeField] private GameObject secondaryCameraOverlay;

    private bool choiceMade;
    private bool correctChoice;
    private Vector2 dragOffset;
    private bool[] previousUIStates;
    private bool previousMovementEnabled;
    private bool hasShownChoicePrompt;
    private Coroutine gateRoutine;
    private Coroutine wrongChoiceRoutine;

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

        if (playerController == null)
            playerController = FindObjectOfType<IsometricPlayerController>();

        // Ensure route choices and the gate button start hidden so they
        // don't briefly appear when the task canvas is first shown.
        if (routeButtonsRoot != null)
        {
            routeButtonsRoot.SetActive(false);
        }
        else
        {
            if (safeRouteButton != null)
                safeRouteButton.gameObject.SetActive(false);

            if (riskyRouteButton != null)
                riskyRouteButton.gameObject.SetActive(false);
        }

        if (chooseNowButton != null)
            chooseNowButton.gameObject.SetActive(false);

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
        hasShownChoicePrompt = false;

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

        // Initially hide route choices and the "Choose Now" gate
        if (routeButtonsRoot != null)
        {
            routeButtonsRoot.SetActive(false);
        }
        else
        {
            if (safeRouteButton != null)
                safeRouteButton.gameObject.SetActive(false);

            if (riskyRouteButton != null)
                riskyRouteButton.gameObject.SetActive(false);
        }

        if (chooseNowButton != null)
        {
            chooseNowButton.gameObject.SetActive(false);
            chooseNowButton.onClick.AddListener(OnChooseNowClicked);
        }

        // Wait for any active NPC bubble dialogue to finish, then
        // show the gate button. If no dialogue is playing, show it
        // immediately.
        if (gateRoutine != null)
            StopCoroutine(gateRoutine);

        gateRoutine = StartCoroutine(WaitForDialogueThenShowGate());
    }

    protected override void OnTaskHide()
    {
        DisableRouteCameras();

        if (chooseNowButton != null)
        {
            chooseNowButton.onClick.RemoveListener(OnChooseNowClicked);
            chooseNowButton.gameObject.SetActive(false);
        }

        if (safeRouteButton != null)
            safeRouteButton.onClick.RemoveListener(OnSafeRouteSelected);

        if (riskyRouteButton != null)
            riskyRouteButton.onClick.RemoveListener(OnRiskyRouteSelected);

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

        if (gateRoutine != null)
        {
            StopCoroutine(gateRoutine);
            gateRoutine = null;
        }

        if (wrongChoiceRoutine != null)
        {
            StopCoroutine(wrongChoiceRoutine);
            wrongChoiceRoutine = null;
        }
    }

    private void OnStartDialogueFinishedForRouteChoice()
    {
        // This method is no longer needed as we handle showing the button in the coroutine
    }

    private System.Collections.IEnumerator WaitForDialogueThenShowGate()
    {
        var director = DuringMissionStoryDirector.Instance;

        if (director != null)
        {
            // If dialogue is currently playing, wait for it to stop.
            while (director.IsPlaying)
            {
                yield return null;
            }
        }
        ShowChooseNowButton();

        gateRoutine = null;
    }

    private void ShowChooseNowButton()
    {
        if (hasShownChoicePrompt)
            return;

        hasShownChoicePrompt = true;

        if (chooseNowButton != null)
        {
            chooseNowButton.gameObject.SetActive(true);
        }
        else
        {
            // Fallback: if no gate button is wired, just
            // reveal the route choices immediately.
            ShowRouteChoices();
        }
    }

    private void ShowRouteChoices()
    {
        EnableRouteCameras();

        if (routeButtonsRoot != null)
        {
            routeButtonsRoot.SetActive(true);
        }
        else
        {
            if (safeRouteButton != null)
                safeRouteButton.gameObject.SetActive(true);

            if (riskyRouteButton != null)
                riskyRouteButton.gameObject.SetActive(true);
        }
    }

    private void EnableRouteCameras()
    {
        // Enable secondary route cameras if configured
        if (safeRouteCamera != null)
        {
            safeRouteCamera.gameObject.SetActive(true);
            safeRouteCamera.enabled = true;
        }

        if (unsafeRouteCamera != null)
        {
            unsafeRouteCamera.gameObject.SetActive(true);
            unsafeRouteCamera.enabled = true;
        }

        if (secondaryCameraOverlay != null)
            secondaryCameraOverlay.SetActive(true);
    }

    private void OnChooseNowClicked()
    {
        if (chooseNowButton != null)
            chooseNowButton.gameObject.SetActive(false);

        ShowRouteChoices();
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

        // Hide route choices and cameras so the NPC's
        // explanation dialogue is unobstructed.
        HideRouteChoices();
        DisableRouteCameras();

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

        // Temporarily hide route choices and cameras so the
        // dialogue bubble explaining the mistake is unobstructed.
        HideRouteChoices();
        DisableRouteCameras();

        // Reset only after all wrong-choice dialogue has finished playing
        if (wrongChoiceRoutine != null)
        {
            StopCoroutine(wrongChoiceRoutine);
            wrongChoiceRoutine = null;
        }

        wrongChoiceRoutine = StartCoroutine(WaitForWrongDialogueThenReset());
    }

    private System.Collections.IEnumerator WaitForWrongDialogueThenReset()
    {
        var director = DuringMissionStoryDirector.Instance;

        if (director != null)
        {
            // Wait until all queued NPC dialogue (including our wrong-choice line)
            // has finished playing.
            while (director.IsPlaying)
            {
                yield return null;
            }
        }
        else
        {
            // Fallback: if no story director is present, wait roughly for the
            // configured wrong-choice voice duration so the text has time to read.
            var duration = wrongChoiceVoiceDuration > 0f ? wrongChoiceVoiceDuration : 2f;
            yield return new WaitForSeconds(duration);
        }

        ResetChoice();
        wrongChoiceRoutine = null;
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

        // Restore route choices and cameras so the player can
        // make another selection after seeing the dialogue.
        ShowRouteChoices();
    }

    private void HideRouteChoices()
    {
        if (routeButtonsRoot != null)
        {
            routeButtonsRoot.SetActive(false);
        }
        else
        {
            if (safeRouteButton != null)
                safeRouteButton.gameObject.SetActive(false);

            if (riskyRouteButton != null)
                riskyRouteButton.gameObject.SetActive(false);
        }
    }

    private void DisableRouteCameras()
    {
        // Disable secondary route cameras if configured
        if (safeRouteCamera != null)
        {
            safeRouteCamera.enabled = false;
            safeRouteCamera.gameObject.SetActive(false);
        }

        if (unsafeRouteCamera != null)
        {
            unsafeRouteCamera.enabled = false;
            unsafeRouteCamera.gameObject.SetActive(false);
        }

        if (secondaryCameraOverlay != null)
            secondaryCameraOverlay.SetActive(false);
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

