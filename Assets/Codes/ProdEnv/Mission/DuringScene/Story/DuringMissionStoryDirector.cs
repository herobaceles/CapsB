using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Orchestrates story dialogue through the NPC bubble system during the
/// During-phase mission. Subscribes to mission events and queues contextual
/// lines on the Professor Lingap follower.
/// </summary>
public class DuringMissionStoryDirector : MonoBehaviour
{
    public static DuringMissionStoryDirector Instance { get; private set; }

    [Header("NPC Reference")]
    [SerializeField] private NPCFollower speakingNPC;

    [Header("Dialogue Timing")]
    [SerializeField] private float defaultLineDuration = 3.5f;
    [SerializeField] private float lineGap = 0.3f;

    [Header("Task Dialogue Overrides")]
    [SerializeField] private List<TaskDialogueOverride> taskDialogueOverrides = new List<TaskDialogueOverride>();

    [Header("Events")]
    public UnityEvent OnDialogueStarted;
    public UnityEvent OnDialogueFinished;

    private Queue<DialogueLine> lineQueue = new Queue<DialogueLine>();
    private Coroutine playbackRoutine;
    private bool isPlayingDialogue;
    private Action pendingCallback;

    [Serializable]
    public class TaskDialogueOverride
    {
        public string taskId;
        [TextArea(2, 4)] public string[] startLines;
        [TextArea(2, 4)] public string[] completeLines;
        [TextArea(2, 4)] public string[] failLines;
    }

    [Serializable]
    private struct DialogueLine
    {
        public string text;
        public float duration;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (speakingNPC == null)
            speakingNPC = FindObjectOfType<NPCFollower>();

        SubscribeToMissionEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnsubscribeFromMissionEvents();
    }

    private void SubscribeToMissionEvents()
    {
        if (MissionSceneManager.Instance != null)
        {
            MissionSceneManager.Instance.OnMissionStarted.AddListener(HandleMissionStarted);
            MissionSceneManager.Instance.OnTaskStarted.AddListener(HandleTaskStarted);
            MissionSceneManager.Instance.OnTaskCompleted.AddListener(HandleTaskCompleted);
            MissionSceneManager.Instance.OnMissionCompleted.AddListener(HandleMissionCompleted);
        }
    }

    private void UnsubscribeFromMissionEvents()
    {
        if (MissionSceneManager.Instance != null)
        {
            MissionSceneManager.Instance.OnMissionStarted.RemoveListener(HandleMissionStarted);
            MissionSceneManager.Instance.OnTaskStarted.RemoveListener(HandleTaskStarted);
            MissionSceneManager.Instance.OnTaskCompleted.RemoveListener(HandleTaskCompleted);
            MissionSceneManager.Instance.OnMissionCompleted.RemoveListener(HandleMissionCompleted);
        }
    }

    #region Mission Event Handlers

    private void HandleMissionStarted()
    {
        Debug.Log("StoryDirector: Mission started.");
    }

    private void HandleTaskStarted(TaskData task)
    {
        if (task == null) return;

        Debug.Log($"StoryDirector: Task started - {task.taskId}");

        // Check for override dialogue
        var overrideData = FindOverride(task.taskId);
        if (overrideData != null && overrideData.startLines != null && overrideData.startLines.Length > 0)
        {
            QueueLines(overrideData.startLines);
        }
    }

    private void HandleTaskCompleted(TaskData task)
    {
        if (task == null) return;

        Debug.Log($"StoryDirector: Task completed - {task.taskId}");

        var overrideData = FindOverride(task.taskId);
        if (overrideData != null && overrideData.completeLines != null && overrideData.completeLines.Length > 0)
        {
            QueueLines(overrideData.completeLines);
        }
    }

    private void HandleMissionCompleted(MissionData mission)
    {
        Debug.Log($"StoryDirector: Mission completed - {mission?.missionId}");
    }

    private TaskDialogueOverride FindOverride(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId)) return null;

        foreach (var entry in taskDialogueOverrides)
        {
            if (string.Equals(entry.taskId, taskId, StringComparison.OrdinalIgnoreCase))
                return entry;
        }
        return null;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Queue a single line to be spoken by the NPC.
    /// </summary>
    public void SpeakLine(string text, float duration = -1f)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        lineQueue.Enqueue(new DialogueLine
        {
            text = text,
            duration = duration > 0f ? duration : defaultLineDuration
        });

        if (playbackRoutine == null)
            playbackRoutine = StartCoroutine(PlaybackRoutine());
    }

    /// <summary>
    /// Queue multiple lines to be spoken sequentially.
    /// </summary>
    public void QueueLines(string[] lines, float duration = -1f, Action onComplete = null)
    {
        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                lineQueue.Enqueue(new DialogueLine
                {
                    text = line,
                    duration = duration > 0f ? duration : defaultLineDuration
                });
            }
        }

        pendingCallback = onComplete;

        if (playbackRoutine == null)
            playbackRoutine = StartCoroutine(PlaybackRoutine());
    }

    /// <summary>
    /// Speak contextual feedback for AR task outcomes.
    /// </summary>
    public void SpeakTaskFeedback(string taskId, bool success, string customLine = null)
    {
        if (!string.IsNullOrWhiteSpace(customLine))
        {
            SpeakLine(customLine);
            return;
        }

        var overrideData = FindOverride(taskId);
        if (overrideData != null)
        {
            string[] lines = success ? overrideData.completeLines : overrideData.failLines;
            if (lines != null && lines.Length > 0)
            {
                QueueLines(lines);
            }
        }
    }

    /// <summary>
    /// Clear all queued dialogue immediately.
    /// </summary>
    public void ClearQueue()
    {
        lineQueue.Clear();
        if (playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }
        isPlayingDialogue = false;

        if (speakingNPC != null)
            speakingNPC.HideDialogueBubble();

        pendingCallback = null;
    }

    /// <summary>
    /// Whether dialogue is currently playing.
    /// </summary>
    public bool IsPlaying => isPlayingDialogue || lineQueue.Count > 0;

    #endregion

    #region Playback

    private IEnumerator PlaybackRoutine()
    {
        isPlayingDialogue = true;
        OnDialogueStarted?.Invoke();

        while (lineQueue.Count > 0)
        {
            DialogueLine line = lineQueue.Dequeue();

            if (speakingNPC != null)
            {
                Debug.Log($"StoryDirector: Speaking -> {line.text}");
                speakingNPC.SpeakLine(line.text, line.duration);
            }

            // Wait for line duration plus gap
            yield return new WaitForSeconds(line.duration + lineGap);
        }

        isPlayingDialogue = false;
        playbackRoutine = null;

        OnDialogueFinished?.Invoke();

        pendingCallback?.Invoke();
        pendingCallback = null;
    }

    #endregion
}
