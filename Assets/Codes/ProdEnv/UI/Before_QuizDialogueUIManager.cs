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

    [Header("Messages")]
    [SerializeField] private string wrongAnswerMessage = "Incorrect answer. Try again.";

    private int correctOptionIndex;
    private UnityAction onCorrectAnswer;

    private void Awake()
    {
        HideQuiz();
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

        questionText.text = quizData.question;
        SetOption(optionButton1, optionButton1Text, optionButton1Image, quizData.options[0], GetOptionSprite(quizData, 0), 0, quizData.placeholderSprite);
        SetOption(optionButton2, optionButton2Text, optionButton2Image, quizData.options[1], GetOptionSprite(quizData, 1), 1, quizData.placeholderSprite);
        SetOption(optionButton3, optionButton3Text, optionButton3Image, quizData.options[2], GetOptionSprite(quizData, 2), 2, quizData.placeholderSprite);

        // Ensure the quiz panel (and its children, including the question portrait)
        // are active before configuring any facial animation that starts coroutines.
        quizPanel.SetActive(true);

        ConfigureQuestionPortrait(quizData);

        if (feedbackText != null)
            feedbackText.text = string.Empty;
    }

    public void HideQuiz()
    {
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
            HideQuiz();
            onCorrectAnswer?.Invoke();
            return;
        }

        if (feedbackText != null)
            feedbackText.text = wrongAnswerMessage;
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

        // Treat the question as the character "talking" while the quiz is visible
        // when we have talking frames available.
        if (talkingFrames != null && talkingFrames.Length > 0)
        {
            faceAnimator.SetTalking(true);
        }
        else
        {
            faceAnimator.SetTalking(false);
        }
    }
}
