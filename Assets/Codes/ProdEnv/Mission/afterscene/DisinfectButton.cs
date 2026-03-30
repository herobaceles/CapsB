using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DisinfectButton : MonoBehaviour
{
    [Header("Optional Polish")]
    [Tooltip("Drag the Spray Bottle's Particle System here if you have one!")]
    public ParticleSystem sprayEffect;
    
    [Tooltip("Add an AudioSource with a spray sound if you want!")]
    public AudioSource spraySound;

    [Header("Towel Animation")]
    [Tooltip("Animator for the cleaning towel/cloth. Should have a 'Wipe' trigger.")]
    public Animator towelAnimator;

    // This remembers which mud pile the player is currently "holding"
    private MudPileInteraction heldMud;

    // --- NEW METHOD ADDED HERE ---
    // Link your Main Menu's "Disinfect Mission" button to this function!
    public void SetDisinfectMissionID()
    {
        PlayerPrefs.SetString("SelectedMissionID", "disinfectmission");
        PlayerPrefs.Save();
        Debug.Log("Mission ID set to: disinfectmission");
    }
    // -----------------------------

    // This gets called by your ARTapDetector when the player taps a mud pile
    public void ShowButtonForMud(MudPileInteraction tappedMud)
    {
        heldMud = tappedMud;
        gameObject.SetActive(true); // Reveal the Disinfect Button
    }

    // This method is triggered by the in-game UI Button (OnClick)
    // and starts the timed clean-up sequence.
    public void CleanHeldMud()
    {
        // Avoid starting multiple sequences at once
        if (!gameObject.activeInHierarchy)
            return;

        StartCoroutine(CleanSequence());
    }

    private IEnumerator CleanSequence()
    {
        // Disable the button so it can't be spam-clicked, but
        // keep this GameObject active so the coroutine can finish.
        var uiButton = GetComponent<Button>();
        if (uiButton != null)
        {
            uiButton.interactable = false;
        }

        // 1. Play central spray FX and sound (bottle tip)
        if (sprayEffect != null)
        {
            sprayEffect.Play();
        }

        if (spraySound != null)
        {
            spraySound.Play();
        }

        // 2. Trigger towel wiping animation
        if (towelAnimator != null)
        {
            towelAnimator.SetTrigger("Wipe");
        }

        // 3. Wait for the wipe animation duration before actually cleaning
        yield return new WaitForSeconds(0.6f);

        // 4. Clean the specific mud we are holding
        if (heldMud != null)
        {
            heldMud.CleanPile();
            heldMud = null; // Clear the reference so we don't clean it twice
        }

        // Re-enable the button for next time, then hide it
        if (uiButton != null)
        {
            uiButton.interactable = true;
        }

        gameObject.SetActive(false);
    }
}