using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles the dialogue flow and logic for the Preparing Go Bag mission.
/// Attach this script to a dedicated GameObject (e.g., PreparingGoBagManager).
/// </summary>
public class PreparingGoBagManager : MonoBehaviour
{
    private const string PreparingGoBagMissionId = "before_01";
    private const string PreparingGoBagTaskId = "before_01_prepare_go_bag";
    private const string QuizCharacterName = "Professor Lingap";

    [SerializeField] private UnityEngine.UI.RawImage cutsceneRawImage; // Assign in inspector
    private void OnDisable()
    {
        StopAllCoroutines();
        SetPlayerMovementLocked(false);
        if (cutsceneRawImage != null)
            cutsceneRawImage.gameObject.SetActive(false);
        if (videoPlayerObject != null)
            videoPlayerObject.SetActive(false);
    }
    public static PreparingGoBagManager Instance { get; private set; }
    public bool IsCutscenePlaying { get; private set; }
    [Header("Achievement UI")]
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private TMPro.TextMeshProUGUI achievementText;
    [Header("Cutscene Video")]
    [SerializeField] private GameObject videoPlayerObject; // Assign a VideoPlayer GameObject or panel
    [SerializeField] private float cutsceneDuration = 5f; // Duration in seconds (replace with actual video length)
    [SerializeField] private UnityEngine.UI.Button skipButton; // Assign the Skip / Fast-Forward button
    [SerializeField] private QuizDialogueUIManager quizDialogueUI;

    private bool cutsceneSkipped = false;
    private IsometricPlayerController playerController;

