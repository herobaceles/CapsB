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

        if (feedbackText != null)
            feedbackText.text = string.Empty;

        quizPanel.SetActive(true);
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
}
