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
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (nextButton != null)
            nextButton.SetActive(false);
    }
    public static CircuitBreakerManager Instance { get; private set; }
    private System.Action onTaskComplete;

    [Header("Achievement UI")]
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private TMPro.TextMeshProUGUI achievementText;


    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject prevButton;

    [Header("Dialogue Content")]
    [TextArea(2, 5)]
    public List<string> dialogueLines = new List<string>();

    private int currentLine = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        currentLine = 0;
        ShowInstructionDialogue();
    }

    // Cutscene logic removed; dialogue now shows immediately on enable.

    private void ShowInstructionDialogue()
    {
        dialogueLines.Clear();
        dialogueLines.Add("The flood is coming! You must secure the circuit breaker to prevent electrical hazards.");
        dialogueLines.Add("Find the circuit breaker and turn it off before the water rises.");
        currentLine = 0;
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (dialogueText != null && dialogueLines.Count > 0)
            dialogueText.text = dialogueLines[currentLine];
        if (nextButton != null)
        {
            nextButton.SetActive(true);
            var btn = nextButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    currentLine++;
                    if (currentLine < dialogueLines.Count)
                    {
                        dialogueText.text = dialogueLines[currentLine];
                    }
                    else
                    {
                        if (dialoguePanel != null)
                            dialoguePanel.SetActive(false);
                        if (nextButton != null)
                            nextButton.SetActive(false);
                        // Start the actual task logic here
                        StartTask();
                    }
                });
            }
        }
    }

    // Call this when the player completes the circuit breaker task
    public void CompleteTask(System.Action onComplete = null)
    {
        onTaskComplete = onComplete;
        ShowAchievementPanel();
    }

    private void ShowAchievementPanel()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
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
