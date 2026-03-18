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

    private MissionMode GetCurrentMissionMode()
    {
        // Use the scene controller as the primary source:
        // it is updated to the actual AR mode when AR starts
        // (CleanupGear, HiddenDanger, etc.).
        // Primary source: AfterSceneController, which is updated
        // to the actual AR mode (CleanupGear, HiddenDanger, etc.)
        // whenever AR starts for a given task.
        if (afterSceneController != null)
        {
            return afterSceneController.GetCurrentMissionMode();
        }

        // Fallback: legacy AR controller cached mode.
        if (AfterRecoveryARController.Instance != null)
        {
            return AfterRecoveryARController.Instance.CurrentMissionMode;
        }

        // Safe default.
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
        if (IsRecovered) return;

        MissionMode mode = GetCurrentMissionMode();

        Debug.LogWarning($"HiddenDangerItem.OnMouseDown: Item '{gameObject.name}' clicked in mode {mode}");

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
            if (afterSceneController != null)
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

            if (afterSceneController != null)
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