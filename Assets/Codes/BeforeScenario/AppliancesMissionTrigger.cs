using UnityEngine;

/// <summary>
/// Attach this script to the appliances/furniture GameObject.
/// Add a Collider component (set as Trigger) to detect when the player approaches.
/// </summary>
public class AppliancesMissionTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool triggerOnce = true;

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private GameObject highlightEffect;
    [SerializeField] private bool disableHighlightAfterTrigger = true;

    private bool hasTriggered = false;

    private void Start()
    {
        // Ensure we have a trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"[AppliancesMissionTrigger] No Collider found on {gameObject.name}. Please add a Collider component and set it as trigger.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if already triggered
        if (triggerOnce && hasTriggered)
            return;

        // Check if it's the player
        if (!other.CompareTag(playerTag))
            return;

        // Mark as triggered
        hasTriggered = true;

        Debug.Log($"[AppliancesMissionTrigger] Player entered appliances trigger on {gameObject.name}");

        // Notify the BeforeSceneManager
        if (BeforeSceneManager.Instance != null)
        {
            BeforeSceneManager.Instance.OnAppliancesMissionFound();
        }
        else
        {
            Debug.LogError("[AppliancesMissionTrigger] BeforeSceneManager.Instance is null!");
        }

        // Optional: Disable highlight effect
        if (disableHighlightAfterTrigger && highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
    }

    /// <summary>
    /// Call this if you want to manually trigger the appliances mission (e.g., from a UI button)
    /// </summary>
    public void TriggerManually()
    {
        if (triggerOnce && hasTriggered)
            return;

        hasTriggered = true;

        if (BeforeSceneManager.Instance != null)
        {
            BeforeSceneManager.Instance.OnAppliancesMissionFound();
        }

        if (disableHighlightAfterTrigger && highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
    }

    // Optional: Reset for testing
    public void ResetTrigger()
    {
        hasTriggered = false;
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(true);
        }
    }
}
