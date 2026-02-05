using UnityEngine;

public class GoBagMissionTrigger : MonoBehaviour
{
    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";

    [Header("One-shot")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Disable Trigger After Start")]
    [SerializeField] private bool disableObjectAfterStart = true;

    private bool fired;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && fired) return;
        if (!other.CompareTag(playerTag)) return;

        Debug.Log($"[GoBagMissionTrigger] Trigger stepped by: {other.name}");

        fired = true;

        if (BeforeSceneManager.Instance != null)
        {
            BeforeSceneManager.Instance.StartARGameFromTrigger();
        }
        else
        {
            Debug.LogWarning("BeforeSceneManager.Instance is null. Make sure the BeforeSceneManager exists in the scene.");
        }

        if (disableObjectAfterStart)
        {
            gameObject.SetActive(false);
        }
    }
}
