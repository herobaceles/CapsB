using System;
using UnityEngine;

public class MudPileInteraction : MonoBehaviour
{
    [Header("Visuals")]
    public ParticleSystem sprayEffect;

    [Header("Disinfect UI")]
    [Tooltip("Reference to the central DisinfectButton controller.")]
    [SerializeField] private DisinfectButton disinfectButton;

    [HideInInspector] public bool isHeld = false; // Tracks if the player has selected this mud pile
    private bool isCleaned = false;
    private MeshRenderer mudRenderer;

    [Header("Scene Controller (optional)")]
    [SerializeField] private AfterSceneController afterSceneController;

    /// <summary>
    /// Raised when this mud pile has been fully cleaned.
    /// HiddenDangerSpawner and other aggregators can subscribe
    /// to track overall progress for the AR task.
    /// </summary>
    public event Action<MudPileInteraction> OnCleaned;

    private void Start()
    {
        mudRenderer = GetComponent<MeshRenderer>();

        // Ensure the mud has the CleanupItem tag for counting
        if (!gameObject.CompareTag("CleanupItem"))
        {
            Debug.LogWarning($"Mud pile {gameObject.name} should have 'CleanupItem' tag for counting!");
        }

        // Try to auto-bind the DisinfectButton if it was not wired in the Inspector.
        // Use Resources.FindObjectsOfTypeAll so we can find it even if the UI is inactive
        // when this mud pile starts up.
        if (disinfectButton == null)
        {
            var buttons = Resources.FindObjectsOfTypeAll<DisinfectButton>();
            foreach (var button in buttons)
            {
                if (button != null && button.gameObject.scene.isLoaded)
                {
                    disinfectButton = button;
                    Debug.Log($"MudPileInteraction: Bound DisinfectButton reference at Start for {gameObject.name}", this);
                    break;
                }
            }

            if (disinfectButton == null)
            {
                Debug.LogWarning("MudPileInteraction: DisinfectButton reference not set in Inspector and could not be found in loaded scenes.", this);
            }
        }
    }

    public void PickUpMud(Camera arCamera)
    {
        if (isCleaned || isHeld) return;

        // If the button reference was lost (e.g., due to AR scene activation order),
        // try to resolve it again at tap time.
        if (disinfectButton == null)
        {
            var buttons = Resources.FindObjectsOfTypeAll<DisinfectButton>();
            foreach (var button in buttons)
            {
                if (button != null && button.gameObject.scene.isLoaded)
                {
                    disinfectButton = button;
                    Debug.Log($"MudPileInteraction: Rebound DisinfectButton reference at tap time for {gameObject.name}", this);
                    break;
                }
            }
        }

        // If we still can't find the button, do NOT lock this mud as held.
        if (disinfectButton == null)
        {
            Debug.LogWarning("MudPileInteraction: Could not find the DisinfectButton controller! Mud will not be held.", this);
            return;
        }

        // Prevent selecting multiple mud piles at the same time!
        if (disinfectButton.gameObject.activeInHierarchy)
        {
            Debug.Log("Player tried to select another mud pile, but one is already selected!");
            return; 
        }

        isHeld = true;

        // Show the central Disinfect Button for this mud pile
        disinfectButton.ShowButtonForMud(this);
    }

    public void CleanPile()
    {
        if (isCleaned) return;
        isCleaned = true;
        isHeld = false;

        if (sprayEffect != null)
        {
            sprayEffect.Play();
        }

        if (mudRenderer != null)
        {
            mudRenderer.enabled = false;
        }

        OnCleaned?.Invoke(this);

        // Now that the wipe animation and delay have completed in DisinfectButton,
        // we can immediately report recovery and deactivate this mud pile.
        Deactivate();
    }

    private void Deactivate()
    {
        // Prefer reporting via AfterSceneController (new architecture),
        // but keep the legacy AfterRecoveryARController path as a fallback.
        if (afterSceneController != null)
        {
            afterSceneController.OnGenericItemRecovered(gameObject);
            Debug.Log($"Mud pile {gameObject.name} cleaned and reported to AfterSceneController");
        }
        else if (AfterRecoveryARController.Instance != null)
        {
            // This will count the mud if it has CleanupItem tag
            AfterRecoveryARController.Instance.HandleItemRecovered(gameObject); 
            Debug.Log($"Mud pile {gameObject.name} cleaned and counted via legacy controller");
        }
        else
        {
            Debug.LogError("MudPileInteraction: No AfterSceneController or AfterRecoveryARController found for recovery reporting!");
        }

        gameObject.SetActive(false);
    }
}   