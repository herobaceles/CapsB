using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles the dialogue flow and logic for the Preparing Go Bag mission.
/// Attach this script to a dedicated GameObject (e.g., PreparingGoBagManager).
/// </summary>
public class PreparingGoBagManager : MonoBehaviour
{
    private void OnDisable()
    {
        StopAllCoroutines();
        if (cutsceneRawImage != null)
            cutsceneRawImage.gameObject.SetActive(false);
        if (videoPlayerObject != null)
            videoPlayerObject.SetActive(false);
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (nextButton != null)
            nextButton.SetActive(false);
    }
    public static PreparingGoBagManager Instance { get; private set; }
    private System.Action onBagFoundNext;
    [Header("Achievement UI")]
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private TMPro.TextMeshProUGUI achievementText;
    // Dialogue state: 0 = warning, 1+ = dialogueLines
    private int dialogueState = 0;
    [Header("Cutscene UI")]
    [SerializeField] private UnityEngine.UI.RawImage cutsceneRawImage; // Assign the RawImage in the Canvas
    [Header("Cutscene Video")]
    [SerializeField] private GameObject videoPlayerObject; // Assign a VideoPlayer GameObject or panel
    [SerializeField] private float cutsceneDuration = 5f; // Duration in seconds (replace with actual video length)

    [Header("Player Info")]
    [SerializeField] private string playerName = "Player"; // Set this from your player profile system
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject prevButton;

    [Header("Dialogue Content")]
    [TextArea(2, 5)]
    public List<string> dialogueLines = new List<string>(); // No extra dialogue

    private bool showAchievementOnNext = false;
    private int currentLine = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        currentLine = 0;
        StartCoroutine(PlayCutsceneThenShowDialogue());
    }

    // Show achievement panel after Next is pressed
    private void ShowAchievementPanel()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
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
        currentLine = 0;
        dialogueLines.Clear();
        dialogueLines.Add("Oh dear! That's a Signal Red warning, Edward. This is serious. The water could rise fast. We need to act now, but we must act smartly. Your family's safety depends on what you do before the flood hits. This is your first test.");
        dialogueLines.Add("Now find the table that has the bag and the items");
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (dialogueText != null && dialogueLines.Count > 0)
            StartCoroutine(TypeLine(dialogueLines[currentLine]));
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
                        StopAllCoroutines();
                        StartCoroutine(TypeLine(dialogueLines[currentLine]));
                    }
                    else
                    {
                        // Hide panel after instructions
                        if (dialoguePanel != null)
                            dialoguePanel.SetActive(false);
                        if (nextButton != null)
                            nextButton.SetActive(false);
                    }
                });
            }
        }

    // Typing animation coroutine
    private System.Collections.IEnumerator TypeLine(string line)
    {
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(0.02f); // Adjust speed as needed
        }
    }
    }

    // Called when the player finds the bag, shows dialogue and invokes callback when done
    public void ShowBagFoundDialogue(System.Action onNext)
    {
        // Example: Show dialogue, then call onNext when finished
        // You can customize this logic as needed
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        currentLine = 0;
        // Show 'You found the Go Bag!' dialogue
        dialogueLines.Clear();
        dialogueLines.Add("You found the Go Bag!");
        onBagFoundNext = onNext;
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
                    OnBagFoundNextPressed();
                });
            }
        }
    }

    // Example handler for Next button after bag found
    private void OnBagFoundNextPressed()
    {
        if (onBagFoundNext != null)
        {
            onBagFoundNext.Invoke();
            onBagFoundNext = null;
        }
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (nextButton != null)
            nextButton.SetActive(false);
    }

    // Called to show completion dialogue and achievement
    public void ShowCompletionDialogueAndAchievement()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (dialogueText != null)
            dialogueText.text = "Mission Complete!";
        ShowAchievementPanel();
    }
}
