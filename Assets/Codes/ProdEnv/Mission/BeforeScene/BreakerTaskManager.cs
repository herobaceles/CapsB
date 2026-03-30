using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BreakerTaskManager : MonoBehaviour
{
    private const string BreakerTaskId = "before_02_secure_circuit_breaker";
    // Call this from the Restart button to restart the AR breaker task
    public void RestartBreakerTask()
    {
        Debug.Log("[BreakerTaskManager] RestartBreakerTask called");
        // Try to destroy by tag first
        var breakers = GameObject.FindGameObjectsWithTag("Breaker");
        if (breakers.Length == 0)
        {
            // Fallback: try to find by name (in case tag is missing)
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Breaker"))
                {
                    Debug.Log("[BreakerTaskManager] Destroying object: " + obj.name);
                    Destroy(obj);
                }
            }
        }
        else
        {
            foreach (var obj in breakers)
            {
                Debug.Log("[BreakerTaskManager] Destroying tagged breaker: " + obj.name);
                Destroy(obj);
            }
        }
        taskStarted = false;
        taskComplete = false;
        if (achievementPanel != null)
            achievementPanel.SetActive(false);
        StartBreakerTask();
    }
    public static BreakerTaskManager Instance { get; private set; }

    [Header("Achievement UI")]
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private TMPro.TextMeshProUGUI achievementText;

    [Header("Breaker Task Prefab")]
    [SerializeField] private GameObject breakerPrefab; // Assign in inspector if you want to spawn it

    [Header("Temporary AR Exit UI")]
    [SerializeField] private GameObject breakerExitButton; // Shown only while breaker AR task is active

    [Header("Dialogue (asset-driven)")]
    [SerializeField] private List<DialogueLineData> instructionDialogueRich;
    [SerializeField] private List<DialogueLineData> completionDialogueRich;

    [Header("Completion Settings")]
    [SerializeField]
    [Tooltip("If enabled, the completion dialogue plays while still in AR, then exits back to the scene. If disabled, AR ends first and the dialogue plays in the normal scene.")]
    private bool showCompletionDialogueInAR = false;

    private bool taskStarted = false;
    private bool taskComplete = false;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartBreakerTask()
    {
        if (taskStarted)
            return;
        taskStarted = true;
        taskComplete = false;

        // Enable AR tap-to-place for breaker prefab
        if (breakerPrefab != null && ARMissionManager.Instance != null)
        {
            ARMissionManager.Instance.EnableBreakerPlacement(breakerPrefab);
        }

        // Show initial dialogue or instructions
        ShowInstructionDialogue();
    }

    private void ShowInstructionDialogue()
    {
        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager == null)
        {
            Debug.LogWarning("BreakerTaskManager: ProdDialogueManager not found; skipping instruction dialogue.");
            return;
        }

        if (instructionDialogueRich != null && instructionDialogueRich.Count > 0)
        {
            dialogueManager.ShowDialogueSequence(instructionDialogueRich, null);
        }
        else
        {
            Debug.LogWarning("BreakerTaskManager: instructionDialogueRich is empty; skipping instruction dialogue.");
        }
    }

    // Call this when the player completes the breaker task
    public void CompleteBreakerTask(UnityAction onComplete = null)
    {
        if (taskComplete)
            return;
        taskComplete = true;

        if (breakerExitButton != null)
            breakerExitButton.SetActive(false);
        var dialogueManager = ProdDialogueManager.Instance;

        // Decide whether to play the completion dialogue while still in AR
        // or after returning to the normal scene.
        if (showCompletionDialogueInAR)
        {
            // Play completion dialogue first (using the current AR camera),
            // then exit AR and finalize the task.
            UnityAction afterDialogue = () =>
            {
                if (BeforeMissionManager.Instance != null)
                    BeforeMissionManager.Instance.EndARMission();

                CompleteExpectedTask();
                ShowAchievementPanel(onComplete);
            };

            if (dialogueManager != null && completionDialogueRich != null && completionDialogueRich.Count > 0)
            {
                dialogueManager.ShowDialogueSequence(completionDialogueRich, afterDialogue);
            }
            else
            {
                if (dialogueManager == null)
                {
                    Debug.LogWarning("BreakerTaskManager: ProdDialogueManager not found; skipping completion dialogue.");
                }
                else
                {
                    Debug.LogWarning("BreakerTaskManager: completionDialogueRich is empty; skipping completion dialogue.");
                }

                afterDialogue();
            }
        }
        else
        {
            // Original behaviour: end AR first so the dialogue appears in the
            // normal scene view, then complete the task.
            if (BeforeMissionManager.Instance != null)
                BeforeMissionManager.Instance.EndARMission();

            UnityAction afterDialogue = () =>
            {
                CompleteExpectedTask();
                ShowAchievementPanel(onComplete);
            };

            if (dialogueManager != null && completionDialogueRich != null && completionDialogueRich.Count > 0)
            {
                dialogueManager.ShowDialogueSequence(completionDialogueRich, afterDialogue);
            }
            else
            {
                if (dialogueManager == null)
                {
                    Debug.LogWarning("BreakerTaskManager: ProdDialogueManager not found; skipping completion dialogue.");
                }
                else
                {
                    Debug.LogWarning("BreakerTaskManager: completionDialogueRich is empty; skipping completion dialogue.");
                }

                afterDialogue();
            }
        }
    }

    private void CompleteExpectedTask()
    {
        var missionManager = BeforeMissionManager.Instance;
        if (missionManager == null || !missionManager.IsMissionActive)
            return;

        var currentTask = missionManager.CurrentTask;
        if (currentTask == null)
        {
            Debug.LogWarning("BreakerTaskManager: Cannot complete task because no current task is active.");
            return;
        }

        if (!string.Equals(currentTask.taskId, BreakerTaskId, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"BreakerTaskManager: Current task '{currentTask.taskId}' does not match expected task '{BreakerTaskId}'.");
            return;
        }

        missionManager.CompleteCurrentTask();
    }

    private void ShowAchievementPanel(UnityAction onComplete = null)
    {
        if (achievementPanel != null)
            achievementPanel.SetActive(true);
        if (achievementText != null)
            achievementText.text = "Breaker Task Complete!";
        if (onComplete != null)
            onComplete.Invoke();
    }

    // Temporary helper for UI button: call this from a button to
    // end the AR breaker task and show the completion flow manually.
    public void OnBreakerExitButtonClicked()
    {
        CompleteBreakerTask();
    }
}
