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
        // If the disinfect button is already visible on the screen, 
        // it means the player has ALREADY selected a different mud pile.
        if (disinfectButton != null && disinfectButton.activeInHierarchy)
        {
            Debug.Log("Player tried to select another mud pile, but one is already selected!");
            return; 
        }

        // Mark as selected
        isHeld = true;

        // Show the Disinfect Button and DYNAMICALLY WIRE IT!
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
            Debug.LogWarning("MudPileInteraction: Could not find the DisinfectButton! Check its name in the hierarchy.");
        }
    }

    public void CleanPile()
    {
        if (isCleaned) return;
        isCleaned = true;

        // Hide the Disinfect Button now that we clicked it!
        // (This also frees up the player to select the next mud pile!)
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
            AfterRecoveryARController.Instance.HandleItemRecovered(gameObject); 
        }
        else
        {
            Debug.LogError("MudPileInteraction: AfterRecoveryARController.Instance not found!");
        }

        gameObject.SetActive(false);
    }
}