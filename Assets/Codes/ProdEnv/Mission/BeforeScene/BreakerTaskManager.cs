using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BreakerTaskManager : MonoBehaviour
{
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

    private bool taskStarted = false;
    private bool taskComplete = false;

    private void Awake()
    {
        Instance = this;
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
        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "Now, turn off the circuit breaker to prevent electrical hazards before the flood!")
        };
        ProdDialogueManager.Instance.ShowDialogueSequence(lines);
    }

    // Call this when the player completes the breaker task
    public void CompleteBreakerTask(UnityAction onComplete = null)
    {
        if (taskComplete)
            return;
        taskComplete = true;

        // First, end AR and return to the normal scene
        if (BeforeMissionManager.Instance != null)
            BeforeMissionManager.Instance.EndARMission();

        // Now show completion dialogue (player is back in the scene with normal camera)
        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "Well done! Turning off the circuit breaker before a flood is critical. Floodwater conducts electricity — leaving it on can cause electrocution, fires, or damage to your appliances."),
            new ProdDialogueLine("Professor Lingap", "Always remember: before evacuating, switch off the main breaker. It could save lives!")
        };

        if (ProdDialogueManager.Instance != null)
        {
            ProdDialogueManager.Instance.ShowDialogueSequence(lines, () =>
            {
                if (BeforeMissionManager.Instance != null && BeforeMissionManager.Instance.IsMissionActive)
                {
                    BeforeMissionManager.Instance.CompleteCurrentTask();
                }
                onComplete?.Invoke();
            });
        }
        else
        {
            if (BeforeMissionManager.Instance != null && BeforeMissionManager.Instance.IsMissionActive)
            {
                BeforeMissionManager.Instance.CompleteCurrentTask();
            }
            onComplete?.Invoke();
        }
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
}
