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
        // Only count toward progress if this is a CleanupItem
        if (AfterRecoveryARController.Instance != null)
        {
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

        // In both missions, we want a Green Check for the items you're looking for
        bool isCorrectItem = false;

        // Check for CleanupItem tag first - these should ALWAYS be correct
        if (gameObject.CompareTag("CleanupItem"))
        {
            isCorrectItem = true;
        }
        else if (gameObject.CompareTag("SafeItem")) 
        {
            isCorrectItem = true;
        }
        // In Hidden Danger mission, the Snake/Rat are the 'correct' targets
        else if (gameObject.CompareTag("UnsafeItem") && 
            AfterRecoveryARController.Instance.currentMissionMode == MissionMode.HiddenDanger)
        {
            isCorrectItem = true;
        }

        if (isCorrectItem)
        {
            AfterRecoveryARController.Instance.TriggerFeedback(true, transform.position);
            Recover();
        }
        else
        {
            // Red X for anything else
            AfterRecoveryARController.Instance.TriggerFeedback(false, transform.position);
        }
    }
}