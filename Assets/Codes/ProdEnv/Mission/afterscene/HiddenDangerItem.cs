using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class HiddenDangerItem : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName;

    [Tooltip("If unchecked, the item disappears when tapped. (Leave unchecked for Bucket now!)")]
    public bool isStationaryFeedbackOnly = false;

    [Header("Events")]
    public UnityAction<HiddenDangerItem> OnRecovered;
    public bool IsRecovered { get; private set; }

    [Header("Scene Controller (optional)")]
    [SerializeField] private AfterSceneController afterSceneController;

    [Header("Hidden Danger Audio (optional)")]
    [Tooltip("Looping hiss/squeak AudioSource for this hazard while being dragged (Hidden Danger mode only).")]
    public AudioSource hazardLoopSource;

    [Tooltip("One-shot AudioSource played when this hazard is successfully dropped into the bucket.")]
    public AudioSource hazardCaptureSource;

    private MissionMode GetCurrentMissionMode()
    {
        // When an AR session is running, prefer the shared AfterRecoveryARController
        // as the source of truth for the current mission mode. This keeps
        // behaviour consistent even if the legacy AfterSceneController isn't
        // kept in sync with the new mission flow.
        if (AfterRecoveryARController.Instance != null && AfterRecoveryARController.Instance.IsARActive)
        {
            return AfterRecoveryARController.Instance.CurrentMissionMode;
        }

        // Otherwise, fall back to the scene controller if available.
        if (afterSceneController != null)
        {
            return afterSceneController.GetCurrentMissionMode();
        }

        // Final fallback: cached AR controller mode if present, or a
        // safe default when nothing else is available.
        if (AfterRecoveryARController.Instance != null)
        {
            return AfterRecoveryARController.Instance.CurrentMissionMode;
        }

        return MissionMode.HiddenDanger;
    }

    public void Recover()
    {
        if (IsRecovered || isStationaryFeedbackOnly) return;

        IsRecovered = true;
        OnRecovered?.Invoke(this);

        // Report recovery into the mission system via the new controller when available,
        // falling back to the legacy AfterRecoveryARController path.
        if (afterSceneController != null)
        {
            Debug.LogWarning($"HiddenDangerItem.Recover: Reporting recovered object '{gameObject.name}' to AfterSceneController.");
            afterSceneController.OnGenericItemRecovered(gameObject);
        }
        else if (AfterRecoveryARController.Instance != null)
        {
            Debug.LogWarning($"HiddenDangerItem.Recover: Calling HandleItemRecovered for '{gameObject.name}' with tag '{gameObject.tag}' via legacy controller.");
            AfterRecoveryARController.Instance.HandleItemRecovered(gameObject);
        }

        StartCoroutine(DisableAfterDelay());
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(0.15f);
        gameObject.SetActive(false);
    }

    private void OnMouseDown()
    {
        // During AR sessions we rely on AfterARTapDetector driving
        // interactions, so ignore legacy OnMouseDown in that case.
        if (AfterRecoveryARController.Instance != null && AfterRecoveryARController.Instance.IsARActive)
            return;

        HandleTap();
    }

    /// <summary>
    /// Public entry point for AR tap detectors. This allows AR-specific
    /// raycasters (using the AR camera) to drive the same behaviour that
    /// OnMouseDown uses, which is more reliable on mobile AR than the
    /// legacy OnMouseDown callback.
    /// </summary>
    public void OnTappedFromAR()
    {
        HandleTap();
    }

    /// <summary>
    /// Core tap handling shared by both OnMouseDown and OnTappedFromAR.
    /// </summary>
    private void HandleTap()
    {
        if (IsRecovered) return;

        MissionMode mode = GetCurrentMissionMode();

        Debug.LogWarning($"HiddenDangerItem.HandleTap: Item '{gameObject.name}' clicked in mode {mode}");

        // In Hidden Danger mission, we DON'T want to recover on tap
        // We want the player to drag to bucket
        if (mode == MissionMode.HiddenDanger)
        {
            Debug.LogWarning($"HiddenDangerItem: In HiddenDanger mode, item '{gameObject.name}' will be dragged, not recovered yet");
            // Don't recover or show feedback on tap
            // The ARTapDetector will handle the dragging
            return;
        }

        // For other missions (CleanupGear, KitchenSafety, DisinfectHouse)
        bool isCorrectItem = false;

        // Check for CleanupItem tag first - these should ALWAYS be correct
        if (gameObject.CompareTag("CleanupItem"))
        {
            isCorrectItem = true;
            Debug.LogWarning($"HiddenDangerItem: Cleanup Gear item '{gameObject.name}' with tag 'CleanupItem' is correct");
        }
        else if (gameObject.CompareTag("SafeItem")) 
        {
            isCorrectItem = true;
            Debug.LogWarning($"HiddenDangerItem: Kitchen Safety item '{gameObject.name}' with tag 'SafeItem' is correct");
        }

        if (isCorrectItem)
        {
            // Prefer AR controller feedback when an AR session is active,
            // otherwise fall back to the legacy AfterSceneController path.
            if (AfterRecoveryARController.Instance != null && AfterRecoveryARController.Instance.IsARActive)
            {
                AfterRecoveryARController.Instance.TriggerFeedback(true, transform.position);
            }
            else if (afterSceneController != null)
            {
                afterSceneController.ShowFeedback(true, transform.position);
            }
            else if (AfterRecoveryARController.Instance != null)
            {
                AfterRecoveryARController.Instance.TriggerFeedback(true, transform.position);
            }

            Recover();
        }
        else
        {
            // Red X for anything else
            Debug.LogWarning($"HiddenDangerItem: Item '{gameObject.name}' with tag '{gameObject.tag}' is incorrect for mode {mode}");

            if (AfterRecoveryARController.Instance != null && AfterRecoveryARController.Instance.IsARActive)
            {
                AfterRecoveryARController.Instance.TriggerFeedback(false, transform.position);
            }
            else if (afterSceneController != null)
            {
                afterSceneController.ShowFeedback(false, transform.position);
            }
            else if (AfterRecoveryARController.Instance != null)
            {
                AfterRecoveryARController.Instance.TriggerFeedback(false, transform.position);
            }
        }
    }
}