    private void Awake()
    {
        Instance = this;

        // Wire up skip button
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipCutscene);
            skipButton.gameObject.SetActive(false); // hidden until cutscene starts
        }
    }

    private void OnEnable()
    {
        if (!IsPreparingGoBagMissionActive())
        {
            if (cutsceneRawImage != null)
                cutsceneRawImage.gameObject.SetActive(false);
            if (videoPlayerObject != null)
                videoPlayerObject.SetActive(false);
            return;
        }

        if (quizDialogueUI == null)
            quizDialogueUI = FindObjectOfType<QuizDialogueUIManager>();

        SetPlayerMovementLocked(true);
        StartCoroutine(PlayCutsceneThenShowDialogue());
    }

    private bool IsPreparingGoBagMissionActive()
    {
        if (MissionSelectManager.SelectedMission == null)
            return false;

        return string.Equals(
            MissionSelectManager.SelectedMission.missionId,
            PreparingGoBagMissionId,
            System.StringComparison.OrdinalIgnoreCase
        );
    }

    // Show achievement panel after Next is pressed
    private void ShowAchievementPanel()
    {
        if (achievementPanel != null)
            achievementPanel.SetActive(true);
        if (achievementText != null)
            achievementText.text = "Task Complete!";
    }

    /// <summary>
    /// Call this from the Skip / Fast-Forward button to end the cutscene immediately.
    /// </summary>
    public void SkipCutscene()
    {
        if (!IsCutscenePlaying) return;
        cutsceneSkipped = true;
    }

    private void EndCutsceneVisuals()
    {
        var vp = videoPlayerObject != null
            ? videoPlayerObject.GetComponent<UnityEngine.Video.VideoPlayer>()
            : null;

        if (vp != null && vp.isPlaying)
            vp.Stop();

        if (videoPlayerObject != null)
            videoPlayerObject.SetActive(false);
        if (cutsceneRawImage != null)
            cutsceneRawImage.gameObject.SetActive(false);
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator PlayCutsceneThenShowDialogue()
    {
        IsCutscenePlaying = true;
        cutsceneSkipped = false;

        // Hide dialogue UI during cutscene so nothing leaks through
        if (ProdDialogueManager.Instance != null)
            ProdDialogueManager.Instance.HideDialogue();

        // Show RawImage for cutscene
        if (cutsceneRawImage != null)
            cutsceneRawImage.gameObject.SetActive(true);
        if (videoPlayerObject != null)
            videoPlayerObject.SetActive(true);

        // Show skip button
        if (skipButton != null)
            skipButton.gameObject.SetActive(true);

        // Wait for the video to finish or be skipped
        var vp = videoPlayerObject != null
            ? videoPlayerObject.GetComponent<UnityEngine.Video.VideoPlayer>()
            : null;

        if (vp != null)
        {
            if (!vp.isPlaying)
                vp.Play();

            // Wait until the VideoPlayer starts (can take a frame)
            yield return new WaitUntil(() => vp.isPlaying || cutsceneSkipped);

            // Wait until it finishes OR gets skipped
            yield return new WaitUntil(() => !vp.isPlaying || cutsceneSkipped);
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < cutsceneDuration && !cutsceneSkipped)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // Clean up visuals
        EndCutsceneVisuals();

        IsCutscenePlaying = false;

        ShowPostCutsceneBriefing();
    }

    private void ShowPostCutsceneBriefing()
    {
        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine(QuizCharacterName, "Oh no! That's a Signal Red warning. This is serious. The water could rise fast, so we need to act smartly before the flood hits."),
            new ProdDialogueLine(QuizCharacterName, "Before you move, answer this quick question first.")
        };

        if (ProdDialogueManager.Instance != null)
        {
            ProdDialogueManager.Instance.ShowDialogueSequence(lines, ShowStartQuizGate);
            return;
        }

        ShowStartQuizGate();
    }

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
            Debug.LogWarning("PreparingGoBagManager: QuizDialogueUIManager not found. Skipping quiz gate to avoid soft lock.");
            SetPlayerMovementLocked(false);
            return;
        }

        quizDialogueUI.ShowQuiz(quizData, OnStartQuizAnsweredCorrectly);
    }

    private void OnStartQuizAnsweredCorrectly()
    {
        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine(QuizCharacterName, "Now find the table that has the go bag.")
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
        SetPlayerMovementLocked(false);
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

    private void SetPlayerMovementLocked(bool isLocked)
    {
        if (playerController == null)
            playerController = FindObjectOfType<IsometricPlayerController>();

        if (playerController == null)
            return;

        var controllerType = playerController.GetType();
        var setMovementEnabledMethod = controllerType.GetMethod("SetMovementEnabled", new[] { typeof(bool) });

        if (setMovementEnabledMethod != null)
        {
            setMovementEnabledMethod.Invoke(playerController, new object[] { !isLocked });
            return;
        }

        if (isLocked)
            playerController.StopMovement();
    }


    // Called when the player finds the bag, shows dialogue and invokes callback when done
    public void ShowBagFoundDialogue(UnityEngine.Events.UnityAction onNext)
    {
        if (!IsPreparingGoBagMissionActive())
        {
            onNext?.Invoke();
            return;
        }

        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "You found the Go Bag!")
        };
        if (ProdDialogueManager.Instance != null)
            ProdDialogueManager.Instance.ShowDialogueSequence(lines, onNext);
        else
            onNext?.Invoke();
    }

    // Example handler for Next button after bag found (no longer needed, handled by DialogueManager)

    // Called to show completion dialogue and achievement
    public void ShowCompletionDialogueAndAchievement()
    {
        if (!IsPreparingGoBagMissionActive())
            return;

        var lines = new List<ProdDialogueLine>
        {
            new ProdDialogueLine("Professor Lingap", "Excellent! Food, water, a first-aid kit, and a light source. That's a perfect start! Your Go Bag is ready for evacuation."),
            new ProdDialogueLine("Professor Lingap", "Mission Complete!")
        };

        // Show dialogue, then complete the mission task when finished
        if (ProdDialogueManager.Instance != null)
        {
            ProdDialogueManager.Instance.ShowDialogueSequence(lines, () => {
                CompleteMissionTask();
            });
        }
        else
        {
            CompleteMissionTask();
        }
    }

    /// <summary>
    /// Finalizes the mission after dialogue. Completes the current task in MissionSceneManager.
    /// </summary>
    private void CompleteMissionTask()
    {
        var missionManager = BeforeMissionManager.Instance;
        if (missionManager == null || !missionManager.IsMissionActive)
            return;

        var currentTask = missionManager.CurrentTask;
        if (currentTask == null)
        {
            Debug.LogWarning("PreparingGoBagManager: Cannot complete task because no current task is active.");
            return;
        }

        if (!string.Equals(currentTask.taskId, PreparingGoBagTaskId, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"PreparingGoBagManager: Current task '{currentTask.taskId}' does not match expected task '{PreparingGoBagTaskId}'.");
            return;
        }

        missionManager.CompleteCurrentTask();
    }

    private void OnDestroy()
    {
        SetPlayerMovementLocked(false);

        if (Instance == this)
            Instance = null;
    }
}
