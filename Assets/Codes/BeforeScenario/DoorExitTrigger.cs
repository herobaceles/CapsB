using UnityEngine;
using System.Collections;

/// <summary>
/// Door trigger that teleports the player to a new location (outside).
/// Attach this to your door GameObject with a trigger collider.
/// </summary>
public class DoorExitTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requireMissionsComplete = true;

    [Header("Teleport Destination")]
    [SerializeField] private Transform teleportDestination;
    [Tooltip("Optional: If not set, will use this GameObject's position")]
    
    [Header("Teleport Settings")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private bool disablePlayerControlDuringTeleport = true;

    [Header("Optional Effects")]
    [SerializeField] private GameObject doorOpenEffect;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private string teleportDialogue = "Going outside...";
    [SerializeField] private bool showTeleportDialogue = true;

    private bool hasTriggered = false;
    private AudioSource audioSource;

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
            Debug.LogWarning($"[DoorExitTrigger] No Collider found on {gameObject.name}. Please add a Collider component and set it as trigger.");
        }

        // Get or add audio source for sound effects
        if (doorOpenSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (!other.CompareTag(playerTag))
            return;

        if (hasTriggered)
            return;

        // Check if missions are complete (if required)
        if (requireMissionsComplete && BeforeSceneManager.Instance != null)
        {
            if (!BeforeSceneManager.Instance.CanStartEvacuation())
            {
                Debug.Log("[DoorExitTrigger] Cannot go outside yet. Complete all missions first!");
                return;
            }
        }

        hasTriggered = true;

        Debug.Log($"[DoorExitTrigger] Player entered door trigger on {gameObject.name}");

        // Start teleport sequence
        StartCoroutine(TeleportPlayerSequence(other.gameObject));
    }

    private IEnumerator TeleportPlayerSequence(GameObject player)
    {
        // Double-check mission completion at the moment of teleport.
        // This ensures teleport won't occur even if the inspector flag was changed
        // or another caller bypassed OnTriggerEnter's initial guard.
        if (requireMissionsComplete && BeforeSceneManager.Instance != null && !BeforeSceneManager.Instance.CanStartEvacuation())
        {
            Debug.Log("[DoorExitTrigger] Teleport blocked: missions not complete.");
            // Show friendly feedback if available
            if (BeforeSceneManager.Instance != null)
                BeforeSceneManager.Instance.ShowGameplayDialogue("You must complete all missions before going outside.");

            // Allow retriggering later
            hasTriggered = false;
            yield break;
        }
        // Show dialogue if enabled
        if (showTeleportDialogue && BeforeSceneManager.Instance != null)
        {
            BeforeSceneManager.Instance.ShowGameplayDialogue(teleportDialogue);
            yield return new WaitForSeconds(2f);
        }

        // Play door sound
        if (audioSource != null && doorOpenSound != null)
        {
            audioSource.PlayOneShot(doorOpenSound);
        }

        // Show door effect
        if (doorOpenEffect != null)
        {
            doorOpenEffect.SetActive(true);
        }

        // Disable player control if needed
        if (disablePlayerControlDuringTeleport && BeforeSceneManager.Instance != null)
        {
            // Use existing player stop method
            // BeforeSceneManager.Instance.StopPlayerMovement();
        }

        // Optional: Fade out (you can implement screen fade later)
        yield return new WaitForSeconds(fadeOutDuration);

        // Teleport the player
        TeleportPlayer(player);

        // Optional: Fade in
        yield return new WaitForSeconds(fadeInDuration);

        // Re-enable player control
        if (disablePlayerControlDuringTeleport && BeforeSceneManager.Instance != null)
        {
            // BeforeSceneManager.Instance.ResumePlayerMovement();
        }

        // Notify BeforeSceneManager (optional - for tracking progression)
        if (BeforeSceneManager.Instance != null)
        {
            BeforeSceneManager.Instance.OnPlayerWentOutside();
        }

        Debug.Log("[DoorExitTrigger] Teleport complete!");
    }

    private void TeleportPlayer(GameObject player)
    {
        if (teleportDestination != null)
        {
            // Disable CharacterController temporarily if exists
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // Teleport
            player.transform.position = teleportDestination.position;
            player.transform.rotation = teleportDestination.rotation;

            Debug.Log($"[DoorExitTrigger] Teleported player to {teleportDestination.name}");

            // Re-enable CharacterController
            if (cc != null)
            {
                cc.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning("[DoorExitTrigger] No teleport destination assigned!");
        }
    }

    /// <summary>
    /// Manual trigger for testing or UI buttons
    /// </summary>
    public void TriggerManually()
    {
        if (hasTriggered) return;

        // Respect mission gating for manual triggers as well
        if (requireMissionsComplete && BeforeSceneManager.Instance != null && !BeforeSceneManager.Instance.CanStartEvacuation())
        {
            Debug.Log("[DoorExitTrigger] Manual trigger blocked: missions not complete.");
            if (BeforeSceneManager.Instance != null)
                BeforeSceneManager.Instance.ShowGameplayDialogue("You must complete all missions before going outside.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            hasTriggered = true;
            StartCoroutine(TeleportPlayerSequence(player));
        }
    }

    /// <summary>
    /// Reset for testing
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
