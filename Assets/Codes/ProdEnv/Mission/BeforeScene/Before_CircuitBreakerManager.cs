using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles the dialogue flow and logic for the Circuit Breaker mission.
/// Attach this script to a dedicated GameObject (e.g., CircuitBreakerManager).
/// </summary>
public class CircuitBreakerManager : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField] private string circuitBreakerTaskId = "before_02_secure_circuit_breaker";
    [SerializeField] private string dialogueSpeaker = "Professor Lingap";

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
        StartCoroutine(WaitForIntroThenShowInstruction());
    }

    // Cutscene logic removed; dialogue now shows immediately on enable.

    private System.Collections.IEnumerator WaitForIntroThenShowInstruction()
    {
        var missionManager = BeforeMissionManager.Instance;
        if (missionManager != null)
        {
            // Wait until the mission intro dialogue (and optional start quiz)
            // has fully completed before showing task start dialogue.
            while (!missionManager.HasIntroSequenceCompleted)
                yield return null;
        }

        ShowInstructionDialogue();
    }

    private void ShowInstructionDialogue()
    {
        var mission = MissionSelectManager.SelectedMission;
        var task = GetCircuitBreakerTask(mission);
        var dialogueManager = ProdDialogueManager.Instance;

        // Use rich start dialogue from the mission task if provided
        if (dialogueManager != null && task != null &&
            task.startDialogueRich != null && task.startDialogueRich.Count > 0)
        {
            dialogueManager.ShowDialogueSequence(task.startDialogueRich, ShowStartQuizGate);
            return;
        }

        // No dialogue configured; go straight to quiz gate
        ShowStartQuizGate();
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
        var mission = MissionSelectManager.SelectedMission;
        var task = GetCircuitBreakerTask(mission);
        var dialogueManager = ProdDialogueManager.Instance;

        // Use rich completion dialogue from the mission task if provided
        if (dialogueManager != null && task != null &&
            task.completeDialogueRich != null && task.completeDialogueRich.Count > 0)
        {
            dialogueManager.ShowDialogueSequence(task.completeDialogueRich, CompleteStartGate);
            return;
        }

        // No completion dialogue configured; just continue
        CompleteStartGate();
    }

    private TaskData GetCircuitBreakerTask(MissionData mission)
    {
        if (mission == null || mission.tasks == null)
            return null;

        foreach (var task in mission.tasks)
        {
            if (task != null && task.taskId == circuitBreakerTaskId)
                return task;
        }

        return null;
    }

    private List<ProdDialogueLine> BuildDialogueLines(string[] dialogue)
    {
        if (dialogue == null || dialogue.Length == 0)
            return null;

        var lines = new List<ProdDialogueLine>();
        foreach (var line in dialogue)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            lines.Add(new ProdDialogueLine(dialogueSpeaker, line));
        }

        return lines.Count > 0 ? lines : null;
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

        var task = GetCircuitBreakerTask(selectedMission);
        if (task == null)
            return false;

        quizData = task.taskStartQuiz;
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
