using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles the dialogue flow and logic for the Circuit Breaker mission.
/// Attach this script to a dedicated GameObject (e.g., CircuitBreakerManager).
/// </summary>
public class CircuitBreakerManager : MonoBehaviour
{
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static CircuitBreakerManager Instance { get; private set; }
    private System.Action onTaskComplete;

    [Header("Achievement UI")]
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private TMPro.TextMeshProUGUI achievementText;


    // Dialogue handled by ProdDialogueManager

    private void OnEnable()
    {
        ShowInstructionDialogue();
    }

    // Cutscene logic removed; dialogue now shows immediately on enable.

    private void ShowInstructionDialogue()
    {
        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "Now for your next task is to find the circuit breaker. It is near the door.")
        };
        ProdDialogueManager.Instance.ShowDialogueSequence(lines, ShowStartQuizGate);
    }

    // --- Quiz UI logic (copied/adapted from PrepareGoBag) ---
    private QuizDialogueUIManager quizDialogueUI;

    private void ShowStartQuizGate()
    {
        if (!TryGetStartQuiz(out MissionQuizData quizData) || !IsQuizDataValid(quizData))
        {
            CompleteStartGate();
            return;
        }

        if (quizDialogueUI == null)
            quizDialogueUI = FindObjectOfType<QuizDialogueUIManager>();

        if (quizDialogueUI == null)
        {
            Debug.LogWarning("CircuitBreakerManager: QuizDialogueUIManager not found. Skipping quiz gate to avoid soft lock.");
            CompleteStartGate();
            return;
        }

        quizDialogueUI.ShowQuiz(quizData, OnStartQuizAnsweredCorrectly);
    }

    private void OnStartQuizAnsweredCorrectly()
    {
        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "Correct! Now, find and interact with the circuit breaker.")
        };

        if (ProdDialogueManager.Instance != null)
        {
            ProdDialogueManager.Instance.ShowDialogueSequence(lines, CompleteStartGate);
            return;
        }

        CompleteStartGate();
    }

    private void CompleteStartGate()
    {
        StartTask();
    }

    private bool TryGetStartQuiz(out MissionQuizData quizData)
    {
        quizData = null;
        var selectedMission = MissionSelectManager.SelectedMission;
        if (selectedMission == null)
            return false;
        quizData = selectedMission.startQuiz;
        return quizData != null;
    }

    private bool IsQuizDataValid(MissionQuizData quizData)
    {
        if (quizData == null)
            return false;
        if (string.IsNullOrWhiteSpace(quizData.question))
            return false;
        if (quizData.options == null || quizData.options.Length < 3)
            return false;
        for (int i = 0; i < 3; i++)
        {
            if (string.IsNullOrWhiteSpace(quizData.options[i]))
                return false;
        }
        return quizData.correctOptionIndex >= 0 && quizData.correctOptionIndex < 3;
    }

    // Call this when the player completes the circuit breaker task
    public void CompleteTask(System.Action onComplete = null)
    {
        onTaskComplete = onComplete;
        ShowAchievementPanel();
    }

    private void ShowAchievementPanel()
    {
        if (achievementPanel != null)
            achievementPanel.SetActive(true);
        if (achievementText != null)
            achievementText.text = "Task Complete!";
        // Call the callback to notify BeforeMissionManager or next system
        if (onTaskComplete != null)
        {
            onTaskComplete.Invoke();
            onTaskComplete = null;
        }
    }

    // Placeholder for actual task logic
    private void StartTask()
    {
        // Implement your circuit breaker interaction logic here
        // When done, call CompleteTask();
    }
}
