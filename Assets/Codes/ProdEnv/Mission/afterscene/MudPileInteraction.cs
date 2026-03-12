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

        Invoke(nameof(Deactivate), 0.5f);
    }

    private void Deactivate()
    {
        if (AfterRecoveryARController.Instance != null)
        {
            // This will count the mud if it has CleanupItem tag
            AfterRecoveryARController.Instance.HandleItemRecovered(gameObject); 
            Debug.Log($"Mud pile {gameObject.name} cleaned and counted");
        }
        else
        {
            Debug.LogError("MudPileInteraction: AfterRecoveryARController.Instance not found!");
        }

        gameObject.SetActive(false);
    }
}