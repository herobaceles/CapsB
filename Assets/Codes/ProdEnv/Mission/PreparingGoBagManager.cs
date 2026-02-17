using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles the dialogue flow and logic for the Preparing Go Bag mission.
/// Attach this script to a dedicated GameObject (e.g., PreparingGoBagManager).
/// </summary>
public class PreparingGoBagManager : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.RawImage cutsceneRawImage; // Assign in inspector
    private void OnDisable()
    {
        StopAllCoroutines();
        if (cutsceneRawImage != null)
            cutsceneRawImage.gameObject.SetActive(false);
        if (videoPlayerObject != null)
            videoPlayerObject.SetActive(false);
    }
    public static PreparingGoBagManager Instance { get; private set; }
    [Header("Achievement UI")]
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private TMPro.TextMeshProUGUI achievementText;
    [Header("Cutscene Video")]
    [SerializeField] private GameObject videoPlayerObject; // Assign a VideoPlayer GameObject or panel
    [SerializeField] private float cutsceneDuration = 5f; // Duration in seconds (replace with actual video length)

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        StartCoroutine(PlayCutsceneThenShowDialogue());
    }

    // Show achievement panel after Next is pressed
    private void ShowAchievementPanel()
    {
        if (achievementPanel != null)
            achievementPanel.SetActive(true);
        if (achievementText != null)
            achievementText.text = "Task Complete!";
    }

    private System.Collections.IEnumerator PlayCutsceneThenShowDialogue()
    {
        // Show RawImage for cutscene
        if (cutsceneRawImage != null)
            cutsceneRawImage.gameObject.SetActive(true);
        if (videoPlayerObject != null)
            videoPlayerObject.SetActive(true);

        // Wait for the cutscene to finish (replace with actual video end event if needed)
        yield return new WaitForSeconds(cutsceneDuration);

        // Hide video cutscene and RawImage
        if (videoPlayerObject != null)
            videoPlayerObject.SetActive(false);
        if (cutsceneRawImage != null)
            cutsceneRawImage.gameObject.SetActive(false);

        // Show instruction dialogue after cutscene
        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "Oh dear! That's a Signal Red warning, Edward. This is serious. The water could rise fast. We need to act now, but we must act smartly. Your family's safety depends on what you do before the flood hits. This is your first test."),
            new ProdDialogueLine("Professor Lingap", "Now find the table that has the bag and the items")
        };
        ProdDialogueManager.Instance.ShowDialogueSequence(lines);
    }


    // Called when the player finds the bag, shows dialogue and invokes callback when done
    public void ShowBagFoundDialogue(UnityEngine.Events.UnityAction onNext)
    {
        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "You found the Go Bag!")
        };
        ProdDialogueManager.Instance.ShowDialogueSequence(lines, onNext);
    }

    // Example handler for Next button after bag found (no longer needed, handled by DialogueManager)

    // Called to show completion dialogue and achievement
    public void ShowCompletionDialogueAndAchievement()
    {
        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "Nice you packed all the items"),
            new ProdDialogueLine("Professor Lingap", "Mission Complete!")
        };
        // Hide achievement panel first
        if (achievementPanel != null)
            achievementPanel.SetActive(false);
        // Show dialogue, then show achievement panel after user clicks next
        ProdDialogueManager.Instance.ShowDialogueSequence(lines, () => {
            ShowAchievementPanel();
        });
    }
}
