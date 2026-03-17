using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Base class for AR mini-game tasks in the During phase.
/// Provides common lifecycle, validation, and completion logic.
/// Derive from this to create specific task types (drag-drop, sorting, etc.).
/// </summary>
public abstract class ARTaskBase : MonoBehaviour
{
    [Header("Task Identity")]
    [SerializeField] protected string taskId;
    [SerializeField] protected string taskDisplayName;
    [TextArea(2, 4)]
    [SerializeField] protected string instructions;

    [Header("UI")]
    [SerializeField] protected GameObject taskCanvas;
    [SerializeField] protected CanvasGroup canvasGroup;

    [Header("Timing")]
    [SerializeField] protected float showDelay = 0.2f;
    [SerializeField] protected float hideDelay = 0.5f;
    [SerializeField] protected bool hasTimeLimit = false;
    [SerializeField] protected float timeLimit = 60f;

    [Header("Dialogue")]
    [TextArea(2, 3)]
    [SerializeField] protected string[] startDialogue;
    [TextArea(2, 3)]
    [SerializeField] protected string[] successDialogue;
    [TextArea(2, 3)]
    [SerializeField] protected string[] failDialogue;

    [Header("Events")]
    public UnityEvent OnTaskStarted;
    public UnityEvent OnTaskCompleted;
    public UnityEvent OnTaskFailed;
    public UnityEvent<float> OnTimeUpdated;

    protected bool isActive;
    protected bool isCompleted;
    protected float elapsedTime;
    protected Coroutine timerRoutine;

    public string TaskId => taskId;
    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public float ElapsedTime => elapsedTime;
    public float RemainingTime => hasTimeLimit ? Mathf.Max(0f, timeLimit - elapsedTime) : -1f;

    protected virtual void Awake()
    {
        if (taskCanvas != null)
            taskCanvas.SetActive(false);
    }

    #region Lifecycle

    /// <summary>
    /// Begin this AR task. Shows UI and starts timer if applicable.
    /// </summary>
    public virtual void StartTask()
    {
        if (isActive) return;

        isActive = true;
        isCompleted = false;
        elapsedTime = 0f;

        StartCoroutine(ShowTaskRoutine());
    }

    /// <summary>
    /// Force end the task without completing it.
    /// </summary>
    public virtual void CancelTask()
    {
        if (!isActive) return;

        StopAllCoroutines();
        isActive = false;

        if (taskCanvas != null)
            taskCanvas.SetActive(false);

        Debug.Log($"ARTask [{taskId}]: Cancelled.");
    }

    /// <summary>
    /// Called when player successfully completes the task.
    /// </summary>
    protected virtual void CompleteTask()
    {
        if (!isActive || isCompleted) return;

        isCompleted = true;
        isActive = false;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        Debug.Log($"ARTask [{taskId}]: Completed successfully!");

        // Play success dialogue
        if (DuringMissionStoryDirector.Instance != null && successDialogue != null && successDialogue.Length > 0)
        {
            DuringMissionStoryDirector.Instance.QueueLines(successDialogue);
        }

        OnTaskCompleted?.Invoke();

        // Notify mission manager
        NotifyMissionManager(true);

        StartCoroutine(HideTaskRoutine());
    }

    /// <summary>
    /// Called when player fails the task (timeout or wrong actions).
    /// </summary>
    protected virtual void FailTask(string reason = null)
    {
        if (!isActive || isCompleted) return;

        isActive = false;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        Debug.Log($"ARTask [{taskId}]: Failed - {reason ?? "Unknown reason"}");

        // Play fail dialogue
        if (DuringMissionStoryDirector.Instance != null && failDialogue != null && failDialogue.Length > 0)
        {
            DuringMissionStoryDirector.Instance.QueueLines(failDialogue);
        }

        OnTaskFailed?.Invoke();

        // Notify mission manager
        NotifyMissionManager(false);

        StartCoroutine(HideTaskRoutine());
    }

    #endregion

    #region Show/Hide

    protected virtual IEnumerator ShowTaskRoutine()
    {
        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        if (taskCanvas != null)
            taskCanvas.SetActive(true);

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        // Play start dialogue
        if (DuringMissionStoryDirector.Instance != null && startDialogue != null && startDialogue.Length > 0)
        {
            DuringMissionStoryDirector.Instance.QueueLines(startDialogue);
        }

        OnTaskStarted?.Invoke();

        // Start timer
        if (hasTimeLimit)
        {
            timerRoutine = StartCoroutine(TimerRoutine());
        }

        // Initialize task-specific logic
        OnTaskShow();
    }

    protected virtual IEnumerator HideTaskRoutine()
    {
        // Fade out
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        if (hideDelay > 0f)
            yield return new WaitForSeconds(hideDelay);

        if (taskCanvas != null)
            taskCanvas.SetActive(false);

        // Cleanup task-specific
        OnTaskHide();
    }

    #endregion

    #region Timer

    protected virtual IEnumerator TimerRoutine()
    {
        while (elapsedTime < timeLimit)
        {
            elapsedTime += Time.deltaTime;
            OnTimeUpdated?.Invoke(RemainingTime);
            yield return null;
        }

        // Time's up
        FailTask("Time limit exceeded");
    }

    #endregion

    #region Mission Integration

    protected virtual void NotifyMissionManager(bool success)
    {
        if (DuringMissionManager.Instance == null) return;

        if (success)
        {
            // Complete the zone (which completes the task)
            DuringMissionManager.Instance.CompleteActiveZone();
        }
        else
        {
            // Just exit the zone, player can retry
            DuringMissionManager.Instance.ExitFloodZone();
        }
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Called when task UI is shown. Initialize draggables, targets, etc.
    /// </summary>
    protected abstract void OnTaskShow();

    /// <summary>
    /// Called when task UI is hidden. Cleanup resources.
    /// </summary>
    protected abstract void OnTaskHide();

    /// <summary>
    /// Validate if the current state completes the task.
    /// Call this from derived classes when player makes an action.
    /// </summary>
    protected abstract bool ValidateCompletion();

    #endregion

    #region Utility

    /// <summary>
    /// Call from derived classes when player performs a valid action.
    /// </summary>
    protected void CheckCompletion()
    {
        if (!isActive || isCompleted) return;

        if (ValidateCompletion())
        {
            CompleteTask();
        }
    }

    #endregion
}
