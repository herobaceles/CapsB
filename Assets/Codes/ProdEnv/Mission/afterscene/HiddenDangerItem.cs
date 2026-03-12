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

    public void Recover()
    {
        if (IsRecovered || isStationaryFeedbackOnly) return;

        IsRecovered = true;
        OnRecovered?.Invoke(this);

        // This tells the AfterRecoveryARController: "One more item collected!"
        if (AfterRecoveryARController.Instance != null)
        {
            Debug.LogWarning($"HiddenDangerItem.Recover: Calling HandleItemRecovered for '{gameObject.name}' with tag '{gameObject.tag}'");
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
        if (AfterRecoveryARController.Instance == null || IsRecovered) return;

        Debug.LogWarning($"HiddenDangerItem.OnMouseDown: Item '{gameObject.name}' clicked in mode {AfterRecoveryARController.Instance.currentMissionMode}");

        // In Hidden Danger mission, we DON'T want to recover on tap
        // We want the player to drag to bucket
        if (AfterRecoveryARController.Instance.currentMissionMode == MissionMode.HiddenDanger)
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
            AfterRecoveryARController.Instance.TriggerFeedback(true, transform.position);
            Recover();
        }
        else
        {
            // Red X for anything else
            Debug.LogWarning($"HiddenDangerItem: Item '{gameObject.name}' with tag '{gameObject.tag}' is incorrect for mode {AfterRecoveryARController.Instance.currentMissionMode}");
            AfterRecoveryARController.Instance.TriggerFeedback(false, transform.position);
        }
    }
}