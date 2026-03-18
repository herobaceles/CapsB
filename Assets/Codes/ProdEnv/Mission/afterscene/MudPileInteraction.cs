using System;
using UnityEngine;
using UnityEngine.UI;

public class MudPileInteraction : MonoBehaviour
{
    [Header("Visuals")]
    public ParticleSystem sprayEffect;

    [Header("UI Elements")]
    [Tooltip("The button that appears when the mud is selected.")]
    public GameObject disinfectButton;

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

        if (disinfectButton == null)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "DisinfectButton" && obj.scene.isLoaded)
                {
                    disinfectButton = obj;
                    break;
                }
            }
        }
    }

    public void PickUpMud(Camera arCamera)
    {
        if (isCleaned || isHeld) return;

        // Prevent selecting multiple mud piles at the same time!
        if (disinfectButton != null && disinfectButton.activeInHierarchy)
        {
            Debug.Log("Player tried to select another mud pile, but one is already selected!");
            return; 
        }

        isHeld = true;

        // Show the Disinfect Button
        if (disinfectButton != null)
        {
            disinfectButton.SetActive(true);

            Button btn = disinfectButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners(); 
                btn.onClick.AddListener(CleanPile); 
            }
        }
        else
        {
            Debug.LogWarning("MudPileInteraction: Could not find the DisinfectButton!");
        }
    }

    public void CleanPile()
    {
        if (isCleaned) return;
        isCleaned = true;

        if (disinfectButton != null)
        {
            disinfectButton.SetActive(false);
        }

        if (sprayEffect != null)
        {
            sprayEffect.Play();
        }

        if (mudRenderer != null)
        {
            mudRenderer.enabled = false;
        }

        OnCleaned?.Invoke(this);

        Invoke(nameof(Deactivate), 0.5f);
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