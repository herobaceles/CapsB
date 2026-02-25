using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Place this component on a GameObject with a Collider (set as Trigger).
/// When the player enters the trigger zone, it notifies the MissionSceneManager.
/// Works with BeforeMissionManager, DuringMissionManager, or AfterMissionManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TaskTrigger : MonoBehaviour
{
    [Header("Task Configuration")]
    [Tooltip("Must match the taskId in MissionData")]
    [SerializeField] private string taskId;
    public string TaskId => taskId;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayer = -1;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject activeIndicator;
    [SerializeField] private GameObject completedIndicator;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("Behavior")]
    [SerializeField] private bool completeOnEnter = true;
    [SerializeField] private bool destroyOnComplete = false;
    [SerializeField] private float interactionDelay = 0f;

    [Header("Events")]
    public UnityEvent OnPlayerEntered;
    public UnityEvent OnPlayerExited;
    public UnityEvent OnTaskCompleted;
    
    private Collider triggerCollider;
    private bool isActive = false;
    private bool isCompleted = false;
    private bool playerInside = false;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        // Hide indicators initially
        if (activeIndicator != null) activeIndicator.SetActive(false);
        if (completedIndicator != null) completedIndicator.SetActive(false);
    }

    private void Start()
    {
        // Register with the current MissionSceneManager (Before/During/After)
        if (MissionSceneManager.Instance != null)
        {
            MissionSceneManager.Instance.RegisterTrigger(this);
        }
        else
        {
            Debug.LogWarning($"TaskTrigger '{taskId}': MissionSceneManager not found!");
        }
    }

    private void OnDestroy()
    {
        // Unregister from MissionSceneManager
        if (MissionSceneManager.Instance != null)
        {
            MissionSceneManager.Instance.UnregisterTrigger(this);
        }
    }

    /// <summary>
    /// Called by MissionManager to activate/deactivate this trigger.
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = active;
        }

        // Show/hide active indicator
        if (activeIndicator != null)
        {
            activeIndicator.SetActive(active && !isCompleted);
        }

        Debug.Log($"TaskTrigger '{taskId}': Set active = {active}");

        // If player was already inside when activated, complete immediately
        if (active && playerInside && completeOnEnter)
        {
            CompleteTrigger();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        playerInside = true;
        OnPlayerEntered?.Invoke();

        if (isActive && completeOnEnter && !isCompleted)
        {
            if (interactionDelay > 0)
            {
                StartCoroutine(CompleteAfterDelay());
            }
            else
            {
                CompleteTrigger();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        playerInside = false;
        OnPlayerExited?.Invoke();
    }

    private System.Collections.IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSeconds(interactionDelay);

        // Check player is still inside
        if (playerInside && isActive && !isCompleted)
        {
            CompleteTrigger();
        }
    }

    /// <summary>
    /// Manually complete this trigger (can be called from UI button, etc.)
    /// </summary>
    public void CompleteTrigger()
    {
        if (isCompleted) return;

        isCompleted = true;
        isActive = false;

        Debug.Log($"TaskTrigger '{taskId}': Completed!");

        // Update visuals
        if (activeIndicator != null) activeIndicator.SetActive(false);
        if (completedIndicator != null) completedIndicator.SetActive(true);

        // Notify MissionSceneManager
        if (MissionSceneManager.Instance != null)
        {
            MissionSceneManager.Instance.OnTriggerActivated(taskId);
        }

        OnTaskCompleted?.Invoke();

        if (destroyOnComplete)
        {
            Destroy(gameObject, 0.5f);
        }
    }

    private bool IsPlayer(Collider other)
    {
        // Check by tag
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
        {
            return true;
        }

        // Check by layer
        if (playerLayer != -1 && ((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reset this trigger for reuse.
    /// </summary>
    public void ResetTrigger()
    {
        isCompleted = false;
        isActive = false;
        playerInside = false;

        if (activeIndicator != null) activeIndicator.SetActive(false);
        if (completedIndicator != null) completedIndicator.SetActive(false);

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }

    #region Editor Visualization

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? gizmoColor : new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.1f);

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }

    #endregion

    #region Queries

    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public bool IsPlayerInside => playerInside;

    #endregion
}
