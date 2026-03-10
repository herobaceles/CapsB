using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class HiddenDangerItem : MonoBehaviour
{
    [Header("Item Settings")]
    [Tooltip("Name of the hidden danger item (Example: Snake, Rat)")]
    public string itemName;

    [Header("Events")]
    public UnityAction<HiddenDangerItem> OnRecovered;

    public bool IsRecovered { get; private set; }

    // This is called by the AR Manager/Tap Detector, AND our fallback OnMouseDown
    public void Recover()
    {
        if (IsRecovered) return;

        IsRecovered = true;
        Debug.Log($"Hidden Danger Found: {itemName}");

        // Tell the manager this item was recovered without strongly coupling to it
        OnRecovered?.Invoke(this);

        // Delay hiding the object slightly so Unity's physics and visual feedback can finish
        StartCoroutine(DisableAfterDelay());
    }

    private IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(0.15f);
        gameObject.SetActive(false);
    }

    // Fallback: If the AR Tap Detector raycast misses in the Editor, Unity's native mouse click will catch it!
    private void OnMouseDown()
    {
        if (AfterRecoveryARController.Instance == null || IsRecovered) return;

        MissionMode mode = AfterRecoveryARController.Instance.currentMissionMode;

        // STRICT TAG CHECK: Feedback only appears for these exact tags!
        if (gameObject.CompareTag("SafeItem"))
        {
            // We now pass the item's position to the controller
            AfterRecoveryARController.Instance.TriggerFeedback(true, transform.position);
            Recover();
        }
        else if (gameObject.CompareTag("UnsafeItem"))
        {
            // We now pass the item's position to the controller
            AfterRecoveryARController.Instance.TriggerFeedback(false, transform.position);
        }
        else
        {
            if (mode == MissionMode.HiddenDanger)
            {
                Recover();
            }
        }
    }
}