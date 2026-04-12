using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dedicated UI controller for mission start quiz prompts.
/// Uses a separate panel from the regular dialogue UI.
/// </summary>
public class QuizDialogueUIManager : MonoBehaviour
{
    [Header("Quiz UI")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Image questionPortraitImage;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button optionButton1;
    [SerializeField] private Button optionButton2;
    [SerializeField] private Button optionButton3;
    [SerializeField] private Image optionButton1Image;
    [SerializeField] private Image optionButton2Image;
    [SerializeField] private Image optionButton3Image;
    [SerializeField] private TMP_Text optionButton1Text;
    [SerializeField] private TMP_Text optionButton2Text;
    [SerializeField] private TMP_Text optionButton3Text;

    [Header("Controls")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonLabel;

    [Header("Messages")]
    [SerializeField] private string wrongAnswerMessage = "Incorrect answer. Try again.";

    [Header("Typing Settings")]
    [SerializeField] private float questionTypingSpeed = 0.03f;

    [Header("Gameplay HUD")]
    [SerializeField] private GameObject[] gameplayUIRootsToHide;

    private int correctOptionIndex;
    private UnityAction onCorrectAnswer;

    private Coroutine typingCoroutine;
    private Coroutine feedbackCoroutine;
    private string currentQuestionText;
    private string[] currentWrongOptionFeedback;
    private bool isShowingFeedback;
    private PortraitFaceAnimator currentFaceAnimator;

    // Tracks which gameplay HUD roots were active before the quiz showed.
    private bool[] previousUIStates;

    // State for waiting on the Continue button after wrong answers
    private bool isWaitingForContinue;

    private void Awake()
    {
        HideQuiz();

        // Ensure continue button starts hidden and wired
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        if (continueButtonLabel != null && string.IsNullOrEmpty(continueButtonLabel.text))
        {
            continueButtonLabel.text = "Continue";
        }
    }

    public bool IsConfigured()
    {
        return quizPanel != null
            && questionText != null
            && optionButton1 != null
            && optionButton2 != null
            && optionButton3 != null;
    }

    public void ShowQuiz(MissionQuizData quizData, UnityAction onCorrect)
    {
        if (quizData == null)
        {
            Debug.LogWarning("QuizDialogueUIManager: Quiz data is null.");
            onCorrect?.Invoke();
            return;
        }

        if (!IsConfigured())
        {
            Debug.LogWarning("QuizDialogueUIManager: Missing UI references. Skipping quiz.");
            onCorrect?.Invoke();
            return;
        }


        if (quizData.options == null || quizData.options.Length < 3)
        {
            Debug.LogWarning("QuizDialogueUIManager: Quiz options are not valid. Skipping quiz.");
            onCorrect?.Invoke();
            return;
        }

        correctOptionIndex = quizData.correctOptionIndex;
        onCorrectAnswer = onCorrect;

        currentQuestionText = quizData.question;
        currentWrongOptionFeedback = quizData.wrongOptionFeedback;
        isShowingFeedback = false;

        // Stop any previous typing/talking state before configuring new quiz.
        StopTypingAndTalking();

    // Hide gameplay HUD (pause/menu, task panels, etc.) while the quiz is active.
    HideGameplayHUD();

        SetOption(optionButton1, optionButton1Text, optionButton1Image, quizData.options[0], GetOptionSprite(quizData, 0), 0, quizData.placeholderSprite);
        SetOption(optionButton2, optionButton2Text, optionButton2Image, quizData.options[1], GetOptionSprite(quizData, 1), 1, quizData.placeholderSprite);
        SetOption(optionButton3, optionButton3Text, optionButton3Image, quizData.options[2], GetOptionSprite(quizData, 2), 2, quizData.placeholderSprite);

        // Ensure the quiz panel (and its children, including the question portrait)
        // are active before configuring any facial animation that starts coroutines.
        quizPanel.SetActive(true);

        ConfigureQuestionPortrait(quizData);

        // Start typing the question text; talking animation is driven
        // by the typing coroutine instead of being permanently on.
        if (questionText != null)
        {
            PlayLine(currentQuestionText);
        }

        if (feedbackText != null)
            feedbackText.text = string.Empty;
    }

    public void HideQuiz()
    {
        StopTypingAndTalking();

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        isShowingFeedback = false;

    // Restore gameplay HUD visibility when the quiz is dismissed.
    RestoreGameplayHUD();

        // Hide and reset continue button state when quiz is hidden
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        isWaitingForContinue = false;

        if (quizPanel != null)
            quizPanel.SetActive(false);

        if (feedbackText != null)
            feedbackText.text = string.Empty;
    }

    private void SetOption(Button button, TMP_Text label, Image image, string text, Sprite sprite, int index, Sprite placeholder)
    {
        if (button == null)
            return;

        if (label != null)
            label.text = text;

        if (image != null)
        {
            var resolvedSprite = sprite != null ? sprite : placeholder;
            image.sprite = resolvedSprite;
            image.enabled = resolvedSprite != null;
            image.preserveAspect = true;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnOptionSelected(index));
    }

    private Sprite GetOptionSprite(MissionQuizData data, int index)
    {
        if (data == null || data.optionSprites == null)
            return null;

        if (index < 0 || index >= data.optionSprites.Length)
            return null;

        return data.optionSprites[index];
    }

    private void OnOptionSelected(int selectedIndex)
    {
        if (selectedIndex == correctOptionIndex)
        {
            StopTypingAndTalking();
            HideQuiz();
            onCorrectAnswer?.Invoke();
            return;
        }

        if (isShowingFeedback)
            return;

        isShowingFeedback = true;

        if (feedbackText != null)
            feedbackText.text = string.Empty;

        SetOptionsInteractable(false);

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = null;
        }

        string feedbackMessage = GetFeedbackForOption(selectedIndex);
        feedbackCoroutine = StartCoroutine(HandleWrongAnswerFeedback(feedbackMessage));
    }

    private void ConfigureQuestionPortrait(MissionQuizData quizData)
    {
        if (questionPortraitImage == null || quizData == null)
            return;

        Sprite portraitSprite = null;

        // Direct override from the quiz data takes precedence.
        if (quizData.questionPortraitOverride != null)
        {
            portraitSprite = quizData.questionPortraitOverride;
        }
        else if (!string.IsNullOrEmpty(quizData.questionCharacterId) && ProdDialogueManager.Instance != null)
        {
            portraitSprite = ProdDialogueManager.Instance.GetPortraitForCharacter(
                quizData.questionCharacterId,
                quizData.questionExpressionId
            );
        }

        if (portraitSprite != null)
        {
            questionPortraitImage.sprite = portraitSprite;
            questionPortraitImage.enabled = true;
            questionPortraitImage.preserveAspect = true;

            ConfigureQuestionPortraitAnimator(quizData, portraitSprite);
        }
        else
        {
            questionPortraitImage.enabled = false;
            // Ensure any existing animator is not left in a talking state
            var existingAnimator = questionPortraitImage.GetComponent<PortraitFaceAnimator>();
            if (existingAnimator != null)
                existingAnimator.SetTalking(false);

            currentFaceAnimator = existingAnimator;
        }
    }

    /// <summary>
    /// Optionally configures a PortraitFaceAnimator on the question portrait so it
    /// can reuse the same expression sprites (idle, blink, talking) as dialogue portraits.
    /// </summary>
    private void ConfigureQuestionPortraitAnimator(MissionQuizData quizData, Sprite baseSprite)
    {
        if (questionPortraitImage == null || quizData == null)
            return;

        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager == null)
            return;

        var faceAnimator = questionPortraitImage.GetComponent<PortraitFaceAnimator>();
        if (faceAnimator == null)
            faceAnimator = questionPortraitImage.gameObject.AddComponent<PortraitFaceAnimator>();

        if (faceAnimator == null)
            return;

        Sprite idle = baseSprite;
        Sprite blink = null;
        Sprite[] talkingFrames = null;

        // If a character id is provided, try to pull the full expression data
        // (idle, blink, talking frames) from the character presets.
        if (!string.IsNullOrEmpty(quizData.questionCharacterId))
        {
            dialogueManager.GetExpressionSpritesForCharacter(
                quizData.questionCharacterId,
                quizData.questionExpressionId,
                out var exprIdle,
                out var exprBlink,
                out var exprTalking
            );

            if (exprIdle != null)
                idle = exprIdle;

            blink = exprBlink;
            talkingFrames = exprTalking;
        }

        faceAnimator.SetExpressionSprites(idle, blink, talkingFrames);

        // Remember the animator so the typing coroutine can control when
        // the character is talking.
        currentFaceAnimator = faceAnimator;
    }

