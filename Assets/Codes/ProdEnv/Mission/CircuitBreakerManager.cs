using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles the dialogue flow and logic for the Circuit Breaker mission.
/// Attach this script to a dedicated GameObject (e.g., CircuitBreakerManager).
/// </summary>
public class CircuitBreakerManager : MonoBehaviour
{
    private void OnDisable()
    {
        StopAllCoroutines();
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
        ProdDialogueManager.Instance.ShowDialogueSequence(lines, StartTask);
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
