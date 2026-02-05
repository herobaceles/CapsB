using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the Evacuation quiz UI panel when player goes outside.
/// </summary>
public class EvacuationQuizUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button confirmButton;

    [Header("Answer Buttons")]
    [SerializeField] private Button answerButtonA;
    [SerializeField] private Button answerButtonB;
    [SerializeField] private Button answerButtonC;

    [Header("Answer Button Texts")]
    [SerializeField] private TMP_Text answerTextA;
    [SerializeField] private TMP_Text answerTextB;
    [SerializeField] private TMP_Text answerTextC;

    [Header("Answer Images (for highlighting)")]
    [SerializeField] private Image answerImageA;
    [SerializeField] private Image answerImageB;
    [SerializeField] private Image answerImageC;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.8f, 0.3f, 1f); // Green
    [SerializeField] private Color incorrectColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red

    [Header("Question Data")]
    [SerializeField] private string question = "You have your family with you. But which path should you take to the evacuation center?";
    [SerializeField] private string answerA = "A. Take the shortcut through the alley (best bet)";
    [SerializeField] private string answerB = "B. Walk down the main road. (Open, but water is flowing)";
    [SerializeField] private string answerC = "C. Stay and wait for the water to recede.";
    [SerializeField] private int correctAnswerIndex = 0; // 0 = A

    [Header("Feedback Messages")]
    [SerializeField] private string correctFeedback = "Good! The main road is the designated evacuation route. It's safer and narrower alley where you could get trapped. Let's go!";
    [SerializeField] private string incorrectFeedbackA = "No, shortcuts through alleys can be dangerous. Stick to the designated evacuation routes.";
    [SerializeField] private string incorrectFeedbackC = "No, waiting is too risky. The water level could rise quickly. You need to evacuate now!";

    [Header("Feedback UI")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDisplayDuration = 3f;

    private int selectedAnswer = -1;
    private bool hasAnswered = false;

    private void OnEnable()
    {
        // Setup button listeners when panel becomes active
        if (answerButtonA != null)
            answerButtonA.onClick.AddListener(() => SelectAnswer(0));
        if (answerButtonB != null)
            answerButtonB.onClick.AddListener(() => SelectAnswer(1));
        if (answerButtonC != null)
            answerButtonC.onClick.AddListener(() => SelectAnswer(2));

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false;
        }

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        Debug.Log("[EvacuationQuizUI] Panel enabled and listeners set up");
    }

    private void OnDisable()
    {
        // Clean up listeners when panel is disabled
        if (answerButtonA != null)
            answerButtonA.onClick.RemoveAllListeners();
        if (answerButtonB != null)
            answerButtonB.onClick.RemoveAllListeners();
        if (answerButtonC != null)
            answerButtonC.onClick.RemoveAllListeners();
        if (confirmButton != null)
            confirmButton.onClick.RemoveAllListeners();
    }

    public void ShowQuiz()
    {
        Debug.Log("[EvacuationQuizUI] ShowQuiz called");
        Debug.Log($"[EvacuationQuizUI] panelRoot assigned: {panelRoot != null}");

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);

            // Check if Canvas is active
            Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[EvacuationQuizUI] Canvas: {canvas.gameObject.name} - Active: {canvas.gameObject.activeSelf}");

                // Force enable Canvas if needed
                if (!canvas.gameObject.activeSelf)
                {
                    Debug.LogWarning("[EvacuationQuizUI] Canvas was DISABLED! Enabling it now...");
                    canvas.gameObject.SetActive(true);
                }

                Canvas.ForceUpdateCanvases();
            }
        }
        else
        {
            Debug.LogError("[EvacuationQuizUI] ❌ panelRoot is not assigned!");
            return;
        }

        ResetQuiz();
        SetupQuestion();
        Debug.Log("[EvacuationQuizUI] Quiz setup complete");
    }

    private void ResetQuiz()
    {
        selectedAnswer = -1;
        hasAnswered = false;

        if (confirmButton != null)
            confirmButton.interactable = false;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        // Reset all button colors
        if (answerImageA != null) answerImageA.color = normalColor;
        if (answerImageB != null) answerImageB.color = normalColor;
        if (answerImageC != null) answerImageC.color = normalColor;
    }

    private void SetupQuestion()
    {
        if (questionText != null)
            questionText.text = question;

        if (answerTextA != null)
            answerTextA.text = answerA;
        if (answerTextB != null)
            answerTextB.text = answerB;
        if (answerTextC != null)
            answerTextC.text = answerC;
    }

    private void SelectAnswer(int answerIndex)
    {
        if (hasAnswered) return;

        selectedAnswer = answerIndex;

        // Reset all colors
        if (answerImageA != null) answerImageA.color = normalColor;
        if (answerImageB != null) answerImageB.color = normalColor;
        if (answerImageC != null) answerImageC.color = normalColor;

        // Highlight selected answer
        switch (answerIndex)
        {
            case 0:
                if (answerImageA != null) answerImageA.color = selectedColor;
                break;
            case 1:
                if (answerImageB != null) answerImageB.color = selectedColor;
                break;
            case 2:
                if (answerImageC != null) answerImageC.color = selectedColor;
                break;
        }

        // Enable confirm button
        if (confirmButton != null)
            confirmButton.interactable = true;

        // Do not auto-confirm on correct selection; require the player to press Confirm
    }

    private void OnConfirmClicked()
    {
        if (hasAnswered || selectedAnswer == -1) return;

        hasAnswered = true;

        // Check if answer is correct
        bool isCorrect = (selectedAnswer == correctAnswerIndex);

        if (isCorrect)
        {
            HandleCorrectAnswer();
        }
        else
        {
            HandleIncorrectAnswer();
        }
    }

    private void HandleCorrectAnswer()
    {
        // Keep the green highlight
        Debug.Log("[EvacuationQuiz] Correct answer selected!");

        // Show correct feedback
        ShowFeedback(correctFeedback);

        // Immediately notify manager that evacuation quiz succeeded and close the panel
        hasAnswered = true;
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (BeforeSceneManager.Instance != null)
        {
            BeforeSceneManager.Instance.OnEvacuationQuizCompleted(true);
        }
    }

    private void HandleIncorrectAnswer()
    {
        // Change selected answer to red
        switch (selectedAnswer)
        {
            case 0:
                if (answerImageA != null) answerImageA.color = incorrectColor;
                break;
            case 1:
                if (answerImageB != null) answerImageB.color = incorrectColor;
                break;
            case 2:
                if (answerImageC != null) answerImageC.color = incorrectColor;
                break;
        }

        Debug.Log("[EvacuationQuiz] Incorrect answer selected!");

        // Show incorrect feedback
        string feedback = selectedAnswer == 0 ? incorrectFeedbackA : incorrectFeedbackC;
        ShowFeedback(feedback);

        // Allow retry
        StartCoroutine(AllowRetryAfterDelay());
    }

    private void ShowFeedback(string message)
    {
        if (feedbackPanel != null && feedbackText != null)
        {
            feedbackText.text = message;
            feedbackPanel.SetActive(true);
        }
    }

    private IEnumerator AllowRetryAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplayDuration);

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        // Reset for retry
        hasAnswered = false;
        selectedAnswer = -1;

        if (confirmButton != null)
            confirmButton.interactable = false;

        // Reset colors
        if (answerImageA != null) answerImageA.color = normalColor;
        if (answerImageB != null) answerImageB.color = normalColor;
        if (answerImageC != null) answerImageC.color = normalColor;
    }

    private IEnumerator CompleteQuizAfterDelay(bool success)
    {
        yield return new WaitForSeconds(feedbackDisplayDuration);

        // Hide quiz panel
        if (panelRoot != null)
            panelRoot.SetActive(false);

        // Notify BeforeSceneManager
        if (BeforeSceneManager.Instance != null)
        {
            BeforeSceneManager.Instance.OnEvacuationQuizCompleted(success);
        }
    }

    public void HideQuiz()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