    private void PlayLine(string text)
    {
        StopTypingAndTalking();

        if (questionText == null)
            return;

        typingCoroutine = StartCoroutine(TypeQuestionText(text));
    }

    private IEnumerator TypeQuestionText(string fullText)
    {
        if (questionText == null)
            yield break;

        if (string.IsNullOrEmpty(fullText))
        {
            questionText.text = string.Empty;
            if (currentFaceAnimator != null)
                currentFaceAnimator.SetTalking(false);
            yield break;
        }

        questionText.text = string.Empty;

        if (currentFaceAnimator != null)
            currentFaceAnimator.SetTalking(true);

        foreach (char c in fullText)
        {
            questionText.text += c;
            yield return new WaitForSeconds(questionTypingSpeed);
        }

        if (currentFaceAnimator != null)
            currentFaceAnimator.SetTalking(false);

        typingCoroutine = null;
    }

    private void StopTypingAndTalking()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (currentFaceAnimator != null)
        {
            currentFaceAnimator.SetTalking(false);
        }
    }

    private void SetOptionsInteractable(bool interactable)
    {
        if (optionButton1 != null)
            optionButton1.interactable = interactable;
        if (optionButton2 != null)
            optionButton2.interactable = interactable;
        if (optionButton3 != null)
            optionButton3.interactable = interactable;
    }

    private string GetFeedbackForOption(int selectedIndex)
    {
        if (currentWrongOptionFeedback != null &&
            selectedIndex >= 0 &&
            selectedIndex < currentWrongOptionFeedback.Length)
        {
            string specific = currentWrongOptionFeedback[selectedIndex];
            if (!string.IsNullOrEmpty(specific))
                return specific;
        }

        return wrongAnswerMessage;
    }

    /// <summary>
    /// Temporarily hides configured gameplay HUD roots (pause button, task panels, etc.)
    /// while the quiz UI is active, remembering their previous active state.
    /// </summary>
    private void HideGameplayHUD()
    {
        if (gameplayUIRootsToHide == null || gameplayUIRootsToHide.Length == 0)
            return;

        if (previousUIStates == null || previousUIStates.Length != gameplayUIRootsToHide.Length)
            previousUIStates = new bool[gameplayUIRootsToHide.Length];

        for (int i = 0; i < gameplayUIRootsToHide.Length; i++)
        {
            GameObject root = gameplayUIRootsToHide[i];
            if (root == null)
                continue;

            previousUIStates[i] = root.activeSelf;
            root.SetActive(false);
        }
    }

    /// <summary>
    /// Restores gameplay HUD roots to the state they had before the quiz was shown.
    /// </summary>
    private void RestoreGameplayHUD()
    {
        if (gameplayUIRootsToHide == null || gameplayUIRootsToHide.Length == 0)
            return;

        for (int i = 0; i < gameplayUIRootsToHide.Length; i++)
        {
            GameObject root = gameplayUIRootsToHide[i];
            if (root == null)
                continue;

            bool wasActive = true;
            if (previousUIStates != null && i < previousUIStates.Length)
                wasActive = previousUIStates[i];

            root.SetActive(wasActive);
        }
    }

    private IEnumerator HandleWrongAnswerFeedback(string feedbackMessage)
    {
        // Show feedback line in place of the question
        PlayLine(feedbackMessage);

        // Wait for feedback typing to finish
        while (typingCoroutine != null)
        {
            yield return null;
        }

        // After feedback is fully shown, wait for the player to
        // explicitly press Continue before re-asking the question.
        if (continueButton != null)
        {
            isWaitingForContinue = true;
            continueButton.gameObject.SetActive(true);

            // Wait until the continue button is pressed
            while (isWaitingForContinue)
            {
                yield return null;
            }

            // Hide the button again before re-asking
            continueButton.gameObject.SetActive(false);
        }

        // Re-show the original question once the player is ready
        PlayLine(currentQuestionText);

        // Wait for question typing to finish
        while (typingCoroutine != null)
        {
            yield return null;
        }

        SetOptionsInteractable(true);

        isShowingFeedback = false;
        feedbackCoroutine = null;
    }

    private void OnContinueButtonClicked()
    {
        isWaitingForContinue = false;
    }
}
