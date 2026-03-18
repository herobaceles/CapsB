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
        UnregisterMissionEvents();
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

    [Header("Dialogue (asset-driven)")]
    [SerializeField] private List<DialogueLineData> goBagFoundDialogueRich;
    [SerializeField] private List<DialogueLineData> goBagCompleteDialogueRich;

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

        RegisterMissionEvents();

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
            achievementText.text = "Preparing Go Bag Complete!";
    }

    public void SkipCutscene()
    {
        if (!IsCutscenePlaying) return;
        cutsceneSkipped = true;
    }

    private void EndCutsceneVisuals()
    {
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        if (cutsceneRawImage != null)
            cutsceneRawImage.gameObject.SetActive(false);

        if (videoPlayerObject != null)
            videoPlayerObject.SetActive(false);
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

        // After cutscene, show post-cutscene briefing and quiz
        ShowPostCutsceneBriefing();
    }

    private void RegisterMissionEvents()
    {
        if (!IsPreparingGoBagMissionActive())
            return;

        if (BeforeMissionManager.Instance != null)
        {
            BeforeMissionManager.Instance.OnMissionStarted.RemoveListener(OnMissionStartedForPreparingGoBag);
            BeforeMissionManager.Instance.OnMissionStarted.AddListener(OnMissionStartedForPreparingGoBag);
        }
    }

    private void UnregisterMissionEvents()
    {
        if (BeforeMissionManager.Instance != null)
        {
            BeforeMissionManager.Instance.OnMissionStarted.RemoveListener(OnMissionStartedForPreparingGoBag);
        }
    }

    private void OnMissionStartedForPreparingGoBag()
    {
        CompleteStartGate();
    }

    private void ShowPostCutsceneBriefing()
    {
        var mission = MissionSelectManager.SelectedMission;
        var task = GetTask(mission);
        var dialogueManager = ProdDialogueManager.Instance;

        // Use rich dialogue authored on the mission task (with sprites, backgrounds, sides)
        if (dialogueManager != null && task != null &&
            task.startDialogueRich != null && task.startDialogueRich.Count > 0)
        {
            dialogueManager.ShowDialogueSequence(task.startDialogueRich, ShowStartQuizGate);
            return;
        }

        // No dialogue configured in mission asset; proceed directly to quiz gate
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
        var mission = MissionSelectManager.SelectedMission;
        var task = GetTask(mission);
        var dialogueManager = ProdDialogueManager.Instance;

        // Use rich completion dialogue from the mission task if provided
        if (dialogueManager != null && task != null &&
            task.completeDialogueRich != null && task.completeDialogueRich.Count > 0)
        {
            dialogueManager.ShowDialogueSequence(task.completeDialogueRich, CompleteStartGate);
            return;
        }

        // No completion dialogue configured; just finish the gate
        CompleteStartGate();
    }

    private TaskData GetTask(MissionData mission)
    {
        if (mission == null || mission.tasks == null)
            return null;

        foreach (var task in mission.tasks)
        {
            if (task != null && task.taskId == PreparingGoBagTaskId)
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
            lines.Add(new ProdDialogueLine(QuizCharacterName, line));
        }

        return lines.Count > 0 ? lines : null;
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

        // Prefer the first valid entry from the mission's startQuizSequence
        if (selectedMission.startQuizSequence != null && selectedMission.startQuizSequence.Count > 0)
        {
            foreach (var q in selectedMission.startQuizSequence)
            {
                if (IsQuizDataValid(q))
                {
                    quizData = q;
                    return true;
                }
            }
        }

        // Fallback to the legacy single startQuiz field
        if (IsQuizDataValid(selectedMission.startQuiz))
        {
            quizData = selectedMission.startQuiz;
            return true;
        }

        return false;
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

        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager != null && goBagFoundDialogueRich != null && goBagFoundDialogueRich.Count > 0)
        {
            dialogueManager.ShowDialogueSequence(goBagFoundDialogueRich, onNext);
        }
        else
        {
            if (dialogueManager == null)
            {
                Debug.LogWarning("Before_PreparingGoBagManager: ProdDialogueManager not found; skipping Go Bag found dialogue.");
            }
            else
            {
                Debug.LogWarning("Before_PreparingGoBagManager: goBagFoundDialogueRich is empty; skipping Go Bag found dialogue.");
            }

            onNext?.Invoke();
        }
    }

    // Example handler for Next button after bag found (no longer needed, handled by DialogueManager)

    // Called to show completion dialogue and achievement
    public void ShowCompletionDialogueAndAchievement()
    {
        if (!IsPreparingGoBagMissionActive())
            return;

        var dialogueManager = ProdDialogueManager.Instance;
        if (dialogueManager != null && goBagCompleteDialogueRich != null && goBagCompleteDialogueRich.Count > 0)
        {
            dialogueManager.ShowDialogueSequence(goBagCompleteDialogueRich, () => {
                CompleteMissionTask();
                ShowAchievementPanel();
            });
        }
        else
        {
            if (dialogueManager == null)
            {
                Debug.LogWarning("Before_PreparingGoBagManager: ProdDialogueManager not found; skipping Go Bag completion dialogue.");
            }
            else
            {
                Debug.LogWarning("Before_PreparingGoBagManager: goBagCompleteDialogueRich is empty; skipping Go Bag completion dialogue.");
            }

            CompleteMissionTask();
            ShowAchievementPanel();
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
        UnregisterMissionEvents();
        SetPlayerMovementLocked(false);

        if (Instance == this)
            Instance = null;
    }
}
